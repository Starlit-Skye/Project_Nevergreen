using System;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Specialized status instance for the Move effect.
    /// Executes the physical displacement logic when added to a character.
    /// </summary>
    [Serializable]
    public class MoveStatusInstance : StatusEffectInstance
    {
        private BattleSystem _battleSystem;

        public MoveStatusInstance(BattleSystem battleSystem, int amplitude) 
            : base(Data.StatusType.Move, amplitude, 0) // Duration 0 ensures instant expiration
        {
            _battleSystem = battleSystem;
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);
            
            if (_battleSystem != null)
            {
                int targetRank = host.rank + amplitude;
                _battleSystem.ExecuteMoveAndShift(host, targetRank);
            }
            else
            {
                Debug.LogWarning("[MoveStatusInstance] BattleSystem reference is null. Cannot execute move.");
            }

            // Move is an instantaneous effect; remove it immediately to keep the status list clean.
            host.RemoveStatus(this);
        }
    }
}
