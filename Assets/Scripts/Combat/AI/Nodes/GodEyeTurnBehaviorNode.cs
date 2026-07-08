using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Custom AI behavior node for the God-Eye boss.
    /// - If team members < 4: uses Summon Ally.
    /// - If team members == 4: randomly uses Buff (targets random ally) or Mark (targets lowest HP enemy).
    /// </summary>
    [Serializable]
    public class GodEyeTurnBehaviorNode : AIBehaviorNode
    {
        [Tooltip("Skill used to summon an ally. Targets self as a signal; actual spawn is handled by GodEyeController.")]
        public SkillData summonSkill;

        [Tooltip("Skill used to buff a random ally.")]
        public SkillData buffSkill;

        [Tooltip("Skill used to mark the lowest HP enemy.")]
        public SkillData markSkill;

        public override bool TryGetDecision(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;
            CombatCharacter self = brain.Self;

            int teamSize = battle.EnemyTeam.Count(c => c.IsAlive);

            if (teamSize < 4)
            {
                if (TrySummon(brain, battle, out decision)) return true;
                
                // Fallback if summon can't be used
                return TryBuffOrMark(brain, battle, out decision);
            }
            else
            {
                return TryBuffOrMark(brain, battle, out decision);
            }
        }

        private bool TrySummon(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;
            if (summonSkill == null) return false;
            if (!brain.Self.CanUseSkillFromRank(summonSkill) || !brain.Self.HasRemainingUses(summonSkill))
                return false;

            var validTargets = battle.GetValidTargets(brain.Self, summonSkill);
            if (validTargets.Count == 0) return false;

            // Pick a random target to damage
            var primaryTarget = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
            var targets = battle.GetAOETargets(summonSkill, primaryTarget);

            decision = AIDecision.UseSkill(summonSkill, targets);
            return true;
        }

        private bool TryBuffOrMark(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;
            
            bool tryBuffFirst = UnityEngine.Random.Range(0, 2) == 0;
            
            if (tryBuffFirst)
            {
                if (TryBuff(brain, battle, out decision)) return true;
                return TryMark(brain, battle, out decision);
            }
            else
            {
                if (TryMark(brain, battle, out decision)) return true;
                return TryBuff(brain, battle, out decision);
            }
        }

        private bool TryBuff(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;
            if (buffSkill == null) return false;
            if (!brain.Self.CanUseSkillFromRank(buffSkill) || !brain.Self.HasRemainingUses(buffSkill))
                return false;

            var validAllies = battle.EnemyTeam.Where(c => c.IsAlive && c != brain.Self).ToList();
            if (validAllies.Count == 0) return false;

            var chosenAlly = validAllies[UnityEngine.Random.Range(0, validAllies.Count)];
            decision = AIDecision.UseSkill(buffSkill, new List<CombatCharacter> { chosenAlly });
            return true;
        }

        private bool TryMark(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;
            if (markSkill == null) return false;
            if (!brain.Self.CanUseSkillFromRank(markSkill) || !brain.Self.HasRemainingUses(markSkill))
                return false;

            var validPlayers = battle.PlayerTeam.Where(c => c.IsAlive).ToList();
            if (validPlayers.Count == 0) return false;

            int minHP = validPlayers.Min(c => c.currentHP);
            var lowestHpPlayers = validPlayers.Where(c => c.currentHP == minHP).ToList();
            
            var chosenEnemy = lowestHpPlayers[UnityEngine.Random.Range(0, lowestHpPlayers.Count)];
            decision = AIDecision.UseSkill(markSkill, new List<CombatCharacter> { chosenEnemy });
            return true;
        }
    }
}
