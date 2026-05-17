using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Combat.AI
{
    /// <summary>
    /// Evaluates a condition against the current state of the battle or the AI history.
    /// </summary>
    public interface IAICondition
    {
        /// <summary>
        /// Checks if the condition is met.
        /// </summary>
        bool IsMet(AIBrain brain, BattleSystem battle);

        /// <summary>
        /// Checks if the condition is met, with additional context about the skill being evaluated.
        /// Defaults to calling the parameterless overload for conditions that don't need skill context.
        /// </summary>
        bool IsMet(AIBrain brain, BattleSystem battle, SkillData contextSkill)
        {
            return IsMet(brain, battle);
        }
    }
}
