using UnityEngine;
using UnityEngine.SceneManagement;
using Nevergreen;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    public class RestartRunButton : MonoBehaviour
    {
        public void RestartRun()
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
