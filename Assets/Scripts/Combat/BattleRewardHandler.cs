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
            EnemyEncounterTier tier,
            System.Random rng,
            out int partsGranted,
            out int scrapsGranted)
        {
            partsGranted = 0;
            scrapsGranted = 0;

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

            // Calculate parts and scraps reward
            if (config != null)
            {
                config.GetRewardRanges(tier, out int minP, out int maxP, out int minS, out int maxS);
                partsGranted = rng.Next(minP, maxP + 1);
                scrapsGranted = rng.Next(minS, maxS + 1);

                RunSessionManager.GrantParts(partsGranted);
                RunSessionManager.GrantScraps(scrapsGranted);
            }
        }
    }
}
