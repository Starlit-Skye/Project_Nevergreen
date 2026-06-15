using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class CombatSceneBootstrapFormationTests
    {
        private CombatConfig config;
        private EnemyFormationDatabase database;
        private EnemyFormationData formationSingle;
        private EnemyFormationData formationMulti;

        private GameObject enemyPrefab1;
        private GameObject enemyPrefab2;
        private GameObject bossPrefab;

        private CombatCharacter cc1;
        private CombatCharacter cc2;
        private CombatCharacter ccBoss;

        private GameDatabase gameDatabase;

        [SetUp]
        public void Setup()
        {
            config = CombatTestHelper.CreateDefaultConfig();
            RunSessionManager.Clear();

            // Create temporary prefabs/GameObjects
            enemyPrefab1 = new GameObject("EnemyPrefab1");
            cc1 = enemyPrefab1.AddComponent<CombatCharacter>();
            cc1.characterData = CombatTestHelper.CreateCharacterData("enemy1", "Enemy 1", CombatTestHelper.CreateStatBlock(), CharacterTeamType.Enemy);
            cc1.characterData.size = 1;

            enemyPrefab2 = new GameObject("EnemyPrefab2");
            cc2 = enemyPrefab2.AddComponent<CombatCharacter>();
            cc2.characterData = CombatTestHelper.CreateCharacterData("enemy2", "Enemy 2", CombatTestHelper.CreateStatBlock(), CharacterTeamType.Enemy);
            cc2.characterData.size = 1;

            bossPrefab = new GameObject("BossPrefab");
            ccBoss = bossPrefab.AddComponent<CombatCharacter>();
            ccBoss.characterData = CombatTestHelper.CreateCharacterData("boss", "Boss", CombatTestHelper.CreateStatBlock(), CharacterTeamType.Enemy);
            ccBoss.characterData.size = 2;

            // Setup Formations
            formationSingle = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationSingle.name = "FormationSingle";
            formationSingle.enemyPrefabs = new List<GameObject> { enemyPrefab1, enemyPrefab2 };

            formationMulti = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationMulti.name = "FormationMulti";
            formationMulti.enemyPrefabs = new List<GameObject> { bossPrefab, enemyPrefab1 };

            database = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            database.trivialFormations = new List<EnemyFormationData> { formationSingle, formationMulti };
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.Clear();
            GameDatabase.SetInstanceForTesting(null);

            ScriptableObject.DestroyImmediate(config);
            ScriptableObject.DestroyImmediate(formationSingle);
            ScriptableObject.DestroyImmediate(formationMulti);
            ScriptableObject.DestroyImmediate(database);
            if (gameDatabase != null) ScriptableObject.DestroyImmediate(gameDatabase);

            if (enemyPrefab1 != null) Object.DestroyImmediate(enemyPrefab1);
            if (enemyPrefab2 != null) Object.DestroyImmediate(enemyPrefab2);
            if (bossPrefab != null) Object.DestroyImmediate(bossPrefab);
        }

        [Test]
        public void SpawnTeams_SpawnsFromDatabase_WhenDatabaseIsActive()
        {
            // Initialize database with ONLY the single formation to guarantee it is picked
            var singleDb = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            singleDb.trivialFormations = new List<EnemyFormationData> { formationSingle };
            gameDatabase = GameDatabase.CreateForTesting(enemyFormations: singleDb);
            GameDatabase.SetInstanceForTesting(gameDatabase);
            RunSessionManager.Initialize();

            // Setup Bootstrap GameObject
            var bootGo = new GameObject("Bootstrap");
            var bootstrap = bootGo.AddComponent<CombatSceneBootstrap>();
            bootstrap.enemyBasePosition = new Vector3(3f, 0f, 0f);
            bootstrap.enemyRankSpacing = 2f;

            // Call private SpawnTeams method via reflection
            var spawnMethod = typeof(CombatSceneBootstrap).GetMethod("SpawnTeams", BindingFlags.NonPublic | BindingFlags.Instance);
            spawnMethod.Invoke(bootstrap, null);

            // Fetch spawned enemies via reflection
            var spawnedEnemyField = typeof(CombatSceneBootstrap).GetField("_spawnedEnemyTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            var spawnedList = (List<CombatCharacter>)spawnedEnemyField.GetValue(bootstrap);

            // Assertions
            Assert.AreEqual(2, spawnedList.Count, "Should spawn exactly 2 enemies from the formation.");
            Assert.AreEqual("Enemy 1", spawnedList[0].DisplayName);
            Assert.AreEqual("Enemy 2", spawnedList[1].DisplayName);

            Assert.AreEqual(1, spawnedList[0].rank, "First enemy should be rank 1.");
            Assert.AreEqual(2, spawnedList[1].rank, "Second enemy should be rank 2.");

            // Clean up spawned GameObjects
            foreach (var cc in spawnedList)
            {
                if (cc != null) Object.DestroyImmediate(cc.gameObject);
            }
            Object.DestroyImmediate(bootGo);
            ScriptableObject.DestroyImmediate(singleDb);
        }

        [Test]
        public void SpawnTeams_FallsBackToInspector_WhenNoDatabaseIsActive()
        {
            // GameDatabase is null (no Initialize)
            GameDatabase.SetInstanceForTesting(null);

            // Setup Bootstrap GameObject
            var bootGo = new GameObject("Bootstrap");
            var bootstrap = bootGo.AddComponent<CombatSceneBootstrap>();
            bootstrap.enemyTeamPrefabs = new List<GameObject> { enemyPrefab2 };

            // Call SpawnTeams
            var spawnMethod = typeof(CombatSceneBootstrap).GetMethod("SpawnTeams", BindingFlags.NonPublic | BindingFlags.Instance);
            spawnMethod.Invoke(bootstrap, null);

            // Fetch spawned enemies
            var spawnedEnemyField = typeof(CombatSceneBootstrap).GetField("_spawnedEnemyTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            var spawnedList = (List<CombatCharacter>)spawnedEnemyField.GetValue(bootstrap);

            // Assertions: Should fallback to spawning enemyPrefab2 from inspector list
            Assert.AreEqual(1, spawnedList.Count);
            Assert.AreEqual("Enemy 2", spawnedList[0].DisplayName);
            Assert.AreEqual(1, spawnedList[0].rank);

            // Clean up
            foreach (var cc in spawnedList)
            {
                if (cc != null) Object.DestroyImmediate(cc.gameObject);
            }
            Object.DestroyImmediate(bootGo);
        }

        [Test]
        public void SpawnTeams_HandlesMultiRankSpacingAndRanksCorrectly()
        {
            // Initialize database with ONLY the multi-rank formation (Boss size 2, then Enemy 1 size 1)
            var singleDb = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            singleDb.trivialFormations = new List<EnemyFormationData> { formationMulti };
            gameDatabase = GameDatabase.CreateForTesting(enemyFormations: singleDb);
            GameDatabase.SetInstanceForTesting(gameDatabase);
            RunSessionManager.Initialize();

            // Setup Bootstrap GameObject
            var bootGo = new GameObject("Bootstrap");
            var bootstrap = bootGo.AddComponent<CombatSceneBootstrap>();
            bootstrap.enemyBasePosition = new Vector3(3f, 0f, 0f);
            bootstrap.enemyRankSpacing = 2f;

            // Call SpawnTeams
            var spawnMethod = typeof(CombatSceneBootstrap).GetMethod("SpawnTeams", BindingFlags.NonPublic | BindingFlags.Instance);
            spawnMethod.Invoke(bootstrap, null);

            // Fetch spawned enemies
            var spawnedEnemyField = typeof(CombatSceneBootstrap).GetField("_spawnedEnemyTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            var spawnedList = (List<CombatCharacter>)spawnedEnemyField.GetValue(bootstrap);

            // Assertions
            Assert.AreEqual(2, spawnedList.Count, "Should spawn 2 enemies.");
            var boss = spawnedList[0];
            var minion = spawnedList[1];

            Assert.AreEqual("Boss", boss.DisplayName);
            Assert.AreEqual(2, boss.characterData.size);
            Assert.AreEqual(1, boss.rank, "Boss should be at rank 1.");

            Assert.AreEqual("Enemy 1", minion.DisplayName);
            Assert.AreEqual(1, minion.characterData.size);
            Assert.AreEqual(3, minion.rank, "Enemy 1 should be at rank 3 because Boss occupies ranks 1 and 2.");

            // Position validation:
            // Boss base position = 3.0
            // Rank 1 and Rank 2 center: (3 + (3 + 2)) / 2 = 4.0
            Assert.AreEqual(4f, boss.transform.position.x, 0.001f);

            // Enemy 1 rank spacing offset for rank 3:
            // 3.0 + 2 * (3 - 1) = 7.0
            Assert.AreEqual(7f, minion.transform.position.x, 0.001f);

            // Clean up
            foreach (var cc in spawnedList)
            {
                if (cc != null) Object.DestroyImmediate(cc.gameObject);
            }
            Object.DestroyImmediate(bootGo);
            ScriptableObject.DestroyImmediate(singleDb);
        }
    }
}
