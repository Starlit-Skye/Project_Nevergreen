using NUnit.Framework;
using UnityEngine;
using Nevergreen.Audio;
using Nevergreen.Combat;
using Nevergreen.Data;
using Nevergreen.UI;

namespace Nevergreen.Tests
{
    public class MainMenuMusicTests
    {
        private GameObject _audioGo;
        private AudioManager _audioManager;

        [SetUp]
        public void Setup()
        {
            // Setup AudioManager
            _audioGo = new GameObject("AudioManager");
            var sourceMain = _audioGo.AddComponent<AudioSource>();
            var sourceFade = _audioGo.AddComponent<AudioSource>();
            var sourceSfx = _audioGo.AddComponent<AudioSource>();

            _audioManager = _audioGo.AddComponent<AudioManager>();

            var type = typeof(AudioManager);
            type.GetField("_bgmSourceMain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_audioManager, sourceMain);
            type.GetField("_bgmSourceFade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_audioManager, sourceFade);
            type.GetField("_sfxSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_audioManager, sourceSfx);

            _audioManager.config = ScriptableObject.CreateInstance<AudioConfig>();
            _audioManager.config.defaultMainMenuMusic = AudioClip.Create("MainMenuBGM", 100, 1, 44100, false);
            _audioManager.config.defaultExplorationMusic = AudioClip.Create("ExplorationBGM", 100, 1, 44100, false);

            var instanceField = typeof(AudioManager).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            instanceField?.SetValue(null, _audioManager);
            
            RunSessionManager.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_audioGo);

            var instanceField = typeof(AudioManager).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            instanceField?.SetValue(null, null);

            if (_audioManager != null && _audioManager.config != null)
            {
                if (_audioManager.config.defaultMainMenuMusic != null)
                    Object.DestroyImmediate(_audioManager.config.defaultMainMenuMusic, true);
                if (_audioManager.config.defaultExplorationMusic != null)
                    Object.DestroyImmediate(_audioManager.config.defaultExplorationMusic, true);
                ScriptableObject.DestroyImmediate(_audioManager.config, true);
            }
            
            RunSessionManager.Clear();
        }

        [Test]
        public void MainMenuMusicController_Start_TransitionsToMainMenuBGM()
        {
            var go = new GameObject("MainMenuController");
            var controller = go.AddComponent<MainMenuMusicController>();

            // Trigger Start
            var startMethod = typeof(MainMenuMusicController).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(controller, null);

            // Verify AudioManager started fading to defaultMainMenuMusic
            var sourceMain = (AudioSource)typeof(AudioManager).GetField("_bgmSourceMain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_audioManager);
            Assert.IsNotNull(sourceMain);
            Assert.AreEqual(_audioManager.config.defaultMainMenuMusic, sourceMain.clip);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BattleMusicController_Start_WithRoomCompleted_TransitionsToExplorationBGM()
        {
            RunSessionManager.RoomCompleted = true;

            var go = new GameObject("BattleSystemGo");
            var bs = go.AddComponent<BattleSystem>();
            var controller = go.AddComponent<BattleMusicController>();

            // Trigger Awake
            var awakeMethod = typeof(BattleMusicController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(controller, null);

            // Trigger Start
            var startMethod = typeof(BattleMusicController).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(controller, null);

            // Verify AudioManager started fading to defaultExplorationMusic
            var sourceMain = (AudioSource)typeof(AudioManager).GetField("_bgmSourceMain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_audioManager);
            Assert.IsNotNull(sourceMain);
            Assert.AreEqual(_audioManager.config.defaultExplorationMusic, sourceMain.clip);

            Object.DestroyImmediate(go);
        }
    }
}
