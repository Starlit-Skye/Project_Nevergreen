using UnityEngine;
using Nevergreen.Combat;

namespace Nevergreen.Data
{
    [System.Serializable]
    public class LowHpDamageBonusTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The target HP percentage threshold (e.g. 50 means 50%).")]
        public float hpThresholdPercent = 50f;

        [Tooltip("The extra damage percentage to deal (e.g. 20 means +20%).")]
        public float damageBonusPercent = 20f;

        public override void OnActivate(TrinketInstance instance)
        {
            System.Action<SkillContext, CombatCharacter> handler = (ctx, target) =>
            {
                if (ctx.user == instance.owner)
                {
                    CombatStats targetStats = target.GetEffectiveStats();
                    float hpPercent = (float)target.currentHP / targetStats.maxHP * 100f;
                    if (hpPercent < hpThresholdPercent)
                    {
                        ctx.damageMultiplier += damageBonusPercent / 100f;
                    }
                }
            };
            instance.battleSystem.OnBeforeDamageCalculationPerTarget += handler;
            instance.extra["OnBeforeDamageCalculationPerTarget"] = handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.extra.TryGetValue("OnBeforeDamageCalculationPerTarget", out object handlerObj))
            {
                var handler = handlerObj as System.Action<SkillContext, CombatCharacter>;
                if (handler != null && instance.battleSystem != null)
                {
                    instance.battleSystem.OnBeforeDamageCalculationPerTarget -= handler;
                }
                instance.extra.Remove("OnBeforeDamageCalculationPerTarget");
            }
        }

        public override string GetTooltipDescription()
        {
            return $"Deals +{damageBonusPercent}% damage to targets below {hpThresholdPercent}% HP.";
        }
    }
}
