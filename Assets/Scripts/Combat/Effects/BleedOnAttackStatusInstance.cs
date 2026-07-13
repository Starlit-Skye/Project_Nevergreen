using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    [Serializable]
    public class BleedOnAttackStatusInstance : StatusEffectInstance
    {
        private BattleSystem _battleSystem;
        private int _bleedAmplitude;
        private int _bleedDuration;
        private float _bleedChance;

        public float BleedChance => _bleedChance;

        public BleedOnAttackStatusInstance(BattleSystem battleSystem, int duration, int bleedAmplitude, int bleedDuration, float bleedChance)
            : base(StatusType.BleedOnAttack, StatTarget.Speed, 0, duration)
        {
            _battleSystem = battleSystem;
            _bleedAmplitude = bleedAmplitude;
            _bleedDuration = bleedDuration;
            _bleedChance = bleedChance;
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
            // Only trigger if the Host of this buff is the actor
            if (actor != Host) return;
            if (!ctx.didHit) return;

            // Only apply Bleed if the skill is a damage-dealing skill
            if (skill == null || skill.modifier == null || !skill.modifier.IsDamage) return;

            if (ctx.primaryTarget != null && ctx.primaryTarget.IsAlive)
            {
                int resistance = ctx.primaryTarget.GetResistance(StatusType.Bleed);
                bool applied = CombatCalculator.ResolveStatusApplication(_bleedChance, resistance, ctx.rng);
                if (applied)
                {
                    var bleedInstance = new StatusEffectInstance(StatusType.Bleed, _bleedAmplitude, _bleedDuration);
                    bleedInstance.Source = Host;
                    ctx.primaryTarget.AddStatus(bleedInstance);
                }
                ctx.primaryTarget.TriggerStatusApplied(StatusType.Bleed, applied);
            }
        }
    }
}
