using NUnit.Framework;
using UnityEngine;
using TMPro;
using Nevergreen.UI;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class RoomTooltipTests
    {
        private GameObject _displayGo;
        private RoomTooltipDisplay _display;
        private GameObject _triggerGo;
        private RoomTooltipTrigger _trigger;
        private RoomData _testRoomData;

        private TextMeshProUGUI _descText;

        [SetUp]
        public void Setup()
        {
            // Setup Display
            _displayGo = new GameObject("RoomTooltipDisplay");
            _display = _displayGo.AddComponent<RoomTooltipDisplay>();
            
            var descGo = new GameObject("DescText");
            _descText = descGo.AddComponent<TextMeshProUGUI>();

            var tooltipField = typeof(RoomTooltipDisplay).GetField("tooltipText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            tooltipField.SetValue(_display, _descText);

            var visualPanelField = typeof(RoomTooltipDisplay).GetField("visualPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            visualPanelField.SetValue(_display, _displayGo); // Self as panel

            var awakeMethod = typeof(RoomTooltipDisplay).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(_display, null);

            var onEnableMethod = typeof(RoomTooltipDisplay).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod?.Invoke(_display, null);

            // Setup Trigger
            _triggerGo = new GameObject("RoomChoiceButton");
            _trigger = _triggerGo.AddComponent<RoomTooltipTrigger>();

            _testRoomData = ScriptableObject.CreateInstance<RoomData>();
            _testRoomData.roomName = "Test Room";
            _testRoomData.description = "Test Description";
            
            _trigger.SetRoom(_testRoomData);
        }

        [TearDown]
        public void Teardown()
        {
            var onDisableMethod = typeof(RoomTooltipDisplay).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisableMethod?.Invoke(_display, null);

            if (_displayGo != null) Object.DestroyImmediate(_displayGo);
            if (_triggerGo != null) Object.DestroyImmediate(_triggerGo);
            if (_testRoomData != null) Object.DestroyImmediate(_testRoomData);
        }

        [Test]
        public void PointerEnter_ShowsTooltipWithCorrectData()
        {
            // Arrange
            _displayGo.SetActive(false); // Tooltip hidden by default

            // Act
            _trigger.OnPointerEnter(null);

            // Assert
            Assert.IsTrue(_displayGo.activeSelf, "Tooltip should be active after hover.");
            Assert.AreEqual("Test Description", _descText.text, "Tooltip description should match room description.");
        }

        [Test]
        public void PointerExit_HidesTooltip()
        {
            // Arrange
            _trigger.OnPointerEnter(null);
            Assert.IsTrue(_displayGo.activeSelf);

            // Act
            _trigger.OnPointerExit(null);

            // Assert
            Assert.IsFalse(_displayGo.activeSelf, "Tooltip should be hidden after hover exit.");
        }

        [Test]
        public void SetRoom_UpdatesTooltip_IfCurrentlyHovered()
        {
            // Arrange
            _trigger.OnPointerEnter(null);
            Assert.AreEqual("Test Description", _descText.text);

            var newRoomData = ScriptableObject.CreateInstance<RoomData>();
            newRoomData.roomName = "New Room";
            newRoomData.description = "New Description";

            // Act
            _trigger.SetRoom(newRoomData);

            // Assert
            Assert.AreEqual("New Description", _descText.text);
            
            Object.DestroyImmediate(newRoomData);
        }
        
        [Test]
        public void PointerEnter_DoesNotShowTooltip_WhenRoomIsNull()
        {
            // Arrange
            _trigger.SetRoom(null);
            _displayGo.SetActive(false);
            
            // Act
            _trigger.OnPointerEnter(null);
            
            // Assert
            Assert.IsFalse(_displayGo.activeSelf, "Tooltip should remain inactive when RoomData is null.");
        }
        
        [Test]
        public void PointerEnter_DoesNotShowTooltip_WhenDescriptionIsEmpty()
        {
            // Arrange
            var emptyDescRoom = ScriptableObject.CreateInstance<RoomData>();
            emptyDescRoom.description = "";
            _trigger.SetRoom(emptyDescRoom);
            _displayGo.SetActive(false);
            
            // Act
            _trigger.OnPointerEnter(null);
            
            // Assert
            Assert.IsFalse(_displayGo.activeSelf, "Tooltip should remain inactive when description is empty.");
            
            Object.DestroyImmediate(emptyDescRoom);
        }
    }
}
