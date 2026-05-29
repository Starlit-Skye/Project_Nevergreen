using System.Collections.Generic;
using Nevergreen.Data;

namespace Nevergreen
{
    /// <summary>
    /// Static session manager that persists party configuration across scenes.
    /// Populated by the Main Menu skill selection, consumed by CombatCharacter.InitializeForCombat.
    /// </summary>
    public static class RunSessionManager
    {
        /// <summary>The active roster for the current run.</summary>
        public static List<PartyMemberInfo> CurrentParty { get; set; } = new List<PartyMemberInfo>();

        /// <summary>
        /// Clear all party data (e.g., on returning to main menu).
        /// </summary>
        public static void Clear()
        {
            CurrentParty.Clear();
        }
    }
}
