using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Trait strategy that increases the healing received when an ally uses a healing skill on the owner.
    /// Subscribes to OnBeforeDamageCalculation to increase the damage multiplier (which affects heals).
    /// </summary>
    [System.Serializable]
    public class HealReceivedBonusTraitStrategy : TraitEffectStrategy
    {
        [Tooltip("The percentage bonus to healing received (e.g. 20 = +20% heal).")]
        public int healBonusPercent = 20;

        public override void OnActivate(TraitInstance instance)
        {
            if (instance.battleSystem == null) return;

            System.Action<SkillContext> handler = (ctx) =>
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
                            string key = $"HealReceived_{instance.owner.GetInstanceID()}";
                            float current = ctx.extra.ContainsKey(key) ? (float)ctx.extra[key] : 0f;
                            ctx.extra[key] = current + (healBonusPercent / 100f);
                        }
                    }
                }
            };

            instance.extra["OnBeforeDamageCalculation"] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TraitInstance instance)
        {
            if (instance.battleSystem == null) return;

            if (instance.extra.TryGetValue("OnBeforeDamageCalculation", out var handlerObj))
            {
                var handler = handlerObj as System.Action<SkillContext>;
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove("OnBeforeDamageCalculation");
            }
        }
    }
}
