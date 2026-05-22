using System;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Instantly expires all active Piles in the battle.
    /// Expiration transitions the Pile to Destroyed state, which triggers proper cleanup and rank shifting.
    /// </summary>
    [Serializable]
    public class ExpirePilesEffect : ISkillEffect
    {
        public void Execute(SkillContext context, CombatCharacter target)
        {
            if (context.battleSystem == null)
                return;

            // Cache execution to run exactly once per skill cast
            string cacheKey = $"ExpirePiles_{this.GetHashCode()}";
            if (context.extra.ContainsKey(cacheKey))
                return;
            context.extra[cacheKey] = true;

            // Get all active piles across both teams and transition them to Destroyed
            var allPiles = context.battleSystem.PlayerTeam
                .Concat(context.battleSystem.EnemyTeam)
                .Where(c => c.IsPile)
                .ToList();

            if (allPiles.Count == 0)
            {
                Debug.Log("[ExpirePilesEffect] No active piles found to expire.");
                return;
            }

            foreach (var pile in allPiles)
            {
                Debug.Log($"[ExpirePilesEffect] Expiring pile {pile.DisplayName}.");
                pile.state = LifeState.Destroyed;
            }
        }
    }
}
