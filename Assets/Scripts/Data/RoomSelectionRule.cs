using System;

namespace Nevergreen.Data
{
    /// <summary>
    /// Abstract base class for room selection rules.
    /// Concrete implementations define the probability weight of a room.
    /// Serialized polymorphically via [SerializeReference] inside RoomData.
    /// </summary>
    [Serializable]
    public abstract class RoomSelectionRule
    {
        /// <summary>
        /// Returns the weight (0 to infinity) this room should contribute 
        /// to the selection pool. 0 = never appears. Higher = more likely.
        /// </summary>
        public abstract float EvaluateWeight();
    }
}
