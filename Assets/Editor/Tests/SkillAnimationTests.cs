using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Tests
{
    public class SkillAnimationTests
    {
        private GameObject _battleSystemObj;
        private BattleSystem _battleSystem;
        private GameObject _userObj;
        private CombatCharacter _user;
        private GameObject _targetObj;
        private CombatCharacter _target;
        private Animator _animator;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();

            _battleSystemObj = new GameObject("BattleSystem");
            _battleSystem = _battleSystemObj.AddComponent<BattleSystem>();
            _battleSystem.animationQueue = _battleSystemObj.AddComponent<AnimationQueueProcessor>();

            _userObj = new GameObject("UserChar");
            _animator = _userObj.AddComponent<Animator>();
            _user = _userObj.AddComponent<CombatCharacter>();
            _user.characterData = ScriptableObject.CreateInstance<CharacterData>();
            _user.characterData.characterId = "user";
            _user.team = Team.Player;
            _user.baseStats = new CombatStats { maxHP = 100, attack = 10, speed = 5 };
            _user.currentHP = 100;

            _targetObj = new GameObject("TargetChar");
            _target = _targetObj.AddComponent<CombatCharacter>();
            _target.characterData = ScriptableObject.CreateInstance<CharacterData>();
            _target.characterData.characterId = "target";
            _target.team = Team.Enemy;
            _target.baseStats = new CombatStats { maxHP = 50, defense = 5 };
            _target.currentHP = 50;

            var prop = typeof(CombatCharacter).GetProperty("animator");
            prop.SetValue(_user, _animator);
        }

        [TearDown]
        public void Teardown()
        {
            CombatTestHelper.CleanupTestDatabase();

            if (_battleSystemObj != null) Object.DestroyImmediate(_battleSystemObj);
            if (_userObj != null) Object.DestroyImmediate(_userObj);
            if (_targetObj != null) Object.DestroyImmediate(_targetObj);
        }

        [Test]
        public void SkillData_HasAnimationClipField()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            var clip = new AnimationClip();
            clip.name = "FireballClip";
            skill.animationClip = clip;

            Assert.AreEqual("FireballClip", skill.animationClip.name);
        }

        [Test]
        public void ExecuteSkill_WithAnimationClip_EnqueuesSkillAnimation()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "custom_anim_skill";
            skill.displayName = "Custom Skill";
            skill.targetScope = TargetScope.Enemies;
            skill.animationClip = new AnimationClip { name = "HeavySlashClip" };

            _battleSystem.ExecuteSkill(_user, skill, new List<CombatCharacter> { _target });

            Assert.IsTrue(_battleSystem.animationQueue.IsBusy);
        }

        [Test]
        public void ExecuteSkill_NullAnimationClip_LogsErrorAndFallsBackToGenericState()
        {
            var attackSkill = ScriptableObject.CreateInstance<SkillData>();
            attackSkill.skillId = "attack_skill";
            attackSkill.displayName = "Basic Attack";
            attackSkill.targetScope = TargetScope.Enemies;
            attackSkill.animationClip = null;

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "[BattleSystem] Skill 'Basic Attack' (attack_skill) has no AnimationClip assigned on UserChar! Falling back to generic animation.");

            _battleSystem.ExecuteSkill(_user, attackSkill, new List<CombatCharacter> { _target });

            Assert.IsTrue(_battleSystem.animationQueue.IsBusy);
        }
    }
}
