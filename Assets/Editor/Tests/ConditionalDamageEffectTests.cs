using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class ConditionalDamageEffectTests
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
                Object.DestroyImmediate(_combatConfig);
            }
        }

        private CombatCharacter CreateTestCharacter(string name, bool isPlayer, int maxHP = 100, int attack = 10)
        {
            var cc = CombatTestHelper.CreateCombatCharacter(
                name,
                isPlayer ? Team.Player : Team.Enemy,
                rank: 1,
                attack: attack,
                maxHP: maxHP,
                config: _combatConfig
            );
            _cleanup.Add(cc.gameObject);
            return cc;
        }

        [Test]
        public void Execute_TargetHasStatus_IncreasesScalingAndDealsDamage()
        {
            var user = CreateTestCharacter("User", true, attack: 100);
            var target = CreateTestCharacter("Target", false, maxHP: 200);

            // Add the specified status to target
            var markStatus = new StatusEffectInstance(StatusType.Mark, StatTarget.Dodge, 0, 3);
            target.AddStatus(markStatus);

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f);
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;

            var effect = new ConditionalDamageEffect
            {
                requiredStatus = StatusType.Mark,
                bonusScaling = 0.5f
            };

            // Execute the effect
            effect.Execute(ctx, target);

            // With scaling 1.0 + 0.5 = 1.5, base attack 100, and fixed RNG seed 42:
            // RollAttackDamage(100) -> 100 * (0.8 + rand * 0.4) -> deterministically calculated by CombatCalculator.
            // Let's verify that damage was dealt and is greater than the base 1.0 scaling damage would be.
            // Let's calculate exactly what the expected damage is:
            // For seed 42, RNG NextDouble() gives a specific roll.
            // Let's compare target's HP change to a control case with no status effect.
            
            // To do this cleanly, let's reset target HP and compare to a control character
            int hpBefore = 200;
            int hpAfterBoosted = target.currentHP;
            int damageBoosted = hpBefore - hpAfterBoosted;

            // Control case (no status)
            var targetControl = CreateTestCharacter("TargetControl", false, maxHP: 200);
            var ctxControl = new SkillContext(user, skill, new List<CombatCharacter> { targetControl }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctxControl.didHit = true;
            ctxControl.hasResolvedHit = true;

            effect.Execute(ctxControl, targetControl);
            int damageControl = hpBefore - targetControl.currentHP;

            // Verify boosted damage is significantly higher than control damage
            Assert.IsTrue(damageBoosted > damageControl, $"Boosted damage ({damageBoosted}) should be higher than control damage ({damageControl})");
            
            // Verify scaling ratio: (damageBoosted / damageControl) is approximately 1.5 / 1.0 = 1.5
            float ratio = (float)damageBoosted / damageControl;
            Assert.AreEqual(1.5f, ratio, 0.01f, "Damage ratio should match scaling ratio (1.5 / 1.0 = 1.5)");
        }

        [Test]
        public void Execute_TargetDoesNotHaveStatus_DealsBaseDamage()
        {
            var user = CreateTestCharacter("User", true, attack: 100);
            var target = CreateTestCharacter("Target", false, maxHP: 200);

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f);
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;

            var effect = new ConditionalDamageEffect
            {
                requiredStatus = StatusType.Mark,
                bonusScaling = 0.5f
            };

            effect.Execute(ctx, target);

            int damageControl = 200 - target.currentHP;
            
            // Verify scaling is exactly 1.0 (no boost applied)
            // Roll attack damage with attack=100 and scaling 1.0:
            // base roll for seed 42:
            // 0.8 + 0.67277... * 0.4 = 1.0691... -> 107 damage
            Assert.AreEqual(107, damageControl, "Base damage should be exactly 107 (no mark status)");
        }

        [Test]
        public void Execute_HitMisses_NoDamageDealt()
        {
            var user = CreateTestCharacter("User", true, attack: 100);
            var target = CreateTestCharacter("Target", false, maxHP: 200);

            // Add the specified status to target
            var markStatus = new StatusEffectInstance(StatusType.Mark, StatTarget.Dodge, 0, 3);
            target.AddStatus(markStatus);

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f);
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = false;
            ctx.hasResolvedHit = true;

            var effect = new ConditionalDamageEffect
            {
                requiredStatus = StatusType.Mark,
                bonusScaling = 0.5f
            };

            effect.Execute(ctx, target);

            Assert.AreEqual(200, target.currentHP, "Target should take no damage on a miss");
        }

        [Test]
        public void Execute_RestoresOriginalScaling_AfterExecution()
        {
            var user = CreateTestCharacter("User", true, attack: 100);
            var target = CreateTestCharacter("Target", false, maxHP: 200);

            // Add the specified status to target
            var markStatus = new StatusEffectInstance(StatusType.Mark, StatTarget.Dodge, 0, 3);
            target.AddStatus(markStatus);

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f);
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;

            var effect = new ConditionalDamageEffect
            {
                requiredStatus = StatusType.Mark,
                bonusScaling = 0.5f
            };

            Assert.AreEqual(1.0f, ctx.skillScaling, "Original scaling should be 1.0");

            effect.Execute(ctx, target);

            Assert.AreEqual(1.0f, ctx.skillScaling, "Scaling should be restored to 1.0 after execution");
        }

        [Test]
        public void Execute_DifferentStatusType_NoBonusApplied()
        {
            var user = CreateTestCharacter("User", true, attack: 100);
            var target = CreateTestCharacter("Target", false, maxHP: 200);

            // Add a different status (Bleed) instead of Mark
            var bleedStatus = new StatusEffectInstance(StatusType.Bleed, StatTarget.MaxHP, 5, 3);
            target.AddStatus(bleedStatus);

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f);
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;

            var effect = new ConditionalDamageEffect
            {
                requiredStatus = StatusType.Mark,
                bonusScaling = 0.5f
            };

            effect.Execute(ctx, target);

            int damage = 200 - target.currentHP;
            Assert.AreEqual(107, damage, "Damage should match base scaling (107) since Bleed is not Mark");
        }

        [Test]
        public void Execute_SelfHasStatus_IncreasesScalingAndDealsDamage()
        {
            var user = CreateTestCharacter("User", true, attack: 100);
            var target = CreateTestCharacter("Target", false, maxHP: 200);

            // Add the specified status to USER, not target
            var markStatus = new StatusEffectInstance(StatusType.Mark, StatTarget.Dodge, 0, 3);
            user.AddStatus(markStatus);

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f);
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;

            var effect = new ConditionalDamageEffect
            {
                conditionSource = ConditionSource.Self,
                requiredStatus = StatusType.Mark,
                bonusScaling = 0.5f
            };

            effect.Execute(ctx, target);

            int damageBoosted = 200 - target.currentHP;

            // Control case (no status on user)
            var userControl = CreateTestCharacter("UserControl", true, attack: 100);
            var targetControl = CreateTestCharacter("TargetControl", false, maxHP: 200);
            var ctxControl = new SkillContext(userControl, skill, new List<CombatCharacter> { targetControl }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctxControl.didHit = true;
            ctxControl.hasResolvedHit = true;

            effect.Execute(ctxControl, targetControl);
            int damageControl = 200 - targetControl.currentHP;

            Assert.IsTrue(damageBoosted > damageControl, $"Boosted damage ({damageBoosted}) should be higher than control damage ({damageControl})");
            float ratio = (float)damageBoosted / damageControl;
            Assert.AreEqual(1.5f, ratio, 0.01f, "Damage ratio should match scaling ratio (1.5 / 1.0 = 1.5)");
        }

        [Test]
        public void Execute_SelfDoesNotHaveStatus_DealsBaseDamage()
        {
            var user = CreateTestCharacter("User", true, attack: 100);
            var target = CreateTestCharacter("Target", false, maxHP: 200);

            // Add status to target, but since conditionSource is Self, it shouldn't trigger
            var markStatus = new StatusEffectInstance(StatusType.Mark, StatTarget.Dodge, 0, 3);
            target.AddStatus(markStatus);

            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 1.0f);
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng(42));
            ctx.didHit = true;
            ctx.hasResolvedHit = true;

            var effect = new ConditionalDamageEffect
            {
                conditionSource = ConditionSource.Self,
                requiredStatus = StatusType.Mark,
                bonusScaling = 0.5f
            };

            effect.Execute(ctx, target);

            int damage = 200 - target.currentHP;
            Assert.AreEqual(107, damage, "Damage should match base scaling (107) since user has no Mark status");
        }
    }
}
