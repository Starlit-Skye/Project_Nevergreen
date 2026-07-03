using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class PeriodicEffectTests
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

        private CombatCharacter Track(string id, int maxHP = 100, int currentHP = -1)
        {
            var c = CombatTestHelper.CreateCombatCharacter(
                id, Team.Player, rank: 1, maxHP: maxHP, config: _config);
            if (currentHP >= 0)
            {
                c.currentHP = currentHP;
            }
            _cleanup.Add(c.gameObject);
            return c;
        }

        // --- Restore (HoT) Tests ---

        [Test]
        public void Restore_SingleStack_HealsCorrectAmount()
        {
            var c = Track("hero", maxHP: 100, currentHP: 50);
            c.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 15, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            Assert.AreEqual(65, c.currentHP, "Restore should heal by its amplitude (50 + 15 = 65).");
        }

        [Test]
        public void Restore_MultipleStacks_AggregatesHealing()
        {
            var c = Track("hero", maxHP: 100, currentHP: 50);
            c.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 10, 3));
            c.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 5, 2));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            Assert.AreEqual(65, c.currentHP, "Multiple Restore stacks should aggregate before healing (50 + 10 + 5 = 65).");
        }

        [Test]
        public void Restore_DoesNotExceedMaxHP()
        {
            var c = Track("hero", maxHP: 100, currentHP: 95);
            c.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 20, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            Assert.AreEqual(100, c.currentHP, "Restore healing should cap at character's Max HP.");
        }

        [Test]
        public void Restore_DoesNotHealDeadCharacter()
        {
            var c = Track("hero", maxHP: 100, currentHP: 0); // HP 0 = Dead
            c.state = LifeState.Dying; // Simulate death state
            c.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 20, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            Assert.AreEqual(0, c.currentHP, "Restore should not revive or heal a dead character.");
        }

        // --- Bleed / Blight (DoT) Tests ---

        [Test]
        public void Bleed_SingleStack_DealsCorrectDamage()
        {
            var c = Track("hero", maxHP: 100, currentHP: 100);
            c.AddStatus(new StatusEffectInstance(StatusType.Bleed, StatTarget.MaxHP, 12, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            Assert.AreEqual(88, c.currentHP, "Bleed should deal damage equal to its amplitude (100 - 12 = 88).");
        }

        [Test]
        public void Blight_MultipleStacks_AggregatesDamage()
        {
            var c = Track("hero", maxHP: 100, currentHP: 100);
            c.AddStatus(new StatusEffectInstance(StatusType.Blight, StatTarget.MaxHP, 10, 3));
            c.AddStatus(new StatusEffectInstance(StatusType.Blight, StatTarget.MaxHP, 8, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            Assert.AreEqual(82, c.currentHP, "Multiple Blight stacks should aggregate damage (100 - 10 - 8 = 82).");
        }

        // --- Burn Tests ---

        [Test]
        public void Burn_SingleStack_IncrementsAmplitudeAndDealsDamage()
        {
            var c = Track("hero", maxHP: 100, currentHP: 100);
            c.AddStatus(new StatusEffectInstance(StatusType.Burn, StatTarget.MaxHP, 2, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            // Starts at 2, increments to 3 before triggering, takes 3 damage
            Assert.AreEqual(97, c.currentHP, "Burn should increment its amplitude to 3 and deal 3 damage (100 - 3 = 97).");
        }

        [Test]
        public void Burn_MultipleStacks_IncrementsIndependently()
        {
            var c = Track("hero", maxHP: 100, currentHP: 100);
            c.AddStatus(new StatusEffectInstance(StatusType.Burn, StatTarget.MaxHP, 2, 3));
            c.AddStatus(new StatusEffectInstance(StatusType.Burn, StatTarget.MaxHP, 3, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            // First stack increments to 3, second increments to 4. Total = 7.
            Assert.AreEqual(93, c.currentHP, "Multiple Burn stacks should increment independently and aggregate damage (100 - 7 = 93).");
        }

        [Test]
        public void PeriodicDamage_DoesNotDropHPBelowZero()
        {
            var c = Track("hero", maxHP: 100, currentHP: 5);
            c.AddStatus(new StatusEffectInstance(StatusType.Bleed, StatTarget.MaxHP, 10, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            Assert.AreEqual(0, c.currentHP, "DoT damage should not drop HP below zero.");
            Assert.IsFalse(c.IsAlive, "Character should be dead.");
        }

        [Test]
        public void MultiplePeriodicTypes_ResolveIndependently()
        {
            var c = Track("hero", maxHP: 100, currentHP: 50);
            c.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 20, 3));
            c.AddStatus(new StatusEffectInstance(StatusType.Bleed, StatTarget.MaxHP, 10, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c);
            
            // Expected flow based on grouping: 
            // Bleed/Restore order depends on Enum/Grouping, but since both resolve to currentHP,
            // the net change should be +10 (50 + 20 - 10 = 60).
            Assert.AreEqual(60, c.currentHP, "Restore (+20) and Bleed (-10) should result in net +10 HP.");
        }

        [Test]
        public void PeriodicEffects_RespectApplicationOrder_FirstAppliedIsResolvedFirst()
        {
            // Scenario 1: Restore then Bleed. 
            // 5 HP + 10 Heal (lives) -> 15 HP - 10 Damage -> 5 HP (End).
            var c1 = Track("hero1", maxHP: 100, currentHP: 5);
            c1.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 10, 3));
            c1.AddStatus(new StatusEffectInstance(StatusType.Bleed, StatTarget.MaxHP, 10, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c1);
            Assert.AreEqual(5, c1.currentHP, "Restore applied first should allow character to survive the Bleed.");
            Assert.IsTrue(c1.IsAlive);

            // Scenario 2: Bleed then Restore.
            // 5 HP - 10 Damage (dies) -> 0 HP. Restore should not fire on dead character.
            var c2 = Track("hero2", maxHP: 100, currentHP: 5);
            c2.AddStatus(new StatusEffectInstance(StatusType.Bleed, StatTarget.MaxHP, 10, 3));
            c2.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 10, 3));
            
            StatusProcessor.ProcessPeriodicEffects(c2);
            Assert.AreEqual(0, c2.currentHP, "Bleed applied first should kill character before Restore triggers.");
            Assert.IsFalse(c2.IsAlive);
        }

        // --- Expiry Tests ---

        [Test]
        public void PeriodicEffect_DurationTicksDown_AndExpires()
        {
            var c = Track("hero", maxHP: 100, currentHP: 50);
            c.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 10, 1));
            
            Assert.AreEqual(1, c.statusEffects.Count, "Effect added.");
            
            StatusProcessor.TickDurations(c, _config.stunRecoveryResistBonus);
            
            Assert.AreEqual(0, c.statusEffects.Count, "Effect should expire after 1 tick if initial duration was 1.");
            
            // Process to ensure it doesn't heal after expiry
            StatusProcessor.ProcessPeriodicEffects(c);
            Assert.AreEqual(50, c.currentHP, "Expired effect should not heal the character.");
        }
    }
}
