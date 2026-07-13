using System;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat.Effects
{
    /// <summary>
    /// Applies a unique buff that boosts the damage multiplier of a specific skill when it's executed,
    /// then consumes itself.
    /// </summary>
    [Serializable]
    public class ApplySkillBoostEffect : ISkillEffect
    {
        [Tooltip("The skill that this buff will boost.")]
        public SkillData targetSkill;

        [Tooltip("The amount to increase the damage multiplier by (e.g., 50 for +50%).")]
        public int amplitude = 50;

        [Tooltip("How many turns the buff lasts before expiring if the skill is not used.")]
        public int duration = 3;

        public void Execute(SkillContext ctx, CombatCharacter target)
        {
            if (targetSkill == null)
            {
                Debug.LogWarning("ApplySkillBoostEffect: targetSkill is null. Cannot apply buff.");
                return;
            }

            // Only apply on hit/crit, not if it missed
            ctx.EnsureHitResolved(target);
            if (ctx.didHit)
            {
                var buffInstance = new SkillBoostStatusInstance(targetSkill.skillId, amplitude, duration, targetSkill.displayName);
                buffInstance.Source = ctx.user;
                target.AddStatus(buffInstance);
            }
        }
    }
}
