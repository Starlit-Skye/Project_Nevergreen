using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class CumulativeDamageTests
    {
        private GameObject _battleSystemObj;
        private BattleSystem _battleSystem;
        private CombatConfig _combatConfig;
        private List<GameObject> _cleanup;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();
            _cleanup = new List<GameObject>();
            _battleSystemObj = new GameObject("BattleSystem");
            _battleSystem = _battleSystemObj.AddComponent<BattleSystem>();
            _combatConfig = CombatTestHelper.CreateDefaultConfig();
            _cleanup.Add(_battleSystemObj);
        }

        [TearDown]
        public void Teardown()
        {
            CombatTestHelper.CleanupTestDatabase();
            foreach (var go in _cleanup)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            if (_combatConfig != null)
            {
                ScriptableObject.DestroyImmediate(_combatConfig, true);
            }
        }

        private CombatCharacter CreateCharacter(string name, Team team, int rank = 1, 
            int attack = 100, int maxHP = 500, int defense = 0, int critChance = 0)
        {
            var cc = CombatTestHelper.CreateCombatCharacter(
                name, team, rank,
                attack: attack, maxHP: maxHP, defense: defense,
                critChance: critChance, config: _combatConfig
            );
            _cleanup.Add(cc.gameObject);
            return cc;
        }

        [Test]
        public void MultiEffect_DamageAccumulates_InCalculatedValue()
        {
            // Arrange: skill with DamageEffect + ConditionalDamageEffect (like hunt_down)
            var user = CreateCharacter("User", Team.Player, attack: 100);
            var target = CreateCharacter("Target", Team.Enemy, maxHP: 500);

            // Add mark so conditional bonus triggers
            target.AddStatus(new StatusEffectInstance(StatusType.Mark, StatTarget.Dodge, 0, 3));

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f, ignoresDefense: true);
            var damageEffect = new DamageEffect();
            var conditionalEffect = new ConditionalDamageEffect
            {
                requiredStatus = StatusType.Mark,
                bonusScaling = 0.1f
            };
            skill.effects = new List<ISkillEffect> { damageEffect, conditionalEffect };

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;
            ctx.isCritical = false;

            // Reset calculatedValue as BattleSystem would
            ctx.calculatedValue = 0;

            // Act: execute both effects sequentially
            damageEffect.Execute(ctx, target);
            int afterFirst = ctx.calculatedValue;

            conditionalEffect.Execute(ctx, target);
            int afterBoth = ctx.calculatedValue;

            // Assert: calculatedValue accumulated both damage ticks
            Assert.Greater(afterFirst, 0, "First effect should deal damage");
            Assert.Greater(afterBoth, afterFirst, "Second effect should add to accumulated damage");
            
            // Verify the accumulated value equals actual damage dealt
            int actualDamage = 500 - target.currentHP;
            Assert.AreEqual(actualDamage, afterBoth, "calculatedValue should match total actual damage dealt");
        }

        [Test]
        public void MultiEffect_CalculatedValueResets_BetweenTargets()
        {
            // Arrange
            var user = CreateCharacter("User", Team.Player, attack: 100);
            var target1 = CreateCharacter("Target1", Team.Enemy, maxHP: 500);
            var target2 = CreateCharacter("Target2", Team.Enemy, maxHP: 500);

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f, ignoresDefense: true);
            var damageEffect = new DamageEffect();
            skill.effects = new List<ISkillEffect> { damageEffect };

            var rng = CombatTestHelper.CreateFixedRng(42);
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target1, target2 }, _battleSystem, rng);
            ctx.didHit = true;
            ctx.hasResolvedHit = true;
            ctx.isCritical = false;

            // Act: Simulate BattleSystem's per-target reset for target 1
            ctx.calculatedValue = 0;
            damageEffect.Execute(ctx, target1);
            int damage1 = ctx.calculatedValue;

            // Simulate BattleSystem's per-target reset for target 2
            ctx.calculatedValue = 0;
            // Reset hit resolution for new target
            ctx.lastResolvedTarget = null;
            ctx.hasResolvedHit = false;
            ctx.didHit = true;
            ctx.hasResolvedHit = true;
            damageEffect.Execute(ctx, target2);
            int damage2 = ctx.calculatedValue;

            // Assert: Each target's calculatedValue is independent (not accumulated across targets)
            Assert.Greater(damage1, 0, "Target1 should take damage");
            Assert.Greater(damage2, 0, "Target2 should take damage");
            // damage2 should NOT include damage1's value
            int target2ActualDamage = 500 - target2.currentHP;
            Assert.AreEqual(target2ActualDamage, damage2, "calculatedValue for target2 should only reflect target2's damage");
        }

        [Test]
        public void CritRolledOnce_SharedAcrossAllEffects()
        {
            // Arrange: Ensure crit set before effects applies to both DamageEffect and ConditionalDamageEffect
            var user = CreateCharacter("User", Team.Player, attack: 100, critChance: 0);
            var target = CreateCharacter("Target", Team.Enemy, maxHP: 1000);

            target.AddStatus(new StatusEffectInstance(StatusType.Mark, StatTarget.Dodge, 0, 3));

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f, ignoresDefense: true);
            var damageEffect = new DamageEffect();
            var conditionalEffect = new ConditionalDamageEffect
            {
                requiredStatus = StatusType.Mark,
                bonusScaling = 0.5f
            };
            skill.effects = new List<ISkillEffect> { damageEffect, conditionalEffect };

            // Run with crit = true
            var rngCrit = CombatTestHelper.CreateFixedRng(42);
            var ctxCrit = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, rngCrit);
            ctxCrit.didHit = true;
            ctxCrit.hasResolvedHit = true;
            ctxCrit.isCritical = true; // Simulating BattleSystem rolling crit once
            ctxCrit.calculatedValue = 0;

            damageEffect.Execute(ctxCrit, target);
            conditionalEffect.Execute(ctxCrit, target);
            int critDamage = ctxCrit.calculatedValue;

            // Run with crit = false on a fresh target with same RNG
            var targetNoCrit = CreateCharacter("TargetNoCrit", Team.Enemy, maxHP: 1000);
            targetNoCrit.AddStatus(new StatusEffectInstance(StatusType.Mark, StatTarget.Dodge, 0, 3));

            var rngNoCrit = CombatTestHelper.CreateFixedRng(42);
            var ctxNoCrit = new SkillContext(user, skill, new List<CombatCharacter> { targetNoCrit }, _battleSystem, rngNoCrit);
            ctxNoCrit.didHit = true;
            ctxNoCrit.hasResolvedHit = true;
            ctxNoCrit.isCritical = false;
            ctxNoCrit.calculatedValue = 0;

            damageEffect.Execute(ctxNoCrit, targetNoCrit);
            conditionalEffect.Execute(ctxNoCrit, targetNoCrit);
            int noCritDamage = ctxNoCrit.calculatedValue;

            // Assert: Crit damage should be 1.5x the non-crit damage (both effects share crit)
            Assert.Greater(critDamage, noCritDamage, "Crit damage should be higher than non-crit");
            float ratio = (float)critDamage / noCritDamage;
            Assert.AreEqual(1.5f, ratio, 0.01f, "Crit damage ratio should be ~1.5x across all effects");
        }

        [Test]
        public void HealEffect_AccumulatesCalculatedValue()
        {
            // Arrange
            var user = CreateCharacter("User", Team.Player, attack: 100);
            var target = CreateCharacter("Target", Team.Player, maxHP: 500);
            target.TakeDamage(300); // Reduce to 200 HP

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "test_heal";
            skill.displayName = "Test Heal";
            skill.modifier = new SkillModifier { healPercent = 1.0f };
            skill.targetScope = TargetScope.Allies;
            skill.hitCount = 1;
            skill.effects = new List<ISkillEffect>();

            var healEffect = new HealEffect();
            skill.effects.Add(healEffect);

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;
            ctx.calculatedValue = 0;

            // Act
            healEffect.Execute(ctx, target);

            // Assert: calculatedValue should be the heal amount
            Assert.Greater(ctx.calculatedValue, 0, "Heal should accumulate in calculatedValue");
            int actualHeal = target.currentHP - 200;
            Assert.AreEqual(actualHeal, ctx.calculatedValue, "calculatedValue should match actual healing done");
        }

        [Test]
        public void ConditionalDamageOnly_AccumulatesCorrectly()
        {
            // Arrange: Skill with ONLY ConditionalDamageEffect (like enforcer_trample)
            var user = CreateCharacter("User", Team.Player, attack: 100);
            var target = CreateCharacter("Target", Team.Enemy, maxHP: 500);

            // Add stun so conditional bonus triggers
            target.AddStatus(new StatusEffectInstance(StatusType.Stun, 0, 1));

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 0.9f, ignoresDefense: true);
            var conditionalEffect = new ConditionalDamageEffect
            {
                requiredStatus = StatusType.Stun,
                bonusScaling = 0.3f
            };
            skill.effects = new List<ISkillEffect> { conditionalEffect };

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;
            ctx.isCritical = false;
            ctx.calculatedValue = 0;

            // Act
            conditionalEffect.Execute(ctx, target);

            // Assert
            int actualDamage = 500 - target.currentHP;
            Assert.Greater(ctx.calculatedValue, 0, "Conditional damage should accumulate");
            Assert.AreEqual(actualDamage, ctx.calculatedValue, "calculatedValue should match actual damage dealt");
        }
        [Test]
        public void MultiConditionalDamageEffect_EvaluatesAllConditions()
        {
            // Arrange
            var user = CreateCharacter("User", Team.Player, attack: 100);
            var target = CreateCharacter("Target", Team.Enemy, maxHP: 500);

            // Add both stealth to user and mark to target
            user.AddStatus(new StatusEffectInstance(StatusType.Stealth, 0, 1));
            target.AddStatus(new StatusEffectInstance(StatusType.Debuff, 0, 1));

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f, ignoresDefense: true);
            var multiConditionalEffect = new MultiConditionalDamageEffect();
            multiConditionalEffect.conditions.Add(new DamageCondition { conditionSource = ConditionSource.Self, requiredStatus = StatusType.Stealth, bonusScaling = 0.5f });
            multiConditionalEffect.conditions.Add(new DamageCondition { conditionSource = ConditionSource.Target, requiredStatus = StatusType.Debuff, bonusScaling = 0.5f });
            
            skill.effects = new List<ISkillEffect> { multiConditionalEffect };

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;
            ctx.isCritical = false;
            ctx.calculatedValue = 0;

            // Act
            multiConditionalEffect.Execute(ctx, target);

            // Assert: Total scaling should be 1.0 + 0.5 + 0.5 = 2.0
            // Since damage percent is 1.0, attack is 100, no defense, damage should be 200
            int actualDamage = 500 - target.currentHP;
            Assert.Greater(actualDamage, 180, "Damage should reflect ~2.0x combined scaling");
            Assert.Less(actualDamage, 220, "Damage should reflect ~2.0x combined scaling");
            Assert.AreEqual(actualDamage, ctx.calculatedValue, "calculatedValue should match actual damage dealt");
            // Also ensure scaling was restored
            Assert.AreEqual(1.0f, ctx.skillScaling, 0.001f, "Skill scaling should be restored after execution");
        }
    }
}
