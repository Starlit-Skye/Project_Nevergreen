using Nevergreen.Data;
using System;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Trait strategy that modifies a single stat, but only applies when the character's HP is at or below a certain percentage threshold.
    /// </summary>
    [Serializable]
    public class LowHpStatModifierTraitStrategy : TraitEffectStrategy
    {
        [Tooltip("The HP percentage threshold (0-100). The buff is active if Current HP <= (Threshold / 100 * Max HP).")]
        public float hpThresholdPercent = 50f;

        [Tooltip("Which stat this trait modifies.")]
        public StatTarget targetStat;

        [Tooltip("How the modifier is applied.")]
        public AmplitudeType amplitudeType = AmplitudeType.Default;

        [Tooltip("The modifier value. Positive = buff, Negative = debuff. " +
                 "For Percentage type, 10 means +10%. For Flat type, 10 means +10 points.")]
        public int amount;

        [NonSerialized]
        private System.Collections.Generic.HashSet<int> _recursionGuards;

        public override void ModifyStats(TraitInstance instance, TraitStatModifier modifier)
        {
            if (instance.owner == null || instance.owner.baseStats == null)
                return;

            if (_recursionGuards == null) 
                _recursionGuards = new System.Collections.Generic.HashSet<int>();

            int ownerId = instance.owner.GetInstanceID();

            // Default to base stats max HP if we are currently deep in recursion
            int effectiveMaxHp = instance.owner.baseStats.maxHP;

            // Fetch the true effective Max HP by calling GetEffectiveStats.
            // We use a recursion guard because GetEffectiveStats() calls this method,
            // which would otherwise cause an infinite loop.
            if (!_recursionGuards.Contains(ownerId))
            {
                _recursionGuards.Add(ownerId);
                try
                {
                    effectiveMaxHp = instance.owner.GetEffectiveStats().maxHP;
                }
                finally
                {
                    _recursionGuards.Remove(ownerId);
                }
            }

            float currentHpPercent = ((float)instance.owner.currentHP / effectiveMaxHp) * 100f;

            if (currentHpPercent > hpThresholdPercent)
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
            string thresholdStr = hpThresholdPercent.ToString("0.##");
            return $"{sign}{Math.Abs(amount)}{unit} {targetStat} when below {thresholdStr}% HP";
        }
    }
}
