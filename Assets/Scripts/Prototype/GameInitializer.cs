using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// Bootstrap MonoBehaviour that initializes the centralized GameDatabase singleton
    /// at runtime. Place this on a GameObject in the first loaded scene (e.g. MainMenu).
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GameDatabase gameDatabase;

        private void Awake()
        {
            if (gameDatabase != null)
            {
                GameDatabase.Initialize(gameDatabase);
            }
            else
            {
                Debug.LogError("[GameInitializer] GameDatabase reference is not assigned!");
            }
        }
    }
}
