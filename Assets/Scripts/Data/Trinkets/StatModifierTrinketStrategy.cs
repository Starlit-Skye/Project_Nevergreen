using UnityEngine;
using Nevergreen.Combat;

namespace Nevergreen.Data
{
    [System.Serializable]
    public class StatModifierTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The stat to modify.")]
        public StatTarget statTarget;

        [Tooltip("Flat amount to add to the stat.")]
        public int flatBonus;

        [Tooltip("Percentage to add to the stat (e.g. 10 = +10%).")]
        public float percentBonus;

        public override void ModifyStats(TrinketInstance instance, TraitStatModifier modifier)
        {
            if (flatBonus != 0) modifier.AddFlat(statTarget, flatBonus);
            if (percentBonus != 0f) modifier.AddPercent(statTarget, percentBonus);
        }

        public override string GetTooltipDescription()
        {
            string desc = "";
            if (flatBonus != 0) desc += $"{statTarget} {(flatBonus > 0 ? "+" : "")}{flatBonus}";
            if (percentBonus != 0f)
            {
                if (desc.Length > 0) desc += ", ";
                desc += $"{statTarget} {(percentBonus > 0 ? "+" : "")}{percentBonus}%";
            }
            return desc;
        }
    }
}
