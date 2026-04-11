using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Holds numeric stat values for one character at one level.
    /// Each index in CharacterData.statPerLevel maps to one of these.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatBlock", menuName = "Nevergreen/Data/Stat Block")]
    public class StatBlockData : ScriptableObject
    {
        [Header("Offensive")]
        [Tooltip("Central attack value. Actual roll is 80%-120% of this.")]
        public int attack = 10;

        [Tooltip("Base accuracy percentage.")]
        [Range(0, 100)]
        public int accuracy = 95;

        [Tooltip("Base critical hit chance percentage.")]
        [Range(0, 100)]
        public int critChance = 5;

        [Header("Defensive")]
        [Tooltip("Max hit points at this level.")]
        public int maxHP = 50;

        [Tooltip("Damage reduction percentage.")]
        [Range(0, 100)]
        public int defense = 0;

        [Tooltip("Dodge chance percentage.")]
        [Range(0, 100)]
        public int dodge = 5;

        [Header("Misc")]
        [Tooltip("Determines turn order. Higher = acts earlier.")]
        public int speed = 5;

        [Header("Resistances")]
        [Range(0, 100)] public int bleedResist = 0;
        [Range(0, 100)] public int blightResist = 0;
        [Range(0, 100)] public int stunResist = 0;
        [Range(0, 100)] public int debuffResist = 0;
        [Range(0, 100)] public int moveResist = 0;
    }
}
