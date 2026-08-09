using System.Collections.Generic;
using System.Linq;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Pure logic static class for handling rewards and party state syncing at the end of a battle.
    /// </summary>
    public static class BattleRewardHandler
    {
        public static void ApplyVictoryRewards(
            List<CombatCharacter> playerTeam, 
            CombatConfig config, 
            System.Random rng,
            out int partsGranted)
        {
            partsGranted = 0;

            if (RunSessionManager.CurrentParty != null)
            {
                var survivingPlayersSorted = playerTeam
                    .Where(c => c.state != LifeState.Pile && c.state != LifeState.Destroyed)
                    .OrderBy(c => c.rank)
                    .ToList();

                var updatedParty = new List<PartyMemberInfo>();
                foreach (var cc in survivingPlayersSorted)
                {
                    if (cc.partyInfo != null)
                    {
                        updatedParty.Add(cc.partyInfo);
                    }
                }

                RunSessionManager.CurrentParty.Clear();
                RunSessionManager.CurrentParty.AddRange(updatedParty);
            }

            // Calculate parts reward
            if (config != null)
            {
                partsGranted = rng.Next(config.minPartsPerBattle, config.maxPartsPerBattle + 1);
                RunSessionManager.Parts += partsGranted;
            }
        }
    }
}
