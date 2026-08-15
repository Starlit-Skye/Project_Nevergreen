using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class SaveManagerTests
    {
        private string testRunSavePath;
        private string testProfileSavePath;

        [SetUp]
        public void Setup()
        {
            // Redirect all save operations to temporary test files — never touch production saves
            testRunSavePath = Path.Combine(Application.temporaryCachePath, "test_run.dat");
            testProfileSavePath = Path.Combine(Application.temporaryCachePath, "test_profile.dat");
            SaveManager.SetSavePathsForTesting(testRunSavePath, testProfileSavePath);

            if (File.Exists(testRunSavePath)) File.Delete(testRunSavePath);
            if (File.Exists(testProfileSavePath)) File.Delete(testProfileSavePath);

            RunSessionManager.ClearAll();
        }

        [TearDown]
        public void Teardown()
        {
            if (!string.IsNullOrEmpty(testRunSavePath) && File.Exists(testRunSavePath))
                File.Delete(testRunSavePath);
            if (!string.IsNullOrEmpty(testProfileSavePath) && File.Exists(testProfileSavePath))
                File.Delete(testProfileSavePath);

            // Restore default save paths
            SaveManager.SetSavePathsForTesting(null, null);
            RunSessionManager.ClearAll();
        }

        [Test]
        public void SaveRun_CreatesEncryptedFile()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.RoomProgression = 5;

            SaveManager.SaveRun();

            Assert.IsTrue(File.Exists(testRunSavePath), "Run save file should be created.");
            
            // File should not be plain text JSON
            string content = File.ReadAllText(testRunSavePath);
            Assert.IsFalse(content.Contains("RoomProgression"), "File should be encrypted and not contain plain text keys.");
        }

        [Test]
        public void HasSavedRun_ReturnsTrue_IfActiveRunExists()
        {
            Assert.IsFalse(SaveManager.HasSavedRun());

            RunSessionManager.RoomProgression = 1;
            SaveManager.SaveRun();

            Assert.IsTrue(SaveManager.HasSavedRun(), "HasSavedRun should return true after saving an active run.");
        }

        [Test]
        public void ClearActiveRun_SetsHasActiveRunToFalse()
        {
            SaveManager.SaveRun();
            Assert.IsTrue(SaveManager.HasSavedRun());

            SaveManager.ClearActiveRun();
            
            Assert.IsTrue(File.Exists(testRunSavePath), "Save file should still exist after clearing.");
            Assert.IsFalse(SaveManager.HasSavedRun(), "HasSavedRun should return false after clearing the active run.");
        }

        [Test]
        public void LoadRun_DeserializesPrimitiveState_EvenWithoutGameDatabase()
        {
            RunSessionManager.RoomProgression = 42;
            SaveManager.SaveRun();

            // Clear state
            RunSessionManager.Clear();
            Assert.AreEqual(0, RunSessionManager.RoomProgression);

            // Attempt to load. Since GameDatabase might be null in this isolated test, 
            // LoadRun might return false and fail to populate complex objects, but let's check its behavior.
            // If GameDatabase.Instance is null, it aborts early. 
            // So we can mock GameDatabase.
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            GameDatabase.SetInstanceForTesting(db);

            bool loaded = SaveManager.LoadRun();
            
            Assert.IsTrue(loaded, "Should load successfully when GameDatabase is mocked.");
            Assert.AreEqual(42, RunSessionManager.RoomProgression, "Room progression should be restored.");

            GameDatabase.SetInstanceForTesting(null);
            Object.DestroyImmediate(db, true);
        }

        [Test]
        public void LoadRun_SetsShouldUseSavedFormation_WhenFormationIsLoaded()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            var formationDb = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            var formation = ScriptableObject.CreateInstance<EnemyFormationData>();
            formation.name = "Test_Formation";
            formation.formationId = "test_formation_01";

            // Add formation to database so it can be resolved
            formationDb.trivialFormations = new List<EnemyFormationData> { formation };
            
            // Set private db fields using reflection or set through GameDatabase
            typeof(GameDatabase).GetField("enemyFormationDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(db, formationDb);
            GameDatabase.SetInstanceForTesting(db);

            typeof(RunSessionManager).GetProperty("LastSelectedFormation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).SetValue(null, formation);
            SaveManager.SaveRun();

            RunSessionManager.Clear();
            Assert.IsNull(RunSessionManager.LastSelectedFormation);
            Assert.IsFalse(RunSessionManager.ShouldUseSavedFormation);

            bool loaded = SaveManager.LoadRun();

            Assert.IsTrue(loaded);
            Assert.AreEqual(formation, RunSessionManager.LastSelectedFormation);
            Assert.IsTrue(RunSessionManager.ShouldUseSavedFormation, "ShouldUseSavedFormation should be true after loading a run with a valid formation.");

            GameDatabase.SetInstanceForTesting(null);
            if (!UnityEditor.EditorUtility.IsPersistent(db)) Object.DestroyImmediate(db, true);
            if (!UnityEditor.EditorUtility.IsPersistent(formationDb)) Object.DestroyImmediate(formationDb, true);
            if (!UnityEditor.EditorUtility.IsPersistent(formation)) Object.DestroyImmediate(formation, true);
        }

        [Test]
        public void SaveRun_MidBattle_SavesPreCombatHP()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            GameDatabase.SetInstanceForTesting(db);

            // Create a party member with max HP initialized
            var member = new PartyMemberInfo();
            // Start of run (max HP)
            member.currentHP = null;
            member.preCombatHP = null;

            RunSessionManager.CurrentParty.Add(member);
            
            // Scene loads
            member.preCombatHP = member.currentHP; // null

            // During combat, character takes damage
            member.currentHP = 50;

            // Quit mid-battle
            SaveManager.SaveRun();

            // Clear and load
            RunSessionManager.Clear();
            bool loaded = SaveManager.LoadRun();

            Assert.IsTrue(loaded);
            Assert.AreEqual(1, RunSessionManager.CurrentParty.Count);
            
            // HP should be loaded from preCombatHP (which was null, mapping to -1, which loads as null)
            Assert.IsNull(RunSessionManager.CurrentParty[0].currentHP, "currentHP should have reset to pre-combat max HP (null).");
            Assert.IsNull(RunSessionManager.CurrentParty[0].preCombatHP, "preCombatHP should have loaded as null.");

            // Also test non-null scenario
            RunSessionManager.Clear();
            member = new PartyMemberInfo { currentHP = 60, preCombatHP = 60 };
            RunSessionManager.CurrentParty.Add(member);
            member.currentHP = 30; // mutation during combat
            SaveManager.SaveRun();

            RunSessionManager.Clear();
            loaded = SaveManager.LoadRun();
            Assert.IsTrue(loaded);
            Assert.AreEqual(60, RunSessionManager.CurrentParty[0].currentHP);
            Assert.AreEqual(60, RunSessionManager.CurrentParty[0].preCombatHP);

            GameDatabase.SetInstanceForTesting(null);
            Object.DestroyImmediate(db, true);
        }

        [Test]
        public void SaveRun_RoomCompleted_PersistsSelectionAndState()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            var roomDb = ScriptableObject.CreateInstance<RoomDatabase>();
            
            var roomA = ScriptableObject.CreateInstance<RoomData>();
            roomA.roomId = "room_a";
            var roomB = ScriptableObject.CreateInstance<RoomData>();
            roomB.roomId = "room_b";

            roomDb.availableRooms = new List<RoomData> { roomA, roomB };
            typeof(GameDatabase).GetField("roomDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(db, roomDb);
            GameDatabase.SetInstanceForTesting(db);

            // Simulate victory
            var choices = new List<RoomData> { roomA, roomB };
            RunSessionManager.CompleteRoom(choices); // Sets RoomCompleted = true, stores choices, and calls SaveRun()

            RunSessionManager.Clear();
            Assert.IsFalse(RunSessionManager.RoomCompleted);
            Assert.AreEqual(0, RunSessionManager.NextRoomChoices.Count);

            bool loaded = SaveManager.LoadRun();

            Assert.IsTrue(loaded);
            Assert.IsTrue(RunSessionManager.RoomCompleted);
            Assert.AreEqual(2, RunSessionManager.NextRoomChoices.Count);
            Assert.AreEqual(roomA, RunSessionManager.NextRoomChoices[0]);
            Assert.AreEqual(roomB, RunSessionManager.NextRoomChoices[1]);

            GameDatabase.SetInstanceForTesting(null);
            Object.DestroyImmediate(db, true);
            Object.DestroyImmediate(roomDb, true);
            Object.DestroyImmediate(roomB, true);
        }

        [Test]
        public void SaveRun_UncompletedRoom_FallbackToCurrentRoomData()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            var roomDb = ScriptableObject.CreateInstance<RoomDatabase>();
            
            var healRoom = ScriptableObject.CreateInstance<RoomData>();
            healRoom.roomId = "RD_HealRoom";
            roomDb.healRoom = healRoom;
            typeof(GameDatabase).GetField("roomDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(db, roomDb);
            GameDatabase.SetInstanceForTesting(db);

            // Simulate entering Heal Room (NextRoomData cleared on activation, but CurrentRoomData remains set)
            RunSessionManager.CurrentRoomData = healRoom;
            RunSessionManager.NextRoomData = null;
            RunSessionManager.RoomCompleted = false;

            SaveManager.SaveRun();

            RunSessionManager.Clear();
            Assert.IsNull(RunSessionManager.NextRoomData);

            bool loaded = SaveManager.LoadRun();

            Assert.IsTrue(loaded);
            Assert.AreEqual(healRoom, RunSessionManager.NextRoomData, "Should restore Heal Room as NextRoomData when loading run.");

            GameDatabase.SetInstanceForTesting(null);
            Object.DestroyImmediate(db, true);
            Object.DestroyImmediate(roomDb, true);
            Object.DestroyImmediate(healRoom, true);
        }

        [Test]
        public void LoadRun_RoomCompleted_ClearsShouldUseSavedFormation_ButRestoresLastSelectedFormation()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            var roomDb = ScriptableObject.CreateInstance<RoomDatabase>();
            var formationDb = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            
            var roomA = ScriptableObject.CreateInstance<RoomData>();
            roomA.roomId = "room_a";
            roomDb.availableRooms = new List<RoomData> { roomA };

            var formation = ScriptableObject.CreateInstance<EnemyFormationData>();
            formation.formationId = "test_formation";
            formationDb.trivialFormations = new List<EnemyFormationData> { formation };

            typeof(GameDatabase).GetField("roomDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(db, roomDb);
            typeof(GameDatabase).GetField("enemyFormationDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(db, formationDb);
            GameDatabase.SetInstanceForTesting(db);

            // Set up state
            RunSessionManager.LastSelectedFormation = formation;
            RunSessionManager.CompleteRoom(new List<RoomData> { roomA }); // Saves with roomCompleted = true

            RunSessionManager.Clear();
            Assert.IsFalse(RunSessionManager.ShouldUseSavedFormation);

            bool loaded = SaveManager.LoadRun();

            Assert.IsTrue(loaded);
            Assert.IsTrue(RunSessionManager.RoomCompleted);
            Assert.AreEqual(formation, RunSessionManager.LastSelectedFormation);
            Assert.IsFalse(RunSessionManager.ShouldUseSavedFormation, "ShouldUseSavedFormation must be false when roomCompleted is true.");

            GameDatabase.SetInstanceForTesting(null);
            Object.DestroyImmediate(db, true);
            Object.DestroyImmediate(roomDb, true);
            Object.DestroyImmediate(formationDb, true);
            Object.DestroyImmediate(roomA, true);
            Object.DestroyImmediate(formation, true);
        }

        [Test]
        public void SaveRun_PersistsPartyMemberLevel()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            GameDatabase.SetInstanceForTesting(db);

            var member = new PartyMemberInfo
            {
                currentLevel = 4
            };
            RunSessionManager.CurrentParty.Add(member);

            SaveManager.SaveRun();

            RunSessionManager.Clear();
            Assert.AreEqual(0, RunSessionManager.CurrentParty.Count);

            bool loaded = SaveManager.LoadRun();

            Assert.IsTrue(loaded);
            Assert.AreEqual(1, RunSessionManager.CurrentParty.Count);
            Assert.AreEqual(4, RunSessionManager.CurrentParty[0].currentLevel, "Loaded party member level should match the saved value.");

            GameDatabase.SetInstanceForTesting(null);
            Object.DestroyImmediate(db, true);
        }

        [Test]
        public void LoadRun_RestoresBossFormation_AndSetsShouldUseSavedFormation()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            var formationDb = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            var bossFormation = ScriptableObject.CreateInstance<EnemyFormationData>();
            bossFormation.name = "Test_Boss_Formation";
            bossFormation.formationId = "test_boss_01";

            // Add formation to bossFormations specifically
            formationDb.bossFormations = new List<EnemyFormationData> { bossFormation };
            
            typeof(GameDatabase).GetField("enemyFormationDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(db, formationDb);
            GameDatabase.SetInstanceForTesting(db);

            // Set up state as if it was saved mid-combat in boss room
            RunSessionManager.RoomCompleted = false;
            typeof(RunSessionManager).GetProperty("LastSelectedFormation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).SetValue(null, bossFormation);
            SaveManager.SaveRun();

            RunSessionManager.Clear();
            Assert.IsNull(RunSessionManager.LastSelectedFormation);
            Assert.IsFalse(RunSessionManager.ShouldUseSavedFormation);

            bool loaded = SaveManager.LoadRun();

            Assert.IsTrue(loaded);
            Assert.AreEqual(bossFormation, RunSessionManager.LastSelectedFormation, "Should restore boss formation.");
            Assert.IsTrue(RunSessionManager.ShouldUseSavedFormation, "Should set ShouldUseSavedFormation to true because RoomCompleted is false.");

            GameDatabase.SetInstanceForTesting(null);
            if (!UnityEditor.EditorUtility.IsPersistent(db)) Object.DestroyImmediate(db, true);
            if (!UnityEditor.EditorUtility.IsPersistent(formationDb)) Object.DestroyImmediate(formationDb, true);
            if (!UnityEditor.EditorUtility.IsPersistent(bossFormation)) Object.DestroyImmediate(bossFormation, true);
        }

        [Test]
        public void SaveProfile_CreatesEncryptedProfileFile()
        {
            // Trigger lazy load to set initial state if needed, or directly modify
            RunSessionManager.BossFormationChances["test_boss"] = 0.8f;
            
            SaveManager.SaveProfile();

            Assert.IsTrue(File.Exists(testProfileSavePath), "Profile save file should be created.");
            
            string content = File.ReadAllText(testProfileSavePath);
            Assert.IsFalse(content.Contains("test_boss"), "Profile file should be encrypted.");
        }

        [Test]
        public void ClearActiveRun_DoesNotDeleteOrAlterProfileFile()
        {
            // Setup profile
            RunSessionManager.BossFormationChances["test_boss"] = 0.8f;
            SaveManager.SaveProfile();
            long profileSize = new FileInfo(testProfileSavePath).Length;

            // Setup run
            SaveManager.SaveRun();
            Assert.IsTrue(SaveManager.HasSavedRun());

            // Clear run
            SaveManager.ClearActiveRun();

            Assert.IsFalse(SaveManager.HasSavedRun());
            Assert.IsTrue(File.Exists(testProfileSavePath), "Profile save should still exist.");
            Assert.AreEqual(profileSize, new FileInfo(testProfileSavePath).Length, "Profile save should not be modified by ClearActiveRun.");
        }

        [Test]
        public void LoadProfile_RestoresBossChances_WithoutActiveRun()
        {
            // Setup profile file
            RunSessionManager.BossFormationChances["test_boss_01"] = 0.4f;
            RunSessionManager.BossFormationChances["test_boss_02"] = 0.6f;
            SaveManager.SaveProfile();

            // Clear in-memory state
            RunSessionManager.ClearAll();
            Assert.IsFalse(SaveManager.HasSavedRun());

            // Trigger lazy load
            float chance = RunSessionManager.BossFormationChances["test_boss_01"];

            Assert.AreEqual(0.4f, chance);
            Assert.AreEqual(0.6f, RunSessionManager.BossFormationChances["test_boss_02"]);
        }
    }
}
