using Nevergreen.Combat;
using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Trinket strategy that increases the healing received when an ally uses a healing skill on the owner.
    /// Subscribes to OnBeforeDamageCalculation to increase the damage multiplier (which affects heals).
    /// </summary>
    [Serializable]
    public class HealReceivedBonusTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The percentage bonus to healing received (e.g. 20 = +20% heal).")]
        public int healBonusPercent = 20;

        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            Action<SkillContext> handler = (ctx) =>
            {
                // Ensure this is a healing skill
                if (ctx.skill != null && ctx.skill.modifier.IsHeal)
                {
                    // Ensure it was cast by an ally
                    if (ctx.user != null && instance.owner != null && ctx.user.team == instance.owner.team)
                    {
                        // Ensure the owner is one of the targets
                        if (ctx.targets != null && ctx.targets.Contains(instance.owner))
                        {
                            string internalKey = $"HealReceived_{instance.owner.GetInstanceID()}";
                            float current = ctx.extra.ContainsKey(internalKey) ? (float)ctx.extra[internalKey] : 0f;
                            ctx.extra[internalKey] = current + (healBonusPercent / 100f);
                        }
                    }
                }
            };

            string key = $"OnBeforeDamageCalculation_HealReceived_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key = $"OnBeforeDamageCalculation_HealReceived_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key, out var handlerObj) && handlerObj is Action<SkillContext> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove(key);
            }
        }

        public override string GetTooltipDescription()
        {
            string sign = healBonusPercent >= 0 ? "+" : "";
            return $"{sign}{Math.Abs(healBonusPercent)}% heal received.";
        }
    }
}
