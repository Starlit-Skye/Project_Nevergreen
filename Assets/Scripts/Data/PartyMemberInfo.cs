using System.Collections.Generic;
using System.Linq;

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

        /// <summary>The current level of this character during the run.</summary>
        public int currentLevel = 1;

        /// <summary>The skills the player selected for this character.</summary>
        public List<SkillData> equippedSkills = new List<SkillData>();

        /// <summary>All skills unlocked by the player for this character during the run, including equipped ones.</summary>
        public List<SkillData> unlockedSkills = new List<SkillData>();

        /// <summary>The persistent HP of the character during a run. Null if starting the run at max HP.</summary>
        public int? currentHP;

        /// <summary>The HP of the character at the start of the current room. Used for persistence to decouple mid-battle mutation.</summary>
        public int? preCombatHP;

        /// <summary>Active Perfection traits on this Marionette.</summary>
        public List<TraitData> perfections = new List<TraitData>();

        /// <summary>Active Imperfection traits on this Marionette.</summary>
        public List<TraitData> imperfections = new List<TraitData>();

        /// <summary>Active Trinkets on this Marionette.</summary>
        public List<TrinketData> equippedTrinkets = new List<TrinketData>();

        /// <summary>
        /// Attempts to equip a trinket. Returns false if the trinket is already equipped
        /// (by trinketId) or the capacity limit of 2 is reached.
        /// </summary>
        public bool TryEquipTrinket(TrinketData trinket)
        {
            if (trinket == null) return false;

            // Capacity check: Each character can only equip 2 trinkets max
            int activeCount = equippedTrinkets.Count(t => t != null);
            if (activeCount >= 2) return false;

            // Uniqueness check: Each character cannot equip 2 of the same trinket
            if (equippedTrinkets.Any(t => t != null && t.trinketId == trinket.trinketId)) return false;

            int nullIndex = equippedTrinkets.IndexOf(null);
            if (nullIndex != -1)
            {
                equippedTrinkets[nullIndex] = trinket;
            }
            else
            {
                equippedTrinkets.Add(trinket);
            }
            return true;
        }

        /// <summary>
        /// Attempts to unequip a trinket. Returns false if the trinket cannot be removed (e.g. cursed).
        /// </summary>
        public bool TryUnequipTrinket(TrinketData trinket)
        {
            if (trinket == null) return false;
            
            // Check if cursed/cannot be removed
            if (trinket.cannotBeRemoved) return false;

            int index = equippedTrinkets.IndexOf(trinket);
            if (index != -1)
            {
                equippedTrinkets[index] = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to add a trait. Returns false if the trait is already present
        /// (by traitId) or the relevant list has reached its capacity.
        /// </summary>
        public bool TryAddTrait(TraitData trait)
        {
            if (trait == null) return false;

            var globalConfig = GameDatabase.Instance.GlobalConfig;
            if (globalConfig == null) return false;

            List<TraitData> list;
            int max;

            if (trait.traitType == TraitType.Perfection)
            {
                list = perfections;
                max = globalConfig.maxPerfections;
            }
            else
            {
                list = imperfections;
                max = globalConfig.maxImperfections;
            }

            // Capacity check
            if (list.Count >= max) return false;

            // Uniqueness check (by traitId)
            if (list.Any(t => t.traitId == trait.traitId)) return false;

            // Opposite trait check (direct)
            if (trait.oppositeTrait != null)
            {
                if (perfections.Any(t => t != null && (t == trait.oppositeTrait || t.traitId == trait.oppositeTrait.traitId))) return false;
                if (imperfections.Any(t => t != null && (t == trait.oppositeTrait || t.traitId == trait.oppositeTrait.traitId))) return false;
            }

            // Opposite trait check (reverse)
            if (perfections.Any(t => t != null && t.oppositeTrait != null && (t.oppositeTrait == trait || t.oppositeTrait.traitId == trait.traitId))) return false;
            if (imperfections.Any(t => t != null && t.oppositeTrait != null && (t.oppositeTrait == trait || t.oppositeTrait.traitId == trait.traitId))) return false;

            list.Add(trait);
            return true;
        }

        /// <summary>
        /// Removes a trait by reference. Returns true if the trait was found and removed.
        /// </summary>
        public bool RemoveTrait(TraitData trait)
        {
            if (trait == null) return false;

            if (trait.traitType == TraitType.Perfection)
                return perfections.Remove(trait);
            else
                return imperfections.Remove(trait);
        }
    }
}

