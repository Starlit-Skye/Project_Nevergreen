using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.UI;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class DollmakerUIControllerTests
    {
        private GameObject _uiRoot;
        private DollmakerUIController _controller;
        
        private Button[] _marionetteButtons;
        private Transform _perfectionsContainer;
        private Transform _imperfectionsContainer;
        private GameObject _perfectionPrefab;
        private GameObject _imperfectionPrefab;

        private CharacterData _mockCharacter1;
        private CharacterData _mockCharacter2;
        private TraitData _mockPerfection;
        private TraitData _mockImperfection;

        [SetUp]
        public void SetUp()
        {
            // Clean session state
            RunSessionManager.Initialize();
            RunSessionManager.RoomCompleted = false;

            // Build hierarchy
            _uiRoot = new GameObject("DollmakerUI");
            _controller = _uiRoot.AddComponent<DollmakerUIController>();

            _marionetteButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                var btnGo = new GameObject($"MarionetteButton{i}");
                btnGo.transform.SetParent(_uiRoot.transform);
                _marionetteButtons[i] = btnGo.AddComponent<Button>();
                
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(btnGo.transform);
                textGo.AddComponent<TextMeshProUGUI>();
            }

            var perfContGo = new GameObject("PerfectionsContainer");
            perfContGo.transform.SetParent(_uiRoot.transform);
            _perfectionsContainer = perfContGo.transform;

            var imperfContGo = new GameObject("ImperfectionsContainer");
            imperfContGo.transform.SetParent(_uiRoot.transform);
            _imperfectionsContainer = imperfContGo.transform;

            _perfectionPrefab = new GameObject("PerfectionPrefab");
            var perfTextGo = new GameObject("Text");
            perfTextGo.transform.SetParent(_perfectionPrefab.transform);
            perfTextGo.AddComponent<TextMeshProUGUI>();
            _perfectionPrefab.AddComponent<TraitTooltipTrigger>();

            _imperfectionPrefab = new GameObject("ImperfectionPrefab");
            var imperfTextGo = new GameObject("Text");
            imperfTextGo.transform.SetParent(_imperfectionPrefab.transform);
            imperfTextGo.AddComponent<TextMeshProUGUI>();
            _imperfectionPrefab.AddComponent<TraitTooltipTrigger>();

            // Inject private fields via Reflection
            typeof(DollmakerUIController).GetField("marionetteButtons", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _marionetteButtons);
            typeof(DollmakerUIController).GetField("perfectionsContainer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _perfectionsContainer);
            typeof(DollmakerUIController).GetField("imperfectionsContainer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _imperfectionsContainer);
            typeof(DollmakerUIController).GetField("perfectionPrefab", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _perfectionPrefab);
            typeof(DollmakerUIController).GetField("imperfectionPrefab", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _imperfectionPrefab);

            // Mock Data
            _mockCharacter1 = ScriptableObject.CreateInstance<CharacterData>();
            _mockCharacter1.displayName = "MockChar1";

            _mockCharacter2 = ScriptableObject.CreateInstance<CharacterData>();
            _mockCharacter2.displayName = "MockChar2";

            _mockPerfection = ScriptableObject.CreateInstance<TraitData>();
            _mockPerfection.displayName = "Test Perfection";
            _mockPerfection.traitType = TraitType.Perfection;

            _mockImperfection = ScriptableObject.CreateInstance<TraitData>();
            _mockImperfection.displayName = "Test Imperfection";
            _mockImperfection.traitType = TraitType.Imperfection;
        }

        [TearDown]
        public void TearDown()
        {
            if (_uiRoot != null) Object.DestroyImmediate(_uiRoot);
            if (_perfectionPrefab != null) Object.DestroyImmediate(_perfectionPrefab);
            if (_imperfectionPrefab != null) Object.DestroyImmediate(_imperfectionPrefab);

            if (_mockCharacter1 != null) Object.DestroyImmediate(_mockCharacter1);
            if (_mockCharacter2 != null) Object.DestroyImmediate(_mockCharacter2);
            if (_mockPerfection != null) Object.DestroyImmediate(_mockPerfection);
            if (_mockImperfection != null) Object.DestroyImmediate(_mockImperfection);
        }

        [Test]
        public void Initialize_ConfiguresMarionetteButtons_ForCurrentParty()
        {
            var party = new List<PartyMemberInfo>
            {
                new PartyMemberInfo { character = _mockCharacter1 },
                new PartyMemberInfo { character = _mockCharacter2 }
            };

            _controller.Initialize(10, 3, party);

            // Buttons 0 and 1 should be active
            Assert.IsTrue(_marionetteButtons[0].interactable, "Button 0 should be interactable.");
            Assert.AreEqual("MockChar1", _marionetteButtons[0].GetComponentInChildren<TextMeshProUGUI>().text);
            
            Assert.IsTrue(_marionetteButtons[1].interactable, "Button 1 should be interactable.");
            Assert.AreEqual("MockChar2", _marionetteButtons[1].GetComponentInChildren<TextMeshProUGUI>().text);

            // Buttons 2 and 3 should be inactive
            Assert.IsFalse(_marionetteButtons[2].interactable, "Button 2 should not be interactable.");
            Assert.AreEqual(string.Empty, _marionetteButtons[2].GetComponentInChildren<TextMeshProUGUI>().text);

            Assert.IsFalse(_marionetteButtons[3].interactable, "Button 3 should not be interactable.");
            Assert.AreEqual(string.Empty, _marionetteButtons[3].GetComponentInChildren<TextMeshProUGUI>().text);
        }

        [Test]
        public void SelectMarionette_InstantiatesPerfectionAndImperfectionPrefabs()
        {
            var party = new List<PartyMemberInfo>
            {
                new PartyMemberInfo 
                { 
                    character = _mockCharacter1,
                    perfections = new List<TraitData> { _mockPerfection },
                    imperfections = new List<TraitData> { _mockImperfection }
                }
            };

            // Initialize auto-selects index 0
            _controller.Initialize(10, 3, party);

            Assert.AreEqual(1, _perfectionsContainer.childCount, "One perfection should be instantiated.");
            Assert.AreEqual("- Test Perfection", _perfectionsContainer.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text);

            Assert.AreEqual(1, _imperfectionsContainer.childCount, "One imperfection should be instantiated.");
            Assert.AreEqual("- Test Imperfection", _imperfectionsContainer.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text);
        }

        [Test]
        public void SelectMarionette_ClearsPreviousContainers()
        {
            var party = new List<PartyMemberInfo>
            {
                new PartyMemberInfo 
                { 
                    character = _mockCharacter1,
                    perfections = new List<TraitData> { _mockPerfection },
                    imperfections = new List<TraitData>()
                },
                new PartyMemberInfo 
                { 
                    character = _mockCharacter2,
                    perfections = new List<TraitData>(),
                    imperfections = new List<TraitData> { _mockImperfection }
                }
            };

            _controller.Initialize(10, 3, party);
            
            // By default, member 0 is selected
            Assert.AreEqual(1, _perfectionsContainer.childCount);
            Assert.AreEqual(0, _imperfectionsContainer.childCount);

            // Select member 1
            _controller.SelectMarionette(1);

            Assert.AreEqual(0, _perfectionsContainer.childCount, "Perfections should be cleared for member 1.");
            Assert.AreEqual(1, _imperfectionsContainer.childCount, "Imperfections should be populated for member 1.");
        }

        [Test]
        public void DollmakerRoomEffectStrategy_ExecuteRoomEffect_SpawnsUIAndInitializes()
        {
            // Setup canvas
            var canvasGo = new GameObject("UICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var strategy = new DollmakerRoomEffectStrategy();
            
            // Inject strategy fields
            typeof(DollmakerRoomEffectStrategy).GetField("partCost", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(strategy, 20);
            typeof(DollmakerRoomEffectStrategy).GetField("perfectionReplacementOptionsCount", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(strategy, 2);
            typeof(DollmakerRoomEffectStrategy).GetField("dollmakerUiPrefab", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(strategy, _uiRoot);

            // Execute
            strategy.ExecuteRoomEffect();

            // Verification
            // It should instantiate a clone of _uiRoot in the Canvas
            Assert.AreEqual(1, canvas.transform.childCount, "UI should be instantiated under canvas.");
            
            var instance = canvas.transform.GetChild(0).gameObject;
            var controllerInstance = instance.GetComponent<DollmakerUIController>();
            
            Assert.IsNotNull(controllerInstance, "Controller should exist on instance.");

            // Verify initialization values (via reflection)
            int partCost = (int)typeof(DollmakerUIController).GetField("_partCost", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controllerInstance);
            int optionsCount = (int)typeof(DollmakerUIController).GetField("_perfectionReplacementOptions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controllerInstance);

            Assert.AreEqual(20, partCost);
            Assert.AreEqual(2, optionsCount);

            Object.DestroyImmediate(canvasGo);
        }
    }
}
