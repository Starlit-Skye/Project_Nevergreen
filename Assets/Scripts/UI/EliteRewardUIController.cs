using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    public class EliteRewardUIController : MonoBehaviour, ITrinketEquipContainerUI
    {
        public void RefreshTrinketUI()
        {
            RefreshPanels();
        }

        public void OnUnassignedTrinketChanged(TrinketData oldTrinket, TrinketData newTrinket)
        {
            _rolledTrinket = newTrinket;
        }
        [Header("UI Elements")]
        [SerializeField] private Button closeButton;

        [Header("Trinket UI")]
        [SerializeField] private GameObject trinketUIItemPrefab;
        [SerializeField] private Transform trinketDroppedContainer;
        [SerializeField] private List<GameObject> trinketEquipPanels;

        private TrinketData _rolledTrinket;
        private List<PartyMemberInfo> _party;

        public void Initialize(TrinketData rolledTrinket, List<PartyMemberInfo> party = null)
        {
            _rolledTrinket = rolledTrinket;
            _party = party ?? RunSessionManager.CurrentParty;

            if (_rolledTrinket != null && trinketUIItemPrefab != null && trinketDroppedContainer != null)
            {
                // Clear old drops
                foreach (Transform child in trinketDroppedContainer)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }

                var item = Instantiate(trinketUIItemPrefab, trinketDroppedContainer);
                var uiItem = item.GetComponent<TrinketUIItem>();
                if (uiItem != null)
                {
                    uiItem.Initialize(_rolledTrinket);
                }
            }

            RefreshPanels();
        }

        public void RefreshPanels()
        {
            if (trinketEquipPanels == null || trinketEquipPanels.Count == 0)
            {
                trinketEquipPanels = new List<GameObject>();
                var nameTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var nt in nameTexts)
                {
                    if (nt.name == "TrinketEquipMarionetteName")
                    {
                        trinketEquipPanels.Add(nt.transform.parent.gameObject);
                    }
                }
            }

            if (_party == null) return;

            for (int i = 0; i < trinketEquipPanels.Count; i++)
            {
                GameObject panel = trinketEquipPanels[i];
                if (panel == null) continue;

                if (i < _party.Count)
                {
                    panel.SetActive(true);
                    PartyMemberInfo member = _party[i];

                    // Set Marionette Name
                    var nameText = panel.transform.Find("TrinketEquipMarionetteName")?.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = member.character != null ? member.character.displayName : "Marionette";
                    }

                    // Setup Trinket 1
                    var trinket1 = panel.transform.Find("Trinket1");
                    if (trinket1 != null)
                    {
                        SetupTrinketSlot(trinket1, member, 0);
                    }

                    // Setup Trinket 2
                    var trinket2 = panel.transform.Find("Trinket2");
                    if (trinket2 != null)
                    {
                        SetupTrinketSlot(trinket2, member, 1);
                    }
                }
                else
                {
                    panel.SetActive(false);
                }
            }
        }

        private void SetupTrinketSlot(Transform container, PartyMemberInfo member, int slotIndex)
        {
            var dropHandler = container.GetComponent<TrinketSlotDropHandler>();
            if (dropHandler == null) dropHandler = container.gameObject.AddComponent<TrinketSlotDropHandler>();
            
            dropHandler.TargetMember = member;
            dropHandler.TargetSlotIndex = slotIndex;

            // Clear existing UI items
            foreach (Transform child in container)
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            if (member.equippedTrinkets != null && slotIndex < member.equippedTrinkets.Count)
            {
                var trinket = member.equippedTrinkets[slotIndex];
                if (trinket == null) return;

                if (trinketUIItemPrefab != null)
                {
                    GameObject item = Instantiate(trinketUIItemPrefab, container);
                    
                    var tooltipTrigger = item.GetComponent<TrinketTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TrinketTooltipTrigger>();
                    tooltipTrigger.SetTrinket(trinket);

                    var uiItem = item.GetComponent<TrinketUIItem>();
                    if (uiItem == null) uiItem = item.AddComponent<TrinketUIItem>();
                    uiItem.Initialize(trinket, member, slotIndex);
                }
            }
        }

        private void OnEnable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnCloseClicked()
        {
            SaveManager.SaveRun();

            gameObject.SetActive(false);

            var combatUI = UnityEngine.Object.FindFirstObjectByType<Nevergreen.Prototype.CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                if (!RunSessionManager.RoomCompleted)
                {
                    RunSessionManager.CompleteRoom(new System.Collections.Generic.List<RoomData>());
                }
            }
        }
    }
}
