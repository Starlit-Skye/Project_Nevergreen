#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Nevergreen;
using Nevergreen.Data;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// Debug utility for the Unity Editor.
    /// Pressing 'R' immediately ends the current run and returns to the main menu.
    /// </summary>
    public static class EditorDebugUtility
    {
        private class DebugBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    // Check if we are not already in MainMenu to avoid redundant transitions
                    if (SceneManager.GetActiveScene().name == "MainMenu")
                    {
                        return;
                    }

                    Debug.Log("[EditorDebugUtility] 'R' pressed. Immediately ending the run and returning to Main Menu...");
                    
                    // Clear the active run save state (deletes/resets save file)
                    SaveManager.ClearActiveRun();

                    // Clear the static run session data
                    RunSessionManager.Clear();

                    // Return to Main Menu
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject("EditorDebugUtility_Helper");
            go.AddComponent<DebugBehaviour>();
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}
#endif
