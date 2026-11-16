using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using TMPro;
using Nevergreen.Data;
using Nevergreen.UI;
using Nevergreen.Prototype;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class ArmamentBuilderRoomControllerTests
    {
        private GameObject _uiRoot;
        private ArmamentBuilderRoomController _controller;

        private Button _recvEquipmentButton;
        private TextMeshProUGUI _recvEquipmentText;
        private Button _leaveButton1;
        private Button _leaveButton2;
        private GameObject _rewardPanel;
        private TextMeshProUGUI _rewardText;

        private TrinketData _common1;
        private TrinketData _uncommon1;
        private TrinketDatabase _trinketDatabase;
        private GameDatabase _gameDatabase;

        private int _initialScraps;
        private List<Object> _objectsToCleanUp;

        [SetUp]
        public void SetUp()
        {
            _objectsToCleanUp = new List<Object>();
            _initialScraps = RunSessionManager.Scraps;

            // Setup mock trinket database
            _common1 = ScriptableObject.CreateInstance<TrinketData>();
            _common1.trinketId = "C1";
            _common1.displayName = "Rusty Gear";
            _objectsToCleanUp.Add(_common1);

            _uncommon1 = ScriptableObject.CreateInstance<TrinketData>();
            _uncommon1.trinketId = "U1";
            _uncommon1.displayName = "Polished Spring";
            _objectsToCleanUp.Add(_uncommon1);

            _trinketDatabase = ScriptableObject.CreateInstance<TrinketDatabase>();
            _objectsToCleanUp.Add(_trinketDatabase);

            var commonField = typeof(TrinketDatabase).GetField("commonTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            commonField.SetValue(_trinketDatabase, new List<TrinketData> { _common1 });

            var uncommonField = typeof(TrinketDatabase).GetField("uncommonTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            uncommonField.SetValue(_trinketDatabase, new List<TrinketData> { _uncommon1 });

            _gameDatabase = GameDatabase.CreateForTesting(trinkets: _trinketDatabase);
            _objectsToCleanUp.Add(_gameDatabase);
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            // Build UI hierarchy
            _uiRoot = new GameObject("ArmamentBuilderUI");
            _objectsToCleanUp.Add(_uiRoot);
            _controller = _uiRoot.AddComponent<ArmamentBuilderRoomController>();

            var recvGo = new GameObject("RecvEquipmentButton");
            recvGo.transform.SetParent(_uiRoot.transform);
            _recvEquipmentButton = recvGo.AddComponent<Button>();

            var recvTextGo = new GameObject("RecvText");
            recvTextGo.transform.SetParent(recvGo.transform);
            _recvEquipmentText = recvTextGo.AddComponent<TextMeshProUGUI>();

            var leave1Go = new GameObject("LeaveButton1");
            leave1Go.transform.SetParent(_uiRoot.transform);
            _leaveButton1 = leave1Go.AddComponent<Button>();

            var leave2Go = new GameObject("LeaveButton2");
            leave2Go.transform.SetParent(_uiRoot.transform);
            _leaveButton2 = leave2Go.AddComponent<Button>();

            _rewardPanel = new GameObject("RewardPanel");
            _rewardPanel.transform.SetParent(_uiRoot.transform);

            var rewardTextGo = new GameObject("RewardText");
            rewardTextGo.transform.SetParent(_rewardPanel.transform);
            _rewardText = rewardTextGo.AddComponent<TextMeshProUGUI>();

            // Inject private fields via Reflection
            typeof(ArmamentBuilderRoomController).GetField("recvEquipmentButton", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _recvEquipmentButton);
            typeof(ArmamentBuilderRoomController).GetField("leaveButton1", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _leaveButton1);
            typeof(ArmamentBuilderRoomController).GetField("leaveButton2", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _leaveButton2);
            typeof(ArmamentBuilderRoomController).GetField("rewardPanel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _rewardPanel);
            typeof(ArmamentBuilderRoomController).GetField("rewardText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _rewardText);

            // Hook up OnEnable
            var onEnableMethod = typeof(ArmamentBuilderRoomController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnableMethod?.Invoke(_controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            RunSessionManager.Scraps = _initialScraps;
            GameDatabase.SetInstanceForTesting(null);

            if (_objectsToCleanUp != null)
            {
                foreach (var obj in _objectsToCleanUp)
                {
                    if (obj != null && !UnityEditor.EditorUtility.IsPersistent(obj))
                    {
                        Object.DestroyImmediate(obj);
                    }
                }
                _objectsToCleanUp.Clear();
            }
        }

        [Test]
        public void Initialize_SetsVisualStateAndCostText()
        {
            _rewardPanel.SetActive(true);
            _recvEquipmentButton.gameObject.SetActive(false);
            _controller.ScrapsCost = 25;

            _controller.Initialize();

            Assert.IsFalse(_rewardPanel.activeSelf, "Reward panel should be hidden on Initialize.");
            Assert.IsTrue(_recvEquipmentButton.gameObject.activeSelf, "Recv button should be active on Initialize.");
            Assert.AreEqual("Spend 25 Scraps, receive a random equipment.", _recvEquipmentText.text);
        }

        [Test]
        public void OnRecvEquipmentClicked_WithSufficientScraps_SpendsScrapsAndShowsReward()
        {
            _controller.ScrapsCost = 20;
            RunSessionManager.Scraps = 50;
            _controller.Initialize();

            _recvEquipmentButton.onClick.Invoke();

            Assert.AreEqual(30, RunSessionManager.Scraps, "Scraps should decrease by cost.");
            Assert.IsTrue(_controller.HasReceived, "HasReceived should be true after successful roll.");
            Assert.IsNotNull(_controller.RolledTrinket, "Rolled trinket should be assigned.");
            Assert.IsTrue(_rewardPanel.activeSelf, "Reward panel should be shown.");
            Assert.IsFalse(_recvEquipmentButton.gameObject.activeSelf, "Recv button should be hidden.");
            Assert.IsTrue(_rewardText.text.Contains(_controller.RolledTrinket.displayName), "Reward text should display trinket name.");
        }

        [Test]
        public void OnRecvEquipmentClicked_WithInsufficientScraps_DoesNotSpendOrShowReward()
        {
            _controller.ScrapsCost = 20;
            RunSessionManager.Scraps = 10;
            _controller.Initialize();

            _recvEquipmentButton.onClick.Invoke();

            Assert.AreEqual(10, RunSessionManager.Scraps, "Scraps should remain unchanged.");
            Assert.IsFalse(_controller.HasReceived, "HasReceived should remain false.");
            Assert.IsNull(_controller.RolledTrinket, "Rolled trinket should be null.");
            Assert.IsFalse(_rewardPanel.activeSelf, "Reward panel should stay hidden.");
            Assert.IsTrue(_recvEquipmentButton.gameObject.activeSelf, "Recv button should remain active.");
        }

        [Test]
        public void OnRecvEquipmentClicked_OnlyExecutesOnce()
        {
            _controller.ScrapsCost = 20;
            RunSessionManager.Scraps = 50;
            _controller.Initialize();

            _recvEquipmentButton.onClick.Invoke();
            Assert.AreEqual(30, RunSessionManager.Scraps);

            // Second click
            _recvEquipmentButton.onClick.Invoke();
            Assert.AreEqual(30, RunSessionManager.Scraps, "Subsequent clicks should not deduct scraps again.");
        }

        [Test]
        public void RollTrinketReward_WeightsPureCommon_ReturnsCommon()
        {
            _controller.CommonTrinketWeight = 100f;
            _controller.UncommonTrinketWeight = 0f;

            var result = _controller.RollTrinketReward();

            Assert.AreSame(_common1, result);
        }

        [Test]
        public void RollTrinketReward_WeightsPureUncommon_ReturnsUncommon()
        {
            _controller.CommonTrinketWeight = 0f;
            _controller.UncommonTrinketWeight = 100f;

            var result = _controller.RollTrinketReward();

            Assert.AreSame(_uncommon1, result);
        }

        [Test]
        public void OnRecvEquipmentClicked_WhenNoTrinketAvailable_DoesNotSpendScraps()
        {
            _controller.CommonTrinketWeight = 0f;
            _controller.UncommonTrinketWeight = 0f;
            _controller.ScrapsCost = 20;
            RunSessionManager.Scraps = 50;
            _controller.Initialize();

            LogAssert.Expect(LogType.Warning, "[ArmamentBuilderRoomController] No trinket available to reward! Scraps not spent.");
            _recvEquipmentButton.onClick.Invoke();

            Assert.AreEqual(50, RunSessionManager.Scraps, "Scraps should not be deducted if no trinket is available.");
            Assert.IsFalse(_controller.HasReceived);
        }

        [Test]
        public void LeaveButton1_DeactivatesGameObjectAndNotifiesCombatUI()
        {
            var combatUIGo = new GameObject("CombatUI");
            _objectsToCleanUp.Add(combatUIGo);
            combatUIGo.AddComponent<CombatUI>();

            _leaveButton1.onClick.Invoke();

            Assert.IsFalse(_uiRoot.activeSelf, "LeaveButton1 should deactivate the room UI.");
        }

        [Test]
        public void LeaveButton2_DeactivatesGameObjectAndNotifiesCombatUI()
        {
            var combatUIGo = new GameObject("CombatUI");
            _objectsToCleanUp.Add(combatUIGo);
            combatUIGo.AddComponent<CombatUI>();

            _leaveButton2.onClick.Invoke();

            Assert.IsFalse(_uiRoot.activeSelf, "LeaveButton2 should deactivate the room UI.");
        }

        [Test]
        public void LeaveButton_WhenCombatUIMissing_LogsErrorAndStillDeactivates()
        {
            LogAssert.Expect(LogType.Error, "[ArmamentBuilderRoomController] CombatUI not found!");
            _leaveButton1.onClick.Invoke();

            Assert.IsFalse(_uiRoot.activeSelf, "UI should still deactivate even if CombatUI is missing.");
        }

        [Test]
        public void OnDisable_RemovesListeners()
        {
            var onDisableMethod = typeof(ArmamentBuilderRoomController).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisableMethod?.Invoke(_controller, null);

            RunSessionManager.Scraps = 50;
            _controller.ScrapsCost = 20;

            _recvEquipmentButton.onClick.Invoke();
            Assert.AreEqual(50, RunSessionManager.Scraps, "Clicks after OnDisable should not invoke handler.");

            _leaveButton1.onClick.Invoke();
            Assert.IsTrue(_uiRoot.activeSelf, "Leave clicks after OnDisable should not deactivate UI.");
        }

        [Test]
        public void OnUnassignedTrinketChanged_UpdatesRolledTrinket()
        {
            _controller.OnUnassignedTrinketChanged(_common1, _uncommon1);

            Assert.AreSame(_uncommon1, _controller.RolledTrinket);
        }
    }
}
