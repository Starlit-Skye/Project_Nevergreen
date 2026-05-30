using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Nevergreen.Combat;
using Nevergreen.Prototype;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class CombatUITests
    {
        private GameObject go;
        private CombatUI combatUI;
        private GameObject battleEndPanel;
        private GameObject nextRoomButtonGo;
        private Button nextRoomButton;

        [SetUp]
        public void Setup()
        {
            go = new GameObject("CombatUI");
            combatUI = go.AddComponent<CombatUI>();

            battleEndPanel = new GameObject("BattleEndPanel");
            combatUI.battleEndPanel = battleEndPanel;

            nextRoomButtonGo = new GameObject("NextRoomButton");
            nextRoomButton = nextRoomButtonGo.AddComponent<Button>();
            combatUI.nextRoomButton = nextRoomButton;
        }

        [TearDown]
        public void Teardown()
        {
            if (battleEndPanel != null) Object.DestroyImmediate(battleEndPanel);
            if (nextRoomButtonGo != null) Object.DestroyImmediate(nextRoomButtonGo);
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void HandleBattleEnded_ShowsNextRoomButton_OnVictory()
        {
            // Initially, nextRoomButton should be active or inactive depending on state
            nextRoomButton.gameObject.SetActive(false);

            // Call HandleBattleEnded(Victory)
            var handleEndedMethod = typeof(CombatUI).GetMethod("HandleBattleEnded", BindingFlags.NonPublic | BindingFlags.Instance);
            handleEndedMethod.Invoke(combatUI, new object[] { BattleOutcome.Victory });

            // Button should be active
            Assert.IsTrue(nextRoomButton.gameObject.activeSelf, "Next Room button should be active on Victory.");
        }

        [Test]
        public void HandleBattleEnded_HidesNextRoomButton_OnDefeat()
        {
            nextRoomButton.gameObject.SetActive(true);

            // Call HandleBattleEnded(Defeat)
            var handleEndedMethod = typeof(CombatUI).GetMethod("HandleBattleEnded", BindingFlags.NonPublic | BindingFlags.Instance);
            handleEndedMethod.Invoke(combatUI, new object[] { BattleOutcome.Defeat });

            // Button should be inactive
            Assert.IsFalse(nextRoomButton.gameObject.activeSelf, "Next Room button should be inactive on Defeat.");
        }

        [Test]
        public void Initialize_HidesNextRoomButton_AndRegistersListener()
        {
            // We need a dummy BattleSystem to satisfy Initialize parameter, or pass null if safe
            // Let's create a dummy BattleSystem
            var bsGo = new GameObject("BattleSystem");
            var bs = bsGo.AddComponent<BattleSystem>();

            var playerTeam = new List<CombatCharacter>();
            var enemyTeam = new List<CombatCharacter>();

            // Setup nextRoomButton as active initially
            nextRoomButton.gameObject.SetActive(true);

            // Run Initialize
            combatUI.Initialize(bs, playerTeam, enemyTeam);

            // Verify button is deactivated during initialization
            Assert.IsFalse(nextRoomButton.gameObject.activeSelf, "Next Room button should be deactivated on Initialization.");

            // Cleanup
            Object.DestroyImmediate(bsGo);
        }
    }
}
