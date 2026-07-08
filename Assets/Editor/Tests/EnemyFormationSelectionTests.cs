using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class EnemyFormationSelectionTests
    {
        private EnemyFormationDatabase database;
        private EnemyFormationData formationA;
        private EnemyFormationData formationB;
        private EnemyFormationData formationC;
        private EnemyFormationData formationBoss;
        private CombatConfig combatConfig;
        private GameDatabase gameDatabase;

        [SetUp]
        public void Setup()
        {
            RunSessionManager.ClearAll();

            formationA = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationA.name = "Formation_A";
            formationA.formationId = "FA";
            formationA.enemyPrefabs = new List<GameObject>();

            formationB = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationB.name = "Formation_B";
            formationB.formationId = "FB";
            formationB.enemyPrefabs = new List<GameObject>();

            formationC = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationC.name = "Formation_C";
            formationC.formationId = "FC";
            formationC.enemyPrefabs = new List<GameObject>();

            formationBoss = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationBoss.name = "Formation_Boss";
            formationBoss.formationId = "BOSS1";
            formationBoss.enemyPrefabs = new List<GameObject>();

            database = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            database.trivialFormations = new List<EnemyFormationData> { formationA, formationB, formationC };
            database.earlyGameFormations = new List<EnemyFormationData> { formationA };
            database.midGameFormations = new List<EnemyFormationData> { formationB };
            database.lateGameFormations = new List<EnemyFormationData> { formationC };
            database.bossFormations = new List<EnemyFormationData> { formationBoss };

            combatConfig = ScriptableObject.CreateInstance<CombatConfig>();
            combatConfig.roomTierMappings = new List<RoomTierMapping>
            {
                new RoomTierMapping { roomCount = 1, tier = EnemyEncounterTier.Trivial },
                new RoomTierMapping { roomCount = 3, tier = EnemyEncounterTier.EarlyGame },
                new RoomTierMapping { roomCount = 6, tier = EnemyEncounterTier.MidGame },
                new RoomTierMapping { roomCount = 10, tier = EnemyEncounterTier.LateGame },
                new RoomTierMapping { roomCount = 15, tier = EnemyEncounterTier.Boss }
            };

            // Inject a mock GameDatabase with our formation database
            gameDatabase = GameDatabase.CreateForTesting(enemyFormations: database);
            GameDatabase.SetInstanceForTesting(gameDatabase);

            // Redirect save operations to temp files so tests never touch production saves
            SaveManager.SetSavePathsForTesting(
                Path.Combine(Application.temporaryCachePath, "formation_test_run.dat"),
                Path.Combine(Application.temporaryCachePath, "formation_test_profile.dat")
            );
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.ClearAll();
            GameDatabase.SetInstanceForTesting(null);

            var runPath = Path.Combine(Application.temporaryCachePath, "formation_test_run.dat");
            var profilePath = Path.Combine(Application.temporaryCachePath, "formation_test_profile.dat");
            if (File.Exists(runPath)) File.Delete(runPath);
            if (File.Exists(profilePath)) File.Delete(profilePath);
            SaveManager.SetSavePathsForTesting(null, null);

            ScriptableObject.DestroyImmediate(formationA);
            ScriptableObject.DestroyImmediate(formationB);
            ScriptableObject.DestroyImmediate(formationC);
            ScriptableObject.DestroyImmediate(formationBoss);
            ScriptableObject.DestroyImmediate(database);
            ScriptableObject.DestroyImmediate(combatConfig);
            if (gameDatabase != null) ScriptableObject.DestroyImmediate(gameDatabase);
        }

        [Test]
        public void CombatConfig_GetEncounterTierForRoom_ResolvesCorrectTier()
        {
            Assert.AreEqual(EnemyEncounterTier.Trivial, combatConfig.GetEncounterTierForRoom(0));
            Assert.AreEqual(EnemyEncounterTier.Trivial, combatConfig.GetEncounterTierForRoom(1));
            Assert.AreEqual(EnemyEncounterTier.Trivial, combatConfig.GetEncounterTierForRoom(2));
            
            Assert.AreEqual(EnemyEncounterTier.EarlyGame, combatConfig.GetEncounterTierForRoom(3));
            Assert.AreEqual(EnemyEncounterTier.EarlyGame, combatConfig.GetEncounterTierForRoom(5));

            Assert.AreEqual(EnemyEncounterTier.MidGame, combatConfig.GetEncounterTierForRoom(6));
            Assert.AreEqual(EnemyEncounterTier.MidGame, combatConfig.GetEncounterTierForRoom(9));

            Assert.AreEqual(EnemyEncounterTier.LateGame, combatConfig.GetEncounterTierForRoom(10));
            Assert.AreEqual(EnemyEncounterTier.LateGame, combatConfig.GetEncounterTierForRoom(14));

            Assert.AreEqual(EnemyEncounterTier.Boss, combatConfig.GetEncounterTierForRoom(15));
            Assert.AreEqual(EnemyEncounterTier.Boss, combatConfig.GetEncounterTierForRoom(99));
        }

        [Test]
        public void CombatConfig_GetEncounterTierForRoom_Fallback_ResolvesBossCorrectly()
        {
            var fallbackConfig = ScriptableObject.CreateInstance<CombatConfig>();
            Assert.AreEqual(EnemyEncounterTier.Trivial, fallbackConfig.GetEncounterTierForRoom(0));
            Assert.AreEqual(EnemyEncounterTier.Trivial, fallbackConfig.GetEncounterTierForRoom(1));
            Assert.AreEqual(EnemyEncounterTier.EarlyGame, fallbackConfig.GetEncounterTierForRoom(2));
            Assert.AreEqual(EnemyEncounterTier.EarlyGame, fallbackConfig.GetEncounterTierForRoom(3));
            Assert.AreEqual(EnemyEncounterTier.MidGame, fallbackConfig.GetEncounterTierForRoom(4));
            Assert.AreEqual(EnemyEncounterTier.MidGame, fallbackConfig.GetEncounterTierForRoom(5));
            Assert.AreEqual(EnemyEncounterTier.LateGame, fallbackConfig.GetEncounterTierForRoom(6));
            Assert.AreEqual(EnemyEncounterTier.LateGame, fallbackConfig.GetEncounterTierForRoom(7));
            Assert.AreEqual(EnemyEncounterTier.Boss, fallbackConfig.GetEncounterTierForRoom(8));
            Assert.AreEqual(EnemyEncounterTier.Boss, fallbackConfig.GetEncounterTierForRoom(99));
            ScriptableObject.DestroyImmediate(fallbackConfig);
        }

        [Test]
        public void GetNextRandomFormation_ReturnsNull_WhenNoDatabaseInitialized()
        {
            // Inject a mock database with NO formations
            var mockDb = GameDatabase.CreateForTesting(enemyFormations: null);
            GameDatabase.SetInstanceForTesting(mockDb);

            var result = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Trivial);
            Assert.IsNull(result);
            
            Object.DestroyImmediate(mockDb, true);
        }

        [Test]
        public void GetNextRandomFormation_ReturnsFormation_WhenDatabaseHasOneEntry()
        {
            var singleDb = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            singleDb.trivialFormations = new List<EnemyFormationData> { formationA };
            var singleGameDb = GameDatabase.CreateForTesting(enemyFormations: singleDb);
            GameDatabase.SetInstanceForTesting(singleGameDb);
            RunSessionManager.Initialize();

            var result = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Trivial);
            Assert.AreEqual(formationA, result);

            // Calling again should still return the same (only option)
            var result2 = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Trivial);
            Assert.AreEqual(formationA, result2);

            ScriptableObject.DestroyImmediate(singleDb);
            ScriptableObject.DestroyImmediate(singleGameDb);
        }

        [Test]
        public void GetNextRandomFormation_NeverReturnsConsecutiveDuplicates()
        {
            RunSessionManager.Initialize();

            EnemyFormationData previous = null;
            for (int i = 0; i < 100; i++)
            {
                var current = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Trivial);
                Assert.IsNotNull(current, $"Formation should not be null on iteration {i}");

                if (previous != null)
                {
                    Assert.AreNotEqual(previous, current,
                        $"Consecutive duplicate detected on iteration {i}: {current.name}");
                }

                previous = current;
            }
        }

        [Test]
        public void GetNextRandomFormation_ReturnsFromSpecifiedTier()
        {
            RunSessionManager.Initialize();

            var early = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.EarlyGame);
            Assert.AreEqual(formationA, early, "EarlyGame should return only Formation A");

            var mid = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.MidGame);
            Assert.AreEqual(formationB, mid, "MidGame should return only Formation B");

            var late = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.LateGame);
            Assert.AreEqual(formationC, late, "LateGame should return only Formation C");

            var boss = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Boss);
            Assert.AreEqual(formationBoss, boss, "Boss should return only Formation Boss");
        }

        [Test]
        public void Initialize_ResetsLastSelectedFormation()
        {
            RunSessionManager.Initialize();
            var first = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Trivial);
            Assert.IsNotNull(first);

            // Re-initialize should reset last selection
            RunSessionManager.Initialize();
            Assert.IsNull(RunSessionManager.LastSelectedFormation,
                "LastSelectedFormation should be null after re-initialization");
        }

        [Test]
        public void Clear_ResetsAllFormationState()
        {
            RunSessionManager.Initialize();
            RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Trivial);

            RunSessionManager.ClearAll();

            Assert.IsNull(RunSessionManager.LastSelectedFormation);
            // GameDatabase.Instance is still set (it's global), but after ClearAll
            // LastSelectedFormation is null and BossFormationChances is empty
            Assert.AreEqual(0, RunSessionManager.BossFormationChances.Count);
            var result = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Trivial);
            Assert.IsNotNull(result,
                "After ClearAll, GetNextRandomFormation should still work if GameDatabase is set");
        }

        // --- Boss formation probability tests ---

        [Test]
        public void BossSelection_InitialProbabilitiesAre50Percent()
        {
            // Set up two boss formations
            var bossA = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossA.name = "Boss_A";
            bossA.formationId = "BOSS_A";
            bossA.enemyPrefabs = new List<GameObject>();

            var bossB = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossB.name = "Boss_B";
            bossB.formationId = "BOSS_B";
            bossB.enemyPrefabs = new List<GameObject>();

            database.bossFormations = new List<EnemyFormationData> { bossA, bossB };

            // Select a boss — this should initialize chances to 50/50 then adjust
            var selected = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Boss);
            Assert.IsNotNull(selected);

            // After one selection, the selected boss should have 40% and the other 60%
            Assert.IsTrue(RunSessionManager.BossFormationChances.ContainsKey("BOSS_A"));
            Assert.IsTrue(RunSessionManager.BossFormationChances.ContainsKey("BOSS_B"));

            float totalChance = RunSessionManager.BossFormationChances["BOSS_A"] + RunSessionManager.BossFormationChances["BOSS_B"];
            Assert.AreEqual(1.0f, totalChance, 0.001f, "Total probability should remain 1.0");

            ScriptableObject.DestroyImmediate(bossA);
            ScriptableObject.DestroyImmediate(bossB);
        }

        [Test]
        public void BossSelection_UpdatesProbabilitiesOnSelection()
        {
            var bossA = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossA.name = "Boss_A";
            bossA.formationId = "BOSS_A";
            bossA.enemyPrefabs = new List<GameObject>();

            var bossB = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossB.name = "Boss_B";
            bossB.formationId = "BOSS_B";
            bossB.enemyPrefabs = new List<GameObject>();

            database.bossFormations = new List<EnemyFormationData> { bossA, bossB };

            // Force boss A to be selected by setting its chance very high
            RunSessionManager.BossFormationChances["BOSS_A"] = 1.0f;
            RunSessionManager.BossFormationChances["BOSS_B"] = 0.0f;

            var selected = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Boss);
            Assert.AreEqual(bossA, selected, "Boss A should be selected when it has 100% chance");

            // After selection: A should drop to 0.9, B should rise to 0.1
            Assert.AreEqual(0.9f, RunSessionManager.BossFormationChances["BOSS_A"], 0.001f);
            Assert.AreEqual(0.1f, RunSessionManager.BossFormationChances["BOSS_B"], 0.001f);

            ScriptableObject.DestroyImmediate(bossA);
            ScriptableObject.DestroyImmediate(bossB);
        }

        [Test]
        public void BossSelection_PersistsBetweenRunsAndClear()
        {
            var bossA = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossA.name = "Boss_A";
            bossA.formationId = "BOSS_A";
            bossA.enemyPrefabs = new List<GameObject>();

            var bossB = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossB.name = "Boss_B";
            bossB.formationId = "BOSS_B";
            bossB.enemyPrefabs = new List<GameObject>();

            database.bossFormations = new List<EnemyFormationData> { bossA, bossB };

            // Set known probabilities
            RunSessionManager.BossFormationChances["BOSS_A"] = 0.3f;
            RunSessionManager.BossFormationChances["BOSS_B"] = 0.7f;
            SaveManager.SaveProfile(); // Save the profile explicitly for this test

            // ClearActiveRun should preserve boss chances
            SaveManager.ClearActiveRun();

            // Simulate fresh boot: clear RunSessionManager then load boss chances from file
            RunSessionManager.ClearAll();

            // Accessing BossFormationChances should lazy-load the profile
            float chanceA = RunSessionManager.BossFormationChances["BOSS_A"];
            Assert.AreEqual(0.3f, chanceA, 0.001f);
            Assert.AreEqual(0.7f, RunSessionManager.BossFormationChances["BOSS_B"], 0.001f);

            ScriptableObject.DestroyImmediate(bossA);
            ScriptableObject.DestroyImmediate(bossB);
        }

        [Test]
        public void BossSelection_FallsBackToUniformWhenNotExactlyTwoFormations()
        {
            // 3 boss formations — should use standard anti-repeat logic, not weighted
            var bossA = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossA.name = "Boss_A";
            bossA.formationId = "BOSS_A";
            bossA.enemyPrefabs = new List<GameObject>();

            var bossB = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossB.name = "Boss_B";
            bossB.formationId = "BOSS_B";
            bossB.enemyPrefabs = new List<GameObject>();

            var bossC = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossC.name = "Boss_C";
            bossC.formationId = "BOSS_C";
            bossC.enemyPrefabs = new List<GameObject>();

            database.bossFormations = new List<EnemyFormationData> { bossA, bossB, bossC };

            // Should not crash or modify BossFormationChances
            var result = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Boss);
            Assert.IsNotNull(result);
            // BossFormationChances should remain empty (weighted selection was not used)
            Assert.AreEqual(0, RunSessionManager.BossFormationChances.Count,
                "Weighted selection should not be used for non-2-boss configurations");

            ScriptableObject.DestroyImmediate(bossA);
            ScriptableObject.DestroyImmediate(bossB);
            ScriptableObject.DestroyImmediate(bossC);
        }
    }
}
