using System;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Grants 1 Part when the skill kills the target.
    /// Pops up the standalone InBattleRewardUI immediately.
    /// </summary>
    [Serializable]
    public class GainPartOnKillEffect : ISkillEffect
    {
        public void Execute(SkillContext context, CombatCharacter target)
        {
            if (context == null || target == null)
                return;

            // Check if the target was actually defeated (HP <= 0 or in Dying/Destroyed state)
            bool isDefeated = target.currentHP <= 0 || target.state == LifeState.Dying || target.state == LifeState.Destroyed;
            if (!isDefeated)
                return;

            // Grant 1 Part
            RunSessionManager.Parts += 1;
            Debug.Log($"[GainPartOnKillEffect] {context.user?.DisplayName} defeated {target.DisplayName}! Gained 1 Part. Total Parts: {RunSessionManager.Parts}");

            // Find the standalone in-battle reward popup
            var rewardUI = UnityEngine.Object.FindAnyObjectByType<Nevergreen.Prototype.InBattleRewardUI>();
            if (rewardUI != null)
            {
                rewardUI.Show(1);
            }
        }
    }
}
