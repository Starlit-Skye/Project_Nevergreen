using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Evaluates resistance and applies a status effect (Buff/Debuff/DoT) to the target.
    /// By default, checks if the overall action hit before attempting application.
    /// </summary>
    [Serializable]
    public class StatusEffect : ISkillEffect
    {
        [Tooltip("The status to apply.")]
        public StatusType statusType;

        [Tooltip("The specific stat to modify if this is a Buff or Debuff type.")]
        public StatTarget targetStat = StatTarget.Speed;

        [Tooltip("Chance to apply before target resistance is considered.")]
        [Range(0, 300)]
        public float applicationChance = 100f;

        [Tooltip("Power/Stack size. For Bleed/Blight/Burn this is the percent of the applier's effective Attack dealt per tick (e.g. 30 = 30%).")]
        public int amplitude = 1;

        [Tooltip("How the amplitude is applied (Default uses standard stat rules, Flat adds directly, Percentage scales base).")]
        public AmplitudeType amplitudeType = AmplitudeType.Default;

        [Tooltip("Duration in turns.")]
        public int duration = 3;

        [Tooltip("Should this effect still attempt application even if the attack 'Missed'?")]
        public bool ignoreMiss = false;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // Only try to apply if the earlier damage phase succeeded (or we explicitly don't care about misses/this is a standalone buff)
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            // Stun does not stack: if target already has an active Stun, ignore the new application completely
            if (statusType == StatusType.Stun && target.HasActiveStatus(StatusType.Stun))
            {
                return;
            }

            float finalChance = applicationChance;
            string key = $"StatusChanceBonus_{statusType}";
            if (context.extra.TryGetValue(key, out object bonusObj) && bonusObj is float bonus)
            {
                finalChance += bonus;
            }

            int resistance = target.GetResistance(statusType);
            
            string unresistableKey = $"StatusUnresistable_{statusType}_{target.GetInstanceID()}";
            if (context.extra.TryGetValue(unresistableKey, out object unresistObj) && unresistObj is bool unresist && unresist)
            {
                resistance = 0; // Bypass this specific target's resistance
            }

            bool applied = CombatCalculator.ResolveStatusApplication(finalChance, resistance, context.rng);

            if (applied)
            {
                int finalAmplitude = amplitude;
                int finalDuration = duration;
                bool isBurst = false;
                
                string burstKey = $"StatusBurst_{statusType}";
                if (context.extra.TryGetValue(burstKey, out object burstObj) && burstObj is bool burst && burst)
                {
                    isBurst = true;
                    if (!StatusProcessor.IsDotType(statusType))
                    {
                        finalAmplitude = amplitude * duration;
                        finalDuration = 1;
                    }
                }

                StatusEffectInstance instance = StatusInstanceFactory.Create(
                    context, 
                    statusType, 
                    targetStat, 
                    finalAmplitude, 
                    finalDuration, 
                    amplitudeType
                );

                if (isBurst && StatusProcessor.IsDotType(statusType))
                {
                    instance.damagePerTick *= duration;
                    instance.remainingDuration = 1;
                }

                bool added = target.AddStatus(instance);
                if (added)
                {
                    if (StatusProcessor.IsDotType(statusType))
                    {
                        Debug.Log($"  -> {target.DisplayName} afflicted with {statusType} (dmg/tick:{instance.damagePerTick}, amp:{finalAmplitude}%, dur:{instance.remainingDuration})");
                    }
                    else
                    {
                        Debug.Log($"  -> {target.DisplayName} afflicted with {statusType} (amp:{finalAmplitude}, dur:{instance.remainingDuration})");
                    }
                }
            }

            target.TriggerStatusApplied(statusType, applied, targetStat);
        }
    }
}
