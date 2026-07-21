using UnityEngine;
using UnityEngine.EventSystems;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    public class TrinketSlotDropHandler : MonoBehaviour, IDropHandler
    {
        public PartyMemberInfo TargetMember { get; set; }
        public int TargetSlotIndex { get; set; } = -1;

        public void OnDrop(PointerEventData eventData)
        {
            TrinketUIItem draggedItem = eventData.pointerDrag?.GetComponent<TrinketUIItem>();
            if (draggedItem == null) return;

            PartyMemberInfo ownerA = draggedItem.Owner;
            TrinketData x = draggedItem.TrinketData;

            if (TargetMember == null)
            {
                var controller = GetComponentInParent<PartyManagementPanelController>();
                // TargetMember can be dynamically resolved from controller if needed
            }

            if (TargetMember == null) return;

            // Handle dropping into an empty slot / container
            if (ownerA != TargetMember)
            {
                if (ownerA != null && x.cannotBeRemoved) return; // Cannot unequip if cursed

                if (ownerA != null)
                {
                    if (!ownerA.TryUnequipTrinket(x)) return;
                }

                if (TargetMember.TryEquipTrinket(x))
                {
                    SaveManager.SaveRun();
                    var controller = GetComponentInParent<PartyManagementPanelController>();
                    if (controller != null)
                    {
                        controller.ForceRefresh();
                    }
                }
                else
                {
                    // Rollback if equip failed
                    if (ownerA != null)
                    {
                        ownerA.TryEquipTrinket(x);
                    }
                }
            }
        }
    }
}
