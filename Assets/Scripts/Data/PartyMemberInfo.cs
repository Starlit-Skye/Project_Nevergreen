using System.Collections.Generic;

namespace Nevergreen.Data
{
    /// <summary>
    /// Holds the run-time state for a single party member during a session.
    /// This is the class to extend for future features like gear, passives, and levels.
    /// </summary>
    [System.Serializable]
    public class PartyMemberInfo
    {
        /// <summary>The immutable base template for the character.</summary>
        public CharacterData character;

        /// <summary>The skills the player selected for this character.</summary>
        public List<SkillData> equippedSkills = new List<SkillData>();

        // NOTE FOR FUTURE EXPANSION:
        // public int currentLevel;
        // public List<GearData> equippedGear;
        // public List<PassiveData> activePassives;
        // public int currentHP; (if HP carries over between encounters)
    }
}
