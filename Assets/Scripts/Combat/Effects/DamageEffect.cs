using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Evaluates if an attack hits, calculates damage using CombatCalculator, and applies it to the target.
    /// Also records the hit/miss result in the context for downstream effects to read.
    /// </summary>
    [Serializable]
    public class DamageEffect : ISkillEffect
    {
        public void Execute(SkillContext context, CombatCharacter target)
        {
            // 1. Resolve Hit Check
            context.EnsureHitResolved(target);
            bool didHit = context.didHit;

            if (didHit)
            {
                // Trigger per-target event and allow strategies to mutate context
                float originalMultiplier = context.damageMultiplier;
                context.battleSystem?.TriggerBeforeDamageCalculationPerTarget(context, target);

                // 2. Math Resolution
                int damage = CombatCalculator.CalculateDamage(context, GameDatabase.Instance.CombatConfig);
                context.calculatedValue += damage;
                
                // Restore original multiplier to prevent pollution across targets in AOE attacks
                context.damageMultiplier = originalMultiplier;
                
                // 3. Application
                target.TakeDamage(damage, context.isCritical);

                string critStr = context.isCritical ? " CRIT!" : "";
                Debug.Log($"  -> {target.DisplayName} takes {damage} damage{critStr} (HP: {target.currentHP}/{target.baseStats.maxHP})");
            }
            else
            {
                Debug.Log($"  -> MISS on {target.DisplayName}! (accuracy: {context.finalAccuracy:F0}%)");
            }
        }
    }
}
