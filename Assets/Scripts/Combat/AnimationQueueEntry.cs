using System;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Represents a single animation entry in the combat animation queue.
    /// Stores the identity and expected duration of one animation step.
    /// </summary>
    public struct AnimationQueueEntry
    {
        public readonly string animationId;
        public readonly string animationName;
        public readonly float durationSeconds;

        public AnimationQueueEntry(string id, string name, float duration)
        {
            animationId = id;
            animationName = name;
            durationSeconds = duration;
        }
    }

    /// <summary>
    /// Snapshot of the animation queue's current state.
    /// Passed via events so consumers know whether inputs should be locked.
    /// </summary>
    public struct AnimationQueueState
    {
        public readonly int queueCount;
        public readonly bool isInputLocked;
        public readonly float expectedLengthSeconds;
        public readonly float lockElapsedSeconds;

        public AnimationQueueState(int count, bool locked, float expected, float elapsed)
        {
            queueCount = count;
            isInputLocked = locked;
            expectedLengthSeconds = expected;
            lockElapsedSeconds = elapsed;
        }
    }

    /// <summary>
    /// Safeguard type identifiers.
    /// </summary>
    public enum SafeguardType
    {
        QueueCap,
        LockOvertime
    }
}
