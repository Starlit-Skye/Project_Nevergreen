using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Helper utilities for discovering and populating party member trinket equip panels across room controllers.
    /// </summary>
    public static class TrinketEquipPanelHelper
    {
        public static List<GameObject> FindEquipPanels(Component root)
        {
            var panels = new List<GameObject>();
            if (root == null) return panels;

            var nameTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var nt in nameTexts)
            {
                if (nt != null && nt.name == "TrinketEquipMarionetteName" && nt.transform.parent != null)
                {
                    panels.Add(nt.transform.parent.gameObject);
                }
            }
            return panels;
        }

        public static void RefreshPanels(List<GameObject> trinketEquipPanels, List<PartyMemberInfo> party, GameObject trinketUIItemPrefab)
        {
            if (trinketEquipPanels == null || party == null) return;

            for (int i = 0; i < trinketEquipPanels.Count; i++)
            {
                GameObject panel = trinketEquipPanels[i];
                if (panel == null) continue;

                if (i < party.Count)
                {
                    panel.SetActive(true);
                    PartyMemberInfo member = party[i];

                    // Set Marionette Name
                    var nameText = panel.transform.Find("TrinketEquipMarionetteName")?.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = member != null && member.character != null ? member.character.displayName : "Marionette";
                    }

                    // Setup Trinket 1
                    var trinket1 = panel.transform.Find("Trinket1");
                    if (trinket1 != null)
                    {
                        SetupTrinketSlot(trinket1, member, 0, trinketUIItemPrefab);
                    }

                    // Setup Trinket 2
                    var trinket2 = panel.transform.Find("Trinket2");
                    if (trinket2 != null)
                    {
                        SetupTrinketSlot(trinket2, member, 1, trinketUIItemPrefab);
                    }
                }
                else
                {
                    panel.SetActive(false);
                }
            }
        }

        public static void SetupTrinketSlot(Transform container, PartyMemberInfo member, int slotIndex, GameObject trinketUIItemPrefab)
        {
            if (container == null) return;

            var dropHandler = container.GetComponent<TrinketSlotDropHandler>();
            if (dropHandler == null) dropHandler = container.gameObject.AddComponent<TrinketSlotDropHandler>();

            dropHandler.TargetMember = member;
            dropHandler.TargetSlotIndex = slotIndex;

            // Clear existing UI items
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i);
                if (Application.isPlaying) Object.Destroy(child.gameObject);
                else Object.DestroyImmediate(child.gameObject);
            }

            if (member != null && member.equippedTrinkets != null && slotIndex >= 0 && slotIndex < member.equippedTrinkets.Count)
            {
                var trinket = member.equippedTrinkets[slotIndex];
                if (trinket == null) return;

                if (trinketUIItemPrefab != null)
                {
                    GameObject item = Object.Instantiate(trinketUIItemPrefab, container);

                    var tooltipTrigger = item.GetComponent<TrinketTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TrinketTooltipTrigger>();
                    tooltipTrigger.SetTrinket(trinket);

                    var uiItem = item.GetComponent<TrinketUIItem>();
                    if (uiItem == null) uiItem = item.AddComponent<TrinketUIItem>();
                    uiItem.Initialize(trinket, member, slotIndex);
                }
            }
        }
    }
}

