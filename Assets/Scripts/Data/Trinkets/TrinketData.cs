using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    [CreateAssetMenu(fileName = "NewTrinketData", menuName = "Nevergreen/Data/Trinket Data")]
    public class TrinketData : ScriptableObject
    {
        [Tooltip("Unique identifier for this trinket.")]
        public string trinketId;

        [Tooltip("Display name shown in UI.")]
        public string displayName;

        [Tooltip("Description of the trinket's effects.")]
        [TextArea]
        public string description;

        [Tooltip("If true, this trinket cannot be unequipped once equipped.")]
        public bool cannotBeRemoved;

        [Tooltip("Modular strategies that define this trinket's mechanical effects.")]
        [SerializeReference]
        public List<TrinketEffectStrategy> effectStrategies = new List<TrinketEffectStrategy>();
    }
}
