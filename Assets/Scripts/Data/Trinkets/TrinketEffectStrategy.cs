using UnityEngine;
using Nevergreen.Combat;

namespace Nevergreen.Data
{
    [System.Serializable]
    public abstract class TrinketEffectStrategy
    {
        public virtual void OnActivate(TrinketInstance instance) { }
        public virtual void OnDeactivate(TrinketInstance instance) { }
        public virtual void ModifyStats(TrinketInstance instance, TraitStatModifier modifier) { }
        public virtual string GetTooltipDescription() => string.Empty;
    }
}
