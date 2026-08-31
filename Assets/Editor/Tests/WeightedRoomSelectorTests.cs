using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class WeightedRoomSelectorTests
    {
        private System.Random _rng;

        [SetUp]
        public void SetUp()
        {
            _rng = new System.Random(12345); // Fixed seed for reproducibility
        }

        [Test]
        public void SelectRooms_UniformWeights_ReturnsRequestedCount()
        {
            var pool = new List<RoomPoolEntry>
            {
                CreateRoomWithFixedWeight("R1", 1f),
                CreateRoomWithFixedWeight("R2", 1f),
                CreateRoomWithFixedWeight("R3", 1f),
                CreateRoomWithFixedWeight("R4", 1f)
            };

            var results = WeightedRoomSelector.SelectRooms(pool, 2, _rng);

            Assert.AreEqual(2, results.Count);
            
            CleanupRooms(pool);
        }

        [Test]
        public void SelectRooms_ZeroWeight_ExcludesRoom()
        {
            var pool = new List<RoomPoolEntry>
            {
                CreateRoomWithFixedWeight("R1", 1f),
                CreateRoomWithFixedWeight("R2", 0f), // Should never be picked
                CreateRoomWithFixedWeight("R3", 1f)
            };

            for (int i = 0; i < 10; i++)
            {
                var results = WeightedRoomSelector.SelectRooms(pool, 2, _rng);
                Assert.IsFalse(results.Exists(r => r.roomId == "R2"));
            }
            
            CleanupRooms(pool);
        }

        [Test]
        public void SelectRooms_NullRule_DefaultsToWeightOne()
        {
            var roomWithNoRule = ScriptableObject.CreateInstance<RoomData>();
            roomWithNoRule.roomId = "R_Null";
            
            var entryNoRule = new RoomPoolEntry { room = roomWithNoRule, selectionRule = null };
            var entryWithWeight1 = CreateRoomWithFixedWeight("R_Weight1", 1f);

            var pool = new List<RoomPoolEntry> { entryNoRule, entryWithWeight1 };

            int nullPicked = 0;
            int weight1Picked = 0;
            
            // Over many iterations, they should be picked roughly equally
            for (int i = 0; i < 1000; i++)
            {
                var results = WeightedRoomSelector.SelectRooms(new List<RoomPoolEntry>(pool), 1, _rng);
                if (results[0].roomId == "R_Null") nullPicked++;
                else weight1Picked++;
            }

            Assert.IsTrue(nullPicked > 400 && nullPicked < 600, $"Expected ~500, got {nullPicked}");
            Assert.IsTrue(weight1Picked > 400 && weight1Picked < 600, $"Expected ~500, got {weight1Picked}");
            
            CleanupRooms(pool);
        }

        [Test]
        public void SelectRooms_HigherWeight_IsSelectedMoreOften()
        {
            var pool = new List<RoomPoolEntry>
            {
                CreateRoomWithFixedWeight("R_Heavy", 10f),
                CreateRoomWithFixedWeight("R_Light", 1f)
            };

            int heavyPicked = 0;
            int lightPicked = 0;

            for (int i = 0; i < 1000; i++)
            {
                var results = WeightedRoomSelector.SelectRooms(new List<RoomPoolEntry>(pool), 1, _rng);
                if (results[0].roomId == "R_Heavy") heavyPicked++;
                else lightPicked++;
            }

            // Expected ratio is ~10:1 (Heavy ~909, Light ~91)
            Assert.IsTrue(heavyPicked > 800, $"Expected heavy to be picked > 800 times, was {heavyPicked}");
            Assert.IsTrue(lightPicked < 200, $"Expected light to be picked < 200 times, was {lightPicked}");
            
            CleanupRooms(pool);
        }

        [Test]
        public void SelectRooms_CanReturnDuplicates()
        {
            var pool = new List<RoomPoolEntry>
            {
                CreateRoomWithFixedWeight("R1", 1f) // Only one room in pool
            };

            // Request 3 rooms from a pool of 1
            var results = WeightedRoomSelector.SelectRooms(pool, 3, _rng);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("R1", results[0].roomId);
            Assert.AreEqual("R1", results[1].roomId);
            Assert.AreEqual("R1", results[2].roomId);
            
            CleanupRooms(pool);
        }

        [Test]
        public void SelectRooms_PoolSmallerThanCount_ReturnsRequestedCount()
        {
            var pool = new List<RoomPoolEntry>
            {
                CreateRoomWithFixedWeight("R1", 1f),
                CreateRoomWithFixedWeight("R2", 1f)
            };

            // We can now request 5 choices from a pool of 2
            var results = WeightedRoomSelector.SelectRooms(pool, 5, _rng);

            Assert.AreEqual(5, results.Count);
            
            CleanupRooms(pool);
        }

        [Test]
        public void SelectRooms_EmptyPool_ReturnsEmpty()
        {
            var results = WeightedRoomSelector.SelectRooms(new List<RoomPoolEntry>(), 3, _rng);
            Assert.AreEqual(0, results.Count);
        }

        // --- Rule Specific Tests ---

        [Test]
        public void PartyCountRule_FewMarionettes_HigherWeight()
        {
            RunSessionManager.Clear();
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo()); // 1 member

            var rule = new PartyCountRule 
            { 
                baseWeight = 1f, 
                bonusPerMissingSlot = 0.5f
            };
            
            // 3 missing slots * 0.5 = 1.5 bonus + 1.0 base = 2.5 weight
            Assert.AreEqual(2.5f, rule.EvaluateWeight());
            
            RunSessionManager.Clear();
        }

        [Test]
        public void PartyCountRule_FullParty_BaseWeight()
        {
            RunSessionManager.Clear();
            for (int i = 0; i < 4; i++) RunSessionManager.CurrentParty.Add(new PartyMemberInfo());

            var rule = new PartyCountRule 
            { 
                baseWeight = 1f, 
                bonusPerMissingSlot = 0.5f
            };
            
            // 0 missing slots = base weight
            Assert.AreEqual(1.0f, rule.EvaluateWeight());
            
            RunSessionManager.Clear();
        }

        [Test]
        public void ProgressionScaledRule_IncreasesWithProgression()
        {
            RunSessionManager.Clear();
            RunSessionManager.RoomProgression = 5;

            var rule = new ProgressionScaledRule
            {
                baseWeight = 1f,
                weightPerRoom = 0.2f
            };
            
            // 5 rooms * 0.2 = 1.0 + 1.0 base = 2.0
            Assert.AreEqual(2.0f, rule.EvaluateWeight());
            
            RunSessionManager.Clear();
        }

        [Test]
        public void FixedWeightRule_ReturnsConfiguredValue()
        {
            var rule = new FixedWeightRule { weight = 3.14f };
            Assert.AreEqual(3.14f, rule.EvaluateWeight());
        }

        // --- Helpers ---

        private RoomPoolEntry CreateRoomWithFixedWeight(string id, float weight)
        {
            var room = ScriptableObject.CreateInstance<RoomData>();
            room.roomId = id;
            return new RoomPoolEntry { room = room, selectionRule = new FixedWeightRule { weight = weight } };
        }

        private void CleanupRooms(List<RoomPoolEntry> pool)
        {
            foreach (var e in pool)
            {
                if (e.room != null) Object.DestroyImmediate(e.room);
            }
        }
    }
}
