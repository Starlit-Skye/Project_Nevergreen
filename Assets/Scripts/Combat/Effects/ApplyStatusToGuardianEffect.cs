using System;
using System.Linq;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Applies a status effect to the guardian of the targeted character.
    /// If the targeted character is not guarded, this effect does nothing.
    /// </summary>
    [Serializable]
    public class ApplyStatusToGuardianEffect : ISkillEffect
    {
        [Tooltip("The status to apply to the guardian.")]
        public StatusType statusType;

        [Tooltip("The specific stat to modify if this is a Buff or Debuff type.")]
        public StatTarget targetStat = StatTarget.Speed;

        [Tooltip("Chance to apply before target resistance is considered.")]
        [Range(0, 300)]
        public float applicationChance = 100f;

        [Tooltip("Power/Stack size of the status.")]
        public int amplitude = 1;

        [Tooltip("How the amplitude is applied (Default uses standard stat rules, Flat adds directly, Percentage scales base).")]
        public AmplitudeType amplitudeType = AmplitudeType.Default;

        [Tooltip("Duration in turns.")]
        public int duration = 3;

        [Tooltip("Should this effect still attempt application even if the attack 'Missed'?")]
        public bool ignoreMiss = false;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // Only try to apply if the earlier damage phase succeeded (or we explicitly don't care about misses)
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            CombatCharacter guardianToTarget = null;

            // Friendly actions (buffs, heals, etc.) targeted at Self or Allies do not undergo guard redirection.
            // In these cases, the target parameter remains the original targeted character, behaving as if guard was bypassed.
            bool isGuardRedirectionBypassed = context.bypassGuard || (context.skill != null && context.skill.targetScope != TargetScope.Enemies);

            if (isGuardRedirectionBypassed)
            {
                // 1. If guard was bypassed (either explicitly or due to non-Enemy target scope), 
                // 'target' is the original targeted character. We check if this targeted character is guarded.
                guardianToTarget = GetActiveGuardian(target);
            }
            else
            {
                // 2. If guard was not bypassed, redirection might have occurred.
                // 'target' is the final target (which is the guardian if redirected, or the original target if not).
                // Let's check if 'target' is the guardian of any of the original targets in the context.
                foreach (var origTarget in context.targets)
                {
                    if (GetActiveGuardian(origTarget) == target)
                    {
                        guardianToTarget = target;
                        break;
                    }
                }
            }

            // If the targeted character is not guarded, the skill does nothing.
            if (guardianToTarget == null)
            {
                return;
            }

            // Prevent duplicate application to the same guardian during this skill execution (handling multi-hit/multi-target)
            string appliedKey = $"ApplyStatusToGuardian_{guardianToTarget.GetHashCode()}_{this.GetHashCode()}";
            if (context.extra.ContainsKey(appliedKey))
            {
                return;
            }
            context.extra[appliedKey] = true;

            // Evaluate resistance on the guardian
            int resistance = guardianToTarget.GetResistance(statusType);
            bool applied = CombatCalculator.ResolveStatusApplication(applicationChance, resistance, context.rng);

            if (applied)
            {
                StatusEffectInstance instance;
                if (statusType == StatusType.Guard)
                {
                    instance = new GuardStatusInstance(context.user, duration);
                }
                else if (statusType == StatusType.Move)
                {
                    instance = new MoveStatusInstance(context.battleSystem, amplitude);
                    instance.Source = context.user;
                }
                else if (statusType == StatusType.Stealth)
                {
                    instance = new StealthStatusInstance(duration);
                    instance.Source = context.user;
                }
                else
                {
                    instance = new StatusEffectInstance(statusType, targetStat, amplitude, duration, amplitudeType);
                    instance.Source = context.user;
                }

                guardianToTarget.AddStatus(instance);
                Debug.Log($"  -> Guardian {guardianToTarget.DisplayName} afflicted with {statusType} (amp:{amplitude}, dur:{duration})");
            }

            guardianToTarget.TriggerStatusApplied(statusType, applied);
        }

        private CombatCharacter GetActiveGuardian(CombatCharacter character)
        {
            if (character == null) return null;

            var guard = character.statusEffects.OfType<GuardStatusInstance>()
                .FirstOrDefault(s => !s.IsExpired);

            if (guard == null || guard.Source == null || !guard.Source.IsAlive || guard.Source.isStunned)
            {
                return null;
            }
            return guard.Source;
        }
    }
}
