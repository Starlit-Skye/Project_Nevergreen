using System;
using System.Collections.Generic;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Applies the Living Fragments status to a target.
    /// Stores enemy prefabs to summon when the target is destroyed.
    /// </summary>
    [Serializable]
    public class LivingFragmentsStatusEffect : ISkillEffect
    {
        [Tooltip("Prefabs containing CombatCharacter components to summon upon destruction.")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();

        [Tooltip("Chance to apply the status.")]
        [Range(0, 100)]
        public float applicationChance = 100f;

        [Tooltip("Duration in turns.")]
        public int duration = 3;

        [Tooltip("Should this effect still attempt application even if the attack 'Missed'?")]
        public bool ignoreMiss = false;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            int resistance = target.GetResistance(StatusType.LivingFragments);
            bool applied = CombatCalculator.ResolveStatusApplication(applicationChance, resistance, context.rng);

            if (applied)
            {
                var instance = new LivingFragmentsStatusInstance(context.battleSystem, enemyPrefabs, duration)
                {
                    Source = context.user
                };

                target.AddStatus(instance);
                Debug.Log($"  -> Applied Living Fragments to {target.DisplayName} (dur:{duration})");
            }

            target.TriggerStatusApplied(StatusType.LivingFragments, applied);
        }
    }
}
