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
            // Check if the next room has a non-combat effect that should run immediately
            if (RunSessionManager.NextRoomData != null &&
                RunSessionManager.NextRoomData.activationType == Nevergreen.Data.RoomActivationType.OnRoomLoaded)
            {
                RunSessionManager.ActivateCurrentRoomEffect();
                RunSessionManager.NextRoomData = null;
                return; // Bypass combat entirely
            }

            SpawnTeams();
            InitializeBattle();

            // If a ContinuousCombat room effect is queued, activate it now (after battle init)
            if (RunSessionManager.NextRoomData != null &&
                RunSessionManager.NextRoomData.activationType == Nevergreen.Data.RoomActivationType.ContinuousCombat)
            {
                RunSessionManager.ActivateCurrentRoomEffect();
                RunSessionManager.NextRoomData = null;
            }
        }

        private void SpawnTeams()
        {
            // 1. Resolve which player prefabs to spawn
            var prefabsToSpawn = new List<(GameObject prefab, PartyMemberInfo partyInfo)>();
            bool useSessionParty = RunSessionManager.CurrentParty != null && RunSessionManager.CurrentParty.Count > 0;

            if (useSessionParty)
            {
                foreach (var partyMember in RunSessionManager.CurrentParty)
                {
                    if (partyMember == null || partyMember.character == null) continue;

                    if (partyMember.currentHP.HasValue && partyMember.currentHP.Value <= 0)
                    {
                        Debug.Log($"[Bootstrap] Skipping spawn for {partyMember.character.displayName} (HP is {partyMember.currentHP.Value})");
                        continue;
                    }

                    // 1. Check direct prefab reference on CharacterData
                    GameObject matchingPrefab = partyMember.character.characterPrefab != null ? partyMember.character.characterPrefab.gameObject : null;

                    // 2. Fallback to searching in playerTeamPrefabs
                    if (matchingPrefab == null)
                    {
                        foreach (var prefab in playerTeamPrefabs)
                        {
                            if (prefab == null) continue;
                            var ccComp = prefab.GetComponent<CombatCharacter>();
                            if (ccComp != null && ccComp.characterData == partyMember.character)
                            {
                                matchingPrefab = prefab;
                                break;
                            }
                        }
                    }

                    if (matchingPrefab != null)
                    {
                        prefabsToSpawn.Add((matchingPrefab, partyMember));
                    }
                    else
                    {
                        Debug.LogWarning($"[Bootstrap] Could not find prefab for character '{partyMember.character.displayName}'!");
                    }
                }
            }
            else
            {
                // Fallback: spawn all default editor prefabs
                foreach (var prefab in playerTeamPrefabs)
                {
                    if (prefab != null)
                    {
                        prefabsToSpawn.Add((prefab, null));
                    }
                }
            }

            // 2. Spawn player team (unified spawning loop)
            int nextPlayerRank = 1;
            foreach (var spawnData in prefabsToSpawn)
            {
                var prefab = spawnData.prefab;
                var partyInfo = spawnData.partyInfo;

                int charSize = 1;
                var prefabCC = prefab.GetComponent<CombatCharacter>();
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
                GameObject go = Instantiate(prefab, pos, Quaternion.identity);
                go.name = $"Player_{nextPlayerRank}_{prefab.name}";

                CombatCharacter cc = go.GetComponent<CombatCharacter>();
                if (cc == null)
                {
                    Debug.LogError($"[Bootstrap] Prefab '{prefab.name}' missing CombatCharacter!");
                    Destroy(go);
                    continue;
                }

                // Face right (player team faces right)
                go.transform.localScale = new Vector3(
                    Mathf.Abs(go.transform.localScale.x),
                    go.transform.localScale.y,
                    go.transform.localScale.z);

                cc.partyInfo = partyInfo;
                cc.InitializeForCombat(Team.Player, nextPlayerRank);
                _spawnedPlayerTeam.Add(cc);

                Debug.Log($"[Bootstrap] Spawned player: {cc.DisplayName} at rank {cc.rank} (size {charSize})");
                nextPlayerRank += charSize;
            }

            // Resolve enemy formation tier based on room progression
            Nevergreen.Data.EnemyEncounterTier tier = Nevergreen.Data.EnemyEncounterTier.Trivial;
            if (GameDatabase.Instance != null && GameDatabase.Instance.CombatConfig != null)
            {
                tier = GameDatabase.Instance.CombatConfig.GetEncounterTierForRoom(RunSessionManager.RoomProgression);
            }

            // Resolve enemy formation: use database if available, else fall back to Inspector list
            var activeEnemyPrefabs = enemyTeamPrefabs;
            var formation = RunSessionManager.GetNextRandomFormation(tier);
            if (formation != null && formation.enemyPrefabs != null && formation.enemyPrefabs.Count > 0)
            {
                activeEnemyPrefabs = formation.enemyPrefabs;
                Debug.Log($"[Bootstrap] Using formation: {formation.name}");
            }
            else
            {
                Debug.Log("[Bootstrap] No formation database configured. Using Inspector enemy prefabs.");
            }

            // Spawn enemy team (enemies can be multi-rank)
            int nextEnemyRank = 1;
            for (int i = 0; i < activeEnemyPrefabs.Count && nextEnemyRank <= 4; i++)
            {
                if (activeEnemyPrefabs[i] == null) continue;

                int charSize = 1;
                var prefabCC = activeEnemyPrefabs[i].GetComponent<CombatCharacter>();
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
                GameObject go = Instantiate(activeEnemyPrefabs[i], pos, Quaternion.identity);
                go.name = $"Enemy_{nextEnemyRank}_{activeEnemyPrefabs[i].name}";

                CombatCharacter cc = go.GetComponent<CombatCharacter>();
                if (cc == null)
                {
                    Debug.LogError($"[Bootstrap] Prefab '{activeEnemyPrefabs[i].name}' missing CombatCharacter!");
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

            // Subscribe RunSessionManager to battle events for victory room effects
            RunSessionManager.SubscribeToBattle(battleSystem);

            battleSystem.StartBattle(_spawnedPlayerTeam, _spawnedEnemyTeam);
        }
    }
}
