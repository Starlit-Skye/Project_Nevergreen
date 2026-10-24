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
            CombatTestHelper.InitializeTestDatabase();
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();
        }

        [TearDown]
        public void TearDown()
        {
            CombatTestHelper.CleanupTestDatabase();
            if (_config != null) ScriptableObject.DestroyImmediate(_config, true);
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
            Assert.AreEqual(310, effective.stunResist,
                "Post-stun recovery should apply a flat +300 to stun resistance (10 + 300 = 310).");
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

        [Test]
        public void Stun_AppliedToAlreadyStunned_DirectAddStatus_ReturnsFalseAndCreatesNoInstance()
        {
            var c = Track("hero");
            bool firstAdded = c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));
            Assert.IsTrue(firstAdded, "First stun should be added.");
            Assert.AreEqual(1, c.statusEffects.Count(s => s.type == StatusType.Stun), "Should have exactly 1 stun.");

            bool secondAdded = c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 2));
            Assert.IsFalse(secondAdded, "Second stun on already-stunned target should return false.");
            Assert.AreEqual(1, c.statusEffects.Count(s => s.type == StatusType.Stun), "Stun count should remain 1.");
            Assert.AreEqual(1, c.statusEffects.First(s => s.type == StatusType.Stun).remainingDuration, 
                "Existing stun duration should not be changed.");
        }

        [Test]
        public void Stun_AppliedToAlreadyStunned_ViaSkillEffect_IsCompletelyIgnored()
        {
            var user = Track("user");
            var target = Track("target");
            target.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));

            bool eventFired = false;
            target.OnStatusApplied += (character, type, success, stat) =>
            {
                if (type == StatusType.Stun) eventFired = true;
            };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "test_stun_skill";

            try
            {
                var rng = CombatTestHelper.CreateFixedRng(42);
                var freshRng = CombatTestHelper.CreateFixedRng(42);
                double expectedFirstRoll = freshRng.NextDouble();

                var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, null, rng);
                ctx.didHit = true;
                ctx.hasResolvedHit = true;

                var effect = new StatusEffect
                {
                    statusType = StatusType.Stun,
                    applicationChance = 100f,
                    duration = 3,
                    ignoreMiss = true
                };

                effect.Execute(ctx, target);

                Assert.AreEqual(1, target.statusEffects.Count(s => s.type == StatusType.Stun), 
                    "No additional stun instance should be added.");
                Assert.AreEqual(1, target.statusEffects.First(s => s.type == StatusType.Stun).remainingDuration, 
                    "Existing stun duration must remain untouched.");
                Assert.IsFalse(eventFired, "OnStatusApplied should NOT be triggered for ignored stun.");
                Assert.AreEqual(expectedFirstRoll, rng.NextDouble(), 
                    "RNG roll must NOT be consumed when stun is ignored.");
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void Stun_AppliedToAlreadyStunned_DoesNotExtendDuration()
        {
            var c = Track("hero");
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));
            
            // Attempt to apply a 3-turn stun
            bool added = c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 3));
            Assert.IsFalse(added, "Duplicate stun should be rejected.");

            // Turn 1 skip
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);

            // Stun should now have expired because the 1-turn stun was not extended
            Assert.IsFalse(c.isStunned, "Stun should expire after 1 tick since incoming 3-turn stun was ignored.");
        }

        [Test]
        public void Stun_AppliedToAlreadyStunned_YieldsSingleRecoveryBuffOnExpiry()
        {
            var c = Track("hero");
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1)); // ignored

            // Expire the stun
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);

            var recoveryBuffs = c.statusEffects.Where(s => s.type == StatusType.Buff && s.targetStat == StatTarget.StunResist).ToList();
            Assert.AreEqual(1, recoveryBuffs.Count, "Exactly 1 recovery buff should be granted, not 2.");
            Assert.AreEqual(300, recoveryBuffs[0].amplitude, "Recovery buff amplitude should be +300%.");
        }

        [Test]
        public void Stun_AppliedAfterPreviousExpired_AppliesNormally()
        {
            var c = Track("hero");
            c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 1));

            // Expire the stun
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            Assert.IsFalse(c.isStunned, "Character should no longer be stunned.");

            // Apply a new stun after previous expired
            bool added = c.AddStatus(new StatusEffectInstance(StatusType.Stun, 1, 2));
            Assert.IsTrue(added, "New stun applied after previous expiration should succeed.");
            Assert.IsTrue(c.isStunned, "Character should now be stunned again.");
            Assert.AreEqual(2, c.statusEffects.First(s => s.type == StatusType.Stun).remainingDuration, 
                "New stun duration should be 2.");
        }

        [Test]
        public void NonStunStatus_LikeBleed_StillStacks()
        {
            var c = Track("hero");
            bool bleed1 = c.AddStatus(new StatusEffectInstance(StatusType.Bleed, 2, 3));
            bool bleed2 = c.AddStatus(new StatusEffectInstance(StatusType.Bleed, 4, 3));

            Assert.IsTrue(bleed1, "First bleed should be added.");
            Assert.IsTrue(bleed2, "Second bleed should also be added (bleed stacks).");
            Assert.AreEqual(2, c.statusEffects.Count(s => s.type == StatusType.Bleed), 
                "Character should have 2 distinct Bleed instances.");
        }
    }
}
