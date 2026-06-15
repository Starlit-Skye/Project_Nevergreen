using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.UI;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class MarionetteSelectionControllerTests
    {
        private GameObject _controllerGo;
        private MarionetteSelectionController _controller;
        
        private MarionetteDatabase _marionetteDb;
        private TraitDatabase _traitDb;
        private CombatConfig _combatConfig;
        private GameDatabase _gameDatabase;

        private CharacterData _charCecilia;
        private CharacterData _charOther;

        private List<GameObject> _createdObjects;

        [SetUp]
        public void Setup()
        {
            RunSessionManager.Clear();
            _createdObjects = new List<GameObject>();

            // Create databases & config
            _marionetteDb = ScriptableObject.CreateInstance<MarionetteDatabase>();
            _marionetteDb.marionettes = new List<CharacterData>();

            _traitDb = ScriptableObject.CreateInstance<TraitDatabase>();
            _traitDb.perfections = new List<TraitData>();
            _traitDb.imperfections = new List<TraitData>();

            _combatConfig = ScriptableObject.CreateInstance<CombatConfig>();
            _combatConfig.marionetteChoiceCount = 2;
            _combatConfig.maxPerfections = 1;
            _combatConfig.maxImperfections = 1;

            // Stats
            var stats = CombatTestHelper.CreateStatBlock();

            // Create character templates
            _charCecilia = CombatTestHelper.CreateCharacterData("ceci", "Cecilia", stats, CharacterTeamType.Player);
            _charCecilia.availableSkills = new List<SkillData>();
            _charCecilia.totalSkillPool = new List<SkillData>();
            _marionetteDb.marionettes.Add(_charCecilia);

            _charOther = CombatTestHelper.CreateCharacterData("other", "Other Hero", stats, CharacterTeamType.Player);
            _charOther.availableSkills = new List<SkillData>();
            _charOther.totalSkillPool = new List<SkillData>();
            _marionetteDb.marionettes.Add(_charOther);

            // Create GameObject & Controller
            // Inject mock GameDatabase
            _gameDatabase = GameDatabase.CreateForTesting(
                marionettes: _marionetteDb,
                traits: _traitDb
            );
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            _controllerGo = new GameObject("MarionetteSelectionController");
            _createdObjects.Add(_controllerGo);
            _controller = _controllerGo.AddComponent<MarionetteSelectionController>();

            _controller.combatConfig = _combatConfig;
            _controller.combatSceneName = ""; // Avoid actually loading scenes in EditMode tests

            // Setup mock UI elements
            var containerGo = new GameObject("ChoicesContainer");
            _createdObjects.Add(containerGo);

            var choiceGo = new GameObject("ChoiceButtonTemplate");
            _createdObjects.Add(choiceGo);
            choiceGo.transform.SetParent(containerGo.transform);
            var choiceBtn = choiceGo.AddComponent<Button>();
            var choiceImage = choiceGo.AddComponent<Image>();
            var choiceTxtGo = new GameObject("ChoiceText");
            _createdObjects.Add(choiceTxtGo);
            choiceTxtGo.transform.SetParent(choiceGo.transform);
            var choiceTxt = choiceTxtGo.AddComponent<TextMeshProUGUI>();

            _controller.choiceButtons = new Button[] { choiceBtn };
            _controller.choiceTexts = new TextMeshProUGUI[] { choiceTxt };

            // Setup Party Buttons
            _controller.partyMemberButtons = new Button[4];
            _controller.partyMemberTexts = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                var partyGo = new GameObject($"PartyMemberButton_{i}");
                _createdObjects.Add(partyGo);
                var btn = partyGo.AddComponent<Button>();
                var img = partyGo.AddComponent<Image>();
                var txtGo = new GameObject("Text");
                _createdObjects.Add(txtGo);
                txtGo.transform.SetParent(partyGo.transform);
                var txt = txtGo.AddComponent<TextMeshProUGUI>();

                _controller.partyMemberButtons[i] = btn;
                _controller.partyMemberTexts[i] = txt;
            }

            var confirmGo = new GameObject("ConfirmButton");
            _createdObjects.Add(confirmGo);
            _controller.confirmButton = confirmGo.AddComponent<Button>();
            confirmGo.AddComponent<Image>();
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.Clear();
            GameDatabase.SetInstanceForTesting(null);

            foreach (var go in _createdObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            ScriptableObject.DestroyImmediate(_marionetteDb);
            ScriptableObject.DestroyImmediate(_traitDb);
            ScriptableObject.DestroyImmediate(_combatConfig);
            ScriptableObject.DestroyImmediate(_charCecilia);
            ScriptableObject.DestroyImmediate(_charOther);
            if (_gameDatabase != null) ScriptableObject.DestroyImmediate(_gameDatabase);
        }

        [Test]
        public void InitializePartyMembers_CeciliaPartyMember_CeciliaSlotIsUninteractable()
        {
            // Arrange: Setup Cecilia in party index 0
            RunSessionManager.CurrentParty = new List<PartyMemberInfo>
            {
                new PartyMemberInfo { character = _charCecilia, equippedSkills = new List<SkillData>() }
            };

            // Act: Start controller
            // Call Start/Awake manually via reflection or just call the methods directly
            var startMethod = typeof(MarionetteSelectionController).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(_controller, null);

            // Assert: index 0 (Cecilia) should be uninteractable
            Assert.IsFalse(_controller.partyMemberButtons[0].interactable, "Cecilia's party button slot should be uninteractable.");
            Assert.AreEqual("Cecilia", _controller.partyMemberTexts[0].text);

            // Check index 1 (Empty Slot) is interactable
            Assert.IsTrue(_controller.partyMemberButtons[1].interactable, "Empty party button slot should be interactable.");
            Assert.AreEqual("Empty Slot", _controller.partyMemberTexts[1].text);
        }

        [Test]
        public void InitializePartyMembers_OtherPartyMember_SlotIsInteractable()
        {
            // Arrange: Setup Other Hero in party index 0
            RunSessionManager.CurrentParty = new List<PartyMemberInfo>
            {
                new PartyMemberInfo { character = _charOther, equippedSkills = new List<SkillData>() }
            };

            // Act
            var startMethod = typeof(MarionetteSelectionController).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(_controller, null);

            // Assert
            Assert.IsTrue(_controller.partyMemberButtons[0].interactable, "Other hero's party button slot should be interactable.");
            Assert.AreEqual("Other Hero", _controller.partyMemberTexts[0].text);
        }

        [Test]
        public void OnPartyMemberClicked_CeciliaClickedProgrammatically_DoesNotSelectCecilia()
        {
            // Arrange
            RunSessionManager.CurrentParty = new List<PartyMemberInfo>
            {
                new PartyMemberInfo { character = _charCecilia, equippedSkills = new List<SkillData>() }
            };

            var startMethod = typeof(MarionetteSelectionController).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(_controller, null);

            // Act: programmatically click Cecilia slot
            var onPartyMemberClickedMethod = typeof(MarionetteSelectionController).GetMethod("OnPartyMemberClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onPartyMemberClickedMethod.Invoke(_controller, new object[] { 0 });

            // Assert: selected party member index remains -1 (or doesn't change to 0)
            var selectedPartyMemberIndexField = typeof(MarionetteSelectionController).GetField("_selectedPartyMemberIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int selectedIndex = (int)selectedPartyMemberIndexField.GetValue(_controller);

            Assert.AreEqual(-1, selectedIndex, "Should not be able to select Cecilia.");
        }

        [Test]
        public void OnConfirmClicked_CeciliaSelectedProgrammatically_DoesNotSwapCecilia()
        {
            // Arrange: Force selection of Cecilia programmatically to test ultimate safety guard
            RunSessionManager.CurrentParty = new List<PartyMemberInfo>
            {
                new PartyMemberInfo { character = _charCecilia, equippedSkills = new List<SkillData>() }
            };

            var startMethod = typeof(MarionetteSelectionController).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(_controller, null);

            // Force selected party member to 0 (Cecilia) via reflection
            var selectedPartyMemberIndexField = typeof(MarionetteSelectionController).GetField("_selectedPartyMemberIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectedPartyMemberIndexField.SetValue(_controller, 0);

            // Select choice 0
            var selectedChoiceIndexField = typeof(MarionetteSelectionController).GetField("_selectedChoiceIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectedChoiceIndexField.SetValue(_controller, 0);

            // Act: Confirm
            var onConfirmClickedMethod = typeof(MarionetteSelectionController).GetMethod("OnConfirmClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onConfirmClickedMethod.Invoke(_controller, null);

            // Assert: Cecilia is still in the party
            Assert.AreEqual(_charCecilia, RunSessionManager.CurrentParty[0].character, "Cecilia should not have been swapped out.");
        }
    }
}
