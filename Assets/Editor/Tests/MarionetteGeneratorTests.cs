using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class MarionetteGeneratorTests
    {
        private CharacterData _template;
        private MarionetteDatabase _marionetteDb;
        private TraitDatabase _traitDb;
        private CombatConfig _config;

        [SetUp]
        public void Setup()
        {
            _template = ScriptableObject.CreateInstance<CharacterData>();
            _template.totalSkillPool = new List<SkillData>();
            for (int i = 0; i < 6; i++)
            {
                var skill = ScriptableObject.CreateInstance<SkillData>();
                skill.skillId = $"skill_{i}";
                _template.totalSkillPool.Add(skill);
            }

            _marionetteDb = ScriptableObject.CreateInstance<MarionetteDatabase>();
            _marionetteDb.marionettes = new List<CharacterData> { _template };

            _traitDb = ScriptableObject.CreateInstance<TraitDatabase>();
            _traitDb.perfections = new List<TraitData>();
            _traitDb.imperfections = new List<TraitData>();

            for (int i = 0; i < 3; i++)
            {
                var perf = ScriptableObject.CreateInstance<TraitData>();
                perf.traitId = $"perf_{i}";
                perf.traitType = TraitType.Perfection;
                _traitDb.perfections.Add(perf);

                var imp = ScriptableObject.CreateInstance<TraitData>();
                imp.traitId = $"imp_{i}";
                imp.traitType = TraitType.Imperfection;
                _traitDb.imperfections.Add(imp);
            }

            _config = ScriptableObject.CreateInstance<CombatConfig>();
            _config.maxPerfections = 3;
            _config.maxImperfections = 3;
        }

        [Test]
        public void GenerateRandomMarionette_ValidInput_Generates4UniqueSkillsAnd1Perf1Imp()
        {
            // Act
            var info = MarionetteGenerator.GenerateRandomMarionette(_marionetteDb, _traitDb, _config);

            // Assert
            Assert.IsNotNull(info);
            Assert.AreEqual(_template, info.character);
            Assert.AreEqual(4, info.equippedSkills.Count, "Should equip exactly 4 skills.");
            
            // Check uniqueness
            var uniqueSkills = new HashSet<SkillData>(info.equippedSkills);
            Assert.AreEqual(4, uniqueSkills.Count, "Skills should be unique.");

            Assert.AreEqual(1, info.perfections.Count, "Should have exactly 1 perfection.");
            Assert.AreEqual(1, info.imperfections.Count, "Should have exactly 1 imperfection.");
        }

        [Test]
        public void GenerateRandomMarionette_LessThan4SkillsInPool_EquipsAllAvailable()
        {
            // Arrange
            _template.totalSkillPool.RemoveAt(0);
            _template.totalSkillPool.RemoveAt(0);
            _template.totalSkillPool.RemoveAt(0);
            _template.totalSkillPool.RemoveAt(0); // Only 2 left

            // Act
            var info = MarionetteGenerator.GenerateRandomMarionette(_marionetteDb, _traitDb, _config);

            // Assert
            Assert.AreEqual(2, info.equippedSkills.Count, "Should equip all available skills if pool is less than 4.");
        }

        [Test]
        public void GenerateRandomMarionette_EmptyTraitDb_NoTraitsAssigned()
        {
            // Arrange
            _traitDb.perfections.Clear();
            _traitDb.imperfections.Clear();

            // Act
            var info = MarionetteGenerator.GenerateRandomMarionette(_marionetteDb, _traitDb, _config);

            // Assert
            Assert.AreEqual(0, info.perfections.Count);
            Assert.AreEqual(0, info.imperfections.Count);
        }

        [Test]
        public void GenerateRandomMarionette_NullDatabase_ReturnsNull()
        {
            // Assert expecting an error log
            LogAssert.Expect(LogType.Error, "[MarionetteGenerator] MarionetteDatabase is null or empty!");

            // Act
            var info = MarionetteGenerator.GenerateRandomMarionette(null, _traitDb, _config);

            // Assert
            Assert.IsNull(info);
        }
    }
}
