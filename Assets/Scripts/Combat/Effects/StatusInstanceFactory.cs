using System;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    public static class StatusInstanceFactory
    {
        /// <summary>
        /// Creates the appropriate StatusEffectInstance subclass based on the StatusType.
        /// </summary>
        /// <param name="context">The skill context initiating the status application.</param>
        /// <param name="type">The type of status to create.</param>
        /// <param name="targetStat">The stat to target, if applicable (e.g. for Buff/Debuff).</param>
        /// <param name="amplitude">The power/stack size.</param>
        /// <param name="duration">Duration in turns.</param>
        /// <param name="amplitudeType">How the amplitude is applied.</param>
        /// <param name="guardian">Who protects the host when type == Guard. Defaults to context.user when null.</param>
        /// <returns>A new StatusEffectInstance with its Source assigned to context.user.</returns>
        public static StatusEffectInstance Create(
            SkillContext context,
            StatusType type,
            StatTarget targetStat,
            int amplitude,
            int duration,
            AmplitudeType amplitudeType,
            CombatCharacter guardian = null)
        {
            StatusEffectInstance instance;
            switch (type)
            {
                case StatusType.Guard:
                    // Guard's constructor already sets Source = guardian
                    return new GuardStatusInstance(guardian ?? context.user, duration);
                case StatusType.Move:
                    instance = new MoveStatusInstance(context.battleSystem, amplitude);
                    break;
                case StatusType.Stealth:
                    instance = new StealthStatusInstance(duration);
                    break;
                case StatusType.Shuffle:
                    instance = new ShuffleStatusInstance(context.battleSystem, context.rng);
                    break;
                case StatusType.HealReceivedReduction:
                    instance = new HealReceivedDebuffStatusInstance(context.battleSystem, amplitude, duration);
                    break;
                case StatusType.Flight:
                    instance = new FlightStatusInstance(context.battleSystem, amplitude, duration);
                    break;
                case StatusType.Unraveled:
                    instance = new UnraveledStatusInstance(context.battleSystem, amplitude, duration);
                    break;
                case StatusType.LivingFragments:
                    // Living Fragments requires enemyPrefabs list, which is not available in the factory arguments.
                    // It should be instantiated directly by LivingFragmentsStatusEffect instead.
                    Debug.LogWarning("[StatusInstanceFactory] LivingFragments cannot be created via generic factory. Defaulting to empty instance.");
                    instance = new LivingFragmentsStatusInstance(context.battleSystem, null, duration);
                    break;
                case StatusType.LifeLink:
                    instance = new LifeLinkStatusInstance(context.battleSystem, amplitude, duration);
                    break;
                default:
                    instance = new StatusEffectInstance(type, targetStat, amplitude, duration, amplitudeType);
                    break;
            }

            instance.Source = context.user;
            return instance;
        }
    }
}
