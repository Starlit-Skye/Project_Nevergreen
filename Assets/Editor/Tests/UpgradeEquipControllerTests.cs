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
    public class UpgradeEquipControllerTests
    {
        private GameObject _uiRoot;
        private UpgradeEquipController _controller;

        private GameObject _reforgePanel;
        private Button _reforgeButton;
        private TextMeshProUGUI _reforgeButtonText;
        private Button _leaveButton;
        private GameObject _dropperContainer;
        private GameObject _trinketPrefab;

        private TrinketData _common1;
        private TrinketData _uncommon1;
        private TrinketData _rare1;
        private TrinketData _rare2;
        private TrinketDatabase _trinketDatabase;
        private GameDatabase _gameDatabase;

        private List<Object> _objectsToCleanUp;

        [SetUp]
        public void SetUp()
        {
            _objectsToCleanUp = new List<Object>();

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

            _rare2 = ScriptableObject.CreateInstance<TrinketData>();
            _rare2.trinketId = "R2";
            _rare2.displayName = "Aether Heart";
            _objectsToCleanUp.Add(_rare2);

            _trinketDatabase = ScriptableObject.CreateInstance<TrinketDatabase>();
            _objectsToCleanUp.Add(_trinketDatabase);

            var commonField = typeof(TrinketDatabase).GetField("commonTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            commonField.SetValue(_trinketDatabase, new List<TrinketData> { _common1 });

            var uncommonField = typeof(TrinketDatabase).GetField("uncommonTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            uncommonField.SetValue(_trinketDatabase, new List<TrinketData> { _uncommon1 });

            var rareField = typeof(TrinketDatabase).GetField("rareTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            rareField.SetValue(_trinketDatabase, new List<TrinketData> { _rare1, _rare2 });

            _gameDatabase = GameDatabase.CreateForTesting(trinkets: _trinketDatabase);
            _objectsToCleanUp.Add(_gameDatabase);
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            // Mock Trinket UI item prefab
            _trinketPrefab = new GameObject("TrinketUIItemPrefab");
            _trinketPrefab.AddComponent<TrinketUIItem>();
            _trinketPrefab.AddComponent<TrinketTooltipTrigger>();
            _objectsToCleanUp.Add(_trinketPrefab);

            // Build UI hierarchy
            _uiRoot = new GameObject("UpgradeEquipUI");
            _objectsToCleanUp.Add(_uiRoot);
            _controller = _uiRoot.AddComponent<UpgradeEquipController>();

            _reforgePanel = new GameObject("ReforgePanel");
            _reforgePanel.transform.SetParent(_uiRoot.transform);

            var reforgeBtnGo = new GameObject("ReforgeButton");
            reforgeBtnGo.transform.SetParent(_uiRoot.transform);
            _reforgeButton = reforgeBtnGo.AddComponent<Button>();

            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(reforgeBtnGo.transform);
            _reforgeButtonText = btnTextGo.AddComponent<TextMeshProUGUI>();
            _reforgeButtonText.text = "Reforge";

            var leaveBtnGo = new GameObject("LeaveButton");
            leaveBtnGo.transform.SetParent(_uiRoot.transform);
            _leaveButton = leaveBtnGo.AddComponent<Button>();

            _dropperContainer = new GameObject("Test_Trinket_Dropper");
            _dropperContainer.transform.SetParent(_uiRoot.transform);

            // Inject fields via reflection
            typeof(UpgradeEquipController).GetField("reforgePanel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _reforgePanel);
            typeof(UpgradeEquipController).GetField("reforgeButton", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _reforgeButton);
            typeof(UpgradeEquipController).GetField("leaveButton", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _leaveButton);
            typeof(UpgradeEquipController).GetField("testTrinketDroppedContainer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _dropperContainer.transform);
            typeof(UpgradeEquipController).GetField("trinketUIItemPrefab", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _trinketPrefab);

            // Hook up OnEnable
            var onEnableMethod = typeof(UpgradeEquipController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnableMethod?.Invoke(_controller, null);
        }

        [TearDown]
        public void TearDown()
        {
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
        public void Initialize_HidesReforgePanel_AndResetsReforgedState()
        {
            _reforgePanel.SetActive(true);
            _controller.Initialize();

            Assert.IsFalse(_reforgePanel.activeSelf, "Reforge panel should be hidden on Initialize.");
            Assert.IsFalse(_controller.HasReforged, "HasReforged should be reset to false.");
        }

        [Test]
        public void OnReforgeClicked_WhenDropperEmpty_DoesNothing()
        {
            _controller.OnReforgeClicked();

            Assert.IsFalse(_controller.HasReforged);
            Assert.AreEqual("Reforge", _reforgeButtonText.text);
        }

        [Test]
        public void OnReforgeClicked_WhenDropperItemHasNullData_DoesNothing()
        {
            PutTrinketInDropper(null);

            _controller.OnReforgeClicked();

            Assert.IsFalse(_controller.HasReforged);
            Assert.AreEqual("Reforge", _reforgeButtonText.text);
        }

        [Test]
        public void OnReforgeClicked_WithCommonTrinket_UpgradesToUncommon()
        {
            var uiItem = PutTrinketInDropper(_common1);

            _controller.OnReforgeClicked();

            Assert.IsTrue(_controller.HasReforged);
            Assert.AreEqual(_uncommon1, uiItem.TrinketData);
            Assert.AreEqual("Leave", _reforgeButtonText.text);
        }

        [Test]
        public void OnReforgeClicked_WithUncommonTrinket_UpgradesToRare()
        {
            var uiItem = PutTrinketInDropper(_uncommon1);

            _controller.OnReforgeClicked();

            Assert.IsTrue(_controller.HasReforged);
            Assert.IsTrue(uiItem.TrinketData == _rare1 || uiItem.TrinketData == _rare2);
            Assert.AreEqual("Leave", _reforgeButtonText.text);
        }

        [Test]
        public void OnReforgeClicked_WithRareTrinket_UpgradesToDifferentRare()
        {
            var uiItem = PutTrinketInDropper(_rare1);

            _controller.OnReforgeClicked();

            Assert.IsTrue(_controller.HasReforged);
            Assert.AreEqual(_rare2, uiItem.TrinketData, "Reforging R1 should select a different Rare (R2).");
            Assert.AreEqual("Leave", _reforgeButtonText.text);
        }

        [Test]
        public void OnReforgeClicked_WithRareTrinket_WhenOnlySameRareInPool_LogsWarningAndDoesNothing()
        {
            // Configure Rare pool to only have _rare1
            var rareField = typeof(TrinketDatabase).GetField("rareTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            rareField.SetValue(_trinketDatabase, new List<TrinketData> { _rare1 });

            var uiItem = PutTrinketInDropper(_rare1);

            LogAssert.Expect(LogType.Warning, "[UpgradeEquipController] No upgrade trinket available in the target pool!");
            _controller.OnReforgeClicked();

            Assert.IsFalse(_controller.HasReforged);
            Assert.AreEqual(_rare1, uiItem.TrinketData);
            Assert.AreEqual("Reforge", _reforgeButtonText.text);
        }

        [Test]
        public void OnReforgeClicked_WhenTargetPoolEmpty_LogsWarningAndDoesNothing()
        {
            // Empty Uncommon pool
            var uncommonField = typeof(TrinketDatabase).GetField("uncommonTrinkets", BindingFlags.NonPublic | BindingFlags.Instance);
            uncommonField.SetValue(_trinketDatabase, new List<TrinketData>());

            var uiItem = PutTrinketInDropper(_common1);

            LogAssert.Expect(LogType.Warning, "[UpgradeEquipController] No upgrade trinket available in the target pool!");
            _controller.OnReforgeClicked();

            Assert.IsFalse(_controller.HasReforged);
            Assert.AreEqual(_common1, uiItem.TrinketData);
        }

        [Test]
        public void OnReforgeClicked_WhenTrinketNotInAnyPool_LogsWarningAndDoesNothing()
        {
            var rogueTrinket = ScriptableObject.CreateInstance<TrinketData>();
            rogueTrinket.trinketId = "X1";
            rogueTrinket.displayName = "Unknown Relic";
            _objectsToCleanUp.Add(rogueTrinket);

            var uiItem = PutTrinketInDropper(rogueTrinket);

            LogAssert.Expect(LogType.Warning, "[UpgradeEquipController] Trinket 'Unknown Relic' is not found in Common, Uncommon, or Rare database pools!");
            _controller.OnReforgeClicked();

            Assert.IsFalse(_controller.HasReforged);
            Assert.AreEqual(rogueTrinket, uiItem.TrinketData);
        }

        [Test]
        public void OnReforgeClicked_WhenAlreadyReforged_CompletesRoomFlow()
        {
            var dummyCombatUI = new GameObject("CombatUI").AddComponent<CombatUI>();
            _objectsToCleanUp.Add(dummyCombatUI.gameObject);

            PutTrinketInDropper(_common1);
            _controller.OnReforgeClicked();
            Assert.IsTrue(_controller.HasReforged);

            // Second click triggers CompleteRoom
            _controller.OnReforgeClicked();

            Assert.IsFalse(_uiRoot.activeSelf, "UI Root should be deactivated on CompleteRoom.");
        }

        [Test]
        public void OnLeaveClicked_CompletesRoomFlow()
        {
            var dummyCombatUI = new GameObject("CombatUI").AddComponent<CombatUI>();
            _objectsToCleanUp.Add(dummyCombatUI.gameObject);

            _controller.OnLeaveClicked();

            Assert.IsFalse(_uiRoot.activeSelf, "UI Root should be deactivated when Leave is clicked.");
        }

        [Test]
        public void CompleteRoom_WhenCombatUIMissing_LogsErrorAndStillDeactivates()
        {
            LogAssert.Expect(LogType.Error, "[UpgradeEquipController] CombatUI not found!");

            _controller.CompleteRoom();

            Assert.IsFalse(_uiRoot.activeSelf, "UI Root should still be deactivated even when CombatUI is missing.");
        }

        [Test]
        public void OnDisable_RemovesListeners()
        {
            var onDisableMethod = typeof(UpgradeEquipController).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisableMethod?.Invoke(_controller, null);

            PutTrinketInDropper(_common1);

            // Invoking onClick directly should have no effect
            _reforgeButton.onClick.Invoke();
            Assert.IsFalse(_controller.HasReforged, "Reforge click should be unhooked after OnDisable.");

            _leaveButton.onClick.Invoke();
            Assert.IsTrue(_uiRoot.activeSelf, "Leave click should be unhooked after OnDisable.");
        }
    }
}

