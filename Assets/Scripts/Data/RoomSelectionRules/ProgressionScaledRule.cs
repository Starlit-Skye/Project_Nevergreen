using System;
using UnityEngine;
using Nevergreen;

namespace Nevergreen.Data
{
    /// <summary>
    /// A rule whose weight scales based on the player's run progression.
    /// </summary>
    [Serializable]
    public class ProgressionScaledRule : RoomSelectionRule
    {
        [Tooltip("The base weight at room progression 0.")]
        [SerializeField]
        public float baseWeight = 1f;

        [Tooltip("How much weight is added (or subtracted) per completed room.")]
        [SerializeField]
        public float weightPerRoom = 0.1f;

        public override float EvaluateWeight()
        {
            float weight = baseWeight + (RunSessionManager.RoomProgression * weightPerRoom);
            return Mathf.Max(0f, weight);
        }
    }
}
