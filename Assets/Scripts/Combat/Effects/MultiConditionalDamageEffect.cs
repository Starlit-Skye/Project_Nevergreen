using System;
using System.Collections.Generic;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    [Serializable]
    public class DamageCondition
    {
        [Tooltip("Who should be checked for the required status.")]
        public ConditionSource conditionSource = ConditionSource.Target;

        [Tooltip("The status type required to trigger the scaling boost.")]
        public StatusType requiredStatus = StatusType.Mark;

        [Tooltip("The bonus damage scaling added if the character has the status (e.g., 0.5 for +50% scaling).")]
        public float bonusScaling = 0.5f;
    }

    /// <summary>
    /// Evaluates if an attack hits. Checks a list of conditions and adds bonus scaling for each one met.
    /// After increasing the scaling, calculates and deals the damage, and then restores the original scaling.
    /// This prevents skills with multiple conditions from hitting the target multiple times.
    /// </summary>
    [Serializable]
    public class MultiConditionalDamageEffect : ISkillEffect
    {
        public List<DamageCondition> conditions = new List<DamageCondition>();

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // 1. Resolve Hit Check
            context.EnsureHitResolved(target);
            bool didHit = context.didHit;

            if (didHit)
            {
                float totalBonusScaling = 0f;
                List<string> metConditions = new List<string>();

                foreach (var condition in conditions)
                {
                    bool hasStatus = false;
                    CombatCharacter checkCharacter = (condition.conditionSource == ConditionSource.Self) ? context.user : target;
                    if (checkCharacter != null && checkCharacter.statusEffects != null)
                    {
                        foreach (var status in checkCharacter.statusEffects)
                        {
                            if (status.type == condition.requiredStatus && !status.IsExpired)
                            {
                                hasStatus = true;
                                break;
                            }
                        }
                    }

                    if (hasStatus)
                    {
                        totalBonusScaling += condition.bonusScaling;
                        metConditions.Add(condition.requiredStatus.ToString());
                    }
                }

                float originalScaling = context.skillScaling;
                if (totalBonusScaling > 0f)
                {
                    context.skillScaling += totalBonusScaling;
                }

                try
                {
                    // 2. Math Resolution
                    int damage = CombatCalculator.CalculateDamage(context, GameDatabase.Instance.CombatConfig);
                    context.calculatedValue += damage;
                    
                    // 3. Application
                    target.TakeDamage(damage, context.isCritical);

                    string critStr = context.isCritical ? " CRIT!" : "";
                    string boostStr = metConditions.Count > 0 ? $" [BOOSTED by required status(es): {string.Join(", ", metConditions)}]" : "";
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