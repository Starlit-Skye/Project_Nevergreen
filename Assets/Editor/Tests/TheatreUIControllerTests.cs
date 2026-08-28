using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.UI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TheatreUIControllerTests
    {
        private GameObject _controllerGo;
        private TheatreUIController _controller;
        private GameObject _buttonGo;
        private GameObject _textGo;
        private TextMeshProUGUI _textMesh;
        private GlobalConfig _globalConfig;
        private GameDatabase _gameDatabase;

        [SetUp]
        public void Setup()
        {
            _globalConfig = ScriptableObject.CreateInstance<GlobalConfig>();
            _globalConfig.theatreRoomProjectorRepairCost = 75;

            _gameDatabase = GameDatabase.CreateForTesting(globalCfg: _globalConfig);
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            _controllerGo = new GameObject("TheatreUIController");
            _controller = _controllerGo.AddComponent<TheatreUIController>();

            _buttonGo = new GameObject("FixProjectorButton");
            _buttonGo.AddComponent<Button>();
            _textGo = new GameObject("ButtonText");
            _textGo.transform.SetParent(_buttonGo.transform);
            _textMesh = _textGo.AddComponent<TextMeshProUGUI>();

            _controller.fixProjectorButton = _buttonGo;
            
            _controller.skillSelectionPanel = new GameObject("SkillSelectionPanel");
            _controller.skillsContainer = new GameObject("SkillsContainer").transform;
            _controller.confirmButton = new GameObject("ConfirmButton").AddComponent<Button>();
            
            _controller.marionetteButtons = new Button[4];
            _controller.marionetteButtonTexts = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                var mbGo = new GameObject($"MB{i}");
                _controller.marionetteButtons[i] = mbGo.AddComponent<Button>();
                var mbtGo = new GameObject($"MBT{i}");
                mbtGo.transform.SetParent(mbGo.transform);
                _controller.marionetteButtonTexts[i] = mbtGo.AddComponent<TextMeshProUGUI>();
            }

            var skillItemPrefabGo = new GameObject("SkillItemPrefab");
            skillItemPrefabGo.AddComponent<Button>();
            skillItemPrefabGo.AddComponent<Image>();
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(skillItemPrefabGo.transform);
            labelGo.AddComponent<TextMeshProUGUI>();
            _controller.skillListItemPrefab = skillItemPrefabGo;

            // Invoke OnEnable via reflection to ensure listeners are hooked up in EditMode
            var onEnableMethod = typeof(TheatreUIController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnableMethod?.Invoke(_controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            GameDatabase.SetInstanceForTesting(null);

            if (_controllerGo != null)
                Object.DestroyImmediate(_controllerGo);
            if (_buttonGo != null)
                Object.DestroyImmediate(_buttonGo);
            if (_gameDatabase != null)
                Object.DestroyImmediate(_gameDatabase);
            if (_globalConfig != null)
                Object.DestroyImmediate(_globalConfig);
        }

        [Test]
        public void GlobalConfig_TheatreRoomProjectorRepairCost_DefaultIs50()
        {
            var config = ScriptableObject.CreateInstance<GlobalConfig>();
            Assert.AreEqual(50, config.theatreRoomProjectorRepairCost);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void UpdateDisplay_SetsButtonTextUsingGlobalConfig()
        {
            _controller.UpdateDisplay();
            Assert.AreEqual("Spend 75 Scraps, unlock a Marionette's skill.", _textMesh.text);
        }

        [Test]
        public void UpdateDisplay_WithExplicitCost_SetsButtonText()
        {
            _controller.UpdateDisplay(120);
            Assert.AreEqual("Spend 120 Scraps, unlock a Marionette's skill.", _textMesh.text);
        }

        [Test]
        public void UpdateDisplay_NullButton_DoesNotThrow()
        {
            _controller.fixProjectorButton = null;
            Assert.DoesNotThrow(() => _controller.UpdateDisplay());
        }

        [Test]
        public void UpdateDisplay_EnoughScraps_ButtonIsInteractable()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);
            _controller.UpdateDisplay(75);
            Assert.IsTrue(_buttonGo.GetComponent<Button>().interactable);
        }

        [Test]
        public void UpdateDisplay_NotEnoughScraps_ButtonIsNotInteractable()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(50);
            _controller.UpdateDisplay(75);
            Assert.IsFalse(_buttonGo.GetComponent<Button>().interactable);
        }

        [Test]
        public void OnRepairButtonClicked_SpendsScraps()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);
            
            _controller.OnRepairButtonClicked();

            Assert.AreEqual(25, RunSessionManager.Scraps);
        }

        [Test]
        public void ButtonClick_InvokesRepairAndSpendsScraps()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);

            // Update display so the button becomes interactable again
            _controller.UpdateDisplay();

            // Invoke the button's onClick event
            var button = _buttonGo.GetComponent<Button>();
            if (button.interactable)
            {
                button.onClick.Invoke();
            }

            Assert.AreEqual(25, RunSessionManager.Scraps);
        }

        [Test]
        public void PartyMemberInfo_UnlockedSkills_DefaultIsEmpty()
        {
            var info = new PartyMemberInfo();
            Assert.IsNotNull(info.unlockedSkills);
            Assert.AreEqual(0, info.unlockedSkills.Count);
        }

        [Test]
        public void OnRepairButtonClicked_ShowsSkillSelectionPanelAndPopulatesMarionettes()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);

            var character1 = ScriptableObject.CreateInstance<CharacterData>();
            character1.displayName = "TestMarionette1";
            var character2 = ScriptableObject.CreateInstance<CharacterData>();
            character2.displayName = "TestMarionette2";

            RunSessionManager.CurrentParty.Add(new PartyMemberInfo { character = character1 });
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo { character = character2 });

            _controller.skillSelectionPanel.SetActive(false);

            _controller.OnRepairButtonClicked();

            Assert.IsTrue(_controller.skillSelectionPanel.activeSelf);
            Assert.AreEqual("TestMarionette1", _controller.marionetteButtonTexts[0].text);
            Assert.IsTrue(_controller.marionetteButtons[0].interactable);
            Assert.AreEqual("TestMarionette2", _controller.marionetteButtonTexts[1].text);
            Assert.IsTrue(_controller.marionetteButtons[1].interactable);
            
            // Unused slots
            Assert.AreEqual("", _controller.marionetteButtonTexts[2].text);
            Assert.IsFalse(_controller.marionetteButtons[2].interactable);
            Assert.AreEqual("", _controller.marionetteButtonTexts[3].text);
            Assert.IsFalse(_controller.marionetteButtons[3].interactable);

            // Confirm button should be disabled initially
            Assert.IsFalse(_controller.confirmButton.interactable);
        }

        [Test]
        public void OnMarionetteSelected_PopulatesSkills()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);

            var skill1 = ScriptableObject.CreateInstance<SkillData>();
            skill1.skillId = "skill_1";
            skill1.displayName = "Skill 1";
            
            var skill2 = ScriptableObject.CreateInstance<SkillData>();
            skill2.skillId = "skill_2";
            skill2.displayName = "Skill 2";

            var character = ScriptableObject.CreateInstance<CharacterData>();
            character.totalSkillPool = new List<SkillData> { skill1, skill2 };
            var member = new PartyMemberInfo { character = character };
            
            // Add a skill to unlockedSkills
            member.unlockedSkills.Add(skill1);

            RunSessionManager.CurrentParty.Add(member);

            _controller.OnRepairButtonClicked(); // Setup

            // Simulate clicking first marionette
            _controller.marionetteButtons[0].onClick.Invoke();

            // Verify skills populated
            Assert.AreEqual(2, _controller.skillsContainer.childCount);
            
            var item1 = _controller.skillsContainer.GetChild(0).gameObject;
            var item2 = _controller.skillsContainer.GetChild(1).gameObject;

            Assert.AreEqual("Skill 1", item1.GetComponentInChildren<TextMeshProUGUI>().text);
            Assert.AreEqual("Skill 2", item2.GetComponentInChildren<TextMeshProUGUI>().text);

            // Skill 1 is already unlocked, should be non-interactable
            Assert.IsFalse(item1.GetComponent<Button>().interactable);
            // Skill 2 is not unlocked, should be interactable
            Assert.IsTrue(item2.GetComponent<Button>().interactable);
        }

        [Test]
        public void OnSkillClicked_EnablesConfirmButton()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);

            var skill1 = ScriptableObject.CreateInstance<SkillData>();
            skill1.skillId = "skill_1";
            var character = ScriptableObject.CreateInstance<CharacterData>();
            character.totalSkillPool = new List<SkillData> { skill1 };
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo { character = character });

            _controller.OnRepairButtonClicked();
            _controller.marionetteButtons[0].onClick.Invoke();

            var item1 = _controller.skillsContainer.GetChild(0).gameObject;
            
            Assert.IsFalse(_controller.confirmButton.interactable);
            
            item1.GetComponent<Button>().onClick.Invoke();
            
            Assert.IsTrue(_controller.confirmButton.interactable);
        }

        [Test]
        public void OnConfirmClicked_AddsSkillToUnlockedSkillsAndCompletesRoom()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);

            var skill1 = ScriptableObject.CreateInstance<SkillData>();
            skill1.skillId = "skill_1";
            var character = ScriptableObject.CreateInstance<CharacterData>();
            character.totalSkillPool = new List<SkillData> { skill1 };
            var member = new PartyMemberInfo { character = character };
            RunSessionManager.CurrentParty.Add(member);

            _controller.OnRepairButtonClicked();
            _controller.marionetteButtons[0].onClick.Invoke();
            
            var item1 = _controller.skillsContainer.GetChild(0).gameObject;
            item1.GetComponent<Button>().onClick.Invoke(); // Select skill
            
            _controller.confirmButton.onClick.Invoke(); // Confirm

            Assert.IsTrue(member.unlockedSkills.Contains(skill1));
            Assert.IsFalse(member.equippedSkills.Contains(skill1));
            Assert.IsFalse(_controller.skillSelectionPanel.activeSelf);
            Assert.IsFalse(_controller.gameObject.activeSelf);
            Assert.IsTrue(RunSessionManager.RoomCompleted);
        }
    }
}
