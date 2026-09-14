using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Tests.Data
{
    public class TreasureRoomEffectStrategyTests
    {
        private GameDatabase _gameDatabase;
        private TrinketDatabase _trinketDatabase;
        private TreasureRoomEffectStrategy _strategy;
        private TrinketData _common1;
        private TrinketData _uncommon1;

        [SetUp]
        public void Setup()
        {
            _common1 = ScriptableObject.CreateInstance<TrinketData>();
            _common1.trinketId = "C1";

            _uncommon1 = ScriptableObject.CreateInstance<TrinketData>();
            _uncommon1.trinketId = "U1";

            _trinketDatabase = ScriptableObject.CreateInstance<TrinketDatabase>();

            // Use Reflection to populate the private serialized fields since they don't have public setters
            var commonField = typeof(TrinketDatabase).GetField("commonTrinkets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            commonField.SetValue(_trinketDatabase, new List<TrinketData> { _common1 });

            var uncommonField = typeof(TrinketDatabase).GetField("uncommonTrinkets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            uncommonField.SetValue(_trinketDatabase, new List<TrinketData> { _uncommon1 });

            _gameDatabase = GameDatabase.CreateForTesting(trinkets: _trinketDatabase);
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            _strategy = new TreasureRoomEffectStrategy();
        }

        [TearDown]
        public void Teardown()
        {
            GameDatabase.SetInstanceForTesting(null);
            Object.DestroyImmediate(_gameDatabase);
            Object.DestroyImmediate(_trinketDatabase);
            Object.DestroyImmediate(_common1);
            Object.DestroyImmediate(_uncommon1);
        }

        private void SetWeights(float common, float uncommon)
        {
            var commonField = typeof(TreasureRoomEffectStrategy).GetField("commonTrinketWeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            commonField.SetValue(_strategy, common);

            var uncommonField = typeof(TreasureRoomEffectStrategy).GetField("uncommonTrinketWeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            uncommonField.SetValue(_strategy, uncommon);
        }

        [Test]
        public void RollTrinketReward_WhenCommonWeightIs100_ReturnsCommonTrinket()
        {
            SetWeights(100f, 0f);
            var result = _strategy.RollTrinketReward();
            Assert.IsNotNull(result);
            Assert.AreEqual("C1", result.trinketId);
        }

        [Test]
        public void RollTrinketReward_WhenUncommonWeightIs100_ReturnsUncommonTrinket()
        {
            SetWeights(0f, 100f);
            var result = _strategy.RollTrinketReward();
            Assert.IsNotNull(result);
            Assert.AreEqual("U1", result.trinketId);
        }

        [Test]
        public void RollTrinketReward_WhenTierListIsEmpty_ReturnsNull()
        {
            // Empty the common list
            var commonField = typeof(TrinketDatabase).GetField("commonTrinkets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            commonField.SetValue(_trinketDatabase, new List<TrinketData>());

            SetWeights(100f, 0f);
            var result = _strategy.RollTrinketReward();
            
            Assert.IsNull(result, "Should return null if the selected tier list is empty.");
        }

        [Test]
        public void RollTrinketReward_WhenTrinketDatabaseIsNull_ReturnsNull()
        {
            GameDatabase.SetInstanceForTesting(GameDatabase.CreateForTesting(trinkets: null));
            SetWeights(100f, 0f);
            
            var result = _strategy.RollTrinketReward();
            
            Assert.IsNull(result);
        }
        
        [Test]
        public void RollTrinketReward_WhenTotalWeightIsZero_ReturnsNull()
        {
            SetWeights(0f, 0f);
            var result = _strategy.RollTrinketReward();
            Assert.IsNull(result);
        }
    }
}
