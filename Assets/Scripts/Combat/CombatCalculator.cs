using Nevergreen.Data;
using UnityEngine;
using System.Linq;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Static utility for combat math. Implements formulas from GDD.
    /// </summary>
    public static class CombatCalculator
    {
        /// <summary>
        /// Roll attack damage: round_to_int(base_attack * random_uniform(0.8, 1.2))
        /// </summary>
        public static int RollAttackDamage(int baseAttack, CombatConfig config, System.Random rng)
        {
            float roll = (float)(config.attackRollMin +
                                  rng.NextDouble() * (config.attackRollMax - config.attackRollMin));
            return Mathf.RoundToInt(baseAttack * roll);
        }

        /// <summary>
        /// Calculate damage after skill scaling, crit, and defense.
        /// </summary>
        public static int CalculateDamage(SkillContext ctx, CombatConfig config)
        {
            CombatStats userStats = ctx.user.GetEffectiveStats();

            // Base roll
            ctx.baseAttackRoll = RollAttackDamage(userStats.attack, config, ctx.rng);

            // Skill scaling (damage percent is like 1.0 = 100%)
            int scaled = Mathf.RoundToInt(ctx.baseAttackRoll * ctx.skillScaling);

            // Apply damage multiplier from buffs/effects
            scaled = Mathf.RoundToInt(scaled * ctx.damageMultiplier);

            // Crit check
            float critChance = userStats.critChance + ctx.skill.modifier.criticalMod;
            ctx.isCritical = (float)ctx.rng.NextDouble() * 100f < critChance;

            if (ctx.isCritical)
            {
                scaled = Mathf.RoundToInt(scaled * ctx.critMultiplier);
            }

            // Defense reduction (unless ignoresDefense)
            if (!ctx.ignoresDefense && ctx.primaryTarget != null)
            {
                CombatStats targetStats = ctx.primaryTarget.GetEffectiveStats();
                float reduction = 1f - (targetStats.defense / 100f);
                scaled = Mathf.RoundToInt(scaled * reduction);
            }

            ctx.calculatedValue = Mathf.Max(0, scaled);
            return ctx.calculatedValue;
        }

        /// <summary>
        /// Calculate healing amount.
        /// </summary>
        public static int CalculateHeal(SkillContext ctx)
        {
            CombatStats userStats = ctx.user.GetEffectiveStats();
            int baseHeal = Mathf.RoundToInt(userStats.attack * ctx.skillScaling);
            ctx.calculatedValue = Mathf.Max(0, baseHeal);
            return ctx.calculatedValue;
        }

        /// <summary>
        /// Check if attack hits: final_hit_chance = min(95, accuracy - dodge)
        /// </summary>
        public static bool ResolveHit(SkillContext ctx, CombatCharacter target, CombatConfig config)
        {
            if (ctx.guaranteedHit)
            {
                ctx.didHit = true;
                ctx.finalAccuracy = 100f;
                return true;
            }

            CombatStats userStats = ctx.user.GetEffectiveStats();
            CombatStats targetStats = target.GetEffectiveStats();

            float accuracy = userStats.accuracy + ctx.skill.modifier.accuracyMod;
            float dodge = ctx.ignoresDodge ? 0f : targetStats.dodge;
            float hitChance = Mathf.Min(config.accuracyCap, accuracy - dodge);

            ctx.finalAccuracy = hitChance;
            float roll = (float)ctx.rng.NextDouble() * 100f;
            ctx.didHit = roll < hitChance;

            return ctx.didHit;
        }

        /// <summary>
        /// Check if a status effect is applied, factoring in resistance.
        /// final_chance = source_chance - target_resistance
        /// </summary>
        public static bool ResolveStatusApplication(float sourceChance, int targetResistance,
                                                     System.Random rng)
        {
            float finalChance = sourceChance - targetResistance;
            if (finalChance <= 0f) return false;

            float roll = (float)rng.NextDouble() * 100f;
            return roll < finalChance;
        }

        public static CombatCharacter GetEffectiveTarget(CombatCharacter target, SkillContext context)
        {
            if (context.bypassGuard) return target;

            // Only redirect hostile actions (targeting Enemies). 
            // Buffs, heals, and other ally-targeted skills bypass guard.
            if (context.skill != null && context.skill.targetScope != TargetScope.Enemies)
                return target;

            // Find the active guard status on the intended target
            var guard = target.statusEffects.OfType<GuardStatusInstance>()
                .FirstOrDefault(s => !s.IsExpired);

            if (guard == null || guard.Source == null || !guard.Source.IsAlive || guard.Source.isStunned) return target;

            // AOE Bypass Check: No redirection if both target and guardian are targeted.
            if (context.targets != null && context.targets.Contains(target) && context.targets.Contains(guard.Source))
                return target;

            return guard.Source;
        }
    }
}
