using Nevergreen.Data;
using System;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Increases the application chance of a specific status effect (like Stun) when the trait owner uses a skill.
    /// </summary>
    [Serializable]
    public class StatusApplicationBonusTraitStrategy : TraitEffectStrategy
    {
        [Tooltip("The status type to boost application chance for.")]
        public StatusType statusType = StatusType.Stun;

        [Tooltip("Flat bonus to application chance (e.g. 20 for +20%).")]
        public float applicationChanceBonus = 20f;

        [Tooltip("If true, the bonus only applies if the skill is an enemy-targeted skill.")]
        public bool onlyAgainstEnemies = true;

        public override void OnActivate(TraitInstance instance)
        {
            if (instance.battleSystem == null) return;

            Action<SkillContext> handler = (ctx) =>
            {
                // Only apply if the trait owner is the one using the skill
                if (ctx.user != instance.owner)
                    return;

                if (onlyAgainstEnemies && ctx.skill != null && ctx.skill.targetScope != TargetScope.Enemies)
                    return;

                // Store the bonus in the context so StatusEffect can pick it up
                string key = $"StatusChanceBonus_{statusType}";
                float current = ctx.extra.ContainsKey(key) ? (float)ctx.extra[key] : 0f;
                ctx.extra[key] = current + applicationChanceBonus;
            };

            instance.extra["OnBeforeDamageCalculation"] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TraitInstance instance)
        {
            if (instance.battleSystem == null) return;

            if (instance.extra.TryGetValue("OnBeforeDamageCalculation", out object handlerObj) && handlerObj is Action<SkillContext> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove("OnBeforeDamageCalculation");
            }
        }

        public override string GetTooltipDescription(TraitType traitType)
        {
            char sign = traitType == TraitType.Perfection ? '+' : '-';
            return $"{sign}{Math.Abs(applicationChanceBonus)}% {statusType} chance";
        }
    }
}
