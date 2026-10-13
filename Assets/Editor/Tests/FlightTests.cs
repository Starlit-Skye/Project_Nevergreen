using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class FlightTests
    {
        private CombatConfig config;
        private CombatCharacter attacker;
        private CombatCharacter target;
        private BattleSystem bs;
        private SkillData damageSkill;
        private SkillData nonDamageSkill;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();
            config = CombatTestHelper.CreateDefaultConfig();
            
            attacker = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Enemy, 1, attack: 20, defense: 0, accuracy: 100, maxHP: 100);
            target = CombatTestHelper.CreateCombatCharacter("Target", Team.Player, 1, attack: 30, defense: 0, accuracy: 100, dodge: 10, maxHP: 100);

            var bsGo = new GameObject("BS");
            bs = bsGo.AddComponent<BattleSystem>();
            
            bs.StartBattle(new List<CombatCharacter> { target }, new List<CombatCharacter> { attacker });

            var rng = new System.Random(42);
            typeof(BattleSystem).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(bs, rng);

            // Create Skills
            damageSkill = CombatTestHelper.CreateDamageSkill(1.0f);
            damageSkill.skillId = "damage_attack";
            damageSkill.guaranteedHit = true; // Ensure it hits unless dodge forced
            damageSkill.effects.Add(new DamageEffect());

            nonDamageSkill = ScriptableObject.CreateInstance<SkillData>();
            nonDamageSkill.skillId = "debuff_attack";
            nonDamageSkill.modifier = new SkillModifier { damagePercent = 0f };
            nonDamageSkill.guaranteedHit = true;
            nonDamageSkill.effects = new List<ISkillEffect>();
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
            if (config != null) ScriptableObject.DestroyImmediate(config, true);
        }

        private void CallExecuteSkill(BattleSystem battleSystem, CombatCharacter user, SkillData skill, List<CombatCharacter> targets)
        {
            battleSystem.ExecuteSkill(user, skill, targets);
        }

        [Test]
        public void TakeFlight_SelfStatusEffect_CreatesFlightInstance()
        {
            var flightSkill = ScriptableObject.CreateInstance<SkillData>();
            flightSkill.skillId = "take_flight";
            flightSkill.effects.Add(new SelfStatusEffect { statusType = StatusType.Flight, amplitude = 20, duration = 3 });
            
            CallExecuteSkill(bs, target, flightSkill, new List<CombatCharacter> { target });

            var instance = target.statusEffects.FirstOrDefault(s => s.type == StatusType.Flight);
            Assert.IsNotNull(instance, "Flight status should be applied");
            Assert.IsInstanceOf<FlightStatusInstance>(instance, "SelfStatusEffect should create FlightStatusInstance");
            
            Object.DestroyImmediate(flightSkill);
        }

        [Test]
        public void Flight_AppliedViaStatusEffect_AlsoRemovable()
        {
            var flightSkill = ScriptableObject.CreateInstance<SkillData>();
            flightSkill.skillId = "take_flight_enemy";
            flightSkill.effects.Add(new StatusEffect { statusType = StatusType.Flight, amplitude = 20, duration = 3, applicationChance = 100f });
            
            CallExecuteSkill(bs, attacker, flightSkill, new List<CombatCharacter> { target });

            var instance = target.statusEffects.FirstOrDefault(s => s.type == StatusType.Flight);
            Assert.IsNotNull(instance);
            Assert.IsInstanceOf<FlightStatusInstance>(instance);

            CallExecuteSkill(bs, attacker, damageSkill, new List<CombatCharacter> { target });

            Assert.IsFalse(target.statusEffects.Any(s => s.type == StatusType.Flight), "Flight should be removed after hit.");
            
            Object.DestroyImmediate(flightSkill);
        }

        [Test]
        public void Flight_GrantsFlatDodgeIncrease()
        {
            int initialDodge = target.GetEffectiveStats().dodge;

            var flightSkill = ScriptableObject.CreateInstance<SkillData>();
            flightSkill.skillId = "take_flight";
            flightSkill.effects.Add(new SelfStatusEffect { statusType = StatusType.Flight, amplitude = 20, duration = 3 });
            
            CallExecuteSkill(bs, target, flightSkill, new List<CombatCharacter> { target });

            int effectiveDodge = target.GetEffectiveStats().dodge;
            Assert.AreEqual(initialDodge + 20, effectiveDodge);
            
            Object.DestroyImmediate(flightSkill);
        }

        [Test]
        public void Flight_RemovedWhenHitByDamageSkill()
        {
            var flightSkill = ScriptableObject.CreateInstance<SkillData>();
            flightSkill.skillId = "take_flight";
            flightSkill.effects.Add(new SelfStatusEffect { statusType = StatusType.Flight, amplitude = 20, duration = 3 });
            
            CallExecuteSkill(bs, target, flightSkill, new List<CombatCharacter> { target });
            Assert.IsTrue(target.statusEffects.Any(s => s.type == StatusType.Flight));

            CallExecuteSkill(bs, attacker, damageSkill, new List<CombatCharacter> { target });

            Assert.IsFalse(target.statusEffects.Any(s => s.type == StatusType.Flight), "Flight should be removed after a damage hit.");
            Object.DestroyImmediate(flightSkill);
        }

        [Test]
        public void Flight_NotRemovedWhenAttackMisses()
        {
            var flightSkill = ScriptableObject.CreateInstance<SkillData>();
            flightSkill.skillId = "take_flight";
            flightSkill.effects.Add(new SelfStatusEffect { statusType = StatusType.Flight, amplitude = 20, duration = 3 });
            
            CallExecuteSkill(bs, target, flightSkill, new List<CombatCharacter> { target });

            var missingSkill = ScriptableObject.CreateInstance<SkillData>();
            missingSkill.skillId = "miss_attack";
            missingSkill.modifier = new SkillModifier { damagePercent = 1.0f, accuracyMod = -999 }; // force miss
            missingSkill.effects.Add(new DamageEffect());
            missingSkill.guaranteedHit = false;

            CallExecuteSkill(bs, attacker, missingSkill, new List<CombatCharacter> { target });

            Assert.IsTrue(target.statusEffects.Any(s => s.type == StatusType.Flight), "Flight should NOT be removed if the attack misses.");
            
            Object.DestroyImmediate(flightSkill);
            Object.DestroyImmediate(missingSkill);
        }

        [Test]
        public void Flight_NotRemovedWhenHitByNonDamageSkill()
        {
            var flightSkill = ScriptableObject.CreateInstance<SkillData>();
            flightSkill.skillId = "take_flight";
            flightSkill.effects.Add(new SelfStatusEffect { statusType = StatusType.Flight, amplitude = 20, duration = 3 });
            
            CallExecuteSkill(bs, target, flightSkill, new List<CombatCharacter> { target });

            CallExecuteSkill(bs, attacker, nonDamageSkill, new List<CombatCharacter> { target });

            Assert.IsTrue(target.statusEffects.Any(s => s.type == StatusType.Flight), "Flight should NOT be removed by non-damaging skills.");
            
            Object.DestroyImmediate(flightSkill);
        }

        [Test]
        public void Flight_NotRemovedByDamageOverTime()
        {
            var flightSkill = ScriptableObject.CreateInstance<SkillData>();
            flightSkill.skillId = "take_flight";
            flightSkill.effects.Add(new SelfStatusEffect { statusType = StatusType.Flight, amplitude = 20, duration = 3 });
            
            CallExecuteSkill(bs, target, flightSkill, new List<CombatCharacter> { target });

            // Simulate DoT directly
            target.TakeDamage(5, false);

            Assert.IsTrue(target.statusEffects.Any(s => s.type == StatusType.Flight), "Flight should NOT be removed by DoT damage (no OnActionResolved event).");
            
            Object.DestroyImmediate(flightSkill);
        }

        [Test]
        public void Flight_RemovedByRiposteDamageHit()
        {
            var flightSkill = ScriptableObject.CreateInstance<SkillData>();
            flightSkill.skillId = "take_flight";
            flightSkill.effects.Add(new SelfStatusEffect { statusType = StatusType.Flight, amplitude = 20, duration = 3 });
            
            // Give target Flight
            CallExecuteSkill(bs, target, flightSkill, new List<CombatCharacter> { target });

            // Give attacker Riposte
            attacker.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));

            // Target attacks the attacker who has Riposte
            var standardAttack = CombatTestHelper.CreateDamageSkill(1.0f);
            standardAttack.skillId = "standard_attack";
            standardAttack.guaranteedHit = true;
            standardAttack.effects.Add(new DamageEffect());
            standardAttack.targetScope = TargetScope.Enemies;
            
            CallExecuteSkill(bs, target, standardAttack, new List<CombatCharacter> { attacker });

            // The Riposte counter should hit the target and remove Flight
            Assert.IsFalse(target.statusEffects.Any(s => s.type == StatusType.Flight), "Flight should be removed after being hit by a Riposte counter.");
            
            Object.DestroyImmediate(flightSkill);
            Object.DestroyImmediate(standardAttack);
        }
    }
}
