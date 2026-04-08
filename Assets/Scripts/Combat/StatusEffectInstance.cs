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
        public int amplitude;
        public int remainingDuration;

        /// <summary>True if this status has expired.</summary>
        public bool IsExpired => remainingDuration <= 0;

        public StatusEffectInstance(Data.StatusType type, int amplitude, int duration)
        {
            this.type = type;
            this.amplitude = amplitude;
            this.remainingDuration = duration;
        }

        /// <summary>Tick down duration by 1 turn.</summary>
        public void TickDuration()
        {
            remainingDuration--;
        }
    }
}
