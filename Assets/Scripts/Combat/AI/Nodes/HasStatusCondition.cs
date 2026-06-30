using System;
using System.Linq;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Checks if characters matching the target criteria possess a specific status effect.
    /// </summary>
    [Serializable]
    public class HasStatusCondition : AIConditionNode
    {
        public enum ComparisonTarget
        {
            Self,
            AnyEnemy,
            AnyAlly
        }

        [Tooltip("Whose status effects to check.")]
        public ComparisonTarget target = ComparisonTarget.AnyEnemy;

        [Tooltip("The status effect type to check for.")]
        public StatusType statusType = StatusType.Mark;

        [Tooltip("The stat targeted by the Buff/Debuff (Only used if statusType is Buff or Debuff).")]
        public StatTarget stat = StatTarget.Speed;

        [Tooltip("How to compare the amplitude of the Buff/Debuff.")]
        public HealthCondition.ComparisonOp amplitudeComparison = HealthCondition.ComparisonOp.GreaterThanOrEqual;

        [Tooltip("The required amplitude of the Buff/Debuff to be considered a match (Only used if statusType is Buff or Debuff).")]
        public int targetAmplitude = 1;

        public override bool IsMet(AIBrain brain, BattleSystem battle)
        {
            switch (target)
            {
                case ComparisonTarget.Self:
                    return HasActiveStatus(brain.Self);

                case ComparisonTarget.AnyEnemy:
                {
                    var enemies = brain.Self.IsPlayerTeam ? battle.EnemyTeam : battle.PlayerTeam;
                    foreach (var c in enemies)
                    {
                        if (c.IsAlive && HasActiveStatus(c)) return true;
                    }
                    return false;
                }

                case ComparisonTarget.AnyAlly:
                {
                    var allies = brain.Self.IsPlayerTeam ? battle.PlayerTeam : battle.EnemyTeam;
                    foreach (var c in allies)
                    {
                        if (c.IsAlive && HasActiveStatus(c)) return true;
                    }
                    return false;
                }

                default:
                    return false;
            }
        }

        private bool HasActiveStatus(CombatCharacter character)
        {
            return character.statusEffects.Any(s => 
            {
                if (s.IsExpired) return false;
                if (s.type != statusType) return false;

                if (statusType == StatusType.Buff || statusType == StatusType.Debuff)
                {
                    if (s.targetStat != stat) return false;
                    
                    return amplitudeComparison switch
                    {
                        HealthCondition.ComparisonOp.Equal => s.amplitude == targetAmplitude,
                        HealthCondition.ComparisonOp.LessThan => s.amplitude < targetAmplitude,
                        HealthCondition.ComparisonOp.LessThanOrEqual => s.amplitude <= targetAmplitude,
                        HealthCondition.ComparisonOp.GreaterThan => s.amplitude > targetAmplitude,
                        HealthCondition.ComparisonOp.GreaterThanOrEqual => s.amplitude >= targetAmplitude,
                        _ => false
                    };
                }

                return true;
            });
        }
    }
}
