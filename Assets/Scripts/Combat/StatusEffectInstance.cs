using System;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Runtime status effect instance applied to a combat character.
    /// Stacks are tracked individually but displayed/applied as aggregate.
    /// </summary>
    [Serializable]
    public class StatusEffectInstance
    {
        public Data.StatusType type;
        public Data.StatTarget targetStat;
        public int amplitude;
        public int remainingDuration;
        public AmplitudeType amplitudeType;

        public CombatCharacter Source { get; set; }
        public CombatCharacter Host { get; set; }

        /// <summary>True if this status has expired.</summary>
        public bool IsExpired => remainingDuration <= 0;

        public StatusEffectInstance(Data.StatusType type, int amplitude, int duration)
        {
            this.type = type;
            this.targetStat = Data.StatTarget.Speed;
            this.amplitude = amplitude;
            this.remainingDuration = duration;
            this.amplitudeType = AmplitudeType.Default;
        }

        /// <summary>
        /// Constructor for Buff/Debuff statuses that target a specific stat.
        /// </summary>
        public StatusEffectInstance(Data.StatusType type, Data.StatTarget targetStat, int amplitude, int duration, AmplitudeType amplitudeType = AmplitudeType.Default)
        {
            this.type = type;
            this.targetStat = targetStat;
            this.amplitude = amplitude;
            this.remainingDuration = duration;
            this.amplitudeType = amplitudeType;
        }

        /// <summary>Tick down duration by 1 turn.</summary>
        public void TickDuration()
        {
            remainingDuration--;
        }

        public virtual void OnAdded(CombatCharacter host)
        {
            Host = host;
        }

        public virtual void OnRemoved()
        {
        }

        public virtual void OnSkillExecute(SkillContext ctx)
        {
        }
    }
}
