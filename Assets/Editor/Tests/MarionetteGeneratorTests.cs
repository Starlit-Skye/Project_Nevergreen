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

            // Ensure CurrentParty is clean
            RunSessionManager.CurrentParty = new List<PartyMemberInfo>();
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.CurrentParty = new List<PartyMemberInfo>();
        }

        [Test]
        public void GenerateRandomMarionette_ValidInput_Generates4UniqueSkillsAnd1Perf1Imp()
        {
            // Act
            var list = MarionetteGenerator.GenerateRandomMarionette(1, _marionetteDb, _traitDb, _config);
            var info = list[0];

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
            var list = MarionetteGenerator.GenerateRandomMarionette(1, _marionetteDb, _traitDb, _config);
            var info = list[0];

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
            var list = MarionetteGenerator.GenerateRandomMarionette(1, _marionetteDb, _traitDb, _config);
            var info = list[0];

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
            var info = MarionetteGenerator.GenerateRandomMarionette(1, null, _traitDb, _config);

            // Assert
            Assert.IsNull(info);
        }

        [Test]
        public void GenerateRandomMarionette_EnforcesUniqueClasses()
        {
            // Arrange
            _marionetteDb.marionettes.Clear();
            for (int i = 0; i < 5; i++)
            {
                var temp = ScriptableObject.CreateInstance<CharacterData>();
                temp.characterId = $"char_{i}";
                _marionetteDb.marionettes.Add(temp);
            }

            // Act
            var list = MarionetteGenerator.GenerateRandomMarionette(3, _marionetteDb, _traitDb, _config);

            // Assert
            Assert.AreEqual(3, list.Count);
            var uniqueIds = new HashSet<string>();
            foreach (var item in list)
            {
                uniqueIds.Add(item.character.characterId);
            }
            Assert.AreEqual(3, uniqueIds.Count, "All generated units should have unique character classes.");
        }

        [Test]
        public void GenerateRandomMarionette_NoHealerInParty_GeneratesAtLeastOneHealer()
        {
            // Arrange
            _marionetteDb.marionettes.Clear();
            for (int i = 0; i < 5; i++)
            {
                var temp = ScriptableObject.CreateInstance<CharacterData>();
                temp.characterId = $"char_{i}";
                _marionetteDb.marionettes.Add(temp);
            }
            var healer = ScriptableObject.CreateInstance<CharacterData>();
            healer.characterId = "maid_marionette";
            _marionetteDb.marionettes.Add(healer);

            RunSessionManager.CurrentParty.Clear(); // No healer

            // Act
            var list = MarionetteGenerator.GenerateRandomMarionette(3, _marionetteDb, _traitDb, _config);

            // Assert
            bool hasHealer = false;
            foreach (var item in list)
            {
                if (item.character.characterId == "maid_marionette")
                {
                    hasHealer = true;
                    break;
                }
            }
            Assert.IsTrue(hasHealer, "A healer should be generated if the party doesn't have one.");
        }

        [Test]
        public void GenerateRandomMarionette_HealerInParty_AllowsNoHealerInBatch()
        {
            // Arrange
            _marionetteDb.marionettes.Clear();
            for (int i = 0; i < 10; i++) // Plenty of non-healers
            {
                var temp = ScriptableObject.CreateInstance<CharacterData>();
                temp.characterId = $"char_{i}";
                _marionetteDb.marionettes.Add(temp);
            }
            var healer = ScriptableObject.CreateInstance<CharacterData>();
            healer.characterId = "maid_marionette";
            _marionetteDb.marionettes.Add(healer);

            var existingHealerData = ScriptableObject.CreateInstance<CharacterData>();
            existingHealerData.characterId = "alchemist_marionette";
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo { character = existingHealerData });

            // Act
            bool generatedWithoutHealer = false;
            for (int i = 0; i < 50; i++)
            {
                var list = MarionetteGenerator.GenerateRandomMarionette(3, _marionetteDb, _traitDb, _config);
                bool batchHasHealer = false;
                foreach (var item in list)
                {
                    if (item.character.characterId == "maid_marionette")
                    {
                        batchHasHealer = true;
                        break;
                    }
                }
                if (!batchHasHealer)
                {
                    generatedWithoutHealer = true;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(generatedWithoutHealer, "Should be able to generate a batch without a healer if the party already has one.");
        }

        [Test]
        public void GenerateRandomMarionette_CeciliaHasHastyRepair_AllowsNoHealerInBatch()
        {
            // Arrange
            _marionetteDb.marionettes.Clear();
            for (int i = 0; i < 10; i++) // Plenty of non-healers
            {
                var temp = ScriptableObject.CreateInstance<CharacterData>();
                temp.characterId = $"char_{i}";
                _marionetteDb.marionettes.Add(temp);
            }
            var healer = ScriptableObject.CreateInstance<CharacterData>();
            healer.characterId = "maid_marionette";
            _marionetteDb.marionettes.Add(healer);

            var ceciData = ScriptableObject.CreateInstance<CharacterData>();
            ceciData.characterId = "ceci";
            
            var hastyRepairSkill = ScriptableObject.CreateInstance<SkillData>();
            hastyRepairSkill.skillId = "hasty_repair";

            var ceciInfo = new PartyMemberInfo { character = ceciData };
            ceciInfo.equippedSkills.Add(hastyRepairSkill);

            RunSessionManager.CurrentParty.Add(ceciInfo);

            // Act
            bool generatedWithoutHealer = false;
            for (int i = 0; i < 50; i++)
            {
                var list = MarionetteGenerator.GenerateRandomMarionette(3, _marionetteDb, _traitDb, _config);
                bool batchHasHealer = false;
                foreach (var item in list)
                {
                    if (item.character.characterId == "maid_marionette")
                    {
                        batchHasHealer = true;
                        break;
                    }
                }
                if (!batchHasHealer)
                {
                    generatedWithoutHealer = true;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(generatedWithoutHealer, "Should be able to generate a batch without a healer if Cecilia has Hasty Repair.");
        }
    }
}
