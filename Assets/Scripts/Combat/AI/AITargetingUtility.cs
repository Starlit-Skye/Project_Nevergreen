using System.Collections.Generic;
using System.Linq;

namespace Nevergreen.Combat.AI
{
    public static class AITargetingUtility
    {
        /// <summary>
        /// Filters out Piles from the target pool if there are active (non-Pile) alternatives available.
        /// If the pool consists entirely of Piles, it returns the original pool so the AI doesn't pass its turn when only Piles exist.
        /// </summary>
        public static List<CombatCharacter> FilterPilesIfAlternativesExist(this IEnumerable<CombatCharacter> targets)
        {
            if (targets == null) return new List<CombatCharacter>();
            
            var list = targets.ToList();
            if (list.Count == 0) return list;

            var nonPiles = list.Where(c => !c.IsPile).ToList();
            
            return nonPiles.Count > 0 ? nonPiles : list;
        }
    }
}
