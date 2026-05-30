using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class EnemyFormationSelectionTests
    {
        private EnemyFormationDatabase database;
        private EnemyFormationData formationA;
        private EnemyFormationData formationB;
        private EnemyFormationData formationC;

        [SetUp]
        public void Setup()
        {
            RunSessionManager.Clear();

            formationA = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationA.name = "Formation_A";
            formationA.enemyPrefabs = new List<GameObject>();

            formationB = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationB.name = "Formation_B";
            formationB.enemyPrefabs = new List<GameObject>();

            formationC = ScriptableObject.CreateInstance<EnemyFormationData>();
            formationC.name = "Formation_C";
            formationC.enemyPrefabs = new List<GameObject>();

            database = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            database.formations = new List<EnemyFormationData> { formationA, formationB, formationC };
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.Clear();
            ScriptableObject.DestroyImmediate(formationA);
            ScriptableObject.DestroyImmediate(formationB);
            ScriptableObject.DestroyImmediate(formationC);
            ScriptableObject.DestroyImmediate(database);
        }

        [Test]
        public void GetNextRandomFormation_ReturnsNull_WhenNoDatabaseInitialized()
        {
            // No Initialize call
            var result = RunSessionManager.GetNextRandomFormation();
            Assert.IsNull(result);
        }

        [Test]
        public void GetNextRandomFormation_ReturnsFormation_WhenDatabaseHasOneEntry()
        {
            var singleDb = ScriptableObject.CreateInstance<EnemyFormationDatabase>();
            singleDb.formations = new List<EnemyFormationData> { formationA };
            RunSessionManager.Initialize(singleDb);

            var result = RunSessionManager.GetNextRandomFormation();
            Assert.AreEqual(formationA, result);

            // Calling again should still return the same (only option)
            var result2 = RunSessionManager.GetNextRandomFormation();
            Assert.AreEqual(formationA, result2);

            ScriptableObject.DestroyImmediate(singleDb);
        }

        [Test]
        public void GetNextRandomFormation_NeverReturnsConsecutiveDuplicates()
        {
            RunSessionManager.Initialize(database);

            EnemyFormationData previous = null;
            for (int i = 0; i < 100; i++)
            {
                var current = RunSessionManager.GetNextRandomFormation();
                Assert.IsNotNull(current, $"Formation should not be null on iteration {i}");

                if (previous != null)
                {
                    Assert.AreNotEqual(previous, current,
                        $"Consecutive duplicate detected on iteration {i}: {current.name}");
                }

                previous = current;
            }
        }

        [Test]
        public void GetNextRandomFormation_ReturnsFromDatabase()
        {
            RunSessionManager.Initialize(database);

            var result = RunSessionManager.GetNextRandomFormation();
            Assert.IsTrue(
                result == formationA || result == formationB || result == formationC,
                "Returned formation should be one of the database entries");
        }

        [Test]
        public void Initialize_ResetsLastSelectedFormation()
        {
            RunSessionManager.Initialize(database);
            var first = RunSessionManager.GetNextRandomFormation();
            Assert.IsNotNull(first);

            // Re-initialize should reset last selection
            RunSessionManager.Initialize(database);
            Assert.IsNull(RunSessionManager.LastSelectedFormation,
                "LastSelectedFormation should be null after re-initialization");
        }

        [Test]
        public void Clear_ResetsAllFormationState()
        {
            RunSessionManager.Initialize(database);
            RunSessionManager.GetNextRandomFormation();

            RunSessionManager.Clear();

            Assert.IsNull(RunSessionManager.ActiveFormationDatabase);
            Assert.IsNull(RunSessionManager.LastSelectedFormation);
            Assert.IsNull(RunSessionManager.GetNextRandomFormation(),
                "After Clear, GetNextRandomFormation should return null");
        }
    }
}
