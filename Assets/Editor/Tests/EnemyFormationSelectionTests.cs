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
            RunSessionManager.Clear();

            formationA = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationA.name = "Formation_A";
            formationA.enemyPrefabs = new List<GameObject>();

            formationB = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationB.name = "Formation_B";
            formationB.enemyPrefabs = new List<GameObject>();

            formationC = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationC.name = "Formation_C";
            formationC.enemyPrefabs = new List<GameObject>();

            formationBoss = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationBoss.name = "Formation_Boss";
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

            // Redirect save operations to a temp file so tests never touch production save.dat
            SaveManager.SetSavePathForTesting(Path.Combine(Application.temporaryCachePath, "formation_test_save.dat"));
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.Clear();
            GameDatabase.SetInstanceForTesting(null);

            var testPath = Path.Combine(Application.temporaryCachePath, "formation_test_save.dat");
            if (File.Exists(testPath)) File.Delete(testPath);
            SaveManager.SetSavePathForTesting(null);

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

            RunSessionManager.Clear();

            Assert.IsNull(RunSessionManager.LastSelectedFormation);
            // GameDatabase.Instance is still set (it's global), but after Clear
            // LastSelectedFormation is null
            var result = RunSessionManager.GetNextRandomFormation(EnemyEncounterTier.Trivial);
            Assert.IsNotNull(result,
                "After Clear, GetNextRandomFormation should still work if GameDatabase is set");
        }
    }
}
