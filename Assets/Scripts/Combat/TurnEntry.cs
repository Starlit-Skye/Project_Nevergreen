using System;

namespace Nevergreen.Combat
{
    /// <summary>
    /// One entry in the turn order list.
    /// </summary>
    public class TurnEntry
    {
        public CombatCharacter character;
        public int speed;

        public TurnEntry(CombatCharacter character, int speed)
        {
            this.character = character;
            this.speed = speed;
        }
    }
}
