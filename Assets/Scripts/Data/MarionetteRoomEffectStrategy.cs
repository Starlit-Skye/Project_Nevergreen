using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Concrete room effect strategy for the Marionette Selection room.
    /// Instantiates the Marionette Selection UI prefab centered in the Canvas.
    /// </summary>
    [Serializable]
    public class MarionetteRoomEffectStrategy : RoomEffectStrategy
    {
        [Tooltip("The Marionette Selection UI prefab to instantiate.")]
        public GameObject marionetteSelectionPrefab;

        public override void ExecuteRoomEffect()
        {
            if (marionetteSelectionPrefab == null)
            {
                Debug.LogError("[MarionetteRoomEffectStrategy] marionetteSelectionPrefab is not assigned!");
                return;
            }

            // Find a Screen-Space Canvas in the scene
            Canvas canvas = null;
            var canvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

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
                Debug.LogError("[MarionetteRoomEffectStrategy] No Screen-Space Canvas found in the scene!");
                return;
            }

            GameObject instance = GameObject.Instantiate(marionetteSelectionPrefab, canvas.transform);
            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            Debug.Log("[MarionetteRoomEffectStrategy] Marionette Selection UI instantiated.");
        }
    }
}
