using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using Nevergreen.Data;

namespace Nevergreen.Audio
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        var prefab = Resources.Load<GameObject>("AudioManager");
                        if (prefab != null)
                        {
                            var go = Object.Instantiate(prefab);
                            go.name = "AudioManager";
                            _instance = go.GetComponent<AudioManager>();
                        }
                        else
                        {
                            var go = new GameObject("AudioManager");
                            _instance = go.AddComponent<AudioManager>();
                        }
                    }
                }
                return _instance;
            }
        }

        [Header("Config")]
        public AudioConfig config;

        [Header("Sources")]
        [SerializeField] private AudioSource _bgmSourceMain;
        [SerializeField] private AudioSource _bgmSourceFade;
        [SerializeField] private AudioSource _sfxSource;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureAudioSources();
            ApplySavedVolumes();
        }

        public void EnsureAudioSources()
        {
            if (config == null)
            {
                config = Resources.Load<AudioConfig>("GlobalAudioConfig");
            }

            if (_bgmSourceMain == null)
            {
                _bgmSourceMain = gameObject.AddComponent<AudioSource>();
                _bgmSourceMain.playOnAwake = false;
            }

            if (_bgmSourceFade == null)
            {
                _bgmSourceFade = gameObject.AddComponent<AudioSource>();
                _bgmSourceFade.playOnAwake = false;
            }

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }

            if (config != null && config.mainMixer != null)
            {
                var bgmGroups = config.mainMixer.FindMatchingGroups("BGM");
                if (bgmGroups != null && bgmGroups.Length > 0)
                {
                    if (_bgmSourceMain.outputAudioMixerGroup == null)
                        _bgmSourceMain.outputAudioMixerGroup = bgmGroups[0];
                    if (_bgmSourceFade.outputAudioMixerGroup == null)
                        _bgmSourceFade.outputAudioMixerGroup = bgmGroups[0];
                }

                var sfxGroups = config.mainMixer.FindMatchingGroups("SFX");
                if (sfxGroups != null && sfxGroups.Length > 0)
                {
                    if (_sfxSource.outputAudioMixerGroup == null)
                        _sfxSource.outputAudioMixerGroup = sfxGroups[0];
                }
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            EnsureAudioSources();
            _sfxSource.PlayOneShot(clip);
        }

        private Coroutine _musicRoutine;
        private Coroutine _crossfadeRoutine;

        public void TransitionToBGM(AudioClip clip, float duration = 1.5f)
        {
            if (clip == null)
            {
                StopMusic(duration);
                return;
            }

            EnsureAudioSources();

            // If it's already playing the same clip, don't restart unless we are force-looping
            if (_bgmSourceMain != null && _bgmSourceMain.clip == clip && _bgmSourceMain.isPlaying) return;

            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            
            SwapSourcesAndPlay(clip);
            _musicRoutine = StartCoroutine(MusicLoopRoutine(clip, duration));
        }

        public void StopMusic(float fadeDuration = 1.0f)
        {
            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            
            _musicRoutine = null;
            _crossfadeRoutine = null;
            
            StartCoroutine(FadeOutSource(_bgmSourceMain, fadeDuration));
            StartCoroutine(FadeOutSource(_bgmSourceFade, fadeDuration));
        }

        private void SwapSourcesAndPlay(AudioClip newClip)
        {
            // Swap sources
            AudioSource oldSource = _bgmSourceMain;
            _bgmSourceMain = _bgmSourceFade;
            _bgmSourceFade = oldSource;

            _bgmSourceMain.clip = newClip;
            _bgmSourceMain.volume = 0f;
            _bgmSourceMain.loop = false; // We handle looping manually with fades
            _bgmSourceMain.Play();
        }

        private IEnumerator MusicLoopRoutine(AudioClip clip, float fadeDuration)
        {
            // Start volume crossfade for the initial transition (don't yield)
            _crossfadeRoutine = StartCoroutine(LerpVolumeRoutine(fadeDuration));

            while (true)
            {
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

                // Swap sources and start crossfade for the next loop (don't yield)
                SwapSourcesAndPlay(clip);
                _crossfadeRoutine = StartCoroutine(LerpVolumeRoutine(fadeDuration));
            }
        }

        private IEnumerator LerpVolumeRoutine(float duration)
        {
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
