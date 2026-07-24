using Nevergreen.Combat;
using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Wearer takes a percentage of their max HP as damage when attacking.
    /// </summary>
    [Serializable]
    public class SelfDamageOnAttackTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("Percentage of Max HP taken as damage when attacking (e.g. 10 = 10%).")]
        public float maxHpPercentageDamage = 10f;

        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            Action<SkillContext> handler = (ctx) =>
            {
                if (ctx.user == instance.owner && ctx.skill != null && ctx.skill.modifier.IsDamage)
                {
                    int dmg = Mathf.RoundToInt(instance.owner.baseStats.maxHP * (maxHpPercentageDamage / 100f));
                    instance.owner.TakeDamage(dmg, false);
                    Debug.Log($"[SelfDamageTrinket] {instance.owner.DisplayName} took {dmg} damage from attacking.");
                }
            };

            string key = $"OnBeforeDamageCalculation_SelfDmg_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key = $"OnBeforeDamageCalculation_SelfDmg_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key, out object handlerObj) && handlerObj is Action<SkillContext> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove(key);
            }
        }

        public override string GetTooltipDescription()
        {
            return $"Take {maxHpPercentageDamage}% Max HP damage when attacking.";
        }
    }
}
