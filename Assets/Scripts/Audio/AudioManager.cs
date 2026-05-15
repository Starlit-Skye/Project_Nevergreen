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

        public void TransitionToBGM(AudioClip clip, float duration = 1.5f)
        {
            if (clip == null || _bgmSourceMain.clip == clip) return;

            StartCoroutine(CrossfadeRoutine(clip, duration));
        }
        
        public void StopMusic(float fadeDuration = 1.0f)
        {
             StartCoroutine(FadeOutSource(_bgmSourceMain, fadeDuration));
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
        {
            // Swap sources
            AudioSource oldSource = _bgmSourceMain;
            _bgmSourceMain = _bgmSourceFade;
            _bgmSourceFade = oldSource;

            _bgmSourceMain.clip = newClip;
            _bgmSourceMain.volume = 0f;
            _bgmSourceMain.loop = true; // Ensure BGM loops
            _bgmSourceMain.Play();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                _bgmSourceMain.volume = Mathf.Lerp(0f, 1f, t);
                _bgmSourceFade.volume = Mathf.Lerp(1f, 0f, t);
                
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
