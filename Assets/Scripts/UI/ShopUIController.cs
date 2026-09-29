using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// UI Controller for managing the Shop room interface.
    /// Handles trinket displays, price tags, equipment panel integration, and purchase transactions.
    /// </summary>
    public class ShopUIController : MonoBehaviour, ITrinketEquipContainerUI
    {
        public void RefreshTrinketUI()
        {
            RefreshEquipPanelsPublic();
        }
        [Header("Shop Slots")]
        [SerializeField] private List<TrinketInventoryDropHandler> shopSlots;
        [SerializeField] private List<TextMeshProUGUI> priceTexts;
        [SerializeField] private TrinketUIItem trinketUIPrefab;
        [SerializeField] private UnityEngine.UI.Button leaveShopButton;

        [Header("Party Equipment")]
        [SerializeField] private List<GameObject> trinketEquipPanels;

        // Runtime state for shop items
        private List<int> slotCosts = new List<int>();
        private List<bool> slotPurchased = new List<bool>();

        public void Initialize(
            float commonTrinketWeight,
            float uncommonTrinketWeight,
            int minCostCommon,
            int maxCostCommon,
            int minCostUncommon,
            int maxCostUncommon,
            List<PartyMemberInfo> activeParty)
        {
            slotCosts.Clear();
            slotPurchased.Clear();

            // Populate the shop slots
            for (int i = 0; i < shopSlots.Count; i++)
            {
                if (i >= priceTexts.Count) break;

                // 1. Two-stage tier roll
                float totalWeight = commonTrinketWeight + uncommonTrinketWeight;
                float roll = Random.Range(0f, totalWeight);
                bool isCommon = roll <= commonTrinketWeight;

                var db = GameDatabase.Instance.TrinketDatabase;
                TrinketData selectedTrinket = null;
                int cost = 0;

                if (isCommon && db.CommonTrinkets.Count > 0)
                {
                    selectedTrinket = db.CommonTrinkets[Random.Range(0, db.CommonTrinkets.Count)];
                    cost = Random.Range(minCostCommon, maxCostCommon + 1);
                }
                else if (!isCommon && db.UncommonTrinkets.Count > 0)
                {
                    selectedTrinket = db.UncommonTrinkets[Random.Range(0, db.UncommonTrinkets.Count)];
                    cost = Random.Range(minCostUncommon, maxCostUncommon + 1);
                }
                else if (db.CommonTrinkets.Count > 0) // Fallback if preferred tier is empty
                {
                    selectedTrinket = db.CommonTrinkets[Random.Range(0, db.CommonTrinkets.Count)];
                    cost = Random.Range(minCostCommon, maxCostCommon + 1);
                }

                if (selectedTrinket != null)
                {
                    // Instantiate Trinket UI
                    var uiItem = Instantiate(trinketUIPrefab, shopSlots[i].transform);
                    uiItem.Initialize(selectedTrinket, null, i); // Use 'i' as slot index
                    uiItem.isShopContext = true;
                    uiItem.shopController = this;
                    
                    priceTexts[i].text = $"{cost}";
                }
                else
                {
                    priceTexts[i].text = "SOLD OUT";
                }

                slotCosts.Add(cost);
                slotPurchased.Add(false);
            }

            RefreshEquipPanels(activeParty);
        }

        public bool CanAfford(int shopSlotIndex)
        {
            if (shopSlotIndex < 0 || shopSlotIndex >= slotCosts.Count) return true;
            if (slotPurchased[shopSlotIndex]) return true;
            return RunSessionManager.Scraps >= slotCosts[shopSlotIndex];
        }

        public void OnItemPurchased(int shopSlotIndex)
        {
            if (shopSlotIndex < 0 || shopSlotIndex >= slotCosts.Count) return;
            if (slotPurchased[shopSlotIndex]) return;

            int cost = slotCosts[shopSlotIndex];
            if (RunSessionManager.TrySpendScraps(cost))
            {
                slotPurchased[shopSlotIndex] = true;
                
                if (priceTexts[shopSlotIndex] != null)
                {
                    priceTexts[shopSlotIndex].fontStyle |= FontStyles.Strikethrough;
                }
            }
        }

        public void RefreshEquipPanelsPublic()
        {
            RefreshEquipPanels(RunSessionManager.CurrentParty);
        }

        private void RefreshEquipPanels(List<PartyMemberInfo> activeParty)
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

            if (activeParty == null) return;

            for (int i = 0; i < trinketEquipPanels.Count; i++)
            {
                GameObject panel = trinketEquipPanels[i];
                if (panel == null) continue;

                if (i < activeParty.Count)
                {
                    panel.SetActive(true);
                    PartyMemberInfo member = activeParty[i];

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
            dropHandler.isShopContext = true;
            dropHandler.shopController = this;

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

                if (trinketUIPrefab != null)
                {
                    GameObject item = Instantiate(trinketUIPrefab.gameObject, container);
                    
                    var tooltipTrigger = item.GetComponent<TrinketTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TrinketTooltipTrigger>();
                    tooltipTrigger.SetTrinket(trinket);

                    var uiItem = item.GetComponent<TrinketUIItem>();
                    if (uiItem == null) uiItem = item.AddComponent<TrinketUIItem>();
                    uiItem.Initialize(trinket, member, slotIndex);
                    uiItem.isShopContext = true;
                    uiItem.shopController = this;
                }
            }
        }

        private void OnEnable()
        {
            if (leaveShopButton != null)
            {
                leaveShopButton.onClick.AddListener(OnLeaveShopClicked);
            }
        }

        private void OnDisable()
        {
            if (leaveShopButton != null)
            {
                leaveShopButton.onClick.RemoveListener(OnLeaveShopClicked);
            }
        }

        private void OnLeaveShopClicked()
        {
            // Save state explicitly
            SaveManager.SaveRun();

            // Room completion flow
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
