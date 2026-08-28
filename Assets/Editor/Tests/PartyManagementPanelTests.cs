using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Nevergreen.Data;
using Nevergreen.UI;
using TMPro;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class PartyManagementPanelTests
    {
        private GameObject panelGo;
        private PartyManagementPanelController controller;
        private Button upgradeButton;
        private CharacterData dummyCharacter;
        private CharacterData dummyCharacter2;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();
            var combatConfig = GameDatabase.Instance.CombatConfig;
            combatConfig.globalMaxLevel = 10;
            combatConfig.levelUpCostCurve = new List<int> { 10, 20, 30, 40, 50, 60, 70, 80, 90 };

            // Create Panel
            panelGo = new GameObject("PartyManagementPanel");
            controller = panelGo.AddComponent<PartyManagementPanelController>();

            // Create Upgrade Button
            var buttonGo = new GameObject("UpgradeButton");
            upgradeButton = buttonGo.AddComponent<Button>();
            controller.upgradeButton = upgradeButton;

            // Setup required fields to avoid NRE
            controller.partyMemberButtons = new Button[4];
            controller.partyMemberNames = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                var btnGo = new GameObject($"Slot{i}");
                var img = btnGo.AddComponent<Image>();
                img.color = Color.white;
                controller.partyMemberButtons[i] = btnGo.AddComponent<Button>();
                
                var nameTextGo = new GameObject($"NameText{i}");
                controller.partyMemberNames[i] = nameTextGo.AddComponent<TextMeshProUGUI>();
            }

            var moveBtnGo = new GameObject("MoveButton");
            var moveImg = moveBtnGo.AddComponent<Image>();
            moveImg.color = Color.white;
            controller.moveButton = moveBtnGo.AddComponent<Button>();

            controller.nameAndLevelText = new GameObject("NameText").AddComponent<TextMeshProUGUI>();
            controller.levelUpCostText = new GameObject("CostText").AddComponent<TextMeshProUGUI>();
            controller.coreStatsText = new GameObject("CoreStats").AddComponent<TextMeshProUGUI>();
            controller.resText = new GameObject("ResText").AddComponent<TextMeshProUGUI>();
            controller.perfectionsContainer = new GameObject("PerfectionsContainer").AddComponent<RectTransform>();
            controller.imperfectionsContainer = new GameObject("ImperfectionsContainer").AddComponent<RectTransform>();
            
            // Create dummy trait item prefab
            controller.perfectionUIItemPrefab = new GameObject("PerfectionItemPrefab");
            controller.perfectionUIItemPrefab.AddComponent<TextMeshProUGUI>();
            controller.perfectionUIItemPrefab.AddComponent<RectTransform>();

            controller.imperfectionUIItemPrefab = new GameObject("ImperfectionItemPrefab");
            controller.imperfectionUIItemPrefab.AddComponent<TextMeshProUGUI>();
            controller.imperfectionUIItemPrefab.AddComponent<RectTransform>();

            controller.skillsContainer = new GameObject("SkillsContainer").AddComponent<RectTransform>();
            
            // Create dummy skill item prefab
            controller.skillListItemPrefab = new GameObject("SkillItemPrefab");
            controller.skillListItemPrefab.AddComponent<RectTransform>();
            controller.skillListItemPrefab.AddComponent<Image>();
            controller.skillListItemPrefab.AddComponent<Button>();
            
            var skillLabelGo = new GameObject("Label");
            skillLabelGo.transform.SetParent(controller.skillListItemPrefab.transform);
            skillLabelGo.AddComponent<TextMeshProUGUI>();
            // Create a dummy character using helper
            var stats = CombatTestHelper.CreateStatBlock();
            dummyCharacter = CombatTestHelper.CreateCharacterData("dummy_char", "Dummy", stats);

            var stats2 = CombatTestHelper.CreateStatBlock();
            dummyCharacter2 = CombatTestHelper.CreateCharacterData("dummy_char_2", "Dummy2", stats2);

            RunSessionManager.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.Clear();
            CombatTestHelper.CleanupTestDatabase();

            if (dummyCharacter != null) Object.DestroyImmediate(dummyCharacter);
            if (dummyCharacter2 != null) Object.DestroyImmediate(dummyCharacter2);
            if (panelGo != null) Object.DestroyImmediate(panelGo);
            if (upgradeButton != null) Object.DestroyImmediate(upgradeButton.gameObject);
        }

        [Test]
        public void UpgradeButton_IsInteractable_WhenBelowMaxLevel()
        {
            // Arrange
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo
            {
                character = dummyCharacter,
                currentLevel = 1
            });
            RunSessionManager.Parts = 100;
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);

            var selectMethod = typeof(PartyManagementPanelController).GetMethod("OnSlotClicked", BindingFlags.NonPublic | BindingFlags.Instance);
            selectMethod.Invoke(controller, new object[] { 0 });

            // Assert
            Assert.IsTrue(upgradeButton.interactable, "Upgrade button should be interactable if below max level.");
        }

        [Test]
        public void UpgradeButton_IsNotInteractable_WhenAtMaxLevel()
        {
            // Arrange
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo
            {
                character = dummyCharacter,
                currentLevel = 10 // Max level
            });
            RunSessionManager.Parts = 100;
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);

            var selectMethod = typeof(PartyManagementPanelController).GetMethod("OnSlotClicked", BindingFlags.NonPublic | BindingFlags.Instance);
            selectMethod.Invoke(controller, new object[] { 0 });

            // Assert
            Assert.IsFalse(upgradeButton.interactable, "Upgrade button should not be interactable at max level.");
        }

        [Test]
        public void OnUpgradeClicked_NotEnoughParts_DoesNotUpgrade()
        {
            // Arrange
            var memberInfo = new PartyMemberInfo
            {
                character = dummyCharacter,
                currentLevel = 1
            };
            RunSessionManager.CurrentParty.Add(memberInfo);
            RunSessionManager.Parts = 5; // Cost is 10
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);

            var selectMethod = typeof(PartyManagementPanelController).GetMethod("OnSlotClicked", BindingFlags.NonPublic | BindingFlags.Instance);
            selectMethod.Invoke(controller, new object[] { 0 });

            // Act
            upgradeButton.onClick.Invoke();

            // Assert
            Assert.AreEqual(1, memberInfo.currentLevel, "Level should not increment without enough parts.");
            Assert.AreEqual(5, RunSessionManager.Parts, "Parts should not be deducted.");
        }

        [Test]
        public void OnUpgradeClicked_EnoughParts_UpgradesAndDeductsParts()
        {
            // Arrange
            var memberInfo = new PartyMemberInfo
            {
                character = dummyCharacter,
                currentLevel = 1
            };
            RunSessionManager.CurrentParty.Add(memberInfo);
            RunSessionManager.Parts = 15; // Cost is 10
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);

            var selectMethod = typeof(PartyManagementPanelController).GetMethod("OnSlotClicked", BindingFlags.NonPublic | BindingFlags.Instance);
            selectMethod.Invoke(controller, new object[] { 0 });

            // Act
            upgradeButton.onClick.Invoke();

            // Assert
            Assert.AreEqual(2, memberInfo.currentLevel, "Level should increment by 1.");
            Assert.AreEqual(5, RunSessionManager.Parts, "Cost (10) should be deducted from Parts (15).");
        }

        [Test]
        public void MoveButton_TogglesMoveMode_AndHighlightsOtherSlots()
        {
            // Arrange
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo { character = dummyCharacter, currentLevel = 1 });
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo { character = dummyCharacter2, currentLevel = 1 });
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);

            var enableMethod = typeof(PartyManagementPanelController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            enableMethod.Invoke(controller, null); // Selects slot 0

            var moveButton = controller.moveButton;
            var slot1Image = controller.partyMemberButtons[1].GetComponent<Image>();
            var originalColor = slot1Image.color;

            // Act
            moveButton.onClick.Invoke();

            // Assert
            Assert.AreEqual(controller.highlightColor, slot1Image.color, "Other slot should be highlighted.");
            Assert.AreNotEqual(originalColor, slot1Image.color);
        }

        [Test]
        public void MoveMode_ClickingHighlightedSlot_SwapsCharacters_AndSaves()
        {
            // Arrange
            var char1 = new PartyMemberInfo { character = dummyCharacter, currentLevel = 1 };
            var char2 = new PartyMemberInfo { character = dummyCharacter2, currentLevel = 1 };
            RunSessionManager.CurrentParty.Add(char1);
            RunSessionManager.CurrentParty.Add(char2);
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);
            var enableMethod = typeof(PartyManagementPanelController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            enableMethod.Invoke(controller, null); // Selects slot 0

            var moveButton = controller.moveButton;

            // Act
            moveButton.onClick.Invoke(); // Toggle move mode on
            controller.partyMemberButtons[1].onClick.Invoke(); // Click slot 1

            // Assert
            Assert.AreEqual(char2, RunSessionManager.CurrentParty[0], "Slot 0 should now be character 2.");
            Assert.AreEqual(char1, RunSessionManager.CurrentParty[1], "Slot 1 should now be character 1.");
            
            var slot1Image = controller.partyMemberButtons[1].GetComponent<Image>();
            Assert.AreNotEqual(controller.highlightColor, slot1Image.color, "Highlight should be removed.");
        }

        [Test]
        public void MoveMode_ClickingSelectedSlot_CancelsMoveMode()
        {
            // Arrange
            var char1 = new PartyMemberInfo { character = dummyCharacter, currentLevel = 1 };
            var char2 = new PartyMemberInfo { character = dummyCharacter2, currentLevel = 1 };
            RunSessionManager.CurrentParty.Add(char1);
            RunSessionManager.CurrentParty.Add(char2);
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);
            var enableMethod = typeof(PartyManagementPanelController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            enableMethod.Invoke(controller, null); // Selects slot 0

            var moveButton = controller.moveButton;

            // Act
            moveButton.onClick.Invoke(); // Toggle move mode on
            controller.partyMemberButtons[0].onClick.Invoke(); // Click slot 0 (selected slot)

            // Assert
            Assert.AreEqual(char1, RunSessionManager.CurrentParty[0], "Party should not swap.");
            
            var slot1Image = controller.partyMemberButtons[1].GetComponent<Image>();
            Assert.AreNotEqual(controller.highlightColor, slot1Image.color, "Highlight should be removed after cancel.");
        }

        [Test]
        public void ClickingEquippedSkill_UnequipsSkillAndSaves()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            dummyCharacter.totalSkillPool = new List<SkillData> { skill };
            
            var char1 = new PartyMemberInfo { character = dummyCharacter, currentLevel = 1 };
            char1.unlockedSkills.Add(skill);
            char1.equippedSkills.Add(skill);
            RunSessionManager.CurrentParty.Add(char1);
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);
            var enableMethod = typeof(PartyManagementPanelController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            enableMethod.Invoke(controller, null); 

            var skillButton = controller.skillsContainer.GetChild(0).GetComponent<Button>();
            Assert.IsTrue(skillButton.interactable);
            
            skillButton.onClick.Invoke();

            Assert.IsFalse(char1.equippedSkills.Contains(skill));
        }

        [Test]
        public void ClickingUnlockedSkill_EquipsSkillWhenUnderCap()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            dummyCharacter.totalSkillPool = new List<SkillData> { skill };
            
            var char1 = new PartyMemberInfo { character = dummyCharacter, currentLevel = 1 };
            char1.unlockedSkills.Add(skill);
            // Not in equippedSkills
            RunSessionManager.CurrentParty.Add(char1);
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);
            var enableMethod = typeof(PartyManagementPanelController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            enableMethod.Invoke(controller, null); 

            var skillButton = controller.skillsContainer.GetChild(0).GetComponent<Button>();
            Assert.IsTrue(skillButton.interactable);
            
            skillButton.onClick.Invoke();

            Assert.IsTrue(char1.equippedSkills.Contains(skill));
        }

        [Test]
        public void ClickingUnlockedSkill_DoesNotEquipWhenAtCap()
        {
            var skill1 = ScriptableObject.CreateInstance<SkillData>();
            var skill2 = ScriptableObject.CreateInstance<SkillData>();
            var skill3 = ScriptableObject.CreateInstance<SkillData>();
            var skill4 = ScriptableObject.CreateInstance<SkillData>();
            var skill5 = ScriptableObject.CreateInstance<SkillData>();
            
            dummyCharacter.totalSkillPool = new List<SkillData> { skill1, skill2, skill3, skill4, skill5 };
            
            var char1 = new PartyMemberInfo { character = dummyCharacter, currentLevel = 1 };
            char1.unlockedSkills.AddRange(new[] { skill1, skill2, skill3, skill4, skill5 });
            char1.equippedSkills.AddRange(new[] { skill1, skill2, skill3, skill4 }); // Cap is 4
            RunSessionManager.CurrentParty.Add(char1);
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);
            var enableMethod = typeof(PartyManagementPanelController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            enableMethod.Invoke(controller, null); 

            var skill5Button = controller.skillsContainer.GetChild(4).GetComponent<Button>(); // Index 4 is skill5
            Assert.IsTrue(skill5Button.interactable);
            
            skill5Button.onClick.Invoke();

            Assert.IsFalse(char1.equippedSkills.Contains(skill5));
            Assert.AreEqual(4, char1.equippedSkills.Count);
        }

        [Test]
        public void LockedSkill_IsUninteractable()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            dummyCharacter.totalSkillPool = new List<SkillData> { skill };
            
            var char1 = new PartyMemberInfo { character = dummyCharacter, currentLevel = 1 };
            // Not in unlockedSkills
            RunSessionManager.CurrentParty.Add(char1);
            
            var startMethod = typeof(PartyManagementPanelController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(controller, null);
            var enableMethod = typeof(PartyManagementPanelController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            enableMethod.Invoke(controller, null); 

            var skillButton = controller.skillsContainer.GetChild(0).GetComponent<Button>();
            Assert.IsFalse(skillButton.interactable);
        }
    }
}
