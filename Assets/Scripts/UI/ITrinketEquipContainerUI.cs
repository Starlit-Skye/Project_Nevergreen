using UnityEngine;

namespace Nevergreen.UI
{
    /// <summary>
    /// Contract for UI containers that display trinket drop pools and party member equip slots.
    /// Enables drag-and-drop handlers to notify parent panels to refresh visually.
    /// </summary>
    public interface ITrinketEquipContainerUI
    {
        /// <summary>
        /// Triggers a full visual refresh of the trinket containers and equip slots.
        /// </summary>
        void RefreshTrinketUI();
    }
}
