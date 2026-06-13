using UnityEngine;
using TMPro;

namespace Nevergreen.UI
{
    /// <summary>
    /// Attached to the actual Tooltip UI GameObject in the scene.
    /// Listens for global TooltipEvents to show/hide itself.
    /// </summary>
    public class SkillTooltipDisplay : MonoBehaviour
    {
        [Tooltip("The text component to display the skill description.")]
        [SerializeField] private TextMeshProUGUI tooltipText;

        [Tooltip("The parent panel to show/hide. Defaults to this GameObject if not set.")]
        [SerializeField] private GameObject tooltipPanel;

        private void Awake()
        {
            if (tooltipPanel == null)
            {
                tooltipPanel = gameObject;
            }
        }

        private void OnEnable()
        {
            TooltipEvents.OnShowTooltip += HandleShowTooltip;
            TooltipEvents.OnHideTooltip += HandleHideTooltip;

            // Ensure tooltip is hidden when the display is enabled
            HandleHideTooltip();
        }

        private void OnDisable()
        {
            TooltipEvents.OnShowTooltip -= HandleShowTooltip;
            TooltipEvents.OnHideTooltip -= HandleHideTooltip;
        }

        private void HandleShowTooltip(string description)
        {
            if (tooltipText != null)
            {
                tooltipText.text = description;
            }
            
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(true);
            }
        }

        private void HandleHideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
    }
}
