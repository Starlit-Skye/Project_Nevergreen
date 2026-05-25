using System;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Increases the duration of all existing Buff status effects on the target by a configurable amount.
    /// </summary>
    [Serializable]
    public class IncreaseBuffDurationEffect : ISkillEffect
    {
        [Tooltip("The amount to increase the duration of buffs by.")]
        public int durationIncreaseAmount = 2;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            context.EnsureHitResolved(target);
            if (!context.didHit)
            {
                return;
            }

            int buffsIncreased = 0;
            foreach (var status in target.statusEffects)
            {
                if (status.type == Data.StatusType.Buff && !status.IsExpired)
                {
                    status.remainingDuration += durationIncreaseAmount;
                    buffsIncreased++;
                }
            }

            if (buffsIncreased > 0)
            {
                Debug.Log($"  -> {target.DisplayName} had {buffsIncreased} buff(s) duration increased by {durationIncreaseAmount}");
                target.TriggerStatsChanged();
            }
        }
    }
}
