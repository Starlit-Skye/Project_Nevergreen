using System.Collections.Generic;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI
{
    /// <summary>
    /// Represents the result of an AI turn evaluation.
    /// </summary>
    public struct AIDecision
    {
        public bool isPass;
        public SkillData skill;
        public List<CombatCharacter> targets;

        public static AIDecision Pass => new AIDecision { isPass = true };
        
        public static AIDecision UseSkill(SkillData skill, List<CombatCharacter> targets)
        {
            return new AIDecision
            {
                isPass = false,
                skill = skill,
                targets = targets
            };
        }
    }
}
