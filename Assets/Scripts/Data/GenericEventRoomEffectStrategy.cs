using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    [Serializable]
    public class GenericEventRoomEffectStrategy : RoomEffectStrategy
    {
        [SerializeField]
        [Tooltip("List of Generic Event UI prefabs to choose from randomly.")]
        public List<GameObject> eventUiPrefabs = new List<GameObject>();

        public GameObject GetRandomPrefab()
        {
            if (eventUiPrefabs == null || eventUiPrefabs.Count == 0) return null;
            return eventUiPrefabs[UnityEngine.Random.Range(0, eventUiPrefabs.Count)];
        }

        public override void ExecuteRoomEffect()
        {
            GameObject selectedPrefab = GetRandomPrefab();
            if (selectedPrefab == null)
            {
                Debug.LogError("[GenericEventRoomEffectStrategy] No eventUiPrefabs assigned!");
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
                Debug.LogError("[GenericEventRoomEffectStrategy] No Screen-Space Canvas found in the scene!");
                return;
            }

            GameObject instance = GameObject.Instantiate(selectedPrefab, canvas.transform);
            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            Debug.Log($"[GenericEventRoomEffectStrategy] Generic Event UI '{selectedPrefab.name}' instantiated.");
        }
    }
}

