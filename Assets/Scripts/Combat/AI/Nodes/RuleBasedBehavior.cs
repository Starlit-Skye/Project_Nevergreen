using System;
using System.Collections.Generic;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Attributes;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// A flexible behavior that uses conditions to determine if a specific skill should be used,
    /// and a targeting strategy to pick the victims.
    /// </summary>
    [Serializable]
    public class RuleBasedBehavior : AIBehaviorNode
    {
        [Tooltip("The skill to use if all conditions are met.")]
        public SkillData skillToUse;

        [Tooltip("Targeting strategy to use for this skill.")]
        [SerializeReference]
        [SubclassSelector]
        public AITargetingNode targeting;

        [Tooltip("All conditions in this list must be met for the rule to trigger.")]
        [SerializeReference]
        [SubclassSelector]
        public List<AIConditionNode> conditions = new List<AIConditionNode>();

        public override bool TryGetDecision(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;

            if (skillToUse == null) return false;

            // 1. Check if the AI can even use this skill (rank + uses)
            if (!brain.Self.CanUseSkillFromRank(skillToUse) || !brain.Self.HasRemainingUses(skillToUse))
            {
                return false;
            }

            // 2. Check all conditions (pass skillToUse as context for history-aware conditions)
            foreach (var condition in conditions)
            {
                if (condition == null) continue;
                if (!condition.IsMet(brain, battle, skillToUse)) return false;
            }

            // 3. Resolve targets
            if (targeting == null || !targeting.TryResolveTargets(brain, battle, skillToUse, out List<CombatCharacter> targets))
            {
                return false;
            }

            // 4. Return decision
            decision = AIDecision.UseSkill(skillToUse, targets);
            return true;
        }
    }
}
