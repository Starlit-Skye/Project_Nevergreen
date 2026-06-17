using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TurnOrderTests
    {
        private GameObject _battleSystemGO;
        private BattleSystem _battleSystem;
        private List<CombatCharacter> _playerTeam;
        private List<CombatCharacter> _enemyTeam;

        [SetUp]
        public void SetUp()
        {
            _battleSystemGO = new GameObject("BattleSystem");
            _battleSystem = _battleSystemGO.AddComponent<BattleSystem>();
            _playerTeam = new List<CombatCharacter>();
            _enemyTeam = new List<CombatCharacter>();

            var db = GameDatabase.CreateForTesting(combatCfg: CombatTestHelper.CreateDefaultConfig());
            GameDatabase.SetInstanceForTesting(db);

            // Setup private fields via reflection to avoid running the full BattleLoop
            typeof(BattleSystem).GetField("_playerTeam", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_battleSystem, _playerTeam);
            typeof(BattleSystem).GetField("_enemyTeam", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_battleSystem, _enemyTeam);
        }

        [TearDown]
        public void TearDown()
        {
            var db = GameDatabase.Instance;
            GameDatabase.SetInstanceForTesting(null);
            if (db != null)
            {
                if (db.CombatConfig != null) Object.DestroyImmediate(db.CombatConfig);
                Object.DestroyImmediate(db);
            }
            foreach (var c in _playerTeam) if (c != null) Object.DestroyImmediate(c.gameObject);
            foreach (var c in _enemyTeam) if (c != null) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(_battleSystemGO);
        }

        [Test]
        public void BuildTurnOrder_RollsSpeedBoostBetweenOneAndFour()
        {
            // Arrange
            var charA = CombatTestHelper.CreateCombatCharacter("charA", Team.Player, 1, speed: 5);
            _playerTeam.Add(charA);

            var rng = new System.Random(12345);
            typeof(BattleSystem).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_battleSystem, rng);

            // Act & Assert
            MethodInfo buildTurnOrder = typeof(BattleSystem).GetMethod("BuildTurnOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            var turnOrderField = typeof(BattleSystem).GetField("_turnOrder", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < 50; i++)
            {
                buildTurnOrder.Invoke(_battleSystem, null);
                var turnOrder = (List<TurnEntry>)turnOrderField.GetValue(_battleSystem);

                Assert.AreEqual(1, turnOrder.Count);
                int speed = turnOrder[0].speed;
                int boost = speed - 5;
                Assert.IsTrue(boost >= 1 && boost <= 4, $"Boost {boost} should be between 1 and 4 inclusive.");
            }
        }

        [Test]
        public void BuildTurnOrder_UsesCustomCombatConfigRollLimits()
        {
            // Arrange
            var charA = CombatTestHelper.CreateCombatCharacter("charA", Team.Player, 1, speed: 5);
            _playerTeam.Add(charA);

            var config = ScriptableObject.CreateInstance<CombatConfig>();
            config.speedRollMin = 5;
            config.speedRollMax = 10;
            
            var oldConfig = GameDatabase.Instance.CombatConfig;
            // Since combatConfig is private serialized in GameDatabase but GameDatabase.CreateForTesting sets it... wait, I'll just create a new DB.
            var newDb = GameDatabase.CreateForTesting(combatCfg: config);
            GameDatabase.SetInstanceForTesting(newDb);

            var rng = new System.Random(12345);
            typeof(BattleSystem).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_battleSystem, rng);

            // Act & Assert
            MethodInfo buildTurnOrder = typeof(BattleSystem).GetMethod("BuildTurnOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            var turnOrderField = typeof(BattleSystem).GetField("_turnOrder", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < 50; i++)
            {
                buildTurnOrder.Invoke(_battleSystem, null);
                var turnOrder = (List<TurnEntry>)turnOrderField.GetValue(_battleSystem);

                Assert.AreEqual(1, turnOrder.Count);
                int speed = turnOrder[0].speed;
                int boost = speed - 5;
                Assert.IsTrue(boost >= 5 && boost <= 10, $"Boost {boost} should be between 5 and 10 inclusive.");
            }

            // Cleanup
            Object.DestroyImmediate(newDb);
            ScriptableObject.DestroyImmediate(config);
        }

        [Test]
        public void BuildTurnOrder_MultipleActionsPerRound_RollsIndependentSpeeds()
        {
            // Arrange
            var charA = CombatTestHelper.CreateCombatCharacter("charA", Team.Player, 1, speed: 5);
            charA.characterData.actionsPerRound = 3;
            _playerTeam.Add(charA);

            // Mock returns 1, 4, 2 for the three actions
            var customRng = new MockRandom(new int[] { 1, 4, 2 });
            typeof(BattleSystem).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_battleSystem, customRng);

            // Act
            MethodInfo buildTurnOrder = typeof(BattleSystem).GetMethod("BuildTurnOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            buildTurnOrder.Invoke(_battleSystem, null);

            var turnOrderField = typeof(BattleSystem).GetField("_turnOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            var turnOrder = (List<TurnEntry>)turnOrderField.GetValue(_battleSystem);

            // Assert
            Assert.AreEqual(3, turnOrder.Count);
            
            // Expected speed values: 5+4 = 9, 5+2 = 7, 5+1 = 6 (sorted descending)
            Assert.AreEqual(9, turnOrder[0].speed);
            Assert.AreEqual(7, turnOrder[1].speed);
            Assert.AreEqual(6, turnOrder[2].speed);
        }

        [Test]
        public void BuildTurnOrder_SortsCorrectlyWithBoostedSpeeds()
        {
            // Arrange
            var charA = CombatTestHelper.CreateCombatCharacter("charA", Team.Player, 1, speed: 10);
            var charB = CombatTestHelper.CreateCombatCharacter("charB", Team.Enemy, 1, speed: 8);
            _playerTeam.Add(charA);
            _enemyTeam.Add(charB);

            // A is Player, gets boost 1 -> 11 speed
            // B is Enemy, gets boost 4 -> 12 speed
            var customRng = new MockRandom(new int[] { 1, 4 });
            typeof(BattleSystem).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_battleSystem, customRng);

            // Act
            MethodInfo buildTurnOrder = typeof(BattleSystem).GetMethod("BuildTurnOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            buildTurnOrder.Invoke(_battleSystem, null);

            var turnOrderField = typeof(BattleSystem).GetField("_turnOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            var turnOrder = (List<TurnEntry>)turnOrderField.GetValue(_battleSystem);

            // Assert
            Assert.AreEqual(2, turnOrder.Count);
            Assert.AreEqual(charB, turnOrder[0].character);
            Assert.AreEqual(charA, turnOrder[1].character);
            Assert.AreEqual(12, turnOrder[0].speed);
            Assert.AreEqual(11, turnOrder[1].speed);
        }

        private class MockRandom : System.Random
        {
            private readonly int[] _values;
            private int _index = 0;

            public MockRandom(int[] values)
            {
                _values = values;
            }

            public override int Next(int minValue, int maxValue)
            {
                if (_index < _values.Length)
                {
                    int val = _values[_index++];
                    if (val >= minValue && val < maxValue)
                        return val;
                }
                return base.Next(minValue, maxValue);
            }
        }
    }
}
