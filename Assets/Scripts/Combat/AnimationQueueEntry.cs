using System;
using UnityEngine;
using DG.Tweening;

namespace Nevergreen.Combat
{
    public interface IAnimationStep
    {
        string Name { get; }
        float ExpectedDuration { get; }
        void Start();
        bool IsFinished();
    }

    /// <summary>
    /// A simple time-based delay step.
    /// </summary>
    public class WaitTimerStep : IAnimationStep
    {
        public string Name { get; }
        public float ExpectedDuration { get; }

        private float _elapsed;

        public WaitTimerStep(string name, float durationSeconds)
        {
            Name = name;
            ExpectedDuration = durationSeconds;
        }

        public void Start()
        {
            _elapsed = 0f;
        }

        public bool IsFinished()
        {
            _elapsed += Time.deltaTime;
            return _elapsed >= ExpectedDuration;
        }
    }

    /// <summary>
    /// Plays an Animator state and waits for it to complete.
    /// Includes a maximum timeout safeguard.
    /// </summary>
    public class AnimatorStep : IAnimationStep
    {
        public string Name { get; }
        public float ExpectedDuration { get; }

        private Animator _animator;
        private string _stateName;
        private float _elapsed;
        private bool _hasStartedPlaying;

        public AnimatorStep(string name, Animator animator, string stateName, float expectedDuration = 1f)
        {
            Name = name;
            _animator = animator;
            _stateName = stateName;
            ExpectedDuration = expectedDuration;
        }

        public void Start()
        {
            _elapsed = 0f;
            _hasStartedPlaying = false;
            if (_animator != null)
            {
                _animator.Play(_stateName, 0, 0f);
            }
        }

        public bool IsFinished()
        {
            _elapsed += Time.deltaTime;

            if (_animator == null) return true;

            // Failsafe timeout 
            if (_elapsed >= ExpectedDuration + 2.0f) return true;

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            // Wait until the animator actually enters the target state (transitioning takes a frame or two)
            if (stateInfo.IsName(_stateName))
            {
                _hasStartedPlaying = true;
            }

            // If we have started playing it, and normalized time reaches 1, it's done. 
            if (_hasStartedPlaying && stateInfo.normalizedTime >= 1.0f)
            {
                return true;
            }

            return false;
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
    /// Executes multiple animation steps concurrently.
    /// Useful for playing attacks and hit reactions at the same time.
    /// </summary>
    public class ParallelStep : IAnimationStep
    {
        public string Name { get; }
        
        public float ExpectedDuration 
        { 
            get 
            {
                float max = 0f;
                foreach (var step in _steps)
                {
                    if (step.ExpectedDuration > max)
                        max = step.ExpectedDuration;
                }
                return max;
            } 
        }

        private System.Collections.Generic.List<IAnimationStep> _steps = new System.Collections.Generic.List<IAnimationStep>();

        public ParallelStep(string name)
        {
            Name = name;
        }

        public void AddStep(IAnimationStep step)
        {
            if (step != null)
            {
                _steps.Add(step);
            }
        }

        public void Start()
        {
            foreach (var step in _steps)
            {
                step.Start();
            }
        }

        public bool IsFinished()
        {
            bool allFinished = true;
            foreach (var step in _steps)
            {
                if (!step.IsFinished())
                {
                    allFinished = false;
                }
            }
            return allFinished;
        }
    }

    /// <summary>
    /// Executes a DOTween and waits for it to complete.
    /// </summary>
    public class DOTweenStep : IAnimationStep
    {
        public string Name { get; }
        public float ExpectedDuration { get; }

        private Tween _tween;
        private bool _isStarted;

        public DOTweenStep(string name, Tween tween, float duration)
        {
            Name = name;
            _tween = tween;
            _tween.Pause(); // Ensure it doesn't start until the queue is ready
            ExpectedDuration = duration;
        }

        public void Start()
        {
            _tween.Play();
            _isStarted = true;
        }

        public bool IsFinished()
        {
            return _isStarted && (!_tween.IsActive() || _tween.IsComplete());
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
