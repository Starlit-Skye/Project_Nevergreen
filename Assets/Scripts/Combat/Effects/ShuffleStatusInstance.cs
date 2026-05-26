using System;
using System.Collections.Generic;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Specialized status instance for the Shuffle effect.
    /// Executes the physical displacement logic when added to a character to move them to a random rank excluding their current rank.
    /// </summary>
    [Serializable]
    public class ShuffleStatusInstance : StatusEffectInstance
    {
        private BattleSystem _battleSystem;
        private System.Random _rng;

        public ShuffleStatusInstance(BattleSystem battleSystem, System.Random rng) 
            : base(StatusType.Shuffle, 1, 0) // Duration 0 ensures instant expiration
        {
            _battleSystem = battleSystem;
            _rng = rng;
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);
            
            if (_battleSystem != null && _rng != null)
            {
                var team = host.IsPlayerTeam ? _battleSystem.PlayerTeam : _battleSystem.EnemyTeam;

                int totalSlotsUsed = 0;
                foreach (var c in team)
                {
                    int s = (c.characterData != null) ? c.characterData.size : 1;
                    totalSlotsUsed += s;
                }
                int maxAnchorRank = Mathf.Max(1, totalSlotsUsed - ((host.characterData != null) ? host.characterData.size : 1) + 1);

                if (maxAnchorRank > 1)
                {
                    List<int> validRanks = new List<int>();
                    for (int r = 1; r <= maxAnchorRank; r++)
                    {
                        if (r != host.rank)
                        {
                            validRanks.Add(r);
                        }
                    }

                    if (validRanks.Count > 0)
                    {
                        int targetRank = validRanks[_rng.Next(validRanks.Count)];
                        _battleSystem.ExecuteMoveAndShift(host, targetRank);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[ShuffleStatusInstance] BattleSystem or RNG reference is null. Cannot execute shuffle.");
            }

            // Shuffle is an instantaneous effect; remove it immediately to keep the status list clean.
            host.RemoveStatus(this);
        }
    }
}
