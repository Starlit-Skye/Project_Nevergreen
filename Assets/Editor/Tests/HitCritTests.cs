using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class HitCritTests
    {
        private CombatConfig _config;
        private List<GameObject> _cleanup;

        [SetUp]
        public void SetUp()
        {
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup)
                if (go != null) Object.DestroyImmediate(go);
        }

        private CombatCharacter Track(string id, Team team, int rank,
            int attack = 100, int defense = 0, int accuracy = 95,
            int dodge = 5, int critChance = 5, int speed = 5, int maxHP = 200)
        {
            var cc = CombatTestHelper.CreateCombatCharacter(id, team, rank, attack, defense,
                accuracy, dodge, critChance, speed, maxHP, config: _config);
            _cleanup.Add(cc.gameObject);
            return cc;
        }

        private (BattleSystem bs, SkillContext ctx) MakeCtx(
            CombatCharacter attacker, CombatCharacter target, SkillData skill, System.Random rng)
        {
            var bsGo = new GameObject("BS");
            _cleanup.Add(bsGo);
            var bs = bsGo.AddComponent<BattleSystem>();
            bs.combatConfig = _config;
            var ctx = new SkillContext(attacker, skill, new List<CombatCharacter> { target }, bs, rng);
            return (bs, ctx);
        }

        [Test]
        public void HitChance_IsAccuracyMinusDodge_CappedAt95()
        {
            var a = Track("a", Team.Player, 1, accuracy: 95);
            var t = Track("t", Team.Enemy, 1, dodge: 5);
            var skill = CombatTestHelper.CreateDamageSkill();
            var (_, ctx) = MakeCtx(a, t, skill, CombatTestHelper.CreateFixedRng());
            CombatCalculator.ResolveHit(ctx, t, _config);
            Assert.AreEqual(90f, ctx.finalAccuracy, "min(95, 95-5) = 90.");
        }

        [Test]
        public void HitChance_CappedAt95()
        {
            var a = Track("a", Team.Player, 1, accuracy: 100, dodge: 0);
            var t = Track("t", Team.Enemy, 1, dodge: 0);
            var skill = CombatTestHelper.CreateDamageSkill();
            var (_, ctx) = MakeCtx(a, t, skill, CombatTestHelper.CreateFixedRng());
            CombatCalculator.ResolveHit(ctx, t, _config);
            Assert.AreEqual(95f, ctx.finalAccuracy, "Capped at 95.");
        }

        [Test]
        public void AccuracyMod_FromSkill_IsApplied()
        {
            var a = Track("a", Team.Player, 1, accuracy: 80);
            var t = Track("t", Team.Enemy, 1, dodge: 5);
            var skill = CombatTestHelper.CreateDamageSkill(accuracyMod: 10f);
            var (_, ctx) = MakeCtx(a, t, skill, CombatTestHelper.CreateFixedRng());
            CombatCalculator.ResolveHit(ctx, t, _config);
            Assert.AreEqual(85f, ctx.finalAccuracy, "min(95, (80+10)-5) = 85.");
        }

        [Test]
        public void GuaranteedHit_AlwaysHits()
        {
            var a = Track("a", Team.Player, 1, accuracy: 10);
            var t = Track("t", Team.Enemy, 1, dodge: 95);
            var skill = CombatTestHelper.CreateDamageSkill(guaranteedHit: true);
            var (_, ctx) = MakeCtx(a, t, skill, CombatTestHelper.CreateFixedRng());
            bool hit = CombatCalculator.ResolveHit(ctx, t, _config);
            Assert.IsTrue(hit);
            Assert.AreEqual(100f, ctx.finalAccuracy);
        }

        [Test]
        public void CritDamage_Applies1Point5xMultiplier()
        {
            var a = Track("a", Team.Player, 1, attack: 100, critChance: 100);
            var t = Track("t", Team.Enemy, 1, defense: 0);
            var skill = CombatTestHelper.CreateDamageSkill(ignoresDefense: true, guaranteedHit: true);
            var (_, ctx) = MakeCtx(a, t, skill, new System.Random(0));
            int damage = CombatCalculator.CalculateDamage(ctx, _config);
            Assert.IsTrue(ctx.isCritical, "100% crit => always crit.");
            int expected = Mathf.RoundToInt(Mathf.RoundToInt(ctx.baseAttackRoll * 1.0f) * 1.5f);
            Assert.AreEqual(expected, damage);
        }

        [Test]
        public void NoCrit_WhenCritChanceIsZero()
        {
            var a = Track("a", Team.Player, 1, attack: 100, critChance: 0);
            var t = Track("t", Team.Enemy, 1, defense: 0);
            var skill = CombatTestHelper.CreateDamageSkill(ignoresDefense: true, guaranteedHit: true, critMod: 0f);
            var (_, ctx) = MakeCtx(a, t, skill, CombatTestHelper.CreateFixedRng());
            CombatCalculator.CalculateDamage(ctx, _config);
            Assert.IsFalse(ctx.isCritical, "0% crit => never crit.");
        }

        [Test]
        public void CritMod_FromSkill_AddsToBaseCritChance()
        {
            var a = Track("a", Team.Player, 1, attack: 100, critChance: 0);
            var t = Track("t", Team.Enemy, 1, defense: 0);
            var skill = CombatTestHelper.CreateDamageSkill(critMod: 100f, ignoresDefense: true, guaranteedHit: true);
            var (_, ctx) = MakeCtx(a, t, skill, CombatTestHelper.CreateFixedRng());
            CombatCalculator.CalculateDamage(ctx, _config);
            Assert.IsTrue(ctx.isCritical, "+100% critMod => guaranteed crit.");
        }

        [Test]
        public void Defense_ReducesDamage()
        {
            var a = Track("a", Team.Player, 1, attack: 100, critChance: 0);
            var t = Track("t", Team.Enemy, 1, defense: 50);
            var skill = CombatTestHelper.CreateDamageSkill(guaranteedHit: true, critMod: 0f);
            var (_, ctx) = MakeCtx(a, t, skill, CombatTestHelper.CreateFixedRng());
            int damage = CombatCalculator.CalculateDamage(ctx, _config);
            int expected = Mathf.RoundToInt(Mathf.RoundToInt(ctx.baseAttackRoll * 1.0f) * 0.5f);
            Assert.AreEqual(expected, damage, "50% defense => halved damage.");
        }

        [Test]
        public void IgnoresDefense_BypassesReduction()
        {
            var a = Track("a", Team.Player, 1, attack: 100, critChance: 0);
            var t = Track("t", Team.Enemy, 1, defense: 50);
            var skill = CombatTestHelper.CreateDamageSkill(ignoresDefense: true, guaranteedHit: true, critMod: 0f);
            var (_, ctx) = MakeCtx(a, t, skill, CombatTestHelper.CreateFixedRng());
            int damage = CombatCalculator.CalculateDamage(ctx, _config);
            int expected = Mathf.RoundToInt(ctx.baseAttackRoll * 1.0f);
            Assert.AreEqual(expected, damage, "IgnoresDefense => full damage.");
        }

        [Test]
        public void SkillScaling_MultipliesBaseRoll()
        {
            var a = Track("a", Team.Player, 1, attack: 100, critChance: 0);
            var t = Track("t", Team.Enemy, 1, defense: 0);
            var skill = CombatTestHelper.CreateDamageSkill(damagePercent: 2.0f, ignoresDefense: true,
                guaranteedHit: true, critMod: 0f);
            var (_, ctx) = MakeCtx(a, t, skill, CombatTestHelper.CreateFixedRng());
            int damage = CombatCalculator.CalculateDamage(ctx, _config);
            int expected = Mathf.RoundToInt(ctx.baseAttackRoll * 2.0f);
            Assert.AreEqual(expected, damage, "200% scaling => 2x base roll.");
        }

        [Test]
        public void Heal_AppliesRandomRollAndScaling()
        {
            var a = Track("a", Team.Player, 1, attack: 100);
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.modifier = new SkillModifier
            {
                healPercent = 0.5f // 50% scaling
            };
            
            var (_, ctx) = MakeCtx(a, a, skill, new System.Random(42));
            int heal = CombatCalculator.CalculateHeal(ctx, _config);
            
            // Expected base roll using standard RollAttackDamage
            int expectedBaseRoll = CombatCalculator.RollAttackDamage(100, _config, new System.Random(42));
            int expectedHeal = Mathf.RoundToInt(expectedBaseRoll * 0.5f);
            
            Assert.AreEqual(expectedHeal, heal, "Heal should scale the rolled attack power correctly.");
            Assert.AreEqual(expectedBaseRoll, ctx.baseAttackRoll, "Context baseAttackRoll should record the roll.");
        }
    }
}
