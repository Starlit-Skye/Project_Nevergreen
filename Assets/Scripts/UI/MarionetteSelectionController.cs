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

        [Header("UI - Info Panel (Split)")]
        public TextMeshProUGUI coreStatsText;
        public TextMeshProUGUI resistancesText;
        public Transform perfectionsContainer;
        public Transform imperfectionsContainer;
        public GameObject perfectionUIItemPrefab;
        public GameObject imperfectionUIItemPrefab;

        [Header("UI - Skills Panel")]
        public Transform skillsPanelContainer;
        public GameObject skillListItemPrefab;

        [Header("UI - Controls")]
        public Button confirmButton;

        [Header("Styling")]
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;

        // State
        private List<PartyMemberInfo> _currentChoices = new List<PartyMemberInfo>();
        private int _selectedChoiceIndex = -1;
        private int _selectedPartyMemberIndex = -1;
        private List<Button> _instantiatedButtons = new List<Button>();
        private List<TextMeshProUGUI> _instantiatedTexts = new List<TextMeshProUGUI>();
        private List<GameObject> _spawnedSkillItems = new List<GameObject>();
        private List<GameObject> _spawnedTraitItems = new List<GameObject>();

        private void Start()
        {

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

            var globalConfig = GameDatabase.Instance.GlobalConfig;
            int choiceCount = globalConfig != null ? globalConfig.marionetteChoiceCount : 4;

            _currentChoices = MarionetteGenerator.GenerateRandomMarionette(choiceCount) 
                ?? new List<PartyMemberInfo>();

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
                    txt.text = _currentChoices[i].character.displayName;
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

                bool isCecilia = false;
                if (party != null && i < party.Count && party[i] != null && party[i].character != null)
                {
                    partyMemberTexts[i].text = party[i].character.displayName;
                    var charData = party[i].character;
                    if (charData.characterId == "ceci" || charData.displayName == "Cecilia")
                    {
                        isCecilia = true;
                    }
                }
                else
                {
                    partyMemberTexts[i].text = "Empty Slot";
                }

                partyMemberButtons[i].interactable = !isCecilia;

                int index = i;
                partyMemberButtons[i].onClick.RemoveAllListeners();
                if (!isCecilia)
                {
                    partyMemberButtons[i].onClick.AddListener(() => OnPartyMemberClicked(index));
                }
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
            var party = RunSessionManager.CurrentParty;
            if (party != null && index < party.Count && party[index] != null && party[index].character != null)
            {
                var charData = party[index].character;
                if (charData.characterId == "ceci" || charData.displayName == "Cecilia")
                {
                    return; // Cecilia is uninteractable
                }
            }

            _selectedPartyMemberIndex = index;
            UpdateHighlights();
            
            if (party != null && index < party.Count && party[index] != null && party[index].character != null)
            {
                UpdateInfoPanel(party[index]);
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

        private void UpdateInfoPanel(PartyMemberInfo info)
        {
            var data = info.character;
            if (infoNameText != null)
                infoNameText.text = data.displayName + " - LV " + info.currentLevel;

            var stats = data.GetStatsForLevel(info.currentLevel);

            if (coreStatsText != null && stats != null)
            {
                coreStatsText.text = $"Max HP: {stats.maxHP}\n" +
                                     $"Attack: {stats.attack}\n" +
                                     $"Defense: {stats.defense}%\n" +
                                     $"Speed: {stats.speed}\n" +
                                     $"Accuracy: {stats.accuracy}%\n" +
                                     $"Dodge: {stats.dodge}%\n" +
                                     $"Crit: {stats.critChance}%";
            }

            if (resistancesText != null && stats != null)
            {
                resistancesText.text = $"Bleed Res: {stats.bleedResist}%\n" +
                                       $"Blight Res: {stats.blightResist}%\n" +
                                       $"Stun Res: {stats.stunResist}%\n" +
                                       $"Debuff Res: {stats.debuffResist}%\n" +
                                       $"Move Res: {stats.moveResist}%";
            }

            foreach (var item in _spawnedTraitItems)
            {
                if (item != null) Destroy(item);
            }
            _spawnedTraitItems.Clear();

            if (info.perfections != null && perfectionsContainer != null && perfectionUIItemPrefab != null)
            {
                foreach (var trait in info.perfections)
                {
                    if (trait == null) continue;
                    GameObject item = Instantiate(perfectionUIItemPrefab, perfectionsContainer);
                    _spawnedTraitItems.Add(item);

                    var tooltipTrigger = item.GetComponent<TraitTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TraitTooltipTrigger>();
                    tooltipTrigger.SetTrait(trait);

                    var label = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null) label.text = $"- {trait.displayName}";
                }
            }

            if (info.imperfections != null && imperfectionsContainer != null && imperfectionUIItemPrefab != null)
            {
                foreach (var trait in info.imperfections)
                {
                    if (trait == null) continue;
                    GameObject item = Instantiate(imperfectionUIItemPrefab, imperfectionsContainer);
                    _spawnedTraitItems.Add(item);

                    var tooltipTrigger = item.GetComponent<TraitTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TraitTooltipTrigger>();
                    tooltipTrigger.SetTrait(trait);

                    var label = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null) label.text = $"- {trait.displayName}";
                }
            }


            foreach (var item in _spawnedSkillItems)
            {
                if (item != null) Destroy(item);
            }
            _spawnedSkillItems.Clear();

            if (skillsPanelContainer != null && skillListItemPrefab != null && info.equippedSkills != null)
            {
                foreach (var skill in info.equippedSkills)
                {
                    if (skill == null) continue;

                    GameObject item = Instantiate(skillListItemPrefab, skillsPanelContainer);
                    _spawnedSkillItems.Add(item);

                    var tooltipTrigger = item.GetComponent<SkillTooltipTrigger>();
                    if (tooltipTrigger == null)
                    {
                        tooltipTrigger = item.AddComponent<SkillTooltipTrigger>();
                    }
                    tooltipTrigger.SetSkill(skill);

                    var label = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                    {
                        label.text = skill.displayName;
                    }
                }
            }
        }

        private void ClearInfoPanel()
        {
            if (infoNameText != null)
                infoNameText.text = "Empty Slot";

            if (coreStatsText != null) coreStatsText.text = "";
            if (resistancesText != null) resistancesText.text = "";

            foreach (var item in _spawnedSkillItems)
            {
                if (item != null) Destroy(item);
            }
            _spawnedSkillItems.Clear();

            foreach (var item in _spawnedTraitItems)
            {
                if (item != null) Destroy(item);
            }
            _spawnedTraitItems.Clear();
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

            var party = RunSessionManager.CurrentParty;
            if (party == null)
            {
                party = new List<PartyMemberInfo>();
                RunSessionManager.CurrentParty = party;
            }

            // Extra safety guard: do not replace Cecilia
            if (_selectedPartyMemberIndex < party.Count && party[_selectedPartyMemberIndex] != null && party[_selectedPartyMemberIndex].character != null)
            {
                var charData = party[_selectedPartyMemberIndex].character;
                if (charData.characterId == "ceci" || charData.displayName == "Cecilia")
                {
                    return;
                }
            }

            // Perform the swap
            var newPartyMember = _currentChoices[_selectedChoiceIndex];

            if (_selectedPartyMemberIndex < party.Count)
            {
                party[_selectedPartyMemberIndex] = newPartyMember;
            }
            else
            {
                party.Add(newPartyMember);
            }

            gameObject.SetActive(false);

            /*// Load the next scene
            if (!string.IsNullOrEmpty(combatSceneName))
            {
                SceneManager.LoadScene(combatSceneName);
            }*/
        }
    }
}
