using Nevergreen.Combat;
using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Increases the damage multiplier (which affects the final heal amount) when the trinket owner uses a healing skill.
    /// </summary>
    [Serializable]
    public class HealOutputBonusTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The percentage bonus to outgoing healing (e.g. 20 = +20% heal).")]
        public int healBonusPercent = 20;

        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            Action<SkillContext> handler = (ctx) =>
            {
                if (ctx.user == instance.owner && ctx.skill != null && ctx.skill.modifier.IsHeal)
                {
                    ctx.damageMultiplier += (healBonusPercent / 100f);
                }
            };

            // Using a unique key to prevent collisions with other trinket strategies that might also hook this event
            string key = $"OnBeforeDamageCalculation_HealOutput_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key = $"OnBeforeDamageCalculation_HealOutput_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key, out object handlerObj) && handlerObj is Action<SkillContext> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove(key);
            }
        }

        public override string GetTooltipDescription()
        {
            string sign = healBonusPercent >= 0 ? "+" : "";
            return $"{sign}{healBonusPercent}% healing output.";
        }
    }
}
