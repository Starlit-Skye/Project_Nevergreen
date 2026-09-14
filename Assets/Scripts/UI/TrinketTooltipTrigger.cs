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
        private TrinketUIItem _trinketUIItem;

        private void Start()
        {
            _trinketUIItem = GetComponent<TrinketUIItem>();
            if (_trinketUIItem != null && _trinketUIItem.TrinketData != null)
            {
                _trinket = _trinketUIItem.TrinketData;
            }
        }

        public void SetTrinket(TrinketData trinket)
        {
            _trinket = trinket;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            TrinketData trinketToShow = _trinket;
            if (trinketToShow == null && _trinketUIItem != null)
            {
                trinketToShow = _trinketUIItem.TrinketData;
            }

            if (trinketToShow != null)
            {
                TooltipEvents.ShowTrinketTooltip(trinketToShow);
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
