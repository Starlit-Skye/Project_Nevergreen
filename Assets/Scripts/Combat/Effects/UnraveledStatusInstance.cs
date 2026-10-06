using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    [Serializable]
    public class UnraveledStatusInstance : StatusEffectInstance
    {
        private BattleSystem _battleSystem;

        public UnraveledStatusInstance(BattleSystem battleSystem, int amplitude, int duration)
            : base(StatusType.Unraveled, StatTarget.Defense, amplitude, duration)
        {
            _battleSystem = battleSystem;
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);

            if (_battleSystem != null)
            {
                _battleSystem.OnBeforeDamageCalculationPerTarget += HandleBeforeDamageCalculationPerTarget;
            }
        }

        public override void OnRemoved()
        {
            base.OnRemoved();

            if (_battleSystem != null)
            {
                _battleSystem.OnBeforeDamageCalculationPerTarget -= HandleBeforeDamageCalculationPerTarget;
            }
        }

        private void HandleBeforeDamageCalculationPerTarget(SkillContext ctx, CombatCharacter target)
        {
            if (target != Host) return;
            
            if (ctx.skill != null && ctx.skill.modifier != null && ctx.skill.modifier.IsDamage)
            {
                ctx.damageMultiplier += (amplitude / 100f);
            }
        }
    }
}
