using UnityEngine;
using TMPro;

namespace Nevergreen.Combat
{
    /// <summary>
    /// UI controller for the enemy skill announcement banner.
    /// Displays the skill name with a single Animator-driven appear/disappear animation.
    /// The component deactivates itself after the total animation duration elapses.
    /// </summary>
    public class EnemySkillBanner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private Animator _animator;

        [Header("Timing")]
        [Tooltip("Duration of the appear phase (seconds). Skill execution begins after this.")]
        [SerializeField] private float _appearDuration = 0.5f;

        [Tooltip("Total duration of the full appear+disappear animation (seconds). Banner deactivates after this.")]
        [SerializeField] private float _totalDuration = 1.5f;

        /// <summary>Duration of the appear phase. Read by BattleSystem to configure the WaitTimerStep.</summary>
        public float AppearDuration => _appearDuration;

        private float _timer;
        private bool _isShowing;

        /// <summary>
        /// Show the banner with the given skill name.
        /// Activates the GameObject, sets the text, and plays the Animator state.
        /// </summary>
        public void Show(string skillName)
        {
            if (_skillNameText != null)
                _skillNameText.text = skillName;

            gameObject.SetActive(true);
            _timer = 0f;
            _isShowing = true;

            if (_animator != null)
                _animator.Play("ShowBanner", 0, 0f);
        }

        private void Update()
        {
            if (!_isShowing) return;

            _timer += Time.deltaTime;

            if (_timer >= _totalDuration)
            {
                _isShowing = false;
                gameObject.SetActive(false);
            }
        }
    }
}
