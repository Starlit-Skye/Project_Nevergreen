using UnityEngine;
using TMPro;
using System.Linq;
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

        private string FormatTooltipText(StatusEffectInstance firstStatus)
        {
            if (firstStatus == null) return "";
            if (firstStatus.Host == null)
            {
                int amp = firstStatus.amplitude;
                if (firstStatus is SkillBoostStatusInstance sb) amp = sb.customAmplitude;
                // Fallback to single status formatting
                return FormatSingleTooltipText(firstStatus, amp, firstStatus.remainingDuration);
            }

            var type = firstStatus.type;
            var host = firstStatus.Host;
            var activeStatuses = host.statusEffects.Where(s => s.type == type && !s.IsExpired).ToList();

            if (type == StatusType.Buff || type == StatusType.Debuff)
            {
                var sb = new System.Text.StringBuilder();
                
                // Group normal buffs by targetStat
                var normalBuffs = activeStatuses.Where(s => !(s is SkillBoostStatusInstance))
                                                .GroupBy(s => s.targetStat);
                foreach (var group in normalBuffs)
                {
                    int totalAmp = group.Sum(s => s.amplitude);
                    int maxDur = group.Max(s => s.remainingDuration);
                    var rep = group.First();
                    sb.AppendLine(FormatSingleTooltipText(rep, totalAmp, maxDur));
                }

                // Group skill boosts by targetSkillId
                var skillBoosts = activeStatuses.OfType<SkillBoostStatusInstance>()
                                                .GroupBy(s => s.targetSkillId);
                foreach (var group in skillBoosts)
                {
                    int totalCustom = group.Sum(s => s.customAmplitude);
                    int maxDur = group.Max(s => s.remainingDuration);
                    var rep = group.First();
                    sb.AppendLine(FormatSingleTooltipText(rep, totalCustom, maxDur));
                }

                return sb.ToString().TrimEnd();
            }
            else if (type == StatusType.BleedOnAttack)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var status in activeStatuses)
                {
                    sb.AppendLine(FormatSingleTooltipText(status, status.amplitude, status.remainingDuration));
                }
                return sb.ToString().TrimEnd();
            }
            else
            {
                // For Bleed, Blight, Burn, Restore, HealReceivedReduction, Stun, Mark, Guard, Riposte, etc.
                // We aggregate amplitude and max duration across ALL active statuses of this type
                int totalAmp = activeStatuses.Sum(s => s.amplitude);
                int maxDur = activeStatuses.Max(s => s.remainingDuration);
                // Also handles Guard, Stun, Riposte (which ignore amplitude/duration or handle them generically)
                return FormatSingleTooltipText(firstStatus, totalAmp, maxDur);
            }
        }

        private string FormatSingleTooltipText(StatusEffectInstance status, int aggregateAmplitude, int maxDuration)
        {
            switch (status.type)
            {
                case StatusType.Bleed:
                case StatusType.Blight:
                    return $"{aggregateAmplitude} dmg for {maxDuration} rounds";
                case StatusType.Stun:
                    return "Skips the next turn";
                case StatusType.Debuff:
                    string dMark = IsPercentageModifier(status) ? "%" : "";
                    return $"-{aggregateAmplitude}{dMark} {status.targetStat} for {maxDuration} rounds";
                case StatusType.Buff:
                    if (status is SkillBoostStatusInstance skillBoost)
                    {
                        string skillName = skillBoost.targetSkillDisplayName ?? "null";
                        return $"{skillName} + {aggregateAmplitude}% dmg for {maxDuration} rounds";
                    }
                    string bMark = IsPercentageModifier(status) ? "%" : "";
                    return $"+{aggregateAmplitude}{bMark} {status.targetStat} for {maxDuration} rounds";
                case StatusType.Mark:
                    return $"Marked as target for {maxDuration} rounds";
                case StatusType.Guard:
                    string guardianName = (status.Source != null) ? status.Source.DisplayName : "unknown";
                    return $"Guarded by {guardianName} for {maxDuration} rounds";
                case StatusType.Restore:
                    return $"Heal {aggregateAmplitude} for {maxDuration} rounds";
                case StatusType.Stealth:
                    return $"Cannot be directly targeted by enemies for {maxDuration} rounds";
                case StatusType.Burn:
                    return $"{aggregateAmplitude}dmg, dmg + 1 each turn, for {maxDuration} rounds";
                case StatusType.HealReceivedReduction:
                    return $"Heal received -{aggregateAmplitude}% for {maxDuration} rounds";
                case StatusType.BleedOnAttack:
                    if (status is BleedOnAttackStatusInstance bleedOnAttack)
                    {
                        return $"Attacks apply Bleed({bleedOnAttack.BleedChance}% chance) for {maxDuration} rounds";
                    }
                    return $"Attacks apply Bleed for {maxDuration} rounds";
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
