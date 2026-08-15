using UnityEngine;
using Nevergreen.UI;

namespace Nevergreen.Data
{
    public class HealRoomEffectStrategy : RoomEffectStrategy
    {
        [SerializeField]
        [Tooltip("The Marionette Heal Choice UI prefab to instantiate.")]
        public GameObject healChoicePrefab;

        public override void ExecuteRoomEffect()
        {
            if (healChoicePrefab == null)
            {
                Debug.LogError("[HealRoomEffectStrategy] healChoicePrefab is not assigned!");
                return;
            }

            // Find a Screen-Space Canvas in the scene
            Canvas canvas = null;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            // 1. Try to find the main UI Canvas by name
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
                Debug.LogError("[HealRoomEffectStrategy] No Screen-Space Canvas found in the scene!");
                return;
            }

            GameObject instance = Object.Instantiate(healChoicePrefab, canvas.transform);
            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            MarionetteHealChoiceController controller = instance.GetComponent<MarionetteHealChoiceController>();
            if (controller == null)
            {
                controller = instance.AddComponent<MarionetteHealChoiceController>();
            }
            
            controller.Initialize(RunSessionManager.CurrentParty);

            Debug.Log("[HealRoomEffectStrategy] Marionette Heal Choice UI instantiated.");
        }
    }
}
