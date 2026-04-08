using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Defines stat-scaling modifiers applied when a skill executes.
    /// Damage and Heal are mutually exclusive.
    /// Values of 0 mean "no modifier" and are hidden in UI.
    /// </summary>
    [Serializable]
    public class SkillModifier
    {
        [Tooltip("Percentage multiplier of user's Attack stat for damage. 0 = no damage.")]
        public float damagePercent = 0f;

        [Tooltip("Percentage multiplier of user's Attack stat for healing. 0 = no heal.")]
        public float healPercent = 0f;

        [Tooltip("Additive modifier to user's base Accuracy. 0 = no change.")]
        public float accuracyMod = 0f;

        [Tooltip("Additive modifier to user's base Critical Chance. 0 = no change.")]
        public float criticalMod = 0f;

        /// <summary>True if this modifier deals damage.</summary>
        public bool IsDamage => damagePercent != 0f;

        /// <summary>True if this modifier heals.</summary>
        public bool IsHeal => healPercent != 0f;
    }
}
