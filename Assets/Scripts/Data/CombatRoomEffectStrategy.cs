using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// A "no-op" strategy for standard combat rooms.
    /// It simply signals room completion so the UI can proceed to room selection.
    /// </summary>
    [Serializable]
    public class CombatRoomEffectStrategy : RoomEffectStrategy
    {
        public override void ExecuteRoomEffect()
        {
            var combatUI = UnityEngine.Object.FindFirstObjectByType<Nevergreen.Prototype.CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                Debug.LogWarning("[CombatRoomEffectStrategy] Could not find CombatUI to trigger room selection.");
                if (!RunSessionManager.RoomCompleted)
                {
                    RunSessionManager.CompleteRoom(new System.Collections.Generic.List<RoomData>());
                }
            }
        }
    }
}
