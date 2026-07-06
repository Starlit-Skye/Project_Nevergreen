using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Boss controller for the God-Eye.
    /// Handles start-of-battle and periodic ultimate executions,
    /// and summon interception when the boss uses its summon skill.
    /// Attach to the same GameObject as CombatCharacter and AIBrain.
    /// </summary>
    public class GodEyeController : MonoBehaviour
    {
        [Header("Ultimate Auto-Action")]
        [Tooltip("Skill executed automatically at the start of battle and every N rounds.")]
        public SkillData ultimateSkill;

        [Tooltip("Interval (in rounds) to execute the ultimate skill again. E.g., 3 means it executes on rounds 1 (start), 4, 7, 10...")]
        public int ultimateRoundInterval = 3;

        [Header("Summon Interception")]
        [Tooltip("The summon skill (self-targeting signal). When the boss uses this skill, the controller spawns the ally.")]
        public SkillData summonSkill;

        [Tooltip("CharacterData for the Protector ally. Used to check if one is already in the team.")]
        public CharacterData protectorData;

        [Tooltip("Prefab to instantiate when summoning a Protector ally.")]
        public GameObject protectorAllyPrefab;

        [Tooltip("CharacterData for the Damage ally.")]
        public CharacterData damageData;

        [Tooltip("Prefab to instantiate when summoning a Damage ally.")]
        public GameObject damageAllyPrefab;

        private CombatCharacter _self;
        private BattleSystem _battleSystem;

        private void Awake()
        {
            _self = GetComponent<CombatCharacter>();
            
            _battleSystem = FindFirstObjectByType<BattleSystem>();
            if (_battleSystem == null)
            {
                Debug.LogError("[GodEyeController] BattleSystem not found in scene.");
                return;
            }

            _battleSystem.OnBattleStarted += HandleBattleStarted;
            _battleSystem.OnRoundStarted += HandleRoundStarted;
            _battleSystem.OnActionResolved += HandleActionResolved;
        }

        private void OnDestroy()
        {
            if (_battleSystem != null)
            {
                _battleSystem.OnBattleStarted -= HandleBattleStarted;
                _battleSystem.OnRoundStarted -= HandleRoundStarted;
                _battleSystem.OnActionResolved -= HandleActionResolved;
            }
        }

        // ==========================================
        // Auto Actions: Ultimate
        // ==========================================

        private void HandleBattleStarted()
        {
            if (!_self.IsAlive) return;
            
            Debug.Log("[GodEyeController] Battle Started: Executing Ultimate");
            ExecuteUltimate();
        }

        private void HandleRoundStarted(int roundNumber)
        {
            if (!_self.IsAlive) return;

            // Execute every 'ultimateRoundInterval' rounds (e.g. 4, 7, 10...)
            // Skip round 1 because it's handled by HandleBattleStarted
            if (roundNumber > 1 && (roundNumber - 1) % ultimateRoundInterval == 0)
            {
                Debug.Log($"[GodEyeController] Round {roundNumber}: Executing Periodic Ultimate");
                ExecuteUltimate();
            }
        }

        private void ExecuteUltimate()
        {
            if (ultimateSkill == null) return;

            var validPool = _battleSystem.GetValidTargets(_self, ultimateSkill);
            if (validPool.Count > 0)
            {
                var primaryTarget = validPool[0];
                var targets = _battleSystem.GetAOETargets(ultimateSkill, primaryTarget);
                _battleSystem.ExecuteSkill(_self, ultimateSkill, targets);
            }
            else
            {
                Debug.Log("[GodEyeController] Ultimate skipped: No valid targets found.");
            }
        }

        // ==========================================
        // Summon Interception
        // ==========================================

        private void HandleActionResolved(CombatCharacter user, SkillData skill, SkillContext context)
        {
            if (user != _self) return;
            if (skill != summonSkill) return;

            // Check if we already have a protector ally alive in the team
            bool hasProtector = _battleSystem.EnemyTeam.Any(c => 
                c.IsAlive && c.characterData.characterId == protectorData.characterId
            );

            GameObject prefabToSpawn = hasProtector ? damageAllyPrefab : protectorAllyPrefab;
            if (prefabToSpawn == null)
            {
                Debug.LogError("[GodEyeController] Missing prefab for summon interception!");
                return;
            }

            // Calculate spawn rank: place ally after the last enemy team member
            int spawnRank = 1;
            if (_battleSystem.EnemyTeam.Count > 0)
            {
                int maxOccupied = _battleSystem.EnemyTeam.Max(c =>
                {
                    var ranks = c.OccupiedRanks;
                    return ranks.Count > 0 ? ranks.Max() : c.rank;
                });
                spawnRank = maxOccupied + 1;
            }

            // Instantiate and configure the summoned ally
            float xPos = _battleSystem.GetXPositionForRank(Team.Enemy, spawnRank);
            Vector3 spawnPos = new Vector3(xPos, 0f, 0f);
            GameObject allyGO = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            // Face left (enemy team faces left)
            allyGO.transform.localScale = new Vector3(
                -Mathf.Abs(allyGO.transform.localScale.x),
                allyGO.transform.localScale.y,
                allyGO.transform.localScale.z);

            CombatCharacter allyCombat = allyGO.GetComponent<CombatCharacter>();
            if (allyCombat == null)
            {
                Debug.LogError("[GodEyeController] Summoned prefab is missing a CombatCharacter component.");
                if (Application.isPlaying) Destroy(allyGO);
                else DestroyImmediate(allyGO);
                return;
            }

            // properly initialize the combat character to set up stats, HP, skills, and AI brain
            allyCombat.InitializeForCombat(Team.Enemy, spawnRank);

            _battleSystem.RegisterSpawnedCharacter(allyCombat);

            string allyType = hasProtector ? "Damage" : "Protector";
            Debug.Log($"[GodEyeController] Summoned {allyType} ally '{allyCombat.DisplayName}' at rank {spawnRank}.");

            // Shift the boss back 1 rank after successfully spawning the ally
            _battleSystem.ExecuteMoveAndShift(_self, _self.rank + 1);
        }
    }
}
