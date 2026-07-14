using UnityEngine;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Component attached to the visual panel used for tooltips.
    /// Listens to global trait tooltip events and toggles visibility.
    /// </summary>
    public class TraitTooltipDisplay : MonoBehaviour
    {
        [Tooltip("The visual container of the tooltip that will be enabled/disabled.")]
        [SerializeField] private GameObject visualPanel;

        [Tooltip("The text component displaying the trait info.")]
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
            TooltipEvents.OnShowTraitTooltip += HandleShowTooltip;
            TooltipEvents.OnHideTraitTooltip += HandleHideTooltip;
            
            HandleHideTooltip();
        }

        private void OnDisable()
        {
            TooltipEvents.OnShowTraitTooltip -= HandleShowTooltip;
            TooltipEvents.OnHideTraitTooltip -= HandleHideTooltip;
        }

        private void HandleShowTooltip(TraitData trait)
        {
            if (trait == null) return;

            if (tooltipText != null)
            {
                var lines = new System.Collections.Generic.List<string>();
                if (trait.effectStrategies != null)
                {
                    foreach (var strategy in trait.effectStrategies)
                    {
                        if (strategy != null)
                        {
                            string desc = strategy.GetTooltipDescription(trait.traitType);
                            if (!string.IsNullOrEmpty(desc))
                            {
                                lines.Add(desc);
                            }
                        }
                    }
                }
                
                if (lines.Count > 0)
                {
                    tooltipText.text = string.Join("\n", lines);
                }
                else
                {
                    tooltipText.text = trait.displayName;
                }
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
