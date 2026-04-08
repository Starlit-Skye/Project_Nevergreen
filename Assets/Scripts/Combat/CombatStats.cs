using System;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Runtime resolved stats for a combat character.
    /// Built from StatBlockData + modifiers from buffs, debuffs, trinkets, perfections, imperfections.
    /// </summary>
    [Serializable]
    public class CombatStats
    {
        public int attack;
        public int defense;
        public int accuracy;
        public int dodge;
        public int critChance;
        public int speed;
        public int maxHP;

        // Resistances
        public int bleedResist;
        public int blightResist;
        public int stunResist;
        public int debuffResist;
        public int moveResist;

        public CombatStats() { }

        /// <summary>
        /// Initialize from a StatBlockData snapshot.
        /// </summary>
        public CombatStats(Data.StatBlockData block)
        {
            if (block == null) return;
            attack = block.attack;
            defense = block.defense;
            accuracy = block.accuracy;
            dodge = block.dodge;
            critChance = block.critChance;
            speed = block.speed;
            maxHP = block.maxHP;
            bleedResist = block.bleedResist;
            blightResist = block.blightResist;
            stunResist = block.stunResist;
            debuffResist = block.debuffResist;
            moveResist = block.moveResist;
        }

        /// <summary>
        /// Deep copy.
        /// </summary>
        public CombatStats Clone()
        {
            return (CombatStats)MemberwiseClone();
        }
    }
}
