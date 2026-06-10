using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Central registry of all available playable Marionettes (CharacterData).
    /// </summary>
    [CreateAssetMenu(fileName = "NewMarionetteDatabase", menuName = "Nevergreen/Data/Marionette Database")]
    public class MarionetteDatabase : ScriptableObject
    {
        [Tooltip("All playable Marionettes in the game.")]
        public List<CharacterData> marionettes = new List<CharacterData>();
    }
}
