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
        private readonly Queue<IAnimationStep> _queue = new Queue<IAnimationStep>();
        private IAnimationStep _currentStep;

        // --- Lock tracking ---
        private bool _isInputLocked;
        private float _expectedTotalLength;
        private float _lockElapsedSeconds;

        // --- Batch tracking ---
        private ParallelStep _activeBatch;

        // --- Events matching spec contracts ---

        /// <summary>combat_animation_enqueued: fired after an entry is added to the queue.</summary>
        public event Action<IAnimationStep, int> OnAnimationEnqueued;

        /// <summary>combat_animation_finished: fired when one entry finishes playback.</summary>
        public event Action<IAnimationStep, float> OnAnimationFinished;

        /// <summary>combat_input_lock_changed: fired when the lock state transitions.</summary>
        public event Action<AnimationQueueState> OnInputLockChanged;

        /// <summary>combat_animation_safeguard_triggered: fired when a safeguard clears the queue.</summary>
        public event Action<SafeguardType, AnimationQueueState> OnSafeguardTriggered;

        /// <summary>True while any animations are queued or playing.</summary>
        public bool IsBusy => _currentStep != null || _queue.Count > 0;

        /// <summary>True while the queue is locking input.</summary>
        public bool IsInputLocked => _isInputLocked;

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>
        /// Begin combining subsequent Enqueue calls into a single ParallelStep.
        /// </summary>
        public void BeginBatch(string batchName)
        {
            if (_activeBatch == null)
            {
                _activeBatch = new ParallelStep(batchName);
            }
        }

        /// <summary>
        /// End the current batch and enqueue it as a single parallel group.
        /// </summary>
        public void EndBatch()
        {
            if (_activeBatch != null)
            {
                var batchToEnqueue = _activeBatch;
                _activeBatch = null;
                Enqueue(batchToEnqueue);
            }
        }

        /// <summary>
        /// Enqueue a new animation step. Automatically locks input on first entry.
        /// </summary>
        public void Enqueue(IAnimationStep step)
        {
            if (step == null) return;

            if (_activeBatch != null)
            {
                _activeBatch.AddStep(step);
                return;
            }

            // Queue-cap safeguard: if already at cap, trigger and bail
            if (_queue.Count + (_currentStep != null ? 1 : 0) >= QUEUE_CAP)
            {
                TriggerSafeguard(SafeguardType.QueueCap);
                return;
            }

            _queue.Enqueue(step);
            _expectedTotalLength += step.ExpectedDuration;

            // Lock input on first enqueue
            if (!_isInputLocked)
            {
                SetInputLocked(true);
            }

            int totalCount = _queue.Count + (_currentStep != null ? 1 : 0);
            OnAnimationEnqueued?.Invoke(step, totalCount);

            Debug.Log($"[AnimQueue] Enqueued '{step.Name}' ({step.ExpectedDuration:F2}s expected) | queue:{totalCount}");
        }

        // Backward compatibility method for code that still expects strings and floats (e.g. UI/timers)
        public void Enqueue(string id, string name, float durationSeconds)
        {
            Enqueue(new WaitTimerStep(name, Mathf.Max(0f, durationSeconds)));
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
            if (_currentStep != null)
            {
                if (_currentStep.IsFinished())
                {
                    FinishCurrentEntry();
                }
            }

            // Start next entry if none is playing
            if (_currentStep == null && _queue.Count > 0)
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
            _currentStep = _queue.Dequeue();
            _currentStep.Start();

            Debug.Log($"[AnimQueue] Playing '{_currentStep.Name}'");
        }

        private void FinishCurrentEntry()
        {
            var finished = _currentStep;
            _currentStep = null;

            OnAnimationFinished?.Invoke(finished, finished.ExpectedDuration);
            Debug.Log($"[AnimQueue] Finished '{finished.Name}'");
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
            _currentStep = null;

            SetInputLocked(false);
            ResetLockTracking();
        }

        private AnimationQueueState BuildState()
        {
            return new AnimationQueueState(
                _queue.Count + (_currentStep != null ? 1 : 0),
                _isInputLocked,
                _expectedTotalLength,
                _lockElapsedSeconds
            );
        }
    }
}
