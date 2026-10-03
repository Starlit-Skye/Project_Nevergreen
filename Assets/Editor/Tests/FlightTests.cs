using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class FlightTests
    {
        private BattleSystem _battleSystem;
        private CombatCharacter _attacker;
        private CombatCharacter _target;
        private SkillData _damageSkill;
        private SkillData _nonDamageSkill;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("BattleSystem");
            _battleSystem = go.AddComponent<BattleSystem>();

            // Setup characters
            var attackerData = ScriptableObject.CreateInstance<CharacterData>();
            attackerData.characterId = "attacker";
            var attackerGO = new GameObject("Attacker");
            _attacker = attackerGO.AddComponent<CombatCharacter>();
            _attacker.characterData = attackerData;
            _attacker.baseStats = new CombatStats { attack = 10, accuracy = 100, critChance = 0 };
            _attacker.team = Team.Player;

            var targetData = ScriptableObject.CreateInstance<CharacterData>();
            targetData.characterId = "target";
            var targetGO = new GameObject("Target");
            _target = targetGO.AddComponent<CombatCharacter>();
            _target.characterData = targetData;
            _target.baseStats = new CombatStats { dodge = 10, defense = 0, maxHP = 100 };
            _target.team = Team.Enemy;
            _target.TakeDamage(-100); // Reset HP to 100

            // Create Skills
            _damageSkill = ScriptableObject.CreateInstance<SkillData>();
            _damageSkill.skillId = "damage_attack";
            _damageSkill.modifier = new SkillModifier { damagePercent = 1.0f };
            _damageSkill.effects = new List<ISkillEffect> { new DamageEffect() };
            _damageSkill.targetScope = TargetScope.Enemies;

            _nonDamageSkill = ScriptableObject.CreateInstance<SkillData>();
            _nonDamageSkill.skillId = "debuff_attack";
            _nonDamageSkill.modifier = new SkillModifier { damagePercent = 0f };
            _nonDamageSkill.effects = new List<ISkillEffect>();
            _nonDamageSkill.targetScope = TargetScope.Enemies;
        }

        [TearDown]
        public void TearDown()
        {
            if (!UnityEditor.EditorUtility.IsPersistent(_damageSkill)) Object.DestroyImmediate(_damageSkill);
            if (!UnityEditor.EditorUtility.IsPersistent(_nonDamageSkill)) Object.DestroyImmediate(_nonDamageSkill);

            Object.DestroyImmediate(_attacker.gameObject);
            Object.DestroyImmediate(_target.gameObject);
            Object.DestroyImmediate(_battleSystem.gameObject);
        }

        [Test]
        public void Flight_GrantsFlatDodgeIncrease()
        {
            int initialDodge = _target.GetEffectiveStats().dodge;

            var flight = new FlightStatusInstance(_battleSystem, 20, 3);
            _target.AddStatus(flight);

            int effectiveDodge = _target.GetEffectiveStats().dodge;
            Assert.AreEqual(initialDodge + 20, effectiveDodge);
        }

        [Test]
        public void Flight_RemovedWhenHitByDamageSkill()
        {
            var flight = new FlightStatusInstance(_battleSystem, 20, 3);
            _target.AddStatus(flight);
            Assert.IsTrue(_target.statusEffects.Contains(flight));

            // Execute hit that deals damage
            var ctx = new SkillContext(_attacker, _damageSkill, new List<CombatCharacter> { _target }, _battleSystem, new System.Random());
            ctx.primaryTarget = _target;
            ctx.didHit = true;
            ctx.calculatedValue = 10;
            
            // Synthesize the event emission that normally happens in SkillExecutor
            var skillExecField = typeof(BattleSystem).GetField("OnActionResolved", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var eventDelegate = (System.Action<CombatCharacter, SkillData, SkillContext>)skillExecField.GetValue(_battleSystem);
            eventDelegate?.Invoke(_attacker, _damageSkill, ctx);

            Assert.IsFalse(_target.statusEffects.Contains(flight), "Flight should be removed after a damage hit.");
        }

        [Test]
        public void Flight_NotRemovedWhenAttackMisses()
        {
            var flight = new FlightStatusInstance(_battleSystem, 20, 3);
            _target.AddStatus(flight);

            var ctx = new SkillContext(_attacker, _damageSkill, new List<CombatCharacter> { _target }, _battleSystem, new System.Random());
            ctx.primaryTarget = _target;
            ctx.didHit = false; // Miss
            ctx.calculatedValue = 0;
            
            var skillExecField = typeof(BattleSystem).GetField("OnActionResolved", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var eventDelegate = (System.Action<CombatCharacter, SkillData, SkillContext>)skillExecField.GetValue(_battleSystem);
            eventDelegate?.Invoke(_attacker, _damageSkill, ctx);

            Assert.IsTrue(_target.statusEffects.Contains(flight), "Flight should NOT be removed if the attack misses.");
        }

        [Test]
        public void Flight_NotRemovedWhenHitByNonDamageSkill()
        {
            var flight = new FlightStatusInstance(_battleSystem, 20, 3);
            _target.AddStatus(flight);

            var ctx = new SkillContext(_attacker, _nonDamageSkill, new List<CombatCharacter> { _target }, _battleSystem, new System.Random());
            ctx.primaryTarget = _target;
            ctx.didHit = true; 
            ctx.calculatedValue = 0;
            
            var skillExecField = typeof(BattleSystem).GetField("OnActionResolved", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var eventDelegate = (System.Action<CombatCharacter, SkillData, SkillContext>)skillExecField.GetValue(_battleSystem);
            eventDelegate?.Invoke(_attacker, _nonDamageSkill, ctx);

            Assert.IsTrue(_target.statusEffects.Contains(flight), "Flight should NOT be removed by non-damaging skills.");
        }

        [Test]
        public void Flight_NotRemovedByDamageOverTime()
        {
            var flight = new FlightStatusInstance(_battleSystem, 20, 3);
            _target.AddStatus(flight);

            // Simulate DoT directly (bypasses OnActionResolved)
            _target.TakeDamage(5, false);

            Assert.IsTrue(_target.statusEffects.Contains(flight), "Flight should NOT be removed by DoT damage (no OnActionResolved event).");
        }
    }
}
