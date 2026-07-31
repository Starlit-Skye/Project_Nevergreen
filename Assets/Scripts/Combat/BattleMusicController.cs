using System.Linq;
using UnityEngine;
using Nevergreen.Audio;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    [RequireComponent(typeof(BattleSystem))]
    public class BattleMusicController : MonoBehaviour
    {
        private BattleSystem _battleSystem;

        private void Awake()
        {
            _battleSystem = GetComponent<BattleSystem>();
            if (_battleSystem != null)
            {
                _battleSystem.OnBattleStarted += HandleBattleStarted;
                _battleSystem.OnBattleEnded += HandleBattleEnded;
            }
        }

        private void Start()
        {
            if (RunSessionManager.RoomCompleted)
            {
                if (AudioManager.Instance != null && AudioManager.Instance.config != null)
                {
                    AudioManager.Instance.TransitionToBGM(AudioManager.Instance.config.defaultExplorationMusic);
                }
            }
        }

        private void OnDestroy()
        {
            if (_battleSystem != null)
            {
                _battleSystem.OnBattleStarted -= HandleBattleStarted;
                _battleSystem.OnBattleEnded -= HandleBattleEnded;
            }
        }

        private void HandleBattleStarted()
        {
            if (AudioManager.Instance == null || AudioManager.Instance.config == null) return;

            AudioClip musicToPlay = AudioManager.Instance.config.defaultBattleMusic;

            // Check if any enemy is a boss (has a bossMusicOverride)
            foreach (var enemy in _battleSystem.EnemyTeam)
            {
                if (enemy.characterData != null && enemy.characterData.bossMusicOverride != null)
                {
                    musicToPlay = enemy.characterData.bossMusicOverride;
                    break; // Use the first boss music found
                }
            }

            if (musicToPlay != null)
            {
                AudioManager.Instance.TransitionToBGM(musicToPlay);
            }
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            if (AudioManager.Instance == null || AudioManager.Instance.config == null) return;

            if (outcome == BattleOutcome.Victory && AudioManager.Instance.config.victoryJingle != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.config.victoryJingle);
            }

            AudioClip explorationMusic = AudioManager.Instance.config.defaultExplorationMusic;
            AudioManager.Instance.TransitionToBGM(explorationMusic);
        }
    }
}
