using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
            CombatTestHelper.InitializeTestDatabase();
            config = CombatTestHelper.CreateDefaultConfig();
            rng = CombatTestHelper.CreateFixedRng(42);

            guardian = CombatTestHelper.CreateCombatCharacter("Guardian", Team.Player, 1, maxHP: 100);
            target = CombatTestHelper.CreateCombatCharacter("Target", Team.Player, 2, maxHP: 50);
            attacker = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Enemy, 1, attack: 20);
        }

        [TearDown]
        public void Teardown()
        {
            CombatTestHelper.CleanupTestDatabase();
            Object.DestroyImmediate(guardian.gameObject);
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(attacker.gameObject);
            ScriptableObject.DestroyImmediate(config, true);
        }

        private (BattleSystem bs, SkillContext ctx) MakeCtx(
            CombatCharacter user, CombatCharacter targetChar, SkillData skill, System.Random random)
        {
            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { targetChar }, bs, random);
            return (bs, ctx);
        }

        [Test]
        public void Redirection_DamageIsRedirectedToGuardian()
        {
            // Apply guard status onto the guardian, protecting target
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            var guard = GuardResolver.GetGuardStatusHeldBy(guardian);
            Assert.IsNotNull(guard, "Guardian should hold the Guard status.");
            Assert.AreEqual(target, guard.GuardedTarget, "Guard status should track the protected target.");
            Assert.AreEqual(0, target.statusEffects.Count, "Protected target should not hold the Guard status.");

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
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            Assert.IsNotNull(GuardResolver.GetGuardStatusHeldBy(guardian), "Guardian should hold guard status.");

            // Apply stun to guardian
            var stunStatus = new StatusEffectInstance(StatusType.Stun, 1, 1);
            guardian.AddStatus(stunStatus);
            guardian.TriggerStatusApplied(StatusType.Stun, true); // Simulate StunEffect executing

            Assert.IsNull(GuardResolver.GetGuardStatusHeldBy(guardian), "Guard status should be removed when guardian is stunned.");
            Assert.IsNull(GuardResolver.GetActiveGuardian(target), "Target should no longer have an active guardian.");
        }

        [Test]
        public void LastInWins_ApplyingNewGuardRemovesOldGuard()
        {
            var oldGuardian = CombatTestHelper.CreateCombatCharacter("OldGuardian", Team.Player, 3);

            oldGuardian.AddStatus(new GuardStatusInstance(oldGuardian, target, 3));
            Assert.AreEqual(1, oldGuardian.statusEffects.Count);
            Assert.AreEqual(oldGuardian, GuardResolver.GetActiveGuardian(target));

            // Apply new guard protecting the same target
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            Assert.AreEqual(0, oldGuardian.statusEffects.Count, "Old guardian's guard should be removed.");
            Assert.AreEqual(1, guardian.statusEffects.Count, "New guardian should hold the guard status.");
            Assert.AreEqual(target, GuardResolver.GetGuardStatusHeldBy(guardian).GuardedTarget);
            Assert.AreEqual(guardian, GuardResolver.GetActiveGuardian(target), "New guardian should be the active guardian.");

            Object.DestroyImmediate(oldGuardian.gameObject);
        }

        [Test]
        public void ReGuard_GuardingNewTargetBreaksPreviousGuard()
        {
            var secondTarget = CombatTestHelper.CreateCombatCharacter("SecondTarget", Team.Player, 3, maxHP: 50);

            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));
            Assert.AreEqual(guardian, GuardResolver.GetActiveGuardian(target));

            // Guardian guards a different ally
            guardian.AddStatus(new GuardStatusInstance(guardian, secondTarget, 3));

            var guardStatuses = guardian.statusEffects.OfType<GuardStatusInstance>().ToList();
            Assert.AreEqual(1, guardStatuses.Count, "Guardian should only hold one Guard status at a time.");
            Assert.AreEqual(secondTarget, guardStatuses[0].GuardedTarget, "Guardian should now guard the new target.");
            Assert.IsNull(GuardResolver.GetActiveGuardian(target), "Previous target should no longer be guarded.");
            Assert.AreEqual(guardian, GuardResolver.GetActiveGuardian(secondTarget), "New target should be guarded.");

            Object.DestroyImmediate(secondTarget.gameObject);
        }

        [Test]
        public void NestedGuard_ApplyingGuardToGuardianBreaksTheirMaintainedGuards()
        {
            var grandGuardian = CombatTestHelper.CreateCombatCharacter("GrandGuardian", Team.Player, 3);

            // guardian guards target
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));
            Assert.AreEqual(1, guardian.statusEffects.Count);

            // grandGuardian guards guardian
            grandGuardian.AddStatus(new GuardStatusInstance(grandGuardian, guardian, 3));

            // Due to Mutual Exclusivity rule, guardian should break its guard on target
            Assert.AreEqual(0, guardian.statusEffects.Count, "Guardian's guard on target should be broken.");
            Assert.IsNull(GuardResolver.GetActiveGuardian(target), "Target should no longer be guarded.");
            Assert.AreEqual(1, grandGuardian.statusEffects.Count, "GrandGuardian should hold guard on guardian.");
            Assert.AreEqual(guardian, GuardResolver.GetGuardStatusHeldBy(grandGuardian).GuardedTarget);
            Assert.AreEqual(grandGuardian, GuardResolver.GetActiveGuardian(guardian));

            Object.DestroyImmediate(grandGuardian.gameObject);
        }

        [Test]
        public void MutualExclusivity_ProtectedBecomingGuardianBreaksGuardProtectingThem()
        {
            var grandGuardian = CombatTestHelper.CreateCombatCharacter("GrandGuardian", Team.Player, 3);

            // grandGuardian guards guardian
            grandGuardian.AddStatus(new GuardStatusInstance(grandGuardian, guardian, 3));
            Assert.AreEqual(grandGuardian, GuardResolver.GetActiveGuardian(guardian));

            // guardian now guards target
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            // Mutual Exclusivity (both directions): grandGuardian's guard on guardian must break
            Assert.AreEqual(0, grandGuardian.statusEffects.Count, "Guard protecting the new guardian should be broken.");
            Assert.IsNull(GuardResolver.GetActiveGuardian(guardian), "New guardian should no longer be protected.");
            Assert.AreEqual(1, guardian.statusEffects.Count, "Guardian should hold guard on target.");
            Assert.AreEqual(guardian, GuardResolver.GetActiveGuardian(target), "Target should be guarded by guardian.");

            Object.DestroyImmediate(grandGuardian.gameObject);
        }

        [Test]
        public void CircularGuard_NewestWinsAndBreaksPreviousLoop()
        {
            // guardian guards target
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));
            Assert.AreEqual(guardian, GuardResolver.GetActiveGuardian(target));

            // target guards guardian (circular)
            target.AddStatus(new GuardStatusInstance(target, guardian, 3));

            Assert.AreEqual(0, guardian.statusEffects.Count, "Previous guard in circular pair should be broken.");
            Assert.AreEqual(1, target.statusEffects.Count, "Newest guard should remain active.");
            Assert.AreEqual(guardian, GuardResolver.GetGuardStatusHeldBy(target).GuardedTarget);
            Assert.AreEqual(target, GuardResolver.GetActiveGuardian(guardian));
            Assert.IsNull(GuardResolver.GetActiveGuardian(target));

            // Verify redirection resolves cleanly without infinite loop
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(attacker, skill, new List<CombatCharacter> { guardian }, null, rng);
            Assert.AreEqual(target, CombatCalculator.GetEffectiveTarget(guardian, ctx));
        }

        [Test]
        public void AOEBypass_GuardIgnoredIfGuardianAlsoTargeted()
        {
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.guaranteedHit = true;

            // Skill targets both Target and Guardian (AOE)
            var targets = new List<CombatCharacter> { target, guardian };
            var ctx = new SkillContext(attacker, skill, targets, null, rng);

            // Evaluate target
            CombatCharacter effectiveTargetForTarget = CombatCalculator.GetEffectiveTarget(target, ctx);

            Assert.AreEqual(target, effectiveTargetForTarget, "Guard should be bypassed during AOE.");
        }

        [Test]
        public void Bypass_SkillBypassFlag_GuardIgnored()
        {
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.bypassGuard = true;

            var ctx = new SkillContext(attacker, skill, new List<CombatCharacter> { target }, null, rng);
            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);

            Assert.AreEqual(target, effectiveTarget, "Guard should be ignored if skill has bypassGuard flag.");
        }

        [Test]
        public void NoRedirection_AllyTargetSkill_GuardIgnored()
        {
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            var skill = CombatTestHelper.CreateDamageSkill(); // Using damage skill for test but setting scope to Allies
            skill.targetScope = TargetScope.Allies;

            // Player character casting on ally
            var ctx = new SkillContext(guardian, skill, new List<CombatCharacter> { target }, null, rng);
            CombatCharacter effectiveTarget = CombatCalculator.GetEffectiveTarget(target, ctx);

            Assert.AreEqual(target, effectiveTarget, "Guard should NOT redirect ally-targeted skills (buffs/heals).");
        }

        [Test]
        public void ImmediateBreak_GuardianDeath_GuardRemoved()
        {
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));
            Assert.AreEqual(1, guardian.statusEffects.Count);

            // Kill guardian
            guardian.TakeDamage(1000);

            Assert.AreEqual(0, guardian.statusEffects.Count, "Guard status should be removed when guardian is defeated.");
            Assert.IsNull(GuardResolver.GetActiveGuardian(target));
        }

        [Test]
        public void ImmediateBreak_TargetDeath_GuardRemoved()
        {
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));
            Assert.AreEqual(1, guardian.statusEffects.Count);

            // Kill protected target
            target.TakeDamage(1000);

            Assert.AreEqual(0, guardian.statusEffects.Count, "Guard status should be removed when protected character is defeated.");
            Assert.IsNull(GuardResolver.GetActiveGuardian(target));
        }

        [Test]
        public void Application_FailsIfGuardianIsAlreadyStunned()
        {
            // Stun the guardian first
            var stunStatus = new StatusEffectInstance(StatusType.Stun, 1, 1);
            guardian.AddStatus(stunStatus);

            // Attempt to apply guard
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            Assert.IsNull(GuardResolver.GetGuardStatusHeldBy(guardian), "Guard should not persist if the guardian is already stunned.");
            Assert.IsNull(GuardResolver.GetActiveGuardian(target));
        }

        [Test]
        public void Expiry_GuardRemovedAfterDurationTicks()
        {
            var guard = new GuardStatusInstance(guardian, target, 1); // 1 turn duration
            guardian.AddStatus(guard);

            Assert.AreEqual(1, guardian.statusEffects.Count);

            // Ticking the protected target should NOT tick or expire the guard
            StatusProcessor.TickDurations(target, 0);
            Assert.AreEqual(1, guardian.statusEffects.Count, "Guard duration should not tick on protected target's turn.");

            // Tick durations on guardian (simulating turn pass for guardian)
            StatusProcessor.TickDurations(guardian, 0);

            Assert.AreEqual(0, guardian.statusEffects.Count, "Guard should be removed after its duration expires on guardian's turn.");
            Assert.IsNull(GuardResolver.GetActiveGuardian(target));
        }

        [Test]
        public void Application_FailsIfTargetIsPile()
        {
            // Arrange
            target.state = LifeState.Pile;

            // Act
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            // Assert
            Assert.AreEqual(0, guardian.statusEffects.Count, "Guard should not be applied to protect a Pile.");
        }

        [Test]
        public void Application_FailsIfGuardianIsPile()
        {
            // Arrange
            guardian.state = LifeState.Pile;

            // Act
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            // Assert
            Assert.AreEqual(0, guardian.statusEffects.Count, "Guard should not be applied if the guardian is a Pile.");
        }

        [Test]
        public void Application_FailsWhenGuardingSelf()
        {
            guardian.AddStatus(new GuardStatusInstance(guardian, guardian, 3));

            Assert.AreEqual(0, guardian.statusEffects.Count, "A character cannot guard themselves.");
        }

        [Test]
        public void StatusEffect_Guard_AppliesStatusToUserGuardingTarget()
        {
            var effect = new StatusEffect
            {
                statusType = StatusType.Guard,
                applicationChance = 100f,
                duration = 3,
                ignoreMiss = true
            };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "test_protect";
            skill.targetScope = TargetScope.Allies;
            skill.effects.Add(effect);

            var (bs, ctx) = MakeCtx(guardian, target, skill, rng);
            ctx.primaryTarget = target;

            bool eventFiredOnGuardian = false;
            bool eventFiredOnTarget = false;
            guardian.OnStatusApplied += (c, t, s, _) => { if (t == StatusType.Guard && s) eventFiredOnGuardian = true; };
            target.OnStatusApplied += (c, t, s, _) => { if (t == StatusType.Guard) eventFiredOnTarget = true; };

            effect.Execute(ctx, target);

            var guard = GuardResolver.GetGuardStatusHeldBy(guardian);
            Assert.IsNotNull(guard, "Guardian (skill user) should hold the Guard status.");
            Assert.AreEqual(target, guard.GuardedTarget, "Guard status should track the targeted ally.");
            Assert.AreEqual(3, guard.remainingDuration);
            Assert.AreEqual(0, target.statusEffects.Count, "Protected ally should not hold the Guard status.");
            Assert.IsTrue(eventFiredOnGuardian, "OnStatusApplied should fire on the guardian.");
            Assert.IsFalse(eventFiredOnTarget, "OnStatusApplied should not fire on the protected target.");

            Object.DestroyImmediate(bs.gameObject);
            ScriptableObject.DestroyImmediate(skill, true);
        }

        [Test]
        public void TargetGuardsUserEffect_AppliesGuardToTargetProtectingUser()
        {
            var effect = new TargetGuardsUserEffect
            {
                applicationChance = 100f,
                duration = 3,
                ignoreMiss = true
            };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "test_target_guards_user";
            skill.targetScope = TargetScope.Allies;
            skill.effects.Add(effect);

            // target (user) commands guardian (targeted ally) to guard target
            var (bs, ctx) = MakeCtx(target, guardian, skill, rng);

            ctx.primaryTarget = guardian;
            effect.Execute(ctx, guardian);

            var guard = GuardResolver.GetGuardStatusHeldBy(guardian);
            Assert.IsNotNull(guard, "Targeted ally (guardian) should hold the Guard status.");
            Assert.AreEqual(guardian, guard.Source, "Guardian should be the Source.");
            Assert.AreEqual(target, guard.GuardedTarget, "User should be the GuardedTarget.");
            Assert.AreEqual(0, target.statusEffects.Count, "User (protected) should not hold the Guard status.");
            Assert.AreEqual(guardian, GuardResolver.GetActiveGuardian(target, ctx));

            Object.DestroyImmediate(bs.gameObject);
            ScriptableObject.DestroyImmediate(skill, true);
        }

        [Test]
        public void AdjacentAllyStatusEffect_Guard_UserGuardsAdjacentAlly()
        {
            // guardian is at rank 1, target is at rank 2 (Behind guardian)
            var effect = new AdjacentAllyStatusEffect
            {
                statusType = StatusType.Guard,
                direction = AllyDirection.Behind,
                applicationChance = 100f,
                duration = 2,
                ignoreMiss = true
            };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "test_adjacent_guard";
            skill.targetScope = TargetScope.Self;
            skill.effects.Add(effect);

            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();
            bs.StartBattle(new List<CombatCharacter> { guardian, target }, new List<CombatCharacter> { attacker });

            var ctx = new SkillContext(guardian, skill, new List<CombatCharacter> { guardian }, bs, rng);
            ctx.primaryTarget = guardian;

            effect.Execute(ctx, guardian);

            var guard = GuardResolver.GetGuardStatusHeldBy(guardian);
            Assert.IsNotNull(guard, "User should hold Guard status.");
            Assert.AreEqual(target, guard.GuardedTarget, "User should guard the adjacent ally.");
            Assert.AreEqual(0, target.statusEffects.Count, "Adjacent ally should not hold Guard status.");
            Assert.AreEqual(guardian, GuardResolver.GetActiveGuardian(target, ctx));

            Object.DestroyImmediate(bsGo);
            ScriptableObject.DestroyImmediate(skill, true);
        }

        [Test]
        public void SelfStatusEffect_Guard_LogsWarningAndSkips()
        {
            var effect = new SelfStatusEffect
            {
                statusType = StatusType.Guard,
                applicationChance = 100f,
                duration = 2,
                ignoreMiss = true
            };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "test_self_guard";
            skill.targetScope = TargetScope.Self;
            skill.effects.Add(effect);

            var (bs, ctx) = MakeCtx(guardian, guardian, skill, rng);
            ctx.primaryTarget = guardian;

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("SelfStatusEffect.*Guard"));
            effect.Execute(ctx, guardian);

            Assert.AreEqual(0, guardian.statusEffects.Count, "SelfStatusEffect with Guard should not apply any status.");

            Object.DestroyImmediate(bs.gameObject);
            ScriptableObject.DestroyImmediate(skill, true);
        }

        [Test]
        public void GuardResolver_WorksWithAndWithoutBattleSystem()
        {
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

            // Without BattleSystem (null context)
            Assert.AreEqual(guardian, GuardResolver.GetActiveGuardian(target, null), "Should resolve guardian via scene fallback when BattleSystem is null.");

            // With initialized BattleSystem teams
            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();
            bs.StartBattle(new List<CombatCharacter> { guardian, target }, new List<CombatCharacter> { attacker });

            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(attacker, skill, new List<CombatCharacter> { target }, bs, rng);

            Assert.AreEqual(guardian, GuardResolver.GetActiveGuardian(target, ctx), "Should resolve guardian via BattleSystem team list.");

            Object.DestroyImmediate(bsGo);
        }

        [Test]
        public void ApplyStatusToGuardianEffect_AppliesStatusToGuardian_WhenRedirectionOccurs()
        {
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

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
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

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
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

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
            guardian.AddStatus(new GuardStatusInstance(guardian, target, 3));

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
            ScriptableObject.DestroyImmediate(skill, true);
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
            ScriptableObject.DestroyImmediate(skill, true);
        }
    }
}
