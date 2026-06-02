using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Runtime wrapper for an active trait during combat.
    /// Created when a CombatCharacter initializes and destroyed when combat ends.
    /// Holds the immutable data and the owning character reference.
    /// </summary>
    public class TraitInstance
    {
        /// <summary>The ScriptableObject definition of this trait.</summary>
        public TraitData data;

        /// <summary>The CombatCharacter this trait is active on.</summary>
        public CombatCharacter owner;

        /// <summary>The BattleSystem reference for accessing combat state.</summary>
        public BattleSystem battleSystem;

        /// <summary>Flexible storage for strategies to store instance-specific state (e.g. event closures).</summary>
        public System.Collections.Generic.Dictionary<string, object> extra = new System.Collections.Generic.Dictionary<string, object>();

        public TraitInstance(TraitData data, CombatCharacter owner, BattleSystem battleSystem)
        {
            this.data = data;
            this.owner = owner;
            this.battleSystem = battleSystem;
        }

        /// <summary>
        /// Activates the trait strategy, subscribing to any needed events.
        /// </summary>
        public void Activate()
        {
            if (data.effectStrategies != null)
            {
                foreach (var strategy in data.effectStrategies)
                {
                    strategy?.OnActivate(this);
                }
            }
        }

        /// <summary>
        /// Deactivates the trait strategy, cleaning up event subscriptions.
        /// </summary>
        public void Deactivate()
        {
            if (data.effectStrategies != null)
            {
                foreach (var strategy in data.effectStrategies)
                {
                    strategy?.OnDeactivate(this);
                }
            }
        }

        /// <summary>
        /// Called during stat calculation to apply passive modifiers.
        /// </summary>
        public void ModifyStats(TraitStatModifier modifier)
        {
            if (data.effectStrategies != null)
            {
                foreach (var strategy in data.effectStrategies)
                {
                    strategy?.ModifyStats(this, modifier);
                }
            }
        }
    }
}
