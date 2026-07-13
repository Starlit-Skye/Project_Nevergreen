using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Nevergreen.Data;
using Nevergreen.UI;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TraitTooltipTests
    {
        private GameObject _displayGo;
        private TraitTooltipDisplay _display;
        private TextMeshProUGUI _nameText;

        private GameObject _triggerGo;
        private TraitTooltipTrigger _trigger;

        private TraitData _testTrait;
        
        [SetUp]
        public void Setup()
        {
            // Setup display
            _displayGo = new GameObject("TraitTooltipDisplay");
            _display = _displayGo.AddComponent<TraitTooltipDisplay>();
            
            var textGo = new GameObject("NameText");
            textGo.transform.SetParent(_displayGo.transform);
            _nameText = textGo.AddComponent<TextMeshProUGUI>();
            
            var tooltipTextField = typeof(TraitTooltipDisplay).GetField("tooltipText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            tooltipTextField.SetValue(_display, _nameText);
            
            // Call Awake manually
            var awakeMethod = typeof(TraitTooltipDisplay).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(_display, null);
            
            // Call OnEnable manually to hook up events
            var onEnableMethod = typeof(TraitTooltipDisplay).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod.Invoke(_display, null);
            
            // Create test trait
            _testTrait = ScriptableObject.CreateInstance<TraitData>();
            _testTrait.displayName = "Test Perfection";
            
            // Setup trigger
            _triggerGo = new GameObject("TraitTrigger");
            _trigger = _triggerGo.AddComponent<TraitTooltipTrigger>();
            _trigger.SetTrait(_testTrait);
        }

        [TearDown]
        public void Teardown()
        {
            var onDisableMethod = typeof(TraitTooltipDisplay).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (onDisableMethod != null && _display != null)
            {
                onDisableMethod.Invoke(_display, null);
            }
            
            var onDestroyMethod = typeof(TraitTooltipDisplay).GetMethod("OnDestroy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (onDestroyMethod != null && _display != null)
            {
                onDestroyMethod.Invoke(_display, null);
            }
            
            if (_displayGo != null) Object.DestroyImmediate(_displayGo);
            if (_triggerGo != null) Object.DestroyImmediate(_triggerGo);
            if (_testTrait != null) ScriptableObject.DestroyImmediate(_testTrait);
        }

        [Test]
        public void HoverEnter_ShowsTooltipWithCorrectData()
        {
            // Arrange - tooltip should be inactive initially
            Assert.IsFalse(_displayGo.activeSelf);
            
            // Act - hover over trigger
            var pointerEventData = new PointerEventData(EventSystem.current);
            _trigger.OnPointerEnter(pointerEventData);
            
            // Assert - tooltip should be active and display the correct text
            Assert.IsTrue(_displayGo.activeSelf, "Tooltip should be active after hover.");
            Assert.AreEqual("Test Perfection", _nameText.text, "Tooltip name text should match the trait's display name.");
        }

        [Test]
        public void HoverExit_HidesTooltip()
        {
            // Arrange - hover first to show tooltip
            var pointerEventData = new PointerEventData(EventSystem.current);
            _trigger.OnPointerEnter(pointerEventData);
            Assert.IsTrue(_displayGo.activeSelf);
            
            // Act - exit hover
            _trigger.OnPointerExit(pointerEventData);
            
            // Assert - tooltip should be inactive
            Assert.IsFalse(_displayGo.activeSelf, "Tooltip should be inactive after hover exit.");
        }
    }
}
