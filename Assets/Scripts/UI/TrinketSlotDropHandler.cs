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

                // TryEquipTrinket will automatically put it in an empty slot or append
                if (TargetMember.TryEquipTrinket(x))
                {
                    // If we have a specific target slot, we want to try to place it exactly there.
                    // TryEquipTrinket might have placed it in the first available slot.
                    // We can reorder it to the TargetSlotIndex.
                    int currentIndex = TargetMember.equippedTrinkets.IndexOf(x);
                    if (currentIndex != -1 && TargetSlotIndex != -1 && currentIndex != TargetSlotIndex)
                    {
                        // Pad with nulls if necessary
                        while (TargetMember.equippedTrinkets.Count <= TargetSlotIndex)
                        {
                            TargetMember.equippedTrinkets.Add(null);
                        }
                        
                        // Swap
                        var temp = TargetMember.equippedTrinkets[TargetSlotIndex];
                        TargetMember.equippedTrinkets[TargetSlotIndex] = x;
                        TargetMember.equippedTrinkets[currentIndex] = temp;
                    }

                    SaveManager.SaveRun();
                    var controller = GetComponentInParent<PartyManagementPanelController>();
                    if (controller != null)
                    {
                        controller.ForceRefresh();
                    }
                    Destroy(draggedItem.gameObject);
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
            else
            {
                // Dragging to an empty slot on the SAME character
                int currentIndex = ownerA.equippedTrinkets.IndexOf(x);
                if (currentIndex != -1 && TargetSlotIndex != -1 && currentIndex != TargetSlotIndex)
                {
                    // Pad with nulls if necessary
                    while (ownerA.equippedTrinkets.Count <= TargetSlotIndex)
                    {
                        ownerA.equippedTrinkets.Add(null);
                    }
                    
                    // Move the item to the new slot
                    ownerA.equippedTrinkets[currentIndex] = null;
                    ownerA.equippedTrinkets[TargetSlotIndex] = x;
                    
                    SaveManager.SaveRun();
                    var controller = GetComponentInParent<PartyManagementPanelController>();
                    if (controller != null)
                    {
                        controller.ForceRefresh();
                    }
                    Destroy(draggedItem.gameObject);
                }
            }
        }
    }
}
