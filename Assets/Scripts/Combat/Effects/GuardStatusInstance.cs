using System.Linq;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    public class GuardStatusInstance : StatusEffectInstance
    {
        public GuardStatusInstance(CombatCharacter guardian, int duration) 
            : base(StatusType.Guard, 1, duration)
        {
            this.Source = guardian;
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);
            
            // 1. Multi-Guard Conflict (Last-In-Wins)
            var oldGuard = host.statusEffects.OfType<GuardStatusInstance>()
                .FirstOrDefault(s => s != this);
            if (oldGuard != null)
            {
                host.RemoveStatus(oldGuard);
            }

            // 2. Nested Guarding Rule
            BreakGuardsMaintainedBy(host);

            // 3. Prevent application if Guardian is already dead or stunned
            if (Source == null || !Source.IsAlive || Source.isStunned)
            {
                host.RemoveStatus(this);
                return;
            }

            // 4. Subscribe to Guardian events for immediate breakage
            if (Source != null)
            {
                Source.OnStatusApplied += HandleGuardianStatusChange;
                Source.OnDefeated += HandleGuardianDefeated;
            }

            // 4. Subscribe to Host events (self cleanup)
            Host.OnDefeated += HandleHostDefeated;
        }

        private void HandleGuardianStatusChange(CombatCharacter c, StatusType type, bool success)
        {
            if (success && type == StatusType.Stun)
            {
                Host.RemoveStatus(this); // Immediate break on Guardian Stun
            }
        }

        private void HandleGuardianDefeated(CombatCharacter c)
        {
            Host.RemoveStatus(this); // Immediate break on Guardian Death
        }

        private void HandleHostDefeated(CombatCharacter c)
        {
            Host.RemoveStatus(this); // Immediate break on Host Death
        }

        public override void OnRemoved()
        {
            if (Source != null)
            {
                Source.OnStatusApplied -= HandleGuardianStatusChange;
                Source.OnDefeated -= HandleGuardianDefeated;
            }
            Host.OnDefeated -= HandleHostDefeated;
        }

        private void BreakGuardsMaintainedBy(CombatCharacter character)
        {
            // Find all characters in the battle and check their status lists
            var allCharacters = UnityEngine.Object.FindObjectsOfType<CombatCharacter>();
            foreach (var c in allCharacters)
            {
                var guardsToBreak = c.statusEffects.OfType<GuardStatusInstance>().Where(g => g.Source == character).ToList();
                foreach (var g in guardsToBreak)
                {
                    c.RemoveStatus(g);
                }
            }
        }
    }
}
