using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Applies a buff to the target (or self) that adds a chance to apply Bleed on their attacks and Riposte counters.
    /// </summary>
    [Serializable]
    public class BleedOnAttackStatusEffect : ISkillEffect
    {
        [Tooltip("If true, applies the buff to the user instead of the target.")]
        public bool targetSelf = true;

        [Tooltip("Chance to apply the buff itself. Bypasses resistance if self-applied.")]
        [Range(0, 100)]
        public float applicationChance = 100f;

        [Tooltip("Duration of the buff in turns.")]
        public int duration = 3;

        [Tooltip("Should this effect still attempt application even if the attack 'Missed'? Default true.")]
        public bool ignoreMiss = true;

        [Header("Bleed on Attack Config")]
        [Tooltip("Amplitude of the applied bleed status.")]
        public int bleedAmplitude = 1;

        [Tooltip("Duration of the applied bleed status.")]
        public int bleedDuration = 3;

        [Tooltip("Chance to apply the bleed status effect on hit.")]
        public float bleedChance = 100f;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // If the attack missed and we don't ignore misses, do nothing
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            CombatCharacter recipient = targetSelf ? context.user : target;
            if (recipient == null || !recipient.IsAlive) return;

            // Roll for application of the buff itself
            int resistance = targetSelf ? 0 : recipient.GetResistance(StatusType.BleedOnAttack);
            bool applied = CombatCalculator.ResolveStatusApplication(applicationChance, resistance, context.rng);

            if (applied)
            {
                var instance = new BleedOnAttackStatusInstance(context.battleSystem, duration, bleedAmplitude, bleedDuration, bleedChance);
                instance.Source = context.user;
                recipient.AddStatus(instance);
                Debug.Log($"  -> Applied BleedOnAttack buff to {recipient.DisplayName} (dur:{duration})");
            }

            recipient.TriggerStatusApplied(StatusType.BleedOnAttack, applied);
        }
    }
}
