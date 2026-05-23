using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Prototype;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Tests
{
    /// <summary>
    /// Unit tests verifying the Battle Variant selection and initialization sequence.
    /// </summary>
    public class BattleVariantTests
    {
        private CombatConfig config;
        private BattleSystem battleSystem;
        private CombatUI combatUI;
        private BattleVariantsConfig variantsConfig;
        private GameObject bootstrapGo;
        private CombatSceneBootstrap bootstrap;
        private GameObject playerPrefab;

        [SetUp]
        public void Setup()
        {
            config = CombatTestHelper.CreateDefaultConfig();
            
            // Create BattleSystem
            var bsGo = new GameObject("BattleSystem");
            battleSystem = bsGo.AddComponent<BattleSystem>();
            battleSystem.combatConfig = config;

            // Create CombatUI
            var uiGo = new GameObject("CombatUI");
            combatUI = uiGo.AddComponent<CombatUI>();

            // Create CombatSceneBootstrap
            bootstrapGo = new GameObject("Bootstrap");
            bootstrap = bootstrapGo.AddComponent<CombatSceneBootstrap>();
            bootstrap.battleSystem = battleSystem;
            bootstrap.combatUI = combatUI;

            // Configure player team defaults so SpawnTeams doesn't fail
            playerPrefab = new GameObject("TestPlayerPrefab");
            playerPrefab.AddComponent<CombatCharacter>();
            var pStats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var pData = CombatTestHelper.CreateCharacterData("player", "Player", pStats, CharacterTeamType.Player);
            playerPrefab.GetComponent<CombatCharacter>().characterData = pData;

            bootstrap.playerTeamPrefabs = new List<GameObject> { playerPrefab };

            // Create BattleVariantsConfig ScriptableObject
            variantsConfig = ScriptableObject.CreateInstance<BattleVariantsConfig>();
            bootstrap.variantsConfig = variantsConfig;
        }

        [TearDown]
        public void Teardown()
        {
            if (bootstrapGo != null) Object.DestroyImmediate(bootstrapGo);
            if (battleSystem != null && battleSystem.gameObject != null) Object.DestroyImmediate(battleSystem.gameObject);
            if (combatUI != null && combatUI.gameObject != null) Object.DestroyImmediate(combatUI.gameObject);
            
            if (variantsConfig != null) ScriptableObject.DestroyImmediate(variantsConfig);
            if (config != null) ScriptableObject.DestroyImmediate(config);
            if (playerPrefab != null) Object.DestroyImmediate(playerPrefab);

            // Clean up any remaining selection canvases
            var canvas = GameObject.Find("VariantSelectionCanvas");
            if (canvas != null) Object.DestroyImmediate(canvas);
        }

        [Test]
        public void FallbackToDefault_WhenConfigIsNull()
        {
            // Arrange
            bootstrap.variantsConfig = null;
            
            var defaultEnemyPrefab = new GameObject("DefaultEnemy");
            defaultEnemyPrefab.AddComponent<CombatCharacter>();
            var eStats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var eData = CombatTestHelper.CreateCharacterData("default_enemy", "Default Enemy", eStats, CharacterTeamType.Enemy);
            defaultEnemyPrefab.GetComponent<CombatCharacter>().characterData = eData;

            bootstrap.enemyTeamPrefabs = new List<GameObject> { defaultEnemyPrefab };

            // Act: Call Start via reflection
            var startMethod = typeof(CombatSceneBootstrap).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(bootstrap, null);

            // Assert: Battle should be active, default enemy spawned
            Assert.IsTrue(battleSystem.CurrentState == BattleState.RoundStart || battleSystem.CurrentState == BattleState.CharacterTurn,
                $"Battle should start immediately using fallback (actual state: {battleSystem.CurrentState}).");
            Assert.AreEqual(1, battleSystem.EnemyTeam.Count, "One default enemy should have spawned.");
            Assert.AreEqual("default_enemy", battleSystem.EnemyTeam[0].CharacterId);

            Object.DestroyImmediate(defaultEnemyPrefab);
        }

        [Test]
        public void SelectionOverlaySpawns_AndBlocksBattleStart_WhenConfigExists()
        {
            // Arrange: Setup 2 variants
            var enemyPrefab1 = new GameObject("Variant1Enemy");
            enemyPrefab1.AddComponent<CombatCharacter>();
            var e1Stats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var e1Data = CombatTestHelper.CreateCharacterData("v1_enemy", "V1 Enemy", e1Stats, CharacterTeamType.Enemy);
            enemyPrefab1.GetComponent<CombatCharacter>().characterData = e1Data;

            var variant1 = new BattleVariant
            {
                variantName = "Variant 1",
                enemyPrefabs = new List<GameObject> { enemyPrefab1 }
            };

            variantsConfig.variants = new List<BattleVariant> { variant1 };

            // Act: Invoke Start
            var startMethod = typeof(CombatSceneBootstrap).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(bootstrap, null);

            // Assert: Battle should NOT be active yet (inactive)
            Assert.AreEqual(BattleState.Inactive, battleSystem.CurrentState, "Battle should not start while selection overlay is active.");

            // Assert: Selection Overlay UI canvas should be in the scene
            var canvas = GameObject.Find("VariantSelectionCanvas");
            Assert.IsNotNull(canvas, "VariantSelectionCanvas overlay should have spawned.");

            // Act: Select variant 0
            var selectMethod = typeof(CombatSceneBootstrap).GetMethod("OnVariantSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectMethod.Invoke(bootstrap, new object[] { 0 });

            // Assert: Now battle should start, and variant 1 enemy spawned
            Assert.IsTrue(battleSystem.CurrentState == BattleState.RoundStart || battleSystem.CurrentState == BattleState.CharacterTurn,
                $"Battle should start after variant selection (actual state: {battleSystem.CurrentState}).");
            Assert.AreEqual(1, battleSystem.EnemyTeam.Count, "One variant enemy should have spawned.");
            Assert.AreEqual("v1_enemy", battleSystem.EnemyTeam[0].CharacterId);

            Object.DestroyImmediate(enemyPrefab1);
        }
    }
}
