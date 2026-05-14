using System.Collections.Generic;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI
{
    /// <summary>
    /// Defines logic for selecting targets for a specific skill.
    /// </summary>
    public interface IAITargeting
    {
        /// <summary>
        /// Resolves a list of targets for the given skill.
        /// </summary>
        /// <param name="brain">The AI Brain instance.</param>
        /// <param name="battle">The current battle state.</param>
        /// <param name="skill">The skill being targeted.</param>
        /// <param name="targets">The resulting list of targets.</param>
        /// <returns>True if targets were successfully resolved, false otherwise.</returns>
        bool TryResolveTargets(AIBrain brain, BattleSystem battle, SkillData skill, out List<CombatCharacter> targets);
    }
}
