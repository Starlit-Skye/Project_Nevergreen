using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Fallback behavior that picks a random usable skill and random valid targets.
    /// Should always be the LAST behavior in an EnemyAIProfile's list to act as a safety net.
    /// Returns Pass if no skills or targets are available.
    /// </summary>
    [Serializable]
    public class RandomSkillBehavior : AIBehaviorNode
    {
        public override bool TryGetDecision(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;
            CombatCharacter self = brain.Self;

            // Filter to skills usable from the current rank with remaining uses
            var validSkills = self.equippedSkills
                .Where(s => self.CanUseSkillFromRank(s) && self.HasRemainingUses(s))
                .ToList();

            if (validSkills.Count == 0)
            {
                // No usable skills — signal pass
                decision = AIDecision.Pass;
                return true;
            }

            // Shuffle the valid skills so we try them in random order
            Shuffle(validSkills);

            foreach (var skill in validSkills)
            {
                List<CombatCharacter> targets = battle.GetValidTargets(self, skill);

                if (targets.Count == 0) continue;

                // Trim to maxTargets, picking randomly
                while (targets.Count > skill.maxTargets)
                {
                    targets.RemoveAt(UnityEngine.Random.Range(0, targets.Count));
                }

                decision = AIDecision.UseSkill(skill, targets);
                return true;
            }

            // All skills had zero valid targets — pass
            decision = AIDecision.Pass;
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
