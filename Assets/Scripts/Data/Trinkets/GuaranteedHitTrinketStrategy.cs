using UnityEngine;
using Nevergreen.Combat;

namespace Nevergreen.Data
{
    [System.Serializable]
    public class GuaranteedHitTrinketStrategy : TrinketEffectStrategy
    {
        public override void OnActivate(TrinketInstance instance)
        {
            System.Action<SkillContext> handler = (ctx) =>
            {
                if (ctx.user == instance.owner)
                {
                    ctx.guaranteedHit = true;
                }
            };
            instance.battleSystem.OnBeforeDamageCalculation += handler;
            instance.extra["OnBeforeDamageCalculation"] = handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.extra.TryGetValue("OnBeforeDamageCalculation", out object handlerObj))
            {
                var handler = handlerObj as System.Action<SkillContext>;
                if (handler != null && instance.battleSystem != null)
                {
                    instance.battleSystem.OnBeforeDamageCalculation -= handler;
                }
                instance.extra.Remove("OnBeforeDamageCalculation");
            }
        }

        public override string GetTooltipDescription()
        {
            return "Makes all attacks guaranteed to hit.";
        }
    }
}
