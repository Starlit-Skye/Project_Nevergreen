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

        [Tooltip("Power/Stack size of the status.")]
        public int amplitude = 1;

        [Tooltip("Duration in turns.")]
        public int duration = 3;

        [Tooltip("Should this effect still attempt application even if the attack 'Missed'?")]
        public bool ignoreMiss = false;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // Only try to apply if the earlier damage phase succeeded (or we explicitly don't care about misses/this is a standalone buff)
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            int resistance = target.GetResistance(statusType);
            bool applied = CombatCalculator.ResolveStatusApplication(applicationChance, resistance, context.rng);

            if (applied)
            {
                var instance = new StatusEffectInstance(statusType, amplitude, duration);
                target.AddStatus(instance);
                Debug.Log($"  -> {target.DisplayName} afflicted with {statusType} (amp:{amplitude}, dur:{duration})");
            }
        }
    }
}
