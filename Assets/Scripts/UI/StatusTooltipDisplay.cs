using UnityEngine;
using TMPro;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// Component attached to the HPBar prefab (or its tooltip container).
    /// Listens to global status tooltip events and toggles its local visual panel.
    /// </summary>
    public class StatusTooltipDisplay : MonoBehaviour
    {
        [Tooltip("The visual container of the tooltip that will be enabled/disabled.")]
        [SerializeField] private GameObject visualPanel;
        
        [Tooltip("The text component displaying the status effect name.")]
        [SerializeField] private TextMeshProUGUI tooltipText;
        
        private HPBar _parentHPBar;
        
        private HPBar ParentHPBar
        {
            get
            {
                if (_parentHPBar == null)
                {
                    _parentHPBar = GetComponent<HPBar>();
                    if (_parentHPBar == null)
                    {
                        _parentHPBar = GetComponentInParent<HPBar>();
                    }
                }
                return _parentHPBar;
            }
        }

        private void Awake()
        {

            if (visualPanel != null)
            {
                visualPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            TooltipEvents.OnShowStatusTooltip += HandleShowTooltip;
            TooltipEvents.OnHideStatusTooltip += HandleHideTooltip;
        }

        private void OnDisable()
        {
            TooltipEvents.OnShowStatusTooltip -= HandleShowTooltip;
            TooltipEvents.OnHideStatusTooltip -= HandleHideTooltip;
        }

        private void HandleShowTooltip(StatusIconTooltipTrigger trigger)
        {
            if (ParentHPBar == null) 
            {
                Debug.LogWarning("[StatusTooltipDisplay] ParentHPBar is null!");
                return;
            }
            
            var triggerHPBar = trigger.GetComponentInParent<HPBar>();
            Debug.Log($"[StatusTooltipDisplay] OnShowTooltip received event from {triggerHPBar?.name}. My HPBar is {ParentHPBar.name}");
            
            if (triggerHPBar == ParentHPBar)
            {
                if (visualPanel != null)
                {
                    visualPanel.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("[StatusTooltipDisplay] visualPanel is not assigned!");
                }
                
                if (tooltipText != null && trigger.StatusEffect != null)
                {
                    tooltipText.text = trigger.StatusEffect.type.ToString();
                }
                else if (tooltipText == null)
                {
                    Debug.LogWarning("[StatusTooltipDisplay] tooltipText is not assigned!");
                }
            }
        }

        private void HandleHideTooltip(StatusIconTooltipTrigger trigger)
        {
            if (trigger != null && trigger.GetComponentInParent<HPBar>() == ParentHPBar)
            {
                if (visualPanel != null)
                {
                    visualPanel.SetActive(false);
                }
            }
        }
    }
}
