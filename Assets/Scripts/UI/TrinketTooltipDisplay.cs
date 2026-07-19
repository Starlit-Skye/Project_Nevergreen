using UnityEngine;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Component attached to the visual panel used for tooltips.
    /// Listens to global trinket tooltip events and toggles visibility.
    /// </summary>
    public class TrinketTooltipDisplay : MonoBehaviour
    {
        [Tooltip("The visual container of the tooltip that will be enabled/disabled.")]
        [SerializeField] private GameObject visualPanel;

        [Tooltip("The text component displaying the trinket info.")]
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
            TooltipEvents.OnShowTrinketTooltip += HandleShowTooltip;
            TooltipEvents.OnHideTrinketTooltip += HandleHideTooltip;
            
            HandleHideTooltip();
        }

        private void OnDisable()
        {
            TooltipEvents.OnShowTrinketTooltip -= HandleShowTooltip;
            TooltipEvents.OnHideTrinketTooltip -= HandleHideTooltip;
        }

        private void HandleShowTooltip(TrinketData trinket)
        {
            if (trinket == null) return;

            if (tooltipText != null)
            {
                var lines = new System.Collections.Generic.List<string>();
                
                // Add name header
                lines.Add($"<b>{trinket.displayName}</b>");
                
                if (!string.IsNullOrEmpty(trinket.description))
                {
                    lines.Add($"<i>{trinket.description}</i>");
                }

                if (trinket.cannotBeRemoved)
                {
                    lines.Add("<color=red>Cursed (Cannot be unequipped)</color>");
                }

                if (trinket.effectStrategies != null)
                {
                    foreach (var strategy in trinket.effectStrategies)
                    {
                        if (strategy != null)
                        {
                            string desc = strategy.GetTooltipDescription();
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
                    tooltipText.text = trinket.displayName;
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
