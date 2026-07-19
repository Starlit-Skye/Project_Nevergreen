using System.Collections.Generic;
using Nevergreen.Combat;
using Nevergreen.Data;
using NUnit.Framework;
using UnityEngine;

namespace Nevergreen.Tests
{
    public class TrinketTests
    {
        private PartyMemberInfo _partyMember;
        private CombatCharacter _character;
        private BattleSystem _battleSystem;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("TestCharacter");
            _character = go.AddComponent<CombatCharacter>();
            
            var battleGo = new GameObject("BattleSystem");
            _battleSystem = battleGo.AddComponent<BattleSystem>();

            var charData = ScriptableObject.CreateInstance<CharacterData>();
            var stats = ScriptableObject.CreateInstance<StatBlockData>();
            stats.maxHP = 100;
            stats.attack = 10;
            charData.statPerLevel.Add(stats);
            _character.characterData = charData;

            _partyMember = new PartyMemberInfo
            {
                character = charData,
                currentLevel = 1,
                currentHP = 100
            };
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_character.gameObject);
            Object.DestroyImmediate(_battleSystem.gameObject);
        }

        [Test]
        public void TryEquipTrinket_EnforcesMaxCapacityOfTwo()
        {
            var t1 = ScriptableObject.CreateInstance<TrinketData>(); t1.trinketId = "t1";
            var t2 = ScriptableObject.CreateInstance<TrinketData>(); t2.trinketId = "t2";
            var t3 = ScriptableObject.CreateInstance<TrinketData>(); t3.trinketId = "t3";

            Assert.IsTrue(_partyMember.TryEquipTrinket(t1));
            Assert.IsTrue(_partyMember.TryEquipTrinket(t2));
            Assert.IsFalse(_partyMember.TryEquipTrinket(t3)); // Capacity reached

            Assert.AreEqual(2, _partyMember.equippedTrinkets.Count);
        }

        [Test]
        public void TryEquipTrinket_EnforcesUniqueness()
        {
            var t1 = ScriptableObject.CreateInstance<TrinketData>(); t1.trinketId = "t1";
            var t2 = ScriptableObject.CreateInstance<TrinketData>(); t2.trinketId = "t1"; // duplicate ID

            Assert.IsTrue(_partyMember.TryEquipTrinket(t1));
            Assert.IsFalse(_partyMember.TryEquipTrinket(t2)); // Already equipped

            Assert.AreEqual(1, _partyMember.equippedTrinkets.Count);
        }

        [Test]
        public void TryUnequipTrinket_PreventsRemovalIfCursed()
        {
            var cursedTrinket = ScriptableObject.CreateInstance<TrinketData>();
            cursedTrinket.trinketId = "cursed";
            cursedTrinket.cannotBeRemoved = true;

            var normalTrinket = ScriptableObject.CreateInstance<TrinketData>();
            normalTrinket.trinketId = "normal";

            _partyMember.TryEquipTrinket(cursedTrinket);
            _partyMember.TryEquipTrinket(normalTrinket);

            Assert.IsFalse(_partyMember.TryUnequipTrinket(cursedTrinket));
            Assert.IsTrue(_partyMember.TryUnequipTrinket(normalTrinket));
        }

        [Test]
        public void StatModifierTrinket_AppliesCorrectStatsInCombat()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            trinket.trinketId = "stat_up";
            
            var statStrat = new StatModifierTrinketStrategy
            {
                statTarget = StatTarget.Attack,
                flatBonus = 5,
                percentBonus = 50f // +50%
            };
            trinket.effectStrategies.Add(statStrat);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);

            // Base attack is 10.
            // Expected: (10 + 5) * 1.5 = 22.5 => 22 (banker's rounding)
            var stats = _character.GetEffectiveStats();
            Assert.AreEqual(22, stats.attack);
        }

        [Test]
        public void GuaranteedHitTrinket_MutatesSkillContextOnDamageCalc()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            trinket.effectStrategies.Add(new GuaranteedHitTrinketStrategy());

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem); // Hooks events

            var skill = ScriptableObject.CreateInstance<SkillData>();
            var targets = new List<CombatCharacter> { _character }; // Target self for test
            var rng = new System.Random();
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, rng);

            // Simulate the event invoke manually because we are isolating it
            ctx.guaranteedHit = false; // base
            
            // Replicate BattleSystem emission
            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx);

            Assert.IsTrue(ctx.guaranteedHit);
        }
    }
}
