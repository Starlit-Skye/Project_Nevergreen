using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    public enum ConditionSource
    {
        Target,
        Self
    }

    /// <summary>
    /// Evaluates if an attack hits. Checks if the target (or self) has a specified status effect.
    /// If they do, increases the skill's damage scaling (skillScaling) in the context by a designer-specified amount.
    /// After increasing the scaling, calculates and deals the damage, and then restores the original scaling.
    /// </summary>
    [Serializable]
    public class ConditionalDamageEffect : ISkillEffect
    {
        [Tooltip("Who should be checked for the required status.")]
        public ConditionSource conditionSource = ConditionSource.Target;

        [Tooltip("The status type required to trigger the scaling boost.")]
        public StatusType requiredStatus = StatusType.Mark;

        [Tooltip("The bonus damage scaling added if the target has the status (e.g., 0.5 for +50% scaling).")]
        public float bonusScaling = 0.5f;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // 1. Resolve Hit Check
            context.EnsureHitResolved(target);
            bool didHit = context.didHit;

            if (didHit)
            {
                // Check if the specified character (target or user) has the status effect and it is active (not expired)
                bool hasStatus = false;
                CombatCharacter checkCharacter = (conditionSource == ConditionSource.Self) ? context.user : target;
                if (checkCharacter != null && checkCharacter.statusEffects != null)
                {
                    foreach (var status in checkCharacter.statusEffects)
                    {
                        if (status.type == requiredStatus && !status.IsExpired)
                        {
                            hasStatus = true;
                            break;
                        }
                    }
                }

                float originalScaling = context.skillScaling;
                if (hasStatus)
                {
                    context.skillScaling += bonusScaling;
                }

                try
                {
                    // 2. Math Resolution
                    int damage = CombatCalculator.CalculateDamage(context, GameDatabase.Instance.CombatConfig);
                    context.calculatedValue += damage;
                    
                    // 3. Application
                    target.TakeDamage(damage, context.isCritical);

                    string critStr = context.isCritical ? " CRIT!" : "";
                    string boostStr = hasStatus ? $" [BOOSTED by required status: {requiredStatus}]" : "";
                    Debug.Log($"  -> {target.DisplayName} takes {damage} damage{critStr}{boostStr} (HP: {target.currentHP}/{target.baseStats.maxHP})");
                }
                finally
                {
                    // Restore original scaling so it doesn't leak to subsequent hits/targets
                    context.skillScaling = originalScaling;
                }
            }
            else
            {
                Debug.Log($"  -> MISS on {target.DisplayName}! (accuracy: {context.finalAccuracy:F0}%)");
            }
        }
    }
}
