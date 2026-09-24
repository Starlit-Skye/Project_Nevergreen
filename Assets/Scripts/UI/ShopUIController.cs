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
    public class ShopUIController : MonoBehaviour
    {
        [Header("Shop Slots")]
        [SerializeField] private List<TrinketInventoryDropHandler> shopSlots;
        [SerializeField] private List<TextMeshProUGUI> priceTexts;
        [SerializeField] private TrinketUIItem trinketUIPrefab;

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
                    uiItem.Initialize(selectedTrinket, null, -1);
                    
                    priceTexts[i].text = $"{cost}";
                }
                else
                {
                    priceTexts[i].text = "SOLD OUT";
                }

                slotCosts.Add(cost);
                slotPurchased.Add(false);
            }
        }
    }
}
