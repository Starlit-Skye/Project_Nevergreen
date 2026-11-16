using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// Controller for the Mural event room UI.
    /// Gives the player a choice between destroying the mural (dealing non-lethal percentage
    /// max-HP damage to all party members in exchange for Scraps) or gazing at it (increasing
    /// exhaustion and granting each party member a random Perfection trait), then reveals the
    /// Leave button to complete the room flow.
    /// </summary>
    public class MuralRoomController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button destroyButton;
        [SerializeField] private Button gazeButton;
        [SerializeField] private Button leaveButton;

        [Header("Destroy Configuration")]
        [Range(0f, 1f)]
        [Tooltip("Fraction of Max HP dealt to all party members when choosing Destroy (e.g. 0.25 = 25%).")]
        [SerializeField] private float damagePercent = 0.25f;

        [Min(0)]
        [Tooltip("Amount of Scraps awarded to the player when choosing Destroy.")]
        [SerializeField] private int scrapReward = 50;

        [Header("Gaze Configuration")]
        [Min(0)]
        [Tooltip("Number of exhaustion rounds added when choosing Gaze.")]
        [SerializeField] private int exhaustionIncrease = 2;

        private List<PartyMemberInfo> _party;
        private readonly System.Random _rng = new System.Random();

        /// <summary>
        /// Fraction of Max HP dealt when choosing Destroy, clamped to [0, 1] (e.g. 0.25 = 25%).
        /// </summary>
        public float DamagePercent
        {
            get => damagePercent;
            set => damagePercent = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Amount of Scraps awarded when choosing Destroy (clamped to >= 0).
        /// </summary>
        public int ScrapReward
        {
            get => scrapReward;
            set => scrapReward = Mathf.Max(0, value);
        }

        /// <summary>
        /// Number of exhaustion rounds added when choosing Gaze (clamped to >= 0).
        /// </summary>
        public int ExhaustionIncrease
        {
            get => exhaustionIncrease;
            set => exhaustionIncrease = Mathf.Max(0, value);
        }

        private void Start()
        {
            Initialize(RunSessionManager.CurrentParty);
        }

        /// <summary>
        /// Initializes the controller with the active party and enables interaction on the choice buttons.
        /// Leave button state is left untouched (configured as disabled by default in the UI prefab).
        /// </summary>
        public void Initialize(List<PartyMemberInfo> party = null)
        {
            _party = party ?? RunSessionManager.CurrentParty;
            SetActionButtonsInteractable(true);
        }

        private void OnEnable()
        {
            if (destroyButton != null)
            {
                destroyButton.onClick.AddListener(OnDestroyClicked);
            }

            if (gazeButton != null)
            {
                gazeButton.onClick.AddListener(OnGazeClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void OnDisable()
        {
            if (destroyButton != null)
            {
                destroyButton.onClick.RemoveListener(OnDestroyClicked);
            }

            if (gazeButton != null)
            {
                gazeButton.onClick.RemoveListener(OnGazeClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(OnLeaveClicked);
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
        /// Grants a single random Perfection from the provided pool to the target party member,
        /// respecting the max Perfection cap, duplicate ID prevention, and opposite trait exclusion.
        /// Returns true if a Perfection was granted.
        /// </summary>
        public static bool TryGrantRandomPerfection(PartyMemberInfo member, IReadOnlyList<TraitData> perfectionPool, System.Random rng)
        {
            if (member == null || member.character == null || perfectionPool == null || rng == null)
            {
                return false;
            }

            if (member.IsTraitFull(TraitType.Perfection))
            {
                return false;
            }

            var validCandidates = new List<TraitData>();
            for (int i = 0; i < perfectionPool.Count; i++)
            {
                var candidate = perfectionPool[i];
                if (candidate == null || candidate.traitType != TraitType.Perfection) continue;

                if (member.CanAddTrait(candidate))
                {
                    validCandidates.Add(candidate);
                }
            }

            if (validCandidates.Count == 0)
            {
                return false;
            }

            var chosen = validCandidates[rng.Next(validCandidates.Count)];
            return member.TryAddTrait(chosen);
        }

        /// <summary>
        /// Handles clicking Destroy: deals percentage max-HP damage to all party members
        /// (never dropping below 1 HP), grants Scraps, hides Destroy and Gaze buttons,
        /// and enables the Leave button.
        /// </summary>
        public void OnDestroyClicked()
        {
            SetActionButtonsInteractable(false);

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

            int scraps = Mathf.Max(0, scrapReward);
            if (scraps > 0)
            {
                RunSessionManager.GrantScraps(scraps);
            }

            TransitionToLeaveState();
        }

        /// <summary>
        /// Handles clicking Gaze: increases run exhaustion by the configured amount
        /// (clamped to ExhaustionLimit; no-op if exhaustion is disabled), grants each
        /// party member a random valid Perfection, hides Destroy and Gaze buttons,
        /// and enables the Leave button.
        /// </summary>
        public void OnGazeClicked()
        {
            SetActionButtonsInteractable(false);

            int increase = Mathf.Max(0, exhaustionIncrease);
            if (RunSessionManager.ExhaustionLimit > 0 && increase > 0)
            {
                RunSessionManager.ExhaustionRound = Mathf.Min(
                    RunSessionManager.ExhaustionRound + increase,
                    RunSessionManager.ExhaustionLimit);
            }

            var party = _party ?? RunSessionManager.CurrentParty;
            var traitDb = GameDatabase.Instance != null ? GameDatabase.Instance.TraitDatabase : null;
            var perfectionPool = traitDb != null ? traitDb.perfections : null;

            if (party != null && perfectionPool != null && perfectionPool.Count > 0)
            {
                for (int i = 0; i < party.Count; i++)
                {
                    TryGrantRandomPerfection(party[i], perfectionPool, _rng);
                }
            }

            TransitionToLeaveState();
        }

        /// <summary>
        /// Handles clicking Leave: proceeds through the room completion flow.
        /// </summary>
        public void OnLeaveClicked()
        {
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
                Debug.LogError("[MuralRoomController] CombatUI not found!");
            }
        }

        private void TransitionToLeaveState()
        {
            if (destroyButton != null)
            {
                destroyButton.gameObject.SetActive(false);
            }

            if (gazeButton != null)
            {
                gazeButton.gameObject.SetActive(false);
            }

            if (leaveButton != null)
            {
                leaveButton.gameObject.SetActive(true);
            }
        }

        private void SetActionButtonsInteractable(bool interactable)
        {
            if (destroyButton != null)
            {
                destroyButton.interactable = interactable;
            }

            if (gazeButton != null)
            {
                gazeButton.interactable = interactable;
            }
        }
    }
}

