using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
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
        }

        private void Unsubscribe()
        {
            if (_target == null) return;
            _target.OnDamageTaken -= HandleDamage;
            _target.OnHealed -= HandleHeal;
            _target.OnStatsChanged -= HandleStatsChanged;
        }

        private void HandleDamage(CombatCharacter c, int amount)
        {
            AnimateHPChange();
        }

        private void HandleHeal(CombatCharacter c, int amount)
        {
            AnimateHPChange();
        }

        private void HandleStatsChanged(CombatCharacter c) { Refresh(); }

        private void AnimateHPChange()
        {
            if (_target == null) return;

            float targetRatio = _target.baseStats != null && _target.baseStats.maxHP > 0
                ? (float)_target.currentHP / _target.baseStats.maxHP
                : 0f;

            Tween tween = null;
            float duration = 0.5f;

            if (hpSlider != null)
            {
                tween = hpSlider.DOValue(targetRatio, duration);
            }
            else if (fillImage != null)
            {
                tween = fillImage.DOFillAmount(targetRatio, duration);
            }

            if (tween != null)
            {
                int maxHP = _target.baseStats?.maxHP ?? 0;
                tween.OnUpdate(() =>
                {
                    float currentRatio = hpSlider != null ? hpSlider.value : (fillImage != null ? fillImage.fillAmount : targetRatio);
                    if (fillImage != null)
                    {
                        fillImage.color = Color.Lerp(lowColor, fullColor, currentRatio);
                    }
                    if (hpText != null)
                    {
                        int displayedHP = Mathf.RoundToInt(currentRatio * maxHP);
                        hpText.text = $"{displayedHP}/{maxHP}";
                    }
                });

                tween.OnComplete(() =>
                {
                    Refresh();
                });

                if (_animationQueue != null)
                {
                    _animationQueue.Enqueue(new DOTweenStep($"{_target.DisplayName} UI HP Update", tween, duration));
                }
                else
                {
                    tween.Play();
                }
            }
            else
            {
                Refresh();
            }
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
