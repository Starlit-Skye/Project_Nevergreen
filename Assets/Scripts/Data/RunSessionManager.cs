using System.Collections.Generic;
using Nevergreen.Data;

namespace Nevergreen
{
    /// <summary>
    /// Static session manager that persists party and encounter configuration across scenes.
    /// Populated by the Main Menu skill selection, consumed by CombatCharacter.InitializeForCombat.
    /// </summary>
    public static class RunSessionManager
    {
        /// <summary>The active roster for the current run.</summary>
        public static List<PartyMemberInfo> CurrentParty { get; set; } = new List<PartyMemberInfo>();

        /// <summary>The enemy formation database for the current run.</summary>
        public static EnemyFormationDatabase ActiveFormationDatabase { get; private set; }

        /// <summary>The trait database for the current run.</summary>
        public static TraitDatabase ActiveTraitDatabase { get; private set; }

        /// <summary>The last formation selected, used to prevent consecutive duplicates.</summary>
        public static EnemyFormationData LastSelectedFormation { get; private set; }

        private static System.Random _rng = new System.Random();

        /// <summary>
        /// Initializes the databases for a new run.
        /// Called by the Main Menu bootstrapper before loading the combat scene.
        /// </summary>
        public static void Initialize(EnemyFormationDatabase database, TraitDatabase traitDatabase = null)
        {
            ActiveFormationDatabase = database;
            ActiveTraitDatabase = traitDatabase;
            LastSelectedFormation = null;
        }

        /// <summary>
        /// Picks a random formation from the active database, ensuring it is not
        /// the same as the last selected formation (unless only one formation exists).
        /// </summary>
        /// <returns>The selected formation, or null if no database/formations are available.</returns>
        public static EnemyFormationData GetNextRandomFormation()
        {
            if (ActiveFormationDatabase == null || ActiveFormationDatabase.formations == null
                || ActiveFormationDatabase.formations.Count == 0)
            {
                return null;
            }

            var formations = ActiveFormationDatabase.formations;

            // Only one formation available — no choice
            if (formations.Count == 1)
            {
                LastSelectedFormation = formations[0];
                return LastSelectedFormation;
            }

            // Pick randomly, excluding the last selected
            EnemyFormationData selected;
            int attempts = 0;
            do
            {
                int index = _rng.Next(formations.Count);
                selected = formations[index];
                attempts++;
            }
            while (selected == LastSelectedFormation && attempts < 100);

            LastSelectedFormation = selected;
            return selected;
        }

        /// <summary>
        /// Clear all party and encounter data (e.g., on returning to main menu).
        /// </summary>
        public static void Clear()
        {
            CurrentParty.Clear();
            ActiveFormationDatabase = null;
            ActiveTraitDatabase = null;
            LastSelectedFormation = null;
        }
    }
}
