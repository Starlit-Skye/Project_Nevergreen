using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.UI;
using Nevergreen.Prototype;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TreasureUIControllerTests
    {
        private GameObject _uiRoot;
        private TreasureUIController _controller;
        
        private Button _openTreasureButton;
        private GameObject _rewardPanel;
        private TextMeshProUGUI _rewardText;
        private Button _closeButton;

        private string _testRunPath;
        private string _testProfilePath;

        [SetUp]
        public void SetUp()
        {
            // Setup mock save paths to prevent overwriting real saves
            _testRunPath = Path.Combine(Application.temporaryCachePath, "test_run_treasure.dat");
            _testProfilePath = Path.Combine(Application.temporaryCachePath, "test_profile_treasure.dat");
            SaveManager.SetSavePathsForTesting(_testRunPath, _testProfilePath);

            // Clean session state
            RunSessionManager.Initialize();
            RunSessionManager.RoomCompleted = false;

            // Build hierarchy
            _uiRoot = new GameObject("TreasureUI");
            _controller = _uiRoot.AddComponent<TreasureUIController>();

            var openGo = new GameObject("OpenButton");
            openGo.transform.SetParent(_uiRoot.transform);
            _openTreasureButton = openGo.AddComponent<Button>();

            _rewardPanel = new GameObject("RewardPanel");
            _rewardPanel.transform.SetParent(_uiRoot.transform);
            
            var textGo = new GameObject("RewardText");
            textGo.transform.SetParent(_rewardPanel.transform);
            _rewardText = textGo.AddComponent<TextMeshProUGUI>();

            var closeGo = new GameObject("CloseButton");
            closeGo.transform.SetParent(_rewardPanel.transform);
            _closeButton = closeGo.AddComponent<Button>();

            // Inject private fields via Reflection
            typeof(TreasureUIController).GetField("openTreasureButton", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _openTreasureButton);
            typeof(TreasureUIController).GetField("rewardPanel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _rewardPanel);
            typeof(TreasureUIController).GetField("rewardText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _rewardText);
            typeof(TreasureUIController).GetField("closeButton", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _closeButton);

            // Invoke OnEnable via reflection to ensure listeners are hooked up in EditMode
            var onEnableMethod = typeof(TreasureUIController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnableMethod?.Invoke(_controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_uiRoot != null)
            {
                Object.DestroyImmediate(_uiRoot);
            }
            
            if (File.Exists(_testRunPath)) File.Delete(_testRunPath);
            if (File.Exists(_testProfilePath)) File.Delete(_testProfilePath);
            
            SaveManager.SetSavePathsForTesting(null, null);
        }

        [Test]
        public void Initialize_SetsVisualStateAndRanges()
        {
            _rewardPanel.SetActive(true); // Set to true to verify it turns false
            _openTreasureButton.gameObject.SetActive(false); // Set to false to verify it turns true

            _controller.Initialize(10, 20, 5, 10);

            Assert.IsFalse(_rewardPanel.activeSelf, "Reward panel should be hidden on initialize.");
            Assert.IsTrue(_openTreasureButton.gameObject.activeSelf, "Open button should be active on initialize.");
        }

        [Test]
        public void OnOpenTreasureClicked_RollsBoundsAndUpdatesUI()
        {
            _controller.Initialize(10, 10, 5, 5); // Fixed ranges for predictable outcome
            
            _openTreasureButton.onClick.Invoke();

            Assert.IsFalse(_openTreasureButton.gameObject.activeSelf, "Open button should be hidden after click.");
            Assert.IsTrue(_rewardPanel.activeSelf, "Reward panel should be visible after click.");
            Assert.AreEqual("You found 10 Scraps and 5 Parts!", _rewardText.text, "Text should reflect rolled rewards.");
        }

        [Test]
        public void OnOpenTreasureClicked_RollsWithinConfiguredRanges()
        {
            _controller.Initialize(10, 30, 5, 15);
            _openTreasureButton.onClick.Invoke();

            // Extract rolled values via reflection since they are private
            int rolledScraps = (int)typeof(TreasureUIController).GetField("_rolledScraps", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_controller);
            int rolledParts = (int)typeof(TreasureUIController).GetField("_rolledParts", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_controller);

            Assert.IsTrue(rolledScraps >= 10 && rolledScraps <= 30, $"Scraps {rolledScraps} should be between 10 and 30");
            Assert.IsTrue(rolledParts >= 5 && rolledParts <= 15, $"Parts {rolledParts} should be between 5 and 15");
        }

        [Test]
        public void OnCloseClicked_GrantsCurrenciesAndSaves()
        {
            int initialScraps = RunSessionManager.Scraps;
            int initialParts = RunSessionManager.Parts;
            
            _controller.Initialize(15, 15, 8, 8); // Fixed amount
            _openTreasureButton.onClick.Invoke();
            
            // Delete save file if it exists to verify it gets created
            if (File.Exists(_testRunPath)) File.Delete(_testRunPath);
            
            _closeButton.onClick.Invoke();

            Assert.AreEqual(initialScraps + 15, RunSessionManager.Scraps, "Scraps should be updated.");
            Assert.AreEqual(initialParts + 8, RunSessionManager.Parts, "Parts should be updated.");
            Assert.IsTrue(File.Exists(_testRunPath), "SaveRun should be called.");
        }

        [Test]
        public void OnCloseClicked_ExecutesRoomCompletionFallback()
        {
            // Set up conditions to trigger the fallback complete room logic
            _controller.Initialize(10, 10, 5, 5);
            _openTreasureButton.onClick.Invoke();
            
            RunSessionManager.RoomCompleted = false;
            
            _closeButton.onClick.Invoke();
            
            Assert.IsTrue(RunSessionManager.RoomCompleted, "Fallback logic should call RunSessionManager.CompleteRoom.");
            Assert.IsFalse(_uiRoot.activeSelf, "The UI should deactivate itself upon completion.");
        }
    }
}
