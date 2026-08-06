using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Manages combat formation logic, character positioning, and rank compaction.
    /// </summary>
    public class FormationManager
    {
        public float playerBaseX = -3f;
        public float playerSpacingX = -2f;
        public float enemyBaseX = 3f;
        public float enemySpacingX = 2f;

        private AnimationQueueProcessor _animationQueue;

        public void Initialize(AnimationQueueProcessor animationQueue)
        {
            _animationQueue = animationQueue;
        }

        public void ExecuteMoveAndShift(CombatCharacter mover, int targetRank, List<CombatCharacter> team)
        {
            if (mover == null || !team.Contains(mover)) return; // Guard against dead/removed characters shifting formation

            // Calculate max usable rank accounting for all character sizes
            int totalSlotsUsed = 0;
            foreach (var c in team)
            {
                int s = (c.characterData != null) ? c.characterData.size : 1;
                totalSlotsUsed += s;
            }
            int maxAnchorRank = Mathf.Max(1, totalSlotsUsed - ((mover.characterData != null) ? mover.characterData.size : 1) + 1);
            targetRank = Mathf.Clamp(targetRank, 1, maxAnchorRank);

            int startRank = mover.rank;
            if (startRank == targetRank) return;

            // 1. Build ordered list by current rank (excluding mover)
            var ordered = team.Where(c => c != mover).OrderBy(c => c.rank).ToList();

            // 2. Find insertion index: where the mover's target rank fits relative to existing anchors
            int insertIndex = 0;
            bool movingForward = targetRank < startRank;

            if (movingForward)
            {
                // Moving forward: insert before characters at or after target rank
                for (int i = 0; i < ordered.Count; i++)
                {
                    if (ordered[i].rank >= targetRank) { insertIndex = i; break; }
                    insertIndex = i + 1;
                }
            }
            else
            {
                // Moving backward: insert after characters before target rank
                insertIndex = ordered.Count; // default: end
                for (int i = 0; i < ordered.Count; i++)
                {
                    if (ordered[i].rank >= targetRank)
                    {
                        insertIndex = i + 1;
                        break;
                    }
                }
            }

            insertIndex = Mathf.Clamp(insertIndex, 0, ordered.Count);
            ordered.Insert(insertIndex, mover);

            // 3. Reassign anchor ranks via compaction
            ParallelStep moveParallel = new ParallelStep($"{mover.DisplayName} MoveAndShift");
            float moveDuration = 0.5f;

            int nextRank = 1;
            foreach (var character in ordered)
            {
                int charSize = (character.characterData != null) ? character.characterData.size : 1;
                int oldRank = character.rank;
                character.rank = nextRank;

                if (oldRank != nextRank)
                {
                    float targetX = GetXPositionForCharacter(character);
                    var tween = ShortcutExtensions.DOMoveX(character.transform, targetX, moveDuration);
                    moveParallel.AddStep(new DOTweenStep(
                        character == mover ? $"Move_{character.DisplayName}" : $"Shift_{character.DisplayName}",
                        tween, moveDuration));
                }

                nextRank += charSize;
            }

            if (_animationQueue != null)
            {
                _animationQueue.Enqueue(moveParallel);
            }
            Debug.Log($"[FormationManager] {mover.DisplayName} moved to rank {mover.rank}. Formation recompacted.");
        }

        public void CompactFormation(List<CombatCharacter> team)
        {
            if (team.Count == 0) return;

            // Sort by current rank to preserve relative ordering
            var sorted = team.OrderBy(c => c.rank).ToList();

            ParallelStep shiftParallel = new ParallelStep("Formation Compact");
            float shiftDuration = 0.4f;
            bool anyShifted = false;

            int nextRank = 1;
            foreach (var character in sorted)
            {
                int oldRank = character.rank;
                int charSize = (character.characterData != null) ? character.characterData.size : 1;

                if (character.rank != nextRank)
                {
                    character.rank = nextRank;
                    float targetX = GetXPositionForCharacter(character);

                    var tween = ShortcutExtensions.DOMoveX(character.transform, targetX, shiftDuration);
                    shiftParallel.AddStep(new DOTweenStep($"Shift_{character.DisplayName}", tween, shiftDuration));
                    anyShifted = true;
                }

                nextRank += charSize;
            }

            if (anyShifted && _animationQueue != null)
            {
                _animationQueue.Enqueue(shiftParallel);
            }
        }

        public float GetXPositionForRank(Team team, int rank)
        {
            if (team == Team.Player)
            {
                return playerBaseX + (rank - 1) * playerSpacingX;
            }
            else
            {
                return enemyBaseX + (rank - 1) * enemySpacingX;
            }
        }

        public float GetXPositionForCharacter(CombatCharacter character)
        {
            var occupiedRanks = character.OccupiedRanks;
            if (occupiedRanks.Count <= 1)
            {
                return GetXPositionForRank(character.team, character.rank);
            }

            float sum = 0f;
            foreach (int r in occupiedRanks)
            {
                sum += GetXPositionForRank(character.team, r);
            }
            return sum / occupiedRanks.Count;
        }
    }
}
