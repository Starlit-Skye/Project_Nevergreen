using System;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    public static class StatusInstanceFactory
    {
        /// <summary>
        /// Resolves which character should hold the status effect instance.
        /// Guard lives on the guardian (<paramref name="user"/> for standard Guard skills);
        /// all other statuses live on <paramref name="target"/>.
        /// </summary>
        public static CombatCharacter ResolveStatusHost(StatusType type, CombatCharacter user, CombatCharacter target)
        {
            return type == StatusType.Guard ? user : target;
        }

        /// <summary>
        /// Creates the appropriate StatusEffectInstance subclass based on the StatusType.
        /// </summary>
        /// <param name="context">The skill context initiating the status application.</param>
        /// <param name="type">The type of status to create.</param>
        /// <param name="targetStat">The stat to target, if applicable (e.g. for Buff/Debuff).</param>
        /// <param name="amplitude">The power/stack size.</param>
        /// <param name="duration">Duration in turns.</param>
        /// <param name="amplitudeType">How the amplitude is applied.</param>
        /// <param name="guardian">Who protects <paramref name="protectedTarget"/> when type == Guard. Defaults to context.user when null.</param>
        /// <param name="protectedTarget">Who is protected by the guardian when type == Guard.</param>
        /// <returns>A new StatusEffectInstance with its Source assigned to context.user (or guardian for Guard).</returns>
        public static StatusEffectInstance Create(
            SkillContext context,
            StatusType type,
            StatTarget targetStat,
            int amplitude,
            int duration,
            AmplitudeType amplitudeType,
            CombatCharacter guardian = null,
            CombatCharacter protectedTarget = null)
        {
            StatusEffectInstance instance;
            switch (type)
            {
                case StatusType.Guard:
                    if (protectedTarget == null)
                    {
                        Debug.LogError("[StatusInstanceFactory] Guard status requires a non-null protectedTarget.");
                        return null;
                    }
                    // Guard's constructor sets Source = guardian and GuardedTarget = protectedTarget
                    return new GuardStatusInstance(guardian ?? context.user, protectedTarget, duration);
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
            if (StatusProcessor.IsDotType(type))
            {
                instance.damagePerTick = ResolveDotDamage(context.user, amplitude);
            }
            return instance;
        }

        /// <summary>
        /// Calculates the snapshot damage dealt per tick for DOT statuses (Bleed, Blight, Burn)
        /// based on the applier's effective attack and authored amplitude (as percentage of attack).
        /// </summary>
        public static int ResolveDotDamage(CombatCharacter applier, int amplitudePercent)
        {
            int effAttack = applier != null ? applier.GetEffectiveStats().attack : 0;
            int rounded = (int)Math.Round(effAttack * amplitudePercent / 100.0, MidpointRounding.AwayFromZero);
            return Math.Max(1, rounded);
        }
    }
}
