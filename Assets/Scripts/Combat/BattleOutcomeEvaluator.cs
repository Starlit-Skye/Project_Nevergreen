using System.Collections.Generic;
using System.Linq;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Pure logic static class for evaluating if the battle has ended.
    /// </summary>
    public static class BattleOutcomeEvaluator
    {
        public static BattleOutcome? Evaluate(
            List<CombatCharacter> playerTeam, 
            List<CombatCharacter> enemyTeam, 
            List<CombatCharacter> initialPlayerTeam,
            out string defeatReason)
        {
            defeatReason = string.Empty;

            // Cecilia (ceci) is the primary character; her defeat is a battle loss.
            bool initialHasCecilia = initialPlayerTeam.Any(c => c.CharacterId == "ceci");
            bool ceciliaDefeated = initialHasCecilia && !playerTeam.Any(c => c.CharacterId == "ceci" && c.state == LifeState.Alive);
            bool allPlayersDead = playerTeam.Count == 0 || playerTeam.All(c => c.state != LifeState.Alive);
            bool allEnemiesDead = enemyTeam.Count == 0 || enemyTeam.All(c => c.state != LifeState.Alive);

            if (ceciliaDefeated || allPlayersDead)
            {
                defeatReason = ceciliaDefeated ? "CECILIA DEFEATED" : "ALL PLAYERS DEFEATED";
                return BattleOutcome.Defeat;
            }

            if (allEnemiesDead)
            {
                return BattleOutcome.Victory;
            }

            return null; // Battle continues
        }
    }
}
