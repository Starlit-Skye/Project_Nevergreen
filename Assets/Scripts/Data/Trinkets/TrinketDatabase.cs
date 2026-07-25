using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Central registry of all available Trinkets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTrinketDatabase", menuName = "Nevergreen/Databases/Trinket Database")]
    public class TrinketDatabase : ScriptableObject
    {
        [Tooltip("All available Trinkets.")]
        public List<TrinketData> trinkets = new List<TrinketData>();
    }
}
