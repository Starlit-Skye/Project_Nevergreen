using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class StunTests
    {
        private CombatConfig _config;
        private List<GameObject> _cleanup;

        [SetUp]
        public void SetUp()
        {
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup)
                if (go != null) Object.DestroyImmediate(go);
        }

        private CombatCharacter Track(string id, int stunResist = 0, int maxHP = 200)
        {
            var cc = CombatTestHelper.CreateCombatCharacter(id, Team.Player, 1,
                maxHP: maxHP, stunResist: stunResist, config: _config);
            _cleanup.Add(cc.gameObject);
            return cc;
        }

        [Test]
        public void StunnedCharacter_IsMarkedAsStunned()
        {
            var c = Track("hero");
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 2));
            Assert.IsTrue(c.isStunned);
        }

        [Test]
        public void StunnedCharacter_RemainsStunned_UntilExpiry()
        {
            var c = Track("hero");
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 2));
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            Assert.IsTrue(c.isStunned, "Still stunned after 1 tick (1 remaining).");
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            Assert.IsFalse(c.isStunned, "Expired after 2 ticks.");
        }

        [Test]
        public void PostStunRecovery_Applies300PercentStunResistBuff()
        {
            var c = Track("hero", stunResist: 0);
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            Assert.IsFalse(c.isStunned);
            var buff = c.statusEffects.Find(
                s => s.type == StatusType.Buff && s.targetStat == StatTarget.StunResist);
            Assert.IsNotNull(buff, "Recovery buff should exist.");
            Assert.AreEqual(300, buff.amplitude, "Amplitude = +300%.");
            Assert.AreEqual(1, buff.remainingDuration, "Duration = 1 turn.");
        }

        [Test]
        public void PostStunRecovery_BuffExpiresAfterOneTick()
        {
            var c = Track("hero");
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            Assert.IsFalse(c.statusEffects.Any(
                s => s.type == StatusType.Buff && s.targetStat == StatTarget.StunResist),
                "Recovery buff should expire after 1 tick.");
        }

        [Test]
        public void PostStunRecovery_IncreasesEffectiveStunResist()
        {
            var c = Track("hero", stunResist: 10);
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            CombatStats effective = c.GetEffectiveStats();
            Assert.Greater(effective.stunResist, c.baseStats.stunResist,
                "Post-stun recovery should increase stun resistance above base.");
        }

        [Test]
        public void StunTiming_1TurnStun_SkipsThenExpires()
        {
            var c = Track("hero");
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));
            Assert.IsTrue(c.isStunned, "Stunned at start of turn.");
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            Assert.IsFalse(c.isStunned, "Expires after tick post-skip.");
        }

        [Test]
        public void StunTiming_2TurnStun_SkipsTwoTurns()
        {
            var c = Track("hero");
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 2));
            Assert.IsTrue(c.isStunned);
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            Assert.IsTrue(c.isStunned, "Still stunned after turn 1.");
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            Assert.IsFalse(c.isStunned, "Expired after turn 2.");
        }

        [Test]
        public void StunResistance_ReducesApplicationChance()
        {
            var rng = CombatTestHelper.CreateFixedRng(42);
            int applied = 0;
            for (int i = 0; i < 1000; i++)
                if (CombatCalculator.ResolveStatusApplication(100f, 60, rng)) applied++;
            float rate = (float)applied / 1000;
            Assert.That(rate, Is.InRange(0.30f, 0.50f), $"40% effective => ~40%. Got {rate:P1}.");
        }

        [Test]
        public void StunResistance_100Percent_BlocksStun()
        {
            var rng = CombatTestHelper.CreateFixedRng(42);
            Assert.IsFalse(CombatCalculator.ResolveStatusApplication(80f, 100, rng),
                "100% resist blocks 80% stun.");
        }

        [Test]
        public void StunnedCharacter_OtherStatusEffects_TickDownCorrectlyDuringSkip()
        {
            var c = Track("hero");
            // Arrange: A 2-turn buff and a 1-turn stun
            c.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 2));
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));

            Assert.IsTrue(c.isStunned, "Should be stunned initially.");
            Assert.AreEqual(2, c.statusEffects.Count, "Should have 2 status effects.");

            // Act: Simulate turn skip tick
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);

            // Assert:
            // 1. Stun should be gone (expiry)
            Assert.IsFalse(c.isStunned, "Stun should have expired.");
            
            // 2. Buff should have 1 turn remaining
            var buff = c.statusEffects.Find(s => s.type == StatusType.Buff && s.targetStat == StatTarget.Attack);
            Assert.IsNotNull(buff, "Buff should still be present.");
            Assert.AreEqual(1, buff.remainingDuration, "Buff duration should have ticked down even though turn was 'skipped'.");

            // 3. One more tick should expire the buff
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            Assert.IsFalse(c.statusEffects.Any(s => s.type == StatusType.Buff && s.targetStat == StatTarget.Attack), 
                "Buff should have expired on second tick.");
        }
    }
}
