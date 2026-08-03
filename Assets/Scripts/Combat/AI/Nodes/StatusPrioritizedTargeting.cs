using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Targets characters based on their active status effects.
    /// Can either strictly require the status, or prioritize characters with the status.
    /// </summary>
    [Serializable]
    public class StatusPrioritizedTargeting : AITargetingNode
    {
        [Tooltip("The status effect type to look for.")]
        public StatusType statusType = StatusType.Mark;

        [Tooltip("If true, only characters with this status effect can be targeted. If false, characters with the status are prioritized, but we fall back to other valid targets if none exist.")]
        public bool strict = true;

        [Tooltip("Strategy used to sort the targets (either within the status-matching group, or for fallback).")]
        public SimpleTargeting.Strategy sortingStrategy = SimpleTargeting.Strategy.Random;

        public override bool TryResolveTargets(AIBrain brain, BattleSystem battle, SkillData skill, out List<CombatCharacter> targets)
        {
            targets = null;

            List<CombatCharacter> pool = battle.GetValidTargets(brain.Self, skill);
            pool = pool.FilterPilesIfAlternativesExist();
            if (pool.Count == 0) return false;

            // Separate pool into those with the status and those without
            List<CombatCharacter> matching = pool.Where(c => c.statusEffects.Any(s => s.type == statusType && !s.IsExpired)).ToList();
            List<CombatCharacter> nonMatching = pool.Where(c => !c.statusEffects.Any(s => s.type == statusType && !s.IsExpired)).ToList();

            List<CombatCharacter> finalPool;
            if (matching.Count > 0)
            {
                SortPool(matching, sortingStrategy);
                finalPool = matching;
            }
            else
            {
                if (strict)
                {
                    return false;
                }
                SortPool(nonMatching, sortingStrategy);
                finalPool = nonMatching;
            }

            if (finalPool.Count == 0) return false;

            CombatCharacter primaryTarget = finalPool[0];
            targets = battle.GetAOETargets(skill, primaryTarget);
            return true;
        }

        private void SortPool(List<CombatCharacter> pool, SimpleTargeting.Strategy strategy)
        {
            switch (strategy)
            {
                case SimpleTargeting.Strategy.Random:
                    Shuffle(pool);
                    break;

                case SimpleTargeting.Strategy.LowestHP:
                    pool.Sort((a, b) => a.currentHP.CompareTo(b.currentHP));
                    break;

                case SimpleTargeting.Strategy.HighestHP:
                    pool.Sort((a, b) => b.currentHP.CompareTo(a.currentHP));
                    break;

                case SimpleTargeting.Strategy.FrontRank:
                    pool.Sort((a, b) => a.rank.CompareTo(b.rank));
                    break;

                case SimpleTargeting.Strategy.BackRank:
                    pool.Sort((a, b) => b.rank.CompareTo(a.rank));
                    break;
            }
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
