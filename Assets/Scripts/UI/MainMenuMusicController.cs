using UnityEngine;
using Nevergreen.Audio;

namespace Nevergreen.UI
{
    /// <summary>
    /// Controller placed in the Main Menu scene to automatically transition
    /// the background music to the Main Menu music upon scene load.
    /// </summary>
    public class MainMenuMusicController : MonoBehaviour
    {
        private void Start()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.config != null)
            {
                AudioManager.Instance.TransitionToBGM(AudioManager.Instance.config.defaultMainMenuMusic);
            }
        }
    }
}
