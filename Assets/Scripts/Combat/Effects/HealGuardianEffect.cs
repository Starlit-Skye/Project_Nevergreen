using System;
using System.Linq;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Calculates healing amount based on the skill parameters, and applies it to the guardian of the targeted character.
    /// If the targeted character is not guarded, this effect does nothing.
    /// </summary>
    [Serializable]
    public class HealGuardianEffect : ISkillEffect
    {
        [Tooltip("Should this effect still attempt healing even if the attack 'Missed'?")]
        public bool ignoreMiss = true;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // Only try to heal if the hit resolved successfully (or we explicitly don't care about misses)
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            CombatCharacter guardianToTarget = null;

            // Friendly actions (buffs, heals, etc.) targeted at Self or Allies do not undergo guard redirection.
            // In these cases, the target parameter remains the original targeted character, behaving as if guard was bypassed.
            bool isGuardRedirectionBypassed = context.bypassGuard || (context.skill != null && context.skill.targetScope != TargetScope.Enemies);

            if (isGuardRedirectionBypassed)
            {
                // 1. If guard was bypassed (either explicitly or due to non-Enemy target scope), 
                // 'target' is the original targeted character. We check if this targeted character is guarded.
                guardianToTarget = GetActiveGuardian(target);
            }
            else
            {
                // 2. If guard was not bypassed, redirection might have occurred.
                // 'target' is the final target (which is the guardian if redirected, or the original target if not).
                // Let's check if 'target' is the guardian of any of the original targets in the context.
                foreach (var origTarget in context.targets)
                {
                    if (GetActiveGuardian(origTarget) == target)
                    {
                        guardianToTarget = target;
                        break;
                    }
                }
            }

            // If the targeted character is not guarded, the skill does nothing.
            if (guardianToTarget == null)
            {
                return;
            }

            // Prevent duplicate healing to the same guardian during this skill execution (handling multi-hit/multi-target)
            string appliedKey = $"HealGuardian_{guardianToTarget.GetHashCode()}_{this.GetHashCode()}";
            if (context.extra.ContainsKey(appliedKey))
            {
                return;
            }
            context.extra[appliedKey] = true;

            // Math Resolution
            CombatConfig config = context.battleSystem != null ? context.battleSystem.combatConfig : null;
            int healAmount = CombatCalculator.CalculateHeal(context, config);

            // Application
            guardianToTarget.Heal(healAmount);

            Debug.Log($"  -> Guardian {guardianToTarget.DisplayName} healed for {healAmount} (HP: {guardianToTarget.currentHP}/{guardianToTarget.baseStats.maxHP})");
        }

        private CombatCharacter GetActiveGuardian(CombatCharacter character)
        {
            if (character == null) return null;

            var guard = character.statusEffects.OfType<GuardStatusInstance>()
                .FirstOrDefault(s => !s.IsExpired);

            if (guard == null || guard.Source == null || !guard.Source.IsAlive || guard.Source.isStunned)
            {
                return null;
            }
            return guard.Source;
        }
    }
}
