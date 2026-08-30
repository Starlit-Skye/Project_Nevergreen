using System;
using UnityEngine;
using Nevergreen;

namespace Nevergreen.Data
{
    /// <summary>
    /// A rule whose weight scales inversely with the current party size.
    /// The fewer characters in the party, the higher the weight.
    /// </summary>
    [Serializable]
    public class PartyCountRule : RoomSelectionRule
    {
        [Tooltip("The base weight when the party is full.")]
        [SerializeField]
        public float baseWeight = 1f;

        [Tooltip("Additional weight added for each empty slot in the party.")]
        [SerializeField]
        public float bonusPerMissingSlot = 0.5f;

        public override float EvaluateWeight()
        {
            int maxPartySlots = 4; // Fallback
            if (GameDatabase.Instance != null && GameDatabase.Instance.CombatConfig != null)
            {
                maxPartySlots = GameDatabase.Instance.CombatConfig.maxPartySize;
            }

            int currentPartyCount = RunSessionManager.CurrentParty != null ? RunSessionManager.CurrentParty.Count : 0;
            int missingSlots = Mathf.Max(0, maxPartySlots - currentPartyCount);
            
            float weight = baseWeight + (missingSlots * bonusPerMissingSlot);
            return Mathf.Max(0f, weight);
        }
    }
}
