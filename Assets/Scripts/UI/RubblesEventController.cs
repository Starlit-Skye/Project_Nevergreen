using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// Controller for the Rubbles event room UI.
    /// Gives the player a choice between going around (increasing exhaustion)
    /// or clearing the rubble (dealing non-lethal percentage max-HP damage to all party members),
    /// then proceeds through the room completion flow.
    /// </summary>
    public class RubblesEventController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button goAroundButton;
        [SerializeField] private Button clearRubbleButton;

        [Header("Configuration")]
        [Min(0)]
        [Tooltip("Number of exhaustion rounds added when choosing Go Around.")]
        [SerializeField] private int exhaustionIncrease = 2;

        [Range(0f, 1f)]
        [Tooltip("Fraction of Max HP dealt to all party members when choosing Clear Rubble (e.g. 0.10 = 10%, 0.25 = 25%, 0.50 = 50%, 1.00 = 100%).")]
        [SerializeField] private float damagePercent = 0.25f;

        private List<PartyMemberInfo> _party;

        /// <summary>
        /// Number of exhaustion rounds added when choosing Go Around (clamped to >= 0).
        /// </summary>
        public int ExhaustionIncrease
        {
            get => exhaustionIncrease;
            set => exhaustionIncrease = Mathf.Max(0, value);
        }

        /// <summary>
        /// Fraction of Max HP dealt when choosing Clear Rubble, clamped to [0, 1]
        /// (e.g. 0.10 = 10%, 0.25 = 25%, 0.50 = 50%, 1.00 = 100%).
        /// </summary>
        public float DamagePercent
        {
            get => damagePercent;
            set => damagePercent = Mathf.Clamp01(value);
        }

        private void Start()
        {
            Initialize(RunSessionManager.CurrentParty);
        }

        /// <summary>
        /// Initializes the event controller with the active party and enables interaction on both choice buttons.
        /// </summary>
        public void Initialize(List<PartyMemberInfo> party = null)
        {
            _party = party ?? RunSessionManager.CurrentParty;
            SetButtonsInteractable(true);
        }

        private void OnEnable()
        {
            if (goAroundButton != null)
            {
                goAroundButton.onClick.AddListener(OnGoAroundClicked);
            }

            if (clearRubbleButton != null)
            {
                clearRubbleButton.onClick.AddListener(OnClearRubbleClicked);
            }
        }

        private void OnDisable()
        {
            if (goAroundButton != null)
            {
                goAroundButton.onClick.RemoveListener(OnGoAroundClicked);
            }

            if (clearRubbleButton != null)
            {
                clearRubbleButton.onClick.RemoveListener(OnClearRubbleClicked);
            }
        }

        /// <summary>
        /// Computes the resulting HP after applying percentage max-HP damage,
        /// leaving any surviving member at a minimum of 1 HP and leaving members
        /// already at or below 1 HP unchanged.
        /// </summary>
        public static int ComputeDamagedHP(int currentHP, int maxHP, float damagePercent)
        {
            if (currentHP <= 1) return currentHP;
            int damage = Mathf.RoundToInt(Mathf.Max(0, maxHP) * Mathf.Clamp01(damagePercent));
            return Mathf.Max(1, currentHP - damage);
        }

        /// <summary>
        /// Handles clicking Go Around: increases run exhaustion by the configured amount
        /// (clamped to ExhaustionLimit; no-op if exhaustion is disabled) and completes the room.
        /// </summary>
        public void OnGoAroundClicked()
        {
            SetButtonsInteractable(false);

            int increase = Mathf.Max(0, exhaustionIncrease);
            if (RunSessionManager.ExhaustionLimit > 0 && increase > 0)
            {
                RunSessionManager.ExhaustionRound = Mathf.Min(
                    RunSessionManager.ExhaustionRound + increase,
                    RunSessionManager.ExhaustionLimit);
            }

            CompleteRoom();
        }

        /// <summary>
        /// Handles clicking Clear Rubble: deals the configured percentage of max HP
        /// to every party member (never dropping below 1 HP) and completes the room.
        /// </summary>
        public void OnClearRubbleClicked()
        {
            SetButtonsInteractable(false);

            var party = _party ?? RunSessionManager.CurrentParty;
            if (party != null)
            {
                for (int i = 0; i < party.Count; i++)
                {
                    var member = party[i];
                    if (member == null || member.character == null) continue;

                    var stats = member.character.GetStatsForLevel(member.currentLevel);
                    if (stats == null) continue;

                    int maxHP = stats.maxHP;
                    int currentHP = member.currentHP ?? maxHP;
                    int newHP = ComputeDamagedHP(currentHP, maxHP, damagePercent);

                    member.currentHP = newHP >= maxHP ? (int?)null : newHP;
                }
            }

            CompleteRoom();
        }

        /// <summary>
        /// Completes the room flow by deactivating this UI and opening room selection via CombatUI.
        /// </summary>
        public void CompleteRoom()
        {
            gameObject.SetActive(false);

            var combatUI = FindFirstObjectByType<CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                Debug.LogError("[RubblesEventController] CombatUI not found!");
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (goAroundButton != null)
            {
                goAroundButton.interactable = interactable;
            }

            if (clearRubbleButton != null)
            {
                clearRubbleButton.interactable = interactable;
            }
        }
    }
}

