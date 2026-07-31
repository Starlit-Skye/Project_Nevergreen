using UnityEngine;
using UnityEngine.Audio;

namespace Nevergreen.Data
{
    [CreateAssetMenu(fileName = "NewAudioConfig", menuName = "Nevergreen/Data/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Mixer")]
        public AudioMixer mainMixer;

        [Header("Volume Settings (0.0 to 1.0)")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        [Header("Default Music")]
        public AudioClip defaultMainMenuMusic;
        public AudioClip defaultExplorationMusic;
        public AudioClip defaultBattleMusic;
        public AudioClip victoryJingle;
    }
}
