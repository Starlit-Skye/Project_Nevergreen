using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Central registry of all available enemy formations.
    /// Designers populate this with EnemyFormationData assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyFormationDatabase", menuName = "Nevergreen/Enemy Formation Database")]
    public class EnemyFormationDatabase : ScriptableObject
    {
        [Tooltip("All available enemy formations that can be randomly selected.")]
        public List<EnemyFormationData> formations = new List<EnemyFormationData>();
    }
}
