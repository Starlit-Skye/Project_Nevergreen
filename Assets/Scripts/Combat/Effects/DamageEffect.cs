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
            bool didHit = CombatCalculator.ResolveHit(context, target, context.battleSystem.combatConfig);

            if (didHit)
            {
                // 2. Math Resolution
                int damage = CombatCalculator.CalculateDamage(context, context.battleSystem.combatConfig);
                
                // 3. Application
                target.TakeDamage(damage);

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
