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
            CombatConfig config = context.battleSystem != null ? context.battleSystem.combatConfig : null;
            int healAmount = CombatCalculator.CalculateHeal(context, config);
            
            // 2. Application
            target.Heal(healAmount);

            Debug.Log($"  -> {target.DisplayName} healed for {healAmount} (HP: {target.currentHP}/{target.baseStats.maxHP})");
        }
    }
}
