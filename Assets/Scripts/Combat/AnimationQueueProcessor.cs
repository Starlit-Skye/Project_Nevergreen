using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// FIFO animation queue processor for combat.
    /// Manages sequential playback of animation entries, locks combat input
    /// while animations are active, and applies safeguards to prevent soft-locks.
    /// 
    /// Spec: Docs/specs/systems/SYSTEM_SPEC_ANIMATION_RUNTIME.md
    /// </summary>
    public class AnimationQueueProcessor : MonoBehaviour
    {
        // --- Safeguard thresholds (from spec) ---
        private const int QUEUE_CAP = 15;
        private const float LOCK_OVERTIME_BUFFER = 5.0f;

        // --- Queue state ---
        private readonly Queue<AnimationQueueEntry> _queue = new Queue<AnimationQueueEntry>();
        private AnimationQueueEntry? _currentEntry;
        private float _currentEntryElapsed;

        // --- Lock tracking ---
        private bool _isInputLocked;
        private float _expectedTotalLength;
        private float _lockElapsedSeconds;

        // --- Events matching spec contracts ---

        /// <summary>combat_animation_enqueued: fired after an entry is added to the queue.</summary>
        public event Action<AnimationQueueEntry, int> OnAnimationEnqueued;

        /// <summary>combat_animation_finished: fired when one entry finishes playback.</summary>
        public event Action<AnimationQueueEntry, float> OnAnimationFinished;

        /// <summary>combat_input_lock_changed: fired when the lock state transitions.</summary>
        public event Action<AnimationQueueState> OnInputLockChanged;

        /// <summary>combat_animation_safeguard_triggered: fired when a safeguard clears the queue.</summary>
        public event Action<SafeguardType, AnimationQueueState> OnSafeguardTriggered;

        /// <summary>True while any animations are queued or playing.</summary>
        public bool IsBusy => _currentEntry.HasValue || _queue.Count > 0;

        /// <summary>True while the queue is locking input.</summary>
        public bool IsInputLocked => _isInputLocked;

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>
        /// Enqueue a new animation. Automatically locks input on first entry.
        /// </summary>
        public void Enqueue(string id, string name, float durationSeconds)
        {
            // Queue-cap safeguard: if already at cap, trigger and bail
            if (_queue.Count + (_currentEntry.HasValue ? 1 : 0) >= QUEUE_CAP)
            {
                TriggerSafeguard(SafeguardType.QueueCap);
                return;
            }

            var entry = new AnimationQueueEntry(id, name, Mathf.Max(0f, durationSeconds));
            _queue.Enqueue(entry);
            _expectedTotalLength += entry.durationSeconds;

            // Lock input on first enqueue
            if (!_isInputLocked)
            {
                SetInputLocked(true);
            }

            int totalCount = _queue.Count + (_currentEntry.HasValue ? 1 : 0);
            OnAnimationEnqueued?.Invoke(entry, totalCount);

            Debug.Log($"[AnimQueue] Enqueued '{name}' ({durationSeconds:F2}s) | queue:{totalCount}");
        }

        // -----------------------------------------------------------------------
        // MonoBehaviour
        // -----------------------------------------------------------------------

        private void Update()
        {
            // Nothing to do when idle
            if (!IsBusy)
            {
                // Unlock if still locked (queue just drained)
                if (_isInputLocked)
                {
                    SetInputLocked(false);
                    ResetLockTracking();
                }
                return;
            }

            // Advance lock timer
            _lockElapsedSeconds += Time.deltaTime;

            // Progress current animation
            if (_currentEntry.HasValue)
            {
                _currentEntryElapsed += Time.deltaTime;

                if (_currentEntryElapsed >= _currentEntry.Value.durationSeconds)
                {
                    FinishCurrentEntry();
                }
            }

            // Start next entry if none is playing
            if (!_currentEntry.HasValue && _queue.Count > 0)
            {
                StartNextEntry();
            }

            // Lock-overtime safeguard
            if (_isInputLocked &&
                _lockElapsedSeconds > _expectedTotalLength + LOCK_OVERTIME_BUFFER)
            {
                TriggerSafeguard(SafeguardType.LockOvertime);
            }
        }

        // -----------------------------------------------------------------------
        // Internal
        // -----------------------------------------------------------------------

        private void StartNextEntry()
        {
            _currentEntry = _queue.Dequeue();
            _currentEntryElapsed = 0f;

            Debug.Log($"[AnimQueue] Playing '{_currentEntry.Value.animationName}'");
        }

        private void FinishCurrentEntry()
        {
            var finished = _currentEntry.Value;
            _currentEntry = null;

            OnAnimationFinished?.Invoke(finished, finished.durationSeconds);
            Debug.Log($"[AnimQueue] Finished '{finished.animationName}'");
        }

        private void SetInputLocked(bool locked)
        {
            if (_isInputLocked == locked) return;

            _isInputLocked = locked;
            OnInputLockChanged?.Invoke(BuildState());

            Debug.Log($"[AnimQueue] Input {(locked ? "LOCKED" : "UNLOCKED")}");
        }

        private void ResetLockTracking()
        {
            _expectedTotalLength = 0f;
            _lockElapsedSeconds = 0f;
        }

        private void TriggerSafeguard(SafeguardType type)
        {
            Debug.LogWarning($"[AnimQueue] Safeguard triggered: {type}");

            var state = BuildState();
            OnSafeguardTriggered?.Invoke(type, state);

            // Clear everything
            _queue.Clear();
            _currentEntry = null;
            _currentEntryElapsed = 0f;

            SetInputLocked(false);
            ResetLockTracking();
        }

        private AnimationQueueState BuildState()
        {
            return new AnimationQueueState(
                _queue.Count + (_currentEntry.HasValue ? 1 : 0),
                _isInputLocked,
                _expectedTotalLength,
                _lockElapsedSeconds
            );
        }
    }
}
