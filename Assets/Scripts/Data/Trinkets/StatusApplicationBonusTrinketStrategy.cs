using Nevergreen.Combat;
using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Increases the application chance of a specific status effect (like Stun) when the trinket owner uses a skill.
    /// </summary>
    [Serializable]
    public class StatusApplicationBonusTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The status type to boost application chance for.")]
        public StatusType statusType = StatusType.Stun;

        [Tooltip("Flat bonus to application chance (e.g. 20 for +20%).")]
        public float applicationChanceBonus = 20f;

        [Tooltip("If true, the bonus only applies if the skill is an enemy-targeted skill.")]
        public bool onlyAgainstEnemies = true;

        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            Action<SkillContext> handler = (ctx) =>
            {
                // Only apply if the trinket owner is the one using the skill
                if (ctx.user != instance.owner)
                    return;

                if (onlyAgainstEnemies && ctx.skill != null && ctx.skill.targetScope != TargetScope.Enemies)
                    return;

                // Store the bonus in the context so StatusEffect can pick it up
                string internalKey = $"StatusChanceBonus_{statusType}";
                float current = ctx.extra.ContainsKey(internalKey) ? (float)ctx.extra[internalKey] : 0f;
                ctx.extra[internalKey] = current + applicationChanceBonus;
            };

            string key = $"OnBeforeDamageCalculation_StatusAppBonus_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key = $"OnBeforeDamageCalculation_StatusAppBonus_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key, out object handlerObj) && handlerObj is Action<SkillContext> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove(key);
            }
        }

        public override string GetTooltipDescription()
        {
            string sign = applicationChanceBonus >= 0 ? "+" : "";
            return $"{sign}{Math.Abs(applicationChanceBonus)}% {statusType} chance.";
        }
    }
}
