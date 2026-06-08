using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// UI Controller for the Marionette Selection screen.
    /// Allows swapping a chosen Marionette into the active party.
    /// </summary>
    public class MarionetteSelectionController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The Marionette database to select choices from.")]
        public MarionetteDatabase marionetteDatabase;
        [Tooltip("Global combat configuration for choice limits.")]
        public CombatConfig combatConfig;
        [Tooltip("The scene to load after confirmation.")]
        public string combatSceneName = "CombatPrototype";

        [Header("UI - Choices")]
        public Button[] choiceButtons;
        public TextMeshProUGUI[] choiceTexts;
        
        [Header("UI - Party Members")]
        public Button[] partyMemberButtons;
        public TextMeshProUGUI[] partyMemberTexts;

        [Header("UI - Info Panel")]
        public TextMeshProUGUI infoNameText;
        public TextMeshProUGUI infoDescriptionText; // To show stats/skills/etc.

        [Header("UI - Controls")]
        public Button confirmButton;

        [Header("Styling")]
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;

        // State
        private List<CharacterData> _currentChoices = new List<CharacterData>();
        private int _selectedChoiceIndex = -1;
        private int _selectedPartyMemberIndex = -1;
        private List<Button> _instantiatedButtons = new List<Button>();
        private List<TextMeshProUGUI> _instantiatedTexts = new List<TextMeshProUGUI>();

        private void Start()
        {
            if (marionetteDatabase == null || combatConfig == null)
            {
                Debug.LogError("[MarionetteSelectionController] Missing Database or Config references!");
                return;
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
                confirmButton.interactable = false;
            }

            InitializeChoices();
            InitializePartyMembers();
        }

        private void InitializeChoices()
        {
            _currentChoices.Clear();

            // Pick N random unique marionettes
            int choiceCount = Mathf.Min(combatConfig.marionetteChoiceCount, marionetteDatabase.marionettes.Count);
            
            // Create a temporary list to pick from
            List<CharacterData> pool = new List<CharacterData>(marionetteDatabase.marionettes);
            for (int i = 0; i < choiceCount; i++)
            {
                int randomIndex = Random.Range(0, pool.Count);
                _currentChoices.Add(pool[randomIndex]);
                pool.RemoveAt(randomIndex);
            }

            // Clean up any previously instantiated choice buttons
            foreach (var btn in _instantiatedButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            _instantiatedButtons.Clear();
            _instantiatedTexts.Clear();

            if (choiceButtons == null || choiceButtons.Length == 0)
            {
                Debug.LogError("[MarionetteSelectionController] No template choice button assigned in choiceButtons array!");
                return;
            }

            Button templateButton = choiceButtons[0];
            Transform container = templateButton.transform.parent;
            templateButton.gameObject.SetActive(false);

            // Setup buttons
            for (int i = 0; i < _currentChoices.Count; i++)
            {
                Button newBtn = Instantiate(templateButton, container);
                newBtn.gameObject.SetActive(true);

                TextMeshProUGUI txt = newBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = _currentChoices[i].displayName;
                }

                int index = i;
                newBtn.onClick.RemoveAllListeners();
                newBtn.onClick.AddListener(() => OnChoiceClicked(index));
                SetButtonColor(newBtn, normalColor);

                _instantiatedButtons.Add(newBtn);
                _instantiatedTexts.Add(txt);
            }

            // Deactivate remaining static template buttons
            for (int i = 1; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void InitializePartyMembers()
        {
            var party = RunSessionManager.CurrentParty;

            for (int i = 0; i < partyMemberButtons.Length; i++)
            {
                partyMemberButtons[i].gameObject.SetActive(true);

                if (party != null && i < party.Count && party[i] != null && party[i].character != null)
                {
                    partyMemberTexts[i].text = party[i].character.displayName;
                }
                else
                {
                    partyMemberTexts[i].text = "Empty Slot";
                }

                int index = i;
                partyMemberButtons[i].onClick.RemoveAllListeners();
                partyMemberButtons[i].onClick.AddListener(() => OnPartyMemberClicked(index));
                SetButtonColor(partyMemberButtons[i], normalColor);
            }
        }

        private void OnChoiceClicked(int index)
        {
            _selectedChoiceIndex = index;
            UpdateHighlights();
            UpdateInfoPanel(_currentChoices[index]);
            EvaluateConfirmButton();
        }

        private void OnPartyMemberClicked(int index)
        {
            _selectedPartyMemberIndex = index;
            UpdateHighlights();
            
            var party = RunSessionManager.CurrentParty;
            if (party != null && index < party.Count && party[index] != null && party[index].character != null)
            {
                UpdateInfoPanel(party[index].character);
            }
            else
            {
                ClearInfoPanel();
            }
            EvaluateConfirmButton();
        }

        private void UpdateHighlights()
        {
            // Update Choice buttons
            for (int i = 0; i < _currentChoices.Count; i++)
            {
                if (i < _instantiatedButtons.Count)
                {
                    SetButtonColor(_instantiatedButtons[i], i == _selectedChoiceIndex ? highlightColor : normalColor);
                }
            }

            // Update Party Member buttons
            for (int i = 0; i < partyMemberButtons.Length; i++)
            {
                if (partyMemberButtons[i].gameObject.activeSelf)
                {
                    SetButtonColor(partyMemberButtons[i], i == _selectedPartyMemberIndex ? highlightColor : normalColor);
                }
            }
        }

        private void SetButtonColor(Button btn, Color color)
        {
            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private void UpdateInfoPanel(CharacterData data)
        {
            if (infoNameText != null)
                infoNameText.text = data.displayName;

            if (infoDescriptionText != null)
            {
                string info = $"Actions per round: {data.actionsPerRound}\n";
                info += $"Size: {data.size}\n";
                info += $"Skills Available: {data.availableSkills.Count}\n";
                infoDescriptionText.text = info;
            }
        }

        private void ClearInfoPanel()
        {
            if (infoNameText != null)
                infoNameText.text = "Empty Slot";

            if (infoDescriptionText != null)
                infoDescriptionText.text = "No character currently occupies this slot.";
        }

        private void EvaluateConfirmButton()
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = (_selectedChoiceIndex != -1 && _selectedPartyMemberIndex != -1);
            }
        }

        private void OnConfirmClicked()
        {
            if (_selectedChoiceIndex == -1 || _selectedPartyMemberIndex == -1) return;

            // Perform the swap
            var newChoice = _currentChoices[_selectedChoiceIndex];
            
            var newPartyMember = new PartyMemberInfo
            {
                character = newChoice,
                equippedSkills = new List<SkillData>()
            };

            var party = RunSessionManager.CurrentParty;
            if (party == null)
            {
                party = new List<PartyMemberInfo>();
                RunSessionManager.CurrentParty = party;
            }

            if (_selectedPartyMemberIndex < party.Count)
            {
                party[_selectedPartyMemberIndex] = newPartyMember;
            }
            else
            {
                party.Add(newPartyMember);
            }

            // Load the next scene
            if (!string.IsNullOrEmpty(combatSceneName))
            {
                SceneManager.LoadScene(combatSceneName);
            }
        }
    }
}
