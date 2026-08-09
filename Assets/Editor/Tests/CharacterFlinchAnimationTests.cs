using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Tests
{
    public class CharacterFlinchAnimationTests
    {
        private GameObject _targetObj;
        private CombatCharacter _target;
        private CharacterData _characterData;

        [SetUp]
        public void Setup()
        {
            _targetObj = new GameObject("TargetChar");
            _target = _targetObj.AddComponent<CombatCharacter>();
            
            _characterData = ScriptableObject.CreateInstance<CharacterData>();
            _characterData.characterId = "test_target";
            _target.characterData = _characterData;
            _target.team = Team.Enemy;
        }

        [TearDown]
        public void Teardown()
        {
            if (_targetObj != null) Object.DestroyImmediate(_targetObj);
            if (_characterData != null) Object.DestroyImmediate(_characterData);
        }

        [Test]
        public void CombatCharacter_WithCustomTakeDamageClip_ResolvesCorrectStateAndDuration()
        {
            var clip = new AnimationClip { name = "CustomFlinchClip" };
            _characterData.takeDamageClip = clip;

            Assert.AreEqual("CustomFlinchClip", _target.TakeDamageStateName);
        }

        [Test]
        public void CombatCharacter_WithNullTakeDamageClip_FallsBackToTakeDamage()
        {
            _characterData.takeDamageClip = null;

            Assert.AreEqual("TakeDamage", _target.TakeDamageStateName);
            Assert.AreEqual(0.5f, _target.TakeDamageClipDuration);
        }

        [Test]
        public void ExecuteSkill_WithDamage_EnqueuesTargetTakeDamageState()
        {
            CombatTestHelper.InitializeTestDatabase();
            var battleSystemObj = new GameObject("BattleSystem");
            var battleSystem = battleSystemObj.AddComponent<BattleSystem>();

            var userObj = new GameObject("UserChar");
            var user = userObj.AddComponent<CombatCharacter>();
            user.characterData = ScriptableObject.CreateInstance<CharacterData>();
            user.characterData.characterId = "user";
            user.team = Team.Player;
            user.baseStats = new CombatStats { maxHP = 100, attack = 10, speed = 5 };
            user.currentHP = 100;
            var userAnim = userObj.AddComponent<Animator>();
            SetAnimator(user, userAnim);
            
            _target.baseStats = new CombatStats { maxHP = 100, speed = 5 };
            _target.currentHP = 100;
            var targetAnim = _targetObj.AddComponent<Animator>();
            SetAnimator(_target, targetAnim);

            var clip = new AnimationClip { name = "SpecialFlinch" };
            _characterData.takeDamageClip = clip;

            var damageSkill = ScriptableObject.CreateInstance<SkillData>();
            damageSkill.skillId = "damage_skill";
            damageSkill.displayName = "Damage Skill";
            damageSkill.targetScope = TargetScope.Enemies;
            damageSkill.modifier = new SkillModifier { damagePercent = 1.0f };
            damageSkill.animationClip = new AnimationClip { name = "AttackAnim" };
            damageSkill.guaranteedHit = true; // Ensure hit for the test
            
            // Add a mock damage effect to guarantee hit
            var dmgEffect = new DamageEffect();
            damageSkill.effects = new List<ISkillEffect> { dmgEffect };

            battleSystem.ExecuteSkill(user, damageSkill, new List<CombatCharacter> { _target });

            StringAssert.Contains("hit_" + _target.DisplayName, GetQueuedStepNames(battleSystem.animationQueue));
            
            CombatTestHelper.CleanupTestDatabase();
            Object.DestroyImmediate(battleSystemObj);
            Object.DestroyImmediate(userObj);
            Object.DestroyImmediate(damageSkill);
        }

        [Test]
        public void ExecuteSkill_WithStatusOnAlly_DoesNotEnqueueTakeDamageState()
        {
            CombatTestHelper.InitializeTestDatabase();
            var battleSystemObj = new GameObject("BattleSystem");
            var battleSystem = battleSystemObj.AddComponent<BattleSystem>();

            var userObj = new GameObject("UserChar");
            var user = userObj.AddComponent<CombatCharacter>();
            user.team = Team.Player;
            user.baseStats = new CombatStats { maxHP = 100, attack = 10, speed = 5 };
            user.currentHP = 100;
            var userAnim = userObj.AddComponent<Animator>();
            SetAnimator(user, userAnim);
            
            _target.team = Team.Player; // Same team
            _target.baseStats = new CombatStats { maxHP = 100, speed = 5 };
            _target.currentHP = 100;
            var targetAnim = _targetObj.AddComponent<Animator>();
            SetAnimator(_target, targetAnim);

            var statusSkill = ScriptableObject.CreateInstance<SkillData>();
            statusSkill.skillId = "buff_skill";
            statusSkill.animationClip = new AnimationClip { name = "Anim" };
            statusSkill.targetScope = TargetScope.Allies;
            statusSkill.modifier = new SkillModifier { damagePercent = 0f, healPercent = 0f };
            statusSkill.effects = new List<ISkillEffect> { new StatusEffect() }; 

            battleSystem.ExecuteSkill(user, statusSkill, new List<CombatCharacter> { _target });

            StringAssert.DoesNotContain("hit_" + _target.DisplayName, GetQueuedStepNames(battleSystem.animationQueue));
            
            CombatTestHelper.CleanupTestDatabase();
            Object.DestroyImmediate(battleSystemObj);
            Object.DestroyImmediate(userObj);
            Object.DestroyImmediate(statusSkill);
        }

        [Test]
        public void ExecuteSkill_WithStatusOnEnemy_EnqueuesTakeDamageState()
        {
            CombatTestHelper.InitializeTestDatabase();
            var battleSystemObj = new GameObject("BattleSystem");
            var battleSystem = battleSystemObj.AddComponent<BattleSystem>();


            var userObj = new GameObject("UserChar");
            var user = userObj.AddComponent<CombatCharacter>();
            user.team = Team.Player;
            user.baseStats = new CombatStats { maxHP = 100, attack = 10, speed = 5 };
            user.currentHP = 100;
            var userAnim = userObj.AddComponent<Animator>();
            SetAnimator(user, userAnim);
            
            _target.team = Team.Enemy; // Different team
            _target.baseStats = new CombatStats { maxHP = 100, speed = 5 };
            _target.currentHP = 100;
            var targetAnim = _targetObj.AddComponent<Animator>();
            SetAnimator(_target, targetAnim);

            var statusSkill = ScriptableObject.CreateInstance<SkillData>();
            statusSkill.skillId = "debuff_skill";
            statusSkill.animationClip = new AnimationClip { name = "Anim" };
            statusSkill.targetScope = TargetScope.Enemies;
            statusSkill.modifier = new SkillModifier { damagePercent = 0f, healPercent = 0f };
            statusSkill.guaranteedHit = true;
            statusSkill.effects = new List<ISkillEffect> { new StatusEffect() }; 

            battleSystem.ExecuteSkill(user, statusSkill, new List<CombatCharacter> { _target });

            StringAssert.Contains("hit_" + _target.DisplayName, GetQueuedStepNames(battleSystem.animationQueue));
            
            CombatTestHelper.CleanupTestDatabase();
            Object.DestroyImmediate(battleSystemObj);
            Object.DestroyImmediate(userObj);
            Object.DestroyImmediate(statusSkill);
        }

        [Test]
        public void ExecuteSkill_WithHeal_DoesNotEnqueueTakeDamageState()
        {
            CombatTestHelper.InitializeTestDatabase();
            var battleSystemObj = new GameObject("BattleSystem");
            var battleSystem = battleSystemObj.AddComponent<BattleSystem>();


            var userObj = new GameObject("UserChar");
            var user = userObj.AddComponent<CombatCharacter>();
            user.team = Team.Player;
            user.baseStats = new CombatStats { maxHP = 100, attack = 10, speed = 5 };
            user.currentHP = 100;
            var userAnim = userObj.AddComponent<Animator>();
            SetAnimator(user, userAnim);
            
            _target.team = Team.Player; // Same team
            _target.baseStats = new CombatStats { maxHP = 100, speed = 5 };
            _target.currentHP = 100;
            var targetAnim = _targetObj.AddComponent<Animator>();
            SetAnimator(_target, targetAnim);

            var healSkill = ScriptableObject.CreateInstance<SkillData>();
            healSkill.skillId = "heal_skill";
            healSkill.animationClip = new AnimationClip { name = "Anim" };
            healSkill.targetScope = TargetScope.Allies;
            healSkill.modifier = new SkillModifier { damagePercent = 0f, healPercent = 1.0f };
            healSkill.effects = new List<ISkillEffect> { new HealEffect() }; 

            battleSystem.ExecuteSkill(user, healSkill, new List<CombatCharacter> { _target });

            StringAssert.DoesNotContain("hit_" + _target.DisplayName, GetQueuedStepNames(battleSystem.animationQueue));
            
            CombatTestHelper.CleanupTestDatabase();
            Object.DestroyImmediate(battleSystemObj);
            Object.DestroyImmediate(userObj);
            Object.DestroyImmediate(healSkill);
        }

        private string GetQueuedStepNames(AnimationQueueProcessor queue)
        {
            var queueField = typeof(AnimationQueueProcessor).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var queueObj = (Queue<IAnimationStep>)queueField.GetValue(queue);
            string names = "";
            foreach (var step in queueObj)
            {
                names += "[" + step.Name + "]";
                if (step is ParallelStep parallelStep)
                {
                    var stepsField = typeof(ParallelStep).GetField("_steps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var steps = (List<IAnimationStep>)stepsField.GetValue(parallelStep);
                    foreach (var s in steps)
                    {
                        names += "(" + s.Name + ")";
                    }
                }
            }
            return names;
        }

        private void SetAnimator(CombatCharacter character, Animator animator)
        {
            typeof(CombatCharacter).GetProperty("animator").SetValue(character, animator);
        }
    }
}
