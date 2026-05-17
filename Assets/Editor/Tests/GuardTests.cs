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
    }
}
