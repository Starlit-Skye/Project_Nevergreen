using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat.AI;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Boss controller for the RoseKnight.
    /// Handles start-of-round telegraph VFX, end-of-round strike execution,
    /// and summon interception when the boss uses its summon skill during normal turns.
    /// Attach to the same GameObject as CombatCharacter and AIBrain.
    /// </summary>
    public class RoseKnightController : MonoBehaviour
    {
        [Header("Round Skills")]
        [Tooltip("Skill executed at start of round to announce the upcoming strike. Can be a null-effect skill.")]
        public SkillData telegraphSkill;

        [Tooltip("Skill executed at end of round to damage players in marked ranks.")]
        public SkillData strikeSkill;

        [Header("Summon")]
        [Tooltip("The summon skill (self-targeting signal). When the boss uses this skill, the controller spawns the ally.")]
        public SkillData summonSkill;

        [Tooltip("Prefab to instantiate when summoning an ally.")]
        public GameObject allyPrefab;

        [Header("Telegraph VFX")]
        [Tooltip("VFX prefab spawned at each marked player rank position during round start.")]
        public GameObject telegraphVFXPrefab;

        // --- Runtime State ---
        private List<int> _markedRanks = new List<int>();
        private List<GameObject> _activeVFX = new List<GameObject>();
        private CombatCharacter _self;
        private BattleSystem _battleSystem;

        private void Awake()
        {
            _self = GetComponent<CombatCharacter>();
            
            _battleSystem = FindFirstObjectByType<BattleSystem>();
            if (_battleSystem == null)
            {
                Debug.LogError("[RoseKnightController] BattleSystem not found in scene.");
                return;
            }

            _battleSystem.OnRoundStarted += HandleRoundStarted;
            _battleSystem.OnRoundEnded += HandleRoundEnded;
            _battleSystem.OnActionResolved += HandleActionResolved;
        }

        private void OnDestroy()
        {
            if (_battleSystem != null)
            {
                _battleSystem.OnRoundStarted -= HandleRoundStarted;
                _battleSystem.OnRoundEnded -= HandleRoundEnded;
                _battleSystem.OnActionResolved -= HandleActionResolved;
            }

            ClearVFX();
        }

        // ==========================================
        // Round Start: Telegraph
        // ==========================================

        private void HandleRoundStarted(int roundNumber)
        {
            if (!_self.IsAlive) return;

            ClearVFX();
            _markedRanks.Clear();

            // Gather all ranks currently occupied by player team candidates (avoiding Piles if non-pile players exist)
            var candidates = _battleSystem.PlayerTeam.FilterPilesIfAlternativesExist();

            var occupiedRanks = candidates
                .Where(c => c.IsAlive || c.IsPile)
                .SelectMany(c => c.OccupiedRanks)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            if (occupiedRanks.Count == 0) return;

            // Determine how many ranks to mark: 1 or 2 adjacent (50/50)
            bool markTwo = Random.Range(0, 2) == 1 && occupiedRanks.Count > 1;

            if (markTwo)
            {
                // Try to find adjacent pairs of occupied ranks
                var adjacentPairs = new List<(int, int)>();
                for (int i = 0; i < occupiedRanks.Count - 1; i++)
                {
                    if (occupiedRanks[i + 1] == occupiedRanks[i] + 1)
                    {
                        adjacentPairs.Add((occupiedRanks[i], occupiedRanks[i + 1]));
                    }
                }

                if (adjacentPairs.Count > 0)
                {
                    var pair = adjacentPairs[Random.Range(0, adjacentPairs.Count)];
                    _markedRanks.Add(pair.Item1);
                    _markedRanks.Add(pair.Item2);
                }
                else
                {
                    // Fallback to 1 rank if no adjacent occupied pairs exist
                    _markedRanks.Add(occupiedRanks[Random.Range(0, occupiedRanks.Count)]);
                }
            }
            else
            {
                _markedRanks.Add(occupiedRanks[Random.Range(0, occupiedRanks.Count)]);
            }

            Debug.Log($"[RoseKnightController] Round {roundNumber}: Telegraphing ranks [{string.Join(", ", _markedRanks)}]");

            // Execute the telegraph skill to announce the action (UI/animation), targeting self to ensure it plays
            if (telegraphSkill != null)
            {
                _battleSystem.QueueEnemySkill(_self, telegraphSkill, new List<CombatCharacter> { _self });
            }
            else
            {
                // If there's no telegraph skill to play, spawn VFX immediately
                SpawnTelegraphVFX();
            }
        }

        private void SpawnTelegraphVFX()
        {
            if (telegraphVFXPrefab == null || _battleSystem == null) return;

            foreach (int rank in _markedRanks)
            {
                float xPos = _battleSystem.GetXPositionForRank(Team.Player, rank);
                Vector3 vfxPos = new Vector3(xPos, 0f, 0f);

                GameObject vfx = Instantiate(telegraphVFXPrefab, vfxPos, Quaternion.identity);
                _activeVFX.Add(vfx);
            }
        }

        private void ClearVFX()
        {
            foreach (var vfx in _activeVFX)
            {
                if (vfx != null)
                {
                    if (Application.isPlaying) Destroy(vfx);
                    else DestroyImmediate(vfx);
                }
            }
            _activeVFX.Clear();
        }

        // ==========================================
        // Round End: Strike
        // ==========================================

        private void HandleRoundEnded(int roundNumber)
        {
            if (!_self.IsAlive) return;
            if (_markedRanks.Count == 0) return;

            // Find all alive player characters currently occupying the marked ranks
            var targets = _battleSystem.PlayerTeam
                .Where(c => c.IsAlive && c.OccupiedRanks.Intersect(_markedRanks).Any())
                .ToList();

            if (targets.Count > 0 && strikeSkill != null)
            {
                Debug.Log($"[RoseKnightController] Round {roundNumber}: Striking {targets.Count} target(s) at ranks [{string.Join(", ", _markedRanks)}]");
                _battleSystem.QueueEnemySkill(_self, strikeSkill, targets);
            }
            else
            {
                Debug.Log($"[RoseKnightController] Round {roundNumber}: No valid targets at marked ranks. Strike skipped.");
            }

            if (_battleSystem.animationQueue != null)
            {
                _battleSystem.animationQueue.Enqueue(new ActionStep("Clear Telegraph VFX", () => 
                {
                    ClearVFX();
                    _markedRanks.Clear();
                }));
            }
            else
            {
                ClearVFX();
                _markedRanks.Clear();
            }
        }

        // ==========================================
        // Summon Interception
        // ==========================================

        private void HandleActionResolved(CombatCharacter user, SkillData skill, SkillContext context)
        {
            if (user != _self) return;
            // Option 1: VFX appears right when the skill animation starts (synchronous)
            if (skill == telegraphSkill && telegraphSkill != null)
            {
                SpawnTelegraphVFX();
                return;
            }

            // Option 2: VFX appears after the skill animation completes (queued ActionStep)
            /*
            if (skill == telegraphSkill && telegraphSkill != null)
            {
                if (_battleSystem.animationQueue != null)
                {
                    _battleSystem.animationQueue.Enqueue(new ActionStep("Spawn Telegraph VFX", () => SpawnTelegraphVFX()));
                }
                else
                {
                    SpawnTelegraphVFX();
                }
                return;
            }
            */

            if (skill != summonSkill) return;
            if (allyPrefab == null) return;

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
            GameObject allyGO = Instantiate(allyPrefab, spawnPos, Quaternion.identity);

            // Face left (enemy team faces left)
            allyGO.transform.localScale = new Vector3(
                -Mathf.Abs(allyGO.transform.localScale.x),
                allyGO.transform.localScale.y,
                allyGO.transform.localScale.z);

            CombatCharacter allyCombat = allyGO.GetComponent<CombatCharacter>();
            if (allyCombat == null)
            {
                Debug.LogError("[RoseKnightController] allyPrefab is missing a CombatCharacter component.");
                if (Application.isPlaying) Destroy(allyGO);
                else DestroyImmediate(allyGO);
                return;
            }

            // properly initialize the combat character to set up stats, HP, skills, and AI brain
            allyCombat.InitializeForCombat(Team.Enemy, spawnRank);

            _battleSystem.RegisterSpawnedCharacter(allyCombat);

            Debug.Log($"[RoseKnightController] Summoned {allyCombat.DisplayName} at rank {spawnRank}.");
        }

        // ==========================================
        // Public Accessors (for testing)
        // ==========================================

        /// <summary>
        /// The player ranks currently marked for a strike at end of round.
        /// </summary>
        public IReadOnlyList<int> MarkedRanks => _markedRanks;
    }
}
