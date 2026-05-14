using System;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Prevents a skill from being used more than N times consecutively.
    /// Add this condition to a RuleBasedBehavior to limit repetition.
    /// Example: Setting maxConsecutiveUses to 2 means the AI can use the skill
    /// twice in a row, but the third consecutive attempt will be blocked.
    /// </summary>
    [Serializable]
    public class RepetitionCondition : AIConditionNode
    {
        [Tooltip("Maximum number of times this skill can be used consecutively before the condition blocks it.")]
        [Min(1)]
        public int maxConsecutiveUses = 2;

        /// <summary>
        /// Non-contextual version. Without knowing which skill is being evaluated,
        /// this checks the total consecutive uses of whatever skill was last used.
        /// </summary>
        public override bool IsMet(AIBrain brain, BattleSystem battle)
        {
            // Without context, block if any skill has been repeated too many times
            return brain.History.consecutiveSkillUses < maxConsecutiveUses;
        }

        /// <summary>
        /// Contextual version. Checks whether the specific skill being evaluated
        /// has been used too many times in a row.
        /// </summary>
        public override bool IsMet(AIBrain brain, BattleSystem battle, SkillData contextSkill)
        {
            if (contextSkill == null) return IsMet(brain, battle);

            // If the last skill used was different, repetition is not a concern
            if (brain.History.lastSkillUsed != contextSkill) return true;

            // Block if the skill has hit the consecutive limit
            return brain.History.consecutiveSkillUses < maxConsecutiveUses;
        }
    }
}
