using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Nevergreen.Data;
using Nevergreen.Prototype;
using Nevergreen.UI;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class SafeRoomControllerTests
    {
        private List<Object> _objectsToCleanUp;

        [SetUp]
        public void SetUp()
        {
            _objectsToCleanUp = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
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

        private CharacterData CreateMockCharacter(int maxHP, string name = "Marionette")
        {
            var character = ScriptableObject.CreateInstance<CharacterData>();
            character.displayName = name;
            character.statPerLevel = new List<StatBlockData>
            {
                new StatBlockData { maxHP = maxHP }
            };
            _objectsToCleanUp.Add(character);
            return character;
        }

        private (GameObject root, SafeRoomController controller, Button[] buttons) CreateControllerHierarchy()
        {
            var go = new GameObject("SafeRoomUI");
            _objectsToCleanUp.Add(go);

            var controller = go.AddComponent<SafeRoomController>();
            var buttons = new Button[4];

            for (int i = 0; i < 4; i++)
            {
                var btnGo = new GameObject($"MarionetteButton_{i}");
                btnGo.transform.SetParent(go.transform);
                var btn = btnGo.AddComponent<Button>();
                btnGo.AddComponent<TMPro.TextMeshProUGUI>();
                buttons[i] = btn;
            }

            var buttonsField = typeof(SafeRoomController).GetField(
                "marionetteButtons",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            buttonsField.SetValue(controller, buttons);

            return (go, controller, buttons);
        }

        [Test]
        public void SafeRoom_SingleMarionetteSelected_HealsConfiguredPercentOfMaxHP()
        {
            // Arrange
            var character = CreateMockCharacter(100);
            var member = new PartyMemberInfo { currentLevel = 1, character = character, currentHP = 20 };
            var party = new List<PartyMemberInfo> { member };

            var (_, controller, buttons) = CreateControllerHierarchy();
            controller.HealPercent = 0.5f;

            // Act
            controller.Initialize(party);
            LogAssert.Expect(LogType.Error, "[SafeRoomController] CombatUI not found!");
            buttons[0].onClick.Invoke();

            // Assert: 20 + 50% of 100 = 70
            Assert.AreEqual(70, member.currentHP, "20 HP + 50% of 100 should equal 70 HP.");
        }

        [Test]
        public void SafeRoom_HealPercentIsConfigurable()
        {
            // Arrange
            var character = CreateMockCharacter(100);
            var member = new PartyMemberInfo { currentLevel = 1, character = character, currentHP = 20 };
            var party = new List<PartyMemberInfo> { member };

            var (_, controller, buttons) = CreateControllerHierarchy();
            controller.HealPercent = 0.25f;

            // Act
            controller.Initialize(party);
            LogAssert.Expect(LogType.Error, "[SafeRoomController] CombatUI not found!");
            buttons[0].onClick.Invoke();

            // Assert: 20 + 25% of 100 = 45
            Assert.AreEqual(45, member.currentHP, "20 HP + 25% of 100 should equal 45 HP.");
        }

        [Test]
        public void SafeRoom_OverhealCapsAtMaxHP_AndSetsCurrentHPToNull()
        {
            // Arrange
            var character = CreateMockCharacter(100);
            var member = new PartyMemberInfo { currentLevel = 1, character = character, currentHP = 80 };
            var party = new List<PartyMemberInfo> { member };

            var (_, controller, buttons) = CreateControllerHierarchy();
            controller.HealPercent = 0.5f;

            // Act
            controller.Initialize(party);
            LogAssert.Expect(LogType.Error, "[SafeRoomController] CombatUI not found!");
            buttons[0].onClick.Invoke();

            // Assert: 80 + 50 = 130 >= 100 -> null
            Assert.IsNull(member.currentHP, "Healed HP exceeding maxHP should be set to null (full HP).");
        }

        [Test]
        public void SafeRoom_AlreadyFullHP_RemainsNull()
        {
            // Arrange
            var character = CreateMockCharacter(100);
            var member = new PartyMemberInfo { currentLevel = 1, character = character, currentHP = null };
            var party = new List<PartyMemberInfo> { member };

            var (_, controller, buttons) = CreateControllerHierarchy();
            controller.HealPercent = 0.5f;

            // Act
            controller.Initialize(party);
            LogAssert.Expect(LogType.Error, "[SafeRoomController] CombatUI not found!");
            buttons[0].onClick.Invoke();

            // Assert: Full HP member should remain null
            Assert.IsNull(member.currentHP, "Member with full HP (null) should remain null after healing.");
        }

        [Test]
        public void SafeRoom_CompletesRoom_DeactivatesGameObjectAndNotifiesCombatUI()
        {
            // Arrange
            var combatUIGo = new GameObject("CombatUI");
            _objectsToCleanUp.Add(combatUIGo);
            combatUIGo.AddComponent<CombatUI>();

            var character = CreateMockCharacter(100);
            var member = new PartyMemberInfo { currentLevel = 1, character = character, currentHP = 30 };
            var party = new List<PartyMemberInfo> { member };

            var (root, controller, buttons) = CreateControllerHierarchy();
            controller.Initialize(party);

            // Act
            buttons[0].onClick.Invoke();

            // Assert
            Assert.IsFalse(root.activeSelf, "SafeRoom UI GameObject should be deactivated after choice.");
        }

        [Test]
        public void SafeRoom_MissingOrEmptyPartySlots_DeactivatesButtonsAndUpdatesLabels()
        {
            // Arrange
            var char1 = CreateMockCharacter(100, "Puppet A");
            var char2 = CreateMockCharacter(120, "Puppet B");

            var member1 = new PartyMemberInfo { currentLevel = 1, character = char1, currentHP = 50 };
            var member2 = new PartyMemberInfo { currentLevel = 1, character = char2, currentHP = 60 };

            var party = new List<PartyMemberInfo> { member1, member2 };

            var (_, controller, buttons) = CreateControllerHierarchy();

            // Act
            controller.Initialize(party);

            // Assert
            Assert.IsTrue(buttons[0].gameObject.activeSelf, "Slot 0 should be active.");
            Assert.IsTrue(buttons[1].gameObject.activeSelf, "Slot 1 should be active.");
            Assert.IsFalse(buttons[2].gameObject.activeSelf, "Slot 2 should be inactive.");
            Assert.IsFalse(buttons[3].gameObject.activeSelf, "Slot 3 should be inactive.");

            var text0 = buttons[0].GetComponentInChildren<TMPro.TextMeshProUGUI>();
            var text1 = buttons[1].GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Assert.AreEqual("Puppet A", text0.text);
            Assert.AreEqual("Puppet B", text1.text);
        }

        [Test]
        public void SafeRoom_NullPartyMemberOrNullCharacter_DeactivatesButton()
        {
            // Arrange
            var char1 = CreateMockCharacter(100, "Puppet A");
            var member1 = new PartyMemberInfo { currentLevel = 1, character = char1, currentHP = 50 };
            var member2 = new PartyMemberInfo { currentLevel = 1, character = null }; // null character

            var party = new List<PartyMemberInfo> { member1, member2, null };

            var (_, controller, buttons) = CreateControllerHierarchy();

            // Act
            controller.Initialize(party);

            // Assert
            Assert.IsTrue(buttons[0].gameObject.activeSelf, "Slot 0 has valid character, should be active.");
            Assert.IsFalse(buttons[1].gameObject.activeSelf, "Slot 1 has null character, should be inactive.");
            Assert.IsFalse(buttons[2].gameObject.activeSelf, "Slot 2 is null member, should be inactive.");
            Assert.IsFalse(buttons[3].gameObject.activeSelf, "Slot 3 out of bounds, should be inactive.");
        }
    }
}
