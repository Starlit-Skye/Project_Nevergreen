using System;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Runtime status effect instance that stores a specific skill ID.
    /// Intercepted during skill execution to boost its damage multiplier.
    /// </summary>
    [Serializable]
    public class SkillBoostStatusInstance : StatusEffectInstance
    {
        public string targetSkillId;
        public string targetSkillDisplayName;
        public int customAmplitude; // Prevents base stat system from applying it as a stat modifier

        public SkillBoostStatusInstance(string targetSkillId, int customAmplitude, int duration, string targetSkillDisplayName = null)
            : base(StatusType.Buff, StatTarget.Attack, 0, duration, AmplitudeType.Percentage)
        {
            this.targetSkillId = targetSkillId;
            this.customAmplitude = customAmplitude;
            this.targetSkillDisplayName = targetSkillDisplayName;
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
        }

        public override void OnSkillExecute(SkillContext ctx)
        {
            if (ctx.skill.skillId == targetSkillId)
            {
                ctx.damageMultiplier += (customAmplitude / 100f);
                Host.RemoveStatus(this);
            }
        }
    }
}
