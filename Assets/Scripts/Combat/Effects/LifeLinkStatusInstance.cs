using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    public class LifeLinkStatusInstance : StatusEffectInstance
    {
        private BattleSystem _battleSystem;

        public LifeLinkStatusInstance(BattleSystem battleSystem, int amplitude, int duration)
            : base(StatusType.LifeLink, StatTarget.MaxHP, amplitude, duration, AmplitudeType.Percentage)
        {
            _battleSystem = battleSystem;
        }

        public override bool TryInterceptDefeat(bool isCritical)
        {
            if (_battleSystem == null)
            {
                _battleSystem = Object.FindObjectOfType<BattleSystem>();
            }

            if (_battleSystem == null) return false;

            // Check if there are other allies with LifeLink who are ALIVE (not downed, not pile, not destroyed)
            var allies = _battleSystem.EnemyTeam;
            if (Host != null && Host.IsPlayerTeam)
            {
                allies = _battleSystem.PlayerTeam;
            }

            bool hasLivingLinkedAlly = allies.Any(c => 
                c != Host && 
                c.IsAlive && 
                c.statusEffects.Any(s => s.type == StatusType.LifeLink && !s.IsExpired));

            if (hasLivingLinkedAlly)
            {
                Debug.Log($"[LifeLink] {Host.DisplayName} intercepts defeat and enters Downed state!");
                Host.EnterDownedState();
                return true; // We intercepted the defeat
            }
            else
            {
                // No living linked allies. This link collapses.
                CollapseLink(allies);
                return false; // Normal defeat proceeds for this host
            }
        }

        private void CollapseLink(System.Collections.Generic.List<CombatCharacter> team)
        {
            // Find all downed allies with Life Link and execute their defeat flow
            var downedLinkedAllies = team.Where(c => 
                c != Host && 
                c.IsDowned && 
                c.statusEffects.Any(s => s.type == StatusType.LifeLink && !s.IsExpired)).ToList();
            
            foreach (var ally in downedLinkedAllies)
            {
                Debug.Log($"[LifeLink] Collapse kills previously downed ally {ally.DisplayName}!");
                ally.ProcessDefeatFromDowned();
            }
        }
    }
}
