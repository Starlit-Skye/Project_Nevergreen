using System;
using System.Linq;
using UnityEngine;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Evaluates the count of active (alive) characters on a specific team, excluding Piles and Destroyed units.
    /// </summary>
    [Serializable]
    public class TeamCountCondition : AIConditionNode
    {
        public enum TargetTeam
        {
            PlayerTeam,
            EnemyTeam
        }

        public enum ComparisonOp
        {
            Equals,
            NotEquals,
            LessThan,
            LessThanOrEqual,
            GreaterThan,
            GreaterThanOrEqual
        }

        [Tooltip("The team to count active characters from.")]
        public TargetTeam targetTeam = TargetTeam.EnemyTeam;

        [Tooltip("The operator to use for comparing the team count.")]
        public ComparisonOp comparison = ComparisonOp.LessThanOrEqual;

        [Tooltip("The threshold count to compare against.")]
        public int targetCount = 1;

        public override bool IsMet(AIBrain brain, BattleSystem battle)
        {
            var teamList = targetTeam == TargetTeam.PlayerTeam ? battle.PlayerTeam : battle.EnemyTeam;

            // IsAlive filters out Piles and Destroyed characters
            int currentCount = teamList.Count(c => c.IsAlive);

            return comparison switch
            {
                ComparisonOp.Equals => currentCount == targetCount,
                ComparisonOp.NotEquals => currentCount != targetCount,
                ComparisonOp.LessThan => currentCount < targetCount,
                ComparisonOp.LessThanOrEqual => currentCount <= targetCount,
                ComparisonOp.GreaterThan => currentCount > targetCount,
                ComparisonOp.GreaterThanOrEqual => currentCount >= targetCount,
                _ => false
            };
        }
    }
}
