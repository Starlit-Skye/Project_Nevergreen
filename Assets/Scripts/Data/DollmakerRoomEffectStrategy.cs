using System;
using UnityEngine;
using Nevergreen.UI;

namespace Nevergreen.Data
{
    /// <summary>
    /// Room effect strategy that instantiates the Dollmaker UI and initializes it.
    /// </summary>
    [Serializable]
    public class DollmakerRoomEffectStrategy : RoomEffectStrategy
    {
        [Tooltip("Cost in Scraps for perfection replacement operations.")]
        [SerializeField] private int scrapCost = 15;

        [Tooltip("Number of perfection replacement options offered.")]
        [SerializeField] private int perfectionReplacementOptionsCount = 3;

        [Tooltip("Prefab for the Dollmaker UI.")]
        [SerializeField] private GameObject dollmakerUiPrefab;

        public override void ExecuteRoomEffect()
        {
            var combatUI = UnityEngine.Object.FindFirstObjectByType<Nevergreen.Prototype.CombatUI>();
            if (dollmakerUiPrefab == null)
            {
                Debug.LogError("[DollmakerRoomEffectStrategy] dollmakerUiPrefab is not assigned! Executing silently.");
                ExecuteSilently(combatUI);
                return;
            }

            Canvas canvas = null;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            // 1. Prefer "UICanvas"
            foreach (var c in canvases)
            {
                if (c.name == "UICanvas" && (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera))
                {
                    canvas = c;
                    break;
                }
            }

            // 2. Fallback to any Screen-Space canvas
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
                Debug.LogError("[DollmakerRoomEffectStrategy] No Screen-Space Canvas found in the scene! Executing silently.");
                ExecuteSilently(combatUI);
                return;
            }

            var uiInstance = UnityEngine.Object.Instantiate(dollmakerUiPrefab, canvas.transform);
            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            var controller = uiInstance.GetComponent<DollmakerUIController>();
            if (controller != null)
            {
                controller.Initialize(scrapCost, perfectionReplacementOptionsCount, RunSessionManager.CurrentParty);
            }
            else
            {
                Debug.LogError("[DollmakerRoomEffectStrategy] Instantiated prefab does not have DollmakerUIController! Executing silently.");
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
