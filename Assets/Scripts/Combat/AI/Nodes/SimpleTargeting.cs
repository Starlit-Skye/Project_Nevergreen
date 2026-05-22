using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Selects targets from the valid target pool using a configurable strategy.
    /// </summary>
    [Serializable]
    public class SimpleTargeting : AITargetingNode
    {
        public enum Strategy
        {
            /// <summary>Pick targets at random from the valid pool.</summary>
            Random,
            /// <summary>Prioritize the target with the lowest current HP.</summary>
            LowestHP,
            /// <summary>Prioritize the target with the highest current HP.</summary>
            HighestHP,
            /// <summary>Prioritize the target in the frontmost rank (lowest rank number).</summary>
            FrontRank,
            /// <summary>Prioritize the target in the backmost rank (highest rank number).</summary>
            BackRank
        }

        [Tooltip("The strategy used to pick targets.")]
        public Strategy strategy = Strategy.Random;

        public override bool TryResolveTargets(AIBrain brain, BattleSystem battle, SkillData skill, out List<CombatCharacter> targets)
        {
            targets = null;

            List<CombatCharacter> pool = battle.GetValidTargets(brain.Self, skill);
            if (pool.Count == 0) return false;

            switch (strategy)
            {
                case Strategy.Random:
                    Shuffle(pool);
                    break;

                case Strategy.LowestHP:
                    pool.Sort((a, b) => a.currentHP.CompareTo(b.currentHP));
                    break;

                case Strategy.HighestHP:
                    pool.Sort((a, b) => b.currentHP.CompareTo(a.currentHP));
                    break;

                case Strategy.FrontRank:
                    pool.Sort((a, b) => a.rank.CompareTo(b.rank));
                    break;

                case Strategy.BackRank:
                    pool.Sort((a, b) => b.rank.CompareTo(a.rank));
                    break;
            }

            // Select the strategy's top choice as the primary target
            CombatCharacter primaryTarget = pool[0];

            // Resolve the linear propagation targets
            targets = battle.GetAOETargets(skill, primaryTarget);
            return true;
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
