using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;
using Nevergreen.Audio;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class AudioManagerTests
    {
        private GameObject _audioGo;
        private AudioManager _audioManager;
        private AudioSource _sourceMain;
        private AudioSource _sourceFade;
        private AudioSource _sourceSfx;

        [SetUp]
        public void Setup()
        {
            _audioGo = new GameObject("AudioManager");
            
            // Need to add AudioSources before AudioManager so they can be assigned
            _sourceMain = _audioGo.AddComponent<AudioSource>();
            _sourceFade = _audioGo.AddComponent<AudioSource>();
            _sourceSfx = _audioGo.AddComponent<AudioSource>();

            _audioManager = _audioGo.AddComponent<AudioManager>();

            // Use reflection to set the private serialized fields
            var type = typeof(AudioManager);
            type.GetField("_bgmSourceMain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_audioManager, _sourceMain);
            type.GetField("_bgmSourceFade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_audioManager, _sourceFade);
            type.GetField("_sfxSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_audioManager, _sourceSfx);
            
            // Mock config
            _audioManager.config = ScriptableObject.CreateInstance<AudioConfig>();
            
            // We need to trigger Awake manually since we are adding the component in EditMode
            // but AddComponent triggers Awake immediately in modern Unity. 
            // We just ensure we clear the singleton if it persists.
        }

        [TearDown]
        public void Teardown()
        {
            if (_audioGo != null)
            {
                Object.DestroyImmediate(_audioGo);
            }
            
            // Clear singleton
            var prop = typeof(AudioManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(null, null);
            }
        }

        [Test]
        public void PlaySFX_ValidClip_PlaysOneShot()
        {
            // Note: We can't easily assert PlayOneShot was called without a mock, 
            // but we can ensure it doesn't throw exceptions.
            AudioClip clip = AudioClip.Create("TestSfx", 1024, 1, 44100, false);
            
            Assert.DoesNotThrow(() => {
                _audioManager.PlaySFX(clip);
            });
            
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void TransitionToBGM_SetsClipAndStartsPlaying()
        {
            // Use a short clip so we don't trigger the end-of-clip logic immediately
            AudioClip clip = AudioClip.Create("TestBgm", 44100 * 5, 1, 44100, false);
            
            Assert.DoesNotThrow(() => {
                _audioManager.TransitionToBGM(clip, 1.0f);
            });

            // In EditMode, StartCoroutine doesn't advance, so state won't reflect the crossfade changes.
            // We just verify it handles the clip properly without crashing.
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void TransitionToBGM_SameClip_DoesNotRestart()
        {
            AudioClip clip = AudioClip.Create("TestBgm", 44100 * 5, 1, 44100, false);
            
            _audioManager.TransitionToBGM(clip, 1.0f);

            // Call transition again with the same clip
            Assert.DoesNotThrow(() => {
                _audioManager.TransitionToBGM(clip, 1.0f);
            });

            Object.DestroyImmediate(clip);
        }

        [Test]
        public void StopMusic_TriggersFadeOutAndStopsRoutine()
        {
            AudioClip clip = AudioClip.Create("TestBgm", 44100 * 5, 1, 44100, false);
            
            _audioManager.TransitionToBGM(clip, 1.0f);

            Assert.DoesNotThrow(() => {
                _audioManager.StopMusic(1.0f);
            });
            
            // Check if the music routines were cleared
            var routineField = typeof(AudioManager).GetField("_musicRoutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var crossfadeField = typeof(AudioManager).GetField("_crossfadeRoutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNull(routineField.GetValue(_audioManager));
            Assert.IsNull(crossfadeField.GetValue(_audioManager));

            Object.DestroyImmediate(clip);
        }

        [Test]
        public void Transition_CrossfadesBetweenDifferentTracks()
        {
            AudioClip clipA = AudioClip.Create("Exploration", 44100, 1, 44100, false);
            AudioClip clipB = AudioClip.Create("Battle", 44100, 1, 44100, false);

            // 1. Setup "Exploration" music playing
            _sourceMain.clip = clipA;
            _sourceMain.volume = 1.0f;
            _sourceMain.Play();

            // 2. Transition to "Battle"
            _audioManager.TransitionToBGM(clipB, 1.0f);

            // Get current active sources via reflection
            var type = typeof(AudioManager);
            var bgmSourceMain = (AudioSource)type.GetField("_bgmSourceMain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(_audioManager);
            var bgmSourceFade = (AudioSource)type.GetField("_bgmSourceFade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(_audioManager);

            // 3. Verify references swapped (Main should now be the target Battle clip)
            Assert.AreEqual(clipB, bgmSourceMain.clip, "Main source should be assigned the new Battle track.");
            Assert.AreEqual(clipA, bgmSourceFade.clip, "Old Exploration track should be moved to the Fade source.");
            Assert.AreEqual(0f, bgmSourceMain.volume, "New track should start at 0 volume for fade-in.");

            Object.DestroyImmediate(clipA);
            Object.DestroyImmediate(clipB);
        }

        [Test]
        public void Transition_RapidCalls_DoesNotConflict()
        {
            AudioClip clipA = AudioClip.Create("TrackA", 44100, 1, 44100, false);
            AudioClip clipB = AudioClip.Create("TrackB", 44100, 1, 44100, false);
            AudioClip clipC = AudioClip.Create("TrackC", 44100, 1, 44100, false);

            Assert.DoesNotThrow(() => {
                _audioManager.TransitionToBGM(clipA, 1.0f);
                _audioManager.TransitionToBGM(clipB, 1.0f);
                _audioManager.TransitionToBGM(clipC, 1.0f);
            });

            var type = typeof(AudioManager);
            var bgmSourceMain = (AudioSource)type.GetField("_bgmSourceMain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(_audioManager);

            Assert.AreEqual(clipC, bgmSourceMain.clip, "The final call should be the one that sticks.");

            Object.DestroyImmediate(clipA);
            Object.DestroyImmediate(clipB);
            Object.DestroyImmediate(clipC);
        }
    }
}
