using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class BuffDebuffTests
    {
        private CombatCharacter _character;
        private CombatConfig _config;
        private List<GameObject> _cleanup;

        [SetUp]
        public void SetUp()
        {
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();
            _character = CombatTestHelper.CreateCombatCharacter(
                "test_hero", Team.Player, rank: 1,
                attack: 100, defense: 20, speed: 10, maxHP: 200, config: _config);
            _cleanup.Add(_character.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup)
                if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void Buff_Attack_IncreasesStatByPercentageOfBase()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(110, effective.attack, "10% buff on base 100 => 110.");
        }

        [Test]
        public void Debuff_Attack_DecreasesStatByPercentageOfBase()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Attack, 20, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(80, effective.attack, "20% debuff on base 100 => 80.");
        }

        [Test]
        public void Buff_Defense_IncreasesStatByPercentageOfBase()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Defense, 50, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(30, effective.defense, "50% buff on base 20 => 30.");
        }

        [Test]
        public void Buff_Speed_IncreasesStatByPercentageOfBase()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Speed, 20, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(12, effective.speed, "20% buff on base 10 => 12.");
        }

        [Test]
        public void MultipleBuffs_SameStat_StackAdditively()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 3));
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 20, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(130, effective.attack, "+10%+20% additive => 130, not 132 compound.");
        }

        [Test]
        public void MultipleDebuffs_SameStat_StackAdditively()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Defense, 10, 3));
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Defense, 15, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(15, effective.defense, "-10%-15% additive on base 20 => 15.");
        }

        [Test]
        public void BuffAndDebuff_SameStat_NetToCorrectPercentage()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 30, 3));
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Attack, 10, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(120, effective.attack, "+30%-10% net +20% on base 100 => 120.");
        }

        [Test]
        public void MultipleBuffs_DifferentStats_ApplyIndependently()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 3));
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Defense, 50, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(110, effective.attack, "Attack: base 100 * 1.10 = 110.");
            Assert.AreEqual(30, effective.defense, "Defense: base 20 * 1.50 = 30.");
        }

        [Test]
        public void DebuffResistance_ReducesApplicationChance()
        {
            var rng = CombatTestHelper.CreateFixedRng(42);
            int applied = 0;
            for (int i = 0; i < 1000; i++)
                if (CombatCalculator.ResolveStatusApplication(100f, 40, rng)) applied++;
            float rate = (float)applied / 1000;
            Assert.That(rate, Is.InRange(0.50f, 0.70f), $"60% effective => ~60%. Got {rate:P1}.");
        }

        [Test]
        public void DebuffResistance_EqualToChance_BlocksAll()
        {
            var rng = CombatTestHelper.CreateFixedRng(42);
            int applied = 0;
            for (int i = 0; i < 100; i++)
                if (CombatCalculator.ResolveStatusApplication(80f, 80, rng)) applied++;
            Assert.AreEqual(0, applied, "0% effective chance => no applications.");
        }

        [Test]
        public void DebuffResistance_ExceedsChance_BlocksAll()
        {
            var rng = CombatTestHelper.CreateFixedRng(42);
            Assert.IsFalse(CombatCalculator.ResolveStatusApplication(50f, 75, rng),
                "Negative effective chance => never applies.");
        }

        [Test]
        public void BuffDuration_ExpiresAfterCorrectTicks()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 2));
            StatusProcessor.TickDurations(_character, _config.stunRecoveryResistBonus);
            Assert.AreEqual(1, _character.statusEffects.Count, "Still active after 1 tick.");
            StatusProcessor.TickDurations(_character, _config.stunRecoveryResistBonus);
            Assert.AreEqual(0, _character.statusEffects.Count, "Removed after 2 ticks.");
            Assert.AreEqual(_character.baseStats.attack, _character.GetEffectiveStats().attack,
                "Stats return to base after expiry.");
        }

        [Test]
        public void DebuffDuration_ExpiresAfterOneTick()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Speed, 20, 1));
            StatusProcessor.TickDurations(_character, _config.stunRecoveryResistBonus);
            Assert.AreEqual(0, _character.statusEffects.Count, "1-turn debuff removed after 1 tick.");
        }

        [Test]
        public void ExpiredBuffs_DoNotAffectStats()
        {
            _character.statusEffects.Add(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 50, 0));
            Assert.AreEqual(_character.baseStats.attack, _character.GetEffectiveStats().attack,
                "Expired buff (duration 0) should not modify stats.");
        }
    }
}
