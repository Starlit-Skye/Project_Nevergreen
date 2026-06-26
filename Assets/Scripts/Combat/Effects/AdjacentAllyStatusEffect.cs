using System;
using System.Collections.Generic;
using System.Linq;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    public enum AllyDirection
    {
        InFront,
        Behind
    }

    /// <summary>
    /// Automatically applies a status effect (Buff/Debuff/Guard/etc.) to an adjacent ally (in front or behind).
    /// Prevents duplicate application during multi-hit or multi-target skill execution.
    /// </summary>
    [Serializable]
    public class AdjacentAllyStatusEffect : ISkillEffect
    {
        [Tooltip("The status to apply to the adjacent ally.")]
        public StatusType statusType = StatusType.Buff;

        [Tooltip("The specific stat to modify if this is a Buff or Debuff type.")]
        public StatTarget targetStat = StatTarget.Speed;

        [Tooltip("Chance to apply the effect to the adjacent ally. Bypasses resistance.")]
        [Range(0, 100)]
        public float applicationChance = 100f;

        [Tooltip("Power/Stack size of the status.")]
        public int amplitude = 1;

        [Tooltip("How the amplitude is applied.")]
        public AmplitudeType amplitudeType = AmplitudeType.Default;

        [Tooltip("Duration in turns.")]
        public int duration = 3;

        [Tooltip("Direction to search for an ally relative to the user.")]
        public AllyDirection direction = AllyDirection.InFront;

        [Tooltip("Should this effect still attempt application even if the attack 'Missed'? Default true.")]
        public bool ignoreMiss = true;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            // If the attack missed and we don't ignore misses, do nothing
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            // Ensure we only apply this effect once per skill execution (handling multi-hit/multi-target)
            string appliedKey = $"AdjacentAllyStatusApplied_{this.GetHashCode()}";
            if (context.extra.ContainsKey(appliedKey))
            {
                return;
            }
            context.extra[appliedKey] = true;

            // Find all potential allies of the user
            List<CombatCharacter> allies;
            if (context.battleSystem != null)
            {
                allies = context.user.IsPlayerTeam ? context.battleSystem.PlayerTeam : context.battleSystem.EnemyTeam;
            }
            else
            {
                allies = UnityEngine.Object.FindObjectsOfType<CombatCharacter>()
                    .Where(c => c.team == context.user.team)
                    .ToList();
            }

            // Calculate the target rank in that direction
            int userSize = (context.user.characterData != null) ? context.user.characterData.size : 1;
            int targetRank = (direction == AllyDirection.InFront) ? context.user.rank - 1 : context.user.rank + userSize;

            // Find the ally whose occupied ranks contains targetRank
            CombatCharacter targetAlly = allies.FirstOrDefault(c => c.IsAlive && c.OccupiedRanks.Contains(targetRank));

            if (targetAlly == null)
            {
                Debug.Log($"[AdjacentAllyStatusEffect] No alive adjacent ally found at rank {targetRank} relative to {context.user.DisplayName}");
                return;
            }

            // Self/Ally buffs generally bypass resistances, so we use 0 resistance
            bool applied = CombatCalculator.ResolveStatusApplication(applicationChance, 0, context.rng);

            if (applied)
            {
                StatusEffectInstance instance;
                if (statusType == StatusType.Guard)
                {
                    instance = new GuardStatusInstance(targetAlly, duration);
                }
                else if (statusType == StatusType.Move)
                {
                    instance = new MoveStatusInstance(context.battleSystem, amplitude);
                    instance.Source = context.user;
                }
                else if (statusType == StatusType.Stealth)
                {
                    instance = new StealthStatusInstance(duration);
                    instance.Source = context.user;
                }
                else if (statusType == StatusType.HealReceivedReduction)
                {
                    instance = new HealReceivedDebuffStatusInstance(context.battleSystem, amplitude, duration);
                    instance.Source = context.user;
                }
                else
                {
                    instance = new StatusEffectInstance(statusType, targetStat, amplitude, duration, amplitudeType);
                    instance.Source = context.user;
                }

                targetAlly.AddStatus(instance);
                Debug.Log($"  -> {context.user.DisplayName} applied adjacent ally status {statusType} to {targetAlly.DisplayName} (amp:{amplitude}, dur:{duration})");
            }

            // Trigger the status applied event on the target ally
            targetAlly.TriggerStatusApplied(statusType, applied);
        }
    }
}
