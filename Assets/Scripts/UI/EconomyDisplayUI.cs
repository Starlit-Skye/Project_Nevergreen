using UnityEngine;
using TMPro;
using Nevergreen;

namespace Nevergreen.UI
{
    public class EconomyDisplayUI : MonoBehaviour
    {
        [Tooltip("The TextMeshPro UI element to display the economy values.")]
        public TextMeshProUGUI textMesh;

        private void Start()
        {
            UpdateDisplay();
        }

        private void OnEnable()
        {
            RunSessionManager.OnPartsChanged += UpdateDisplay;
            RunSessionManager.OnScrapsChanged += UpdateDisplay;
            UpdateDisplay();
        }

        private void OnDisable()
        {
            RunSessionManager.OnPartsChanged -= UpdateDisplay;
            RunSessionManager.OnScrapsChanged -= UpdateDisplay;
        }

        private void UpdateDisplay()
        {
            if (textMesh != null)
            {
                textMesh.text = $"Parts: {RunSessionManager.Parts} Scraps: {RunSessionManager.Scraps}";
            }
        }
    }
}
