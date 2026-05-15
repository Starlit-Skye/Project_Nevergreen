using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using Nevergreen.Data;

namespace Nevergreen.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Config")]
        public AudioConfig config;

        [Header("Sources")]
        [SerializeField] private AudioSource _bgmSourceMain;
        [SerializeField] private AudioSource _bgmSourceFade;
        [SerializeField] private AudioSource _sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ApplySavedVolumes();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }

        private Coroutine _musicRoutine;

        public void TransitionToBGM(AudioClip clip, float duration = 1.5f)
        {
            if (clip == null)
            {
                StopMusic(duration);
                return;
            }

            // If it's already playing the same clip, don't restart unless we are force-looping
            if (_bgmSourceMain.clip == clip && _bgmSourceMain.isPlaying) return;

            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            _musicRoutine = StartCoroutine(MusicLoopRoutine(clip, duration));
        }

        public void StopMusic(float fadeDuration = 1.0f)
        {
            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            _musicRoutine = null;
            StartCoroutine(FadeOutSource(_bgmSourceMain, fadeDuration));
            StartCoroutine(FadeOutSource(_bgmSourceFade, fadeDuration));
        }

        private IEnumerator MusicLoopRoutine(AudioClip clip, float fadeDuration)
        {
            while (true)
            {
                // Start crossfade (don't yield)
                StartCoroutine(CrossfadeRoutine(clip, fadeDuration));

                // Wait until the source is playing and we are before the crossfade point
                // (We need to wait for it to actually start playing to get valid time)
                yield return new WaitUntil(() => _bgmSourceMain.isPlaying);

                // Wait until it's time to trigger the next loop's crossfade.
                // Using a while loop with source.time makes this reactive to debug skips!
                float triggerTime = clip.length - fadeDuration;
                while (_bgmSourceMain.isPlaying && _bgmSourceMain.time < triggerTime)
                {
                    yield return null;
                }
            }
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
        {
            // Swap sources
            AudioSource oldSource = _bgmSourceMain;
            _bgmSourceMain = _bgmSourceFade;
            _bgmSourceFade = oldSource;

            _bgmSourceMain.clip = newClip;
            _bgmSourceMain.volume = 0f;
            _bgmSourceMain.loop = false; // We handle looping manually with fades
            _bgmSourceMain.Play();

            float elapsed = 0f;
            float startOldVol = _bgmSourceFade.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                _bgmSourceMain.volume = Mathf.Lerp(0f, 1f, t);
                _bgmSourceFade.volume = Mathf.Lerp(startOldVol, 0f, t);

                yield return null;
            }

            _bgmSourceMain.volume = 1f;
            _bgmSourceFade.volume = 0f;
            _bgmSourceFade.Stop();
        }

        private IEnumerator FadeOutSource(AudioSource source, float duration)
        {
            float startVol = source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            source.Stop();
            source.volume = startVol;
        }

        // --- Settings Management ---

        public void ApplySavedVolumes()
        {
            if (config == null || config.mainMixer == null) return;

            // Load from PlayerPrefs if available, else use config defaults
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", config.masterVolume);
            float bgmVol = PlayerPrefs.GetFloat("BGMVolume", config.bgmVolume);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", config.sfxVolume);

            SetVolume("MasterVolume", masterVol);
            SetVolume("BGMVolume", bgmVol);
            SetVolume("SFXVolume", sfxVol);
        }

        public void SetVolume(string parameter, float linearValue)
        {
            if (config == null || config.mainMixer == null) return;

            float dB = Mathf.Log10(Mathf.Max(0.0001f, linearValue)) * 20f;
            config.mainMixer.SetFloat(parameter, dB);
        }

        public void SaveVolume(string key, float linearValue)
        {
            PlayerPrefs.SetFloat(key, linearValue);
            PlayerPrefs.Save();
        }
    }
}
