using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class AoeTargetingTests
    {
        private BattleSystem _battleSystem;
        private CombatCharacter _attacker;
        private SkillData _skill;
        private CombatConfig _config;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();

            GameObject battleGo = new GameObject("BattleSystem");
            _battleSystem = battleGo.AddComponent<BattleSystem>();

            _config = CombatTestHelper.CreateDefaultConfig();

            _attacker = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Player, 1, size: 1);
            _skill = ScriptableObject.CreateInstance<SkillData>();
            _skill.targetScope = TargetScope.Enemies;
            _skill.maxTargets = 2;
            _skill.effects = new List<ISkillEffect>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_battleSystem != null)
                Object.DestroyImmediate(_battleSystem.gameObject);
            if (_config != null)
                ScriptableObject.DestroyImmediate(_config, true);
            if (_skill != null)
                ScriptableObject.DestroyImmediate(_skill, true);
                
            CombatTestHelper.CleanupTestDatabase();
        }

        private void InjectTeams(List<CombatCharacter> playerTeam, List<CombatCharacter> enemyTeam)
        {
            var playerTeamField = typeof(BattleSystem).GetField("_playerTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            playerTeamField.SetValue(_battleSystem, playerTeam);

            var enemyTeamField = typeof(BattleSystem).GetField("_enemyTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            enemyTeamField.SetValue(_battleSystem, enemyTeam);
        }

        [Test]
        public void GetAOETargets_SimpleLinearHits_ReturnsPrimaryAndBehind()
        {
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 1);
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 2, size: 1);
            var e3 = CombatTestHelper.CreateCombatCharacter("E3", Team.Enemy, 3, size: 1);

            InjectTeams(new List<CombatCharacter> { _attacker }, new List<CombatCharacter> { e1, e2, e3 });

            // Click E1, should hit E1 and E2
            var targets = _battleSystem.GetAOETargets(_skill, e1);

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(e1, targets[0]);
            Assert.AreEqual(e2, targets[1]);

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
            Object.DestroyImmediate(e3.gameObject);
        }

        [Test]
        public void GetAOETargets_TrailingLimit_ReturnsOnlyAvailable()
        {
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 1);
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 2, size: 1);

            InjectTeams(new List<CombatCharacter> { _attacker }, new List<CombatCharacter> { e1, e2 });

            // Click E2 (the last character), should only hit E2
            var targets = _battleSystem.GetAOETargets(_skill, e2);

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(e2, targets[0]);

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
        }

        [Test]
        public void GetAOETargets_Size2Target_Budget2_HitsOnlySize2()
        {
            // E1 is size 2, occupies ranks 1 and 2
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 2);
            // E2 is size 1, occupies rank 3
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 3, size: 1);

            InjectTeams(new List<CombatCharacter> { _attacker }, new List<CombatCharacter> { e1, e2 });

            // Click E1, should hit ONLY E1. E1 is size 2, so it consumes the entire maxTargets = 2 budget.
            var targets = _battleSystem.GetAOETargets(_skill, e1);

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(e1, targets[0]);

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
        }

        [Test]
        public void GetAOETargets_Size2Target_Budget3_HitsSize2AndNext()
        {
            // E1 is size 2, occupies ranks 1 and 2
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 2);
            // E2 is size 1, occupies rank 3
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 3, size: 1);
            // E3 is size 1, occupies rank 4
            var e3 = CombatTestHelper.CreateCombatCharacter("E3", Team.Enemy, 4, size: 1);

            InjectTeams(new List<CombatCharacter> { _attacker }, new List<CombatCharacter> { e1, e2, e3 });

            // Set skill budget to 3
            _skill.maxTargets = 3;

            // Click E1. Budget=3. E1 consumes 2. Remaining=1. E2 consumes 1. E3 is ignored.
            var targets = _battleSystem.GetAOETargets(_skill, e1);

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(e1, targets[0]);
            Assert.AreEqual(e2, targets[1]);

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
            Object.DestroyImmediate(e3.gameObject);
        }

        [Test]
        public void GetAOETargets_Size3Target_Budget2_HitsOnlySize3()
        {
            // E1 is size 3, occupies ranks 1, 2, 3
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 3);
            // E2 is size 1, occupies rank 4
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 4, size: 1);

            InjectTeams(new List<CombatCharacter> { _attacker }, new List<CombatCharacter> { e1, e2 });

            // Skill budget is 2
            _skill.maxTargets = 2;

            // Click E1. E1 consumes 3, exceeding budget of 2. But primary target is always included.
            // E2 is ignored because budget is exhausted.
            var targets = _battleSystem.GetAOETargets(_skill, e1);

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(e1, targets[0]);

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
        }

        [Test]
        public void GetAOETargets_DamagingSkill_IncludesPiles()
        {
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 1);
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 2, size: 1);
            e2.state = LifeState.Pile; // make E2 a pile
            var e3 = CombatTestHelper.CreateCombatCharacter("E3", Team.Enemy, 3, size: 1);

            InjectTeams(new List<CombatCharacter> { _attacker }, new List<CombatCharacter> { e1, e2, e3 });

            // Click E1, should hit E1 and the pile E2
            var targets = _battleSystem.GetAOETargets(_skill, e1);

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(e1, targets[0]);
            Assert.AreEqual(e2, targets[1]);

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
            Object.DestroyImmediate(e3.gameObject);
        }

        [Test]
        public void GetAOETargets_HealingSkill_IncludesPilesButDoesNotHeal()
        {
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 1);
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 2, size: 1);
            e2.state = LifeState.Pile; // make E2 a pile
            var e3 = CombatTestHelper.CreateCombatCharacter("E3", Team.Enemy, 3, size: 1);

            InjectTeams(new List<CombatCharacter> { _attacker }, new List<CombatCharacter> { e1, e2, e3 });

            // Create a healing skill
            var healSkill = ScriptableObject.CreateInstance<SkillData>();
            healSkill.targetScope = TargetScope.Enemies;
            healSkill.maxTargets = 2;
            healSkill.modifier.healPercent = 1.0f;
            healSkill.effects = new List<ISkillEffect> { new HealEffect() };

            // Click E1, should hit E1 and E2 (pile). It should NOT skip E2.
            var targets = _battleSystem.GetAOETargets(healSkill, e1);

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(e1, targets[0]);
            Assert.AreEqual(e2, targets[1]);

            // Execute the heal effect to ensure E2 does not actually heal (piles refuse healing)
            int initialHP = e2.currentHP;
            foreach (var t in targets)
            {
                foreach (var effect in healSkill.effects)
                {
                    effect.Execute(new SkillContext(_attacker, healSkill, targets, _battleSystem, new System.Random()), t);
                }
            }

            Assert.AreEqual(initialHP, e2.currentHP, "Pile should not receive healing");

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
            Object.DestroyImmediate(e3.gameObject);
            ScriptableObject.DestroyImmediate(healSkill, true);
        }

        [Test]
        public void GetValidTargets_AOEHealingSkill_AllowsPileAnchorIfLivingUnitInAOERange()
        {
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 1);
            e1.state = LifeState.Pile; // make E1 a pile
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 2, size: 1); // alive

            InjectTeams(new List<CombatCharacter> { _attacker }, new List<CombatCharacter> { e1, e2 });

            var healSkill = ScriptableObject.CreateInstance<SkillData>();
            healSkill.targetScope = TargetScope.Enemies;
            healSkill.maxTargets = 2;
            healSkill.targetRanks = new List<int> { 1, 2, 3, 4 };
            healSkill.effects = new List<ISkillEffect> { new HealEffect() };

            var validTargets = _battleSystem.GetValidTargets(_attacker, healSkill);

            // E1 is a pile but has a living unit (E2) behind it in the AOE range, so E1 should be valid as an anchor.
            // E2 is also valid on its own.
            Assert.Contains(e1, validTargets);
            Assert.Contains(e2, validTargets);

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
            ScriptableObject.DestroyImmediate(healSkill, true);
        }

        [Test]
        public void GetValidTargets_AOEHealingSkill_RejectsPileAnchorIfNoLivingUnitInAOERange()
        {
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 1);
            e1.state = LifeState.Pile; // pile
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 2, size: 1);
            e2.state = LifeState.Pile; // pile

            InjectTeams(new List<CombatCharacter> { _attacker }, new List<CombatCharacter> { e1, e2 });

            var healSkill = ScriptableObject.CreateInstance<SkillData>();
            healSkill.targetScope = TargetScope.Enemies;
            healSkill.maxTargets = 2;
            healSkill.targetRanks = new List<int> { 1, 2, 3, 4 };
            healSkill.effects = new List<ISkillEffect> { new HealEffect() };

            var validTargets = _battleSystem.GetValidTargets(_attacker, healSkill);

            // Neither E1 nor E2 are alive, and for E1 the trailing targets contain only piles.
            // So neither should be a valid primary anchor for a healing skill.
            Assert.IsFalse(validTargets.Contains(e1));
            Assert.IsFalse(validTargets.Contains(e2));

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
            ScriptableObject.DestroyImmediate(healSkill, true);
        }
    }
}
