using System.Collections.Generic;
using System.Linq;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Pure logic static class for building and sorting the turn order in combat.
    /// </summary>
    public static class TurnOrderBuilder
    {
        public static List<TurnEntry> Build(
            List<CombatCharacter> playerTeam, 
            List<CombatCharacter> enemyTeam, 
            CombatConfig combatConfig, 
            System.Random rng)
        {
            var turnOrder = new List<TurnEntry>();
            int minRoll = combatConfig != null ? combatConfig.speedRollMin : 1;
            int maxRoll = combatConfig != null ? combatConfig.speedRollMax : 4;

            foreach (var c in playerTeam.Where(c => c.IsAlive))
            {
                CombatStats stats = c.GetEffectiveStats();
                int actionsPerRound = (c.characterData != null) ? c.characterData.actionsPerRound : 1;
                for (int a = 0; a < actionsPerRound; a++)
                {
                    int roll = rng.Next(minRoll, maxRoll + 1);
                    int speedWithRoll = stats.speed + roll;
                    turnOrder.Add(new TurnEntry(c, speedWithRoll));
                }
            }

            foreach (var c in enemyTeam.Where(c => c.IsAlive))
            {
                CombatStats stats = c.GetEffectiveStats();
                int actionsPerRound = (c.characterData != null) ? c.characterData.actionsPerRound : 1;
                for (int a = 0; a < actionsPerRound; a++)
                {
                    int roll = rng.Next(minRoll, maxRoll + 1);
                    int speedWithRoll = stats.speed + roll;
                    turnOrder.Add(new TurnEntry(c, speedWithRoll));
                }
            }

            // Sort: higher speed first
            // Tie-break 1: enemies before players
            // Tie-break 2: lower rank (front) first
            turnOrder.Sort((a, b) =>
            {
                int speedCompare = b.speed.CompareTo(a.speed);
                if (speedCompare != 0) return speedCompare;

                // Enemies act before players on tie
                int teamCompare = GetTeamPriority(a.character.team)
                                  .CompareTo(GetTeamPriority(b.character.team));
                if (teamCompare != 0) return teamCompare;

                // Same team: front rank first
                return a.character.rank.CompareTo(b.character.rank);
            });

            return turnOrder;
        }

        private static int GetTeamPriority(Team team)
        {
            return team == Team.Enemy ? 0 : 1; // Enemies go first on tie
        }
    }
}
