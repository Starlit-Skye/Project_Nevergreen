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
                    var controller = GetComponentInParent<PartyManagementPanelController>();
                    if (controller != null)
                    {
                        controller.ForceRefresh();
                    }
                }
            }
        }
    }
}
