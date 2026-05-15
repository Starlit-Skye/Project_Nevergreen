using UnityEngine;
using Nevergreen.Audio;

namespace Nevergreen.Combat
{
    /// <summary>
    /// An animation step that plays a sound effect via the AudioManager.
    /// It completes instantly so it does not block the animation queue.
    /// </summary>
    public class PlaySoundStep : IAnimationStep
    {
        public string Name => "PlaySFX_" + (_clip != null ? _clip.name : "null");
        public float ExpectedDuration => 0f; // Non-blocking

        private AudioClip _clip;

        public PlaySoundStep(AudioClip clip)
        {
            _clip = clip;
        }

        public void Start()
        {
            if (_clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(_clip);
            }
        }

        public bool IsFinished()
        {
            return true;
        }
    }
}
