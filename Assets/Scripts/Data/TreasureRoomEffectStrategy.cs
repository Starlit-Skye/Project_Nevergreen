using System;
using UnityEngine;
using Nevergreen.UI;

namespace Nevergreen.Data
{
    /// <summary>
    /// Room effect strategy that grants Scraps and Parts to the player via a Treasure UI.
    /// Typically used in a Treasure room.
    /// </summary>
    [Serializable]
    public class TreasureRoomEffectStrategy : RoomEffectStrategy
    {
        [Tooltip("Minimum amount of Scraps awarded.")]
        [SerializeField] private int minScraps = 10;
        
        [Tooltip("Maximum amount of Scraps awarded.")]
        [SerializeField] private int maxScraps = 30;

        [Tooltip("Minimum amount of Parts awarded.")]
        [SerializeField] private int minParts = 5;

        [Tooltip("Maximum amount of Parts awarded.")]
        [SerializeField] private int maxParts = 15;

        [Tooltip("Weight for dropping a Common tier trinket. Set to 0 to disable Common trinkets.")]
        [SerializeField] private float commonTrinketWeight = 70f;

        [Tooltip("Weight for dropping an Uncommon tier trinket. Set to 0 to disable Uncommon trinkets.")]
        [SerializeField] private float uncommonTrinketWeight = 30f;

        [Tooltip("Prefab for the dedicated Treasure UI. Must contain TreasureUIController.")]
        [SerializeField] private GameObject treasureUiPrefab;

        public override void ExecuteRoomEffect()
        {
            var combatUI = UnityEngine.Object.FindFirstObjectByType<Nevergreen.Prototype.CombatUI>();
            if (treasureUiPrefab == null)
            {
                Debug.LogError("[TreasureRoomEffectStrategy] treasureUiPrefab is not assigned! Executing silently.");
                ExecuteSilently(combatUI);
                return;
            }

            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas canvas = null;

            // 1. Prefer "UICanvas"
            foreach (var c in canvases)
            {
                if (c.name == "UICanvas" && (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera))
                {
                    canvas = c;
                    break;
                }
            }

            // 2. Fallback to any Screen-Space canvas if "UICanvas" is missing
            if (canvas == null)
            {
                foreach (var c in canvases)
                {
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        canvas = c;
                        break;
                    }
                }
            }

            if (canvas == null)
            {
                Debug.LogError("[TreasureRoomEffectStrategy] No Screen-Space Canvas found in the scene! Executing silently.");
                ExecuteSilently(combatUI);
                return;
            }

            // Instantiate and initialize the dedicated UI
            var uiInstance = UnityEngine.Object.Instantiate(treasureUiPrefab, canvas.transform);
            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            
            var controller = uiInstance.GetComponent<TreasureUIController>();
            if (controller != null)
            {
                var rolledTrinket = RollTrinketReward();
                controller.Initialize(minScraps, maxScraps, minParts, maxParts, rolledTrinket, RunSessionManager.CurrentParty);
            }
            else
            {
                Debug.LogError("[TreasureRoomEffectStrategy] Instantiated prefab does not have TreasureUIController! Executing silently.");
                UnityEngine.Object.Destroy(uiInstance);
                ExecuteSilently(combatUI);
            }
        }

        private void ExecuteSilently(Nevergreen.Prototype.CombatUI combatUI)
        {
            var rng = new System.Random();
            int amountScraps = rng.Next(minScraps, maxScraps + 1);
            int amountParts = rng.Next(minParts, maxParts + 1);
            
            RunSessionManager.GrantScraps(amountScraps);
            RunSessionManager.GrantParts(amountParts);
            SaveManager.SaveRun();
            
            Debug.Log($"[TreasureRoomEffectStrategy] Silently awarded {amountScraps} Scraps and {amountParts} Parts.");

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

        /// <summary>
        /// Rolls a random trinket based on the configured tier weights.
        /// Returns null if the rolled tier list is empty, or if TrinketDatabase is unavailable.
        /// </summary>
        public TrinketData RollTrinketReward(System.Random rng = null)
        {
            var db = GameDatabase.Instance;
            if (db == null || db.TrinketDatabase == null) return null;

            if (rng == null) rng = new System.Random();

            float totalWeight = commonTrinketWeight + uncommonTrinketWeight;
            if (totalWeight <= 0f) return null;

            double roll = rng.NextDouble() * totalWeight;
            bool isCommon = roll < commonTrinketWeight;

            var targetList = isCommon ? db.TrinketDatabase.CommonTrinkets : db.TrinketDatabase.UncommonTrinkets;

            if (targetList == null || targetList.Count == 0)
            {
                return null;
            }

            int index = rng.Next(targetList.Count);
            return targetList[index];
        }
    }
}
