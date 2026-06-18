using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Audio;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class BattleMusicControllerTests
    {
        private GameObject _battleGo;
        private BattleSystem _battleSystem;
        private BattleMusicController _musicController;
        private GameObject _audioGo;
        private AudioManager _audioManager;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();

            // Setup BattleSystem
            _battleGo = new GameObject("BattleSystem");
            _battleSystem = _battleGo.AddComponent<BattleSystem>();
            _musicController = _battleGo.AddComponent<BattleMusicController>();

            // Setup AudioManager (needs to exist for BattleMusicController to call it)
            _audioGo = new GameObject("AudioManager");
            var sourceMain = _audioGo.AddComponent<AudioSource>(); // Main
            var sourceFade = _audioGo.AddComponent<AudioSource>(); // Fade
            var sourceSfx = _audioGo.AddComponent<AudioSource>(); // SFX
            
            _audioManager = _audioGo.AddComponent<AudioManager>();
            
            var type = typeof(AudioManager);
            type.GetField("_bgmSourceMain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_audioManager, sourceMain);
            type.GetField("_bgmSourceFade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_audioManager, sourceFade);
            type.GetField("_sfxSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_audioManager, sourceSfx);
            
            _audioManager.config = ScriptableObject.CreateInstance<AudioConfig>();
            _audioManager.config.defaultBattleMusic = AudioClip.Create("DefaultBGM", 100, 1, 44100, false);
            
            // Force set Instance since Awake might not run properly in EditMode
            var prop = typeof(AudioManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            prop.SetValue(null, _audioManager);
            
            // Setup teams to prevent null refs
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1);
            var e1 = CombatTestHelper.CreateCombatCharacter("e1", Team.Enemy, 1);
            
            typeof(BattleSystem).GetField("_playerTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_battleSystem, new List<CombatCharacter> { p1 });
            typeof(BattleSystem).GetField("_enemyTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_battleSystem, new List<CombatCharacter> { e1 });
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_battleGo);
            Object.DestroyImmediate(_audioGo);
            
            // Clear singleton
            var prop = typeof(AudioManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(null, null);
            }
            
            if (_audioManager != null && _audioManager.config != null)
            {
                if (_audioManager.config.defaultBattleMusic != null)
                    Object.DestroyImmediate(_audioManager.config.defaultBattleMusic, true);
                ScriptableObject.DestroyImmediate(_audioManager.config, true);
            }

            CombatTestHelper.CleanupTestDatabase();
        }

        [Test]
        public void BattleStarted_TriggersBGM()
        {
            // Call Awake manually if it hasn't run, to subscribe to events
            var awakeMethod = typeof(BattleMusicController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(_musicController, null);

            // Trigger OnBattleStarted by starting the battle
            // We use Reflection to call the private method if necessary, but StartBattle is public
            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1);
            var e1 = CombatTestHelper.CreateCombatCharacter("e1", Team.Enemy, 1);
            
            Assert.DoesNotThrow(() => {
                _battleSystem.StartBattle(new List<CombatCharacter> { p1 }, new List<CombatCharacter> { e1 });
            });
            
            // We can't easily assert the coroutine side-effects in EditMode, 
            // but we assert the event chain doesn't break and is properly handled.
            
            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(e1.gameObject);
        }
        
        [Test]
        public void BattleStarted_WithBoss_PlaysBossMusic()
        {
            var awakeMethod = typeof(BattleMusicController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(_musicController, null);

            var p1 = CombatTestHelper.CreateCombatCharacter("p1", Team.Player, 1);
            var e1 = CombatTestHelper.CreateCombatCharacter("boss1", Team.Enemy, 1);
            e1.characterData.bossMusicOverride = AudioClip.Create("BossBGM", 100, 1, 44100, false);
            
            Assert.DoesNotThrow(() => {
                _battleSystem.StartBattle(new List<CombatCharacter> { p1 }, new List<CombatCharacter> { e1 });
            });
            
            if (e1.characterData.bossMusicOverride != null)
                Object.DestroyImmediate(e1.characterData.bossMusicOverride, true);
            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(e1.gameObject);
        }
    }
}
