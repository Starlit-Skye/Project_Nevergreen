using UnityEngine;
using System.Collections.Generic;

namespace Nevergreen.Data
{
    /// <summary>
    /// Classifies a trait as either a positive (Perfection) or negative (Imperfection) passive.
    /// </summary>
    public enum TraitType
    {
        Perfection,
        Imperfection
    }

    /// <summary>
    /// ScriptableObject definition for a single Perfection or Imperfection.
    /// Holds identity data and a reference to the strategy that implements the effect.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTrait", menuName = "Nevergreen/Data/Trait Data")]
    public class TraitData : ScriptableObject
    {
        [Tooltip("Unique identifier for this trait.")]
        public string traitId;

        [Tooltip("Display name shown in UI.")]
        public string displayName;

        [Tooltip("Short description of the trait's effect.")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Whether this trait is a Perfection (positive) or Imperfection (negative).")]
        public TraitType traitType = TraitType.Perfection;

        [Header("Effect Strategies")]
        [Tooltip("The modular effects executed by this trait.")]
        [SerializeReference]
        [Nevergreen.Attributes.SubclassSelector]
        public List<TraitEffectStrategy> effectStrategies = new List<TraitEffectStrategy>();
    }
}
