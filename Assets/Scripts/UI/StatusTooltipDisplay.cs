using UnityEngine;
using TMPro;
using Nevergreen.Prototype;
using Nevergreen.Combat;
using Nevergreen.Data;

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
                    tooltipText.text = FormatTooltipText(trigger.StatusEffect);
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

        private string FormatTooltipText(StatusEffectInstance status)
        {
            switch (status.type)
            {
                case StatusType.Bleed:
                case StatusType.Blight:
                    return $"{status.amplitude} dmg for {status.remainingDuration} rounds";
                case StatusType.Stun:
                    return "Skips the next turn";
                case StatusType.Debuff:
                    string dMark = IsPercentageModifier(status) ? "%" : "";
                    return $"-{status.amplitude}{dMark} {status.targetStat} for {status.remainingDuration} rounds";
                case StatusType.Buff:
                    string bMark = IsPercentageModifier(status) ? "%" : "";
                    return $"+{status.amplitude}{bMark} {status.targetStat} for {status.remainingDuration} rounds";
                case StatusType.Mark:
                    return $"Marked as target for {status.remainingDuration} rounds";
                case StatusType.Guard:
                    string guardianName = (status.Source != null) ? status.Source.DisplayName : "unknown";
                    return $"Guarded by {guardianName} for {status.remainingDuration} rounds";
                case StatusType.Restore:
                    return $"Heal {status.amplitude} for {status.remainingDuration} rounds";
                case StatusType.Stealth:
                    return $"Cannot be directly targeted by enemies for {status.remainingDuration} rounds";
                case StatusType.Burn:
                    return $"{status.amplitude}dmg, dmg + 1 each turn, for {status.remainingDuration} rounds";
                case StatusType.HealReceivedReduction:
                    return $"Heal received -{status.amplitude}% for {status.remainingDuration} rounds";
                case StatusType.BleedOnAttack:
                    if (status is BleedOnAttackStatusInstance bleedOnAttack)
                    {
                        return $"Attacks apply Bleed({bleedOnAttack.BleedChance}% chance) for {status.remainingDuration} rounds";
                    }
                    return $"Attacks apply Bleed for {status.remainingDuration} rounds";
                default:
                    return status.type.ToString();
            }
        }

        private bool IsPercentageModifier(StatusEffectInstance status)
        {
            if (status.amplitudeType == AmplitudeType.Percentage) return true;
            if (status.amplitudeType == AmplitudeType.Flat) return false;
            return !IsFlatStat(status.targetStat);
        }

        private bool IsFlatStat(StatTarget target)
        {
            return target == StatTarget.CritChance ||
                   target == StatTarget.BleedResist ||
                   target == StatTarget.BlightResist ||
                   target == StatTarget.StunResist ||
                   target == StatTarget.DebuffResist ||
                   target == StatTarget.MoveResist;
        }
    }
}
