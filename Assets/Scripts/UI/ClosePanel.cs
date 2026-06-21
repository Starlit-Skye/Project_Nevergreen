using UnityEngine;

namespace Nevergreen.UI
{
    public class ClosePanel : MonoBehaviour
    {
        [Tooltip("The panel GameObject that will be closed when the Close method is called.")]
        public GameObject panelToClose;

        public void Close()
        {
            if (panelToClose != null)
            {
                panelToClose.SetActive(false);
            }
            else
            {
                // Fall back to disabling the GameObject this script is attached to
                gameObject.SetActive(false);
            }
        }
    }
}
