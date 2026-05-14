using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Combat.AI;
using Nevergreen.Combat.AI.Nodes;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class AIRuleTests
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

        private void SetEnemyTeam(List<CombatCharacter> team)
        {
            typeof(BattleSystem).GetField("_enemyTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_battleSystem, team);
        }

        // ===================================================================
        // RepetitionCondition Tests
        // ===================================================================

        [Test]
        public void RepetitionCondition_AllowsFirstUse()
        {
            var skill = CombatTestHelper.CreateDamageSkill();
            var condition = new RepetitionCondition { maxConsecutiveUses = 2 };

            // Fresh history — no skill used yet
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem, skill));
        }

        [Test]
        public void RepetitionCondition_AllowsUseBelowLimit()
        {
            var skill = CombatTestHelper.CreateDamageSkill();
            var condition = new RepetitionCondition { maxConsecutiveUses = 3 };

            // Simulate using the skill twice
            _brain.History.RecordDecision(AIDecision.UseSkill(skill, new List<CombatCharacter>()));
            _brain.History.RecordDecision(AIDecision.UseSkill(skill, new List<CombatCharacter>()));

            // 2 consecutive uses, limit is 3 — should be allowed
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem, skill));
        }

        [Test]
        public void RepetitionCondition_BlocksAtLimit()
        {
            var skill = CombatTestHelper.CreateDamageSkill();
            var condition = new RepetitionCondition { maxConsecutiveUses = 2 };

            // Simulate using the skill twice (hitting the limit)
            _brain.History.RecordDecision(AIDecision.UseSkill(skill, new List<CombatCharacter>()));
            _brain.History.RecordDecision(AIDecision.UseSkill(skill, new List<CombatCharacter>()));

            // 2 consecutive uses, limit is 2 — should be blocked
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem, skill));
        }

        [Test]
        public void RepetitionCondition_ResetsAfterDifferentSkill()
        {
            var skillA = CombatTestHelper.CreateDamageSkill();
            skillA.skillId = "skill_a";
            var skillB = CombatTestHelper.CreateDamageSkill();
            skillB.skillId = "skill_b";
            var condition = new RepetitionCondition { maxConsecutiveUses = 2 };

            // Use skill A twice (hits limit)
            _brain.History.RecordDecision(AIDecision.UseSkill(skillA, new List<CombatCharacter>()));
            _brain.History.RecordDecision(AIDecision.UseSkill(skillA, new List<CombatCharacter>()));
            Assert.IsFalse(condition.IsMet(_brain, _battleSystem, skillA));

            // Use a different skill — should reset consecutive tracking
            _brain.History.RecordDecision(AIDecision.UseSkill(skillB, new List<CombatCharacter>()));

            // Now skill A should be allowed again
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem, skillA));
        }

        [Test]
        public void RepetitionCondition_DoesNotBlockDifferentSkill()
        {
            var skillA = CombatTestHelper.CreateDamageSkill();
            skillA.skillId = "skill_a";
            var skillB = CombatTestHelper.CreateDamageSkill();
            skillB.skillId = "skill_b";
            var condition = new RepetitionCondition { maxConsecutiveUses = 2 };

            // Use skill A twice (hits limit for A)
            _brain.History.RecordDecision(AIDecision.UseSkill(skillA, new List<CombatCharacter>()));
            _brain.History.RecordDecision(AIDecision.UseSkill(skillA, new List<CombatCharacter>()));

            // Skill B should not be blocked by skill A's repetition
            Assert.IsTrue(condition.IsMet(_brain, _battleSystem, skillB));
        }

        [Test]
        public void RepetitionCondition_IntegrationWithRuleBasedBehavior()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skill = CombatTestHelper.CreateDamageSkill();
            _brainChar.equippedSkills.Add(skill);

            var rule = new RuleBasedBehavior
            {
                skillToUse = skill,
                conditions = new List<AIConditionNode>
                {
                    new RepetitionCondition { maxConsecutiveUses = 2 }
                },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random }
            };

            // First use — should succeed
            Assert.IsTrue(rule.TryGetDecision(_brain, _battleSystem, out AIDecision d1));
            _brain.RecordDecision(d1);

            // Second use — should still succeed (limit is 2)
            Assert.IsTrue(rule.TryGetDecision(_brain, _battleSystem, out AIDecision d2));
            _brain.RecordDecision(d2);

            // Third use — should be blocked (consecutive uses = 2, limit is 2)
            Assert.IsFalse(rule.TryGetDecision(_brain, _battleSystem, out _));

            Object.DestroyImmediate(p1.gameObject);
        }

        // ===================================================================
        // SequenceBehavior Tests
        // ===================================================================

        [Test]
        public void SequenceBehavior_CyclesThroughSkillsInOrder()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill();
            skillA.skillId = "skill_a";
            skillA.displayName = "Skill A";
            var skillB = CombatTestHelper.CreateDamageSkill();
            skillB.skillId = "skill_b";
            skillB.displayName = "Skill B";
            var skillC = CombatTestHelper.CreateDamageSkill();
            skillC.skillId = "skill_c";
            skillC.displayName = "Skill C";

            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);
            _brainChar.equippedSkills.Add(skillC);

            var sequence = new SequenceBehavior
            {
                sequenceId = "test_combo",
                skillSequence = new List<SkillData> { skillA, skillB, skillC },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random }
            };

            // Turn 1: should use skill A
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d1));
            Assert.AreEqual(skillA, d1.skill);
            _brain.RecordDecision(d1);

            // Turn 2: should use skill B
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d2));
            Assert.AreEqual(skillB, d2.skill);
            _brain.RecordDecision(d2);

            // Turn 3: should use skill C
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d3));
            Assert.AreEqual(skillC, d3.skill);
            _brain.RecordDecision(d3);

            // Turn 4: should loop back to skill A
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d4));
            Assert.AreEqual(skillA, d4.skill);

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_SkipsUnavailableSkill_WhenSkipOnFailureEnabled()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill();
            skillA.skillId = "skill_a";
            // Skill B can only be used from rank 4, but enemy is at rank 1
            var skillB = CombatTestHelper.CreateDamageSkill();
            skillB.skillId = "skill_b";
            skillB.useRanks = new List<int> { 4 };
            var skillC = CombatTestHelper.CreateDamageSkill();
            skillC.skillId = "skill_c";

            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);
            _brainChar.equippedSkills.Add(skillC);

            var sequence = new SequenceBehavior
            {
                sequenceId = "test_skip",
                skillSequence = new List<SkillData> { skillA, skillB, skillC },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random },
                skipOnFailure = true
            };

            // Turn 1: skill A — should succeed
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d1));
            Assert.AreEqual(skillA, d1.skill);
            _brain.RecordDecision(d1);

            // Turn 2: skill B is unavailable (wrong rank), should skip to skill C
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d2));
            Assert.AreEqual(skillC, d2.skill, "Should skip unavailable skill B and use skill C.");
            _brain.RecordDecision(d2);

            // Turn 3: should loop back to skill A (index advanced past C)
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d3));
            Assert.AreEqual(skillA, d3.skill);

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_FailsEntirely_WhenSkipOnFailureDisabled()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            // Skill A can only be used from rank 4, but enemy is at rank 1
            var skillA = CombatTestHelper.CreateDamageSkill();
            skillA.skillId = "skill_a";
            skillA.useRanks = new List<int> { 4 };
            var skillB = CombatTestHelper.CreateDamageSkill();
            skillB.skillId = "skill_b";

            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            var sequence = new SequenceBehavior
            {
                sequenceId = "test_noskip",
                skillSequence = new List<SkillData> { skillA, skillB },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random },
                skipOnFailure = false
            };

            // Skill A is current but unavailable — the entire behavior should fail
            Assert.IsFalse(sequence.TryGetDecision(_brain, _battleSystem, out _));

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_IndependentTracking_PerBrain()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });

            // Create a second enemy with its own brain
            var brainChar2 = CombatTestHelper.CreateCombatCharacter("enemy_2", Team.Enemy, 2, maxHP: 100);
            var brain2 = brainChar2.gameObject.AddComponent<AIBrain>();
            SetEnemyTeam(new List<CombatCharacter> { _brainChar, brainChar2 });

            var skillA = CombatTestHelper.CreateDamageSkill();
            skillA.skillId = "skill_a";
            var skillB = CombatTestHelper.CreateDamageSkill();
            skillB.skillId = "skill_b";

            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);
            brainChar2.equippedSkills.Add(skillA);
            brainChar2.equippedSkills.Add(skillB);

            // Same sequence definition, shared across both brains
            var sequence = new SequenceBehavior
            {
                sequenceId = "shared_combo",
                skillSequence = new List<SkillData> { skillA, skillB },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random }
            };

            // Brain 1 takes turn — should use skill A
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d1));
            Assert.AreEqual(skillA, d1.skill);
            _brain.RecordDecision(d1);

            // Brain 2 takes turn — should ALSO use skill A (independent tracking)
            Assert.IsTrue(sequence.TryGetDecision(brain2, _battleSystem, out AIDecision d2));
            Assert.AreEqual(skillA, d2.skill, "Second brain should start its own sequence independently.");
            brain2.RecordDecision(d2);

            // Brain 1 takes next turn — should use skill B
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d3));
            Assert.AreEqual(skillB, d3.skill);

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(brainChar2.gameObject);
        }

        [Test]
        public void SequenceBehavior_EmptySequence_ReturnsFalse()
        {
            var sequence = new SequenceBehavior
            {
                sequenceId = "empty",
                skillSequence = new List<SkillData>(),
                targeting = new SimpleTargeting()
            };

            Assert.IsFalse(sequence.TryGetDecision(_brain, _battleSystem, out _));
        }

        [Test]
        public void AIHistory_SequenceIndex_DefaultsToZero()
        {
            var history = new AIHistory();
            Assert.AreEqual(0, history.GetSequenceIndex("new_sequence"));
        }

        [Test]
        public void AIHistory_SequenceIndex_AdvancesAndWraps()
        {
            var history = new AIHistory();
            string id = "test_seq";
            int length = 3;

            Assert.AreEqual(0, history.GetSequenceIndex(id));
            
            history.AdvanceSequenceIndex(id, length);
            Assert.AreEqual(1, history.GetSequenceIndex(id));
            
            history.AdvanceSequenceIndex(id, length);
            Assert.AreEqual(2, history.GetSequenceIndex(id));
            
            // Should wrap around to 0
            history.AdvanceSequenceIndex(id, length);
            Assert.AreEqual(0, history.GetSequenceIndex(id));
        }
    }
}
