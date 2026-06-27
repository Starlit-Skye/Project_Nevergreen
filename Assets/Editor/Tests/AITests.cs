using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Combat.AI;
using Nevergreen.Combat.AI.Nodes;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class AITests
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

            // Setup a dummy character to host the brain
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

        private void SetEnemyTeam(List<CombatCharacter> team)
        {
            typeof(BattleSystem).GetField("_enemyTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_battleSystem, team);
        }

        [Test]
        public void AIBrain_EmptyProfile_ReturnsPassTurn()
        {
            _brain.profile = ScriptableObject.CreateInstance<EnemyAIProfile>();
            
            AIDecision decision = _brain.EvaluateTurn(_battleSystem);
            
            Assert.IsTrue(decision.isPass);
            Assert.IsNull(decision.skill);
            Assert.IsNull(decision.targets);
        }

        [Test]
        public void HealthCondition_SelfLessThan_EvaluatesCorrectly()
        {
            var condition = new HealthCondition
            {
                target = HealthCondition.ComparisonTarget.Self,
                comparison = HealthCondition.ComparisonOp.LessThan,
                threshold = 50f,
                usePercentage = true
            };

            // HP is 100/100, should be false
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem));

            // HP is 40/100, should be true
            _brainChar.currentHP = 40;
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem));
        }

        [Test]
        public void HealthCondition_AnyAllyLessThan_EvaluatesCorrectly()
        {
            var condition = new HealthCondition
            {
                target = HealthCondition.ComparisonTarget.AnyAlly,
                comparison = HealthCondition.ComparisonOp.LessThanOrEqual,
                threshold = 30f,
                usePercentage = false // Test absolute HP
            };

            var ally1 = CombatTestHelper.CreateCombatCharacter("ally_1", Team.Enemy, 2, maxHP: 100);
            var ally2 = CombatTestHelper.CreateCombatCharacter("ally_2", Team.Enemy, 3, maxHP: 100);
            ally1.currentHP = 100;
            ally2.currentHP = 50;

            SetEnemyTeam(new List<CombatCharacter> { _brainChar, ally1, ally2 });

            // Lowest ally HP is 50, condition is <= 30
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem));

            // Drop ally2 to 20
            ally2.currentHP = 20;
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem));

            Object.DestroyImmediate(ally1.gameObject);
            Object.DestroyImmediate(ally2.gameObject);
        }

        [Test]
        public void SimpleTargeting_LowestHP_FindsCorrectTarget()
        {
            var targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.LowestHP };
            
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            var p2 = CombatTestHelper.CreateCombatCharacter("p2", Team.Player, 2, maxHP: 100);
            var p3 = CombatTestHelper.CreateCombatCharacter("p3", Team.Player, 3, maxHP: 100);
            
            p1.currentHP = 80;
            p2.currentHP = 20; // Lowest
            p3.currentHP = 50;

            SetPlayerTeam(new List<CombatCharacter> { p1, p2, p3 });
            
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.maxTargets = 1;

            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);
            
            Assert.IsTrue(success);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(p2, targets[0]);

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
            Object.DestroyImmediate(p3.gameObject);
        }

        [Test]
        public void RuleBasedBehavior_ConditionMet_ReturnsDecision()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.maxTargets = 1;

            // Make sure the character actually has the skill and is in a valid rank
            _brainChar.equippedSkills.Add(skill);

            var rule = new RuleBasedBehavior
            {
                skillToUse = skill,
                conditions = new List<AIConditionNode>
                {
                    new HealthCondition { target = HealthCondition.ComparisonTarget.Self, comparison = HealthCondition.ComparisonOp.LessThan, threshold = 50f, usePercentage = true }
                },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.HighestHP }
            };

            // Set up brain char state
            _brainChar.currentHP = 30; // Condition met (< 50%)
            
            bool success = rule.TryGetDecision(_brain, _battleSystem, out AIDecision decision);
            
            Assert.IsTrue(success);
            Assert.IsFalse(decision.isPass);
            Assert.AreEqual(skill, decision.skill);
            Assert.AreEqual(p1, decision.targets[0]);

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void AIBrain_Priority_SelectsFirstValidBehavior()
        {
            var profile = ScriptableObject.CreateInstance<EnemyAIProfile>();
            
            var skill1 = CombatTestHelper.CreateDamageSkill();
            skill1.skillId = "skill_1";
            var skill2 = CombatTestHelper.CreateDamageSkill();
            skill2.skillId = "skill_2";

            _brainChar.equippedSkills.Add(skill1);
            _brainChar.equippedSkills.Add(skill2);

            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1);
            SetPlayerTeam(new List<CombatCharacter> { p1 });

            // Behavior 1: Only works if self HP < 20%
            var rule1 = new RuleBasedBehavior
            {
                skillToUse = skill1,
                conditions = new List<AIConditionNode>
                {
                    new HealthCondition { target = HealthCondition.ComparisonTarget.Self, comparison = HealthCondition.ComparisonOp.LessThan, threshold = 20f }
                },
                targeting = new SimpleTargeting()
            };

            // Behavior 2: Fallback random
            var rule2 = new RandomSkillBehavior();

            profile.behaviors.Add(rule1);
            profile.behaviors.Add(rule2);
            _brain.profile = profile;

            // HP is 100, rule 1 should fail, rule 2 should run
            _brainChar.currentHP = 100;
            var decision = _brain.EvaluateTurn(_battleSystem);
            
            Assert.IsFalse(decision.isPass);
            
            var strictRule2 = new RuleBasedBehavior
            {
                skillToUse = skill2,
                conditions = new List<AIConditionNode>(), // No conditions
                targeting = new SimpleTargeting()
            };
            profile.behaviors[1] = strictRule2;

            var decision2 = _brain.EvaluateTurn(_battleSystem);
            Assert.AreEqual(skill2, decision2.skill, "Should fallback to rule 2 since rule 1 conditions failed.");

            // Now make rule 1 succeed
            _brainChar.currentHP = 10;
            var decision3 = _brain.EvaluateTurn(_battleSystem);
            Assert.AreEqual(skill1, decision3.skill, "Should select rule 1 since conditions are now met.");

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void HasStatusCondition_OpponentMarked_EvaluatesCorrectly()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1);
            SetPlayerTeam(new List<CombatCharacter> { p1 });

            var condition = new HasStatusCondition
            {
                target = HasStatusCondition.ComparisonTarget.AnyEnemy,
                statusType = StatusType.Mark
            };

            // Player is not marked
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem));

            // Mark the player
            p1.AddStatus(new StatusEffectInstance(StatusType.Mark, 0, 1));
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem));

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void StatusPrioritizedTargeting_Strict_FailsIfNoMarkedTargets()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1);
            SetPlayerTeam(new List<CombatCharacter> { p1 });

            var targeting = new StatusPrioritizedTargeting
            {
                statusType = StatusType.Mark,
                strict = true
            };
            
            var skill = CombatTestHelper.CreateDamageSkill();

            // Should return false because no one is marked
            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);
            Assert.IsFalse(success);
            Assert.IsNull(targets);

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void StatusPrioritizedTargeting_Strict_FindsMarkedTarget()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1);
            var p2 = CombatTestHelper.CreateCombatCharacter("p2", Team.Player, 2);
            SetPlayerTeam(new List<CombatCharacter> { p1, p2 });

            // Mark p2 only
            p2.AddStatus(new StatusEffectInstance(StatusType.Mark, 0, 1));

            var targeting = new StatusPrioritizedTargeting
            {
                statusType = StatusType.Mark,
                strict = true
            };
            
            var skill = CombatTestHelper.CreateDamageSkill();

            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);
            Assert.IsTrue(success);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(p2, targets[0], "Should target the marked character.");

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
        }

        [Test]
        public void StatusPrioritizedTargeting_NonStrict_FallsBack()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1);
            var p2 = CombatTestHelper.CreateCombatCharacter("p2", Team.Player, 2);
            SetPlayerTeam(new List<CombatCharacter> { p1, p2 });
            p1.currentHP = 100;
            p2.currentHP = 50;

            var targeting = new StatusPrioritizedTargeting
            {
                statusType = StatusType.Mark,
                strict = false, // non-strict
                sortingStrategy = SimpleTargeting.Strategy.LowestHP
            };
            
            var skill = CombatTestHelper.CreateDamageSkill();

            // No one is marked, should fallback to LowestHP (p2)
            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);
            Assert.IsTrue(success);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(p2, targets[0]);

            // Now mark p1
            p1.AddStatus(new StatusEffectInstance(StatusType.Mark, 0, 1));

            // Even though p2 has lower HP, p1 has Mark and we prioritize marked
            bool successMark = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targetsMark);
            Assert.IsTrue(successMark);
            Assert.AreEqual(1, targetsMark.Count);
            Assert.AreEqual(p1, targetsMark[0], "Should prioritize the marked character even if HP is higher.");

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
        }

        [Test]
        public void AIBrain_Fallthrough_WithStatusRules()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1);
            SetPlayerTeam(new List<CombatCharacter> { p1 });

            var skillSpecific = CombatTestHelper.CreateDamageSkill();
            skillSpecific.skillId = "skill_specific";
            var skillRandom = CombatTestHelper.CreateDamageSkill();
            skillRandom.skillId = "skill_random";

            _brainChar.equippedSkills.Add(skillSpecific);
            _brainChar.equippedSkills.Add(skillRandom);

            var profile = ScriptableObject.CreateInstance<EnemyAIProfile>();

            // Behavior 1: Only use specific skill on marked target
            var rule1 = new RuleBasedBehavior
            {
                skillToUse = skillSpecific,
                conditions = new List<AIConditionNode>
                {
                    new HasStatusCondition { target = HasStatusCondition.ComparisonTarget.AnyEnemy, statusType = StatusType.Mark }
                },
                targeting = new StatusPrioritizedTargeting { statusType = StatusType.Mark, strict = true }
            };

            // Behavior 2: Fallback random
            var rule2 = new RuleBasedBehavior
            {
                skillToUse = skillRandom,
                conditions = new List<AIConditionNode>(),
                targeting = new SimpleTargeting()
            };

            profile.behaviors.Add(rule1);
            profile.behaviors.Add(rule2);
            _brain.profile = profile;

            // p1 is not marked, should fallback to rule2
            var decision = _brain.EvaluateTurn(_battleSystem);
            Assert.AreEqual(skillRandom, decision.skill);

            // mark p1
            p1.AddStatus(new StatusEffectInstance(StatusType.Mark, 0, 1));
            
            // p1 is marked, should use rule1
            var decision2 = _brain.EvaluateTurn(_battleSystem);
            Assert.AreEqual(skillSpecific, decision2.skill);
            Assert.AreEqual(p1, decision2.targets[0]);

            Object.DestroyImmediate(p1.gameObject);
        }
        [Test]
        public void TeamCountCondition_ExcludesPilesAndEvaluatesCorrectly()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("Player1", Team.Player, 1);
            var p2 = CombatTestHelper.CreateCombatCharacter("Player2", Team.Player, 2);
            _battleSystem.PlayerTeam.Add(p1);
            _battleSystem.PlayerTeam.Add(p2);

            var condition = new TeamCountCondition 
            { 
                targetTeam = TeamCountCondition.TargetTeam.PlayerTeam,
                comparison = TeamCountCondition.ComparisonOp.Equals,
                targetCount = 2
            };

            // Both alive, count = 2
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem));

            // Set p2 to pile, should count as 1
            p2.state = LifeState.Pile;
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem));
            
            condition.targetCount = 1;
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem));

            // Set p1 to destroyed, should count as 0
            p1.state = LifeState.Destroyed;
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem));
            
            condition.targetCount = 0;
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem));

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
        }

        [Test]
        public void TeamCountCondition_Comparisons_WorkCorrectly()
        {
            var e1 = CombatTestHelper.CreateCombatCharacter("Enemy1", Team.Enemy, 1);
            var e2 = CombatTestHelper.CreateCombatCharacter("Enemy2", Team.Enemy, 2);
            var e3 = CombatTestHelper.CreateCombatCharacter("Enemy3", Team.Enemy, 3);
            _battleSystem.EnemyTeam.Add(e1);
            _battleSystem.EnemyTeam.Add(e2);
            _battleSystem.EnemyTeam.Add(e3); // Count is 3

            var condition = new TeamCountCondition 
            { 
                targetTeam = TeamCountCondition.TargetTeam.EnemyTeam,
                targetCount = 2
            };

            condition.comparison = TeamCountCondition.ComparisonOp.GreaterThan;
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem)); // 3 > 2

            condition.comparison = TeamCountCondition.ComparisonOp.LessThan;
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem)); // 3 < 2 is false

            condition.comparison = TeamCountCondition.ComparisonOp.NotEquals;
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem)); // 3 != 2

            condition.targetCount = 3;
            condition.comparison = TeamCountCondition.ComparisonOp.LessThanOrEqual;
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem)); // 3 <= 3

            condition.comparison = TeamCountCondition.ComparisonOp.GreaterThanOrEqual;
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem)); // 3 >= 3

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
            Object.DestroyImmediate(e3.gameObject);
        }
        [Test]
        public void SimpleTargeting_RandomNotSelf_ExcludesSelf()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("Player1", Team.Player, 1);
            var e1 = CombatTestHelper.CreateCombatCharacter("Enemy1", Team.Enemy, 1);
            _battleSystem.PlayerTeam.Add(p1);
            _battleSystem.EnemyTeam.Add(_brainChar);
            _battleSystem.EnemyTeam.Add(e1);

            // Give skill that targets ANY ally (so it could target self or e1)
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.targetScope = TargetScope.Allies;
            
            var targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.RandomNotSelf };

            // Attempt to resolve targets
            bool result = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);

            // Should successfully find e1
            Assert.IsTrue(result);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(e1, targets[0]);

            // Now remove e1, only self remains
            _battleSystem.EnemyTeam.Remove(e1);
            
            // Should fail since RandomNotSelf excludes the only valid target (self)
            bool result2 = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets2);
            Assert.IsFalse(result2);

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(e1.gameObject);
        }
        [Test]
        public void RandomSkillBehavior_ExcludesSkill_WorksCorrectly()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("Player1", Team.Player, 1);
            _battleSystem.PlayerTeam.Add(p1);

            var skill1 = CombatTestHelper.CreateDamageSkill();
            skill1.skillId = "skill_1";
            var skill2 = CombatTestHelper.CreateDamageSkill();
            skill2.skillId = "skill_2";

            _brainChar.equippedSkills.Add(skill1);
            _brainChar.equippedSkills.Add(skill2);

            var behavior = new RandomSkillBehavior
            {
                excludeSkill = skill1
            };

            // It should always pick skill2 since skill1 is excluded
            bool result = behavior.TryGetDecision(_brain, _battleSystem, out var decision);
            
            Assert.IsTrue(result);
            Assert.AreEqual(skill2, decision.skill);

            Object.DestroyImmediate(p1.gameObject);
        }
        [Test]
        public void NotHasStatusCondition_EvaluatesCorrectly()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("Player1", Team.Player, 1);
            var p2 = CombatTestHelper.CreateCombatCharacter("Player2", Team.Player, 2);
            _battleSystem.PlayerTeam.Add(p1);
            _battleSystem.PlayerTeam.Add(p2);

            var condition = new NotHasStatusCondition 
            { 
                target = NotHasStatusCondition.ComparisonTarget.AnyEnemy, // From brain's perspective (Enemy team), the Player team is the Enemy.
                statusType = StatusType.Mark
            };

            // Neither has Mark, so "NotHasStatusCondition" should be TRUE
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem));

            // Give p1 Mark
            p1.AddStatus(new StatusEffectInstance(StatusType.Mark, 0, 2));

            // Now p1 has Mark, so "NotHasStatusCondition" on the enemy team should be FALSE
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem));

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
        }

        [Test]
        public void NotHasStatusCondition_BuffDebuffEvaluatesCorrectly()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("Player1", Team.Player, 1);
            _battleSystem.PlayerTeam.Add(p1);

            var condition = new NotHasStatusCondition 
            { 
                target = NotHasStatusCondition.ComparisonTarget.AnyEnemy, // Player team is enemy
                statusType = StatusType.Buff,
                stat = StatTarget.Speed,
                amplitudeComparison = HealthCondition.ComparisonOp.GreaterThanOrEqual,
                targetAmplitude = 2
            };

            // No Buff on player yet, should return TRUE
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem));

            // Give player Buff on Speed with amplitude 1 (less than target of 2)
            p1.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Speed, 1, 2));

            // Still doesn't match the condition criteria of amplitude >= 2, so technically NO enemy has the matching buff -> TRUE
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem));

            // Give player Buff on Speed with amplitude 3 (matches criteria)
            p1.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Speed, 3, 2));

            // Now p1 has the matching Buff, so it is FALSE that no one has it
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem));

            Object.DestroyImmediate(p1.gameObject);
        }
    }
}
