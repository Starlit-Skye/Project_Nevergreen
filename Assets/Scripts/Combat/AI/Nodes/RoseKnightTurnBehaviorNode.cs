using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Custom AI behavior node for the RoseKnight boss's normal turn logic.
    /// - If not at rank 1: use moveForwardSkill (targeting self).
    /// - If at rank 1 with 0 allies: summon.
    /// - If at rank 1 with 1 ally: randomly summon or buff.
    /// - If at rank 1 with 2+ allies: buff.
    /// </summary>
    [Serializable]
    public class RoseKnightTurnBehaviorNode : AIBehaviorNode
    {
        [Tooltip("Skill used to advance the boss toward rank 1.")]
        public SkillData moveForwardSkill;

        [Tooltip("Skill used to summon an ally. Targets self as a signal; actual spawn is handled by RoseKnightController.")]
        public SkillData summonSkill;

        [Tooltip("Skill used to buff allies.")]
        public SkillData buffSkill;

        [Tooltip("Maximum number of summoned allies allowed on the field.")]
        public int maxAllies = 2;

        public override bool TryGetDecision(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;
            CombatCharacter self = brain.Self;

            // --- Priority 1: Move forward if not at rank 1 ---
            if (self.rank != 1 && moveForwardSkill != null)
            {
                if (self.CanUseSkillFromRank(moveForwardSkill) && self.HasRemainingUses(moveForwardSkill))
                {
                    decision = AIDecision.UseSkill(moveForwardSkill, new List<CombatCharacter> { self });
                    return true;
                }
            }

            // --- Priority 2: At rank 1, decide between summon and buff ---
            int allyCount = battle.EnemyTeam.Count(c => c.IsAlive && c != self);

            if (allyCount >= maxAllies)
            {
                // Max allies reached — always buff
                return TryBuff(brain, battle, out decision);
            }

            if (allyCount == 0)
            {
                // No allies — always summon
                return TrySummon(brain, out decision);
            }

            // 1 ally — 50/50 summon or buff
            bool chooseSummon = UnityEngine.Random.Range(0, 2) == 0;
            if (chooseSummon)
            {
                if (TrySummon(brain, out decision)) return true;
                // Summon failed (e.g. no uses), fallback to buff
                return TryBuff(brain, battle, out decision);
            }
            else
            {
                if (TryBuff(brain, battle, out decision)) return true;
                // Buff failed, fallback to summon
                return TrySummon(brain, out decision);
            }
        }

        private bool TrySummon(AIBrain brain, out AIDecision decision)
        {
            decision = default;
            if (summonSkill == null) return false;
            if (!brain.Self.CanUseSkillFromRank(summonSkill) || !brain.Self.HasRemainingUses(summonSkill))
                return false;

            decision = AIDecision.UseSkill(summonSkill, new List<CombatCharacter> { brain.Self });
            return true;
        }

        private bool TryBuff(AIBrain brain, BattleSystem battle, out AIDecision decision)
        {
            decision = default;
            if (buffSkill == null) return false;
            if (!brain.Self.CanUseSkillFromRank(buffSkill) || !brain.Self.HasRemainingUses(buffSkill))
                return false;

            List<CombatCharacter> candidates = battle.GetValidTargets(brain.Self, buffSkill);
            
            // Remove self from candidates since skill says "except self"
            candidates.Remove(brain.Self);
            candidates = candidates.FilterPilesIfAlternativesExist();
            
            if (candidates.Count == 0) return false;

            // Pick front-most ally to maximize AOE sweep for GetAOETargets
            var primaryTarget = candidates.OrderBy(c => c.rank).First();
            List<CombatCharacter> targets = battle.GetAOETargets(buffSkill, primaryTarget);

            // Double check to ensure self is not in the final target list
            targets.Remove(brain.Self);

            Debug.Log($"[RoseKnight] Decided to cast Buff '{buffSkill.displayName}' on {targets.Count} target(s).");
            decision = AIDecision.UseSkill(buffSkill, targets);
            return true;
        }
    }
}
