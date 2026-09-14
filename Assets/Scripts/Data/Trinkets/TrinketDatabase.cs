using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Database ScriptableObject that holds the pools of available trinkets
    /// categorized by tier (Common, Uncommon, Rare).
    /// </summary>
    [CreateAssetMenu(fileName = "TrinketDatabase", menuName = "Nevergreen/Databases/Trinket Database")]
    public class TrinketDatabase : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Pool of common tier trinkets.")]
        private List<TrinketData> commonTrinkets = new List<TrinketData>();

        [SerializeField]
        [Tooltip("Pool of uncommon tier trinkets.")]
        private List<TrinketData> uncommonTrinkets = new List<TrinketData>();

        [SerializeField]
        [Tooltip("Pool of rare tier trinkets.")]
        private List<TrinketData> rareTrinkets = new List<TrinketData>();

        public IReadOnlyList<TrinketData> CommonTrinkets => commonTrinkets;
        public IReadOnlyList<TrinketData> UncommonTrinkets => uncommonTrinkets;
        public IReadOnlyList<TrinketData> RareTrinkets => rareTrinkets;

        /// <summary>
        /// Combined collection of all trinkets across all tiers.
        /// </summary>
        public IEnumerable<TrinketData> trinkets
        {
            get
            {
                if (commonTrinkets != null)
                {
                    foreach (var t in commonTrinkets)
                    {
                        if (t != null) yield return t;
                    }
                }
                if (uncommonTrinkets != null)
                {
                    foreach (var t in uncommonTrinkets)
                    {
                        if (t != null) yield return t;
                    }
                }
                if (rareTrinkets != null)
                {
                    foreach (var t in rareTrinkets)
                    {
                        if (t != null) yield return t;
                    }
                }
            }
        }
    }
}
