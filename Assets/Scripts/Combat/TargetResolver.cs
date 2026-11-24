using System.Collections.Generic;
using System.Linq;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Pure logic static class for determining valid skill targets and AOE propagation.
    /// </summary>
    public static class TargetResolver
    {
        /// <summary>
        /// Checks if a character is a valid receiver for a skill's primary effect.
        /// </summary>
        public static bool IsValidReceiver(CombatCharacter target, SkillData skill)
        {
            bool isHealingSkill = skill.effects.Any(e => e is HealEffect);
            return target.IsAlive || target.IsDowned || (target.IsPile && !isHealingSkill);
        }

        /// <summary>
        /// Checks whether a character can be directly targeted by a skill (or count as a valid
        /// targetable unit inside an AOE skill's range).
        /// </summary>
        public static bool IsTargetable(CombatCharacter target, SkillData skill)
        {
            if (target == null) return false;
            if (target.IsDowned) return false;
            if (skill.targetScope == TargetScope.Enemies && target.IsStealthed && !skill.ignoresStealth)
                return false;

            return IsValidReceiver(target, skill);
        }

        /// <summary>
        /// Get valid targets for a skill based on scope and rank constraints.
        /// </summary>
        public static List<CombatCharacter> GetValidTargets(
            CombatCharacter user, 
            SkillData skill,
            List<CombatCharacter> playerTeam,
            List<CombatCharacter> enemyTeam)
        {
            List<CombatCharacter> pool;

            switch (skill.targetScope)
            {
                case TargetScope.Self:
                    return new List<CombatCharacter> { user };

                case TargetScope.Allies:
                    pool = user.IsPlayerTeam ? playerTeam : enemyTeam;
                    break;

                case TargetScope.Enemies:
                default:
                    pool = user.IsPlayerTeam ? enemyTeam : playerTeam;
                    break;
            }

            return pool
                .Where(c =>
                {
                    if (!c.IsAlive && !c.IsPile && !c.IsDowned)
                        return false;

                    if (!c.OccupiedRanks.Intersect(skill.targetRanks).Any())
                        return false;

                    if (skill.maxTargets > 1)
                    {
                        // For AOE, anchor is valid if at least one unit in the resulting AOE range is targetable
                        var aoeTargets = GetAOETargets(skill, c, playerTeam, enemyTeam);
                        return aoeTargets.Any(t => IsTargetable(t, skill));
                    }
                    else
                    {
                        // For single target, the anchor itself must be targetable
                        return IsTargetable(c, skill);
                    }
                })
                .ToList();
        }

        /// <summary>
        /// Expands a primary (clicked/selected) target to include the trailing targets behind it
        /// up to the skill's maxTargets limit.
        /// </summary>
        public static List<CombatCharacter> GetAOETargets(
            SkillData skill, 
            CombatCharacter primaryTarget,
            List<CombatCharacter> playerTeam,
            List<CombatCharacter> enemyTeam)
        {
            if (primaryTarget == null)
                return new List<CombatCharacter>();

            if (skill.maxTargets <= 1)
                return new List<CombatCharacter> { primaryTarget };

            // Get the team pool (allies or enemies) of the target
            List<CombatCharacter> pool = primaryTarget.IsPlayerTeam ? playerTeam : enemyTeam;

            // Filter to targets that are alive, piles, or downed
            var sortedTeam = pool
                .Where(c => c.IsAlive || c.IsPile || c.IsDowned)
                .OrderBy(c => c.rank) // Sorted from frontmost to backmost
                .ToList();

            int primaryIndex = sortedTeam.IndexOf(primaryTarget);
            if (primaryIndex == -1)
                return new List<CombatCharacter> { primaryTarget };

            // Take characters starting from primary target until the rank budget (maxTargets) is exhausted.
            // A multi-rank character (size > 1) consumes multiple slots in the budget.
            // The primary target is always included, even if its size exceeds the budget.
            var result = new List<CombatCharacter>();
            int ranksUsed = 0;

            for (int i = primaryIndex; i < sortedTeam.Count && ranksUsed < skill.maxTargets; i++)
            {
                var character = sortedTeam[i];
                int charSize = character.characterData != null ? character.characterData.size : 1;
                result.Add(character);
                ranksUsed += charSize;
            }

            return result;
        }
    }
}
