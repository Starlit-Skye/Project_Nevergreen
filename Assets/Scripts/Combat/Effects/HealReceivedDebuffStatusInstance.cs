using System;
using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    [Serializable]
    public class HealReceivedDebuffStatusInstance : StatusEffectInstance
    {
        private BattleSystem _battleSystem;

        public HealReceivedDebuffStatusInstance(BattleSystem battleSystem, int amplitude, int duration)
            : base(StatusType.HealReceivedReduction, StatTarget.Speed, amplitude, duration)
        {
            _battleSystem = battleSystem;
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);

            if (_battleSystem != null)
            {
                _battleSystem.OnBeforeDamageCalculation += HandleBeforeDamageCalculation;
            }
        }

        public override void OnRemoved()
        {
            base.OnRemoved();

            if (_battleSystem != null)
            {
                _battleSystem.OnBeforeDamageCalculation -= HandleBeforeDamageCalculation;
            }
        }

        private void HandleBeforeDamageCalculation(SkillContext ctx)
        {
            // Ensure this is a healing skill
            if (ctx.skill != null && ctx.skill.modifier.IsHeal)
            {
                // Ensure the host (the one who has this debuff) is one of the targets
                if (Host != null && ctx.targets != null && ctx.targets.Contains(Host))
                {
                    string key = $"HealReceived_{Host.GetInstanceID()}";
                    float current = ctx.extra.ContainsKey(key) ? (float)ctx.extra[key] : 0f;
                    // Reducing heal received: the amplitude is the percent reduction (e.g. 30 = -30% heal received).
                    // So we subtract the percentage from the key's value.
                    ctx.extra[key] = current - (amplitude / 100f);
                }
            }
        }
    }
}
