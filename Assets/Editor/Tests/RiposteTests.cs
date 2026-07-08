using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class RiposteTests
    {
        private CombatConfig config;
        private CombatCharacter attacker;
        private CombatCharacter target;
        private CombatCharacter guardian;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();
            config = CombatTestHelper.CreateDefaultConfig();
            
            attacker = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Enemy, 1, attack: 20, defense: 0, accuracy: 100, maxHP: 100);
            target = CombatTestHelper.CreateCombatCharacter("Target", Team.Player, 1, attack: 30, defense: 0, accuracy: 100, maxHP: 100);
            guardian = CombatTestHelper.CreateCombatCharacter("Guardian", Team.Player, 2, attack: 40, defense: 0, accuracy: 100, maxHP: 100);
        }

        [TearDown]
        public void Teardown()
        {
            CombatTestHelper.CleanupTestDatabase();
            if (attacker != null) Object.DestroyImmediate(attacker.gameObject);
            if (target != null) Object.DestroyImmediate(target.gameObject);
            if (guardian != null) Object.DestroyImmediate(guardian.gameObject);
            if (config != null) ScriptableObject.DestroyImmediate(config, true);
        }

        private BattleSystem MakeBattleSystem()
        {
            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();
            
            // Call StartBattle to initialize internal RNG and teams
            bs.StartBattle(new List<CombatCharacter> { target, guardian }, new List<CombatCharacter> { attacker });

            // Inject a deterministic RNG seed for test predictability
            var rng = new System.Random(42);
            typeof(BattleSystem).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(bs, rng);

            return bs;
        }

        private void CallExecuteSkill(BattleSystem bs, CombatCharacter user, SkillData skill, List<CombatCharacter> targets)
        {
            bs.ExecuteSkill(user, skill, targets);
        }

        [Test]
        public void Riposte_TriggersCounterAttack_OnAttackTargeted()
        {
            var bs = MakeBattleSystem();
            
            // Add Riposte to target (100% amplitude)
            target.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));

            var skill = CombatTestHelper.CreateDamageSkill(1.0f);
            skill.guaranteedHit = true;
            skill.effects.Add(new DamageEffect());

            int initialTargetHP = target.currentHP;
            int initialAttackerHP = attacker.currentHP;

            CallExecuteSkill(bs, attacker, skill, new List<CombatCharacter> { target });

            Assert.Less(target.currentHP, initialTargetHP, "Target should have taken damage from the attack.");
            Assert.Less(attacker.currentHP, initialAttackerHP, "Attacker should have taken damage from the counter-attack.");
            
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Riposte_DoesNotTrigger_IfStunned()
        {
            var bs = MakeBattleSystem();
            
            target.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));
            
            // Apply stun
            target.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));

            var skill = CombatTestHelper.CreateDamageSkill(1.0f);
            skill.guaranteedHit = true;
            skill.effects.Add(new DamageEffect());

            int initialAttackerHP = attacker.currentHP;
            CallExecuteSkill(bs, attacker, skill, new List<CombatCharacter> { target });

            Assert.AreEqual(initialAttackerHP, attacker.currentHP, "Attacker should not take damage because the riposter is stunned.");
            
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Riposte_DoesNotTrigger_IfStunnedByAttack()
        {
            var bs = MakeBattleSystem();
            
            target.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));
            
            // Create a skill that deals damage AND applies stun with 100% chance
            var skill = CombatTestHelper.CreateDamageSkill(1.0f);
            skill.guaranteedHit = true;
            skill.effects.Add(new DamageEffect());
            var stunEffect = new StatusEffect
            {
                statusType = StatusType.Stun,
                applicationChance = 300f,
                duration = 1
            };
            skill.effects.Add(stunEffect);

            int initialAttackerHP = attacker.currentHP;
            CallExecuteSkill(bs, attacker, skill, new List<CombatCharacter> { target });

            Assert.IsTrue(target.isStunned, "Target should have been stunned by the attack.");
            Assert.AreEqual(initialAttackerHP, attacker.currentHP, "Attacker should not take damage because the target was stunned by the attack.");
            
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Riposte_SubjectToHitCritDefense()
        {
            var bs = MakeBattleSystem();
            
            // Attacker has high dodge, riposter has low accuracy
            attacker.baseStats.dodge = 95;
            target.baseStats.accuracy = 0;
            
            target.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));

            var skill = CombatTestHelper.CreateDamageSkill(1.0f);
            skill.guaranteedHit = true;

            int initialAttackerHP = attacker.currentHP;
            CallExecuteSkill(bs, attacker, skill, new List<CombatCharacter> { target });

            // Due to accuracy 0 vs dodge 95, the counter-attack should miss
            Assert.AreEqual(initialAttackerHP, attacker.currentHP, "Counter-attack should miss due to dodge and accuracy difference.");
            
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Riposte_NoCircularTrigger()
        {
            var bs = MakeBattleSystem();
            
            // Both have riposte
            target.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));
            attacker.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));

            var skill = CombatTestHelper.CreateDamageSkill(1.0f);
            skill.guaranteedHit = true;
            skill.effects.Add(new DamageEffect());

            int initialAttackerHP = attacker.currentHP;
            CallExecuteSkill(bs, attacker, skill, new List<CombatCharacter> { target });

            Assert.Less(attacker.currentHP, initialAttackerHP, "Attacker should take damage from one counter-attack.");
            
            // If circular triggers existed, they would bounce until one died.
            // Since maxHP is 100, and one counter-attack does around 24-36 damage, 
            // if it were circular, attacker HP would be very low or 0.
            Assert.Greater(attacker.currentHP, 40, "Attacker should not be dead from circular counter-attacks.");
            
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Riposte_GuardRedirection()
        {
            var bs = MakeBattleSystem();
            
            // target is guarded by guardian
            target.AddStatus(new GuardStatusInstance(guardian, 3));
            
            // guardian has riposte
            guardian.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));
            // target does not have riposte

            var skill = CombatTestHelper.CreateDamageSkill(1.0f);
            skill.guaranteedHit = true;
            skill.effects.Add(new DamageEffect());

            int initialTargetHP = target.currentHP;
            int initialGuardianHP = guardian.currentHP;
            int initialAttackerHP = attacker.currentHP;

            CallExecuteSkill(bs, attacker, skill, new List<CombatCharacter> { target });

            Assert.AreEqual(initialTargetHP, target.currentHP, "Target should not take damage (guarded).");
            Assert.Less(guardian.currentHP, initialGuardianHP, "Guardian should take damage (interception).");
            Assert.Less(attacker.currentHP, initialAttackerHP, "Attacker should take damage from guardian's riposte.");
            
            Object.DestroyImmediate(bs.gameObject);
        }
        
        [Test]
        public void Riposte_DoesNotTrigger_IfRiposterDies()
        {
            var bs = MakeBattleSystem();
            
            target.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));

            // Create a skill that deals lethal damage
            var skill = CombatTestHelper.CreateDamageSkill(10.0f); // 10x multiplier
            skill.guaranteedHit = true;
            skill.effects.Add(new DamageEffect());

            int initialAttackerHP = attacker.currentHP;
            CallExecuteSkill(bs, attacker, skill, new List<CombatCharacter> { target });

            Assert.IsFalse(target.IsAlive, "Target should have died from the attack.");
            Assert.AreEqual(initialAttackerHP, attacker.currentHP, "Attacker should not take damage because the riposter died.");
            
            Object.DestroyImmediate(bs.gameObject);
        }
    }
}
