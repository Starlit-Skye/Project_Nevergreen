using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class GuardTests
    {
        private CombatConfig config;
        private CombatCharacter guardian;
        private CombatCharacter target;
        private CombatCharacter attacker;
        private System.Random rng;

        [SetUp]
        public void Setup()
        {
            config = CombatTestHelper.CreateDefaultConfig();
            rng = CombatTestHelper.CreateFixedRng(42);

            guardian = CombatTestHelper.CreateCombatCharacter("Guardian", Team.Player, 1, maxHP: 100);
            target = CombatTestHelper.CreateCombatCharacter("Target", Team.Player, 2, maxHP: 50);
            attacker = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Enemy, 1, attack: 20);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(guardian.gameObject);
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(attacker.gameObject);
            ScriptableObject.DestroyImmediate(config);
        }

        private (BattleSystem bs, SkillContext ctx) MakeCtx(
            CombatCharacter attacker, CombatCharacter target, SkillData skill, System.Random rng)
        {
            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();
            bs.combatConfig = config;
            var ctx = new SkillContext(attacker, skill, new System.Collections.Generic.List<CombatCharacter> { target }, bs, rng);
            return (bs, ctx);
        }

        [Test]
        public void Redirection_DamageIsRedirectedToGuardian()
        {
            // Apply guard
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            // Create damage skill
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.guaranteedHit = true;
            skill.modifier.damagePercent = 1.0f;
            skill.effects.Add(new DamageEffect());

            var (bs, ctx) = MakeCtx(attacker, target, skill, rng);

            // Using CombatCalculator logic to get effective target
            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);

            Assert.AreEqual(guardian, effectiveTarget, "Effective target should be the guardian.");

            // Manually execute what BattleSystem would do
            ctx.primaryTarget = effectiveTarget;
            foreach (var effect in skill.effects)
            {
                effect.Execute(ctx, effectiveTarget);
            }

            Assert.AreEqual(50, target.currentHP, "Target should take no damage.");
            Assert.Less(guardian.currentHP, 100, "Guardian should take damage.");
            
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void ImmediateBreak_StunOnGuardianRemovesGuard()
        {
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            Assert.AreEqual(1, target.statusEffects.Count, "Target should have guard status.");

            // Apply stun to guardian
            var stunStatus = new StatusEffectInstance(StatusType.Stun, 1, 1);
            guardian.AddStatus(stunStatus);
            guardian.TriggerStatusApplied(StatusType.Stun, true); // Simulate StunEffect executing

            Assert.AreEqual(0, target.statusEffects.Count, "Guard status should be removed when guardian is stunned.");
        }

        [Test]
        public void LastInWins_ApplyingNewGuardRemovesOldGuard()
        {
            var oldGuardian = CombatTestHelper.CreateCombatCharacter("OldGuardian", Team.Player, 3);
            
            target.AddStatus(new GuardStatusInstance(oldGuardian, 3));
            Assert.AreEqual(1, target.statusEffects.Count);
            Assert.AreEqual(oldGuardian, target.statusEffects[0].Source);

            // Apply new guard
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            Assert.AreEqual(1, target.statusEffects.Count, "Should only have one guard status.");
            Assert.AreEqual(guardian, target.statusEffects[0].Source, "Guardian should be the new source.");

            Object.DestroyImmediate(oldGuardian.gameObject);
        }

        [Test]
        public void NestedGuard_ApplyingGuardToGuardianBreaksTheirMaintainedGuards()
        {
            var grandGuardian = CombatTestHelper.CreateCombatCharacter("GrandGuardian", Team.Player, 3);

            // guardian guards target
            target.AddStatus(new GuardStatusInstance(guardian, 3));
            Assert.AreEqual(1, target.statusEffects.Count);

            // grandGuardian guards guardian
            guardian.AddStatus(new GuardStatusInstance(grandGuardian, 3));

            // Due to Nested Guarding rule, guardian should break its guard on target
            Assert.AreEqual(0, target.statusEffects.Count, "Guard on target should be broken.");
            Assert.AreEqual(1, guardian.statusEffects.Count, "Guardian should still have guard from GrandGuardian.");

            Object.DestroyImmediate(grandGuardian.gameObject);
        }

        [Test]
        public void AOEBypass_GuardIgnoredIfGuardianAlsoTargeted()
        {
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.guaranteedHit = true;
            
            // Skill targets both Target and Guardian (AOE)
            var targets = new System.Collections.Generic.List<CombatCharacter> { target, guardian };
            var ctx = new SkillContext(attacker, skill, targets, null, rng);

            // Evaluate target
            CombatCharacter effectiveTargetForTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
            
            Assert.AreEqual(target, effectiveTargetForTarget, "Guard should be bypassed during AOE.");
        }

        [Test]
        public void Bypass_SkillBypassFlag_GuardIgnored()
        {
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.bypassGuard = true;

            var ctx = new SkillContext(attacker, skill, new System.Collections.Generic.List<CombatCharacter> { target }, null, rng);
            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);

            Assert.AreEqual(target, effectiveTarget, "Guard should be ignored if skill has bypassGuard flag.");
        }

        [Test]
        public void NoRedirection_AllyTargetSkill_GuardIgnored()
        {
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            var skill = CombatTestHelper.CreateDamageSkill(); // Using damage skill for test but setting scope to Allies
            skill.targetScope = TargetScope.Allies;

            // Player character casting on ally
            var ctx = new SkillContext(guardian, skill, new System.Collections.Generic.List<CombatCharacter> { target }, null, rng);
            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);

            Assert.AreEqual(target, effectiveTarget, "Guard should NOT redirect ally-targeted skills (buffs/heals).");
        }

        [Test]
        public void ImmediateBreak_GuardianDeath_GuardRemoved()
        {
            target.AddStatus(new GuardStatusInstance(guardian, 3));
            Assert.AreEqual(1, target.statusEffects.Count);

            // Kill guardian
            guardian.TakeDamage(1000);
            
            Assert.AreEqual(0, target.statusEffects.Count, "Guard status should be removed when guardian is defeated.");
        }

        [Test]
        public void ImmediateBreak_TargetDeath_GuardRemoved()
        {
            target.AddStatus(new GuardStatusInstance(guardian, 3));
            Assert.AreEqual(1, target.statusEffects.Count);

            // Kill target
            target.TakeDamage(1000);

            Assert.AreEqual(0, target.statusEffects.Count, "Guard status should be removed when protected character is defeated.");
        }

        [Test]
        public void Application_FailsIfGuardianIsAlreadyStunned()
        {
            // Stun the guardian first
            var stunStatus = new StatusEffectInstance(StatusType.Stun, 1, 1);
            guardian.AddStatus(stunStatus);
            
            // Apply guard
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            // It should either not apply, or immediately break.
            Assert.AreEqual(0, target.statusEffects.Count, "Guard should not be applied or should immediately break if the guardian is already stunned.");
        }

        [Test]
        public void Expiry_GuardRemovedAfterDurationTicks()
        {
            var guard = new GuardStatusInstance(guardian, 1); // 1 turn duration
            target.AddStatus(guard);

            Assert.AreEqual(1, target.statusEffects.Count);

            // Tick durations using StatusProcessor (simulating turn pass for target)
            StatusProcessor.TickDurations(target, 0);

            Assert.AreEqual(0, target.statusEffects.Count, "Guard should be removed after its duration expires.");
        }

        [Test]
        public void Application_FailsIfTargetIsPile()
        {
            // Arrange
            target.state = LifeState.Pile;

            // Act
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            // Assert
            Assert.AreEqual(0, target.statusEffects.Count, "Guard should not be applied to a Pile.");
        }

        [Test]
        public void Application_FailsIfGuardianIsPile()
        {
            // Arrange
            guardian.state = LifeState.Pile;

            // Act
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            // Assert
            Assert.AreEqual(0, target.statusEffects.Count, "Guard should not be applied if the guardian is a Pile.");
        }

        [Test]
        public void TargetGuardsUserEffect_AppliesGuardToUser()
        {
            var effect = new TargetGuardsUserEffect
            {
                applicationChance = 100f,
                duration = 3,
                ignoreMiss = true
            };

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.effects.Add(effect);

            var (bs, ctx) = MakeCtx(attacker, target, skill, rng);

            ctx.primaryTarget = target;
            effect.Execute(ctx, target);

            Assert.AreEqual(1, attacker.statusEffects.Count, "Attacker (user) should be guarded.");
            Assert.AreEqual(target, attacker.statusEffects[0].Source, "Target should be the guardian.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void ApplyStatusToGuardianEffect_AppliesStatusToGuardian_WhenRedirectionOccurs()
        {
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            var effect = new ApplyStatusToGuardianEffect
            {
                statusType = StatusType.Bleed,
                amplitude = 5,
                duration = 2,
                applicationChance = 100f,
                ignoreMiss = true
            };

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.effects.Add(effect);

            var (bs, ctx) = MakeCtx(attacker, target, skill, rng);

            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
            Assert.AreEqual(guardian, effectiveTarget, "Redirection should resolve to the guardian.");

            ctx.primaryTarget = effectiveTarget;
            effect.Execute(ctx, effectiveTarget);

            var bleedStatus = guardian.statusEffects.FirstOrDefault(s => s.type == StatusType.Bleed);
            Assert.IsNotNull(bleedStatus, "Guardian should have Bleed status.");
            Assert.AreEqual(5, bleedStatus.amplitude, "Bleed amplitude should match.");
            Assert.AreEqual(2, bleedStatus.remainingDuration, "Bleed duration should match.");

            Assert.IsFalse(target.statusEffects.Any(s => s.type == StatusType.Bleed), "Target should not have Bleed status.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void ApplyStatusToGuardianEffect_AppliesStatusToGuardian_WhenGuardBypassed()
        {
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            var effect = new ApplyStatusToGuardianEffect
            {
                statusType = StatusType.Bleed,
                amplitude = 5,
                duration = 2,
                applicationChance = 100f,
                ignoreMiss = true
            };

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.bypassGuard = true;
            skill.effects.Add(effect);

            var (bs, ctx) = MakeCtx(attacker, target, skill, rng);

            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
            Assert.AreEqual(target, effectiveTarget, "Effective target should be target since guard is bypassed.");

            ctx.primaryTarget = effectiveTarget;
            effect.Execute(ctx, effectiveTarget);

            var bleedStatus = guardian.statusEffects.FirstOrDefault(s => s.type == StatusType.Bleed);
            Assert.IsNotNull(bleedStatus, "Guardian should have Bleed status.");
            Assert.AreEqual(5, bleedStatus.amplitude);

            Assert.IsFalse(target.statusEffects.Any(s => s.type == StatusType.Bleed), "Target should not have Bleed status.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void ApplyStatusToGuardianEffect_DoesNothing_WhenTargetNotGuarded()
        {
            var effect = new ApplyStatusToGuardianEffect
            {
                statusType = StatusType.Bleed,
                amplitude = 5,
                duration = 2,
                applicationChance = 100f,
                ignoreMiss = true
            };

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.effects.Add(effect);

            var (bs, ctx) = MakeCtx(attacker, target, skill, rng);

            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
            ctx.primaryTarget = effectiveTarget;
            effect.Execute(ctx, effectiveTarget);

            Assert.IsFalse(target.statusEffects.Any(s => s.type == StatusType.Bleed));
            Assert.IsFalse(guardian.statusEffects.Any(s => s.type == StatusType.Bleed));

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void ApplyStatusToGuardianEffect_AppliesStatusToGuardian_WhenTargetIsSelfAndGuarded()
        {
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            var effect = new ApplyStatusToGuardianEffect
            {
                statusType = StatusType.Buff,
                targetStat = StatTarget.Defense,
                amplitude = 20,
                duration = 3,
                applicationChance = 100f,
                ignoreMiss = true
            };

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.targetScope = TargetScope.Self;
            skill.effects.Add(effect);

            var (bs, ctx) = MakeCtx(target, target, skill, rng);

            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
            Assert.AreEqual(target, effectiveTarget, "Should NOT redirect self-targeted skill.");

            ctx.primaryTarget = effectiveTarget;
            effect.Execute(ctx, effectiveTarget);

            var buffStatus = guardian.statusEffects.FirstOrDefault(s => s.type == StatusType.Buff);
            Assert.IsNotNull(buffStatus, "Guardian should have Buff status.");
            Assert.AreEqual(20, buffStatus.amplitude);
            Assert.AreEqual(StatTarget.Defense, buffStatus.targetStat);

            Assert.IsFalse(target.statusEffects.Any(s => s.type == StatusType.Buff), "Target should not have Buff status.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void HealGuardianEffect_HealsGuardian_WhenTargetIsSelfAndGuarded()
        {
            guardian.TakeDamage(50);
            Assert.AreEqual(50, guardian.currentHP);
            target.AddStatus(new GuardStatusInstance(guardian, 3));

            var effect = new HealGuardianEffect { ignoreMiss = true };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "test_heal_guardian";
            skill.modifier = new SkillModifier
            {
                damagePercent = 0f,
                healPercent = 0.5f
            };
            skill.targetScope = TargetScope.Self;
            skill.effects.Add(effect);

            var (bs, ctx) = MakeCtx(target, target, skill, rng);

            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
            Assert.AreEqual(target, effectiveTarget);

            ctx.primaryTarget = effectiveTarget;
            effect.Execute(ctx, effectiveTarget);

            Assert.Greater(guardian.currentHP, 50, "Guardian should be healed.");
            Assert.AreEqual(50, target.currentHP, "Target (user) should not be healed.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void HealGuardianEffect_DoesNothing_WhenTargetNotGuarded()
        {
            guardian.TakeDamage(50);
            Assert.AreEqual(50, guardian.currentHP);

            var effect = new HealGuardianEffect { ignoreMiss = true };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "test_heal_guardian";
            skill.modifier = new SkillModifier
            {
                damagePercent = 0f,
                healPercent = 0.5f
            };
            skill.targetScope = TargetScope.Self;
            skill.effects.Add(effect);

            var (bs, ctx) = MakeCtx(target, target, skill, rng);

            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
            ctx.primaryTarget = effectiveTarget;
            effect.Execute(ctx, effectiveTarget);

            Assert.AreEqual(50, guardian.currentHP, "Guardian should not be healed if target is not guarded.");

            Object.DestroyImmediate(bs.gameObject);
        }
    }
}
