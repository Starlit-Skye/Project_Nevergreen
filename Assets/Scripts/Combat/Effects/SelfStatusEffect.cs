using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Automatically applies a status effect (Buff/Debuff/Guard/etc.) to the user of the skill (Self).
    /// Prevents duplicate application during multi-hit or multi-target skill execution using the SkillContext.
    /// </summary>
    [Serializable]
    public class SelfStatusEffect : ISkillEffect
    {
        [Tooltip("The status to apply to self.")]
        public StatusType statusType = StatusType.Buff;

        [Tooltip("The specific stat to modify if this is a Buff or Debuff type.")]
        public StatTarget targetStat = StatTarget.Speed;

        [Tooltip("Chance to apply the effect to self. Bypasses resistance.")]
        [Range(0, 100)]
        public float applicationChance = 100f;

        [Tooltip("Power/Stack size of the status.")]
        public int amplitude = 1;

        [Tooltip("How the amplitude is applied (Default uses standard stat rules, Flat adds directly, Percentage scales base).")]
        public AmplitudeType amplitudeType = AmplitudeType.Default;

        [Tooltip("Duration in turns.")]
        public int duration = 3;

        [Tooltip("Should this effect still attempt application even if the attack 'Missed'? Default true for self-buffs.")]
        public bool ignoreMiss = true;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // If the attack missed and we don't ignore misses, do nothing
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            // Ensure we only apply this effect once per skill execution (handling multi-hit/multi-target)
            string appliedKey = $"SelfStatusApplied_{this.GetHashCode()}";
            if (context.extra.ContainsKey(appliedKey))
            {
                return;
            }
            context.extra[appliedKey] = true;

            // Self effects generally bypass target resistances, so we use 0 resistance
            bool applied = CombatCalculator.ResolveStatusApplication(applicationChance, 0, context.rng);

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
                else
                {
                    instance = new StatusEffectInstance(statusType, targetStat, amplitude, duration, amplitudeType);
                    instance.Source = context.user;
                }

                context.user.AddStatus(instance);
                Debug.Log($"  -> {context.user.DisplayName} applied self-status {statusType} (amp:{amplitude}, dur:{duration})");
            }

            // Trigger the status applied event on the user
            context.user.TriggerStatusApplied(statusType, applied);
        }
    }
}
