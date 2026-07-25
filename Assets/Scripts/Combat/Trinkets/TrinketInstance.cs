using System.Collections.Generic;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Runtime wrapper for TrinketData. Maintains active state and dynamic extra data during combat.
    /// </summary>
    public class TrinketInstance
    {
        public TrinketData data;
        public CombatCharacter owner;
        public BattleSystem battleSystem;
        
        /// <summary>Generic storage for strategy event closures and state.</summary>
        public Dictionary<string, object> extra = new Dictionary<string, object>();

        public TrinketInstance(TrinketData data, CombatCharacter owner, BattleSystem battleSystem)
        {
            this.data = data;
            this.owner = owner;
            this.battleSystem = battleSystem;
        }

        public void Activate()
        {
            if (data == null || data.effectStrategies == null) return;
            foreach (var strategy in data.effectStrategies)
            {
                strategy?.OnActivate(this);
            }
        }

        public void Deactivate()
        {
            if (data == null || data.effectStrategies == null) return;
            foreach (var strategy in data.effectStrategies)
            {
                strategy?.OnDeactivate(this);
            }
        }

        public void ModifyStats(TraitStatModifier modifier)
        {
            if (data == null || data.effectStrategies == null) return;
            foreach (var strategy in data.effectStrategies)
            {
                strategy?.ModifyStats(this, modifier);
            }
        }
    }
}
