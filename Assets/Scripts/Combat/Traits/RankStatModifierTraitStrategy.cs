using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Trait strategy that modifies a stat, but only applies when the character is at a specific rank.
    /// </summary>
    [System.Serializable]
    public class RankStatModifierTraitStrategy : TraitEffectStrategy
    {
        [Tooltip("The rank the owner must be at for the bonus to apply.")]
        [Range(1, 4)]
        public int requiredRank = 1;

        [Tooltip("Which stat this trait modifies.")]
        public StatTarget targetStat;

        [Tooltip("How the modifier is applied.")]
        public AmplitudeType amplitudeType = AmplitudeType.Default;

        [Tooltip("The modifier value. Positive = buff, Negative = debuff. " +
                 "For Percentage type, 10 means +10%. For Flat type, 10 means +10 points.")]
        public int amount;

        public override void ModifyStats(TraitInstance instance, TraitStatModifier modifier)
        {
            if (instance.owner == null)
                return;

            // OccupiedRanks properly accounts for multi-tile characters
            if (!instance.owner.OccupiedRanks.Contains(requiredRank))
                return;

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

        private bool IsFlatStat(StatTarget stat)
        {
            return stat == StatTarget.CritChance ||
                   stat == StatTarget.BleedResist ||
                   stat == StatTarget.BlightResist ||
                   stat == StatTarget.StunResist ||
                   stat == StatTarget.DebuffResist ||
                   stat == StatTarget.MoveResist;
        }

        public override string GetTooltipDescription(TraitType traitType)
        {
            char sign = traitType == TraitType.Perfection ? '+' : '-';
            bool isFlat = amplitudeType == AmplitudeType.Flat || 
                          (amplitudeType == AmplitudeType.Default && IsFlatStat(targetStat));
            string unit = isFlat ? "" : "%";
            return $"{sign}{System.Math.Abs(amount)}{unit} {targetStat} when at rank {requiredRank}";
        }
    }
}
