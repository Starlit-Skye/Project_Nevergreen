using System;
using System.Collections.Generic;
using System.IO;
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

        private string _testRunPath; private string _testProfilePath;

        [SetUp]
        public void SetUp()
        {
            RunSessionManager.Clear();
            RunSessionManager.IsResumingRun = false;
            TestRoomEffectStrategy.ExecutionCount = 0;

            // Redirect save operations to a temp file so tests never touch production save.dat
            _testRunPath = Path.Combine(Application.temporaryCachePath, "room_effect_test_run.dat"); _testProfilePath = Path.Combine(Application.temporaryCachePath, "room_effect_test_profile.dat");
            SaveManager.SetSavePathsForTesting(_testRunPath, _testProfilePath);
        }

        [TearDown]
        public void TearDown()
        {
            RunSessionManager.Clear();
            RunSessionManager.IsResumingRun = false;

            if (!string.IsNullOrEmpty(_testRunPath) && File.Exists(_testRunPath)) File.Delete(_testRunPath); if (!string.IsNullOrEmpty(_testProfilePath) && File.Exists(_testProfilePath))
            {
                File.Delete(_testProfilePath);
            }
            SaveManager.SetSavePathsForTesting(null, null);
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
            Assert.IsNull(RunSessionManager.NextRoomData,
                "NextRoomData should be cleared on Defeat because the run is wiped.");

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
        // Room Progression Tests
        // ============================================================

        [Test]
        public void RoomProgression_StartsAtZero()
        {
            Assert.AreEqual(0, RunSessionManager.RoomProgression);
        }

        [Test]
        public void RoomProgression_ClearResetsToZero()
        {
            RunSessionManager.RoomProgression = 5;
            RunSessionManager.Clear();
            Assert.AreEqual(0, RunSessionManager.RoomProgression);
        }

        [Test]
        public void RoomProgression_InitializeResetsToZero()
        {
            RunSessionManager.RoomProgression = 3;
            RunSessionManager.Initialize();
            Assert.AreEqual(0, RunSessionManager.RoomProgression);
        }

        [Test]
        public void Initialize_AssignsMarionetteRoomToNextRoomData()
        {
            // Arrange
            var roomDb = ScriptableObject.CreateInstance<RoomDatabase>();
            var marionetteRoom = ScriptableObject.CreateInstance<RoomData>();
            marionetteRoom.roomId = "RD_MarionetteRoom";
            marionetteRoom.roomName = "Marionette Room";
            roomDb.availableRooms.Add(marionetteRoom);

            var mockGameDb = GameDatabase.CreateForTesting(rooms: roomDb);
            GameDatabase.SetInstanceForTesting(mockGameDb);

            // Act
            RunSessionManager.Initialize();

            // Assert
            Assert.AreSame(marionetteRoom, RunSessionManager.NextRoomData);

            // Cleanup
            GameDatabase.SetInstanceForTesting(null);
            UnityEngine.Object.DestroyImmediate(mockGameDb);
            UnityEngine.Object.DestroyImmediate(marionetteRoom);
            UnityEngine.Object.DestroyImmediate(roomDb);
        }

        [Test]
        public void RoomProgression_OnSceneLoaded_IncrementsWhenCombatSceneAndPartyExists()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.OnSceneLoaded("CombatPrototype");
            
            Assert.AreEqual(1, RunSessionManager.RoomProgression);
        }

        [Test]
        public void RoomProgression_OnSceneLoaded_DoesNotIncrementWhenNotCombatScene()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.OnSceneLoaded("MainMenu");
            
            Assert.AreEqual(0, RunSessionManager.RoomProgression);
        }

        [Test]
        public void RoomProgression_OnSceneLoaded_DoesNotIncrementWhenPartyEmpty()
        {
            // CurrentParty is empty by default after Setup/Clear
            RunSessionManager.OnSceneLoaded("CombatPrototype");
            
            Assert.AreEqual(0, RunSessionManager.RoomProgression);
        }

        [Test]
        public void RoomProgression_OnSceneLoaded_SkipsIncrementWhenIsResumingRun()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.RoomProgression = 3;
            RunSessionManager.IsResumingRun = true;

            RunSessionManager.OnSceneLoaded("CombatPrototype");

            Assert.AreEqual(3, RunSessionManager.RoomProgression, "RoomProgression should not increment when resuming.");
            Assert.IsFalse(RunSessionManager.IsResumingRun, "IsResumingRun should be reset to false after scene load.");
        }

        // ============================================================
        // CombatConfig Room Selection Tests
        // ============================================================

        [Test]
        public void GlobalConfig_RoomChoiceCount_DefaultIs3()
        {
            var config = ScriptableObject.CreateInstance<GlobalConfig>();
            Assert.AreEqual(3, config.roomChoiceCount);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void RoomDatabase_AvailableRooms_DefaultIsEmpty()
        {
            var roomDb = ScriptableObject.CreateInstance<RoomDatabase>();
            Assert.IsNotNull(roomDb.availableRooms);
            Assert.AreEqual(0, roomDb.availableRooms.Count);
            UnityEngine.Object.DestroyImmediate(roomDb);
        }
        // ============================================================
        // Team Formation Update Tests
        // ============================================================

        [Test]
        public void BattleVictory_UpdatesRosterFormationOrder()
        {
            // Arrange
            var battleGO = new GameObject("BattleSystem");
            var battleSystem = battleGO.AddComponent<BattleSystem>();

            var p1 = new PartyMemberInfo();
            var p2 = new PartyMemberInfo();
            var p3 = new PartyMemberInfo();

            RunSessionManager.CurrentParty.Add(p1);
            RunSessionManager.CurrentParty.Add(p2);
            RunSessionManager.CurrentParty.Add(p3);

            var c1GO = new GameObject("C1");
            var c1 = c1GO.AddComponent<CombatCharacter>();
            c1.partyInfo = p1;
            c1.rank = 3;
            c1.state = LifeState.Alive;

            var c2GO = new GameObject("C2");
            var c2 = c2GO.AddComponent<CombatCharacter>();
            c2.partyInfo = p2;
            c2.rank = 1;
            c2.state = LifeState.Alive;

            var c3GO = new GameObject("C3");
            var c3 = c3GO.AddComponent<CombatCharacter>();
            c3.partyInfo = p3;
            c3.rank = 2;
            c3.state = LifeState.Alive;

            var playerTeam = new List<CombatCharacter> { c1, c2, c3 };
            typeof(BattleSystem).GetField("_playerTeam", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(battleSystem, playerTeam);
            typeof(BattleSystem).GetField("_enemyTeam", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(battleSystem, new List<CombatCharacter>());

            // Act - invoke CheckBattleEnd
            var isEnd = battleSystem.CheckBattleEnd();

            // Assert
            Assert.IsTrue(isEnd, "Battle should end in victory since enemy team is empty.");
            Assert.AreEqual(3, RunSessionManager.CurrentParty.Count);
            
            // Expected order based on ranks: c2 (rank 1) -> c3 (rank 2) -> c1 (rank 3)
            // Which maps to: p2, p3, p1
            Assert.AreSame(p2, RunSessionManager.CurrentParty[0]);
            Assert.AreSame(p3, RunSessionManager.CurrentParty[1]);
            Assert.AreSame(p1, RunSessionManager.CurrentParty[2]);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(battleGO);
            UnityEngine.Object.DestroyImmediate(c1GO);
            UnityEngine.Object.DestroyImmediate(c2GO);
            UnityEngine.Object.DestroyImmediate(c3GO);
        }

        [Test]
        public void BattleVictory_RemovesDeadAndPilesAndMaintainsFormation()
        {
            // Arrange
            var battleGO = new GameObject("BattleSystem");
            var battleSystem = battleGO.AddComponent<BattleSystem>();

            var p1 = new PartyMemberInfo();
            var p2 = new PartyMemberInfo();
            var p3 = new PartyMemberInfo();

            RunSessionManager.CurrentParty.Add(p1);
            RunSessionManager.CurrentParty.Add(p2);
            RunSessionManager.CurrentParty.Add(p3);

            var c1GO = new GameObject("C1");
            var c1 = c1GO.AddComponent<CombatCharacter>();
            c1.partyInfo = p1;
            c1.rank = 1;
            c1.state = LifeState.Pile; // Should be removed

            var c2GO = new GameObject("C2");
            var c2 = c2GO.AddComponent<CombatCharacter>();
            c2.partyInfo = p2;
            c2.rank = 3;
            c2.state = LifeState.Alive;

            var c3GO = new GameObject("C3");
            var c3 = c3GO.AddComponent<CombatCharacter>();
            c3.partyInfo = p3;
            c3.rank = 2;
            c3.state = LifeState.Alive;

            var playerTeam = new List<CombatCharacter> { c1, c2, c3 };
            typeof(BattleSystem).GetField("_playerTeam", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(battleSystem, playerTeam);
            typeof(BattleSystem).GetField("_enemyTeam", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(battleSystem, new List<CombatCharacter>());

            // Act
            var isEnd = battleSystem.CheckBattleEnd();

            // Assert
            Assert.IsTrue(isEnd);
            Assert.AreEqual(2, RunSessionManager.CurrentParty.Count);
            
            // Expected order based on remaining ranks: c3 (rank 2) -> c2 (rank 3)
            // Which maps to: p3, p2
            Assert.AreSame(p3, RunSessionManager.CurrentParty[0]);
            Assert.AreSame(p2, RunSessionManager.CurrentParty[1]);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(battleGO);
            UnityEngine.Object.DestroyImmediate(c1GO);
            UnityEngine.Object.DestroyImmediate(c2GO);
            UnityEngine.Object.DestroyImmediate(c3GO);
        }
        // ============================================================
        // BossRoomEffectStrategy Tests
        // ============================================================

        [Test]
        public void BossRoomEffectStrategy_Execute_CreatesUIWithEndRunButton()
        {
            // Arrange
            var canvasGO = new GameObject("TestCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var endRunPrefab = new GameObject("EndRunPrefab");
            endRunPrefab.AddComponent<RectTransform>(); // Need RectTransform to avoid null ref in strategy
            
            var strategy = new BossRoomEffectStrategy();
            strategy.endRunPrefab = endRunPrefab;

            // Act
            strategy.ExecuteRoomEffect();

            // Assert
            var victoryPanel = canvasGO.transform.Find("BossRoomVictoryPanel");
            Assert.IsNotNull(victoryPanel, "BossRoomVictoryPanel should be created under Canvas.");

            var textGo = victoryPanel.Find("RunCompletedText");
            Assert.IsNotNull(textGo, "RunCompletedText should be created under the panel.");
            var textComp = textGo.GetComponent<TMPro.TextMeshProUGUI>();
            Assert.IsNotNull(textComp, "TextMeshProUGUI component should be attached.");
            Assert.AreEqual("Run Completed", textComp.text);

            var endRunClone = victoryPanel.Find("EndRunPrefab(Clone)");
            Assert.IsNotNull(endRunClone, "EndRun prefab should be instantiated under the panel.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(canvasGO);
            UnityEngine.Object.DestroyImmediate(endRunPrefab);
        }
    }
}
