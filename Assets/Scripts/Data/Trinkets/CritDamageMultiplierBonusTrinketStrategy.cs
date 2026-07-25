using Nevergreen.Combat;
using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Increases the critical damage multiplier when the trinket owner uses an attack skill.
    /// </summary>
    [Serializable]
    public class CritDamageMultiplierBonusTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The flat amount to add to the critical multiplier (e.g. 0.5 adds 50% extra critical damage).")]
        public float critMultiplierBonus = 0.5f;

        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            Action<SkillContext> handler = (ctx) =>
            {
                // Only apply if the trinket owner is the one using the skill
                if (ctx.user == instance.owner && ctx.skill != null && ctx.skill.modifier.IsDamage)
                {
                    ctx.critMultiplier += critMultiplierBonus;
                }
            };

            string key = $"OnBeforeDamageCalculation_CritMultBonus_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key = $"OnBeforeDamageCalculation_CritMultBonus_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key, out object handlerObj) && handlerObj is Action<SkillContext> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove(key);
            }
        }

        public override string GetTooltipDescription()
        {
            string sign = critMultiplierBonus >= 0 ? "+" : "";
            return $"{sign}{Mathf.RoundToInt(critMultiplierBonus * 100f)}% critical damage.";
        }
    }
}
