using System;
using UnityEngine;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Compares the AI-controlled character's HP to a threshold.
    /// Can check either as a percentage or absolute value.
    /// </summary>
    [Serializable]
    public class HealthCondition : AIConditionNode
    {
        public enum ComparisonTarget
        {
            Self,
            /// <summary>Reserved for future expansion (e.g., checking a specific enemy target).</summary>
            AnyEnemy,
            AnyAlly
        }

        public enum ComparisonOp
        {
            Equal,
            LessThan,
            LessThanOrEqual,
            GreaterThan,
            GreaterThanOrEqual
        }

        [Tooltip("Whose HP to check.")]
        public ComparisonTarget target = ComparisonTarget.Self;

        [Tooltip("How to compare the HP value.")]
        public ComparisonOp comparison = ComparisonOp.LessThanOrEqual;

        [Tooltip("Threshold value to compare against (0-100 for percentage, raw int for absolute).")]
        public float threshold = 50f;

        [Tooltip("If true, threshold is treated as a percentage of max HP. If false, it is an absolute HP value.")]
        public bool usePercentage = true;

        public override bool IsMet(AIBrain brain, BattleSystem battle)
        {
            switch (target)
            {
                case ComparisonTarget.Self:
                    return EvaluateCharacter(brain.Self);

                case ComparisonTarget.AnyEnemy:
                {
                    var enemies = brain.Self.IsPlayerTeam ? battle.EnemyTeam : battle.PlayerTeam;
                    foreach (var c in enemies)
                    {
                        if (c.IsAlive && EvaluateCharacter(c)) return true;
                    }
                    return false;
                }

                case ComparisonTarget.AnyAlly:
                {
                    var allies = brain.Self.IsPlayerTeam ? battle.PlayerTeam : battle.EnemyTeam;
                    foreach (var c in allies)
                    {
                        if (c.IsAlive && EvaluateCharacter(c)) return true;
                    }
                    return false;
                }

                default:
                    return false;
            }
        }

        private bool EvaluateCharacter(CombatCharacter character)
        {
            float value = usePercentage
                ? (character.baseStats.maxHP > 0 ? (float)character.currentHP / character.baseStats.maxHP * 100f : 0f)
                : character.currentHP;

            return comparison switch
            {
                ComparisonOp.Equal => Mathf.Approximately(value, threshold),
                ComparisonOp.LessThan => value < threshold,
                ComparisonOp.LessThanOrEqual => value <= threshold,
                ComparisonOp.GreaterThan => value > threshold,
                ComparisonOp.GreaterThanOrEqual => value >= threshold,
                _ => false
            };
        }
    }
}
