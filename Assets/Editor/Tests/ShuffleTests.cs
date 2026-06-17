using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class ShuffleTests
    {
        private CombatConfig config;
        private System.Random rng;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();
            config = CombatTestHelper.CreateDefaultConfig();
            rng = CombatTestHelper.CreateFixedRng(42);
        }

        [TearDown]
        public void Teardown()
        {
            CombatTestHelper.CleanupTestDatabase();
            ScriptableObject.DestroyImmediate(config);
        }

        private BattleSystem CreateBattleSystem(List<CombatCharacter> playerTeam, List<CombatCharacter> enemyTeam)
        {
            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();

            var playerTeamField = typeof(BattleSystem).GetField("_playerTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            playerTeamField.SetValue(bs, playerTeam);

            var enemyTeamField = typeof(BattleSystem).GetField("_enemyTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            enemyTeamField.SetValue(bs, enemyTeam);

            var animQueue = bsGo.AddComponent<AnimationQueueProcessor>();

            return bs;
        }

        [Test]
        public void Shuffle_MovesToDifferentRank()
        {
            var char1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            var char2 = CombatTestHelper.CreateCombatCharacter("P2", Team.Player, 2);
            var char3 = CombatTestHelper.CreateCombatCharacter("P3", Team.Player, 3);
            
            var playerTeam = new List<CombatCharacter> { char1, char2, char3 };
            var bs = CreateBattleSystem(playerTeam, new List<CombatCharacter>());

            // Start at rank 2
            int startingRank = char2.rank;

            // Apply shuffle status
            var shuffleStatus = new ShuffleStatusInstance(bs, rng);
            char2.AddStatus(shuffleStatus);

            // It should move to rank 1 or 3, but definitely not rank 2.
            Assert.AreNotEqual(startingRank, char2.rank, "P2 should have moved to a different rank.");
            Assert.IsTrue(char2.rank == 1 || char2.rank == 3, $"P2 rank should be 1 or 3, but was {char2.rank}.");

            Object.DestroyImmediate(char1.gameObject);
            Object.DestroyImmediate(char2.gameObject);
            Object.DestroyImmediate(char3.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Shuffle_NoChangeIfSingleCharacter()
        {
            var char1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            
            var playerTeam = new List<CombatCharacter> { char1 };
            var bs = CreateBattleSystem(playerTeam, new List<CombatCharacter>());

            var shuffleStatus = new ShuffleStatusInstance(bs, rng);
            char1.AddStatus(shuffleStatus);

            // Since there's only 1 character, max rank is 1. Rank should remain 1.
            Assert.AreEqual(1, char1.rank, "P1 should remain at rank 1 because there is no other rank to shuffle to.");

            Object.DestroyImmediate(char1.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Shuffle_StatusExpiresImmediately()
        {
            var char1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            var char2 = CombatTestHelper.CreateCombatCharacter("P2", Team.Player, 2);
            
            var playerTeam = new List<CombatCharacter> { char1, char2 };
            var bs = CreateBattleSystem(playerTeam, new List<CombatCharacter>());

            var shuffleStatus = new ShuffleStatusInstance(bs, rng);
            char1.AddStatus(shuffleStatus);

            Assert.IsTrue(shuffleStatus.IsExpired, "ShuffleStatus should be expired immediately.");
            Assert.AreEqual(0, char1.statusEffects.Count, "ShuffleStatus should be removed immediately from character's status list.");

            Object.DestroyImmediate(char1.gameObject);
            Object.DestroyImmediate(char2.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Shuffle_NullBattleSystem_FailsGracefully()
        {
            var char1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            
            var shuffleStatus = new ShuffleStatusInstance(null, rng);
            
            Assert.DoesNotThrow(() => char1.AddStatus(shuffleStatus), "Adding shuffle status with null BattleSystem should not throw.");
            Assert.AreEqual(1, char1.rank, "Rank should not change when BattleSystem is null.");
            
            Object.DestroyImmediate(char1.gameObject);
        }

        [Test]
        public void Shuffle_AppliedViaStatusEffect_IgnoresResistance()
        {
            // Give 300 move resistance to mimic high resistance (which usually blocks Move status)
            var target = CombatTestHelper.CreateCombatCharacter("Target", Team.Enemy, 1, moveResist: 300);
            var attacker = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Player, 1);
            
            var playerTeam = new List<CombatCharacter> { attacker };
            // Need a second enemy for shuffle to have a valid alternative rank
            var target2 = CombatTestHelper.CreateCombatCharacter("Target2", Team.Enemy, 2);
            var enemyTeam = new List<CombatCharacter> { target, target2 };
            
            var bs = CreateBattleSystem(playerTeam, enemyTeam);

            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(attacker, skill, enemyTeam, bs, rng);
            ctx.didHit = true; 

            var shuffleEffect = new Nevergreen.Combat.StatusEffect
            {
                statusType = StatusType.Shuffle,
                applicationChance = 100,
                amplitude = 1,
                duration = 0
            };

            // Before shuffling, target is at rank 1
            Assert.AreEqual(1, target.rank);

            // Execute the Shuffle effect (should succeed because it has no resistance mapping, defaulting to 0 resistance)
            shuffleEffect.Execute(ctx, target);

            // Target should now be at rank 2
            Assert.AreEqual(2, target.rank, "Target should have shuffled to rank 2 despite having 300 moveResist.");

            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(target2.gameObject);
            Object.DestroyImmediate(attacker.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }
    }
}
