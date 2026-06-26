using System;
using System.Linq;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Checks if characters matching the target criteria DO NOT possess a specific status effect.
    /// Returns true if NO character in the target group has the status effect.
    /// </summary>
    [Serializable]
    public class NotHasStatusCondition : AIConditionNode
    {
        public enum ComparisonTarget
        {
            Self,
            AnyEnemy,
            AnyAlly
        }

        [Tooltip("Whose status effects to check. For teams (AnyEnemy/AnyAlly), returns true only if NO character has the status.")]
        public ComparisonTarget target = ComparisonTarget.AnyEnemy;

        [Tooltip("The status effect type to check for absence of.")]
        public StatusType statusType = StatusType.Mark;

        public override bool IsMet(AIBrain brain, BattleSystem battle)
        {
            switch (target)
            {
                case ComparisonTarget.Self:
                    return !HasActiveStatus(brain.Self);

                case ComparisonTarget.AnyEnemy:
                {
                    var enemies = brain.Self.IsPlayerTeam ? battle.EnemyTeam : battle.PlayerTeam;
                    foreach (var c in enemies)
                    {
                        if (c.IsAlive && HasActiveStatus(c)) return false; // Found one with the status
                    }
                    return true; // None had the status
                }

                case ComparisonTarget.AnyAlly:
                {
                    var allies = brain.Self.IsPlayerTeam ? battle.PlayerTeam : battle.EnemyTeam;
                    foreach (var c in allies)
                    {
                        if (c.IsAlive && HasActiveStatus(c)) return false; // Found one with the status
                    }
                    return true; // None had the status
                }

                default:
                    return false;
            }
        }

        private bool HasActiveStatus(CombatCharacter character)
        {
            return character.statusEffects.Any(s => s.type == statusType && !s.IsExpired);
        }
    }
}
