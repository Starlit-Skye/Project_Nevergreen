using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using Nevergreen.UI;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TooltipSystemTests
    {
        private GameObject _triggerGo;
        private SkillTooltipTrigger _trigger;
        private SkillData _testSkill;

        private bool _eventShowFired;
        private bool _eventHideFired;
        private string _eventDescription;

        [SetUp]
        public void Setup()
        {
            _triggerGo = new GameObject("Trigger");
            _trigger = _triggerGo.AddComponent<SkillTooltipTrigger>();
            
            _testSkill = ScriptableObject.CreateInstance<SkillData>();
            _testSkill.displayName = "Test Skill";
            _testSkill.description = "Test description";

            _eventShowFired = false;
            _eventHideFired = false;
            _eventDescription = null;

            TooltipEvents.OnShowTooltip += OnShow;
            TooltipEvents.OnHideTooltip += OnHide;
        }

        [TearDown]
        public void Teardown()
        {
            TooltipEvents.OnShowTooltip -= OnShow;
            TooltipEvents.OnHideTooltip -= OnHide;

            if (_triggerGo != null) Object.DestroyImmediate(_triggerGo);
            if (_testSkill != null) ScriptableObject.DestroyImmediate(_testSkill);
        }

        private void OnShow(string desc)
        {
            _eventShowFired = true;
            _eventDescription = desc;
        }

        private void OnHide()
        {
            _eventHideFired = true;
        }

        [Test]
        public void ShowTooltip_FiresEventWithDescription()
        {
            TooltipEvents.ShowTooltip("hello");
            Assert.IsTrue(_eventShowFired);
            Assert.AreEqual("hello", _eventDescription);
        }

        [Test]
        public void HideTooltip_FiresHideEvent()
        {
            TooltipEvents.HideTooltip();
            Assert.IsTrue(_eventHideFired);
        }

        [Test]
        public void PointerEnter_ShowsTooltip_WhenSkillIsSet()
        {
            _trigger.SetSkill(_testSkill);
            _trigger.OnPointerEnter(new PointerEventData(EventSystem.current));

            Assert.IsTrue(_eventShowFired);
            Assert.AreEqual("Test description", _eventDescription);
        }

        [Test]
        public void PointerEnter_DoesNotShowTooltip_WhenSkillIsNull()
        {
            _trigger.SetSkill(null);
            _trigger.OnPointerEnter(new PointerEventData(EventSystem.current));

            Assert.IsFalse(_eventShowFired);
        }

        [Test]
        public void PointerExit_HidesTooltip()
        {
            _trigger.SetSkill(_testSkill);
            _trigger.OnPointerEnter(new PointerEventData(EventSystem.current));
            
            _trigger.OnPointerExit(new PointerEventData(EventSystem.current));

            Assert.IsTrue(_eventHideFired);
        }

        [Test]
        public void Disable_HidesTooltip_IfHovered()
        {
            _trigger.SetSkill(_testSkill);
            _trigger.OnPointerEnter(new PointerEventData(EventSystem.current));
            
            // Simulating OnDisable call (EditMode doesn't auto-fire lifecycle events)
            var onDisableMethod = typeof(SkillTooltipTrigger).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisableMethod.Invoke(_trigger, null);

            Assert.IsTrue(_eventHideFired);
        }

        [Test]
        public void SetSkill_UpdatesTooltip_IfCurrentlyHovered()
        {
            // Enter without skill
            _trigger.SetSkill(null);
            _trigger.OnPointerEnter(new PointerEventData(EventSystem.current));
            Assert.IsFalse(_eventShowFired);

            // Now assign skill while hovered
            _trigger.SetSkill(_testSkill);
            
            Assert.IsTrue(_eventShowFired);
            Assert.AreEqual("Test description", _eventDescription);
        }

        [Test]
        public void SetSkill_Null_HidesTooltip_IfCurrentlyHovered()
        {
            _trigger.SetSkill(_testSkill);
            _trigger.OnPointerEnter(new PointerEventData(EventSystem.current));
            Assert.IsTrue(_eventShowFired);

            // Now assign null while hovered
            _trigger.SetSkill(null);
            
            Assert.IsTrue(_eventHideFired);
        }
    }
}
