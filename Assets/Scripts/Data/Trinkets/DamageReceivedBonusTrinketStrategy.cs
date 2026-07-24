using Nevergreen.Combat;
using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Increases or decreases the damage multiplier of all incoming attacks targeting the wearer.
    /// Used for effects that increase vulnerability or damage reduction.
    /// </summary>
    [Serializable]
    public class DamageReceivedBonusTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The percentage bonus to incoming damage (e.g. 20 = takes +20% damage).")]
        public int damageBonusPercent = 20;

        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            Action<SkillContext, CombatCharacter> handler = (ctx, target) =>
            {
                if (target == instance.owner && ctx.skill != null && ctx.skill.modifier.IsDamage)
                {
                    ctx.damageMultiplier += (damageBonusPercent / 100f);
                }
            };

            string key = $"OnBeforeDamageCalculationPerTarget_DmgRecv_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key] = handler;
            instance.battleSystem.OnBeforeDamageCalculationPerTarget += handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key = $"OnBeforeDamageCalculationPerTarget_DmgRecv_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key, out object handlerObj) && handlerObj is Action<SkillContext, CombatCharacter> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculationPerTarget -= handler;
                instance.extra.Remove(key);
            }
        }

        public override string GetTooltipDescription()
        {
            string sign = damageBonusPercent >= 0 ? "+" : "";
            return $"Takes {sign}{damageBonusPercent}% damage from attacks.";
        }
    }
}
