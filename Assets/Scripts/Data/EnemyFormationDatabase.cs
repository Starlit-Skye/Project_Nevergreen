using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Central registry of all available enemy formations.
    /// Designers populate this with EnemyFormationData assets for each tier.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyFormationDatabase", menuName = "Nevergreen/Enemy Formation Database")]
    public class EnemyFormationDatabase : ScriptableObject
    {
        [Header("Tiers")]
        [Tooltip("Formations for the Trivial encounter tier (e.g. earliest rooms).")]
        public List<EnemyFormationData> trivialFormations = new List<EnemyFormationData>();

        [Tooltip("Formations for the Early Game encounter tier.")]
        public List<EnemyFormationData> earlyGameFormations = new List<EnemyFormationData>();

        [Tooltip("Formations for the Mid Game encounter tier.")]
        public List<EnemyFormationData> midGameFormations = new List<EnemyFormationData>();

        [Tooltip("Formations for the Late Game encounter tier.")]
        public List<EnemyFormationData> lateGameFormations = new List<EnemyFormationData>();

        /// <summary>
        /// Retrieves the list of formations corresponding to the provided difficulty tier.
        /// </summary>
        public IReadOnlyList<EnemyFormationData> GetFormations(EnemyEncounterTier tier)
        {
            return tier switch
            {
                EnemyEncounterTier.Trivial => trivialFormations,
                EnemyEncounterTier.EarlyGame => earlyGameFormations,
                EnemyEncounterTier.MidGame => midGameFormations,
                EnemyEncounterTier.LateGame => lateGameFormations,
                _ => trivialFormations
            };
        }
    }
}
