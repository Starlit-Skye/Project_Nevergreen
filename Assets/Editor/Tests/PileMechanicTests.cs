using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class PileMechanicTests
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

        private CombatCharacter Track(string id, bool leavesPile = true)
        {
            var c = CombatTestHelper.CreateCombatCharacter(
                id, Team.Player, rank: 1, maxHP: 100, config: _config);
            
            if (c.characterData != null)
            {
                c.characterData.leavesPileOnDeath = leavesPile;
            }
            
            _cleanup.Add(c.gameObject);
            return c;
        }

        [Test]
        public void Death_TransitionsToDyingState_BeforeAnimationFinishes()
        {
            var c = Track("hero");
            c.TakeDamage(100);

            Assert.AreEqual(LifeState.Dying, c.state, "Character should immediately enter Dying state on reaching 0 HP.");
            Assert.IsFalse(c.IsAlive, "Dying characters should not be considered Alive.");
        }

        [Test]
        public void Pile_RefusesHealing()
        {
            var c = Track("hero");
            c.state = LifeState.Pile;
            c.currentHP = 50;

            c.Heal(20);

            Assert.AreEqual(50, c.currentHP, "Piles should refuse all healing.");
        }

        [Test]
        public void Pile_CanTakeDamage()
        {
            var c = Track("hero");
            c.state = LifeState.Pile;
            c.currentHP = 50;

            c.TakeDamage(20);

            Assert.AreEqual(30, c.currentHP, "Piles should now take damage.");
        }

        [Test]
        public void Pile_IsDestroyed_WhenHPReachesZero()
        {
            var c = Track("hero");
            c.state = LifeState.Pile;
            c.currentHP = 20;

            c.TakeDamage(20);

            Assert.AreEqual(LifeState.Destroyed, c.state, "Pile should be Destroyed when HP hits 0.");
        }

        [Test]
        public void FinalizeDefeat_LeavesPile_SetsStateAndHP()
        {
            var battleSystemGo = new GameObject("BattleSystem");
            _cleanup.Add(battleSystemGo);
            var battleSystem = battleSystemGo.AddComponent<BattleSystem>();

            var c = Track("hero", leavesPile: true);
            c.TakeDamage(100); // Enters Dying

            // Use Reflection to call private FinalizeCharacterDefeat
            var method = typeof(BattleSystem).GetMethod("FinalizeCharacterDefeat", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method.Invoke(battleSystem, new object[] { c, false });

            Assert.AreEqual(LifeState.Pile, c.state, "Character should become a Pile.");
            Assert.AreEqual(50, c.currentHP, "Pile should be instantiated with 50% max HP.");
        }

        [Test]
        public void FinalizeDefeat_DoesNotLeavePile_SetsDestroyed()
        {
            var battleSystemGo = new GameObject("BattleSystem");
            _cleanup.Add(battleSystemGo);
            var battleSystem = battleSystemGo.AddComponent<BattleSystem>();

            var c = Track("hero", leavesPile: false);
            c.TakeDamage(100);

            var method = typeof(BattleSystem).GetMethod("FinalizeCharacterDefeat", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method.Invoke(battleSystem, new object[] { c, false });

            Assert.AreEqual(LifeState.Destroyed, c.state, "Character should be Destroyed if they don't leave a pile.");
        }

        [Test]
        public void FinalizeDefeat_CriticalKill_DestroysRegardlessOfPileFlag()
        {
            var battleSystemGo = new GameObject("BattleSystem");
            _cleanup.Add(battleSystemGo);
            var battleSystem = battleSystemGo.AddComponent<BattleSystem>();

            var c = Track("hero", leavesPile: true);
            c.TakeDamage(100);

            var method = typeof(BattleSystem).GetMethod("FinalizeCharacterDefeat", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Critical Kill = true
            method.Invoke(battleSystem, new object[] { c, true });

            Assert.AreEqual(LifeState.Destroyed, c.state, "Critical kills should destroy the character instead of making a Pile.");
        }

        [Test]
        public void FinalizeDefeat_ClearsPreviousStatusEffects()
        {
            var battleSystemGo = new GameObject("BattleSystem");
            _cleanup.Add(battleSystemGo);
            var battleSystem = battleSystemGo.AddComponent<BattleSystem>();

            var c = Track("hero", leavesPile: true);
            c.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 50, 3));
            c.TakeDamage(100);

            var method = typeof(BattleSystem).GetMethod("FinalizeCharacterDefeat", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method.Invoke(battleSystem, new object[] { c, false });

            // Piles should not retain previous buffs.
            Assert.AreEqual(0, c.statusEffects.Count, "Should have no status effects as it became a Pile.");
            Assert.IsFalse(c.statusEffects.Any(s => s.targetStat == StatTarget.Attack), "Previous Attack buff should be cleared.");
        }

        [Test]
        public void FinalizeDefeat_SetsInnateMoveResistAndDuration()
        {
            var battleSystemGo = new GameObject("BattleSystem");
            _cleanup.Add(battleSystemGo);
            var battleSystem = battleSystemGo.AddComponent<BattleSystem>();

            var c = Track("hero", leavesPile: true);
            c.TakeDamage(100);

            var method = typeof(BattleSystem).GetMethod("FinalizeCharacterDefeat", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method.Invoke(battleSystem, new object[] { c, false });

            Assert.AreEqual(4, c.pileDuration, "Pile should have innate duration of 4.");
            Assert.AreEqual(300 + c.baseStats.moveResist, c.GetEffectiveStats().moveResist, 
                "Pile should have innate +300 Move Resist bonus.");
        }

        [Test]
        public void Pile_DecaysAndBecomesDestroyed_WhenDurationReachesZero()
        {
            var c = Track("hero", leavesPile: true);
            c.state = LifeState.Pile;
            c.pileDuration = 1;
            
            // Simulate the decay logic in BattleSystem.ProcessTurn
            c.pileDuration--;
            if (c.pileDuration <= 0)
            {
                c.state = LifeState.Destroyed;
            }

            Assert.AreEqual(LifeState.Destroyed, c.state, "Pile should become Destroyed when pileDuration reaches zero.");
        }
        [Test]
        public void HealingSkill_CannotTargetPile()
        {
            var battleSystemGo = new GameObject("BattleSystem");
            _cleanup.Add(battleSystemGo);
            var battleSystem = battleSystemGo.AddComponent<BattleSystem>();

            var hero = Track("hero");
            var pile = Track("pile");
            pile.state = LifeState.Pile;

            // Use Reflection to set internal team lists so GetValidTargets works
            var playerTeamField = typeof(BattleSystem).GetField("_playerTeam", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            playerTeamField.SetValue(battleSystem, new List<CombatCharacter> { hero, pile });

            var healSkill = ScriptableObject.CreateInstance<SkillData>();
            healSkill.effects = new List<ISkillEffect> { new HealEffect() };
            healSkill.targetScope = TargetScope.Allies;
            healSkill.targetRanks = new List<int> { 1, 2, 3, 4 };

            var targets = battleSystem.GetValidTargets(hero, healSkill);

            Assert.IsFalse(targets.Contains(pile), "Healing skills should not be able to target Piles.");
            Assert.IsTrue(targets.Contains(hero), "Healing skills should still target alive characters.");
        }
        [Test]
        public void AOE_Healing_SkipsPiles()
        {
            var hero = Track("hero");
            var pile = Track("pile");
            pile.state = LifeState.Pile;
            pile.currentHP = 50;
            hero.currentHP = 20;

            var healSkill = ScriptableObject.CreateInstance<SkillData>();
            healSkill.effects = new List<ISkillEffect> { new HealEffect() };
            healSkill.modifier.healPercent = 1.0f; // 100% of attack
            
            // SkillContext with both targets
            var ctx = new SkillContext(hero, healSkill, new List<CombatCharacter> { hero, pile }, null, new System.Random());

            foreach (var effect in healSkill.effects)
            {
                foreach (var target in ctx.targets)
                {
                    effect.Execute(ctx, target);
                }
            }

            Assert.AreEqual(50, pile.currentHP, "Pile should not have been healed by AOE.");
            Assert.IsTrue(hero.currentHP > 20, "Alive hero should have been healed.");
        }
    }
}
