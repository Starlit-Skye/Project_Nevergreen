using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Global combat tuning variables. Single instance used by BattleSystem.
    /// Values derived from GDD Combat/Stats sections.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "Nevergreen/Data/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        [Header("Attack Roll")]
        [Tooltip("Minimum multiplier for attack roll (GDD: 0.8).")]
        public float attackRollMin = 0.8f;

        [Tooltip("Maximum multiplier for attack roll (GDD: 1.2).")]
        public float attackRollMax = 1.2f;

        [Header("Accuracy & Defense")]
        [Tooltip("Maximum accuracy cap in percent (GDD: 95).")]
        [Range(0, 100)]
        public int accuracyCap = 95;

        [Tooltip("Maximum defense cap in percent (User Request: 95).")]
        [Range(0, 100)]
        public int defenseCap = 95;

        [Tooltip("Maximum dodge cap in percent (User Request: 95).")]
        [Range(0, 100)]
        public int dodgeCap = 95;

        [Header("Critical")]
        [Tooltip("Damage multiplier on critical hit (GDD: 1.5).")]
        public float critDamageMultiplier = 1.5f;

        [Header("Stun")]
        [Tooltip("Stun resistance bonus after stun expires (GDD: +300%).")]
        public int stunRecoveryResistBonus = 300;

        [Header("Speed Roll")]
        [Tooltip("Minimum random speed bonus roll (inclusive).")]
        public int speedRollMin = 1;

        [Tooltip("Maximum random speed bonus roll (inclusive).")]
        public int speedRollMax = 4;

        [Header("Team")]
        [Tooltip("Maximum party size per team (GDD: 4).")]
        [Range(1, 4)]
        public int maxPartySize = 4;

        [Tooltip("Number of ranks per team (GDD: 4).")]
        public int rankCount = 4;

        [Header("Leveling")]
        [Tooltip("Global maximum character level.")]
        public int globalMaxLevel = 10;

        [Header("Traits (Perfections / Imperfections)")]
        [Tooltip("Maximum number of Perfections a single Marionette can have.")]
        [Min(0)]
        public int maxPerfections = 3;

        [Tooltip("Maximum number of Imperfections a single Marionette can have.")]
        [Min(0)]
        public int maxImperfections = 3;

        [Header("Marionette Selection UI")]
        [Tooltip("Number of Marionette choices displayed to the player.")]
        public int marionetteChoiceCount = 4;

        [Header("Room Selection")]
        [Tooltip("Number of room choices displayed to the player after victory.")]
        public int roomChoiceCount = 3;

        [Tooltip("Pool of available room types the player can choose from.")]
        public System.Collections.Generic.List<RoomData> availableRooms = new System.Collections.Generic.List<RoomData>();
    }
}
