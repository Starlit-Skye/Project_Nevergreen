using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    public class PartyManagementPanelController : MonoBehaviour
    {
        [Header("Party Members")]
        [Tooltip("The 4 buttons corresponding to the party slots.")]
        public Button[] partyMemberButtons = new Button[4];
        [Tooltip("The names text objects for each party member slot.")]
        public TextMeshProUGUI[] partyMemberNames = new TextMeshProUGUI[4];

        [Header("Character Data")]
        public TextMeshProUGUI nameAndLevelText;
        public TextMeshProUGUI levelUpCostText;
        public Button upgradeButton;
        
        [Header("Skills")]
        public Transform skillsContainer;
        public GameObject skillListItemPrefab;

        [Header("Traits & Stats")]
        public Transform perfectionsContainer;
        public Transform imperfectionsContainer;
        public GameObject perfectionUIItemPrefab;
        public GameObject imperfectionUIItemPrefab;
        
        [Header("Trinkets")]
        public Transform[] trinketsContainers;
        public GameObject trinketUIItemPrefab;

        public TextMeshProUGUI coreStatsText;
        public TextMeshProUGUI resText;

        private PartyMemberInfo _currentSelectedMember;
        private List<GameObject> _spawnedSkillItems = new List<GameObject>();
        private List<GameObject> _spawnedTraitItems = new List<GameObject>();

        [Header("Move Feature")]
        [Tooltip("The Move button used to swap member ranks.")]
        public Button moveButton;
        [Tooltip("The highlight color to show when Move mode is active.")]
        public Color highlightColor = Color.yellow;
        
        private Color[] _originalSlotColors;
        private bool _isMoveMode = false;
        private int _selectedSlotIndex = -1;

        private void OnEnable()
        {
            _selectedSlotIndex = -1;
            ResetMoveModeUI();
            InitializeSlots();
        }

        private void Start()
        {
            // Bind buttons
            for (int i = 0; i < partyMemberButtons.Length; i++)
            {
                int slotIndex = i;
                if (partyMemberButtons[i] != null)
                {
                    partyMemberButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
                }
            }

            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (moveButton == null)
            {
                var moveBtnTransform = transform.Find("Move");
                if (moveBtnTransform != null)
                {
                    moveButton = moveBtnTransform.GetComponent<Button>();
                }
            }
            if (moveButton != null)
            {
                moveButton.onClick.AddListener(ToggleMoveMode);
            }
        }

        private void OnUpgradeClicked()
        {
            if (_currentSelectedMember == null) return;

            if (GameDatabase.Instance != null && GameDatabase.Instance.CombatConfig != null)
            {
                int cost = GameDatabase.Instance.CombatConfig.GetLevelUpCost(_currentSelectedMember.currentLevel);
                
                if (cost >= 0 && RunSessionManager.TrySpendParts(cost))
                {
                    _currentSelectedMember.currentLevel++;
                    SaveManager.SaveRun();
                    DisplayCharacterData(_currentSelectedMember);
                }
            }
        }

        private void InitializeSlots()
        {
            RefreshSlotUI();

            var party = RunSessionManager.CurrentParty;
            PartyMemberInfo rank1Member = null;
            PartyMemberInfo fallbackMember = null;

            if (party != null && party.Count > 0)
            {
                if (party[0] != null && party[0].character != null)
                    rank1Member = party[0];
                fallbackMember = party.Find(m => m != null && m.character != null);
            }

            // Default selection
            PartyMemberInfo toSelect = rank1Member != null ? rank1Member : fallbackMember;
            if (toSelect != null)
            {
                _selectedSlotIndex = party.IndexOf(toSelect);
                DisplayCharacterData(toSelect);
            }
            else
            {
                _selectedSlotIndex = -1;
                ClearDisplay();
            }
        }

        private void RefreshSlotUI()
        {
            var party = RunSessionManager.CurrentParty;
            for (int i = 0; i < partyMemberButtons.Length; i++)
            {
                if (party != null && i < party.Count && party[i] != null && party[i].character != null)
                {
                    var member = party[i];
                    if (partyMemberNames[i] != null)
                        partyMemberNames[i].text = member.character.displayName;
                    
                    if (partyMemberButtons[i] != null)
                        partyMemberButtons[i].interactable = true;
                }
                else
                {
                    if (partyMemberNames[i] != null)
                        partyMemberNames[i].text = "";
                    if (partyMemberButtons[i] != null)
                        partyMemberButtons[i].interactable = false;
                }
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            var party = RunSessionManager.CurrentParty;
            if (party == null || slotIndex >= party.Count || party[slotIndex] == null) return;

            if (_isMoveMode)
            {
                if (slotIndex == _selectedSlotIndex)
                {
                    ToggleMoveMode(); // Cancel move
                }
                else
                {
                    // Swap logic
                    if (_selectedSlotIndex >= 0 && _selectedSlotIndex < party.Count && party[_selectedSlotIndex] != null)
                    {
                        var temp = party[_selectedSlotIndex];
                        party[_selectedSlotIndex] = party[slotIndex];
                        party[slotIndex] = temp;

                        SaveManager.SaveRun();
                        ResetMoveModeUI();
                        RefreshSlotUI();

                        // Update selection to the new position
                        _selectedSlotIndex = slotIndex;
                        DisplayCharacterData(party[slotIndex]);
                    }
                    else
                    {
                        ResetMoveModeUI();
                    }
                }
            }
            else
            {
                _selectedSlotIndex = slotIndex;
                DisplayCharacterData(party[slotIndex]);
            }
        }

        public void ForceRefresh()
        {
            if (_currentSelectedMember != null)
            {
                DisplayCharacterData(_currentSelectedMember);
                RefreshSlotUI();
            }
        }

        private void ToggleMoveMode()
        {
            if (_selectedSlotIndex == -1) return;

            _isMoveMode = !_isMoveMode;

            if (_isMoveMode)
            {
                var party = RunSessionManager.CurrentParty;
                if (_originalSlotColors == null || _originalSlotColors.Length != partyMemberButtons.Length)
                {
                    StoreOriginalSlotColors();
                }

                // Highlight all OTHER team member buttons
                for (int i = 0; i < partyMemberButtons.Length; i++)
                {
                    if (partyMemberButtons[i] == null) continue;

                    if (i != _selectedSlotIndex && party != null && i < party.Count && party[i] != null && party[i].character != null)
                    {
                        var image = partyMemberButtons[i].GetComponent<Image>();
                        if (image != null)
                        {
                            image.color = highlightColor;
                        }
                    }
                }
                
                // Highlight the Move button itself
                if (moveButton != null)
                {
                    var moveImg = moveButton.GetComponent<Image>();
                    if (moveImg != null)
                    {
                        moveImg.color = highlightColor;
                    }
                }
            }
            else
            {
                ResetMoveModeUI();
            }
        }

        private void StoreOriginalSlotColors()
        {
            _originalSlotColors = new Color[partyMemberButtons.Length];
            for (int i = 0; i < partyMemberButtons.Length; i++)
            {
                if (partyMemberButtons[i] != null)
                {
                    var img = partyMemberButtons[i].GetComponent<Image>();
                    if (img != null)
                    {
                        _originalSlotColors[i] = img.color;
                    }
                    else
                    {
                        _originalSlotColors[i] = Color.white;
                    }
                }
            }
        }

        private void ResetMoveModeUI()
        {
            _isMoveMode = false;
            if (_originalSlotColors != null)
            {
                for (int i = 0; i < partyMemberButtons.Length; i++)
                {
                    if (partyMemberButtons[i] != null && i < _originalSlotColors.Length)
                    {
                        var img = partyMemberButtons[i].GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = _originalSlotColors[i];
                        }
                    }
                }
            }

            if (moveButton != null)
            {
                var moveImg = moveButton.GetComponent<Image>();
                if (moveImg != null)
                {
                    moveImg.color = Color.white;
                }
            }
        }

        private void DisplayCharacterData(PartyMemberInfo member)
        {
            _currentSelectedMember = member;
            if (moveButton != null) moveButton.interactable = true;

            if (member == null || member.character == null)
            {
                ClearDisplay();
                return;
            }

            // Name and Level
            if (nameAndLevelText != null)
            {
                nameAndLevelText.text = $"{member.character.displayName} - Lv {member.currentLevel}";
            }

            // Level Up Cost
            if (levelUpCostText != null)
            {
                int cost = -1;
                if (GameDatabase.Instance != null && GameDatabase.Instance.CombatConfig != null)
                {
                    cost = GameDatabase.Instance.CombatConfig.GetLevelUpCost(member.currentLevel);
                }

                if (cost >= 0)
                {
                    levelUpCostText.text = $"Next Upgrade Costs: {cost} Parts (current: {RunSessionManager.Parts})";
                }
                else
                {
                    levelUpCostText.text = $"MAX LEVEL (current: {RunSessionManager.Parts} Parts)";
                }

                if (upgradeButton != null)
                {
                    upgradeButton.interactable = (cost >= 0);
                }
            }

            // Skills
            PopulateSkills(member);

            // Traits
            PopulateTraits(member);

            // Trinkets
            PopulateTrinkets(member);

            // Stats
            PopulateStats(member);
        }

        private void SafeDestroy(GameObject obj)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private void PopulateSkills(PartyMemberInfo member)
        {
            // Clear existing
            foreach (var item in _spawnedSkillItems)
            {
                if (item != null) SafeDestroy(item);
            }
            _spawnedSkillItems.Clear();

            if (skillsContainer == null || skillListItemPrefab == null) return;

            var pool = member.character.totalSkillPool;
            if (pool == null) return;
            
            if (member.equippedSkills == null) member.equippedSkills = new List<SkillData>();
            if (member.unlockedSkills == null) member.unlockedSkills = new List<SkillData>();

            foreach (var skill in pool)
            {
                if (skill == null) continue;

                GameObject item = Instantiate(skillListItemPrefab, skillsContainer);
                _spawnedSkillItems.Add(item);

                // Setup Tooltip
                var tooltipTrigger = item.GetComponent<SkillTooltipTrigger>();
                if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<SkillTooltipTrigger>();
                tooltipTrigger.SetSkill(skill);

                // Setup Name
                var label = item.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = skill.displayName;

                bool isUnlocked = member.unlockedSkills.Contains(skill);
                bool isEquipped = member.equippedSkills.Contains(skill);

                var btn = item.GetComponent<Button>();
                if (btn == null) btn = item.GetComponentInChildren<Button>();
                
                var img = item.GetComponent<Image>();
                if (img == null) img = item.GetComponentInChildren<Image>();

                if (isEquipped)
                {
                    if (btn != null)
                    {
                        // Set the button to "clicked" state by modifying its ColorBlock
                        var cb = btn.colors;
                        cb.normalColor = cb.pressedColor;
                        cb.highlightedColor = cb.pressedColor;
                        cb.selectedColor = cb.pressedColor;
                        btn.colors = cb;

                        btn.interactable = true;
                        btn.onClick.AddListener(() =>
                        {
                            member.equippedSkills.Remove(skill);
                            SaveManager.SaveRun();
                            PopulateSkills(member);
                        });
                    }
                }
                else if (isUnlocked)
                {
                    if (btn != null)
                    {
                        // Default state
                        btn.interactable = true;
                        btn.onClick.AddListener(() =>
                        {
                            if (member.equippedSkills.Count < 4)
                            {
                                member.equippedSkills.Add(skill);
                                SaveManager.SaveRun();
                                PopulateSkills(member);
                            }
                        });
                    }
                }
                else
                {
                    if (btn != null)
                    {
                        // Locked
                        btn.interactable = false;
                    }
                }
            }
        }

        private void PopulateTraits(PartyMemberInfo member)
        {
            foreach (var item in _spawnedTraitItems)
            {
                if (item != null) SafeDestroy(item);
            }
            _spawnedTraitItems.Clear();

            if (member.perfections != null && perfectionsContainer != null && perfectionUIItemPrefab != null)
            {
                foreach (var trait in member.perfections)
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

            if (member.imperfections != null && imperfectionsContainer != null && imperfectionUIItemPrefab != null)
            {
                foreach (var trait in member.imperfections)
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
        }

        private void PopulateTrinkets(PartyMemberInfo member)
        {
            if (trinketsContainers != null)
            {
                foreach (var container in trinketsContainers)
                {
                    if (container == null) continue;
                    foreach (Transform child in container)
                    {
                        SafeDestroy(child.gameObject);
                    }
                }
            }

            if (trinketsContainers != null && trinketUIItemPrefab != null)
            {
                for (int i = 0; i < trinketsContainers.Length; i++)
                {
                    Transform container = trinketsContainers[i];
                    if (container == null) continue;

                    var dropHandler = container.GetComponent<TrinketSlotDropHandler>();
                    if (dropHandler == null) dropHandler = container.gameObject.AddComponent<TrinketSlotDropHandler>();
                    dropHandler.TargetMember = member;
                    dropHandler.TargetSlotIndex = i;

                    if (member.equippedTrinkets != null && i < member.equippedTrinkets.Count)
                    {
                        var trinket = member.equippedTrinkets[i];
                        if (trinket == null) continue;

                        GameObject item = Instantiate(trinketUIItemPrefab, container);

                        var tooltipTrigger = item.GetComponent<TrinketTooltipTrigger>();
                        if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TrinketTooltipTrigger>();
                        tooltipTrigger.SetTrinket(trinket);

                        var uiItem = item.GetComponent<TrinketUIItem>();
                        if (uiItem == null) uiItem = item.AddComponent<TrinketUIItem>();
                        uiItem.Initialize(trinket, member, i);

                        var label = item.GetComponentInChildren<TextMeshProUGUI>();
                        if (label != null) label.text = $"- {trinket.displayName}";
                    }
                }
            }
        }

        private void PopulateStats(PartyMemberInfo member)
        {
            var stats = member.character.GetStatsForLevel(member.currentLevel);

            if (coreStatsText != null)
            {
                coreStatsText.text = $"Max HP: {stats.maxHP}\n" +
                                     $"Attack: {stats.attack}\n" +
                                     $"Defense: {stats.defense}%\n" +
                                     $"Speed: {stats.speed}\n" +
                                     $"Accuracy: {stats.accuracy}%\n" +
                                     $"Dodge: {stats.dodge}%\n" +
                                     $"Crit: {stats.critChance}%";
            }

            if (resText != null)
            {
                resText.text = $"Bleed Res: {stats.bleedResist}%\n" +
                               $"Blight Res: {stats.blightResist}%\n" +
                               $"Stun Res: {stats.stunResist}%\n" +
                               $"Debuff Res: {stats.debuffResist}%\n" +
                               $"Move Res: {stats.moveResist}%";
            }
        }

        private void ClearDisplay()
        {
            _currentSelectedMember = null;
            if (moveButton != null) moveButton.interactable = false;
            if (nameAndLevelText != null) nameAndLevelText.text = "Select a character";
            if (levelUpCostText != null) levelUpCostText.text = "";
            if (coreStatsText != null) coreStatsText.text = "";
            if (resText != null) resText.text = "";

            foreach (var item in _spawnedSkillItems)
            {
                if (item != null) SafeDestroy(item);
            }
            _spawnedSkillItems.Clear();

            foreach (var item in _spawnedTraitItems)
            {
                if (item != null) SafeDestroy(item);
            }
            _spawnedTraitItems.Clear();

            if (trinketsContainers != null)
            {
                foreach (var container in trinketsContainers)
                {
                    if (container == null) continue;
                    foreach (Transform child in container)
                    {
                        SafeDestroy(child.gameObject);
                    }
                }
            }
        }
    }
}
