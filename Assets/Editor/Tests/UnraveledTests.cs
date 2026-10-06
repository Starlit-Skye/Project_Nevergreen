using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class UnraveledTests
    {
        private CombatConfig config;
        private CombatCharacter attacker;
        private CombatCharacter target;
        private BattleSystem bs;
        private SkillData damageSkill;
        private SkillData nonDamageSkill;
        private SkillExecutor executor;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();
            config = CombatTestHelper.CreateDefaultConfig();
            
            attacker = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Enemy, 1, attack: 100, defense: 0, accuracy: 100, critChance: 0, maxHP: 100);
            target = CombatTestHelper.CreateCombatCharacter("Target", Team.Player, 1, attack: 100, defense: 0, accuracy: 100, critChance: 0, maxHP: 500);

            var bsGo = new GameObject("BS");
            bs = bsGo.AddComponent<BattleSystem>();
            
            bs.StartBattle(new List<CombatCharacter> { target }, new List<CombatCharacter> { attacker });

            var rng = new System.Random(42);
            typeof(BattleSystem).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(bs, rng);

            executor = new SkillExecutor();
            executor.Initialize(null, null, bs);

            // Set fixed rolls to eliminate RNG variance in damage
            GameDatabase.Instance.CombatConfig.attackRollMin = 1.0f;
            GameDatabase.Instance.CombatConfig.attackRollMax = 1.0f;

            // Create Skills
            damageSkill = CombatTestHelper.CreateDamageSkill(1.0f);
            damageSkill.skillId = "damage_attack";
            damageSkill.guaranteedHit = true;
            damageSkill.effects.Add(new DamageEffect());

            nonDamageSkill = ScriptableObject.CreateInstance<SkillData>();
            nonDamageSkill.skillId = "debuff_attack";
            nonDamageSkill.modifier = new SkillModifier { damagePercent = 0f };
            nonDamageSkill.guaranteedHit = true;
            nonDamageSkill.targetScope = TargetScope.Enemies;
        }

        [TearDown]
        public void Teardown()
        {
            if (damageSkill != null && !UnityEditor.EditorUtility.IsPersistent(damageSkill)) Object.DestroyImmediate(damageSkill);
            if (nonDamageSkill != null && !UnityEditor.EditorUtility.IsPersistent(nonDamageSkill)) Object.DestroyImmediate(nonDamageSkill);
            CombatTestHelper.CleanupTestDatabase();
            if (attacker != null) Object.DestroyImmediate(attacker.gameObject);
            if (target != null) Object.DestroyImmediate(target.gameObject);
            if (bs != null) Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Unraveled_IncreasesDamageTaken_FromNormalAttack()
        {
            // Base attack is 100, multiplier is 1.0, so base damage is 100.
            // Add Unraveled amplitude 30 -> damage multiplier should be +30% -> 130 damage.
            var unraveled = StatusInstanceFactory.Create(
                new SkillContext(attacker, damageSkill, new List<CombatCharacter> { target }, bs, new System.Random()),
                StatusType.Unraveled, StatTarget.Defense, 30, 2, AmplitudeType.Flat);
            
            target.AddStatus(unraveled);

            int startHP = target.currentHP;

            executor.Execute(attacker, damageSkill, new List<CombatCharacter> { target }, new System.Random());

            int damageTaken = startHP - target.currentHP;
            
            Assert.AreEqual(130, damageTaken);
        }

        [Test]
        public void Unraveled_StacksProperly_AddingAmplitudes()
        {
            // Base attack is 100
            // Add Unraveled amplitude 15 and 25 -> damage multiplier should be +40% -> 140 damage.
            var ctx = new SkillContext(attacker, damageSkill, new List<CombatCharacter> { target }, bs, new System.Random());
            
            var unraveled1 = StatusInstanceFactory.Create(ctx, StatusType.Unraveled, StatTarget.Defense, 15, 2, AmplitudeType.Flat);
            var unraveled2 = StatusInstanceFactory.Create(ctx, StatusType.Unraveled, StatTarget.Defense, 25, 2, AmplitudeType.Flat);
            
            target.AddStatus(unraveled1);
            target.AddStatus(unraveled2);

            int startHP = target.currentHP;

            executor.Execute(attacker, damageSkill, new List<CombatCharacter> { target }, new System.Random());

            int damageTaken = startHP - target.currentHP;
            
            Assert.AreEqual(140, damageTaken);
        }

        [Test]
        public void Unraveled_DoesNotIncreaseDamageTaken_AfterExpiration()
        {
            var unraveled = StatusInstanceFactory.Create(
                new SkillContext(attacker, damageSkill, new List<CombatCharacter> { target }, bs, new System.Random()),
                StatusType.Unraveled, StatTarget.Defense, 30, 1, AmplitudeType.Flat);
            
            target.AddStatus(unraveled);
            
            // Tick duration so it expires
            StatusProcessor.TickDurations(target, 0);

            Assert.IsFalse(target.statusEffects.Contains(unraveled));

            int startHP = target.currentHP;

            executor.Execute(attacker, damageSkill, new List<CombatCharacter> { target }, new System.Random());

            int damageTaken = startHP - target.currentHP;
            
            Assert.AreEqual(100, damageTaken); // Base damage is 100
        }

        [Test]
        public void Unraveled_IncreasesDamageTaken_FromRiposte()
        {
            // Attacker has Unraveled
            var unraveled = StatusInstanceFactory.Create(
                new SkillContext(target, damageSkill, new List<CombatCharacter> { attacker }, bs, new System.Random()),
                StatusType.Unraveled, StatTarget.Defense, 20, 2, AmplitudeType.Flat);
            attacker.AddStatus(unraveled);

            // Target has Riposte (amplitude 50 = 50% damage percent)
            var riposte = StatusInstanceFactory.Create(
                new SkillContext(target, damageSkill, new List<CombatCharacter> { attacker }, bs, new System.Random()),
                StatusType.Riposte, StatTarget.Defense, 50, 2, AmplitudeType.Flat);
            target.AddStatus(riposte);

            // Target base attack is 100. Riposte modifier is 50%, so base Riposte damage is 50.
            // Attacker has Unraveled 20, so damage multiplier is +20% -> total Riposte damage: 50 * 1.2 = 60.

            int startHP = attacker.currentHP;

            // Attacker hits Target to trigger Riposte
            executor.Execute(attacker, damageSkill, new List<CombatCharacter> { target }, new System.Random(42));

            int damageTaken = startHP - attacker.currentHP;
            
            Assert.AreEqual(60, damageTaken);
        }

        [Test]
        public void Unraveled_DoesNotAffectNonDamagingSkills()
        {
            var unraveled = StatusInstanceFactory.Create(
                new SkillContext(attacker, damageSkill, new List<CombatCharacter> { target }, bs, new System.Random()),
                StatusType.Unraveled, StatTarget.Defense, 30, 2, AmplitudeType.Flat);
            
            target.AddStatus(unraveled);

            // Mock an effect that reads damageMultiplier to ensure it hasn't changed for non-damage skills
            float multiplierInContext = -1f;
            var testEffect = new DelegateSkillEffect((ctx, tgt) => {
                multiplierInContext = ctx.damageMultiplier;
            });
            nonDamageSkill.effects.Add(testEffect);

            executor.Execute(attacker, nonDamageSkill, new List<CombatCharacter> { target }, new System.Random());

            // Since skill.modifier.IsDamage is false, Unraveled should not increase multiplier
            Assert.AreEqual(1.0f, multiplierInContext);
        }

        // Helper effect for testing
        [System.Serializable]
        private class DelegateSkillEffect : ISkillEffect
        {
            private System.Action<SkillContext, CombatCharacter> _action;

            public DelegateSkillEffect(System.Action<SkillContext, CombatCharacter> action)
            {
                _action = action;
            }

            public void Execute(SkillContext context, CombatCharacter target)
            {
                _action?.Invoke(context, target);
            }
        }
    }
}
