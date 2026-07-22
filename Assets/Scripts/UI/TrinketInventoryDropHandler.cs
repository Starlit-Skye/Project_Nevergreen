using UnityEngine;
using UnityEngine.EventSystems;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    public class TrinketInventoryDropHandler : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            TrinketUIItem draggedItem = eventData.pointerDrag?.GetComponent<TrinketUIItem>();
            if (draggedItem != null && draggedItem.Owner != null)
            {
                TrinketData data = draggedItem.TrinketData;
                if (draggedItem.Owner.TryUnequipTrinket(data))
                {
                    SaveManager.SaveRun();
                    
                    // Reparent to the inventory container so it doesn't get destroyed by ForceRefresh
                    draggedItem.transform.SetParent(this.transform);
                    draggedItem.Initialize(data, null, -1);
                    
                    var controller = GetComponentInParent<PartyManagementPanelController>();
                    if (controller == null) controller = Object.FindAnyObjectByType<PartyManagementPanelController>();
                    
                    if (controller != null)
                    {
                        controller.ForceRefresh();
                    }
                }
            }
        }
    }
}
