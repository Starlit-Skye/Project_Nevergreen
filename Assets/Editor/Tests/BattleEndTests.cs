using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;
using System.Reflection;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class BattleEndTests
    {
        private GameObject _battleSystemGO;
        private BattleSystem _battleSystem;
        private List<CombatCharacter> _playerTeam;
        private List<CombatCharacter> _enemyTeam;

        [SetUp]
        public void SetUp()
        {
            CombatTestHelper.InitializeTestDatabase();
            
            _battleSystemGO = new GameObject("BattleSystem");
            _battleSystem = _battleSystemGO.AddComponent<BattleSystem>();
            _playerTeam = new List<CombatCharacter>();
            _enemyTeam = new List<CombatCharacter>();

            // Setup private fields via reflection to avoid running the full BattleLoop
            typeof(BattleSystem).GetField("_playerTeam", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_battleSystem, _playerTeam);
            typeof(BattleSystem).GetField("_enemyTeam", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_battleSystem, _enemyTeam);
            typeof(BattleSystem).GetField("_initialPlayerTeam", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_battleSystem, _playerTeam);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var c in _playerTeam) if (c != null) Object.DestroyImmediate(c.gameObject);
            foreach (var c in _enemyTeam) if (c != null) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(_battleSystemGO);
            
            CombatTestHelper.CleanupTestDatabase();
        }

        [Test]
        public void CheckBattleEnd_CeciliaDefeated_TriggersDefeat()
        {
            // Arrange
            var cecilia = CombatTestHelper.CreateCombatCharacter("ceci", Team.Player, 1);
            var ally = CombatTestHelper.CreateCombatCharacter("knight", Team.Player, 2);
            var enemy = CombatTestHelper.CreateCombatCharacter("golem", Team.Enemy, 1);

            _playerTeam.Add(cecilia);
            _playerTeam.Add(ally);
            _enemyTeam.Add(enemy);

            // Kill Cecilia
            cecilia.TakeDamage(cecilia.currentHP + 1);
            Assert.IsFalse(cecilia.IsAlive);
            Assert.IsTrue(ally.IsAlive);

            // Act
            MethodInfo checkBattleEnd = typeof(BattleSystem).GetMethod("CheckBattleEnd", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isOver = (bool)checkBattleEnd.Invoke(_battleSystem, null);

            // Assert
            Assert.IsTrue(isOver);
            Assert.AreEqual(BattleState.BattleEnd, _battleSystem.CurrentState);
        }

        [Test]
        public void CheckBattleEnd_CeciliaIsPile_TriggersDefeat()
        {
            // Arrange
            var cecilia = CombatTestHelper.CreateCombatCharacter("ceci", Team.Player, 1);
            _playerTeam.Add(cecilia);

            // Set to Pile state
            cecilia.state = LifeState.Pile;

            // Act
            MethodInfo checkBattleEnd = typeof(BattleSystem).GetMethod("CheckBattleEnd", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isOver = (bool)checkBattleEnd.Invoke(_battleSystem, null);

            // Assert
            Assert.IsTrue(isOver, "Battle should end if Cecilia is a Pile.");
            Assert.AreEqual(BattleState.BattleEnd, _battleSystem.CurrentState);
        }

        [Test]
        public void CheckBattleEnd_AllPlayersDefeated_TriggersDefeat()
        {
            // Arrange
            var ally1 = CombatTestHelper.CreateCombatCharacter("knight", Team.Player, 1);
            var ally2 = CombatTestHelper.CreateCombatCharacter("maid", Team.Player, 2);
            var enemy = CombatTestHelper.CreateCombatCharacter("golem", Team.Enemy, 1);

            _playerTeam.Add(ally1);
            _playerTeam.Add(ally2);
            _enemyTeam.Add(enemy);

            // Kill all players
            ally1.TakeDamage(ally1.currentHP + 1);
            ally2.TakeDamage(ally2.currentHP + 1);

            // Act
            MethodInfo checkBattleEnd = typeof(BattleSystem).GetMethod("CheckBattleEnd", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isOver = (bool)checkBattleEnd.Invoke(_battleSystem, null);

            // Assert
            Assert.IsTrue(isOver);
            Assert.AreEqual(BattleState.BattleEnd, _battleSystem.CurrentState);
        }

        [Test]
        public void CheckBattleEnd_AllEnemiesDefeated_TriggersVictory()
        {
            // Arrange
            var cecilia = CombatTestHelper.CreateCombatCharacter("ceci", Team.Player, 1);
            var enemy = CombatTestHelper.CreateCombatCharacter("golem", Team.Enemy, 1);

            _playerTeam.Add(cecilia);
            _enemyTeam.Add(enemy);

            // Kill all enemies
            enemy.TakeDamage(enemy.currentHP + 1);

            // Act
            MethodInfo checkBattleEnd = typeof(BattleSystem).GetMethod("CheckBattleEnd", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isOver = (bool)checkBattleEnd.Invoke(_battleSystem, null);

            // Assert
            Assert.IsTrue(isOver);
            Assert.AreEqual(BattleState.BattleEnd, _battleSystem.CurrentState);
            // In a real scenario, we'd check the outcome event to verify it's Victory.
        }

        [Test]
        public void CheckBattleEnd_BattleContinues_IfCeciliaAliveAndEnemiesAlive()
        {
            // Arrange
            var cecilia = CombatTestHelper.CreateCombatCharacter("ceci", Team.Player, 1);
            var ally = CombatTestHelper.CreateCombatCharacter("knight", Team.Player, 2);
            var enemy = CombatTestHelper.CreateCombatCharacter("golem", Team.Enemy, 1);

            _playerTeam.Add(cecilia);
            _playerTeam.Add(ally);
            _enemyTeam.Add(enemy);

            // Kill the ally, but Cecilia and Enemy are alive
            ally.TakeDamage(ally.currentHP + 1);

            // Act
            MethodInfo checkBattleEnd = typeof(BattleSystem).GetMethod("CheckBattleEnd", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isOver = (bool)checkBattleEnd.Invoke(_battleSystem, null);

            // Assert
            Assert.IsFalse(isOver);
            Assert.AreNotEqual(BattleState.BattleEnd, _battleSystem.CurrentState);
        }

        [Test]
        public void HandleCharacterDefeated_TriggersBattleEnd()
        {
            // Arrange
            var cecilia = CombatTestHelper.CreateCombatCharacter("ceci", Team.Player, 1);
            cecilia.TakeDamage(cecilia.currentHP + 1); // Mark as dead
            _playerTeam.Add(cecilia);

            // Act
            MethodInfo handleDefeated = typeof(BattleSystem).GetMethod("HandleCharacterDefeated", BindingFlags.NonPublic | BindingFlags.Instance);
            handleDefeated.Invoke(_battleSystem, new object[] { cecilia, false });

            // Assert
            Assert.AreEqual(BattleState.BattleEnd, _battleSystem.CurrentState);
        }

        [Test]
        public void ProcessTurn_ExitsEarly_IfBattleAlreadyOver()
        {
            // Arrange
            // Force BattleEnd state
            typeof(BattleSystem).GetProperty("CurrentState", BindingFlags.Public | BindingFlags.Instance)
                .SetValue(_battleSystem, BattleState.BattleEnd);
            
            // Act
            var enumerator = (System.Collections.IEnumerator)typeof(BattleSystem)
                .GetMethod("ProcessTurn", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_battleSystem, null);
            
            bool hasNext = enumerator.MoveNext();

            // Assert
            Assert.IsFalse(hasNext, "ProcessTurn should yield break immediately if battle is over");
        }

        [Test]
        public void BattleSystem_SubscribesToDeath_AndEndsBattle()
        {
            // Arrange
            var config = CombatTestHelper.CreateDefaultConfig();

            var cecilia = CombatTestHelper.CreateCombatCharacter("ceci", Team.Player, 1);
            var golem = CombatTestHelper.CreateCombatCharacter("golem", Team.Enemy, 1);

            var players = new List<CombatCharacter> { cecilia };
            var enemies = new List<CombatCharacter> { golem };

            // Act
            // StartBattle handles the event subscription to OnDefeated
            _battleSystem.StartBattle(players, enemies);
            
            // Kill cecilia
            cecilia.TakeDamage(cecilia.currentHP + 1);

            // Assert
            Assert.AreEqual(BattleState.BattleEnd, _battleSystem.CurrentState);
        }
    }
}
