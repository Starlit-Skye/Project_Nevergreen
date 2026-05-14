using System;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Abstract base class for AI Conditions to support polymorphic serialization.
    /// </summary>
    [Serializable]
    public abstract class AIConditionNode : IAICondition
    {
        public abstract bool IsMet(AIBrain brain, BattleSystem battle);

        /// <summary>
        /// Overload that receives the skill being evaluated by the parent behavior.
        /// Override this in conditions that need to know which skill is being considered
        /// (e.g., RepetitionCondition). Defaults to calling the parameterless version.
        /// </summary>
        public virtual bool IsMet(AIBrain brain, BattleSystem battle, SkillData contextSkill)
        {
            return IsMet(brain, battle);
        }
    }
}
