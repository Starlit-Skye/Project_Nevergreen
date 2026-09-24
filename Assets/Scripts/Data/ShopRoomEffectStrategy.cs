using System;
using UnityEngine;
using Nevergreen.UI;

namespace Nevergreen.Data
{
    /// <summary>
    /// Room effect strategy that instantiates the Shop UI and initializes it with
    /// tier weights, min/max costs for common and uncommon trinkets, and active party info.
    /// </summary>
    [Serializable]
    public class ShopRoomEffectStrategy : RoomEffectStrategy
    {
        [Tooltip("Weight for dropping/rolling a Common tier trinket.")]
        [SerializeField] private float commonTrinketWeight = 70f;

        [Tooltip("Weight for dropping/rolling an Uncommon tier trinket.")]
        [SerializeField] private float uncommonTrinketWeight = 30f;

        [Tooltip("Minimum cost in Scraps for Common trinkets.")]
        [SerializeField] private int minCostCommon = 15;

        [Tooltip("Maximum cost in Scraps for Common trinkets.")]
        [SerializeField] private int maxCostCommon = 30;

        [Tooltip("Minimum cost in Scraps for Uncommon trinkets.")]
        [SerializeField] private int minCostUncommon = 40;

        [Tooltip("Maximum cost in Scraps for Uncommon trinkets.")]
        [SerializeField] private int maxCostUncommon = 70;

        [Tooltip("Prefab for the Shop UI. Must contain ShopUIController.")]
        [SerializeField] private GameObject shopUiPrefab;

        public override void ExecuteRoomEffect()
        {
            var combatUI = UnityEngine.Object.FindFirstObjectByType<Nevergreen.Prototype.CombatUI>();
            if (shopUiPrefab == null)
            {
                Debug.LogError("[ShopRoomEffectStrategy] shopUiPrefab is not assigned! Executing silently.");
                ExecuteSilently(combatUI);
                return;
            }

            Canvas canvas = null;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            // 1. Prioritize "UICanvas"
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
                Debug.LogError("[ShopRoomEffectStrategy] No Screen-Space Canvas found in the scene! Executing silently.");
                ExecuteSilently(combatUI);
                return;
            }

            var uiInstance = UnityEngine.Object.Instantiate(shopUiPrefab, canvas.transform);
            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            var controller = uiInstance.GetComponent<ShopUIController>();
            if (controller != null)
            {
                controller.Initialize(commonTrinketWeight, uncommonTrinketWeight, minCostCommon, maxCostCommon, minCostUncommon, maxCostUncommon, RunSessionManager.CurrentParty);
            }
            else
            {
                Debug.LogError("[ShopRoomEffectStrategy] Instantiated prefab does not have ShopUIController! Executing silently.");
                UnityEngine.Object.Destroy(uiInstance);
                ExecuteSilently(combatUI);
            }
        }

        private void ExecuteSilently(Nevergreen.Prototype.CombatUI combatUI)
        {
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
