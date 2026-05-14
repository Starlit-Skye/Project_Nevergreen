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
    public class SequenceBehavior : AIBehaviorNode
    {
        [Tooltip("Unique identifier for this sequence. Different SequenceBehaviors should have different IDs.")]
        public string sequenceId = "default";

        [Tooltip("The ordered list of skills to cycle through.")]
        public List<SkillData> skillSequence = new List<SkillData>();

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

            if (skillSequence == null || skillSequence.Count == 0) return false;
            if (targeting == null) return false;

            int startIndex = brain.History.GetSequenceIndex(sequenceId);
            int length = skillSequence.Count;

            // Try skills starting from the current index.
            // If skipOnFailure is true, try up to the full sequence length before giving up.
            int maxAttempts = skipOnFailure ? length : 1;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int index = (startIndex + attempt) % length;
                SkillData skill = skillSequence[index];

                if (skill == null) continue;

                // Check rank and usage constraints
                if (!brain.Self.CanUseSkillFromRank(skill) || !brain.Self.HasRemainingUses(skill))
                {
                    if (skipOnFailure) continue;
                    return false;
                }

                // Resolve targets
                if (!targeting.TryResolveTargets(brain, battle, skill, out List<CombatCharacter> targets))
                {
                    if (skipOnFailure) continue;
                    return false;
                }

                // Success — advance the sequence index past this skill
                // We advance by (attempt + 1) to account for any skipped entries
                for (int i = 0; i <= attempt; i++)
                {
                    brain.History.AdvanceSequenceIndex(sequenceId, length);
                }

                decision = AIDecision.UseSkill(skill, targets);
                return true;
            }

            return false;
        }
    }
}
