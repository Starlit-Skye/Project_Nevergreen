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
            controller.nameAndLevelText = new GameObject("NameText").AddComponent<TextMeshProUGUI>();
            controller.levelUpCostText = new GameObject("CostText").AddComponent<TextMeshProUGUI>();
            controller.coreStatsText = new GameObject("CoreStats").AddComponent<TextMeshProUGUI>();
            controller.resText = new GameObject("ResText").AddComponent<TextMeshProUGUI>();
            controller.perfectionsText = new GameObject("Perfections").AddComponent<TextMeshProUGUI>();
            controller.imperfectionsText = new GameObject("Imperfections").AddComponent<TextMeshProUGUI>();
            controller.skillsContainer = new GameObject("SkillsContainer").AddComponent<RectTransform>();
            
            // Create a dummy character using helper
            var stats = CombatTestHelper.CreateStatBlock();
            dummyCharacter = CombatTestHelper.CreateCharacterData("dummy_char", "Dummy", stats);

            RunSessionManager.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            RunSessionManager.Clear();
            CombatTestHelper.CleanupTestDatabase();

            if (dummyCharacter != null) Object.DestroyImmediate(dummyCharacter);
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
    }
}
