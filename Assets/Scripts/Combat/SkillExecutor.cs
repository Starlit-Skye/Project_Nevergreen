using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Executes combat skills, orchestrates hit/damage calculations, and queues animations.
    /// </summary>
    public class SkillExecutor
    {
        public event Action<SkillContext> OnBeforeDamageCalculation;
        public event Action<SkillContext, CombatCharacter> OnBeforeDamageCalculationPerTarget;
        public event Action<CombatCharacter, SkillData, SkillContext> OnActionResolved;

        private AnimationQueueProcessor _animationQueue;
        private EnemySkillBanner _enemySkillBanner;
        private BattleSystem _battleSystem;

        public void Initialize(AnimationQueueProcessor animationQueue, EnemySkillBanner enemySkillBanner, BattleSystem battleSystem)
        {
            _animationQueue = animationQueue;
            _enemySkillBanner = enemySkillBanner;
            _battleSystem = battleSystem;
        }

        public void TriggerBeforeDamageCalculationPerTarget(SkillContext ctx, CombatCharacter target)
        {
            OnBeforeDamageCalculationPerTarget?.Invoke(ctx, target);
        }

        public void QueueEnemySkill(CombatCharacter user, SkillData skill, List<CombatCharacter> targets, System.Random rng)
        {
            if (_enemySkillBanner != null && _animationQueue != null && user.team == Team.Enemy)
            {
                _animationQueue.Enqueue(new ActionStep($"{user.DisplayName} Skill Banner Show", () => _enemySkillBanner.Show(skill.displayName)));
                _animationQueue.Enqueue(new WaitTimerStep($"{user.DisplayName} Skill Banner Wait", _enemySkillBanner.AppearDuration));
                _animationQueue.Enqueue(new ActionStep($"{user.DisplayName} Execute {skill.displayName}", () => Execute(user, skill, targets, rng)));
            }
            else
            {
                Execute(user, skill, targets, rng);
            }
        }

        public void Execute(CombatCharacter user, SkillData skill, List<CombatCharacter> targets, System.Random rng)
        {
            user.RecordSkillUse(skill);

            var ctx = new SkillContext(user, skill, targets, _battleSystem, rng);

            // Allow statuses to modify the skill context
            foreach (var status in user.statusEffects.ToList())
            {
                if (!status.IsExpired)
                {
                    status.OnSkillExecute(ctx);
                }
            }

            Debug.Log($"[SkillExecutor] {user.DisplayName} uses {skill.displayName}" +
                      $" on {string.Join(", ", targets.Select(t => t.DisplayName))}");

            // Create parallel container for simultaneous skill animations
            ParallelStep skillAnimParallel = null;
            if (_animationQueue != null)
            {
                skillAnimParallel = new ParallelStep($"{user.DisplayName}:{skill.displayName}");

                if (skill.sfx != null)
                {
                    skillAnimParallel.AddStep(new PlaySoundStep(skill.sfx));
                }

                if (user.animator != null)
                {
                    string stateName;
                    float duration = 1.0f;

                    if (skill.animationClip != null)
                    {
                        stateName = skill.animationClip.name;
                        if (skill.animationClip.length > 0f)
                        {
                            duration = skill.animationClip.length;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[SkillExecutor] Skill '{skill.displayName}' ({skill.skillId}) has no AnimationClip assigned on {user.DisplayName}! Falling back to generic animation.");
                        stateName = (skill.targetScope == TargetScope.Self || skill.targetScope == TargetScope.Allies)
                            ? "Cast"
                            : "Attack";
                    }

                    skillAnimParallel.AddStep(new AnimatorStep($"{user.DisplayName}:{skill.displayName}_act", user.animator, stateName, duration));
                }
                else
                {
                    // Fallback
                    skillAnimParallel.AddStep(new WaitTimerStep($"{user.DisplayName}:{skill.displayName}_wait", 1.0f));
                }

                // Enqueue parallel group now (it will gather steps before starting next frame)
                _animationQueue.Enqueue(skillAnimParallel);
            }

            if (_animationQueue != null)
            {
                _animationQueue.BeginBatch($"{user.DisplayName}:{skill.displayName}_UI_Batch");
            }

            OnBeforeDamageCalculation?.Invoke(ctx);

            for (int hit = 0; hit < ctx.totalHits; hit++)
            {
                ctx.currentHitIndex = hit;

                foreach (var target in targets)
                {
                    if (!target.IsAlive && !target.IsPile) continue;

                    // 1. Resolve Target
                    CombatCharacter finalTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
                    ctx.primaryTarget = finalTarget;

                    // Reset accumulated value for this target/hit iteration
                    ctx.calculatedValue = 0;

                    // Roll crit once per target (only for damage skills)
                    if (skill.modifier.IsDamage)
                    {
                        CombatStats userStats = ctx.user.GetEffectiveStats();
                        float critChance = userStats.critChance + skill.modifier.criticalMod;
                        ctx.isCritical = (float)ctx.rng.NextDouble() * 100f < critChance;
                    }

                    // The pure strategy approach: Execute modular effects
                    foreach (var effect in skill.effects)
                    {
                        if (effect != null)
                        {
                            effect.Execute(ctx, finalTarget);
                        }
                    }

                    // Check if taking damage killed the target or if we need to do UI syncing post-hit
                    bool isStatusSkill = !skill.modifier.IsDamage && !skill.modifier.IsHeal;
                    bool isSameTeamStatus = isStatusSkill && (user.team == finalTarget.team);

                    if (ctx.didHit && !skill.modifier.IsHeal && !isSameTeamStatus && skillAnimParallel != null)
                    {
                        // The Guardian always flinches and takes the effects
                        if (finalTarget.animator != null)
                        {
                            skillAnimParallel.AddStep(new AnimatorStep($"hit_{finalTarget.DisplayName}", finalTarget.animator, finalTarget.TakeDamageStateName, finalTarget.TakeDamageClipDuration));
                        }

                        // The Protected ally also flinches if they were the original target
                        if (finalTarget != target && target.animator != null)
                        {
                            skillAnimParallel.AddStep(new AnimatorStep($"guard_flinch_{target.DisplayName}", target.animator, target.TakeDamageStateName, target.TakeDamageClipDuration));
                        }
                    }

                    // Note: Event emission here could be tied to context data at the end of the effect resolution.
                    // For the sake of the combat ui prototype reacting, we synthesize the event.
                    OnActionResolved?.Invoke(user, skill, ctx);
                }
            }

            if (_animationQueue != null)
            {
                _animationQueue.EndBatch();
            }

            // --- Riposte Trigger Check ---
            // Only trigger if this is a hostile skill and NOT already a Riposte counter-attack
            if (skill.skillId != "riposte_counter" && skill.targetScope == TargetScope.Enemies)
            {
                var uniqueAttacked = new HashSet<CombatCharacter>();
                for (int hit = 0; hit < ctx.totalHits; hit++)
                {
                    foreach (var target in targets)
                    {
                        if (!target.IsAlive && !target.IsPile) continue;
                        CombatCharacter finalTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
                        if (finalTarget != null && finalTarget.IsAlive)
                        {
                            uniqueAttacked.Add(finalTarget);
                        }
                    }
                }

                if (user.IsAlive)
                {
                    foreach (var riposter in uniqueAttacked)
                    {
                        var riposteStatus = riposter.statusEffects.FirstOrDefault(s => s.type == StatusType.Riposte && !s.IsExpired);
                        if (riposteStatus != null && !riposter.isStunned)
                        {
                            ExecuteRiposteCounter(riposter, user, riposteStatus.amplitude, rng);
                        }
                    }
                }
            }
        }

        private void ExecuteRiposteCounter(CombatCharacter riposter, CombatCharacter target, int amplitude, System.Random rng)
        {
            var riposteSkill = ScriptableObject.CreateInstance<SkillData>();
            riposteSkill.skillId = "riposte_counter";
            riposteSkill.displayName = "Riposte Counter";
            riposteSkill.modifier = new SkillModifier
            {
                damagePercent = amplitude / 100f
            };
            riposteSkill.targetScope = TargetScope.Enemies;
            riposteSkill.effects.Add(new DamageEffect());

            Debug.Log($"[SkillExecutor] {riposter.DisplayName} counter-attacks {target.DisplayName} with {amplitude}% amplitude!");

            Execute(riposter, riposteSkill, new List<CombatCharacter> { target }, rng);
        }
    }
}
