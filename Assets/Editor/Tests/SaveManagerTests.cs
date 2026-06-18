using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class SaveManagerTests
    {
        private string testSavePath;

        [SetUp]
        public void Setup()
        {
            // We use reflection to get the SavePath so we can delete it if needed.
            var prop = typeof(SaveManager).GetProperty("SavePath", BindingFlags.NonPublic | BindingFlags.Static);
            if (prop != null)
            {
                testSavePath = (string)prop.GetValue(null);
            }
            
            if (!string.IsNullOrEmpty(testSavePath) && File.Exists(testSavePath))
            {
                File.Delete(testSavePath);
            }

            RunSessionManager.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            if (!string.IsNullOrEmpty(testSavePath) && File.Exists(testSavePath))
            {
                File.Delete(testSavePath);
            }
            RunSessionManager.Clear();
        }

        [Test]
        public void SaveRun_CreatesEncryptedFile()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.RoomProgression = 5;

            SaveManager.SaveRun();

            Assert.IsTrue(File.Exists(testSavePath), "Save file should be created.");
            
            // File should not be plain text JSON
            string content = File.ReadAllText(testSavePath);
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
            
            Assert.IsTrue(File.Exists(testSavePath), "Save file should still exist after clearing (meta progression).");
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
    }
}
