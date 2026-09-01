using UnityEngine;
using UnityEngine.EventSystems;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Attached to Room Choice UI buttons.
    /// Detects mouse hover and fires global room tooltip events.
    /// </summary>
    public class RoomTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RoomData _roomData;
        private bool _isHovered = false;

        public void SetRoom(RoomData roomData)
        {
            _roomData = roomData;
            
            // If we're currently hovering and the room changes, update the tooltip immediately
            if (_isHovered)
            {
                if (_roomData != null && !string.IsNullOrEmpty(_roomData.description))
                {
                    TooltipEvents.ShowRoomTooltip(_roomData);
                }
                else
                {
                    TooltipEvents.HideRoomTooltip();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (_roomData != null && !string.IsNullOrEmpty(_roomData.description))
            {
                TooltipEvents.ShowRoomTooltip(_roomData);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            TooltipEvents.HideRoomTooltip();
        }

        private void OnDisable()
        {
            if (_isHovered)
            {
                _isHovered = false;
                TooltipEvents.HideRoomTooltip();
            }
        }
    }
}
