using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Combat;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// Controller for the Reroll Perfections Room UI.
    /// Allows the player to select a Marionette in the party and pay a configurable percentage of
    /// its effective Max HP to randomize all of its current Perfections into new ones.
    /// </summary>
    public class RerollPerfectionsRoomController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button[] marionetteButtons;
        [SerializeField] private Button rerollButton;
        [SerializeField] private Button leaveButton;

        [Header("Panels")]
        [SerializeField] private GameObject perfectionsPanel;
        [SerializeField] private Transform perfectionsContainer;
        [SerializeField] private GameObject perfectionUIItemPrefab;
        [SerializeField] private GameObject hpTextPanel;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Styling")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;

        [Header("Configuration")]
        [Tooltip("Fraction of the Marionette's max HP dealt when rerolling, as a value from 0 to 1 (for example 0.25 = 25% of max HP).")]
        [Range(0f, 1f)]
        [SerializeField] private float rerollDamagePercent = 0.25f;

        private List<PartyMemberInfo> _party;
        private int _selectedIndex = -1;
        private readonly List<GameObject> _spawnedPerfectionItems = new List<GameObject>();
        private System.Random _rng = new System.Random();

        public int SelectedIndex => _selectedIndex;

        public float RerollDamagePercent
        {
            get => rerollDamagePercent;
            set => rerollDamagePercent = Mathf.Clamp01(value);
        }

        void Start()
        {
            Initialize(RunSessionManager.CurrentParty);
        }
        /// <summary>
        /// Initializes the room UI with the active party (or RunSessionManager.CurrentParty if null).
        /// </summary>
        public void Initialize(List<PartyMemberInfo> party = null)
        {
            _party = party ?? RunSessionManager.CurrentParty;
            _selectedIndex = -1;

            SetupMarionetteButtons();

            if (perfectionsPanel != null)
            {
                perfectionsPanel.SetActive(false);
            }

            if (hpTextPanel != null)
            {
                hpTextPanel.SetActive(false);
            }

            ClearSpawnedPerfectionItems();
            ResetRerollButton();
        }

        private void OnEnable()
        {
            if (rerollButton != null)
            {
                rerollButton.onClick.AddListener(OnRerollClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void OnDisable()
        {
            if (rerollButton != null)
            {
                rerollButton.onClick.RemoveListener(OnRerollClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(OnLeaveClicked);
            }
        }

        private void SetupMarionetteButtons()
        {
            if (marionetteButtons == null) return;

            for (int i = 0; i < marionetteButtons.Length; i++)
            {
                Button btn = marionetteButtons[i];
                if (btn == null) continue;

                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();

                bool hasMember = _party != null && i < _party.Count && _party[i] != null && _party[i].character != null;
                TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();

                if (hasMember)
                {
                    if (label != null)
                    {
                        label.text = _party[i].character.displayName;
                    }

                    btn.interactable = true;
                    int capturedIndex = i;
                    btn.onClick.AddListener(() => OnMarionetteClicked(capturedIndex));
                }
                else
                {
                    if (label != null)
                    {
                        label.text = "Empty Slot";
                    }

                    btn.interactable = false;
                }

                SetButtonColor(btn, normalColor);
            }
        }

        /// <summary>
        /// Handles clicking one of the Marionette buttons.
        /// </summary>
        public void OnMarionetteClicked(int index)
        {
            if (_party == null || index < 0 || index >= _party.Count || _party[index] == null || _party[index].character == null)
            {
                return;
            }

            _selectedIndex = index;
            UpdateMarionetteButtonHighlights();

            if (perfectionsPanel != null)
            {
                perfectionsPanel.SetActive(true);
            }

            if (hpTextPanel != null)
            {
                hpTextPanel.SetActive(true);
            }

            PartyMemberInfo member = _party[_selectedIndex];
            RefreshPanels(member);
            RefreshRerollButton(member);
        }

        private void UpdateMarionetteButtonHighlights()
        {
            if (marionetteButtons == null) return;

            for (int i = 0; i < marionetteButtons.Length; i++)
            {
                if (marionetteButtons[i] != null)
                {
                    SetButtonColor(marionetteButtons[i], i == _selectedIndex ? highlightColor : normalColor);
                }
            }
        }

        private void SetButtonColor(Button btn, Color color)
        {
            if (btn == null) return;
            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private void RefreshPanels(PartyMemberInfo member)
        {
            ClearSpawnedPerfectionItems();

            if (member == null) return;

            if (member.perfections != null && perfectionsContainer != null && perfectionUIItemPrefab != null)
            {
                for (int i = 0; i < member.perfections.Count; i++)
                {
                    var trait = member.perfections[i];
                    if (trait == null) continue;

                    GameObject item = Instantiate(perfectionUIItemPrefab, perfectionsContainer);
                    _spawnedPerfectionItems.Add(item);

                    var tooltipTrigger = item.GetComponent<TraitTooltipTrigger>();
                    if (tooltipTrigger == null)
                    {
                        tooltipTrigger = item.AddComponent<TraitTooltipTrigger>();
                    }
                    tooltipTrigger.SetTrait(trait);

                    var label = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                    {
                        label.text = $"- {trait.displayName}";
                    }
                }
            }

            if (hpText != null)
            {
                int effectiveMaxHP = PartyMemberStatCalculator.GetEffectiveMaxHP(member);
                int currentHP = member.currentHP ?? effectiveMaxHP;
                hpText.text = $"HP: {currentHP}/{effectiveMaxHP}";
            }
        }

        private void RefreshRerollButton(PartyMemberInfo member)
        {
            if (rerollButton == null) return;

            if (member == null || member.character == null)
            {
                ResetRerollButton();
                return;
            }

            int effectiveMaxHP = PartyMemberStatCalculator.GetEffectiveMaxHP(member);
            int currentHP = member.currentHP ?? effectiveMaxHP;
            int damage = CalculateRerollDamage(effectiveMaxHP);

            bool canSurvive = (currentHP - damage) >= 1;
            bool hasPerfections = HasAnyPerfection(member);

            rerollButton.interactable = canSurvive && hasPerfections;

            var btnText = rerollButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = $"Randomize all Perfections. -{damage} HP";
            }
        }

        private void ResetRerollButton()
        {
            if (rerollButton == null) return;

            rerollButton.interactable = false;
            var btnText = rerollButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = "Randomize all Perfections.";
            }
        }

        private int CalculateRerollDamage(int effectiveMaxHP)
        {
            return Mathf.RoundToInt(effectiveMaxHP * rerollDamagePercent);
        }

        private static bool HasAnyPerfection(PartyMemberInfo member)
        {
            if (member == null || member.perfections == null) return false;
            for (int i = 0; i < member.perfections.Count; i++)
            {
                if (member.perfections[i] != null) return true;
            }
            return false;
        }

        /// <summary>
        /// Handles clicking the Reroll button: deals configured Max HP percentage damage to the selected
        /// Marionette and replaces all of its current Perfections with new ones.
        /// </summary>
        public void OnRerollClicked()
        {
            if (_party == null || _selectedIndex < 0 || _selectedIndex >= _party.Count) return;

            PartyMemberInfo member = _party[_selectedIndex];
            if (member == null || member.character == null) return;

            int effectiveMaxHP = PartyMemberStatCalculator.GetEffectiveMaxHP(member);
            int currentHP = member.currentHP ?? effectiveMaxHP;
            int damage = CalculateRerollDamage(effectiveMaxHP);

            if ((currentHP - damage) < 1 || !HasAnyPerfection(member))
            {
                return;
            }

            // 1. Deal damage based on current effective Max HP
            int newHP = currentHP - damage;
            member.currentHP = newHP >= effectiveMaxHP ? (int?)null : newHP;

            // 2. Reroll all current Perfections into new ones
            RerollPerfections(member);

            // 3. Recompute effective Max HP after trait changes and clamp currentHP if Max HP dropped
            int postRerollMaxHP = PartyMemberStatCalculator.GetEffectiveMaxHP(member);
            if (member.currentHP.HasValue && member.currentHP.Value >= postRerollMaxHP)
            {
                member.currentHP = null;
            }

            // 4. Refresh UI
            RefreshPanels(member);
            RefreshRerollButton(member);
        }

        /// <summary>
        /// Replaces all Perfections on the target Marionette with an equal number of new Perfections
        /// from TraitDatabase, excluding all previously equipped Perfections and respecting
        /// duplicate and opposite-trait exclusion rules.
        /// </summary>
        public void RerollPerfections(PartyMemberInfo member)
        {
            if (member == null || member.perfections == null) return;

            var excludedOldIds = new HashSet<string>();
            var excludedOldRefs = new HashSet<TraitData>();
            int countToReplace = 0;

            for (int i = 0; i < member.perfections.Count; i++)
            {
                var oldTrait = member.perfections[i];
                if (oldTrait != null)
                {
                    countToReplace++;
                    excludedOldRefs.Add(oldTrait);
                    if (!string.IsNullOrEmpty(oldTrait.traitId))
                    {
                        excludedOldIds.Add(oldTrait.traitId);
                    }
                }
            }

            member.perfections.Clear();

            if (countToReplace == 0) return;

            var traitDb = GameDatabase.Instance != null ? GameDatabase.Instance.TraitDatabase : null;
            if (traitDb == null || traitDb.perfections == null) return;

            for (int i = 0; i < countToReplace; i++)
            {
                if (member.IsTraitFull(TraitType.Perfection)) break;

                var validCandidates = GetValidPerfectionCandidates(member, traitDb.perfections, excludedOldIds, excludedOldRefs);
                if (validCandidates.Count == 0)
                {
                    break;
                }

                var chosen = validCandidates[_rng.Next(validCandidates.Count)];
                member.TryAddTrait(chosen);
            }
        }

        private static List<TraitData> GetValidPerfectionCandidates(
            PartyMemberInfo target,
            List<TraitData> pool,
            HashSet<string> excludedIds,
            HashSet<TraitData> excludedRefs)
        {
            var candidates = new List<TraitData>();
            if (target == null || pool == null) return candidates;

            for (int i = 0; i < pool.Count; i++)
            {
                var trait = pool[i];
                if (trait == null) continue;
                if (excludedRefs.Contains(trait)) continue;
                if (!string.IsNullOrEmpty(trait.traitId) && excludedIds.Contains(trait.traitId)) continue;

                if (target.CanAddTrait(trait))
                {
                    candidates.Add(trait);
                }
            }
            return candidates;
        }

        /// <summary>
        /// Handles clicking the Leave button to complete the room.
        /// </summary>
        public void OnLeaveClicked()
        {
            CompleteRoom();
        }

        /// <summary>
        /// Completes the room flow by deactivating this room and opening the room selection map.
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
                Debug.LogError("[RerollPerfectionsRoomController] CombatUI not found!");
            }
        }

        private void ClearSpawnedPerfectionItems()
        {
            for (int i = _spawnedPerfectionItems.Count - 1; i >= 0; i--)
            {
                var item = _spawnedPerfectionItems[i];
                if (item != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(item);
                    }
                    else
                    {
                        DestroyImmediate(item);
                    }
                }
            }
            _spawnedPerfectionItems.Clear();
        }
    }
}

