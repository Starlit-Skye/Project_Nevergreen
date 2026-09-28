using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Room effect strategy for Elite combat encounters.
    /// Overrides the encounter tier to EnemyEncounterTier.Elite and handles victory room completion.
    /// </summary>
    [Serializable]
    public class EliteEncounterRoomEffectStrategy : CombatRoomEffectStrategy
    {
        public override EnemyEncounterTier? OverrideEncounterTier => EnemyEncounterTier.Elite;
    }
}
