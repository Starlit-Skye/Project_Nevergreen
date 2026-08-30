using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// A simple rule that always returns a constant weight.
    /// Used for rooms that should have a fixed probability of appearing.
    /// </summary>
    [Serializable]
    public class FixedWeightRule : RoomSelectionRule
    {
        [Tooltip("The constant weight this room will have in the selection pool.")]
        [SerializeField]
        public float weight = 1f;

        public override float EvaluateWeight()
        {
            return Mathf.Max(0f, weight);
        }
    }
}
