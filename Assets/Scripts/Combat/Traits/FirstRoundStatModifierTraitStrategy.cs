using Nevergreen.Data;
using System;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Trait strategy that modifies a single stat, but only applies up to the end of the first round of combat.
    /// Outside of combat (when battleSystem is null) or during Round 1, the bonus is active.
    /// From Round 2 onwards, the bonus is deactivated.
    /// </summary>
    [Serializable]
    public class FirstRoundStatModifierTraitStrategy : TraitEffectStrategy
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
            // Only apply outside of combat OR during the first round (Round 0 or 1).
            if (instance.battleSystem != null && instance.battleSystem.CurrentRound > 1)
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
            // Speed is normally percentage by default for buffs/debuffs, but core stats usually scale.
            // Critical Chance and Resistances are flat by default in GDD.
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
            return $"{sign}{Math.Abs(amount)}{unit} {targetStat} on first round";
        }
    }
}
