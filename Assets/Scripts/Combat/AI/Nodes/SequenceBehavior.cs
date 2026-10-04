using System;
using System.Collections.Generic;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Attributes;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Cycles through a fixed sequence of skills in order (A → B → C → A → ...).
    /// Each time this behavior successfully produces a decision, the index advances.
    /// The sequence state is tracked per-brain instance via AIHistory, so different
    /// enemies using the same AI profile maintain independent positions.
    /// </summary>
    [Serializable]
    public class SequenceStep
    {
        [Tooltip("Skill used if there is no condition, or when the condition is TRUE.")]
        public SkillData skill;

        [Tooltip("Optional. Leave empty for an unconditional step.")]
        [SerializeReference]
        [SubclassSelector]
        public AIConditionNode condition;

        [Tooltip("Skill used when the condition is FALSE. Leave empty to skip this step instead.")]
        public SkillData elseSkill;
    }

    [Serializable]
    public class SequenceBehavior : AIBehaviorNode
    {
        [Tooltip("Unique identifier for this sequence. Different SequenceBehaviors should have different IDs.")]
        public string sequenceId = "default";

        [Tooltip("Structured sequence steps with optional conditional branching.")]
        public List<SequenceStep> steps = new List<SequenceStep>();

        [Tooltip("Targeting strategy used for all skills in this sequence.")]
        [SerializeReference]
        [SubclassSelector]
        public AITargetingNode targeting;

        [Tooltip("If a skill in the sequence can't be used (wrong rank, no targets, no uses), " +
                 "skip to the next skill in the sequence instead of failing the entire behavior.")]
        public bool skipOnFailure = true;

        public override bool TryGetDecision(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;

            if (steps == null || steps.Count == 0) return false;
            if (targeting == null) return false;

            int startIndex = brain.History.GetSequenceIndex(sequenceId);
            int length = steps.Count;

            // Try skills starting from the current index.
            // We can check up to 'length' steps.
            for (int attempt = 0; attempt < length; attempt++)
            {
                int index = (startIndex + attempt) % length;
                SequenceStep step = steps[index];

                if (step == null) continue;

                SkillData chosen = null;

                if (step.condition == null)
                {
                    chosen = step.skill;
                }
                else if (step.condition.IsMet(brain, battle, step.skill))
                {
                    chosen = step.skill;
                }
                else
                {
                    chosen = step.elseSkill;
                }

                if (chosen == null) continue;

                // Check rank and usage constraints
                if (!brain.Self.CanUseSkillFromRank(chosen) || !brain.Self.HasRemainingUses(chosen))
                {
                    if (skipOnFailure) continue;
                    return false;
                }

                // Resolve targets
                if (!targeting.TryResolveTargets(brain, battle, chosen, out List<CombatCharacter> targets))
                {
                    if (skipOnFailure) continue;
                    return false;
                }

                // Success — advance the sequence index past this step
                // We advance by (attempt + 1) to account for any skipped entries
                for (int i = 0; i <= attempt; i++)
                {
                    brain.History.AdvanceSequenceIndex(sequenceId, length);
                }

                decision = AIDecision.UseSkill(chosen, targets);
                return true;
            }

            return false;
        }
    }
}
