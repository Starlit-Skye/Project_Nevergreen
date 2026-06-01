using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Trait strategy that unconditionally modifies a single stat.
    /// Examples:
    ///   - Perfection: "+10 Attack" (flat) or "+15% Speed" (percent)
    ///   - Imperfection: "-5 Defense" (flat) or "-10% Accuracy" (percent)
    /// The sign should be baked into the value (positive for buff, negative for debuff).
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatModTrait", menuName = "Nevergreen/Traits/Stat Modifier")]
    public class StatModifierTraitStrategy : TraitEffectStrategy
    {
        [Tooltip("Which stat this trait modifies.")]
        public StatTarget targetStat;

        [Tooltip("How the modifier is applied.")]
        public AmplitudeType amplitudeType = AmplitudeType.Default;

        [Tooltip("The modifier value. Positive = buff, Negative = debuff. " +
                 "For Percentage type, 10 means +10%. For Flat type, 10 means +10 points.")]
        public int amount;

        public override void ModifyStats(TraitInstance instance, TraitStatModifier modifier)
        {
            // Resolve Default amplitude type to the correct concrete type
            AmplitudeType resolved = amplitudeType;
            if (resolved == AmplitudeType.Default)
            {
                resolved = IsFlatStat(targetStat) ? AmplitudeType.Flat : AmplitudeType.Percentage;
            }

            if (resolved == AmplitudeType.Flat)
            {
                modifier.AddFlat(targetStat, amount);
            }
            else
            {
                modifier.AddPercent(targetStat, amount);
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
