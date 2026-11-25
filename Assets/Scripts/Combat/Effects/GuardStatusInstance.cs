using System.Linq;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    public class GuardStatusInstance : StatusEffectInstance
    {
        /// <summary>
        /// The ally being protected by the guardian (Host/Source).
        /// </summary>
        public CombatCharacter GuardedTarget { get; private set; }

        public GuardStatusInstance(CombatCharacter guardian, CombatCharacter protectedTarget, int duration)
            : base(StatusType.Guard, 1, duration)
        {
            this.Source = guardian;
            this.GuardedTarget = protectedTarget;
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);

            // 1. Validate: guardian (host) and protected target must exist, be distinct, alive, and guardian not stunned
            if (host == null || GuardedTarget == null || host == GuardedTarget ||
                Source == null || Source != host ||
                !host.IsAlive || host.isStunned || !GuardedTarget.IsAlive)
            {
                if (host != null)
                {
                    host.RemoveStatus(this);
                }
                return;
            }

            // 2. One guard per guardian: guarding a new target breaks any existing guard held by this guardian
            var existingSelfGuards = host.statusEffects
                .OfType<GuardStatusInstance>()
                .Where(s => s != this)
                .ToList();
            foreach (var oldGuard in existingSelfGuards)
            {
                host.RemoveStatus(oldGuard);
            }

            // 3. Multi-Guard Conflict (Last-In-Wins): remove any other guard currently protecting GuardedTarget
            var existingGuardsOnTarget = GuardResolver.FindAllGuardsProtecting(GuardedTarget)
                .Where(g => g != this)
                .ToList();
            foreach (var oldGuard in existingGuardsOnTarget)
            {
                oldGuard.Host?.RemoveStatus(oldGuard);
            }

            // 4. Mutual Exclusivity (both directions):
            // 4a. Protected target cannot also be guarding someone else
            var targetMaintainedGuards = GuardedTarget.statusEffects
                .OfType<GuardStatusInstance>()
                .ToList();
            foreach (var g in targetMaintainedGuards)
            {
                GuardedTarget.RemoveStatus(g);
            }

            // 4b. Guardian cannot also be protected by someone else
            var guardsProtectingGuardian = GuardResolver.FindAllGuardsProtecting(host)
                .Where(g => g != this)
                .ToList();
            foreach (var g in guardsProtectingGuardian)
            {
                g.Host?.RemoveStatus(g);
            }

            // 5. Subscribe to guardian (Host) and protected target events for immediate breakage
            Host.OnStatusApplied += HandleGuardianStatusChange;
            Host.OnDefeated += HandleGuardianDefeated;
            GuardedTarget.OnDefeated += HandleGuardedTargetDefeated;
        }

        private void HandleGuardianStatusChange(CombatCharacter c, StatusType type, bool success, StatTarget? targetStat)
        {
            if (success && type == StatusType.Stun)
            {
                Host?.RemoveStatus(this); // Immediate break on Guardian Stun
            }
        }

        private void HandleGuardianDefeated(CombatCharacter c, bool wasCritical)
        {
            Host?.RemoveStatus(this); // Immediate break on Guardian Death
        }

        private void HandleGuardedTargetDefeated(CombatCharacter c, bool wasCritical)
        {
            Host?.RemoveStatus(this); // Immediate break on Protected Target Death
        }

        public override void OnRemoved()
        {
            if (Host != null)
            {
                Host.OnStatusApplied -= HandleGuardianStatusChange;
                Host.OnDefeated -= HandleGuardianDefeated;
            }
            if (GuardedTarget != null)
            {
                GuardedTarget.OnDefeated -= HandleGuardedTargetDefeated;
            }
        }
    }
}
