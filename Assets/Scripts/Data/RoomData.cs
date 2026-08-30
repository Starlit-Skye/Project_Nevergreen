using UnityEngine;
using Nevergreen.Attributes;

namespace Nevergreen.Data
{
    /// <summary>
    /// ScriptableObject that holds room data and a reference to the room effect strategy.
    /// Separates room metadata from execution logic.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRoomData", menuName = "Nevergreen/Data/Room Data")]
    public class RoomData : ScriptableObject
    {
        [Tooltip("Unique identifier for this room data.")]
        public string roomId;

        [Tooltip("Display name for this room type.")]
        public string roomName;

        [Tooltip("Description shown to the player when selecting this room.")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("When this room's effect should be activated.")]
        public RoomActivationType activationType = RoomActivationType.OnRoomLoaded;

        [Header("Selection")]
        [Tooltip("Rule that determines how likely this room is to appear as a choice. If null, the room uses a default weight of 1.")]
        [SerializeReference]
        [SubclassSelector]
        public RoomSelectionRule selectionRule;

        [Header("Strategy")]
        [Tooltip("The strategy that defines how this room's effect is executed.")]
        [SerializeReference]
        [SubclassSelector]
        public RoomEffectStrategy strategy;

        /// <summary>
        /// Activates the room's effect strategy, if one is assigned.
        /// </summary>
        public void ActivateEffect()
        {
            if (strategy != null)
            {
                strategy.ExecuteRoomEffect();
            }
            else
            {
                Debug.LogWarning($"[RoomData] Room '{roomName}' has no strategy assigned.");
            }
        }
    }
}
