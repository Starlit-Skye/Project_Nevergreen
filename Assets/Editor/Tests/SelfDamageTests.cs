using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Tests;

namespace Nevergreen.Combat.Tests
{
    public class SelfDamageTests
    {
        private List<GameObject> _cleanup = new List<GameObject>();
        private CombatCharacter _character;
        private BattleSystem _battleSystem;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("BattleSystem");
            _battleSystem = go.AddComponent<BattleSystem>();
            _cleanup.Add(go);

            _character = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Player, 1);
            _character.baseStats.maxHP = 100;
            _character.currentHP = 100;
            _cleanup.Add(_character.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _cleanup.Clear();
        }

        [Test]
        public void SelfDamageEffect_AppliesFlatDamage()
        {
            var effect = new SelfDamageEffect
            {
                damageAmount = 15,
                isPercentageOfMaxHP = false,
                ignoreMiss = true
            };

            var target = CombatTestHelper.CreateCombatCharacter("Target", Team.Enemy, 1);
            _cleanup.Add(target.gameObject);

            var ctx = new SkillContext(_character, CombatTestHelper.CreateDamageSkill(), new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng());
            ctx.didHit = true;

            effect.Execute(ctx, target);

            Assert.AreEqual(85, _character.currentHP, "Character should have taken 15 flat damage.");
        }

        [Test]
        public void SelfDamageEffect_AppliesPercentageDamage()
        {
            var effect = new SelfDamageEffect
            {
                damageAmount = 25, // 25%
                isPercentageOfMaxHP = true,
                ignoreMiss = true
            };

            var target = CombatTestHelper.CreateCombatCharacter("Target", Team.Enemy, 1);
            _cleanup.Add(target.gameObject);

            var ctx = new SkillContext(_character, CombatTestHelper.CreateDamageSkill(), new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng());
            ctx.didHit = true;

            effect.Execute(ctx, target);

            Assert.AreEqual(75, _character.currentHP, "Character should have taken 25 damage (25% of 100 max HP).");
        }

        [Test]
        public void SelfDamageEffect_OnlyAppliesOncePerExecution()
        {
            var effect = new SelfDamageEffect
            {
                damageAmount = 10,
                isPercentageOfMaxHP = false,
                ignoreMiss = true
            };

            var target1 = CombatTestHelper.CreateCombatCharacter("Target1", Team.Enemy, 1);
            var target2 = CombatTestHelper.CreateCombatCharacter("Target2", Team.Enemy, 2);
            _cleanup.Add(target1.gameObject);
            _cleanup.Add(target2.gameObject);

            var targets = new List<CombatCharacter> { target1, target2 };
            var ctx = new SkillContext(_character, CombatTestHelper.CreateDamageSkill(), targets, _battleSystem, CombatTestHelper.CreateFixedRng());
            ctx.didHit = true;

            // Execute twice, representing hitting two targets in an AoE attack
            effect.Execute(ctx, target1);
            effect.Execute(ctx, target2);

            Assert.AreEqual(90, _character.currentHP, "Character should have only taken damage once despite multiple targets.");
        }

        [Test]
        public void SelfDamageEffect_IgnoresMiss_WhenConfigured()
        {
            var effect = new SelfDamageEffect
            {
                damageAmount = 20,
                isPercentageOfMaxHP = false,
                ignoreMiss = true
            };

            var target = CombatTestHelper.CreateCombatCharacter("Target", Team.Enemy, 1);
            _cleanup.Add(target.gameObject);

            var ctx = new SkillContext(_character, CombatTestHelper.CreateDamageSkill(), new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng());
            ctx.hasResolvedHit = true;
            ctx.didHit = false; // Simulated Miss

            effect.Execute(ctx, target);

            Assert.AreEqual(80, _character.currentHP, "Character should still take damage when ignoreMiss is true.");
        }

        [Test]
        public void SelfDamageEffect_RespectsMiss_WhenConfigured()
        {
            var effect = new SelfDamageEffect
            {
                damageAmount = 20,
                isPercentageOfMaxHP = false,
                ignoreMiss = false
            };

            var target = CombatTestHelper.CreateCombatCharacter("Target", Team.Enemy, 1);
            _cleanup.Add(target.gameObject);

            var ctx = new SkillContext(_character, CombatTestHelper.CreateDamageSkill(), new List<CombatCharacter> { target }, _battleSystem, CombatTestHelper.CreateFixedRng());
            ctx.hasResolvedHit = true;
            ctx.didHit = false; // Simulated Miss

            effect.Execute(ctx, target);

            Assert.AreEqual(100, _character.currentHP, "Character should NOT take damage on miss if ignoreMiss is false.");
        }
    }
}
