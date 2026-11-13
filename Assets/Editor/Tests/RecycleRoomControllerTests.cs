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
    public class RecycleRoomControllerTests
    {
        private GameObject _uiRoot;
        private RecycleRoomController _controller;

        private Button _recycleButton;
        private Button _confirmButton;
        private Button _leaveButton;
        private GameObject _recycleSelectionPanel;
        private TextMeshProUGUI _scrapsRecvText;
        private GameObject _dropperContainer;
        private GameObject _trinketPrefab;

        private TrinketData _common1;
        private TrinketData _uncommon1;
        private TrinketData _rare1;
        private TrinketDatabase _trinketDatabase;
        private GameDatabase _gameDatabase;

        private int _initialScraps;
        private List<Object> _objectsToCleanUp;

        [SetUp]
        public void SetUp()
        {
            _objectsToCleanUp = new List<Object>();
            _initialScraps = RunSessionManager.Scraps;
            RunSessionManager.Scraps = 0;

            // Setup mock trinket database
            _common1 = ScriptableObject.CreateInstance<TrinketData>();
            _common1.trinketId = "C1";
            _common1.displayName = "Rusty Gear";
            _objectsToCleanUp.Add(_common1);

            _uncommon1 = ScriptableObject.CreateInstance<TrinketData>();
            _uncommon1.trinketId = "U1";
            _uncommon1.displayName = "Polished Spring";
            _objectsToCleanUp.Add(_uncommon1);

            _rare1 = ScriptableObject.CreateInstance<TrinketData>();
            _rare1.trinketId = "R1";
            _rare1.displayName = "Chrono Crystal";
            _objectsToCleanUp.Add(_rare1);

            _trinketDatabase = ScriptableObject.CreateInstance<TrinketDatabase>();
            _objectsToCleanUp.Add(_trinketDatabase);

            var commonField = typeof(TrinketDatabase).GetField("commonTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            commonField.SetValue(_trinketDatabase, new List<TrinketData> { _common1 });

            var uncommonField = typeof(TrinketDatabase).GetField("uncommonTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            uncommonField.SetValue(_trinketDatabase, new List<TrinketData> { _uncommon1 });

            var rareField = typeof(TrinketDatabase).GetField("rareTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            rareField.SetValue(_trinketDatabase, new List<TrinketData> { _rare1 });

            _gameDatabase = GameDatabase.CreateForTesting(trinkets: _trinketDatabase);
            _objectsToCleanUp.Add(_gameDatabase);
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            // Mock Trinket UI item prefab
            _trinketPrefab = new GameObject("TrinketUIItemPrefab");
            _trinketPrefab.AddComponent<TrinketUIItem>();
            _trinketPrefab.AddComponent<TrinketTooltipTrigger>();
            _objectsToCleanUp.Add(_trinketPrefab);

            // Build UI hierarchy
            _uiRoot = new GameObject("RecycleRoomUI");
            _objectsToCleanUp.Add(_uiRoot);
            _controller = _uiRoot.AddComponent<RecycleRoomController>();

            var recycleBtnGo = new GameObject("RecycleButton");
            recycleBtnGo.transform.SetParent(_uiRoot.transform);
            _recycleButton = recycleBtnGo.AddComponent<Button>();

            var leaveBtnGo = new GameObject("LeaveButton");
            leaveBtnGo.transform.SetParent(_uiRoot.transform);
            _leaveButton = leaveBtnGo.AddComponent<Button>();

            _recycleSelectionPanel = new GameObject("RecycleSelectionPanel");
            _recycleSelectionPanel.transform.SetParent(_uiRoot.transform);

            var confirmBtnGo = new GameObject("ConfirmButton");
            confirmBtnGo.transform.SetParent(_recycleSelectionPanel.transform);
            _confirmButton = confirmBtnGo.AddComponent<Button>();

            _dropperContainer = new GameObject("Test_Trinket_Dropper");
            _dropperContainer.transform.SetParent(_recycleSelectionPanel.transform);

            var scrapsRecvGo = new GameObject("ScrapsRecv");
            scrapsRecvGo.transform.SetParent(_recycleSelectionPanel.transform);
            _scrapsRecvText = scrapsRecvGo.AddComponent<TextMeshProUGUI>();

            // Inject serialized fields
            SetPrivateField("recycleButton", _recycleButton);
            SetPrivateField("confirmButton", _confirmButton);
            SetPrivateField("leaveButton", _leaveButton);
            SetPrivateField("recycleSelectionPanel", _recycleSelectionPanel);
            SetPrivateField("scrapsRecvText", _scrapsRecvText);
            SetPrivateField("testTrinketDroppedContainer", _dropperContainer.transform);
            SetPrivateField("trinketUIItemPrefab", _trinketPrefab);

            // Invoke OnEnable
            var onEnable = typeof(RecycleRoomController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnable?.Invoke(_controller, null);

            _controller.Initialize();
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

        private void SetPrivateField(string fieldName, object value)
        {
            typeof(RecycleRoomController)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_controller, value);
        }

        private TrinketUIItem PutTrinketInDropper(TrinketData trinket)
        {
            var itemGo = new GameObject("SlottedTrinket");
            itemGo.transform.SetParent(_dropperContainer.transform);
            var tooltip = itemGo.AddComponent<TrinketTooltipTrigger>();
            tooltip.SetTrinket(trinket);
            var uiItem = itemGo.AddComponent<TrinketUIItem>();
            uiItem.Initialize(trinket);
            return uiItem;
        }

        [Test]
        public void Initialize_HidesSelectionPanel_ShowsRecycleButton_DisablesConfirm_AndClearsDropper()
        {
            PutTrinketInDropper(_common1);
            _recycleSelectionPanel.SetActive(true);

            _controller.Initialize();

            Assert.IsFalse(_recycleSelectionPanel.activeSelf, "Recycle selection panel should be hidden on Initialize.");
            Assert.IsTrue(_recycleButton.gameObject.activeSelf, "Recycle button should be visible on Initialize.");
            Assert.IsFalse(_confirmButton.interactable, "Confirm button should be disabled when dropper is empty.");
            Assert.AreEqual(0, _dropperContainer.transform.childCount, "Dropper should be cleared on Initialize.");
        }

        [Test]
        public void OnRecycleClicked_OpensSelectionPanel_KeepsRecycleButtonVisible_AndSpawnsNoTrinket()
        {
            _recycleButton.onClick.Invoke();

            Assert.IsTrue(_recycleSelectionPanel.activeSelf, "Recycle selection panel should open on Recycle click.");
            Assert.IsTrue(_recycleButton.gameObject.activeSelf, "Recycle button must stay visible after opening panel.");
            Assert.AreEqual(0, _dropperContainer.transform.childCount, "No trinket should be generated in the dropper.");
            Assert.IsFalse(_confirmButton.interactable, "Confirm button must remain disabled until a trinket is placed in dropper.");
        }

        [Test]
        public void ConfirmButton_EnablesWhenTrinketInDropper_AndDisablesWhenRemoved()
        {
            _controller.OnRecycleClicked();
            Assert.IsFalse(_confirmButton.interactable);

            var item = PutTrinketInDropper(_common1);
            _controller.RefreshTrinketUI();
            Assert.IsTrue(_confirmButton.interactable, "Confirm should enable when a trinket is in the dropper.");

            Object.DestroyImmediate(item.gameObject);
            _controller.EvaluateConfirmButton();
            Assert.IsFalse(_confirmButton.interactable, "Confirm should disable once the trinket is removed from the dropper.");
        }

        [Test]
        public void OnConfirmClicked_WithCommonTrinket_ConsumesTrinket_GrantsCommonScraps_UpdatesText_AndKeepsRoomOpen()
        {
            _controller.CommonScraps = 10;
            _controller.OnRecycleClicked();
            PutTrinketInDropper(_common1);
            _controller.EvaluateConfirmButton();

            _confirmButton.onClick.Invoke();

            Assert.AreEqual(10, RunSessionManager.Scraps);
            Assert.AreEqual("Received 10 Scraps", _scrapsRecvText.text);
            Assert.AreEqual(0, _dropperContainer.transform.childCount, "Consumed trinket should be destroyed.");
            Assert.IsFalse(_confirmButton.interactable, "Confirm button should disable after consuming trinket.");
            Assert.IsTrue(_uiRoot.activeSelf, "Room must remain open after Confirm.");
        }

        [Test]
        public void OnConfirmClicked_WithUncommonTrinket_GrantsUncommonScraps_AndUpdatesText()
        {
            _controller.UncommonScraps = 20;
            _controller.OnRecycleClicked();
            PutTrinketInDropper(_uncommon1);

            _confirmButton.onClick.Invoke();

            Assert.AreEqual(20, RunSessionManager.Scraps);
            Assert.AreEqual("Received 20 Scraps", _scrapsRecvText.text);
            Assert.AreEqual(0, _dropperContainer.transform.childCount);
        }

        [Test]
        public void OnConfirmClicked_WithRareTrinket_GrantsRareScraps_AndUpdatesText()
        {
            _controller.RareScraps = 40;
            _controller.OnRecycleClicked();
            PutTrinketInDropper(_rare1);

            _confirmButton.onClick.Invoke();

            Assert.AreEqual(40, RunSessionManager.Scraps);
            Assert.AreEqual("Received 40 Scraps", _scrapsRecvText.text);
            Assert.AreEqual(0, _dropperContainer.transform.childCount);
        }

        [Test]
        public void OnConfirmClicked_AllowsRecyclingMultipleTrinketsSequentially()
        {
            _controller.CommonScraps = 10;
            _controller.RareScraps = 40;
            _controller.OnRecycleClicked();

            // First recycle
            PutTrinketInDropper(_common1);
            _confirmButton.onClick.Invoke();
            Assert.AreEqual(10, RunSessionManager.Scraps);
            Assert.IsTrue(_uiRoot.activeSelf);

            // Second recycle
            PutTrinketInDropper(_rare1);
            _confirmButton.onClick.Invoke();
            Assert.AreEqual(50, RunSessionManager.Scraps);
            Assert.AreEqual(0, _dropperContainer.transform.childCount);
            Assert.IsTrue(_uiRoot.activeSelf);
        }

        [Test]
        public void OnConfirmClicked_WithMultipleTrinketsInDropper_ConsumesAllAndSumsScraps()
        {
            _controller.CommonScraps = 10;
            _controller.UncommonScraps = 20;
            _controller.OnRecycleClicked();

            PutTrinketInDropper(_common1);
            PutTrinketInDropper(_uncommon1);

            _confirmButton.onClick.Invoke();

            Assert.AreEqual(30, RunSessionManager.Scraps);
            Assert.AreEqual(0, _dropperContainer.transform.childCount);
        }

        [Test]
        public void OnConfirmClicked_WhenTrinketNotInAnyPool_LogsWarning_DoesNotConsume_AndGrantsNoScraps()
        {
            var unmapped = ScriptableObject.CreateInstance<TrinketData>();
            unmapped.trinketId = "X1";
            unmapped.displayName = "Quest Relic";
            _objectsToCleanUp.Add(unmapped);

            _controller.OnRecycleClicked();
            PutTrinketInDropper(unmapped);

            LogAssert.Expect(LogType.Warning, "[RecycleRoomController] Trinket 'Quest Relic' is not found in Common, Uncommon, or Rare database pools!");
            _confirmButton.onClick.Invoke();

            Assert.AreEqual(0, RunSessionManager.Scraps);
            Assert.AreEqual(1, _dropperContainer.transform.childCount, "Unmapped trinket should not be consumed.");
        }

        [Test]
        public void OnLeaveClicked_CompletesRoomFlow()
        {
            var dummyCombatUI = new GameObject("CombatUI").AddComponent<CombatUI>();
            _objectsToCleanUp.Add(dummyCombatUI.gameObject);

            _leaveButton.onClick.Invoke();

            Assert.IsFalse(_uiRoot.activeSelf);
        }

        [Test]
        public void CompleteRoom_WhenCombatUIMissing_LogsErrorAndStillDeactivates()
        {
            LogAssert.Expect(LogType.Error, "[RecycleRoomController] CombatUI not found!");

            _controller.CompleteRoom();

            Assert.IsFalse(_uiRoot.activeSelf);
        }

        [Test]
        public void OnDisable_RemovesButtonListeners()
        {
            var onDisable = typeof(RecycleRoomController).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisable?.Invoke(_controller, null);

            _recycleButton.onClick.Invoke();
            Assert.IsFalse(_recycleSelectionPanel.activeSelf, "Recycle click should do nothing after OnDisable.");

            PutTrinketInDropper(_common1);
            _confirmButton.onClick.Invoke();
            Assert.AreEqual(0, RunSessionManager.Scraps, "Confirm click should do nothing after OnDisable.");

            _leaveButton.onClick.Invoke();
            Assert.IsTrue(_uiRoot.activeSelf, "Leave click should do nothing after OnDisable.");
        }
    }
}

