using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class MainMenuSkillSelectionTests
    {
        private CombatConfig config;
        private CharacterData charDataCeci;
        private CharacterData charDataOther;
        private SkillData skill1;
        private SkillData skill2;
        private SkillData skill3;
        private SkillData skill4;
        private SkillData skillFallback;

        [SetUp]
        public void Setup()
        {
            config = CombatTestHelper.CreateDefaultConfig();

            // Create skills
            skill1 = ScriptableObject.CreateInstance<SkillData>();
            skill1.skillId = "skill_1";
            skill1.displayName = "Skill 1";

            skill2 = ScriptableObject.CreateInstance<SkillData>();
            skill2.skillId = "skill_2";
            skill2.displayName = "Skill 2";

            skill3 = ScriptableObject.CreateInstance<SkillData>();
            skill3.skillId = "skill_3";
            skill3.displayName = "Skill 3";

            skill4 = ScriptableObject.CreateInstance<SkillData>();
            skill4.skillId = "skill_4";
            skill4.displayName = "Skill 4";

            skillFallback = ScriptableObject.CreateInstance<SkillData>();
            skillFallback.skillId = "skill_fallback";
            skillFallback.displayName = "Skill Fallback";

            // Create character templates
            var stats = CombatTestHelper.CreateStatBlock();
            charDataCeci = CombatTestHelper.CreateCharacterData("ceci", "Cecilia", stats, CharacterTeamType.Player);
            charDataCeci.availableSkills = new List<SkillData> { skillFallback };
            charDataCeci.totalSkillPool = new List<SkillData> { skill1, skill2, skill3, skill4 };

            charDataOther = CombatTestHelper.CreateCharacterData("other", "Other Hero", stats, CharacterTeamType.Player);
            charDataOther.availableSkills = new List<SkillData> { skillFallback };

            RunSessionManager.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.Clear();
            ScriptableObject.DestroyImmediate(config);
            ScriptableObject.DestroyImmediate(charDataCeci);
            ScriptableObject.DestroyImmediate(charDataOther);
            ScriptableObject.DestroyImmediate(skill1);
            ScriptableObject.DestroyImmediate(skill2);
            ScriptableObject.DestroyImmediate(skill3);
            ScriptableObject.DestroyImmediate(skill4);
            ScriptableObject.DestroyImmediate(skillFallback);
        }

        [Test]
        public void RunSessionManager_Clear_ClearsRoster()
        {
            RunSessionManager.CurrentParty = new List<PartyMemberInfo>
            {
                new PartyMemberInfo { character = charDataCeci, equippedSkills = new List<SkillData>() }
            };

            Assert.AreEqual(1, RunSessionManager.CurrentParty.Count);
            RunSessionManager.Clear();
            Assert.AreEqual(0, RunSessionManager.CurrentParty.Count);
        }

        [Test]
        public void CombatCharacter_InitializeForCombat_UsesSessionSkills_IfMatching()
        {
            // Configure equipped session skills
            var equippedSkills = new List<SkillData> { skill1, skill2, skill3, skill4 };
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo
            {
                character = charDataCeci,
                equippedSkills = equippedSkills
            });

            // Create character
            var go = new GameObject("CeciHero");
            var cc = go.AddComponent<CombatCharacter>();
            cc.characterData = charDataCeci;
            cc.currentLevel = 1;
            cc.combatConfig = config;

            // Act
            cc.InitializeForCombat(Team.Player, 1);

            // Assert: should use session skills instead of fallback
            Assert.AreEqual(4, cc.equippedSkills.Count);
            Assert.AreEqual("skill_1", cc.equippedSkills[0].skillId);
            Assert.AreEqual("skill_2", cc.equippedSkills[1].skillId);
            Assert.AreEqual("skill_3", cc.equippedSkills[2].skillId);
            Assert.AreEqual("skill_4", cc.equippedSkills[3].skillId);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CombatCharacter_InitializeForCombat_FallsBackToDefaultSkills_IfNotInSession()
        {
            // Roster has other hero, but not Cecilia
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo
            {
                character = charDataOther,
                equippedSkills = new List<SkillData> { skill1 }
            });

            // Create Cecilia character
            var go = new GameObject("CeciHero");
            var cc = go.AddComponent<CombatCharacter>();
            cc.characterData = charDataCeci;
            cc.currentLevel = 1;
            cc.combatConfig = config;

            // Act
            cc.InitializeForCombat(Team.Player, 1);

            // Assert: fallback to default availableSkills
            Assert.AreEqual(1, cc.equippedSkills.Count);
            Assert.AreEqual("skill_fallback", cc.equippedSkills[0].skillId);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CombatSceneBootstrap_SpawnTeams_SpawnsOnlySessionCharacters_WhenSessionIsActive()
        {
            // Set up prefabs
            var ceciPrefab = new GameObject("CeciPrefab");
            var ceciCC = ceciPrefab.AddComponent<CombatCharacter>();
            ceciCC.characterData = charDataCeci;

            var otherPrefab = new GameObject("OtherPrefab");
            var otherCC = otherPrefab.AddComponent<CombatCharacter>();
            otherCC.characterData = charDataOther;

            // Set up Bootstrap
            var bootGo = new GameObject("Bootstrap");
            var bootstrap = bootGo.AddComponent<CombatSceneBootstrap>();
            bootstrap.playerTeamPrefabs = new List<GameObject> { ceciPrefab, otherPrefab };

            // Setup session: only Ceci
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo
            {
                character = charDataCeci,
                equippedSkills = new List<SkillData>()
            });

            // Act: SpawnTeams via reflection
            var spawnMethod = typeof(CombatSceneBootstrap).GetMethod("SpawnTeams", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            spawnMethod.Invoke(bootstrap, null);

            // Verify: only Cecilia is spawned, not "OtherPrefab"
            var spawnedField = typeof(CombatSceneBootstrap).GetField("_spawnedPlayerTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spawnedList = (List<CombatCharacter>)spawnedField.GetValue(bootstrap);

            Assert.AreEqual(1, spawnedList.Count, "Should spawn exactly one character from the session party");
            Assert.AreEqual(charDataCeci, spawnedList[0].characterData, "Spawned character should be Cecilia");

            // Clean up spawned GameObjects
            foreach (var cc in spawnedList)
            {
                if (cc != null) Object.DestroyImmediate(cc.gameObject);
            }
            Object.DestroyImmediate(ceciPrefab);
            Object.DestroyImmediate(otherPrefab);
            Object.DestroyImmediate(bootGo);
        }

        [Test]
        public void CombatSceneBootstrap_SpawnTeams_SpawnsAllPrefabs_WhenSessionIsEmpty()
        {
            // Set up prefabs
            var ceciPrefab = new GameObject("CeciPrefab");
            var ceciCC = ceciPrefab.AddComponent<CombatCharacter>();
            ceciCC.characterData = charDataCeci;

            var otherPrefab = new GameObject("OtherPrefab");
            var otherCC = otherPrefab.AddComponent<CombatCharacter>();
            otherCC.characterData = charDataOther;

            // Set up Bootstrap
            var bootGo = new GameObject("Bootstrap");
            var bootstrap = bootGo.AddComponent<CombatSceneBootstrap>();
            bootstrap.playerTeamPrefabs = new List<GameObject> { ceciPrefab, otherPrefab };

            // Session is empty

            // Act: SpawnTeams
            var spawnMethod = typeof(CombatSceneBootstrap).GetMethod("SpawnTeams", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            spawnMethod.Invoke(bootstrap, null);

            // Verify: both are spawned (fallback behaviour)
            var spawnedField = typeof(CombatSceneBootstrap).GetField("_spawnedPlayerTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spawnedList = (List<CombatCharacter>)spawnedField.GetValue(bootstrap);

            Assert.AreEqual(2, spawnedList.Count, "Should fallback to spawn all player prefabs in default config");
            Assert.AreEqual(charDataCeci, spawnedList[0].characterData);
            Assert.AreEqual(charDataOther, spawnedList[1].characterData);

            // Clean up spawned GameObjects
            foreach (var cc in spawnedList)
            {
                if (cc != null) Object.DestroyImmediate(cc.gameObject);
            }
            Object.DestroyImmediate(ceciPrefab);
            Object.DestroyImmediate(otherPrefab);
            Object.DestroyImmediate(bootGo);
        }

        [Test]
        public void CombatSceneBootstrap_SpawnTeams_SpawnsDirectCharacterPrefab_WhenConfiguredOnCharacterData()
        {
            // Set up direct prefab on CharacterData
            var ceciPrefab = new GameObject("CeciDirectPrefab");
            var ceciCC = ceciPrefab.AddComponent<CombatCharacter>();
            ceciCC.characterData = charDataCeci;
            charDataCeci.characterPrefab = ceciCC;

            // Set up Bootstrap (playerTeamPrefabs is empty)
            var bootGo = new GameObject("Bootstrap");
            var bootstrap = bootGo.AddComponent<CombatSceneBootstrap>();
            bootstrap.playerTeamPrefabs = new List<GameObject>();

            // Setup session: Cecilia
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo
            {
                character = charDataCeci,
                equippedSkills = new List<SkillData>()
            });

            // Act: SpawnTeams
            var spawnMethod = typeof(CombatSceneBootstrap).GetMethod("SpawnTeams", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            spawnMethod.Invoke(bootstrap, null);

            // Verify: Cecilia is spawned from her direct prefab reference
            var spawnedField = typeof(CombatSceneBootstrap).GetField("_spawnedPlayerTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spawnedList = (List<CombatCharacter>)spawnedField.GetValue(bootstrap);

            Assert.AreEqual(1, spawnedList.Count, "Should spawn exactly one character from CharacterData prefab");
            Assert.AreEqual(charDataCeci, spawnedList[0].characterData);
            Assert.IsTrue(spawnedList[0].name.Contains("CeciDirectPrefab"), "Should instantiate the direct prefab reference");

            // Clean up
            foreach (var cc in spawnedList)
            {
                if (cc != null) Object.DestroyImmediate(cc.gameObject);
            }
            Object.DestroyImmediate(ceciPrefab);
            Object.DestroyImmediate(bootGo);
            charDataCeci.characterPrefab = null;
        }
    }
}
