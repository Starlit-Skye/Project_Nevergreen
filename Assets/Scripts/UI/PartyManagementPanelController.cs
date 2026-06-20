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
        
        [Header("Skills")]
        public Transform skillsContainer;
        public GameObject skillListItemPrefab;

        [Header("Traits & Stats")]
        public TextMeshProUGUI perfectionsText;
        public TextMeshProUGUI imperfectionsText;
        public TextMeshProUGUI coreStatsText;
        public TextMeshProUGUI resText;

        private PartyMemberInfo _currentSelectedMember;
        private List<GameObject> _spawnedSkillItems = new List<GameObject>();

        private void OnEnable()
        {
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
        }

        private void InitializeSlots()
        {
            var party = RunSessionManager.CurrentParty;
            PartyMemberInfo rank1Member = null;
            PartyMemberInfo fallbackMember = null;

            for (int i = 0; i < partyMemberButtons.Length; i++)
            {
                if (party != null && i < party.Count && party[i] != null && party[i].character != null)
                {
                    var member = party[i];
                    if (partyMemberNames[i] != null)
                        partyMemberNames[i].text = member.character.displayName;
                    
                    if (partyMemberButtons[i] != null)
                        partyMemberButtons[i].interactable = true;

                    if (fallbackMember == null) fallbackMember = member;
                    if (i == 0) rank1Member = member;
                }
                else
                {
                    if (partyMemberNames[i] != null)
                        partyMemberNames[i].text = "";
                    if (partyMemberButtons[i] != null)
                        partyMemberButtons[i].interactable = false;
                }
            }

            // Default selection
            PartyMemberInfo toSelect = rank1Member != null ? rank1Member : fallbackMember;
            if (toSelect != null)
            {
                DisplayCharacterData(toSelect);
            }
            else
            {
                ClearDisplay();
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            var party = RunSessionManager.CurrentParty;
            if (party != null && slotIndex < party.Count && party[slotIndex] != null)
            {
                DisplayCharacterData(party[slotIndex]);
            }
        }

        private void DisplayCharacterData(PartyMemberInfo member)
        {
            _currentSelectedMember = member;

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
            }

            // Skills
            PopulateSkills(member);

            // Traits
            PopulateTraits(member);

            // Stats
            PopulateStats(member);
        }

        private void PopulateSkills(PartyMemberInfo member)
        {
            // Clear existing
            foreach (var item in _spawnedSkillItems)
            {
                if (item != null) Destroy(item);
            }
            _spawnedSkillItems.Clear();

            if (skillsContainer == null || skillListItemPrefab == null) return;

            var pool = member.character.totalSkillPool;
            if (pool == null) return;

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

                // Gray out if not equipped
                bool isEquipped = member.equippedSkills != null && member.equippedSkills.Contains(skill);
                if (!isEquipped)
                {
                    var img = item.GetComponent<Image>();
                    if (img != null) img.color = Color.gray;
                    else
                    {
                        var btnImg = item.GetComponentInChildren<Image>();
                        if (btnImg != null) btnImg.color = Color.gray;
                    }
                }
            }
        }

        private void PopulateTraits(PartyMemberInfo member)
        {
            string perfectionsStr = "";
            string imperfectionsStr = "";

            if (member.perfections != null)
            {
                foreach (var trait in member.perfections)
                {
                    if (trait != null) perfectionsStr += $"- {trait.displayName}\n";
                }
            }

            if (member.imperfections != null)
            {
                foreach (var trait in member.imperfections)
                {
                    if (trait != null) imperfectionsStr += $"- {trait.displayName}\n";
                }
            }

            if (perfectionsText != null) perfectionsText.text = string.IsNullOrEmpty(perfectionsStr) ? "None" : perfectionsStr;
            if (imperfectionsText != null) imperfectionsText.text = string.IsNullOrEmpty(imperfectionsStr) ? "None" : imperfectionsStr;
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
            if (nameAndLevelText != null) nameAndLevelText.text = "Select a character";
            if (levelUpCostText != null) levelUpCostText.text = "";
            if (perfectionsText != null) perfectionsText.text = "";
            if (imperfectionsText != null) imperfectionsText.text = "";
            if (coreStatsText != null) coreStatsText.text = "";
            if (resText != null) resText.text = "";

            foreach (var item in _spawnedSkillItems)
            {
                if (item != null) Destroy(item);
            }
            _spawnedSkillItems.Clear();
        }
    }
}
