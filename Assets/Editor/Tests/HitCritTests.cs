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
            CombatTestHelper.InitializeTestDatabase();
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();
        }

        [TearDown]
        public void TearDown()
        {
            CombatTestHelper.CleanupTestDatabase();
            
            if (_config != null)
                ScriptableObject.DestroyImmediate(_config, true);
                
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

        [Test]
        public void EnsureHitResolved_AOEIndependentRolls()
        {
            var a = Track("a", Team.Player, 1, accuracy: 50); // Set low accuracy for 50% hit chance
            var t1 = Track("t1", Team.Enemy, 1, dodge: 0);
            var t2 = Track("t2", Team.Enemy, 2, dodge: 0);
            var skill = CombatTestHelper.CreateDamageSkill(accuracyMod: 0f);
            
            int workingSeed = -1;
            for (int seed = 0; seed < 100; seed++)
            {
                var tempRng = new System.Random(seed);
                double roll1 = tempRng.NextDouble() * 100.0;
                double roll2 = tempRng.NextDouble() * 100.0;
                bool hit1 = roll1 < 50.0;
                bool hit2 = roll2 < 50.0;
                if (hit1 != hit2)
                {
                    workingSeed = seed;
                    break;
                }
            }
            
            Assert.AreNotEqual(-1, workingSeed, "Should find a seed where first and second rolls differ.");
            
            var rng = new System.Random(workingSeed);
            var (_, ctx) = MakeCtx(a, t1, skill, rng);
            ctx.targets = new List<CombatCharacter> { t1, t2 };
            
            // Resolve hit for t1
            ctx.EnsureHitResolved(t1);
            bool firstHit = ctx.didHit;
            
            // Resolve hit for t2 (new target)
            ctx.EnsureHitResolved(t2);
            bool secondHit = ctx.didHit;
            
            Assert.AreNotEqual(firstHit, secondHit, "Hit check should resolve independently and produce different results for t1 and t2.");
            
            // Verify that subsequent call to the same target/hitIndex returns cached result
            ctx.didHit = !secondHit; // Flip value to check caching
            ctx.EnsureHitResolved(t2);
            Assert.AreEqual(!secondHit, ctx.didHit, "Subsequent EnsureHitResolved on same target/hitIndex should use cached result.");
        }

        [Test]
        public void EnsureHitResolved_MultiHitIndependentRolls()
        {
            var a = Track("a", Team.Player, 1, accuracy: 50);
            var t = Track("t", Team.Enemy, 1, dodge: 0);
            var skill = CombatTestHelper.CreateDamageSkill();
            
            int workingSeed = -1;
            for (int seed = 0; seed < 100; seed++)
            {
                var tempRng = new System.Random(seed);
                double roll1 = tempRng.NextDouble() * 100.0;
                double roll2 = tempRng.NextDouble() * 100.0;
                bool hit1 = roll1 < 50.0;
                bool hit2 = roll2 < 50.0;
                if (hit1 != hit2)
                {
                    workingSeed = seed;
                    break;
                }
            }
            
            var rng = new System.Random(workingSeed);
            var (_, ctx) = MakeCtx(a, t, skill, rng);
            ctx.totalHits = 2;
            
            // Hit 0
            ctx.currentHitIndex = 0;
            ctx.EnsureHitResolved(t);
            bool firstHit = ctx.didHit;
            
            // Hit 1 (same target, different hit index)
            ctx.currentHitIndex = 1;
            ctx.EnsureHitResolved(t);
            bool secondHit = ctx.didHit;
            
            Assert.AreNotEqual(firstHit, secondHit, "Hit check should resolve independently for different hit indices of multi-hit skills.");
        }
    }
}
