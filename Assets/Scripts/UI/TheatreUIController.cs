using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// UI Controller for the Theatre Room interface.
    /// Manages the repair button text using configuration from GlobalConfig.
    /// </summary>
    public class TheatreUIController : MonoBehaviour
    {
        [Tooltip("The TextMeshPro text component attached to the Theatre Room repair button.")]
        public GameObject fixProjectorButton;

        [Header("Skill Selection")]
        public Button[] marionetteButtons = new Button[4];
        public TextMeshProUGUI[] marionetteButtonTexts = new TextMeshProUGUI[4];
        public Transform skillsContainer;
        public GameObject skillListItemPrefab;
        public Button confirmButton;
        public GameObject skillSelectionPanel;
        
        [Header("Leave")]
        public Button leaveButton;

        private PartyMemberInfo _selectedMarionette;
        private SkillData _selectedSkill;
        private List<GameObject> _spawnedSkillItems = new List<GameObject>();

        private void Start()
        {
            UpdateDisplay();
        }

        private void OnEnable()
        {
            if (fixProjectorButton != null)
            {
                var button = fixProjectorButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(OnRepairButtonClicked);
                }
            }
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }
            if (leaveButton != null)
            {
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }
            UpdateDisplay();
        }

        private void OnDisable()
        {
            if (fixProjectorButton != null)
            {
                var button = fixProjectorButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveListener(OnRepairButtonClicked);
                }
            }
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(OnLeaveClicked);
            }
        }

        /// <summary>
        /// Called when the fix projector button is clicked.
        /// </summary>
        public void OnRepairButtonClicked()
        {
            int cost = (GameDatabase.Instance != null && GameDatabase.Instance.GlobalConfig != null)
                ? GameDatabase.Instance.GlobalConfig.theatreRoomProjectorRepairCost
                : 0;

            if (RunSessionManager.TrySpendScraps(cost))
            {
                UpdateDisplay();
                if (skillSelectionPanel != null)
                {
                    skillSelectionPanel.SetActive(true);
                    InitializeMarionetteButtons();
                }
            }
        }

        /// <summary>
        /// Updates the button text using the repair cost from GlobalConfig.
        /// </summary>
        public void UpdateDisplay()
        {
            int cost = (GameDatabase.Instance != null && GameDatabase.Instance.GlobalConfig != null)
                ? GameDatabase.Instance.GlobalConfig.theatreRoomProjectorRepairCost
                : 0;
            UpdateDisplay(cost);
        }

        /// <summary>
        /// Updates the button text with a specific repair cost value.
        /// </summary>
        /// <param name="cost">The repair cost in Scraps to display.</param>
        public void UpdateDisplay(int cost)
        {
            if (fixProjectorButton != null)
            {
                var button = fixProjectorButton.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = RunSessionManager.Scraps >= cost;
                }

                if (fixProjectorButton.transform.childCount > 0)
                {
                    var textComp = fixProjectorButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    if (textComp != null)
                    {
                        textComp.text = $"Spend {cost} Scraps, unlock a Marionette's skill.";
                    }
                }
            }
        }

        private void InitializeMarionetteButtons()
        {
            var party = RunSessionManager.CurrentParty;
            for (int i = 0; i < 4; i++)
            {
                if (marionetteButtons[i] == null) continue;

                if (party != null && i < party.Count && party[i] != null && party[i].character != null)
                {
                    var member = party[i];
                    if (marionetteButtonTexts[i] != null)
                        marionetteButtonTexts[i].text = member.character.displayName;
                    
                    marionetteButtons[i].interactable = true;
                    int slotIndex = i;
                    marionetteButtons[i].onClick.RemoveAllListeners();
                    marionetteButtons[i].onClick.AddListener(() => OnMarionetteSelected(slotIndex));
                }
                else
                {
                    if (marionetteButtonTexts[i] != null)
                        marionetteButtonTexts[i].text = "";
                    marionetteButtons[i].interactable = false;
                    marionetteButtons[i].onClick.RemoveAllListeners();
                }
            }

            if (confirmButton != null)
                confirmButton.interactable = false;

            ClearSkills();
        }

        private void OnMarionetteSelected(int index)
        {
            var party = RunSessionManager.CurrentParty;
            if (party == null || index >= party.Count || party[index] == null) return;

            _selectedMarionette = party[index];
            PopulateSkills(_selectedMarionette);
        }

        private void PopulateSkills(PartyMemberInfo member)
        {
            ClearSkills();
            _selectedSkill = null;
            if (confirmButton != null) confirmButton.interactable = false;

            if (skillsContainer == null || skillListItemPrefab == null) return;
            if (member.character == null || member.character.totalSkillPool == null) return;

            foreach (var skill in member.character.totalSkillPool)
            {
                if (skill == null) continue;

                GameObject item = Instantiate(skillListItemPrefab, skillsContainer);
                _spawnedSkillItems.Add(item);

                var tooltipTrigger = item.GetComponent<SkillTooltipTrigger>();
                tooltipTrigger.SetSkill(skill);

                var label = item.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = skill.displayName;

                bool isUnlocked = member.unlockedSkills != null && member.unlockedSkills.Contains(skill);
                var btn = item.GetComponent<Button>();

                if (isUnlocked)
                {
                    if (btn != null) btn.interactable = false;
                    var img = item.GetComponent<Image>();
                    if (img != null) img.color = Color.gray;
                    else
                    {
                        var btnImg = item.GetComponentInChildren<Image>();
                        if (btnImg != null) btnImg.color = Color.gray;
                    }
                }
                else
                {
                    if (btn != null)
                    {
                        btn.interactable = true;
                        SkillData capturedSkill = skill;
                        GameObject capturedItem = item;
                        btn.onClick.AddListener(() => OnSkillClicked(capturedSkill, capturedItem));
                    }
                }
            }
        }

        private void ClearSkills()
        {
            foreach (var item in _spawnedSkillItems)
            {
                if (item != null) Destroy(item);
            }
            _spawnedSkillItems.Clear();
        }

        private void OnSkillClicked(SkillData skill, GameObject item)
        {
            _selectedSkill = skill;

            // Reset visuals
            foreach (var spawnedItem in _spawnedSkillItems)
            {
                if (spawnedItem == null) continue;
                
                var btn = spawnedItem.GetComponent<Button>();
                if (btn != null && btn.interactable)
                {
                    var img = spawnedItem.GetComponent<Image>();
                    if (img != null) img.color = Color.white;
                    else
                    {
                        var btnImg = spawnedItem.GetComponentInChildren<Image>();
                        if (btnImg != null) btnImg.color = Color.white;
                    }
                }
            }

            // Highlight selected
            if (item != null)
            {
                var img = item.GetComponent<Image>();
                if (img != null) img.color = Color.yellow;
                else
                {
                    var btnImg = item.GetComponentInChildren<Image>();
                    if (btnImg != null) btnImg.color = Color.yellow;
                }
            }

            if (confirmButton != null) confirmButton.interactable = true;
        }

        private void OnConfirmClicked()
        {
            if (_selectedMarionette == null || _selectedSkill == null) return;

            if (_selectedMarionette.unlockedSkills == null)
            {
                _selectedMarionette.unlockedSkills = new List<SkillData>();
            }

            if (!_selectedMarionette.unlockedSkills.Contains(_selectedSkill))
            {
                _selectedMarionette.unlockedSkills.Add(_selectedSkill);
            }

            CompleteTheatreRoom();
        }

        private void OnLeaveClicked()
        {
            CompleteTheatreRoom();
        }

        private void CompleteTheatreRoom()
        {
            if (skillSelectionPanel != null)
            {
                skillSelectionPanel.SetActive(false);
            }

            // Hide the entire TheatreUIController/Panel to complete the room
            gameObject.SetActive(false);

            _selectedMarionette = null;
            _selectedSkill = null;

            // Transition to room selection
            CombatUI combatUI = Object.FindFirstObjectByType<CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                // Fallback for tests or standalone scenes
                if (!RunSessionManager.RoomCompleted)
                {
                    RunSessionManager.CompleteRoom(new List<RoomData>());
                }
            }
        }
    }
}
