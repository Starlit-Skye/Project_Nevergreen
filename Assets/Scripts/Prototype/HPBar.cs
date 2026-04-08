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
        public Image fillImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;

        [Header("Settings")]
        public Vector3 offset = new Vector3(0f, 1.5f, 0f);
        public Color fullColor = new Color(0.2f, 0.8f, 0.2f);
        public Color lowColor = new Color(0.8f, 0.2f, 0.2f);

        private CombatCharacter _target;

        public void SetTarget(CombatCharacter target)
        {
            _target = target;

            if (nameText != null)
                nameText.text = target.DisplayName;

            Refresh();
            UpdatePosition();
        }

        public void Refresh()
        {
            if (_target == null) return;

            float ratio = _target.baseStats != null && _target.baseStats.maxHP > 0
                ? (float)_target.currentHP / _target.baseStats.maxHP
                : 0f;

            if (fillImage != null)
            {
                fillImage.fillAmount = ratio;
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
