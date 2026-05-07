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
        public Color aliveColor = new Color(0.8f, 0.2f, 0.2f); // Red
        public Color pileColor = new Color(0.4f, 0.4f, 0.4f);  // Gray

        private CombatCharacter _target;
        private AnimationQueueProcessor _animationQueue;

        private int _currentMaxHP;
        private Color _currentColor;

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

            UpdateStateConfig();
            Refresh();
            UpdatePosition();
        }

        private void Subscribe()
        {
            if (_target == null) return;
            _target.OnDamageTaken += HandleDamage;
            _target.OnHealed += HandleHeal;
            _target.OnStatsChanged += HandleStatsChanged;
            _target.OnStateChanged += HandleStateChanged;
        }

        private void Unsubscribe()
        {
            if (_target == null) return;
            _target.OnDamageTaken -= HandleDamage;
            _target.OnHealed -= HandleHeal;
            _target.OnStatsChanged -= HandleStatsChanged;
            _target.OnStateChanged -= HandleStateChanged;
        }

        private void HandleDamage(CombatCharacter c, int amount)
        {
            AnimateHPChange();
        }

        private void HandleHeal(CombatCharacter c, int amount)
        {
            AnimateHPChange();
        }

        private void HandleStatsChanged(CombatCharacter c) 
        { 
            Refresh(); 
        }

        private void HandleStateChanged(CombatCharacter c, LifeState state) 
        { 
            UpdateStateConfig();
            Refresh(); 
        }

        private void UpdateStateConfig()
        {
            if (_target == null) return;

            _currentMaxHP = _target.baseStats?.maxHP ?? 0;
            if (_target.IsPile)
            {
                _currentMaxHP /= 2;
                _currentColor = pileColor;
            }
            else
            {
                _currentColor = aliveColor;
            }

            if (fillImage != null)
                fillImage.color = _currentColor;
        }

        private void AnimateHPChange()
        {
            if (_target == null) return;

            float targetRatio = _currentMaxHP > 0
                ? (float)_target.currentHP / _currentMaxHP
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
                tween.OnUpdate(() =>
                {
                    float currentRatio = hpSlider != null ? hpSlider.value : (fillImage != null ? fillImage.fillAmount : targetRatio);
                    if (hpText != null)
                    {
                        int displayedHP = Mathf.RoundToInt(currentRatio * _currentMaxHP);
                        hpText.text = $"{displayedHP}/{_currentMaxHP}";
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

            float ratio = _currentMaxHP > 0
                ? (float)_target.currentHP / _currentMaxHP
                : 0f;

            if (hpSlider != null)
            {
                hpSlider.value = ratio;
            }
            else if (fillImage != null)
            {
                fillImage.fillAmount = ratio;
            }

            if (hpText != null)
            {
                hpText.text = _currentMaxHP > 0
                    ? $"{_target.currentHP}/{_currentMaxHP}"
                    : "0/0";
            }

            // Hide if destroyed or dying (if we want dying to hide immediately)
            // But keep it for Alive and Pile.
            gameObject.SetActive(_target.IsAlive || _target.IsPile);
        }

        public void UpdatePosition()
        {
            if (_target == null) return;

            transform.position = _target.transform.position + offset;
        }
    }
}
