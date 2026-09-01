using UnityEngine;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Component attached to the visual panel used for room tooltips.
    /// Listens to global room tooltip events and toggles visibility.
    /// </summary>
    public class RoomTooltipDisplay : MonoBehaviour
    {
        [Tooltip("The visual container of the tooltip that will be enabled/disabled.")]
        [SerializeField] private GameObject visualPanel;

        [Tooltip("The text component displaying the room description.")]
        [SerializeField] private TextMeshProUGUI tooltipText;

        private void Awake()
        {
            if (visualPanel == null)
            {
                visualPanel = gameObject;
            }
        }

        private void OnEnable()
        {
            TooltipEvents.OnShowRoomTooltip += HandleShowTooltip;
            TooltipEvents.OnHideRoomTooltip += HandleHideTooltip;
            
            HandleHideTooltip();
        }

        private void OnDisable()
        {
            TooltipEvents.OnShowRoomTooltip -= HandleShowTooltip;
            TooltipEvents.OnHideRoomTooltip -= HandleHideTooltip;
        }

        private void HandleShowTooltip(RoomData room)
        {
            if (room == null) return;

            if (tooltipText != null)
            {
                tooltipText.text = room.description;
            }

            if (visualPanel != null)
            {
                visualPanel.SetActive(true);
            }
        }

        private void HandleHideTooltip()
        {
            if (visualPanel != null)
            {
                visualPanel.SetActive(false);
            }
        }
    }
}
