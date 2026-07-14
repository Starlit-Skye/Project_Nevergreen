using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Abstract base for all trait effect behaviours.
    /// Concrete strategies define how a single Perfection or Imperfection modifies combat.
    /// </summary>
    [System.Serializable]
    public abstract class TraitEffectStrategy
    {
        /// <summary>
        /// Called once when combat starts and the trait is activated on a character.
        /// Use this to subscribe to combat events or cache references.
        /// </summary>
        public virtual void OnActivate(Combat.TraitInstance instance) { }

        /// <summary>
        /// Called when combat ends or the character is destroyed.
        /// Use this to unsubscribe from events and release references.
        /// </summary>
        public virtual void OnDeactivate(Combat.TraitInstance instance) { }

        /// <summary>
        /// Called during GetEffectiveStats() to allow the trait to inject
        /// passive stat modifiers. Writes into the provided modifier accumulator.
        /// </summary>
        public virtual void ModifyStats(Combat.TraitInstance instance, Combat.TraitStatModifier modifier) { }

        /// <summary>
        /// Generates the formatted tooltip text for this effect strategy based on the parent trait's type.
        /// </summary>
        public virtual string GetTooltipDescription(TraitType traitType)
        {
            return string.Empty;
        }
    }
}
