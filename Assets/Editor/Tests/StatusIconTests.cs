using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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
    }
}
