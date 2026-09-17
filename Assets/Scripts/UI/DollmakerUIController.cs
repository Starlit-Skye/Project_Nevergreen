using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    public class DollmakerUIController : MonoBehaviour
    {
        [Header("Marionette Selection")]
        [Tooltip("The 4 buttons corresponding to the party slots.")]
        [SerializeField] private Button[] marionetteButtons = new Button[4];
        
        [Header("Trait Displays")]
        [Tooltip("Container for perfection prefabs.")]
        [SerializeField] private Transform perfectionsContainer;
        [Tooltip("Container for imperfection prefabs.")]
        [SerializeField] private Transform imperfectionsContainer;
        [Tooltip("Prefab for a Perfection UI item.")]
        [SerializeField] private GameObject perfectionPrefab;
        [Tooltip("Prefab for an Imperfection UI item.")]
        [SerializeField] private GameObject imperfectionPrefab;
        
        [Header("UI Control")]
        [Tooltip("Skip button to finish interaction and proceed.")]
        [SerializeField] private Button skipButton;
        [Tooltip("Confirm button to execute selection.")]
        [SerializeField] private Button confirmButton;

        [Header("Replacement Panel")]
        [Tooltip("Panel shown when selecting a replacement perfection.")]
        [SerializeField] private GameObject replacePanel;
        [Tooltip("Container for replacement options.")]
        [SerializeField] private Transform replacementContainer;
        [Tooltip("Button to confirm replacement selection.")]
        [SerializeField] private Button confirmReplacementButton;
        [Tooltip("Button to cancel replacement and select another trait.")]
        [SerializeField] private Button selectAnotherButton;

        private int _partCost;
        private int _perfectionReplacementOptions;
        private List<PartyMemberInfo> _party;
        private int _selectedMemberIndex = -1;

        // Main Panel Selection State
        private TraitData _selectedTrait;
        private TextMeshProUGUI _selectedTraitLabel;

        // Replacement Panel Selection State
        private TraitData _selectedReplacement;
        private TextMeshProUGUI _selectedReplacementLabel;

        public void Initialize(int partCost, int perfectionReplacementOptions, List<PartyMemberInfo> party = null)
        {
            _partCost = partCost;
            _perfectionReplacementOptions = perfectionReplacementOptions;
            _party = party ?? RunSessionManager.CurrentParty;

            if (_party == null) _party = new List<PartyMemberInfo>();

            for (int i = 0; i < marionetteButtons.Length; i++)
            {
                if (marionetteButtons[i] == null) continue;

                if (i < _party.Count && _party[i] != null && _party[i].character != null)
                {
                    marionetteButtons[i].interactable = true;
                    
                    var btnText = marionetteButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = _party[i].character.displayName;
                    }

                    int slotIndex = i; // capture loop variable
                    marionetteButtons[i].onClick.RemoveAllListeners();
                    marionetteButtons[i].onClick.AddListener(() => SelectMarionette(slotIndex));
                }
                else
                {
                    marionetteButtons[i].interactable = false;
                    
                    var btnText = marionetteButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = string.Empty;
                    }

                    marionetteButtons[i].onClick.RemoveAllListeners();
                }
            }

            if (_party.Count > 0)
            {
                SelectMarionette(0);
            }

            if (confirmButton != null) confirmButton.interactable = false;
            if (replacePanel != null) replacePanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (confirmReplacementButton != null) confirmReplacementButton.onClick.AddListener(OnConfirmReplacementClicked);
            if (selectAnotherButton != null) selectAnotherButton.onClick.AddListener(OnSelectAnotherClicked);
        }

        private void OnDisable()
        {
            if (skipButton != null) skipButton.onClick.RemoveListener(OnSkipClicked);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (confirmReplacementButton != null) confirmReplacementButton.onClick.RemoveListener(OnConfirmReplacementClicked);
            if (selectAnotherButton != null) selectAnotherButton.onClick.RemoveListener(OnSelectAnotherClicked);
        }

        public void SelectMarionette(int index)
        {
            if (_party == null || index < 0 || index >= _party.Count || _party[index] == null) return;

            _selectedMemberIndex = index;
            PartyMemberInfo member = _party[index];

            ClearContainers();
            ClearSelection();

            if (member.perfections != null && perfectionsContainer != null && perfectionPrefab != null)
            {
                foreach (var trait in member.perfections)
                {
                    if (trait == null) continue;
                    GameObject item = Instantiate(perfectionPrefab, perfectionsContainer);
                    SetupTraitItem(item, trait);
                }
            }

            if (member.imperfections != null && imperfectionsContainer != null && imperfectionPrefab != null)
            {
                foreach (var trait in member.imperfections)
                {
                    if (trait == null) continue;
                    GameObject item = Instantiate(imperfectionPrefab, imperfectionsContainer);
                    SetupTraitItem(item, trait);
                }
            }
        }

        private void SetupTraitItem(GameObject item, TraitData trait)
        {
            var label = item.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = $"- {trait.displayName}";

            var tooltipTrigger = item.GetComponent<TraitTooltipTrigger>();
            if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TraitTooltipTrigger>();
            tooltipTrigger.SetTrait(trait);

            var button = item.GetComponent<Button>();
            if (button == null) button = item.AddComponent<Button>();
            
            button.onClick.AddListener(() => OnTraitItemClicked(trait, label));
        }

        private void OnTraitItemClicked(TraitData trait, TextMeshProUGUI label)
        {
            if (_selectedTrait == trait)
            {
                ClearSelection();
            }
            else
            {
                if (_selectedTraitLabel != null)
                {
                    _selectedTraitLabel.fontStyle = FontStyles.Normal;
                }

                _selectedTrait = trait;
                _selectedTraitLabel = label;
                if (_selectedTraitLabel != null)
                {
                    _selectedTraitLabel.fontStyle = FontStyles.Strikethrough;
                }

                if (confirmButton != null) confirmButton.interactable = true;
            }
        }

        private void ClearSelection()
        {
            if (_selectedTraitLabel != null)
            {
                _selectedTraitLabel.fontStyle = FontStyles.Normal;
            }
            _selectedTrait = null;
            _selectedTraitLabel = null;
            if (confirmButton != null) confirmButton.interactable = false;
        }

        private void ClearContainers()
        {
            if (perfectionsContainer != null)
            {
                foreach (Transform child in perfectionsContainer)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            if (imperfectionsContainer != null)
            {
                foreach (Transform child in imperfectionsContainer)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        private void OnSkipClicked()
        {
            CompleteRoomFlow();
        }

        private void OnConfirmClicked()
        {
            if (_selectedTrait == null || _selectedMemberIndex < 0 || _selectedMemberIndex >= _party.Count) return;

            PartyMemberInfo member = _party[_selectedMemberIndex];

            if (_selectedTrait.traitType == TraitType.Imperfection)
            {
                member.RemoveTrait(_selectedTrait);
                CompleteRoomFlow();
            }
            else if (_selectedTrait.traitType == TraitType.Perfection)
            {
                ShowReplacePanel(member);
            }
        }

        private void ShowReplacePanel(PartyMemberInfo member)
        {
            if (replacePanel != null) replacePanel.SetActive(true);
            
            _selectedReplacement = null;
            _selectedReplacementLabel = null;
            if (confirmReplacementButton != null) confirmReplacementButton.interactable = false;

            if (replacementContainer != null)
            {
                foreach (Transform child in replacementContainer)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            var db = GameDatabase.Instance;
            if (db != null && db.TraitDatabase != null && db.TraitDatabase.perfections != null)
            {
                List<TraitData> validCandidates = new List<TraitData>();
                
                // Exclusions logic:
                // We test if we can add candidate after removing _selectedTrait.
                foreach (var candidate in db.TraitDatabase.perfections)
                {
                    if (candidate == null) continue;
                    
                    // Exclude duplicates in pool
                    if (validCandidates.Contains(candidate)) continue;
                    
                    // Exclude already owned
                    bool alreadyOwned = false;
                    foreach (var t in member.perfections)
                    {
                        if (t != null && t.traitId == candidate.traitId && t != _selectedTrait)
                        {
                            alreadyOwned = true;
                            break;
                        }
                    }
                    if (alreadyOwned) continue;

                    // Exclude opposite trait rules
                    bool hasOpposite = false;
                    
                    // Direct
                    if (candidate.oppositeTrait != null)
                    {
                        foreach (var t in member.perfections)
                        {
                            if (t != null && t != _selectedTrait && (t == candidate.oppositeTrait || t.traitId == candidate.oppositeTrait.traitId))
                                hasOpposite = true;
                        }
                        foreach (var t in member.imperfections)
                        {
                            if (t != null && (t == candidate.oppositeTrait || t.traitId == candidate.oppositeTrait.traitId))
                                hasOpposite = true;
                        }
                    }
                    
                    // Reverse
                    foreach (var t in member.perfections)
                    {
                        if (t != null && t != _selectedTrait && t.oppositeTrait != null && (t.oppositeTrait == candidate || t.oppositeTrait.traitId == candidate.traitId))
                            hasOpposite = true;
                    }
                    foreach (var t in member.imperfections)
                    {
                        if (t != null && t.oppositeTrait != null && (t.oppositeTrait == candidate || t.oppositeTrait.traitId == candidate.traitId))
                            hasOpposite = true;
                    }

                    if (!hasOpposite)
                    {
                        validCandidates.Add(candidate);
                    }
                }

                // Shuffle valid candidates
                for (int i = 0; i < validCandidates.Count; i++)
                {
                    int rand = Random.Range(i, validCandidates.Count);
                    var temp = validCandidates[i];
                    validCandidates[i] = validCandidates[rand];
                    validCandidates[rand] = temp;
                }

                int toGenerate = Mathf.Min(_perfectionReplacementOptions, validCandidates.Count);
                for (int i = 0; i < toGenerate; i++)
                {
                    var candidate = validCandidates[i];
                    GameObject item = Instantiate(perfectionPrefab, replacementContainer);
                    
                    var label = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null) label.text = $"- {candidate.displayName}";

                    var tooltipTrigger = item.GetComponent<TraitTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TraitTooltipTrigger>();
                    tooltipTrigger.SetTrait(candidate);

                    var button = item.GetComponent<Button>();
                    if (button == null) button = item.AddComponent<Button>();
                    
                    button.onClick.AddListener(() => OnReplacementItemClicked(candidate, label));
                }
            }
        }

        private void OnReplacementItemClicked(TraitData trait, TextMeshProUGUI label)
        {
            if (_selectedReplacementLabel != null)
            {
                _selectedReplacementLabel.fontStyle = FontStyles.Normal;
            }

            _selectedReplacement = trait;
            _selectedReplacementLabel = label;
            
            if (_selectedReplacementLabel != null)
            {
                _selectedReplacementLabel.fontStyle = FontStyles.Strikethrough;
            }

            if (confirmReplacementButton != null) confirmReplacementButton.interactable = true;
        }

        private void OnConfirmReplacementClicked()
        {
            if (_selectedReplacement == null || _selectedTrait == null || _selectedMemberIndex < 0 || _selectedMemberIndex >= _party.Count) return;

            PartyMemberInfo member = _party[_selectedMemberIndex];
            
            member.RemoveTrait(_selectedTrait);
            member.TryAddTrait(_selectedReplacement);

            CompleteRoomFlow();
        }

        private void OnSelectAnotherClicked()
        {
            if (_selectedReplacementLabel != null)
            {
                _selectedReplacementLabel.fontStyle = FontStyles.Normal;
            }
            _selectedReplacement = null;
            _selectedReplacementLabel = null;
            
            if (confirmReplacementButton != null) confirmReplacementButton.interactable = false;
            
            if (replacePanel != null) replacePanel.SetActive(false);
        }

        private void CompleteRoomFlow()
        {
            gameObject.SetActive(false);

            var combatUI = UnityEngine.Object.FindFirstObjectByType<Nevergreen.Prototype.CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                if (!RunSessionManager.RoomCompleted)
                {
                    RunSessionManager.CompleteRoom(new System.Collections.Generic.List<RoomData>());
                }
            }
        }
    }
}
