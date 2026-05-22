using System;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Removes either all stacks of a specific status effect type, or all status effects from the target.
    /// Removal goes through the character's standard RemoveStatus flow to ensure proper clean up.
    /// </summary>
    [Serializable]
    public class RemoveStatusEffect : ISkillEffect
    {
        [Tooltip("If true, removes all status effects on the target.")]
        public bool removeAll;

        [Tooltip("The specific type of status effect to remove. Only used if removeAll is false.")]
        public StatusType targetStatusType;

        [Tooltip("Should this effect still attempt removal even if the attack 'Missed'?")]
        public bool ignoreMiss = false;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // Only try to apply if the earlier damage phase succeeded (or we ignore misses / standalone utility)
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            // Deduplicate per target per skill context invocation to avoid redundant calculations or log spam on multi-hits
            string cacheKey = $"RemoveStatus_{this.GetHashCode()}_{target.GetInstanceID()}";
            if (context.extra.ContainsKey(cacheKey))
            {
                return;
            }
            context.extra[cacheKey] = true;

            // Select status effects to remove
            var toRemove = target.statusEffects
                .Where(s => removeAll || s.type == targetStatusType)
                .ToList();

            if (toRemove.Count == 0)
            {
                return;
            }

            Debug.Log($"[RemoveStatusEffect] Removing {toRemove.Count} status effect(s) from {target.DisplayName} (removeAll: {removeAll}, targetStatusType: {targetStatusType}).");

            foreach (var status in toRemove)
            {
                target.RemoveStatus(status);
            }
        }
    }
}
