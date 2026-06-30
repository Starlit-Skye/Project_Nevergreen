using System;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Specialized status instance for the Stealth effect.
    /// Handles spawning and destroying its own "In Stealth" debug UI text.
    /// </summary>
    [Serializable]
    public class StealthStatusInstance : StatusEffectInstance
    {
        private StealthTextTracker _tracker;

        public StealthStatusInstance(int duration) 
            : base(StatusType.Stealth, 0, duration)
        {
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);
            if (host != null)
            {
                _tracker = host.gameObject.AddComponent<StealthTextTracker>();
                _tracker.Initialize(host);
            }
        }

        public override void OnRemoved()
        {
            if (_tracker != null)
            {
                _tracker.Cleanup();
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_tracker);
                else
                    UnityEngine.Object.DestroyImmediate(_tracker);
                _tracker = null;
            }
            base.OnRemoved();
        }
    }

    /// <summary>
    /// Companion MonoBehaviour to handle the dynamic creation and parenting of the "In Stealth" text
    /// to the target's HPBar once the HPBar is instantiated.
    /// </summary>
    public class StealthTextTracker : MonoBehaviour
    {
        private CombatCharacter _host;
        private GameObject _stealthTextGo;

        public void Initialize(CombatCharacter host)
        {
            _host = host;
        }

        private void Update()
        {
            if (_host == null) return;

            // If the UI text is not created yet, try to find the HPBar and instantiate it
            if (_stealthTextGo == null)
            {
                var hpBar = FindHPBar(_host);
                if (hpBar != null)
                {
                    CreateStealthText(hpBar);
                }
            }
        }

        private void CreateStealthText(Nevergreen.Prototype.HPBar bar)
        {
            _stealthTextGo = new GameObject("StealthText");
            _stealthTextGo.transform.SetParent(bar.transform, false);

            var rectTransform = _stealthTextGo.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, 58f); // Nicely above the NameText (which is at y=33.8)
            rectTransform.sizeDelta = new Vector2(200, 24);

            var textComponent = _stealthTextGo.AddComponent<TMPro.TextMeshProUGUI>();
            textComponent.text = "In Stealth";

            if (bar.nameText != null)
            {
                textComponent.font = bar.nameText.font;
                textComponent.fontSize = 30; // Slightly smaller than nameText (25)
                textComponent.color = new Color(0.4f, 0.7f, 1.0f); // Light blue/cyan stealth color
                textComponent.alignment = TMPro.TextAlignmentOptions.Center;
            }
        }

        private Nevergreen.Prototype.HPBar FindHPBar(CombatCharacter character)
        {
            var hpBars = UnityEngine.Object.FindObjectsOfType<Nevergreen.Prototype.HPBar>();
            foreach (var bar in hpBars)
            {
                var targetField = typeof(Nevergreen.Prototype.HPBar).GetField("_target", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (targetField != null)
                {
                    var target = targetField.GetValue(bar) as CombatCharacter;
                    if (target == character)
                    {
                        return bar;
                    }
                }
            }
            return null;
        }

        public void Cleanup()
        {
            if (_stealthTextGo != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_stealthTextGo);
                else
                    UnityEngine.Object.DestroyImmediate(_stealthTextGo);
                _stealthTextGo = null;
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
