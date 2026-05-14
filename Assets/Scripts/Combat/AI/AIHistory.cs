using System.Collections.Generic;
using Nevergreen.Data;

namespace Nevergreen.Combat.AI
{
    /// <summary>
    /// Tracks turn-by-turn history for an individual enemy's AI decision making.
    /// </summary>
    [System.Serializable]
    public class AIHistory
    {
        public int turnCount = 0;
        public SkillData lastSkillUsed;
        public int consecutiveSkillUses = 0;

        /// <summary>
        /// Per-sequence index tracking. Keyed by a unique sequence identifier
        /// so multiple SequenceBehaviors on the same AI profile track independently.
        /// </summary>
        private Dictionary<string, int> _sequenceIndices = new Dictionary<string, int>();

        public void RecordDecision(AIDecision decision)
        {
            turnCount++;
            
            if (decision.isPass)
            {
                lastSkillUsed = null;
                consecutiveSkillUses = 0;
                return;
            }

            if (decision.skill == lastSkillUsed)
            {
                consecutiveSkillUses++;
            }
            else
            {
                lastSkillUsed = decision.skill;
                consecutiveSkillUses = 1;
            }
        }

        /// <summary>
        /// Gets the current index for a specific sequence.
        /// Returns 0 if the sequence has never been advanced.
        /// </summary>
        public int GetSequenceIndex(string sequenceId)
        {
            if (string.IsNullOrEmpty(sequenceId)) return 0;
            return _sequenceIndices.TryGetValue(sequenceId, out int index) ? index : 0;
        }

        /// <summary>
        /// Advances the sequence index, wrapping around to 0 at the given length.
        /// </summary>
        public void AdvanceSequenceIndex(string sequenceId, int sequenceLength)
        {
            if (string.IsNullOrEmpty(sequenceId) || sequenceLength <= 0) return;

            int current = GetSequenceIndex(sequenceId);
            _sequenceIndices[sequenceId] = (current + 1) % sequenceLength;
        }
    }
}
