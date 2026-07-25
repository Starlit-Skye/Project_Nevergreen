using Nevergreen.Combat;
using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Increases the damage multiplier of all outgoing attacks from the wearer.
    /// </summary>
    [Serializable]
    public class DamageOutputBonusTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The percentage bonus to outgoing damage (e.g. 20 = +20% damage).")]
        public int damageBonusPercent = 20;

        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            Action<SkillContext> handler = (ctx) =>
            {
                if (ctx.user == instance.owner && ctx.skill != null && ctx.skill.modifier.IsDamage)
                {
                    ctx.damageMultiplier += (damageBonusPercent / 100f);
                }
            };

            string key = $"OnBeforeDamageCalculation_DmgOut_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key = $"OnBeforeDamageCalculation_DmgOut_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key, out object handlerObj) && handlerObj is Action<SkillContext> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove(key);
            }
        }

        public override string GetTooltipDescription()
        {
            string sign = damageBonusPercent >= 0 ? "+" : "";
            return $"{sign}{damageBonusPercent}% outgoing damage.";
        }
    }
}
