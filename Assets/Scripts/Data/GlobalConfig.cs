using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Global non-combat tuning variables. Single instance used across the game,
    /// accessible via GameDatabase.Instance.GlobalConfig.
    /// Values derived from GDD.
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalConfig", menuName = "Nevergreen/Data/Global Config")]
    public class GlobalConfig : ScriptableObject
    {
        [Header("Character Progression")]
        [Tooltip("Maximum perfections per character (GDD: 3).")]
        public int maxPerfections = 3;

        [Tooltip("Maximum imperfections per character (GDD: 3).")]
        public int maxImperfections = 3;

        [Header("Room Rewards")]
        [Tooltip("Number of marionettes presented as choices when finding a new marionette (GDD: 4).")]
        public int marionetteChoiceCount = 4;

        [Tooltip("Number of rooms presented as choices at the end of each battle (GDD: 3).")]
        public int roomChoiceCount = 3;
    }
}
