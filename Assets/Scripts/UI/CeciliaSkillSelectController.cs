using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Controls the Skill Selection UI panel in the Main Menu.
    /// Allows the player to pick exactly 4 skills for Cecilia before starting a run.
    /// </summary>
    public class CeciliaSkillSelectController : MonoBehaviour
    {
        [Header("Character Setup")]
        [Tooltip("Cecilia's CharacterData asset, used to load her totalSkillPool.")]
        public CharacterData ceciliaData;

        [Header("UI References")]
        [Tooltip("The root panel for the skill selection UI. Toggled on/off.")]
        public GameObject skillSelectPanel;

        [Tooltip("Parent transform for skill pool list items (ScrollView content).")]
        public Transform skillListContent;

        [Tooltip("Prefab for each skill entry in the skill pool list.")]
        public GameObject skillListItemPrefab;

        [Tooltip("The 4 equipped skill slot images (left to right).")]
        public Image[] equippedSlotImages = new Image[4];

        [Tooltip("The 4 equipped skill slot buttons (clicking removes a skill).")]
        public Button[] equippedSlotButtons = new Button[4];

        [Tooltip("The Start button. Only interactable when exactly 4 skills are equipped.")]
        public Button startButton;

        [Tooltip("The Close button. Closes the skill selection panel.")]
        public Button closeButton;

        [Tooltip("Portrait image of Cecilia.")]
        public Image portraitImage;

        [Header("Main Menu Integration")]
        [Tooltip("The Play/Continue button on the main menu.")]
        public Button playButton;

        [Header("Scene")]
        [Tooltip("Name of the combat scene to load.")]
        public string combatSceneName = "CombatPrototype";

        // Runtime state
        private List<SkillData> _availableSkills = new List<SkillData>();
        private SkillData[] _equippedSkills = new SkillData[4];
        private List<GameObject> _spawnedListItems = new List<GameObject>();

        private void Start()
        {
            ConfigurePlayButton();
        }

        /// <summary>
        /// Configures the Play/Continue button based on whether a saved run exists.
        /// </summary>
        private void ConfigurePlayButton()
        {
            if (playButton == null) return;

            playButton.onClick.RemoveAllListeners();

            var label = playButton.GetComponentInChildren<TextMeshProUGUI>();

            if (SaveManager.HasSavedRun())
            {
                if (label != null) label.text = "Continue";
                playButton.onClick.AddListener(OnContinueClicked);
            }
            else
            {
                if (label != null) label.text = "Play";
                playButton.onClick.AddListener(Open);
            }
        }

        private void OnEnable()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
        }

        private void OnDisable()
        {
            TooltipEvents.HideTooltip();

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);

            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
        }

        /// <summary>
        /// Opens the skill selection panel and populates the skill list.
        /// Called by the Play button on the main menu.
        /// </summary>
        public void Open()
        {
            if (ceciliaData == null)
            {
                Debug.LogError("[CeciliaSkillSelectController] No CharacterData assigned!");
                return;
            }

            // Reset equipped skills
            for (int i = 0; i < 4; i++)
                _equippedSkills[i] = null;

            // Load available skills from CharacterData's totalSkillPool
            _availableSkills.Clear();
            _availableSkills.AddRange(ceciliaData.totalSkillPool);

            // Show panel
            if (skillSelectPanel != null)
                skillSelectPanel.SetActive(true);

            PopulateSkillList();
            RefreshUI();
        }

        /// <summary>
        /// Closes the skill selection panel without starting a run.
        /// </summary>
        public void OnCloseClicked()
        {
            if (skillSelectPanel != null)
                skillSelectPanel.SetActive(false);
        }

        private void OnStartClicked()
        {
            // Build party info
            var partyMember = new PartyMemberInfo
            {
                character = ceciliaData,
                equippedSkills = new List<SkillData>(),
                unlockedSkills = new List<SkillData>()
            };

            for (int i = 0; i < 4; i++)
            {
                if (_equippedSkills[i] != null)
                {
                    partyMember.equippedSkills.Add(_equippedSkills[i]);
                    partyMember.unlockedSkills.Add(_equippedSkills[i]);
                }
            }

            // Set into RunSessionManager
            RunSessionManager.Clear();
            RunSessionManager.CurrentParty.Add(partyMember);

            // Initialize session for the run
            RunSessionManager.Initialize();

            // Load combat scene
            SceneManager.LoadScene(combatSceneName);
        }

        /// <summary>
        /// Called when the player clicks "Continue" on the main menu.
        /// Loads the saved run and transitions directly to combat, skipping skill selection.
        /// </summary>
        private void OnContinueClicked()
        {
            bool loaded = SaveManager.LoadRun();
            if (loaded)
            {
                RunSessionManager.IsResumingRun = true;
                SceneManager.LoadScene(combatSceneName);
            }
            else
            {
                Debug.LogWarning("[CeciliaSkillSelectController] Failed to load saved run. Falling back to skill selection.");
                Open();
            }
        }

        private void PopulateSkillList()
        {
            // Clear existing items
            foreach (var item in _spawnedListItems)
            {
                if (item != null)
                    Destroy(item);
            }
            _spawnedListItems.Clear();

            if (skillListContent == null || skillListItemPrefab == null)
            {
                Debug.LogWarning("[CeciliaSkillSelectController] Missing skillListContent or skillListItemPrefab.");
                return;
            }

            // Spawn an entry for each skill in the pool
            foreach (var skill in _availableSkills)
            {
                if (skill == null) continue;

                GameObject item = Instantiate(skillListItemPrefab, skillListContent);
                _spawnedListItems.Add(item);

                // Set up the text
                var label = item.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null)
                    label.text = skill.displayName;

                // Set up click to equip
                var btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    SkillData capturedSkill = skill;
                    btn.onClick.AddListener(() => OnSkillPoolItemClicked(capturedSkill));
                }

                // Attach TooltipTrigger
                var tooltipTrigger = item.GetComponent<SkillTooltipTrigger>();
                if (tooltipTrigger == null)
                {
                    tooltipTrigger = item.AddComponent<SkillTooltipTrigger>();
                }
                tooltipTrigger.SetSkill(skill);
            }
        }

        private void OnSkillPoolItemClicked(SkillData skill)
        {
            // Check if already equipped
            for (int i = 0; i < 4; i++)
            {
                if (_equippedSkills[i] == skill)
                    return; // Already equipped, ignore
            }

            // Find the first empty slot
            for (int i = 0; i < 4; i++)
            {
                if (_equippedSkills[i] == null)
                {
                    _equippedSkills[i] = skill;
                    RefreshUI();
                    return;
                }
            }

            // All slots full, do nothing
        }

        private void OnEquippedSlotClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4) return;

            _equippedSkills[slotIndex] = null;
            RefreshUI();
        }

        private void RefreshUI()
        {
            int equippedCount = 0;

            for (int i = 0; i < 4; i++)
            {
                if (equippedSlotButtons != null && i < equippedSlotButtons.Length && equippedSlotButtons[i] != null)
                {
                    int capturedIndex = i;
                    equippedSlotButtons[i].onClick.RemoveAllListeners();
                    equippedSlotButtons[i].onClick.AddListener(() => OnEquippedSlotClicked(capturedIndex));

                    var tooltipTrigger = equippedSlotButtons[i].GetComponent<SkillTooltipTrigger>();
                    if (tooltipTrigger == null)
                    {
                        tooltipTrigger = equippedSlotButtons[i].gameObject.AddComponent<SkillTooltipTrigger>();
                    }
                    tooltipTrigger.SetSkill(_equippedSkills[i]);
                }

                if (equippedSlotImages != null && i < equippedSlotImages.Length && equippedSlotImages[i] != null)
                {
                    // Show skill name on the slot via a child text component
                    var slotLabel = equippedSlotImages[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (slotLabel != null)
                    {
                        slotLabel.text = _equippedSkills[i] != null ? _equippedSkills[i].displayName : "";
                    }

                    // Dim empty slots
                    equippedSlotImages[i].color = _equippedSkills[i] != null
                        ? Color.white
                        : new Color(1f, 1f, 1f, 0.3f);
                }

                if (_equippedSkills[i] != null)
                    equippedCount++;
            }

            // Update pool list item interactability
            for (int i = 0; i < _spawnedListItems.Count && i < _availableSkills.Count; i++)
            {
                var btn = _spawnedListItems[i].GetComponent<Button>();
                if (btn != null)
                {
                    bool isEquipped = System.Array.IndexOf(_equippedSkills, _availableSkills[i]) >= 0;
                    btn.interactable = !isEquipped;
                }
            }

            // Start button only interactable with exactly 4 skills
            if (startButton != null)
                startButton.interactable = equippedCount == 4;
        }
    }
}
