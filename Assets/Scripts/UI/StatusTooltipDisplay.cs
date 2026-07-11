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

        private int GetAggregateAmplitude(StatusEffectInstance status)
        {
            if (status == null) return 0;
            if (status.Host == null)
            {
                if (status is SkillBoostStatusInstance skillBoost) return skillBoost.customAmplitude;
                return status.amplitude;
            }

            if (status is SkillBoostStatusInstance sbBoost)
            {
                int totalCustom = 0;
                foreach (var s in status.Host.statusEffects)
                {
                    if (s is SkillBoostStatusInstance sb && !sb.IsExpired && sb.targetSkillId == sbBoost.targetSkillId)
                    {
                        totalCustom += sb.customAmplitude;
                    }
                }
                return totalCustom;
            }

            if (status.type == StatusType.Bleed || status.type == StatusType.Blight || status.type == StatusType.Burn || status.type == StatusType.Restore || status.type == StatusType.HealReceivedReduction)
            {
                int total = 0;
                foreach (var s in status.Host.statusEffects)
                {
                    if (s.type == status.type && !s.IsExpired)
                    {
                        total += s.amplitude;
                    }
                }
                return total;
            }

            if (status.type == StatusType.Buff || status.type == StatusType.Debuff)
            {
                int total = 0;
                foreach (var s in status.Host.statusEffects)
                {
                    if (s.type == status.type && s.targetStat == status.targetStat && !s.IsExpired)
                    {
                        total += s.amplitude;
                    }
                }
                return total;
            }

            return status.amplitude;
        }

        private string FormatTooltipText(StatusEffectInstance status)
        {
            int aggregateAmplitude = GetAggregateAmplitude(status);

            switch (status.type)
            {
                case StatusType.Bleed:
                case StatusType.Blight:
                    return $"{aggregateAmplitude} dmg for {status.remainingDuration} rounds";
                case StatusType.Stun:
                    return "Skips the next turn";
                case StatusType.Debuff:
                    string dMark = IsPercentageModifier(status) ? "%" : "";
                    return $"-{aggregateAmplitude}{dMark} {status.targetStat} for {status.remainingDuration} rounds";
                case StatusType.Buff:
                    if (status is SkillBoostStatusInstance skillBoost)
                    {
                        string skillName = skillBoost.targetSkillDisplayName ?? "null";
                        return $"{skillName} + {aggregateAmplitude}% dmg";
                    }
                    string bMark = IsPercentageModifier(status) ? "%" : "";
                    return $"+{aggregateAmplitude}{bMark} {status.targetStat} for {status.remainingDuration} rounds";
                case StatusType.Mark:
                    return $"Marked as target for {status.remainingDuration} rounds";
                case StatusType.Guard:
                    string guardianName = (status.Source != null) ? status.Source.DisplayName : "unknown";
                    return $"Guarded by {guardianName} for {status.remainingDuration} rounds";
                case StatusType.Restore:
                    return $"Heal {aggregateAmplitude} for {status.remainingDuration} rounds";
                case StatusType.Stealth:
                    return $"Cannot be directly targeted by enemies for {status.remainingDuration} rounds";
                case StatusType.Burn:
                    return $"{aggregateAmplitude}dmg, dmg + 1 each turn, for {status.remainingDuration} rounds";
                case StatusType.HealReceivedReduction:
                    return $"Heal received -{aggregateAmplitude}% for {status.remainingDuration} rounds";
                case StatusType.BleedOnAttack:
                    if (status is BleedOnAttackStatusInstance bleedOnAttack)
                    {
                        return $"Attacks apply Bleed({bleedOnAttack.BleedChance}% chance) for {status.remainingDuration} rounds";
                    }
                    return $"Attacks apply Bleed for {status.remainingDuration} rounds";
                case StatusType.Riposte:
                    return "Counter when attacked";
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
