using Nevergreen.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Causes specified statuses applied by the wearer to have their duration reduced to 1, 
    /// but deal their total intended amplitude (original amplitude * original duration) immediately on expiration.
    /// </summary>
    [Serializable]
    public class StatusBurstTrinketStrategy : TrinketEffectStrategy
    {
        [Tooltip("The statuses that will burst.")]
        public List<StatusType> statusTypes = new List<StatusType>();

        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null || statusTypes == null) return;

            Action<SkillContext> handler = (ctx) =>
            {
                if (ctx.user == instance.owner)
                {
                    foreach (var type in statusTypes)
                    {
                        ctx.extra[$"StatusBurst_{type}"] = true;
                    }
                }
            };

            string key = $"OnBeforeDamageCalculation_Burst_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key] = handler;
            instance.battleSystem.OnBeforeDamageCalculation += handler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key = $"OnBeforeDamageCalculation_Burst_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key, out object handlerObj) && handlerObj is Action<SkillContext> handler)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler;
                instance.extra.Remove(key);
            }
        }

        public override string GetTooltipDescription()
        {
            if (statusTypes == null || statusTypes.Count == 0) return "";
            string typesStr = string.Join(" and ", statusTypes);
            return $"{typesStr} Duration decreased to 1 and deals its full damage on expiration.";
        }
    }
}
