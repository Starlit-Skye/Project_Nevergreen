using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// A single designer-authored enemy team formation.
    /// Each entry in the prefab list corresponds to a rank slot (index 0 = rank 1, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyFormation", menuName = "Nevergreen/Enemy Formation")]
    public class EnemyFormationData : ScriptableObject
    {
        [Tooltip("Enemy prefabs for this formation. Index 0 = rank 1 (front), up to 4.")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();
    }
}
