using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Deals direct damage to the skill user (self-damage).
    /// Prevents duplicate application during multi-hit or multi-target skill execution using the SkillContext.
    /// </summary>
    [Serializable]
    public class SelfDamageEffect : ISkillEffect
    {
        [Tooltip("The amount of damage to deal. If isPercentageOfMaxHP is true, this is a percentage (e.g., 10 for 10%).")]
        public int damageAmount = 10;

        [Tooltip("If true, damageAmount is treated as a percentage of the user's max HP.")]
        public bool isPercentageOfMaxHP = false;

        [Tooltip("Should this effect still attempt application even if the attack 'Missed'? Default true for self-damage costs.")]
        public bool ignoreMiss = true;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // If the attack missed and we don't ignore misses, do nothing
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            // Ensure we only apply this effect once per skill execution (handling multi-hit/multi-target)
            string appliedKey = $"SelfDamageApplied_{this.GetHashCode()}";
            if (context.extra.ContainsKey(appliedKey))
            {
                return;
            }
            context.extra[appliedKey] = true;

            int finalDamage = damageAmount;
            if (isPercentageOfMaxHP)
            {
                finalDamage = Mathf.RoundToInt(context.user.baseStats.maxHP * (damageAmount / 100f));
            }

            // Apply damage to self
            context.user.TakeDamage(finalDamage, false); // self damage doesn't crit
            Debug.Log($"  -> {context.user.DisplayName} took {finalDamage} self-damage (HP: {context.user.currentHP}/{context.user.baseStats.maxHP})");
        }
    }
}
