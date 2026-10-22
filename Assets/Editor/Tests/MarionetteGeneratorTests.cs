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

        [Test]
        public void GenerateMarionetteFromTemplate_PopulatesUnlockedSkills()
        {
            var result = MarionetteGenerator.GenerateMarionetteFromTemplate(_template, _traitDb);
            
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.unlockedSkills);
            Assert.AreEqual(result.equippedSkills.Count, result.unlockedSkills.Count);
            
            for (int i = 0; i < result.equippedSkills.Count; i++)
            {
                Assert.AreEqual(result.equippedSkills[i], result.unlockedSkills[i]);
            }
        }

        // ==========================================
        // Level Up Trait Grant Tests
        // ==========================================

        private class FixedRng : System.Random
        {
            private readonly int[] _sequence;
            private int _index;
            public FixedRng(params int[] sequence) => _sequence = sequence;
            public override int Next(int maxValue)
            {
                if (_sequence != null && _sequence.Length > 0)
                {
                    int val = _sequence[_index % _sequence.Length];
                    _index++;
                    return val % maxValue;
                }
                return 0;
            }
        }

        [Test]
        public void PartyMemberInfo_IsTraitFull_RespectsCapacity()
        {
            var info = new PartyMemberInfo();
            Assert.IsFalse(info.IsTraitFull(TraitType.Perfection));
            Assert.IsFalse(info.IsTraitFull(TraitType.Imperfection));

            for (int i = 0; i < _globalConfig.maxPerfections; i++)
            {
                var perf = ScriptableObject.CreateInstance<TraitData>();
                perf.traitId = $"test_p_{i}";
                perf.traitType = TraitType.Perfection;
                info.perfections.Add(perf);
            }

            Assert.IsTrue(info.IsTraitFull(TraitType.Perfection));
            Assert.IsFalse(info.IsTraitFull(TraitType.Imperfection));
        }

        [Test]
        public void PartyMemberInfo_CanAddTrait_ChecksUniquenessAndOpposites()
        {
            var info = new PartyMemberInfo();
            var traitA = ScriptableObject.CreateInstance<TraitData>();
            traitA.traitId = "trait_a";
            traitA.traitType = TraitType.Perfection;

            var traitOpp = ScriptableObject.CreateInstance<TraitData>();
            traitOpp.traitId = "trait_opp";
            traitOpp.traitType = TraitType.Imperfection;
            traitA.oppositeTrait = traitOpp;
            traitOpp.oppositeTrait = traitA;

            Assert.IsTrue(info.CanAddTrait(traitA));
            info.perfections.Add(traitA);

            // Duplicate ID check
            var traitADup = ScriptableObject.CreateInstance<TraitData>();
            traitADup.traitId = "trait_a";
            traitADup.traitType = TraitType.Perfection;
            Assert.IsFalse(info.CanAddTrait(traitADup));

            // Opposite trait check (direct and reverse)
            Assert.IsFalse(info.CanAddTrait(traitOpp));
        }

        [Test]
        public void TryGrantLevelUpTrait_RollPerfection_GainsPerfection()
        {
            var member = new PartyMemberInfo { character = _template };
            var rng = new FixedRng(0); // 0 % 2 == 0 -> Perfection

            var granted = MarionetteGenerator.TryGrantLevelUpTrait(member, _traitDb, rng);

            Assert.IsNotNull(granted);
            Assert.AreEqual(TraitType.Perfection, granted.traitType);
            Assert.AreEqual(1, member.perfections.Count);
            Assert.AreEqual(0, member.imperfections.Count);
            Assert.Contains(granted, member.perfections);
        }

        [Test]
        public void TryGrantLevelUpTrait_RollImperfection_GainsImperfection()
        {
            var member = new PartyMemberInfo { character = _template };
            var rng = new FixedRng(1); // 1 % 2 != 0 -> Imperfection

            var granted = MarionetteGenerator.TryGrantLevelUpTrait(member, _traitDb, rng);

            Assert.IsNotNull(granted);
            Assert.AreEqual(TraitType.Imperfection, granted.traitType);
            Assert.AreEqual(0, member.perfections.Count);
            Assert.AreEqual(1, member.imperfections.Count);
            Assert.Contains(granted, member.imperfections);
        }

        [Test]
        public void TryGrantLevelUpTrait_PerfectionsFull_SwitchesToImperfection()
        {
            var member = new PartyMemberInfo { character = _template };
            // Fill perfections
            for (int i = 0; i < _globalConfig.maxPerfections; i++)
            {
                var perf = ScriptableObject.CreateInstance<TraitData>();
                perf.traitId = $"owned_perf_{i}";
                perf.traitType = TraitType.Perfection;
                member.perfections.Add(perf);
            }
            Assert.IsTrue(member.IsTraitFull(TraitType.Perfection));

            // Roll 0 (Perfection), but it is full -> should switch to Imperfection
            var rng = new FixedRng(0);
            var granted = MarionetteGenerator.TryGrantLevelUpTrait(member, _traitDb, rng);

            Assert.IsNotNull(granted);
            Assert.AreEqual(TraitType.Imperfection, granted.traitType);
            Assert.AreEqual(_globalConfig.maxPerfections, member.perfections.Count);
            Assert.AreEqual(1, member.imperfections.Count);
        }

        [Test]
        public void TryGrantLevelUpTrait_ImperfectionsFull_SwitchesToPerfection()
        {
            var member = new PartyMemberInfo { character = _template };
            // Fill imperfections
            for (int i = 0; i < _globalConfig.maxImperfections; i++)
            {
                var imp = ScriptableObject.CreateInstance<TraitData>();
                imp.traitId = $"owned_imp_{i}";
                imp.traitType = TraitType.Imperfection;
                member.imperfections.Add(imp);
            }
            Assert.IsTrue(member.IsTraitFull(TraitType.Imperfection));

            // Roll 1 (Imperfection), but it is full -> should switch to Perfection
            var rng = new FixedRng(1);
            var granted = MarionetteGenerator.TryGrantLevelUpTrait(member, _traitDb, rng);

            Assert.IsNotNull(granted);
            Assert.AreEqual(TraitType.Perfection, granted.traitType);
            Assert.AreEqual(1, member.perfections.Count);
            Assert.AreEqual(_globalConfig.maxImperfections, member.imperfections.Count);
        }

        [Test]
        public void TryGrantLevelUpTrait_BothFull_ReturnsNull()
        {
            var member = new PartyMemberInfo { character = _template };
            for (int i = 0; i < _globalConfig.maxPerfections; i++)
            {
                var perf = ScriptableObject.CreateInstance<TraitData>();
                perf.traitId = $"owned_perf_{i}";
                perf.traitType = TraitType.Perfection;
                member.perfections.Add(perf);
            }
            for (int i = 0; i < _globalConfig.maxImperfections; i++)
            {
                var imp = ScriptableObject.CreateInstance<TraitData>();
                imp.traitId = $"owned_imp_{i}";
                imp.traitType = TraitType.Imperfection;
                member.imperfections.Add(imp);
            }

            var rng = new FixedRng(0);
            var granted = MarionetteGenerator.TryGrantLevelUpTrait(member, _traitDb, rng);

            Assert.IsNull(granted);
            Assert.AreEqual(_globalConfig.maxPerfections, member.perfections.Count);
            Assert.AreEqual(_globalConfig.maxImperfections, member.imperfections.Count);
        }

        [Test]
        public void TryGrantLevelUpTrait_EligibleList_ExcludesOwnedAndOpposites()
        {
            var member = new PartyMemberInfo { character = _template };

            // Suppose traitDb has perf_0, perf_1, perf_2
            // Member already owns perf_0
            member.perfections.Add(_traitDb.perfections[0]);

            // perf_1 is opposite of an imperfection member owns
            var oppositeToPerf1 = ScriptableObject.CreateInstance<TraitData>();
            oppositeToPerf1.traitId = "imp_opp_to_perf1";
            oppositeToPerf1.traitType = TraitType.Imperfection;
            _traitDb.perfections[1].oppositeTrait = oppositeToPerf1;
            oppositeToPerf1.oppositeTrait = _traitDb.perfections[1];
            member.imperfections.Add(oppositeToPerf1);

            // Roll 0 (Perfection). Only perf_2 is eligible!
            var rng = new FixedRng(0);
            var granted = MarionetteGenerator.TryGrantLevelUpTrait(member, _traitDb, rng);

            Assert.IsNotNull(granted);
            Assert.AreEqual(_traitDb.perfections[2].traitId, granted.traitId);
        }

        [Test]
        public void TryGrantLevelUpTrait_EligibleListEmpty_ReturnsNull()
        {
            var member = new PartyMemberInfo { character = _template };

            // Owns all perfections in DB, but maxPerfections is higher (not full)
            _globalConfig.maxPerfections = 10;
            foreach (var p in _traitDb.perfections)
            {
                member.perfections.Add(p);
            }

            // Roll 0 (Perfection). It is not full (count 3 < 10), but eligible list is empty -> returns null
            var rng = new FixedRng(0);
            var granted = MarionetteGenerator.TryGrantLevelUpTrait(member, _traitDb, rng);

            Assert.IsNull(granted);
        }

        [Test]
        public void TryGrantLevelUpTrait_NullInputs_ReturnsNull()
        {
            Assert.IsNull(MarionetteGenerator.TryGrantLevelUpTrait(null, _traitDb));
            Assert.IsNull(MarionetteGenerator.TryGrantLevelUpTrait(new PartyMemberInfo(), null));
        }

        [Test]
        public void TryGrantLevelUpTrait_Distribution_BothTypesAwarded()
        {
            var rng = new System.Random(42);
            int perfectionCount = 0;
            int imperfectionCount = 0;

            for (int i = 0; i < 200; i++)
            {
                var member = new PartyMemberInfo { character = _template };
                var granted = MarionetteGenerator.TryGrantLevelUpTrait(member, _traitDb, rng);
                Assert.IsNotNull(granted);
                if (granted.traitType == TraitType.Perfection) perfectionCount++;
                else imperfectionCount++;
            }

            // Over 200 trials with 50% probability, counts should be reasonably balanced (e.g. between 70 and 130)
            Assert.GreaterOrEqual(perfectionCount, 70, $"Perfection count {perfectionCount} unexpectedly low");
            Assert.GreaterOrEqual(imperfectionCount, 70, $"Imperfection count {imperfectionCount} unexpectedly low");
        }

        // ==========================================
        // Higher Level Marionette Generation Tests
        // ==========================================

        [Test]
        public void GenerateMarionetteFromTemplate_Level1_Has2Traits()
        {
            // Empty party defaults to level 1
            RunSessionManager.CurrentParty.Clear();

            var result = MarionetteGenerator.GenerateMarionetteFromTemplate(_template, _traitDb);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.currentLevel);
            Assert.AreEqual(1, result.perfections.Count);
            Assert.AreEqual(1, result.imperfections.Count);
            Assert.AreEqual(2, result.perfections.Count + result.imperfections.Count);
        }

        [Test]
        public void GenerateMarionetteFromTemplate_Level2_Has3Traits()
        {
            // Team at level 2
            RunSessionManager.CurrentParty.Clear();
            var existingMember = new PartyMemberInfo { character = _template, currentLevel = 2 };
            RunSessionManager.CurrentParty.Add(existingMember);

            var result = MarionetteGenerator.GenerateMarionetteFromTemplate(_template, _traitDb);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.currentLevel);
            Assert.AreEqual(3, result.perfections.Count + result.imperfections.Count, "Level 2 should have 2 baseline traits + 1 level up trait = 3 traits.");
        }

        [Test]
        public void GenerateMarionetteFromTemplate_Level3_Has4Traits()
        {
            // Team at level 3 (user's explicit specification: 4 traits total)
            RunSessionManager.CurrentParty.Clear();
            var existingMember = new PartyMemberInfo { character = _template, currentLevel = 3 };
            RunSessionManager.CurrentParty.Add(existingMember);

            var result = MarionetteGenerator.GenerateMarionetteFromTemplate(_template, _traitDb);

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.currentLevel);
            Assert.AreEqual(4, result.perfections.Count + result.imperfections.Count, "Level 3 should have 2 baseline traits + 2 level up traits = 4 traits.");
        }

        [Test]
        public void GenerateMarionetteFromTemplate_Level5_Has6Traits_3Perf3Imp()
        {
            // Team at level 5
            RunSessionManager.CurrentParty.Clear();
            var existingMember = new PartyMemberInfo { character = _template, currentLevel = 5 };
            RunSessionManager.CurrentParty.Add(existingMember);

            var result = MarionetteGenerator.GenerateMarionetteFromTemplate(_template, _traitDb);

            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.currentLevel);
            Assert.AreEqual(3, result.perfections.Count, "Perfections should reach max capacity 3.");
            Assert.AreEqual(3, result.imperfections.Count, "Imperfections should reach max capacity 3.");
            Assert.AreEqual(6, result.perfections.Count + result.imperfections.Count);
        }

        [Test]
        public void GenerateMarionetteFromTemplate_Level10_CappedAtMax_NoOverflow()
        {
            // Team at level 10
            RunSessionManager.CurrentParty.Clear();
            var existingMember = new PartyMemberInfo { character = _template, currentLevel = 10 };
            RunSessionManager.CurrentParty.Add(existingMember);

            var result = MarionetteGenerator.GenerateMarionetteFromTemplate(_template, _traitDb);

            Assert.IsNotNull(result);
            Assert.AreEqual(10, result.currentLevel);
            Assert.AreEqual(3, result.perfections.Count, "Should not exceed max perfections.");
            Assert.AreEqual(3, result.imperfections.Count, "Should not exceed max imperfections.");
            Assert.AreEqual(6, result.perfections.Count + result.imperfections.Count);
        }

        [Test]
        public void GenerateMarionetteFromTemplate_HighLevel_NoDuplicateOrOppositeTraits()
        {
            // Add opposite trait relationships to traitDb
            _traitDb.perfections[0].oppositeTrait = _traitDb.imperfections[0];
            _traitDb.imperfections[0].oppositeTrait = _traitDb.perfections[0];

            RunSessionManager.CurrentParty.Clear();
            var existingMember = new PartyMemberInfo { character = _template, currentLevel = 4 };
            RunSessionManager.CurrentParty.Add(existingMember);

            var result = MarionetteGenerator.GenerateMarionetteFromTemplate(_template, _traitDb);

            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.currentLevel);

            // Check no duplicates in perfections
            var perfIds = new HashSet<string>();
            foreach (var p in result.perfections)
            {
                Assert.IsTrue(perfIds.Add(p.traitId), $"Duplicate perfection found: {p.traitId}");
            }

            // Check no duplicates in imperfections
            var impIds = new HashSet<string>();
            foreach (var imp in result.imperfections)
            {
                Assert.IsTrue(impIds.Add(imp.traitId), $"Duplicate imperfection found: {imp.traitId}");
            }

            // Check opposite traits are never both equipped
            foreach (var p in result.perfections)
            {
                if (p.oppositeTrait != null)
                {
                    Assert.IsFalse(result.imperfections.Contains(p.oppositeTrait), $"Opposite trait conflict: {p.traitId} and {p.oppositeTrait.traitId}");
                }
            }
        }

        [Test]
        public void GenerateMarionetteFromTemplate_SmallTraitPool_StopsGracefully()
        {
            // TraitDb with only 1 perfection and 1 imperfection
            var smallDb = ScriptableObject.CreateInstance<TraitDatabase>();
            smallDb.perfections = new List<TraitData> { _traitDb.perfections[0] };
            smallDb.imperfections = new List<TraitData> { _traitDb.imperfections[0] };

            RunSessionManager.CurrentParty.Clear();
            var existingMember = new PartyMemberInfo { character = _template, currentLevel = 5 };
            RunSessionManager.CurrentParty.Add(existingMember);

            // Level 5 requires 4 additional traits, but smallDb has no extra traits
            var result = MarionetteGenerator.GenerateMarionetteFromTemplate(_template, smallDb);

            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.currentLevel);
            Assert.AreEqual(1, result.perfections.Count);
            Assert.AreEqual(1, result.imperfections.Count);
            Assert.AreEqual(2, result.perfections.Count + result.imperfections.Count, "Should stop gracefully at 2 traits when trait pool is exhausted.");
        }

        [Test]
        public void GenerateMarionetteFromTemplate_NullTraitDb_NoTraits()
        {
            RunSessionManager.CurrentParty.Clear();
            var existingMember = new PartyMemberInfo { character = _template, currentLevel = 4 };
            RunSessionManager.CurrentParty.Add(existingMember);

            var result = MarionetteGenerator.GenerateMarionetteFromTemplate(_template, null);

            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.currentLevel);
            Assert.AreEqual(0, result.perfections.Count);
            Assert.AreEqual(0, result.imperfections.Count);
        }
    }
}
