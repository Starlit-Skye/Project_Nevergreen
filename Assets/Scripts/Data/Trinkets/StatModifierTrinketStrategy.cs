using UnityEngine;
using Nevergreen.Combat;

namespace Nevergreen.Data
{
    [System.Serializable]
    public class StatModifierTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The stat to modify.")]
        public StatTarget statTarget;

        [Tooltip("How the modifier is applied.")]
        public AmplitudeType amplitudeType = AmplitudeType.Default;

        [Tooltip("The modifier value. Positive = buff, Negative = debuff. For Percentage type, 10 means +10%. For Flat type, 10 means +10 points.")]
        public float amount;

        public override void ModifyStats(TrinketInstance instance, TraitStatModifier modifier)
        {
            AmplitudeType resolved = amplitudeType;
            if (resolved == AmplitudeType.Default)
            {
                resolved = IsFlatStat(statTarget) ? AmplitudeType.Flat : AmplitudeType.Percentage;
            }

            if (resolved == AmplitudeType.Flat)
            {
                modifier.AddFlat(statTarget, Mathf.RoundToInt(amount));
            }
            else
            {
                modifier.AddPercent(statTarget, amount);
            }
        }

        public override string GetTooltipDescription()
        {
            AmplitudeType resolved = amplitudeType;
            if (resolved == AmplitudeType.Default)
            {
                resolved = IsFlatStat(statTarget) ? AmplitudeType.Flat : AmplitudeType.Percentage;
            }

            string sign = amount >= 0 ? "+" : "";
            if (resolved == AmplitudeType.Flat)
            {
                return $"{statTarget} {sign}{Mathf.RoundToInt(amount)}";
            }
            else
            {
                return $"{statTarget} {sign}{amount}%";
            }
        }

        private static bool IsFlatStat(StatTarget target)
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
