using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using Nevergreen.Data;
using Nevergreen.UI;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TrinketEquipPanelHelperTests
    {
        private List<Object> _objectsToCleanUp;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _objectsToCleanUp = new List<Object>();

            // Setup a mock TrinketUIItem prefab
            _prefab = new GameObject("MockTrinketUIItemPrefab");
            _prefab.AddComponent<TrinketUIItem>();
            _prefab.AddComponent<TrinketTooltipTrigger>();
            _objectsToCleanUp.Add(_prefab);
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

        [Test]
        public void FindEquipPanels_WhenRootNull_ReturnsEmptyList()
        {
            var panels = TrinketEquipPanelHelper.FindEquipPanels(null);
            Assert.IsNotNull(panels);
            Assert.AreEqual(0, panels.Count);
        }

        [Test]
        public void FindEquipPanels_ReturnsPanelsWithMatchingChildNames()
        {
            var root = new GameObject("Root");
            _objectsToCleanUp.Add(root);

            var panel1 = new GameObject("Panel1");
            panel1.transform.SetParent(root.transform);
            var text1 = new GameObject("TrinketEquipMarionetteName");
            text1.transform.SetParent(panel1.transform);
            text1.AddComponent<TextMeshProUGUI>();

            var panel2 = new GameObject("Panel2");
            panel2.transform.SetParent(root.transform);
            var text2 = new GameObject("TrinketEquipMarionetteName");
            text2.transform.SetParent(panel2.transform);
            text2.AddComponent<TextMeshProUGUI>();

            var dummyPanel = new GameObject("DummyPanel");
            dummyPanel.transform.SetParent(root.transform);
            var dummyText = new GameObject("OtherText");
            dummyText.transform.SetParent(dummyPanel.transform);
            dummyText.AddComponent<TextMeshProUGUI>();

            var panels = TrinketEquipPanelHelper.FindEquipPanels(root.transform);
            Assert.AreEqual(2, panels.Count);
            Assert.AreEqual(panel1, panels[0]);
            Assert.AreEqual(panel2, panels[1]);
        }

        [Test]
        public void RefreshPanels_WhenPartyNull_DoesNothing()
        {
            var panel = new GameObject("Panel");
            _objectsToCleanUp.Add(panel);
            panel.SetActive(true);

            TrinketEquipPanelHelper.RefreshPanels(new List<GameObject> { panel }, null, _prefab);
            Assert.IsTrue(panel.activeSelf);
        }

        [Test]
        public void RefreshPanels_ConfiguresActivePanelsAndHidesExcess()
        {
            var panel1 = new GameObject("Panel1");
            _objectsToCleanUp.Add(panel1);
            var nameGo1 = new GameObject("TrinketEquipMarionetteName");
            nameGo1.transform.SetParent(panel1.transform);
            var nameText1 = nameGo1.AddComponent<TextMeshProUGUI>();

            var panel2 = new GameObject("Panel2");
            _objectsToCleanUp.Add(panel2);
            var nameGo2 = new GameObject("TrinketEquipMarionetteName");
            nameGo2.transform.SetParent(panel2.transform);
            var nameText2 = nameGo2.AddComponent<TextMeshProUGUI>();

            var charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.displayName = "Doll Alpha";
            _objectsToCleanUp.Add(charData);

            var member = new PartyMemberInfo
            {
                character = charData,
                equippedTrinkets = new List<TrinketData>()
            };

            var party = new List<PartyMemberInfo> { member };
            var panels = new List<GameObject> { panel1, panel2 };

            TrinketEquipPanelHelper.RefreshPanels(panels, party, _prefab);

            Assert.IsTrue(panel1.activeSelf);
            Assert.AreEqual("Doll Alpha", nameText1.text);
            Assert.IsFalse(panel2.activeSelf);
        }

        [Test]
        public void SetupTrinketSlot_CreatesDropHandlerAndInstantiatesItem()
        {
            var container = new GameObject("Trinket1");
            _objectsToCleanUp.Add(container);

            var oldChild = new GameObject("OldChild");
            oldChild.transform.SetParent(container.transform);

            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            trinket.trinketId = "T1";
            trinket.displayName = "Gear";
            _objectsToCleanUp.Add(trinket);

            var member = new PartyMemberInfo
            {
                equippedTrinkets = new List<TrinketData> { trinket }
            };

            TrinketEquipPanelHelper.SetupTrinketSlot(container.transform, member, 0, _prefab);

            var dropHandler = container.GetComponent<TrinketSlotDropHandler>();
            Assert.IsNotNull(dropHandler);
            Assert.AreEqual(member, dropHandler.TargetMember);
            Assert.AreEqual(0, dropHandler.TargetSlotIndex);

            // Verify old child removed and new child instantiated
            Assert.AreEqual(1, container.transform.childCount);
            var uiItem = container.GetComponentInChildren<TrinketUIItem>();
            Assert.IsNotNull(uiItem);
            Assert.AreEqual(trinket, uiItem.TrinketData);
        }

        [Test]
        public void SetupTrinketSlot_WhenTrinketSlotEmpty_ClearsChildrenAndCreatesNoItem()
        {
            var container = new GameObject("Trinket1");
            _objectsToCleanUp.Add(container);

            var oldChild = new GameObject("OldChild");
            oldChild.transform.SetParent(container.transform);

            var member = new PartyMemberInfo
            {
                equippedTrinkets = new List<TrinketData> { null }
            };

            TrinketEquipPanelHelper.SetupTrinketSlot(container.transform, member, 0, _prefab);

            var dropHandler = container.GetComponent<TrinketSlotDropHandler>();
            Assert.IsNotNull(dropHandler);
            Assert.AreEqual(0, container.transform.childCount);
        }
    }
}

