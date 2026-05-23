using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// Dynamic, beautiful Canvas-based selection overlay for playtesting battle variants.
    /// Blocks gameplay input and displays configurable buttons for each team variant.
    /// </summary>
    public class VariantSelectionOverlay : MonoBehaviour
    {
        private Action<int> _onVariantSelected;
        private List<BattleVariant> _variants;

        /// <summary>
        /// Creates and instantiates a styled VariantSelectionOverlay Canvas in the scene.
        /// </summary>
        public static VariantSelectionOverlay Create(List<BattleVariant> variants, Action<int> onSelect)
        {
            // Ensure EventSystem exists so UI interacts correctly
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create root canvas GameObject
            var canvasGo = new GameObject("VariantSelectionCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Render on top of standard combat UI

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var overlay = canvasGo.AddComponent<VariantSelectionOverlay>();
            overlay._variants = variants;
            overlay._onVariantSelected = onSelect;
            overlay.BuildUI(canvasGo.transform);

            return overlay;
        }

        private void BuildUI(Transform parent)
        {
            // 1. Full screen dark backdrop
            var backdropGo = new GameObject("Backdrop");
            backdropGo.transform.SetParent(parent, false);
            var backdropImg = backdropGo.AddComponent<Image>();
            backdropImg.color = new Color(0.04f, 0.06f, 0.10f, 0.96f); // Slate-950 very dark blue-grey

            var rectBackdrop = backdropGo.GetComponent<RectTransform>();
            rectBackdrop.anchorMin = Vector2.zero;
            rectBackdrop.anchorMax = Vector2.one;
            rectBackdrop.sizeDelta = Vector2.zero;

            // 2. Center card parent (Teal Outline/Border glow effect)
            var cardBorderGo = new GameObject("CardBorder");
            cardBorderGo.transform.SetParent(parent, false);
            var borderImg = cardBorderGo.AddComponent<Image>();
            borderImg.color = new Color(0.11f, 0.74f, 0.67f, 1f); // Neon Teal/Cyan #1cbdab

            var rectBorder = cardBorderGo.GetComponent<RectTransform>();
            rectBorder.sizeDelta = new Vector2(504, 604); // Slightly larger than content for 2px border

            // 3. Center card background
            var cardBgGo = new GameObject("CardBackground");
            cardBgGo.transform.SetParent(cardBorderGo.transform, false);
            var cardBgImg = cardBgGo.AddComponent<Image>();
            cardBgImg.color = new Color(0.08f, 0.11f, 0.18f, 1f); // Slate-900 dark gray-blue #141c2e

            var rectBg = cardBgGo.GetComponent<RectTransform>();
            rectBg.anchorMin = Vector2.zero;
            rectBg.anchorMax = Vector2.one;
            rectBg.sizeDelta = new Vector2(-4, -4); // 2px margin on all sides for the border

            // 4. Content layout
            var contentGo = new GameObject("ContentLayout");
            contentGo.transform.SetParent(cardBgGo.transform, false);
            var rectContent = contentGo.AddComponent<RectTransform>();
            rectContent.anchorMin = Vector2.zero;
            rectContent.anchorMax = Vector2.one;
            rectContent.sizeDelta = new Vector2(-40, -40); // 20px padding

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            // 5. Title
            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(contentGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(460, 40);

            var titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.text = "SELECT BATTLE VARIANT";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.17f, 0.83f, 0.75f, 1f); // Glowing Teal
            AssignDefaultFont(titleText);

            // 6. Subtitle
            var subGo = new GameObject("SubtitleText");
            subGo.transform.SetParent(contentGo.transform, false);
            var subRect = subGo.AddComponent<RectTransform>();
            subRect.sizeDelta = new Vector2(460, 30);

            var subText = subGo.AddComponent<TextMeshProUGUI>();
            subText.text = "Choose an enemy team variant for playtesting.";
            subText.alignment = TextAlignmentOptions.Center;
            subText.fontSize = 14;
            subText.color = new Color(0.58f, 0.64f, 0.72f, 1f); // Slate-400
            AssignDefaultFont(subText);

            // Add spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(contentGo.transform, false);
            var spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(460, 10);

            // 7. Buttons
            for (int i = 0; i < _variants.Count; i++)
            {
                int index = i;
                string name = _variants[i].variantName;
                if (string.IsNullOrEmpty(name)) name = $"Battle Variant {i + 1}";

                var btnGo = new GameObject($"VariantButton_{i}");
                btnGo.transform.SetParent(contentGo.transform, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.sizeDelta = new Vector2(440, 56);

                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = Color.white; // Needed for ColorBlock tinting to apply

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.transition = Selectable.Transition.ColorTint;

                var cb = new ColorBlock();
                cb.normalColor = new Color(0.15f, 0.20f, 0.29f, 1f);      // Slate-800 #26334a
                cb.highlightedColor = new Color(0.11f, 0.74f, 0.67f, 1f); // Neon Teal #1cbdab
                cb.pressedColor = new Color(0.07f, 0.53f, 0.48f, 1f);     // Teal-600 #12877a
                cb.selectedColor = cb.normalColor;
                cb.disabledColor = new Color(0.08f, 0.11f, 0.18f, 0.5f);
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.1f;
                btn.colors = cb;

                // Handle Button Click Action
                btn.onClick.AddListener(() =>
                {
                    _onVariantSelected?.Invoke(index);
                    Destroy(gameObject); // Destroys the Canvas containing the selection screen
                });

                // Button Text
                var btnTextGo = new GameObject("Text");
                btnTextGo.transform.SetParent(btnGo.transform, false);
                var textRect = btnTextGo.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
                btnText.text = name;
                btnText.alignment = TextAlignmentOptions.Center;
                btnText.fontSize = 18;
                btnText.fontStyle = FontStyles.Bold;
                btnText.color = Color.white;
                AssignDefaultFont(btnText);
            }
        }

        private void AssignDefaultFont(TextMeshProUGUI tmpText)
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
            {
                tmpText.font = defaultFont;
            }
        }
    }
}
