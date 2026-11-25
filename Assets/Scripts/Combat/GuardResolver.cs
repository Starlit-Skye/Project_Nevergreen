using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Stateless lookup utility for Guard relationships.
    /// The GuardStatusInstance held on the guardian is the single source of truth.
    /// </summary>
    public static class GuardResolver
    {
        /// <summary>
        /// Returns the team candidates for <paramref name="character"/> using <paramref name="battleSystem"/>
        /// when its team list is populated, falling back to a scene-wide lookup otherwise.
        /// </summary>
        public static IEnumerable<CombatCharacter> GetTeam(CombatCharacter character, BattleSystem battleSystem = null)
        {
            if (character == null) return Enumerable.Empty<CombatCharacter>();

            if (battleSystem != null)
            {
                var team = character.IsPlayerTeam ? battleSystem.PlayerTeam : battleSystem.EnemyTeam;
                if (team != null && team.Count > 0)
                {
                    return team;
                }
            }

            return Object.FindObjectsOfType<CombatCharacter>()
                .Where(c => c != null && c.team == character.team);
        }

        /// <summary>
        /// Finds the first non-expired GuardStatusInstance among <paramref name="candidates"/>
        /// whose <see cref="GuardStatusInstance.GuardedTarget"/> matches <paramref name="protectedTarget"/>.
        /// </summary>
        public static GuardStatusInstance GetActiveGuardStatus(CombatCharacter protectedTarget, IEnumerable<CombatCharacter> candidates)
        {
            if (protectedTarget == null || candidates == null) return null;

            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;

                var guard = candidate.statusEffects
                    .OfType<GuardStatusInstance>()
                    .FirstOrDefault(s => !s.IsExpired && s.GuardedTarget == protectedTarget);

                if (guard != null)
                {
                    return guard;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns all GuardStatusInstances across the scene currently protecting <paramref name="protectedTarget"/>.
        /// </summary>
        public static List<GuardStatusInstance> FindAllGuardsProtecting(CombatCharacter protectedTarget)
        {
            var results = new List<GuardStatusInstance>();
            if (protectedTarget == null) return results;

            var allCharacters = Object.FindObjectsOfType<CombatCharacter>();
            foreach (var c in allCharacters)
            {
                if (c == null) continue;
                foreach (var s in c.statusEffects)
                {
                    if (s is GuardStatusInstance guard && guard.GuardedTarget == protectedTarget)
                    {
                        results.Add(guard);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Returns the active guardian protecting <paramref name="protectedTarget"/> if valid
        /// (alive and not stunned), or null otherwise.
        /// </summary>
        public static CombatCharacter GetActiveGuardian(CombatCharacter protectedTarget, SkillContext context = null)
        {
            if (protectedTarget == null) return null;

            var candidates = GetTeam(protectedTarget, context?.battleSystem);
            var guard = GetActiveGuardStatus(protectedTarget, candidates);
            if (guard == null) return null;

            var guardian = guard.Host ?? guard.Source;
            if (guardian == null || !guardian.IsAlive || guardian.isStunned)
            {
                return null;
            }

            return guardian;
        }

        /// <summary>
        /// Returns the active GuardStatusInstance held by <paramref name="guardian"/> (at most one), or null.
        /// </summary>
        public static GuardStatusInstance GetGuardStatusHeldBy(CombatCharacter guardian)
        {
            if (guardian == null) return null;

            return guardian.statusEffects
                .OfType<GuardStatusInstance>()
                .FirstOrDefault(s => !s.IsExpired);
        }
    }
}

