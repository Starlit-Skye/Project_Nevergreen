using Nevergreen.Data;
using System;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Increases the damage multiplier (which affects the final heal amount) when the trait owner uses a healing skill.
    /// </summary>
    [Serializable]
    public class HealOutputBonusTraitStrategy : TraitEffectStrategy
    {
        [Tooltip("The percentage bonus to outgoing healing (e.g. 20 = +20% heal).")]
        public int healBonusPercent = 20;

        public override void OnActivate(TraitInstance instance)
        {
            if (instance.battleSystem == null) return;

            Action<SkillContext> handler = (ctx) =>
            {
                if (ctx.user == instance.owner && ctx.skill != null && ctx.skill.modifier.IsHeal)
                {
                    ctx.damageMultiplier += (healBonusPercent / 100f);
                }
            };

            // Using a unique key to prevent collisions with other trait strategies that might also hook this event
            instance.extra["OnBeforeDamageCalculation_HealOutput"] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TraitInstance instance)
        {
            if (instance.battleSystem == null) return;

            if (instance.extra.TryGetValue("OnBeforeDamageCalculation_HealOutput", out object handlerObj) && handlerObj is Action<SkillContext> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove("OnBeforeDamageCalculation_HealOutput");
            }
        }
    }
}
