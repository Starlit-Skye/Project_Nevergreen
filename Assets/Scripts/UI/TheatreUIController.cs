using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// UI Controller for the Theatre Room interface.
    /// Manages the repair button text using configuration from GlobalConfig.
    /// </summary>
    public class TheatreUIController : MonoBehaviour
    {
        [Tooltip("The TextMeshPro text component attached to the Theatre Room repair button.")]
        public GameObject fixProjectorButton;

        private void Start()
        {
            UpdateDisplay();
        }

        private void OnEnable()
        {
            if (fixProjectorButton != null)
            {
                var button = fixProjectorButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(OnRepairButtonClicked);
                }
            }
            UpdateDisplay();
        }

        private void OnDisable()
        {
            if (fixProjectorButton != null)
            {
                var button = fixProjectorButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveListener(OnRepairButtonClicked);
                }
            }
        }

        /// <summary>
        /// Called when the fix projector button is clicked.
        /// </summary>
        public void OnRepairButtonClicked()
        {
            int cost = (GameDatabase.Instance != null && GameDatabase.Instance.GlobalConfig != null)
                ? GameDatabase.Instance.GlobalConfig.theatreRoomProjectorRepairCost
                : 0;

            if (RunSessionManager.TrySpendScraps(cost))
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Updates the button text using the repair cost from GlobalConfig.
        /// </summary>
        public void UpdateDisplay()
        {
            int cost = (GameDatabase.Instance != null && GameDatabase.Instance.GlobalConfig != null)
                ? GameDatabase.Instance.GlobalConfig.theatreRoomProjectorRepairCost
                : 0;
            UpdateDisplay(cost);
        }

        /// <summary>
        /// Updates the button text with a specific repair cost value.
        /// </summary>
        /// <param name="cost">The repair cost in Scraps to display.</param>
        public void UpdateDisplay(int cost)
        {
            if (fixProjectorButton != null)
            {
                var button = fixProjectorButton.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = RunSessionManager.Scraps >= cost;
                }

                if (fixProjectorButton.transform.childCount > 0)
                {
                    var textComp = fixProjectorButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    if (textComp != null)
                    {
                        textComp.text = $"Spend {cost} Scraps, unlock a Marionette's skill.";
                    }
                }
            }
        }
    }
}
