using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Trait strategy that increases the damage multiplier when the owner
    /// is standing at a specific rank. Subscribes to OnBeforeDamageCalculation
    /// to apply the bonus damage to the damage multiplier in the skill context.
    /// </summary>
    [System.Serializable]
    public class RankDamageBonusTraitStrategy : TraitEffectStrategy
    {
        [Tooltip("The rank the owner must be at for the bonus to apply.")]
        [Range(1, 4)]
        public int requiredRank = 1;

        [Tooltip("The percentage bonus to damage when at the required rank (e.g. 15 = +15% damage).")]
        public int damageBonusPercent = 15;

        public override void OnActivate(TraitInstance instance)
        {
            if (instance.battleSystem == null) return;

            System.Action<SkillContext> handler = (ctx) =>
            {
                if (ctx.user == instance.owner && instance.owner.rank == requiredRank)
                {
                    ctx.damageMultiplier += damageBonusPercent / 100f;
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
