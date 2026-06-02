using System.Collections.Generic;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Accumulator passed to TraitEffectStrategy.ModifyStats() so strategies can
    /// inject passive stat changes without touching CombatCharacter internals.
    /// </summary>
    public class TraitStatModifier
    {
        /// <summary>Flat additive bonuses keyed by StatTarget (applied before percentage).</summary>
        public Dictionary<StatTarget, int> flatBonuses = new Dictionary<StatTarget, int>();

        /// <summary>Percentage multiplier bonuses keyed by StatTarget (as a 0-100 scale, e.g. 10 = +10%).</summary>
        public Dictionary<StatTarget, float> percentBonuses = new Dictionary<StatTarget, float>();

        public void AddFlat(StatTarget stat, int amount)
        {
            if (!flatBonuses.ContainsKey(stat)) flatBonuses[stat] = 0;
            flatBonuses[stat] += amount;
        }

        public void AddPercent(StatTarget stat, float amount)
        {
            if (!percentBonuses.ContainsKey(stat)) percentBonuses[stat] = 0f;
            percentBonuses[stat] += amount;
        }
    }
}
