using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class LivingFragmentsTests
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
            if (config != null) ScriptableObject.DestroyImmediate(config, true);
        }

        private BattleSystem CreateBattleSystem(List<CombatCharacter> playerTeam, List<CombatCharacter> enemyTeam)
        {
            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();

            var playerTeamField = typeof(BattleSystem).GetField("_playerTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            playerTeamField?.SetValue(bs, playerTeam);

            var enemyTeamField = typeof(BattleSystem).GetField("_enemyTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enemyTeamField?.SetValue(bs, enemyTeam);

            // Needed for character lifecycle events like registering/removing characters during destruction
            var formationManager = new FormationManager();
            var animationQueue = bsGo.AddComponent<AnimationQueueProcessor>();
            formationManager.Initialize(animationQueue);
            
            var lifecycleManager = new CharacterLifecycleManager();
            lifecycleManager.Initialize(formationManager, animationQueue, bs);

            var lmField = typeof(BattleSystem).GetField("_lifecycleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lmField?.SetValue(bs, lifecycleManager);

            foreach (var c in playerTeam) lifecycleManager.SubscribeCharacter(c);
            foreach (var c in enemyTeam) lifecycleManager.SubscribeCharacter(c);

            return bs;
        }

        [Test]
        public void StatusApplied_HostDestroyed_SummonsPrefabsSequentiallyFromHostRank()
        {
            var target = CombatTestHelper.CreateCombatCharacter("Target", Team.Enemy, 1);
            var enemyTeam = new List<CombatCharacter> { target };
            var bs = CreateBattleSystem(new List<CombatCharacter>(), enemyTeam);

            // Create prefabs to summon
            var prefab1 = CombatTestHelper.CreateCombatCharacter("Prefab1", Team.Enemy, 1);
            var prefab2 = CombatTestHelper.CreateCombatCharacter("Prefab2", Team.Enemy, 1);

            var enemyPrefabs = new List<GameObject> { prefab1.gameObject, prefab2.gameObject };

            var livingFragmentsStatus = new LivingFragmentsStatusInstance(bs, enemyPrefabs, 3);
            target.AddStatus(livingFragmentsStatus);

            Assert.AreEqual(1, bs.EnemyTeam.Count);

            // Trigger destruction
            target.state = LifeState.Destroyed;
            
            Assert.AreEqual(2, bs.EnemyTeam.Count, "Two fragments should be summoned, target is removed.");
            Assert.IsFalse(bs.EnemyTeam.Contains(target));
            
            var firstSummoned = bs.EnemyTeam.FirstOrDefault(c => c.gameObject.name.Contains("Prefab1"));
            var secondSummoned = bs.EnemyTeam.FirstOrDefault(c => c.gameObject.name.Contains("Prefab2"));
            
            Assert.IsNotNull(firstSummoned);
            Assert.IsNotNull(secondSummoned);

            Assert.AreEqual(1, firstSummoned.rank);
            Assert.AreEqual(2, secondSummoned.rank);

            Object.DestroyImmediate(prefab1.gameObject);
            Object.DestroyImmediate(prefab2.gameObject);
            foreach (var c in bs.EnemyTeam.ToList()) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void StatusApplied_TeamAtCapacity_SkipsSummonWhenFull()
        {
            var target = CombatTestHelper.CreateCombatCharacter("Target", Team.Enemy, 1);
            var ally2 = CombatTestHelper.CreateCombatCharacter("Ally2", Team.Enemy, 2);
            var ally3 = CombatTestHelper.CreateCombatCharacter("Ally3", Team.Enemy, 3);
            var ally4 = CombatTestHelper.CreateCombatCharacter("Ally4", Team.Enemy, 4);

            var enemyTeam = new List<CombatCharacter> { target, ally2, ally3, ally4 };
            var bs = CreateBattleSystem(new List<CombatCharacter>(), enemyTeam);

            var prefab1 = CombatTestHelper.CreateCombatCharacter("Prefab1", Team.Enemy, 1);
            var prefab2 = CombatTestHelper.CreateCombatCharacter("Prefab2", Team.Enemy, 1);

            var livingFragmentsStatus = new LivingFragmentsStatusInstance(bs, new List<GameObject> { prefab1.gameObject, prefab2.gameObject }, 3);
            target.AddStatus(livingFragmentsStatus);

            Assert.AreEqual(4, bs.EnemyTeam.Count);

            target.state = LifeState.Destroyed;

            // Target is removed (team drops to 3 temporarily).
            // Living fragments tries to spawn 2 prefabs.
            // First prefab spawns (making team 4).
            // Second prefab is skipped because team is at max capacity (4).
            
            Assert.AreEqual(4, bs.EnemyTeam.Count, "Team should not exceed 4 members.");
            Assert.IsFalse(bs.EnemyTeam.Contains(target));
            Assert.AreEqual(1, bs.EnemyTeam.Count(c => c.gameObject.name.Contains("Prefab")));

            Object.DestroyImmediate(prefab1.gameObject);
            Object.DestroyImmediate(prefab2.gameObject);
            foreach (var c in bs.EnemyTeam.ToList()) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void StatusExpired_HostDestroyedLater_DoesNotSummon()
        {
            var target = CombatTestHelper.CreateCombatCharacter("Target", Team.Enemy, 1);
            var enemyTeam = new List<CombatCharacter> { target };
            var bs = CreateBattleSystem(new List<CombatCharacter>(), enemyTeam);

            var prefab1 = CombatTestHelper.CreateCombatCharacter("Prefab1", Team.Enemy, 1);

            var livingFragmentsStatus = new LivingFragmentsStatusInstance(bs, new List<GameObject> { prefab1.gameObject }, 3);
            target.AddStatus(livingFragmentsStatus);

            // Remove status before destruction
            target.RemoveStatus(livingFragmentsStatus);

            target.state = LifeState.Destroyed;

            // Should not summon anything
            Assert.AreEqual(0, bs.EnemyTeam.Count);
            
            Object.DestroyImmediate(prefab1.gameObject);
            foreach (var c in bs.EnemyTeam.ToList()) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }
    }
}
