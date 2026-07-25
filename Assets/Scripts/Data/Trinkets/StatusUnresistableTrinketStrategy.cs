using Nevergreen.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Causes the wearer to be unable to resist specified statuses applied by incoming attacks.
    /// </summary>
    [Serializable]
    public class StatusUnresistableTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The statuses that the wearer cannot resist.")]
        public List<StatusType> statusTypes = new List<StatusType>();

        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null || statusTypes == null) return;

            Action<SkillContext, CombatCharacter> handler = (ctx, target) =>
            {
                if (target == instance.owner)
                {
                    foreach (var type in statusTypes)
                    {
                        ctx.extra[$"StatusUnresistable_{type}_{target.GetInstanceID()}"] = true;
                    }
                }
            };

            string key = $"OnBeforeDamageCalculationPerTarget_Unresistable_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key] = handler;
            instance.battleSystem.OnBeforeDamageCalculationPerTarget += handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key = $"OnBeforeDamageCalculationPerTarget_Unresistable_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key, out object handlerObj) && handlerObj is Action<SkillContext, CombatCharacter> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculationPerTarget -= handler;
                instance.extra.Remove(key);
            }
        }

        public override string GetTooltipDescription()
        {
            if (statusTypes == null || statusTypes.Count == 0) return "";
            string typesStr = string.Join(" and ", statusTypes);
            return $"Cannot resist {typesStr}.";
        }
    }
}
