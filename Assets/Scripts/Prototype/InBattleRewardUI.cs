using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// Standalone popup for mid-combat rewards (e.g. parts gained from skill effects).
    /// Has no dependencies on CombatUI, BattleSystem, or any other system.
    /// Panel appears, shows how many parts the player gained, and hides on close.
    /// </summary>
    public class InBattleRewardUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject panel;
        public TextMeshProUGUI rewardText;
        public Button closeButton;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        /// <summary>
        /// Shows the reward popup with the given number of parts.
        /// </summary>
        public void Show(int parts)
        {
            Debug.Log("Activating battle reward popup");
            if (panel != null)
                panel.SetActive(true);

            if (rewardText != null)
                rewardText.text = $"You found {parts} Parts!";
        }

        private void Close()
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
