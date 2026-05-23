using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    [System.Serializable]
    public class BattleVariant
    {
        [Tooltip("The name of this variant displayed on the button.")]
        public string variantName = "Battle Variant";

        [Tooltip("Up to 4 enemy character prefabs. Index 0 = Rank 1 (front).")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();
    }

    [CreateAssetMenu(fileName = "BattleVariantsConfig", menuName = "Nevergreen/Data/Battle Variants Config")]
    public class BattleVariantsConfig : ScriptableObject
    {
        [Tooltip("Configure up to 5 battle variants for designers to select.")]
        public List<BattleVariant> variants = new List<BattleVariant>();
    }
}
