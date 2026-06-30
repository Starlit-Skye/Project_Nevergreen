using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;
using System.Linq;

namespace Nevergreen.Tests
{
    public class StatusEffectOnSpawnTests
    {
        private GameObject _characterObject;
        private CombatCharacter _combatCharacter;
        private StatusEffectOnSpawn _spawnEffect;

        [SetUp]
        public void Setup()
        {
            _characterObject = new GameObject("TestCharacter");
            _combatCharacter = _characterObject.AddComponent<CombatCharacter>();

            // Setup minimal required character data to avoid nullrefs in InitializeForCombat
            var charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.characterId = "test_char";
            
            // Need to setup basic stats so GetStatsForLevel works
            var statBlock = new StatBlockData();
            statBlock.maxHP = 100;
            statBlock.attack = 10;
            statBlock.defense = 10;
            charData.statPerLevel.Add(statBlock);
            
            _combatCharacter.characterData = charData;

            // Add the component we are testing
            _spawnEffect = _characterObject.AddComponent<StatusEffectOnSpawn>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_characterObject);
        }

        [Test]
        public void InitializeForCombat_AppliesStatusEffectOnSpawn()
        {
            // Arrange
            _spawnEffect.statusType = StatusType.Stealth;
            _spawnEffect.duration = 2;
            _spawnEffect.amplitude = 0;
            _spawnEffect.targetStat = StatTarget.MaxHP;

            // Act
            _combatCharacter.InitializeForCombat(Team.Enemy, 1);

            // Assert
            Assert.AreEqual(1, _combatCharacter.statusEffects.Count, "Status effect should be added to the character.");
            
            var appliedEffect = _combatCharacter.statusEffects[0];
            Assert.AreEqual(StatusType.Stealth, appliedEffect.type, "Status effect type should match.");
            Assert.AreEqual(2, appliedEffect.remainingDuration, "Status effect duration should match.");
        }

        [Test]
        public void InitializeForCombat_AppliesMultipleStatusEffectOnSpawn()
        {
            // Arrange
            _spawnEffect.statusType = StatusType.Stealth;
            _spawnEffect.duration = 2;

            var secondEffect = _characterObject.AddComponent<StatusEffectOnSpawn>();
            secondEffect.statusType = StatusType.Buff;
            secondEffect.targetStat = StatTarget.Attack;
            secondEffect.amplitude = 50;

            // Act
            _combatCharacter.InitializeForCombat(Team.Enemy, 1);

            // Assert
            Assert.AreEqual(2, _combatCharacter.statusEffects.Count, "Both status effects should be added.");
            
            Assert.IsTrue(_combatCharacter.statusEffects.Any(e => e.type == StatusType.Stealth), "Should have Stealth effect");
            Assert.IsTrue(_combatCharacter.statusEffects.Any(e => e.type == StatusType.Buff && e.targetStat == StatTarget.Attack && e.amplitude == 50), "Should have Attack Buff effect");
        }

        [Test]
        public void InitializeForCombat_AppliesStealthStatusInstanceSubclass()
        {
            // Arrange
            _spawnEffect.statusType = StatusType.Stealth;
            _spawnEffect.duration = 2;

            // Act
            _combatCharacter.InitializeForCombat(Team.Enemy, 1);

            // Assert
            var appliedEffect = _combatCharacter.statusEffects.FirstOrDefault(e => e.type == StatusType.Stealth);
            Assert.IsNotNull(appliedEffect, "Stealth status should be applied.");
            Assert.IsInstanceOf<StealthStatusInstance>(appliedEffect, "Applied Stealth status should be an instance of StealthStatusInstance subclass.");
        }
    }
}
