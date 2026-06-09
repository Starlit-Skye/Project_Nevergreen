using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Central registry of all available Perfection and Imperfection traits.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTraitDatabase", menuName = "Nevergreen/Data/Trait Database")]
    public class TraitDatabase : ScriptableObject
    {
        [Tooltip("All available Perfection traits.")]
        public List<TraitData> perfections = new List<TraitData>();

        [Tooltip("All available Imperfection traits.")]
        public List<TraitData> imperfections = new List<TraitData>();
    }
}
