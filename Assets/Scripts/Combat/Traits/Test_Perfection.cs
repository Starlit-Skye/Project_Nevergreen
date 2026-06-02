using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// A simple test trait strategy that gives a flat +2 Speed bonus to the owner.
    /// </summary>
    [System.Serializable]
    public class Test_Perfection : TraitEffectStrategy
    {
        public override void ModifyStats(TraitInstance instance, TraitStatModifier modifier)
        {
            modifier.AddFlat(StatTarget.Speed, 2);
        }
    }
}
