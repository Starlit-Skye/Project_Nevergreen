using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class RoomEffectTests
    {
        // --- Test double for RoomEffectStrategy ---
        [Serializable]
        private class TestRoomEffectStrategy : RoomEffectStrategy
        {
            public static int ExecutionCount;
            public override void ExecuteRoomEffect()
            {
                ExecutionCount++;
            }
        }

        [SetUp]
        public void SetUp()
        {
            RunSessionManager.Clear();
            TestRoomEffectStrategy.ExecutionCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            RunSessionManager.Clear();
        }

        // ============================================================
        // RoomData & Strategy Tests
        // ============================================================

        [Test]
        public void RoomData_ActivateEffect_InvokesStrategy()
        {
            var roomData = ScriptableObject.CreateInstance<RoomData>();
            roomData.roomName = "Test Room";
            roomData.strategy = new TestRoomEffectStrategy();

            roomData.ActivateEffect();

            Assert.AreEqual(1, TestRoomEffectStrategy.ExecutionCount);
            UnityEngine.Object.DestroyImmediate(roomData);
        }

        [Test]
        public void RoomData_ActivateEffect_NullStrategy_DoesNotThrow()
        {
            var roomData = ScriptableObject.CreateInstance<RoomData>();
            roomData.roomName = "Empty Room";
            roomData.strategy = null;

            Assert.DoesNotThrow(() => roomData.ActivateEffect());
            UnityEngine.Object.DestroyImmediate(roomData);
        }

        // ============================================================
        // RunSessionManager NextRoomData Tests
        // ============================================================

        [Test]
        public void RunSessionManager_NextRoomData_DefaultIsNull()
        {
            Assert.IsNull(RunSessionManager.NextRoomData);
        }

        [Test]
        public void RunSessionManager_NextRoomData_SetAndGet()
        {
            var roomData = ScriptableObject.CreateInstance<RoomData>();
            roomData.roomName = "Test";

            RunSessionManager.NextRoomData = roomData;
            Assert.AreSame(roomData, RunSessionManager.NextRoomData);

            UnityEngine.Object.DestroyImmediate(roomData);
        }

        [Test]
        public void RunSessionManager_Clear_ResetsNextRoomData()
        {
            var roomData = ScriptableObject.CreateInstance<RoomData>();
            RunSessionManager.NextRoomData = roomData;

            RunSessionManager.Clear();

            Assert.IsNull(RunSessionManager.NextRoomData);
            UnityEngine.Object.DestroyImmediate(roomData);
        }

        [Test]
        public void RunSessionManager_ActivateCurrentRoomEffect_InvokesStrategy()
        {
            var roomData = ScriptableObject.CreateInstance<RoomData>();
            roomData.strategy = new TestRoomEffectStrategy();
            RunSessionManager.NextRoomData = roomData;

            RunSessionManager.ActivateCurrentRoomEffect();

            Assert.AreEqual(1, TestRoomEffectStrategy.ExecutionCount);
            UnityEngine.Object.DestroyImmediate(roomData);
        }

        [Test]
        public void RunSessionManager_ActivateCurrentRoomEffect_NullNextRoom_DoesNotThrow()
        {
            RunSessionManager.NextRoomData = null;
            Assert.DoesNotThrow(() => RunSessionManager.ActivateCurrentRoomEffect());
        }

        // ============================================================
        // Victory Subscription Tests
        // ============================================================

        [Test]
        public void SubscribeToBattle_Victory_ActivatesOnCombatVictoryRoom()
        {
            // Arrange
            var battleGO = new GameObject("BattleSystem");
            var battleSystem = battleGO.AddComponent<BattleSystem>();

            var roomData = ScriptableObject.CreateInstance<RoomData>();
            roomData.roomName = "Victory Room";
            roomData.activationType = RoomActivationType.OnCombatVictory;
            roomData.strategy = new TestRoomEffectStrategy();

            RunSessionManager.NextRoomData = roomData;
            RunSessionManager.SubscribeToBattle(battleSystem);

            // Act - fire OnBattleEnded(Victory) via reflection
            var onBattleEnded = typeof(BattleSystem)
                .GetField("OnBattleEnded", BindingFlags.NonPublic | BindingFlags.Instance);
            var del = onBattleEnded.GetValue(battleSystem) as Action<BattleOutcome>;
            del?.Invoke(BattleOutcome.Victory);

            // Assert
            Assert.AreEqual(1, TestRoomEffectStrategy.ExecutionCount,
                "Strategy should have been executed on Victory.");
            Assert.IsNull(RunSessionManager.NextRoomData,
                "NextRoomData should be cleared after victory activation.");

            UnityEngine.Object.DestroyImmediate(roomData);
            UnityEngine.Object.DestroyImmediate(battleGO);
        }

        [Test]
        public void SubscribeToBattle_Defeat_DoesNotActivateRoom()
        {
            // Arrange
            var battleGO = new GameObject("BattleSystem");
            var battleSystem = battleGO.AddComponent<BattleSystem>();

            var roomData = ScriptableObject.CreateInstance<RoomData>();
            roomData.roomName = "Victory Room";
            roomData.activationType = RoomActivationType.OnCombatVictory;
            roomData.strategy = new TestRoomEffectStrategy();

            RunSessionManager.NextRoomData = roomData;
            RunSessionManager.SubscribeToBattle(battleSystem);

            // Act - fire Defeat
            var onBattleEnded = typeof(BattleSystem)
                .GetField("OnBattleEnded", BindingFlags.NonPublic | BindingFlags.Instance);
            var del = onBattleEnded.GetValue(battleSystem) as Action<BattleOutcome>;
            del?.Invoke(BattleOutcome.Defeat);

            // Assert
            Assert.AreEqual(0, TestRoomEffectStrategy.ExecutionCount,
                "Strategy should NOT execute on Defeat.");
            Assert.AreSame(roomData, RunSessionManager.NextRoomData,
                "NextRoomData should remain set on Defeat.");

            UnityEngine.Object.DestroyImmediate(roomData);
            UnityEngine.Object.DestroyImmediate(battleGO);
        }

        [Test]
        public void SubscribeToBattle_Victory_OnRoomLoadedType_DoesNotActivate()
        {
            // Arrange - room type is OnRoomLoaded, not OnCombatVictory
            var battleGO = new GameObject("BattleSystem");
            var battleSystem = battleGO.AddComponent<BattleSystem>();

            var roomData = ScriptableObject.CreateInstance<RoomData>();
            roomData.activationType = RoomActivationType.OnRoomLoaded;
            roomData.strategy = new TestRoomEffectStrategy();

            RunSessionManager.NextRoomData = roomData;
            RunSessionManager.SubscribeToBattle(battleSystem);

            // Act
            var onBattleEnded = typeof(BattleSystem)
                .GetField("OnBattleEnded", BindingFlags.NonPublic | BindingFlags.Instance);
            var del = onBattleEnded.GetValue(battleSystem) as Action<BattleOutcome>;
            del?.Invoke(BattleOutcome.Victory);

            // Assert
            Assert.AreEqual(0, TestRoomEffectStrategy.ExecutionCount,
                "OnRoomLoaded strategy should NOT activate on Victory.");
            Assert.AreSame(roomData, RunSessionManager.NextRoomData,
                "NextRoomData should remain set (wrong activation type).");

            UnityEngine.Object.DestroyImmediate(roomData);
            UnityEngine.Object.DestroyImmediate(battleGO);
        }

        [Test]
        public void SubscribeToBattle_Unsubscribes_AfterBattleEnded()
        {
            // Arrange
            var battleGO = new GameObject("BattleSystem");
            var battleSystem = battleGO.AddComponent<BattleSystem>();

            RunSessionManager.SubscribeToBattle(battleSystem);

            // Act - fire event once
            var onBattleEnded = typeof(BattleSystem)
                .GetField("OnBattleEnded", BindingFlags.NonPublic | BindingFlags.Instance);
            var del = onBattleEnded.GetValue(battleSystem) as Action<BattleOutcome>;
            del?.Invoke(BattleOutcome.Victory);

            // Now set a new room and fire again — should NOT execute since unsubscribed
            var roomData = ScriptableObject.CreateInstance<RoomData>();
            roomData.activationType = RoomActivationType.OnCombatVictory;
            roomData.strategy = new TestRoomEffectStrategy();
            RunSessionManager.NextRoomData = roomData;

            // Re-read the delegate — it should have been unsubscribed
            del = onBattleEnded.GetValue(battleSystem) as Action<BattleOutcome>;
            del?.Invoke(BattleOutcome.Victory);

            // Assert — strategy should NOT have fired from the second invocation
            // because RunSessionManager unsubscribed after the first event
            Assert.AreEqual(0, TestRoomEffectStrategy.ExecutionCount,
                "Strategy should not execute after unsubscription.");

            UnityEngine.Object.DestroyImmediate(roomData);
            UnityEngine.Object.DestroyImmediate(battleGO);
        }

        // ============================================================
        // CombatConfig Room Selection Tests
        // ============================================================

        [Test]
        public void CombatConfig_RoomChoiceCount_DefaultIs3()
        {
            var config = ScriptableObject.CreateInstance<CombatConfig>();
            Assert.AreEqual(3, config.roomChoiceCount);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void CombatConfig_AvailableRooms_DefaultIsEmpty()
        {
            var config = ScriptableObject.CreateInstance<CombatConfig>();
            Assert.IsNotNull(config.availableRooms);
            Assert.AreEqual(0, config.availableRooms.Count);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
