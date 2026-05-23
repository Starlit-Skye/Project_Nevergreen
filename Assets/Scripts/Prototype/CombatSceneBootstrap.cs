using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// Prototype combat scene bootstrap.
    /// Takes lists of character prefabs for player/enemy teams,
    /// spawns them at ranked positions, and starts combat.
    /// </summary>
    public class CombatSceneBootstrap : MonoBehaviour
    {
        [Header("Team Setup")]
        [Tooltip("Character prefabs for the player team (up to 4). Index 0 = rank 1 (front).")]
        public List<GameObject> playerTeamPrefabs = new List<GameObject>();

        [Tooltip("Character prefabs for the enemy team (up to 4). Index 0 = rank 1 (front).")]
        public List<GameObject> enemyTeamPrefabs = new List<GameObject>();

        [Header("Spawn Layout")]
        [Tooltip("Base position for player team rank 1 (front-most, closest to center).")]
        public Vector3 playerBasePosition = new Vector3(-3f, 0f, 0f);

        [Tooltip("Spacing between player ranks (positive X moves away from center).")]
        public float playerRankSpacing = -2f;

        [Tooltip("Base position for enemy team rank 1 (front-most, closest to center).")]
        public Vector3 enemyBasePosition = new Vector3(3f, 0f, 0f);

        [Tooltip("Spacing between enemy ranks (positive X moves away from center).")]
        public float enemyRankSpacing = 2f;

        [Header("References")]
        [Tooltip("BattleSystem component in the scene.")]
        public BattleSystem battleSystem;

        [Tooltip("Combat UI controller in the scene.")]
        public CombatUI combatUI;

        [Header("Playtesting Variants")]
        [Tooltip("The ScriptableObject containing enemy configs for battle variants.")]
        public BattleVariantsConfig variantsConfig;

        private List<CombatCharacter> _spawnedPlayerTeam = new List<CombatCharacter>();
        private List<CombatCharacter> _spawnedEnemyTeam = new List<CombatCharacter>();

        private void Start()
        {
            if (variantsConfig != null && variantsConfig.variants != null && variantsConfig.variants.Count > 0)
            {
                // Instantiate selection overlay
                VariantSelectionOverlay.Create(variantsConfig.variants, OnVariantSelected);
            }
            else
            {
                Debug.LogWarning("[Bootstrap] No BattleVariantsConfig found or configured. Spawning default enemy team.");
                SpawnTeams();
                InitializeBattle();
            }
        }

        private void OnVariantSelected(int variantIndex)
        {
            if (variantsConfig == null || variantsConfig.variants == null || variantIndex < 0 || variantIndex >= variantsConfig.variants.Count)
            {
                Debug.LogError($"[Bootstrap] Invalid variant index selected: {variantIndex}");
                SpawnTeams();
                InitializeBattle();
                return;
            }

            var chosenVariant = variantsConfig.variants[variantIndex];
            Debug.Log($"[Bootstrap] Selected Battle Variant: {chosenVariant.variantName}");

            // Assign the selected variant's enemy prefabs to the team prefabs list
            enemyTeamPrefabs = new List<GameObject>(chosenVariant.enemyPrefabs);

            // Spawn the teams and initialize battle
            SpawnTeams();
            InitializeBattle();
        }

        private void SpawnTeams()
        {
            // Spawn player team (players are always size 1)
            int nextPlayerRank = 1;
            for (int i = 0; i < playerTeamPrefabs.Count && nextPlayerRank <= 4; i++)
            {
                if (playerTeamPrefabs[i] == null) continue;

                int charSize = 1;
                var prefabCC = playerTeamPrefabs[i].GetComponent<CombatCharacter>();
                if (prefabCC != null && prefabCC.characterData != null)
                    charSize = prefabCC.characterData.size;

                // Check if this character fits within the remaining slots
                if (nextPlayerRank + charSize - 1 > 4) break;

                // Calculate centered position for multi-rank characters
                float posX = playerBasePosition.x;
                if (charSize == 1)
                {
                    posX += playerRankSpacing * (nextPlayerRank - 1);
                }
                else
                {
                    float sum = 0f;
                    for (int r = 0; r < charSize; r++)
                        sum += playerRankSpacing * (nextPlayerRank - 1 + r);
                    posX += sum / charSize;
                }

                Vector3 pos = new Vector3(posX, playerBasePosition.y, playerBasePosition.z);
                GameObject go = Instantiate(playerTeamPrefabs[i], pos, Quaternion.identity);
                go.name = $"Player_{nextPlayerRank}_{playerTeamPrefabs[i].name}";

                CombatCharacter cc = go.GetComponent<CombatCharacter>();
                if (cc == null)
                {
                    Debug.LogError($"[Bootstrap] Prefab '{playerTeamPrefabs[i].name}' missing CombatCharacter!");
                    Destroy(go);
                    continue;
                }

                // Face right (player team faces right)
                go.transform.localScale = new Vector3(
                    Mathf.Abs(go.transform.localScale.x),
                    go.transform.localScale.y,
                    go.transform.localScale.z);

                cc.InitializeForCombat(Team.Player, nextPlayerRank);
                _spawnedPlayerTeam.Add(cc);

                Debug.Log($"[Bootstrap] Spawned player: {cc.DisplayName} at rank {cc.rank} (size {charSize})");
                nextPlayerRank += charSize;
            }

            // Spawn enemy team (enemies can be multi-rank)
            int nextEnemyRank = 1;
            for (int i = 0; i < enemyTeamPrefabs.Count && nextEnemyRank <= 4; i++)
            {
                if (enemyTeamPrefabs[i] == null) continue;

                int charSize = 1;
                var prefabCC = enemyTeamPrefabs[i].GetComponent<CombatCharacter>();
                if (prefabCC != null && prefabCC.characterData != null)
                    charSize = prefabCC.characterData.size;

                // Check if this character fits within the remaining slots
                if (nextEnemyRank + charSize - 1 > 4) break;

                // Calculate centered position for multi-rank characters
                float posX = enemyBasePosition.x;
                if (charSize == 1)
                {
                    posX += enemyRankSpacing * (nextEnemyRank - 1);
                }
                else
                {
                    float sum = 0f;
                    for (int r = 0; r < charSize; r++)
                        sum += enemyRankSpacing * (nextEnemyRank - 1 + r);
                    posX += sum / charSize;
                }

                Vector3 pos = new Vector3(posX, enemyBasePosition.y, enemyBasePosition.z);
                GameObject go = Instantiate(enemyTeamPrefabs[i], pos, Quaternion.identity);
                go.name = $"Enemy_{nextEnemyRank}_{enemyTeamPrefabs[i].name}";

                CombatCharacter cc = go.GetComponent<CombatCharacter>();
                if (cc == null)
                {
                    Debug.LogError($"[Bootstrap] Prefab '{enemyTeamPrefabs[i].name}' missing CombatCharacter!");
                    Destroy(go);
                    continue;
                }

                // Face left (enemy team faces left)
                go.transform.localScale = new Vector3(
                    -Mathf.Abs(go.transform.localScale.x),
                    go.transform.localScale.y,
                    go.transform.localScale.z);

                cc.InitializeForCombat(Team.Enemy, nextEnemyRank);
                _spawnedEnemyTeam.Add(cc);

                Debug.Log($"[Bootstrap] Spawned enemy: {cc.DisplayName} at rank {cc.rank} (size {charSize})");
                nextEnemyRank += charSize;
            }
        }

        private void InitializeBattle()
        {
            if (battleSystem == null)
            {
                Debug.LogError("[Bootstrap] BattleSystem not assigned!");
                return;
            }

            if (combatUI != null)
            {
                combatUI.Initialize(battleSystem, _spawnedPlayerTeam, _spawnedEnemyTeam);
            }

            // Inject layout settings for rank shifting
            battleSystem.playerBaseX = playerBasePosition.x;
            battleSystem.playerSpacingX = playerRankSpacing;
            battleSystem.enemyBaseX = enemyBasePosition.x;
            battleSystem.enemySpacingX = enemyRankSpacing;

            battleSystem.StartBattle(_spawnedPlayerTeam, _spawnedEnemyTeam);
        }
    }
}
