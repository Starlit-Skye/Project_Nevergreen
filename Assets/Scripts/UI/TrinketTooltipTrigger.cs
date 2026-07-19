using UnityEngine;
using UnityEngine.EventSystems;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Attached to an individual Trinket UI item prefab.
    /// Detects mouse hover and fires global trinket tooltip events.
    /// </summary>
    public class TrinketTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TrinketData _trinket;

        public void SetTrinket(TrinketData trinket)
        {
            _trinket = trinket;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_trinket != null)
            {
                TooltipEvents.ShowTrinketTooltip(_trinket);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipEvents.HideTrinketTooltip();
        }

        private void OnDisable()
        {
            TooltipEvents.HideTrinketTooltip();
        }
    }
}
