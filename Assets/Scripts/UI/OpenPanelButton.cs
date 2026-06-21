using UnityEngine;
using UnityEngine.UI;

namespace Nevergreen.UI
{
    [RequireComponent(typeof(Button))]
    public class OpenPanelButton : MonoBehaviour
    {
        [Tooltip("The panel to set active when this button is clicked.")]
        public GameObject panelToOpen;

        private void Start()
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnButtonClicked()
        {
            if (panelToOpen != null)
            {
                panelToOpen.SetActive(true);
            }
        }
    }
}
