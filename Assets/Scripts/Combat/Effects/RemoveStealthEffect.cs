using System;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Skill effect that removes Stealth status from the target.
    /// By default, checks if the overall action hit before attempting removal.
    /// </summary>
    [Serializable]
    public class RemoveStealthEffect : ISkillEffect
    {
        [Tooltip("Should this effect still attempt removal even if the attack 'Missed'?")]
        public bool ignoreMiss = false;

        public void Execute(SkillContext context, CombatCharacter target)
        {
            context.EnsureHitResolved(target);
            if (!context.didHit && !ignoreMiss)
            {
                return;
            }

            var stealthStatuses = target.statusEffects
                .Where(s => s.type == StatusType.Stealth && !s.IsExpired)
                .ToList();

            foreach (var status in stealthStatuses)
            {
                target.RemoveStatus(status);
            }
            
            Debug.Log($"  -> Removed Stealth from {target.DisplayName}");
        }
    }
}
