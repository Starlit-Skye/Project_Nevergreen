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
        private GlobalConfig _globalConfig;
        private GameDatabase _gameDatabase;

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

            _globalConfig = ScriptableObject.CreateInstance<GlobalConfig>();
            _globalConfig.maxPerfections = 3;
            _globalConfig.maxImperfections = 3;

            // Inject mock GameDatabase
            _gameDatabase = GameDatabase.CreateForTesting(
                globalCfg: _globalConfig,
                marionettes: _marionetteDb,
                traits: _traitDb
            );
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            // Ensure CurrentParty is clean
            RunSessionManager.CurrentParty = new List<PartyMemberInfo>();
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.CurrentParty = new List<PartyMemberInfo>();
            GameDatabase.SetInstanceForTesting(null);
            if (_gameDatabase != null) ScriptableObject.DestroyImmediate(_gameDatabase);
        }

        [Test]
        public void GenerateRandomMarionette_ValidInput_Generates4UniqueSkillsAnd1Perf1Imp()
        {
            // Act
            var list = MarionetteGenerator.GenerateRandomMarionette(1);
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
            var list = MarionetteGenerator.GenerateRandomMarionette(1);
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
            var list = MarionetteGenerator.GenerateRandomMarionette(1);
            var info = list[0];

            // Assert
            Assert.AreEqual(0, info.perfections.Count);
            Assert.AreEqual(0, info.imperfections.Count);
        }

        [Test]
        public void GenerateRandomMarionette_NullDatabase_ReturnsNull()
        {
            // Arrange: set GameDatabase with null MarionetteDatabase
            var emptyGameDb = GameDatabase.CreateForTesting();
            GameDatabase.SetInstanceForTesting(emptyGameDb);

            // Assert expecting an error log
            LogAssert.Expect(LogType.Error, "[MarionetteGenerator] MarionetteDatabase is null or empty!");

            // Act
            var info = MarionetteGenerator.GenerateRandomMarionette(1);

            // Assert
            Assert.IsNull(info);

            ScriptableObject.DestroyImmediate(emptyGameDb);
        }

        [Test]
        public void GenerateRandomMarionette_OppositeTraitChosen_PicksAlternative()
        {
            // Arrange
            _traitDb.perfections.Clear();
            _traitDb.imperfections.Clear();

            var perfA = ScriptableObject.CreateInstance<TraitData>();
            perfA.traitId = "perf_A";
            perfA.traitType = TraitType.Perfection;

            var imperfB = ScriptableObject.CreateInstance<TraitData>();
            imperfB.traitId = "imperf_B";
            imperfB.traitType = TraitType.Imperfection;
            imperfB.oppositeTrait = perfA;

            var imperfC = ScriptableObject.CreateInstance<TraitData>();
            imperfC.traitId = "imperf_C";
            imperfC.traitType = TraitType.Imperfection;

            _traitDb.perfections.Add(perfA);
            _traitDb.imperfections.Add(imperfB);
            _traitDb.imperfections.Add(imperfC);

            // Act
            // Since perfA is the only perfection, it will always be picked.
            // imperfB is its opposite, so if picked, it should be rejected and imperfC should be picked instead.
            var list = MarionetteGenerator.GenerateRandomMarionette(1);
            var info = list[0];

            // Assert
            Assert.AreEqual(1, info.perfections.Count);
            Assert.AreEqual("perf_A", info.perfections[0].traitId);
            Assert.AreEqual(1, info.imperfections.Count);
            Assert.AreEqual("imperf_C", info.imperfections[0].traitId, "Should have picked alternative imperf_C instead of opposite imperf_B.");
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
            var list = MarionetteGenerator.GenerateRandomMarionette(3);

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
            var list = MarionetteGenerator.GenerateRandomMarionette(3);

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
                var list = MarionetteGenerator.GenerateRandomMarionette(3);
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
                var list = MarionetteGenerator.GenerateRandomMarionette(3);
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

        [Test]
        public void GenerateRandomMarionette_ViolinistInParty_AllowsNoHealerInBatch()
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
            existingHealerData.characterId = "violinist_marionette";
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo { character = existingHealerData });

            // Act
            bool generatedWithoutHealer = false;
            for (int i = 0; i < 50; i++)
            {
                var list = MarionetteGenerator.GenerateRandomMarionette(3);
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
            Assert.IsTrue(generatedWithoutHealer, "Should be able to generate a batch without a healer if the party already has a Violinist.");
        }

        [Test]
        public void GenerateRandomMarionette_PrincessInParty_AllowsNoHealerInBatch()
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
            existingHealerData.characterId = "princess_marionette";
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo { character = existingHealerData });

            // Act
            bool generatedWithoutHealer = false;
            for (int i = 0; i < 50; i++)
            {
                var list = MarionetteGenerator.GenerateRandomMarionette(3);
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
            Assert.IsTrue(generatedWithoutHealer, "Should be able to generate a batch without a healer if the party already has a Princess.");
        }

        [Test]
        public void CalculateLowestTeamLevel_EmptyParty_Returns1()
        {
            // Arrange
            RunSessionManager.CurrentParty.Clear();

            // Act
            int lowestLevel = MarionetteGenerator.CalculateLowestTeamLevel(RunSessionManager.CurrentParty);

            // Assert
            Assert.AreEqual(1, lowestLevel);
        }

        [Test]
        public void CalculateLowestTeamLevel_VariousLevels_ReturnsLowest()
        {
            // Arrange
            var ceci = new PartyMemberInfo { character = ScriptableObject.CreateInstance<CharacterData>(), currentLevel = 5, currentHP = 10 };
            ceci.character.characterId = "ceci";
            
            var maid = new PartyMemberInfo { character = ScriptableObject.CreateInstance<CharacterData>(), currentLevel = 3, currentHP = 10 };
            maid.character.characterId = "maid_marionette";

            var commander = new PartyMemberInfo { character = ScriptableObject.CreateInstance<CharacterData>(), currentLevel = 7, currentHP = 10 };
            commander.character.characterId = "commander_marionette";

            RunSessionManager.CurrentParty.Add(ceci);
            RunSessionManager.CurrentParty.Add(maid);
            RunSessionManager.CurrentParty.Add(commander);

            // Act
            int lowestLevel = MarionetteGenerator.CalculateLowestTeamLevel(RunSessionManager.CurrentParty);

            // Assert
            Assert.AreEqual(3, lowestLevel, "Should return the lowest level among all party members (3).");
        }

        [Test]
        public void CalculateLowestTeamLevel_DestroyedMember_IsExcluded()
        {
            // Arrange
            var ceci = new PartyMemberInfo { character = ScriptableObject.CreateInstance<CharacterData>(), currentLevel = 5, currentHP = 10 };
            ceci.character.characterId = "ceci";
            
            // Maid is level 2, but destroyed (HP <= 0)
            var maid = new PartyMemberInfo { character = ScriptableObject.CreateInstance<CharacterData>(), currentLevel = 2, currentHP = 0 };
            maid.character.characterId = "maid_marionette";

            RunSessionManager.CurrentParty.Add(ceci);
            RunSessionManager.CurrentParty.Add(maid);

            // Act
            int lowestLevel = MarionetteGenerator.CalculateLowestTeamLevel(RunSessionManager.CurrentParty);

            // Assert
            Assert.AreEqual(5, lowestLevel, "Should ignore the destroyed member (level 2) and return the lowest level among living members (5).");
        }

        [Test]
        public void GenerateRandomMarionette_UsesLowestTeamLevel()
        {
            // Arrange
            var ceci = new PartyMemberInfo { character = ScriptableObject.CreateInstance<CharacterData>(), currentLevel = 4, currentHP = 10 };
            ceci.character.characterId = "ceci";
            RunSessionManager.CurrentParty.Add(ceci);
            
            // Party has lowest level 4

            // Act
            var list = MarionetteGenerator.GenerateRandomMarionette(1);

            // Assert
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(4, list[0].currentLevel, "Generated marionette should have its level set to the lowest team level.");
        }
    }
}
