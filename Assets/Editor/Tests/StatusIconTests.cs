using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Combat;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class StatusIconTests
    {
        private CombatConfig config;
        private HPBar hpBar;
        private GameObject hpBarGO;
        private CombatCharacter character;
        private GameObject statusIconPrefab;
        private RectTransform statusIconContainer;

        [SetUp]
        public void Setup()
        {
            // Setup CombatConfig
            config = ScriptableObject.CreateInstance<CombatConfig>();
            config.statusIcons = new List<StatusIconMapping>();

            // Mock GameDatabase (since HPBar queries it)
            var gameDb = GameDatabase.CreateForTesting(combatCfg: config);
            GameDatabase.SetInstanceForTesting(gameDb);

            // Setup CombatCharacter
            var charGo = new GameObject("TestCharacter");
            character = charGo.AddComponent<CombatCharacter>();
            character.baseStats = new CombatStats { maxHP = 100 };
            character.currentHP = 100;

            // Setup HPBar components
            hpBarGO = new GameObject("HPBar");
            hpBar = hpBarGO.AddComponent<HPBar>();
            
            var containerGO = new GameObject("Container");
            statusIconContainer = containerGO.AddComponent<RectTransform>();
            statusIconContainer.SetParent(hpBarGO.transform, false);

            var iconGO = new GameObject("IconPrefab");
            iconGO.AddComponent<Image>();
            statusIconPrefab = iconGO;

            hpBar.statusIconContainer = statusIconContainer;
            hpBar.statusIconPrefab = statusIconPrefab;
        }

        [TearDown]
        public void Teardown()
        {
            var db = GameDatabase.Instance;
            GameDatabase.SetInstanceForTesting(null);
            if (db != null) Object.DestroyImmediate(db);
            
            Object.DestroyImmediate(hpBarGO);
            Object.DestroyImmediate(character.gameObject);
            Object.DestroyImmediate(statusIconPrefab);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void CombatConfig_GetStatusIcon_ResolvesCorrectly()
        {
            var genericSprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            var specificSprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);

            config.statusIcons.Add(new StatusIconMapping
            {
                statusType = StatusType.Buff,
                specifyStatTarget = false,
                icon = genericSprite
            });

            config.statusIcons.Add(new StatusIconMapping
            {
                statusType = StatusType.Buff,
                specifyStatTarget = true,
                targetStat = StatTarget.Speed,
                icon = specificSprite
            });

            // Specific match (Buff on Speed)
            Assert.AreEqual(specificSprite, config.GetStatusIcon(StatusType.Buff, StatTarget.Speed));

            // Generic fallback (Buff on Attack)
            Assert.AreEqual(genericSprite, config.GetStatusIcon(StatusType.Buff, StatTarget.Attack));

            // No match
            Assert.IsNull(config.GetStatusIcon(StatusType.Bleed, StatTarget.Attack));
        }

        [Test]
        public void HPBar_Refresh_InstantiatesStatusIcons()
        {
            var sprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            config.statusIcons.Add(new StatusIconMapping { statusType = StatusType.Bleed, icon = sprite });

            hpBar.Initialize(character, null);
            
            // Container should be empty initially
            Assert.AreEqual(0, statusIconContainer.childCount);

            // Apply status
            var bleedStatus = new StatusEffectInstance(StatusType.Bleed, StatTarget.Attack, 10, 3);
            character.AddStatus(bleedStatus);

            // HPBar should have instantiated 1 icon
            Assert.AreEqual(1, statusIconContainer.childCount);
            
            var instIcon = statusIconContainer.GetChild(0).GetComponent<Image>();
            Assert.IsNotNull(instIcon);
            Assert.AreEqual(sprite, instIcon.sprite);
        }

        [Test]
        public void HPBar_Refresh_GroupsSameStatusType()
        {
            var sprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            config.statusIcons.Add(new StatusIconMapping { statusType = StatusType.Bleed, icon = sprite });

            hpBar.Initialize(character, null);

            // Apply two bleeds
            character.AddStatus(new StatusEffectInstance(StatusType.Bleed, StatTarget.Attack, 10, 3));
            character.AddStatus(new StatusEffectInstance(StatusType.Bleed, StatTarget.Attack, 5, 2));

            // Should still only instantiate 1 icon due to grouping by type
            Assert.AreEqual(1, statusIconContainer.childCount);
        }

        [Test]
        public void HPBar_Refresh_RemovesExpiredIcons()
        {
            var sprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            config.statusIcons.Add(new StatusIconMapping { statusType = StatusType.Bleed, icon = sprite });

            hpBar.Initialize(character, null);

            var bleedStatus = new StatusEffectInstance(StatusType.Bleed, StatTarget.Attack, 10, 1);
            character.AddStatus(bleedStatus);

            Assert.AreEqual(1, statusIconContainer.childCount);

            // Tick duration so it expires
            StatusProcessor.TickDurations(character, 0);

        // Icon should be removed
            Assert.AreEqual(0, statusIconContainer.childCount);
        }

        [Test]
        public void StatusIcon_Hover_TriggersGlobalEvent()
        {
            var sprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            config.statusIcons.Add(new StatusIconMapping { statusType = StatusType.Bleed, icon = sprite });

            // Add the trigger component to the prefab before instantiation
            statusIconPrefab.AddComponent<Nevergreen.UI.StatusIconTooltipTrigger>();

            hpBar.Initialize(character, null);
            var bleedStatus = new StatusEffectInstance(StatusType.Bleed, StatTarget.Attack, 10, 1);
            character.AddStatus(bleedStatus);

            var instIcon = statusIconContainer.GetChild(0).gameObject;
            var trigger = instIcon.GetComponent<Nevergreen.UI.StatusIconTooltipTrigger>();
            Assert.IsNotNull(trigger, "Trigger should be attached to instantiated icon.");

            bool showEventFired = false;
            bool hideEventFired = false;

            System.Action<Nevergreen.UI.StatusIconTooltipTrigger> onShow = t => { if (t == trigger) showEventFired = true; };
            System.Action<Nevergreen.UI.StatusIconTooltipTrigger> onHide = t => { if (t == trigger) hideEventFired = true; };

            Nevergreen.UI.TooltipEvents.OnShowStatusTooltip += onShow;
            Nevergreen.UI.TooltipEvents.OnHideStatusTooltip += onHide;

            try
            {
                trigger.OnPointerEnter(null);
                Assert.IsTrue(showEventFired, "OnShowStatusTooltip should be fired on pointer enter.");

                trigger.OnPointerExit(null);
                Assert.IsTrue(hideEventFired, "OnHideStatusTooltip should be fired on pointer exit.");
            }
            finally
            {
                Nevergreen.UI.TooltipEvents.OnShowStatusTooltip -= onShow;
                Nevergreen.UI.TooltipEvents.OnHideStatusTooltip -= onHide;
            }
        }

        [Test]
        public void StatusTooltipDisplay_FiltersByHPBar()
        {
            var sprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            config.statusIcons.Add(new StatusIconMapping { statusType = StatusType.Bleed, icon = sprite });
            statusIconPrefab.AddComponent<Nevergreen.UI.StatusIconTooltipTrigger>();

            // Setup a local tooltip on the HPBar
            var visualPanelGO = new GameObject("VisualPanel");
            visualPanelGO.transform.SetParent(hpBarGO.transform, false);

            var tooltipDisplay = hpBarGO.AddComponent<Nevergreen.UI.StatusTooltipDisplay>();
            // Use reflection to set private serialized fields for the test
            var visualPanelField = typeof(Nevergreen.UI.StatusTooltipDisplay).GetField("visualPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            visualPanelField.SetValue(tooltipDisplay, visualPanelGO);

            // Initialize tooltip (Awake/OnEnable equivalent)
            visualPanelGO.SetActive(false);
            var onEnableMethod = typeof(Nevergreen.UI.StatusTooltipDisplay).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod.Invoke(tooltipDisplay, null);

            hpBar.Initialize(character, null);
            var bleedStatus = new StatusEffectInstance(StatusType.Bleed, StatTarget.Attack, 10, 1);
            character.AddStatus(bleedStatus);

            var trigger = statusIconContainer.GetChild(0).GetComponent<Nevergreen.UI.StatusIconTooltipTrigger>();
            
            // Hover enter
            trigger.OnPointerEnter(null);
            Assert.IsTrue(visualPanelGO.activeSelf, "Visual panel should be active after valid hover enter.");

            // Hover exit
            trigger.OnPointerExit(null);
            Assert.IsFalse(visualPanelGO.activeSelf, "Visual panel should be inactive after valid hover exit.");

            // Create a fake trigger that doesn't belong to this HPBar
            var fakeIcon = new GameObject("FakeIcon");
            var fakeTrigger = fakeIcon.AddComponent<Nevergreen.UI.StatusIconTooltipTrigger>();
            fakeTrigger.Initialize(bleedStatus);

            // Hover fake trigger
            fakeTrigger.OnPointerEnter(null);
            Assert.IsFalse(visualPanelGO.activeSelf, "Visual panel should not activate for a trigger from another HPBar.");

            var onDisableMethod = typeof(Nevergreen.UI.StatusTooltipDisplay).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisableMethod.Invoke(tooltipDisplay, null);
            
            Object.DestroyImmediate(fakeIcon);
        }

        [Test]
        public void StatusTooltipDisplay_FormatsTooltipText_Correctly()
        {
            var textGO = new GameObject("Text");
            var tooltipText = textGO.AddComponent<TextMeshProUGUI>();

            var tooltipDisplay = hpBarGO.AddComponent<Nevergreen.UI.StatusTooltipDisplay>();
            var textField = typeof(Nevergreen.UI.StatusTooltipDisplay).GetField("tooltipText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            textField.SetValue(tooltipDisplay, tooltipText);

            var onEnableMethod = typeof(Nevergreen.UI.StatusTooltipDisplay).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod.Invoke(tooltipDisplay, null);

            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(hpBarGO.transform, false);
            var trigger = iconGO.AddComponent<Nevergreen.UI.StatusIconTooltipTrigger>();

            void TestFormat(StatusEffectInstance status, string expectedText)
            {
                trigger.Initialize(status);
                trigger.OnPointerEnter(null);
                Assert.AreEqual(expectedText, tooltipText.text, $"Formatting failed for {status.type}");
            }

            // Bleed
            TestFormat(new StatusEffectInstance(StatusType.Bleed, StatTarget.Attack, 10, 3), "10 dmg for 3 rounds");
            
            // Buff/Debuff
            TestFormat(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 15, 2, AmplitudeType.Default), "+15 Attack% for 2 rounds");
            TestFormat(new StatusEffectInstance(StatusType.Buff, StatTarget.CritChance, 15, 2, AmplitudeType.Default), "+15 CritChance for 2 rounds");
            TestFormat(new StatusEffectInstance(StatusType.Debuff, StatTarget.Speed, 15, 2, AmplitudeType.Percentage), "-15 Speed% for 2 rounds");
            TestFormat(new StatusEffectInstance(StatusType.Debuff, StatTarget.Speed, 15, 2, AmplitudeType.Flat), "-15 Speed for 2 rounds");

            // Guard
            var guardStatus = new Nevergreen.Combat.GuardStatusInstance(character, 2);
            TestFormat(guardStatus, $"Guarded by {character.DisplayName} for 2 rounds");

            // HealReceivedReduction
            TestFormat(new StatusEffectInstance(StatusType.HealReceivedReduction, StatTarget.Speed, 50, 2), "Heal received -50% for 2 rounds");

            // BleedOnAttack
            TestFormat(new Nevergreen.Combat.BleedOnAttackStatusInstance(null, 2, 5, 3, 25f), "Attacks apply Bleed(25% chance) for 2 rounds");
            
            // Burn
            TestFormat(new StatusEffectInstance(StatusType.Burn, StatTarget.Speed, 5, 2), "5dmg, dmg + 1 each turn, for 2 rounds");

            var onDisableMethod = typeof(Nevergreen.UI.StatusTooltipDisplay).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisableMethod.Invoke(tooltipDisplay, null);

            Object.DestroyImmediate(textGO);
            Object.DestroyImmediate(iconGO);
        }
    }
}
