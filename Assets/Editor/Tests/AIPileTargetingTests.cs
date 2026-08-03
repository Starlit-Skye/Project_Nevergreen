using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Combat.AI;
using Nevergreen.Combat.AI.Nodes;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class AIPileTargetingTests
    {
        private GameObject _battleGo;
        private BattleSystem _battleSystem;
        private CombatCharacter _brainChar;
        private AIBrain _brain;

        [SetUp]
        public void Setup()
        {
            _battleGo = new GameObject("BattleSystem");
            _battleSystem = _battleGo.AddComponent<BattleSystem>();

            _brainChar = CombatTestHelper.CreateCombatCharacter("enemy_1", Team.Enemy, 1, maxHP: 100);
            _brain = _brainChar.gameObject.GetComponent<AIBrain>();
            if (_brain == null) _brain = _brainChar.gameObject.AddComponent<AIBrain>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_battleGo);
            if (_brainChar != null && _brainChar.gameObject != null)
            {
                Object.DestroyImmediate(_brainChar.gameObject);
            }
        }

        private void SetPlayerTeam(List<CombatCharacter> team)
        {
            typeof(BattleSystem).GetField("_playerTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_battleSystem, team);
        }

        [Test]
        public void SimpleTargeting_Random_AvoidsPilesIfAlternativeExists()
        {
            var targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random };
            
            var p1 = CombatTestHelper.CreateCombatCharacter("hero", Team.Player, 1, maxHP: 100);
            var p2 = CombatTestHelper.CreateCombatCharacter("pile_1", Team.Player, 2, maxHP: 100);
            p2.state = LifeState.Pile;

            SetPlayerTeam(new List<CombatCharacter> { p1, p2 });
            
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.maxTargets = 1;

            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);
            
            Assert.IsTrue(success);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(p1, targets[0], "Random targeting should have strictly picked the non-pile character.");

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
        }

        [Test]
        public void SimpleTargeting_Random_TargetsPileIfOnlyPileExists()
        {
            var targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random };
            
            var p1 = CombatTestHelper.CreateCombatCharacter("pile_1", Team.Player, 1, maxHP: 100);
            p1.state = LifeState.Pile;

            SetPlayerTeam(new List<CombatCharacter> { p1 });
            
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.maxTargets = 1;

            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);
            
            Assert.IsTrue(success);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(p1, targets[0], "Random targeting should pick the pile if it's the only available target.");

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SimpleTargeting_LowestHP_AvoidsPiles()
        {
            var targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.LowestHP };
            
            var p1 = CombatTestHelper.CreateCombatCharacter("hero", Team.Player, 1, maxHP: 100);
            var p2 = CombatTestHelper.CreateCombatCharacter("pile_1", Team.Player, 2, maxHP: 100);
            
            p1.currentHP = 80;
            p2.currentHP = 10;
            p2.state = LifeState.Pile;

            SetPlayerTeam(new List<CombatCharacter> { p1, p2 });
            
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.maxTargets = 1;

            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);
            
            Assert.IsTrue(success);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(p1, targets[0], "LowestHP should avoid the pile (even if it has lower HP) since a non-pile exists.");

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
        }

        [Test]
        public void RandomSkillBehavior_AvoidsPilesIfAlternativeExists()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("hero", Team.Player, 1, maxHP: 100);
            var p2 = CombatTestHelper.CreateCombatCharacter("pile_1", Team.Player, 2, maxHP: 100);
            p2.state = LifeState.Pile;

            SetPlayerTeam(new List<CombatCharacter> { p1, p2 });

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.maxTargets = 1;
            _brainChar.equippedSkills.Add(skill);

            var behavior = new RandomSkillBehavior();

            bool success = behavior.TryGetDecision(_brain, _battleSystem, out AIDecision decision);
            
            Assert.IsTrue(success);
            Assert.IsFalse(decision.isPass);
            Assert.AreEqual(skill, decision.skill);
            Assert.AreEqual(1, decision.targets.Count);
            Assert.AreEqual(p1, decision.targets[0], "RandomSkillBehavior should strictly pick the non-pile character.");

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
        }
    }
}
