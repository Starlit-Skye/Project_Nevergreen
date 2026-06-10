using System;

namespace Nevergreen.Data
{
    /// <summary>
    /// Abstract base class for room effect execution strategies.
    /// Concrete implementations define what happens when a room effect is triggered.
    /// Serialized polymorphically via [SerializeReference] inside RoomData.
    /// </summary>
    [Serializable]
    public abstract class RoomEffectStrategy
    {
        /// <summary>
        /// Execute the room effect. Called by RoomData.ActivateEffect().
        /// </summary>
        public abstract void ExecuteRoomEffect();
    }
}
