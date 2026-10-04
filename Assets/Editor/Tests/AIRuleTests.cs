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
        // RandomSkillBehavior (Repetition Limit) Tests
        // ===================================================================

        [Test]
        public void RandomSkillBehavior_AllowsFirstUse()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skill = CombatTestHelper.CreateDamageSkill();
            _brainChar.equippedSkills.Add(skill);

            var behavior = new RandomSkillBehavior { maxConsecutiveUses = 2 };

            // Fresh history — no skill used yet
            Assert.IsTrue(behavior.TryGetDecision(_brain, _battleSystem, out AIDecision d));
            Assert.AreEqual(skill, d.skill);

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void RandomSkillBehavior_AllowsUseBelowLimit()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skill = CombatTestHelper.CreateDamageSkill();
            _brainChar.equippedSkills.Add(skill);

            var behavior = new RandomSkillBehavior { maxConsecutiveUses = 3 };

            // Simulate using the skill twice
            _brain.History.RecordDecision(AIDecision.UseSkill(skill, new List<CombatCharacter>()));
            _brain.History.RecordDecision(AIDecision.UseSkill(skill, new List<CombatCharacter>()));

            // 2 consecutive uses, limit is 3 — should be allowed
            Assert.IsTrue(behavior.TryGetDecision(_brain, _battleSystem, out AIDecision d));
            Assert.AreEqual(skill, d.skill);

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void RandomSkillBehavior_BlocksAtLimitAndPassesIfNoOtherSkills()
        {
            var skill = CombatTestHelper.CreateDamageSkill();
            _brainChar.equippedSkills.Add(skill);

            var behavior = new RandomSkillBehavior { maxConsecutiveUses = 2 };

            // Simulate using the skill twice (hitting the limit)
            _brain.History.RecordDecision(AIDecision.UseSkill(skill, new List<CombatCharacter>()));
            _brain.History.RecordDecision(AIDecision.UseSkill(skill, new List<CombatCharacter>()));

            // 2 consecutive uses, limit is 2. Skill should be removed from valid skills.
            // Since it's the only skill, it should return Pass.
            Assert.IsTrue(behavior.TryGetDecision(_brain, _battleSystem, out AIDecision d));
            Assert.IsTrue(d.isPass);
        }

        [Test]
        public void RandomSkillBehavior_PicksAlternativeSkillAtLimit()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill();
            skillA.skillId = "skill_a";
            var skillB = CombatTestHelper.CreateDamageSkill();
            skillB.skillId = "skill_b";
            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            var behavior = new RandomSkillBehavior { maxConsecutiveUses = 2 };

            // Use skill A twice (hits limit for A)
            _brain.History.RecordDecision(AIDecision.UseSkill(skillA, new List<CombatCharacter>()));
            _brain.History.RecordDecision(AIDecision.UseSkill(skillA, new List<CombatCharacter>()));

            // Skill A is blocked, must pick Skill B
            Assert.IsTrue(behavior.TryGetDecision(_brain, _battleSystem, out AIDecision d));
            Assert.AreEqual(skillB, d.skill);

            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void RandomSkillBehavior_ResetsLimitAfterPassing()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skill = CombatTestHelper.CreateDamageSkill();
            _brainChar.equippedSkills.Add(skill);

            var behavior = new RandomSkillBehavior { maxConsecutiveUses = 1 };

            // 1. Use skill once (hits limit of 1)
            _brain.RecordDecision(AIDecision.UseSkill(skill, new List<CombatCharacter> { p1 }));
            
            // 2. Next turn should be a Pass
            Assert.IsTrue(behavior.TryGetDecision(_brain, _battleSystem, out AIDecision dPass));
            Assert.IsTrue(dPass.isPass);
            _brain.RecordDecision(dPass);

            // 3. Following turn, the limit should be reset because the last action was a Pass (not the skill)
            Assert.IsTrue(behavior.TryGetDecision(_brain, _battleSystem, out AIDecision dSkill));
            Assert.AreEqual(skill, dSkill.skill);

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
                steps = new List<SequenceStep> 
                { 
                    new SequenceStep { skill = skillA }, 
                    new SequenceStep { skill = skillB }, 
                    new SequenceStep { skill = skillC } 
                },
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
                steps = new List<SequenceStep> 
                { 
                    new SequenceStep { skill = skillA }, 
                    new SequenceStep { skill = skillB }, 
                    new SequenceStep { skill = skillC } 
                },
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
                steps = new List<SequenceStep> 
                { 
                    new SequenceStep { skill = skillA }, 
                    new SequenceStep { skill = skillB }
                },
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
                steps = new List<SequenceStep> 
                { 
                    new SequenceStep { skill = skillA }, 
                    new SequenceStep { skill = skillB }
                },
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
                steps = new List<SequenceStep>(),
                targeting = new SimpleTargeting()
            };

            Assert.IsFalse(sequence.TryGetDecision(_brain, _battleSystem, out _));
        }

        private SequenceStep Step(SkillData skill, AIConditionNode condition = null, SkillData elseSkill = null)
        {
            return new SequenceStep { skill = skill, condition = condition, elseSkill = elseSkill };
        }

        [Test]
        public void SequenceBehavior_UsesSkill_WhenConditionMet()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill(); skillA.skillId = "A";
            var skillB = CombatTestHelper.CreateDamageSkill(); skillB.skillId = "B";
            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            // Add buff
            _brainChar.statusEffects.Add(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 5, 3));

            var cond = new HasStatusCondition { target = HasStatusCondition.ComparisonTarget.Self, statusType = StatusType.Buff, stat = StatTarget.Attack };
            var sequence = new SequenceBehavior
            {
                sequenceId = "cond_met",
                steps = new List<SequenceStep> { Step(skillA, cond, skillB) },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random }
            };

            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision decision));
            Assert.AreEqual(skillA, decision.skill);
            
            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_UsesElseSkill_WhenConditionNotMet()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill(); skillA.skillId = "A";
            var skillB = CombatTestHelper.CreateDamageSkill(); skillB.skillId = "B";
            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            // No buff added
            var cond = new HasStatusCondition { target = HasStatusCondition.ComparisonTarget.Self, statusType = StatusType.Buff, stat = StatTarget.Attack };
            var sequence = new SequenceBehavior
            {
                sequenceId = "cond_not_met",
                steps = new List<SequenceStep> { Step(skillA, cond, skillB) },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random }
            };

            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision decision));
            Assert.AreEqual(skillB, decision.skill);
            
            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_ConditionFalseNoElse_SkipsStep_EvenWhenSkipOnFailureDisabled()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill(); skillA.skillId = "A";
            var skillB = CombatTestHelper.CreateDamageSkill(); skillB.skillId = "B";
            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            // Condition is FALSE. No elseSkill. Should skip to B.
            var cond = new HasStatusCondition { target = HasStatusCondition.ComparisonTarget.Self, statusType = StatusType.Buff, stat = StatTarget.Attack };
            var sequence = new SequenceBehavior
            {
                sequenceId = "skip_no_else",
                steps = new List<SequenceStep> { Step(skillA, cond, null), Step(skillB) },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random },
                skipOnFailure = false // even with this false, an intentional condition skip works!
            };

            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision decision));
            Assert.AreEqual(skillB, decision.skill);
            
            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_SelectedBranchUnusable_DoesNotTryOtherBranch()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill(); skillA.skillId = "A"; 
            skillA.useRanks = new List<int> { 4 }; // unusable
            var skillB = CombatTestHelper.CreateDamageSkill(); skillB.skillId = "B";
            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            // Condition is TRUE. It picks A, but A is unusable. It should NOT fall back to B.
            _brainChar.statusEffects.Add(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 5, 3));
            var cond = new HasStatusCondition { target = HasStatusCondition.ComparisonTarget.Self, statusType = StatusType.Buff, stat = StatTarget.Attack };
            
            var sequence = new SequenceBehavior
            {
                sequenceId = "branch_unusable",
                steps = new List<SequenceStep> { Step(skillA, cond, skillB) },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random },
                skipOnFailure = false
            };

            Assert.IsFalse(sequence.TryGetDecision(_brain, _battleSystem, out _));
            
            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_ConditionReevaluatedEachCycle()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill(); skillA.skillId = "A";
            var skillB = CombatTestHelper.CreateDamageSkill(); skillB.skillId = "B";
            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            var cond = new HasStatusCondition { target = HasStatusCondition.ComparisonTarget.Self, statusType = StatusType.Buff, stat = StatTarget.Attack };
            var sequence = new SequenceBehavior
            {
                sequenceId = "reeval",
                steps = new List<SequenceStep> { Step(skillA, cond, skillB) },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random }
            };

            // Cycle 1: No buff -> B
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d1));
            Assert.AreEqual(skillB, d1.skill);
            _brain.RecordDecision(d1);

            // Cycle 2: Buff added -> A
            _brainChar.statusEffects.Add(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 5, 3));
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d2));
            Assert.AreEqual(skillA, d2.skill);
            
            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_IndexWrapsCorrectly_WhenLastStepSkipped()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill(); skillA.skillId = "A";
            var skillB = CombatTestHelper.CreateDamageSkill(); skillB.skillId = "B";
            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            var cond = new HasStatusCondition { target = HasStatusCondition.ComparisonTarget.Self, statusType = StatusType.Buff, stat = StatTarget.Attack };
            var sequence = new SequenceBehavior
            {
                sequenceId = "wrap",
                steps = new List<SequenceStep> { Step(skillA), Step(skillB, cond, null) },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random },
                skipOnFailure = true
            };

            // Turn 1: Uses A. Index goes from 0 -> 1.
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d1));
            Assert.AreEqual(skillA, d1.skill);
            _brain.RecordDecision(d1);

            // Turn 2: Starts at 1. Condition is FALSE. No elseSkill. Skips index 1 -> wraps to 0. Uses A.
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision d2));
            Assert.AreEqual(skillA, d2.skill);
            
            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_ExpiredStatus_TreatedAsAbsent()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill(); skillA.skillId = "A";
            var skillB = CombatTestHelper.CreateDamageSkill(); skillB.skillId = "B";
            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            // Add expired buff
            _brainChar.statusEffects.Add(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 5, 0));

            var cond = new HasStatusCondition { target = HasStatusCondition.ComparisonTarget.Self, statusType = StatusType.Buff, stat = StatTarget.Attack };
            var sequence = new SequenceBehavior
            {
                sequenceId = "expired",
                steps = new List<SequenceStep> { Step(skillA, cond, skillB) },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random }
            };

            // Should use B since buff is expired
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision decision));
            Assert.AreEqual(skillB, decision.skill);
            
            Object.DestroyImmediate(p1.gameObject);
        }

        [Test]
        public void SequenceBehavior_BuffOnDifferentStat_DoesNotMatch()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var skillA = CombatTestHelper.CreateDamageSkill(); skillA.skillId = "A";
            var skillB = CombatTestHelper.CreateDamageSkill(); skillB.skillId = "B";
            _brainChar.equippedSkills.Add(skillA);
            _brainChar.equippedSkills.Add(skillB);

            // Add defense buff, but we check for attack buff
            _brainChar.statusEffects.Add(new StatusEffectInstance(StatusType.Buff, StatTarget.Defense, 5, 3));

            var cond = new HasStatusCondition { target = HasStatusCondition.ComparisonTarget.Self, statusType = StatusType.Buff, stat = StatTarget.Attack };
            var sequence = new SequenceBehavior
            {
                sequenceId = "diff_stat",
                steps = new List<SequenceStep> { Step(skillA, cond, skillB) },
                targeting = new SimpleTargeting { strategy = SimpleTargeting.Strategy.Random }
            };

            // Should use B since Attack buff is missing
            Assert.IsTrue(sequence.TryGetDecision(_brain, _battleSystem, out AIDecision decision));
            Assert.AreEqual(skillB, decision.skill);
            
            Object.DestroyImmediate(p1.gameObject);
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

        // ===================================================================
        // SpecificCharacterTargeting Tests
        // ===================================================================

        [Test]
        public void SpecificCharacterTargeting_TargetsCorrectCharacter_WhenPresentInValidPool()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            var p2 = CombatTestHelper.CreateCombatCharacter("p2", Team.Player, 2, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1, p2 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            // Create a matching CharacterData scriptable object
            var targetData = ScriptableObject.CreateInstance<CharacterData>();
            targetData.characterId = "p2";

            var targeting = new SpecificCharacterTargeting { targetCharacterData = targetData };
            var skill = CombatTestHelper.CreateDamageSkill();

            // Setup mock valid pool targeting
            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);

            Assert.IsTrue(success, "Targeting should succeed when the character is present.");
            Assert.IsNotNull(targets, "Targets list should not be null.");
            Assert.AreEqual(1, targets.Count, "Should target exactly one character (the matched one).");
            Assert.AreEqual("p2", targets[0].CharacterId, "Should target the correct specific character.");

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
            Object.DestroyImmediate(targetData);
        }

        [Test]
        public void SpecificCharacterTargeting_ReturnsFalse_WhenCharacterNotPresent()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            var targetData = ScriptableObject.CreateInstance<CharacterData>();
            targetData.characterId = "missing_character";

            var targeting = new SpecificCharacterTargeting { targetCharacterData = targetData };
            var skill = CombatTestHelper.CreateDamageSkill();

            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);

            Assert.IsFalse(success, "Targeting should fail when the character is missing.");
            Assert.IsNull(targets, "Targets should be null on failure.");

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(targetData);
        }

        [Test]
        public void SpecificCharacterTargeting_ReturnsFalse_WhenCharacterDataNotAssigned()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1, maxHP: 100);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _brainChar });

            // Pass null for targetCharacterData
            var targeting = new SpecificCharacterTargeting { targetCharacterData = null };
            var skill = CombatTestHelper.CreateDamageSkill();

            bool success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out var targets);

            Assert.IsFalse(success, "Targeting should fail when the character data is null.");
            Assert.IsNull(targets, "Targets should be null on failure.");

            // Also test when CharacterData exists but ID is null
            var emptyData = ScriptableObject.CreateInstance<CharacterData>();
            emptyData.characterId = "";
            targeting.targetCharacterData = emptyData;

            success = targeting.TryResolveTargets(_brain, _battleSystem, skill, out targets);
            Assert.IsFalse(success, "Targeting should fail when the character data ID is empty.");

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(emptyData);
        }
    }
}
