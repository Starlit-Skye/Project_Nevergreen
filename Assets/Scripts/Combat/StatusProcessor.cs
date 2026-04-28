using System.Linq;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    public static class StatusProcessor
    {
        public static bool IsPeriodicType(StatusType type)
        {
            return type == StatusType.Bleed || type == StatusType.Blight || type == StatusType.Restore;
        }

        public static void ProcessPeriodicEffects(CombatCharacter character)
        {
            // GroupBy preserves the order of the first occurrence in the source list.
            var periodicGroups = character.statusEffects
                .Where(s => IsPeriodicType(s.type) && !s.IsExpired)
                .GroupBy(s => s.type);

            foreach (var group in periodicGroups)
            {
                StatusType type = group.Key;
                int aggregateAmplitude = group.Sum(s => s.amplitude);

                if (aggregateAmplitude <= 0) continue;

                // Resolve the effect
                if (type == StatusType.Restore)
                {
                    character.Heal(aggregateAmplitude);
                }
                else
                {
                    character.TakeDamage(aggregateAmplitude);
                }

                // Trigger the character's event
                character.TriggerPeriodicEffectApplied(type, aggregateAmplitude);

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
        }
    }
}
