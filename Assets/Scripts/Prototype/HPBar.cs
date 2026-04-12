using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Combat;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// World-space HP bar that follows a CombatCharacter.
    /// </summary>
    public class HPBar : MonoBehaviour
    {
        [Header("References")]
        public Slider hpSlider;
        [Tooltip("Optional. Assigned to recolor the bar based on health ratio.")]
        public Image fillImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;

        [Header("Settings")]
        public Vector3 offset = new Vector3(0f, 1.5f, 0f);
        public Color fullColor = new Color(0.2f, 0.8f, 0.2f);
        public Color lowColor = new Color(0.8f, 0.2f, 0.2f);

        private CombatCharacter _target;
        private AnimationQueueProcessor _animationQueue;

        /// <summary>
        /// Initialize with target and animation queue reference.
        /// Used by CombatUI to inject the shared queue.
        /// </summary>
        public void Initialize(CombatCharacter target, AnimationQueueProcessor queue)
        {
            _animationQueue = queue;
            SetTarget(target);
        }

        public void SetTarget(CombatCharacter target)
        {
            if (_target != null)
            {
                Unsubscribe();
            }

            _target = target;

            if (nameText != null)
                nameText.text = target.DisplayName;

            if (_target != null)
            {
                Subscribe();
            }

            Refresh();
            UpdatePosition();
        }

        private void Subscribe()
        {
            if (_target == null) return;
            _target.OnDamageTaken += HandleDamage;
            _target.OnHealed += HandleHeal;
            _target.OnStatsChanged += HandleStatsChanged;
            _target.OnDefeated += HandleStatsChanged;
        }

        private void Unsubscribe()
        {
            if (_target == null) return;
            _target.OnDamageTaken -= HandleDamage;
            _target.OnHealed -= HandleHeal;
            _target.OnStatsChanged -= HandleStatsChanged;
            _target.OnDefeated -= HandleStatsChanged;
        }

        private void HandleDamage(CombatCharacter c, int amount)
        {
            EnqueueHPAnimation("damage");
            Refresh();
        }

        private void HandleHeal(CombatCharacter c, int amount)
        {
            EnqueueHPAnimation("heal");
            Refresh();
        }

        private void HandleStatsChanged(CombatCharacter c) { Refresh(); }

        /// <summary>
        /// Enqueue a short UI animation entry so the input lock stays active
        /// while HP bar visuals update after a skill animation.
        /// </summary>
        private void EnqueueHPAnimation(string type)
        {
            if (_animationQueue == null || _target == null) return;

            _animationQueue.Enqueue(
                $"ui_hp_{type}",
                $"{_target.DisplayName} HP {type}",
                0.5f);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void Refresh()
        {
            if (_target == null) return;

            float ratio = _target.baseStats != null && _target.baseStats.maxHP > 0
                ? (float)_target.currentHP / _target.baseStats.maxHP
                : 0f;

            if (hpSlider != null)
            {
                hpSlider.value = ratio;
            }
            else if (fillImage != null)
            {
                fillImage.fillAmount = ratio;
            }

            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(lowColor, fullColor, ratio);
            }

            if (hpText != null)
            {
                hpText.text = _target.baseStats != null
                    ? $"{_target.currentHP}/{_target.baseStats.maxHP}"
                    : "0/0";
            }

            // Hide if dead
            gameObject.SetActive(_target.IsAlive);
        }

        public void UpdatePosition()
        {
            if (_target == null) return;

            transform.position = _target.transform.position + offset;
        }
    }
}
