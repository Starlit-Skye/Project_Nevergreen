using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Calculates healing amount based on user's attack power and target's missing HP, then applies it.
    /// Heals are considered to "always hit" unless dodge rules are explicitly requested in the future.
    /// </summary>
    [Serializable]
    public class HealEffect : ISkillEffect
    {
        public void Execute(SkillContext context, CombatCharacter target)
        {
            // Assume heals always hit (or define your "ally miss" rules here if needed)
            context.EnsureHitResolved(target);

            // 1. Math Resolution
            CombatConfig config = GameDatabase.Instance.CombatConfig;
            int healAmount = CombatCalculator.CalculateHeal(context, config);
            
            // Check for per-target "Heal Received" bonuses stored by traits in the context
            string key = $"HealReceived_{target.GetInstanceID()}";
            if (context.extra.TryGetValue(key, out object bonusObj) && bonusObj is float bonusPercent)
            {
                healAmount = Mathf.RoundToInt(healAmount * (1f + bonusPercent));
            }

            // 2. Application
            context.calculatedValue += healAmount;
            target.Heal(healAmount);

            Debug.Log($"  -> {target.DisplayName} healed for {healAmount} (HP: {target.currentHP}/{target.baseStats.maxHP})");
        }
    }
}
