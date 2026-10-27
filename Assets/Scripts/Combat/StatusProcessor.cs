using System.Linq;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    public static class StatusProcessor
    {
        public static bool IsDotType(StatusType type)
        {
            return type == StatusType.Bleed || type == StatusType.Blight || type == StatusType.Burn;
        }

        public static bool IsPeriodicType(StatusType type)
        {
            return IsDotType(type) || type == StatusType.Restore;
        }

        public static void ProcessPeriodicEffects(CombatCharacter character)
        {
            // Burn increases its own damage by 1 right before triggering damage
            foreach (var status in character.statusEffects)
            {
                if (status.type == StatusType.Burn && !status.IsExpired)
                {
                    status.damagePerTick += 1;
                }
            }

            // GroupBy preserves the order of the first occurrence in the source list.
            var periodicGroups = character.statusEffects
                .Where(s => IsPeriodicType(s.type) && !s.IsExpired)
                .GroupBy(s => s.type);

            foreach (var group in periodicGroups)
            {
                StatusType type = group.Key;
                int aggregateAmount = IsDotType(type) ? group.Sum(s => s.damagePerTick) : group.Sum(s => s.amplitude);

                if (aggregateAmount <= 0) continue;

                // Resolve the effect
                int appliedAmount = aggregateAmount;
                if (type == StatusType.Restore)
                {
                    appliedAmount = character.Heal(aggregateAmount);
                }
                else
                {
                    character.TakeDamage(aggregateAmount);
                }

                // Trigger the character's event
                character.TriggerPeriodicEffectApplied(type, appliedAmount);

                // Death Guard - Stop ticking if character died
                if (!character.IsAlive) break;
            }
        }

        public static void TickDurations(CombatCharacter character, int stunRecoveryResistBonus)
        {
            for (int i = character.statusEffects.Count - 1; i >= 0; i--)
            {
                var status = character.statusEffects[i];
                status.TickDuration();

                if (status.IsExpired)
                {
                    // Stun specific logic
                    if (status.type == StatusType.Stun)
                    {
                        character.AddStatus(new StatusEffectInstance(StatusType.Buff,
                            StatTarget.StunResist, stunRecoveryResistBonus, 1));
                    }

                    // Use the centralized removal path to trigger OnRemoved
                    character.RemoveStatus(status);
                }
            }

            // Re-check stun state
            character.isStunned = character.HasActiveStatus(StatusType.Stun);
            character.TriggerStatsChanged();
        }
    }
}
