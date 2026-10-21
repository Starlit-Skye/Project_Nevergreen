using UnityEngine;
using TMPro;
using Nevergreen;

namespace Nevergreen.UI
{
    /// <summary>
    /// Displays the current run's exhaustion status in the UI.
    /// Updates automatically whenever ExhaustionRound or ExhaustionLimit changes.
    /// Format: "Exhaustion: {exhaustionRound}/{exhaustionLimit}"
    /// </summary>
    public class ExhaustionDisplayUI : MonoBehaviour
    {
        [Tooltip("The TextMeshPro UI element to display the exhaustion state.")]
        public TextMeshProUGUI textMesh;

        private void Start()
        {
            UpdateDisplay();
        }

        private void OnEnable()
        {
            RunSessionManager.OnExhaustionChanged += UpdateDisplay;
            UpdateDisplay();
        }

        private void OnDisable()
        {
            RunSessionManager.OnExhaustionChanged -= UpdateDisplay;
        }

        private void UpdateDisplay()
        {
            if (textMesh != null)
            {
                textMesh.text = $"Exhaustion: {RunSessionManager.ExhaustionRound}/{RunSessionManager.ExhaustionLimit}";
            }
        }
    }
}
