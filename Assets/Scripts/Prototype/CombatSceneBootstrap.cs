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

        private List<CombatCharacter> _spawnedPlayerTeam = new List<CombatCharacter>();
        private List<CombatCharacter> _spawnedEnemyTeam = new List<CombatCharacter>();

        private void Start()
        {
            SpawnTeams();
            InitializeBattle();
        }

        private void SpawnTeams()
        {
            // Spawn player team
            for (int i = 0; i < playerTeamPrefabs.Count && i < 4; i++)
            {
                if (playerTeamPrefabs[i] == null) continue;

                Vector3 pos = playerBasePosition + new Vector3(playerRankSpacing * i, 0f, 0f);
                GameObject go = Instantiate(playerTeamPrefabs[i], pos, Quaternion.identity);
                go.name = $"Player_{i + 1}_{playerTeamPrefabs[i].name}";

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

                cc.InitializeForCombat(Team.Player, i + 1);
                _spawnedPlayerTeam.Add(cc);

                Debug.Log($"[Bootstrap] Spawned player: {cc.DisplayName} at rank {cc.rank}");
            }

            // Spawn enemy team
            for (int i = 0; i < enemyTeamPrefabs.Count && i < 4; i++)
            {
                if (enemyTeamPrefabs[i] == null) continue;

                Vector3 pos = enemyBasePosition + new Vector3(enemyRankSpacing * i, 0f, 0f);
                GameObject go = Instantiate(enemyTeamPrefabs[i], pos, Quaternion.identity);
                go.name = $"Enemy_{i + 1}_{enemyTeamPrefabs[i].name}";

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

                cc.InitializeForCombat(Team.Enemy, i + 1);
                _spawnedEnemyTeam.Add(cc);

                Debug.Log($"[Bootstrap] Spawned enemy: {cc.DisplayName} at rank {cc.rank}");
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
