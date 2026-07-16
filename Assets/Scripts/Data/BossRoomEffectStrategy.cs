using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nevergreen.Data
{
    /// <summary>
    /// Concrete room effect strategy for the Boss Room.
    /// Instantiates a "Run Completed" panel and the EndRun UI prefab upon combat victory.
    /// </summary>
    [Serializable]
    public class BossRoomEffectStrategy : RoomEffectStrategy
    {
        [Tooltip("The prefab for the EndRun button (Assets/Prefabs/UI/EndRun.prefab).")]
        public GameObject endRunPrefab;

        public override void ExecuteRoomEffect()
        {
            if (endRunPrefab == null)
            {
                Debug.LogError("[BossRoomEffectStrategy] endRunPrefab is not assigned!");
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
                Debug.LogError("[BossRoomEffectStrategy] No Screen-Space Canvas found in the scene!");
                return;
            }

            // 1. Create Container Panel
            GameObject panelGo = new GameObject("BossRoomVictoryPanel");
            panelGo.transform.SetParent(canvas.transform, false);

            RectTransform panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panelRt.localScale = Vector3.one;

            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f); // Dark semi-transparent background

            // 2. Create Text
            GameObject textGo = new GameObject("RunCompletedText");
            textGo.transform.SetParent(panelGo.transform, false);

            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = new Vector2(0f, 100f);
            textRt.sizeDelta = new Vector2(600f, 100f);

            TextMeshProUGUI tmpText = textGo.AddComponent<TextMeshProUGUI>();
            tmpText.text = "Run Completed";
            tmpText.fontSize = 48f;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.enableWordWrapping = false;

            // 3. Instantiate EndRun Prefab
            GameObject endRunInstance = GameObject.Instantiate(endRunPrefab, panelGo.transform);
            RectTransform buttonRt = endRunInstance.GetComponent<RectTransform>();
            if (buttonRt != null)
            {
                buttonRt.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRt.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRt.pivot = new Vector2(0.5f, 0.5f);
                buttonRt.anchoredPosition = new Vector2(0f, -50f);
                buttonRt.localScale = Vector3.one;
            }

            Debug.Log("[BossRoomEffectStrategy] Boss Room Victory UI instantiated.");
        }
    }
}
