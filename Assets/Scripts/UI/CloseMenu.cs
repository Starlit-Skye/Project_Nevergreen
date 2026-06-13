using UnityEngine;

public class CloseMenu : MonoBehaviour
{
    public GameObject parent;

    public void Close()
    {
        parent.SetActive(false);
    }
}
