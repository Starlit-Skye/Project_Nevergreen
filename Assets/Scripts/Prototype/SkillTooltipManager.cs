using UnityEngine;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// Manages the visual state and text of the skill hover tooltip.
    /// </summary>
    public class SkillTooltipManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("The parent panel GameObject to show/hide.")]
        public GameObject tooltipPanel;

        [Tooltip("TextMeshPro text field for the skill's name.")]
        public TextMeshProUGUI nameText;

        [Tooltip("TextMeshPro text field for the skill's description.")]
        public TextMeshProUGUI descriptionText;

        private void Awake()
        {
            HideTooltip();
        }

        /// <summary>
        /// Populates the tooltip text fields and makes the panel visible.
        /// </summary>
        public void ShowTooltip(SkillData skill)
        {
            if (skill == null) return;

            if (nameText != null)
            {
                nameText.text = skill.displayName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = skill.description;
            }

            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the tooltip panel.
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
    }
}
