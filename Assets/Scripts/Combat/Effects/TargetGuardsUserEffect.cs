using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Applies a Guard status effect to the user, making the targeted character the guardian.
    /// This causes the targeted character to protect the user.
    /// </summary>
    [Serializable]
    public class TargetGuardsUserEffect : ISkillEffect
    {
        [Tooltip("Chance to apply the effect. Bypasses resistance.")]
        [Range(0, 100)]
        public float applicationChance = 100f;

        [Tooltip("Duration in turns.")]
        public int duration = 3;

        [Tooltip("Should this effect still attempt application even if the attack 'Missed'? Default true.")]
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
            string appliedKey = $"TargetGuardsUserApplied_{this.GetHashCode()}";
            if (context.extra.ContainsKey(appliedKey))
            {
                return;
            }
            context.extra[appliedKey] = true;

            // Apply the guard status to the user, with the target as the source (guardian)
            bool applied = CombatCalculator.ResolveStatusApplication(applicationChance, 0, context.rng);

            if (applied)
            {
                // The target acts as the guardian (Source)
                StatusEffectInstance instance = new GuardStatusInstance(target, duration);
                
                // The user is the one being protected (Host)
                context.user.AddStatus(instance);
                Debug.Log($"  -> {target.DisplayName} is now guarding {context.user.DisplayName} (dur:{duration})");
            }

            // Trigger the status applied event on the user
            context.user.TriggerStatusApplied(StatusType.Guard, applied);
        }
    }
}
