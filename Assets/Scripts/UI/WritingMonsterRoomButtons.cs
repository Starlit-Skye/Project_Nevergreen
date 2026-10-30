using UnityEngine;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// UI Controller for the Writing Monster event room buttons.
    /// Provides actions for helping the monster, attacking the monster, and closing the room.
    /// </summary>
    public class WritingMonsterRoomButtons : MonoBehaviour
    {
        [SerializeField, Min(0)]
        [Tooltip("Amount of Exhaustion rounds reduced when helping the monster.")]
        private int exhaustionReduction = 1;

        [SerializeField, Min(0)]
        [Tooltip("Amount of Parts granted to the player team when attacking the monster.")]
        private int partsReward = 1;

        private bool _hasChosen;

        public int ExhaustionReduction
        {
            get => exhaustionReduction;
            set => exhaustionReduction = Mathf.Max(0, value);
        }

        public int PartsReward
        {
            get => partsReward;
            set => partsReward = Mathf.Max(0, value);
        }

        public bool HasChosen => _hasChosen;

        /// <summary>
        /// Reduces exhaustion by the configured amount (clamped at 0).
        /// Can only be selected once per visit.
        /// </summary>
        public void HelpMonster()
        {
            if (_hasChosen) return;
            _hasChosen = true;

            RunSessionManager.ExhaustionRound = Mathf.Max(0, RunSessionManager.ExhaustionRound - exhaustionReduction);
        }

        /// <summary>
        /// Grants the configured amount of Parts to the player team.
        /// Can only be selected once per visit.
        /// </summary>
        public void AttackMonster()
        {
            if (_hasChosen) return;
            _hasChosen = true;

            RunSessionManager.GrantParts(partsReward);
        }

        /// <summary>
        /// Completes the room flow: hides this UI and shows the room selection screen.
        /// Always available regardless of whether a choice was made.
        /// </summary>
        public void Close()
        {
            CompleteRoom();
        }

        private void CompleteRoom()
        {
            gameObject.SetActive(false);

            CombatUI combatUI = Object.FindFirstObjectByType<CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                Debug.LogError("[WritingMonsterRoomButtons] CombatUI not found!");
            }
        }
    }
}
