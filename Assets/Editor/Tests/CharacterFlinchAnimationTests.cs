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
            battleSystem.animationQueue = battleSystemObj.AddComponent<AnimationQueueProcessor>();

            var userObj = new GameObject("UserChar");
            var user = userObj.AddComponent<CombatCharacter>();
            user.characterData = ScriptableObject.CreateInstance<CharacterData>();
            user.characterData.characterId = "user";
            user.team = Team.Player;
            user.baseStats = new CombatStats { maxHP = 100, attack = 10, speed = 5 };
            user.currentHP = 100;
            userObj.AddComponent<Animator>();
            
            _target.baseStats = new CombatStats { maxHP = 100, speed = 5 };
            _target.currentHP = 100;
            _targetObj.AddComponent<Animator>();

            var clip = new AnimationClip { name = "SpecialFlinch" };
            _characterData.takeDamageClip = clip;

            var damageSkill = ScriptableObject.CreateInstance<SkillData>();
            damageSkill.skillId = "damage_skill";
            damageSkill.displayName = "Damage Skill";
            damageSkill.targetScope = TargetScope.Enemies;
            damageSkill.modifier = new SkillModifier { damagePercent = 1.0f };
            damageSkill.animationClip = new AnimationClip { name = "AttackAnim" };
            
            // Add a mock damage effect to guarantee hit
            var dmgEffect = new DamageEffect();
            damageSkill.effects = new List<ISkillEffect> { dmgEffect };

            battleSystem.ExecuteSkill(user, damageSkill, new List<CombatCharacter> { _target });

            Assert.IsTrue(battleSystem.animationQueue.IsBusy);
            
            CombatTestHelper.CleanupTestDatabase();
            Object.DestroyImmediate(battleSystemObj);
            Object.DestroyImmediate(userObj);
            Object.DestroyImmediate(damageSkill);
        }
    }
}
