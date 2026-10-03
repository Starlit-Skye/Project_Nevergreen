using System;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    [Serializable]
    public class FlightStatusInstance : StatusEffectInstance
    {
        private BattleSystem _battleSystem;

        public FlightStatusInstance(BattleSystem battleSystem, int amplitude, int duration)
            : base(StatusType.Flight, StatTarget.Dodge, amplitude, duration, AmplitudeType.Flat)
        {
            _battleSystem = battleSystem;
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);
            if (_battleSystem != null)
            {
                _battleSystem.OnActionResolved += HandleActionResolved;
            }
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            if (_battleSystem != null)
            {
                _battleSystem.OnActionResolved -= HandleActionResolved;
            }
        }

        private void HandleActionResolved(CombatCharacter actor, SkillData skill, SkillContext ctx)
        {
            // 1. Only process if Host is the primary target of the attack step
            if (ctx.primaryTarget != Host) return;

            // 2. Only remove if the attack actually HIT the Host
            if (!ctx.didHit) return;

            // 3. Only remove if the skill is a damage-dealing attack skill
            if (skill == null || skill.modifier == null || !skill.modifier.IsDamage) return;

            // 4. Only remove if actual damage was dealt by the hit
            if (ctx.calculatedValue <= 0) return;

            // Condition met: Host was struck by a damaging attack hit -> remove Flight
            Host.RemoveStatus(this);
        }
    }
}
