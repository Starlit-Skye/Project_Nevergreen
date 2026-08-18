using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class EconomySystemTests
    {
        private string _testRunPath;
        private string _testProfilePath;

        [SetUp]
        public void SetUp()
        {
            _testRunPath = System.IO.Path.Combine(Application.temporaryCachePath, "test_run.dat");
            _testProfilePath = System.IO.Path.Combine(Application.temporaryCachePath, "test_profile.dat");
            SaveManager.SetSavePathsForTesting(_testRunPath, _testProfilePath);
            
            RunSessionManager.ClearAll();
            CombatTestHelper.InitializeTestDatabase();
        }

        [TearDown]
        public void TearDown()
        {
            if (System.IO.File.Exists(_testRunPath)) System.IO.File.Delete(_testRunPath);
            if (System.IO.File.Exists(_testProfilePath)) System.IO.File.Delete(_testProfilePath);
            SaveManager.SetSavePathsForTesting(null, null);

            RunSessionManager.ClearAll();
            CombatTestHelper.CleanupTestDatabase();
        }

        [Test]
        public void Scraps_Initialize_SetsToZero()
        {
            RunSessionManager.Scraps = 50;
            RunSessionManager.Initialize();
            Assert.AreEqual(0, RunSessionManager.Scraps);
        }

        [Test]
        public void GrantScraps_IncreasesBalance()
        {
            RunSessionManager.Scraps = 10;
            RunSessionManager.GrantScraps(15);
            Assert.AreEqual(25, RunSessionManager.Scraps);
        }

        [Test]
        public void TrySpendScraps_SucceedsIfEnoughBalance()
        {
            RunSessionManager.Scraps = 20;
            bool success = RunSessionManager.TrySpendScraps(15);
            Assert.IsTrue(success);
            Assert.AreEqual(5, RunSessionManager.Scraps);
        }

        [Test]
        public void TrySpendScraps_FailsIfNotEnoughBalance()
        {
            RunSessionManager.Scraps = 10;
            bool success = RunSessionManager.TrySpendScraps(15);
            Assert.IsFalse(success);
            Assert.AreEqual(10, RunSessionManager.Scraps);
        }

        [Test]
        public void SaveManager_SavesAndRestoresScraps()
        {
            RunSessionManager.Initialize();
            RunSessionManager.Parts = 100;
            RunSessionManager.Scraps = 50;
            SaveManager.SaveRun();

            RunSessionManager.Clear();
            Assert.AreEqual(0, RunSessionManager.Parts);
            Assert.AreEqual(0, RunSessionManager.Scraps);

            bool loaded = SaveManager.LoadRun();
            Assert.IsTrue(loaded);
            Assert.AreEqual(100, RunSessionManager.Parts);
            Assert.AreEqual(50, RunSessionManager.Scraps);
        }

        [Test]
        public void CombatConfig_GetRewardRanges_ReturnsProfileValues()
        {
            var config = ScriptableObject.CreateInstance<CombatConfig>();
            config.minPartsPerBattle = 5;
            config.maxPartsPerBattle = 10;
            config.minScrapsPerBattle = 1;
            config.maxScrapsPerBattle = 5;

            config.tierRewardProfiles.Add(new TierRewardProfile 
            { 
                tier = EnemyEncounterTier.Boss, 
                minParts = 50, 
                maxParts = 100, 
                minScraps = 20, 
                maxScraps = 40 
            });

            config.GetRewardRanges(EnemyEncounterTier.Boss, out int minP, out int maxP, out int minS, out int maxS);
            Assert.AreEqual(50, minP);
            Assert.AreEqual(100, maxP);
            Assert.AreEqual(20, minS);
            Assert.AreEqual(40, maxS);
        }

        [Test]
        public void CombatConfig_GetRewardRanges_FallsBackToDefaults()
        {
            var config = ScriptableObject.CreateInstance<CombatConfig>();
            config.minPartsPerBattle = 5;
            config.maxPartsPerBattle = 10;
            config.minScrapsPerBattle = 1;
            config.maxScrapsPerBattle = 5;

            config.GetRewardRanges(EnemyEncounterTier.MidGame, out int minP, out int maxP, out int minS, out int maxS);
            Assert.AreEqual(5, minP);
            Assert.AreEqual(10, maxP);
            Assert.AreEqual(1, minS);
            Assert.AreEqual(5, maxS);
        }

        [Test]
        public void BattleRewardHandler_ApplyVictoryRewards_GrantsScrapsBasedOnTier()
        {
            var config = ScriptableObject.CreateInstance<CombatConfig>();
            config.tierRewardProfiles.Add(new TierRewardProfile 
            { 
                tier = EnemyEncounterTier.LateGame, 
                minParts = 100, 
                maxParts = 100, 
                minScraps = 50, 
                maxScraps = 50 
            });

            var playerTeam = new List<CombatCharacter>(); // Empty for simple test
            var rng = new System.Random(123);

            RunSessionManager.Parts = 0;
            RunSessionManager.Scraps = 0;

            BattleRewardHandler.ApplyVictoryRewards(playerTeam, config, EnemyEncounterTier.LateGame, rng, out int partsGranted, out int scrapsGranted);

            Assert.AreEqual(100, partsGranted);
            Assert.AreEqual(50, scrapsGranted);
            Assert.AreEqual(100, RunSessionManager.Parts);
            Assert.AreEqual(50, RunSessionManager.Scraps);
        }
    }
}
