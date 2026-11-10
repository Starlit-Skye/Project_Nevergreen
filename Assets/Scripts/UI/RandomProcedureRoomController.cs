using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// Controller for the Random Procedure Room UI.
    /// Allows the player to choose between two procedures (Lose a random trait vs. Gamble for a Perfection/Imperfection)
    /// and apply the chosen procedure to a selected Marionette in the party.
    /// </summary>
    public class RandomProcedureRoomController : MonoBehaviour
    {
        public enum ProcedureType
        {
            None,
            LoseTrait,
            Gamble
        }

        [Header("Procedure UI")]
        [SerializeField] private Button procedureButton1;
        [SerializeField] private Button procedureButton2;
        [SerializeField] private GameObject procedurePanel;

        [Header("Selection UI")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Button confirmButton;

        [Header("Info Panel")]
        [SerializeField] private TextMeshProUGUI infoNameText;
        [SerializeField] private TextMeshProUGUI coreStatsText;
        [SerializeField] private TextMeshProUGUI resistancesText;
        [SerializeField] private Transform perfectionsContainer;
        [SerializeField] private Transform imperfectionsContainer;
        [SerializeField] private GameObject perfectionUIItemPrefab;
        [SerializeField] private GameObject imperfectionUIItemPrefab;

        [Header("Styling")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;

        [Header("Configuration")]
        [Tooltip("Chance (0 to 1) of a good outcome when clicking Procedure Button 2.")]
        [Range(0f, 1f)]
        [SerializeField] private float goodOutcomeChance = 0.5f;

        private List<PartyMemberInfo> _party;
        private ProcedureType _chosenProcedure = ProcedureType.None;
        private int _selectedMarionetteIndex = -1;
        private readonly List<Button> _instantiatedChoiceButtons = new List<Button>();
        private readonly List<GameObject> _spawnedTraitItems = new List<GameObject>();
        private System.Random _rng = new System.Random();

        public ProcedureType ChosenProcedure => _chosenProcedure;
        public int SelectedMarionetteIndex => _selectedMarionetteIndex;

        public float GoodOutcomeChance
        {
            get => goodOutcomeChance;
            set => goodOutcomeChance = Mathf.Clamp01(value);
        }
        
        void Start()
        {
            Initialize(RunSessionManager.CurrentParty);
        }
        /// <summary>
        /// Initializes the room state with the provided party (or RunSessionManager.CurrentParty if null).
        /// </summary>
        public void Initialize(List<PartyMemberInfo> party = null)
        {
            _party = party ?? RunSessionManager.CurrentParty;
            _chosenProcedure = ProcedureType.None;
            _selectedMarionetteIndex = -1;

            if (procedurePanel != null)
            {
                procedurePanel.SetActive(true);
            }

            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            ClearChoiceButtons();
            ClearInfoPanel();
            UpdateProcedureButtonHighlights();
            EvaluateConfirmButton();
        }

        private void OnEnable()
        {
            if (procedureButton1 != null)
            {
                procedureButton1.onClick.AddListener(OnProcedure1Clicked);
            }

            if (procedureButton2 != null)
            {
                procedureButton2.onClick.AddListener(OnProcedure2Clicked);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }
        }

        private void OnDisable()
        {
            if (procedureButton1 != null)
            {
                procedureButton1.onClick.RemoveListener(OnProcedure1Clicked);
            }

            if (procedureButton2 != null)
            {
                procedureButton2.onClick.RemoveListener(OnProcedure2Clicked);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }
        }

        /// <summary>
        /// Selects Procedure 1 (Lose 1 Perfection or 1 Imperfection) and opens the Marionette selection screen.
        /// </summary>
        public void OnProcedure1Clicked()
        {
            SelectProcedure(ProcedureType.LoseTrait);
        }

        /// <summary>
        /// Selects Procedure 2 (Gamble between Good and Bad outcome) and opens the Marionette selection screen.
        /// </summary>
        public void OnProcedure2Clicked()
        {
            SelectProcedure(ProcedureType.Gamble);
        }

        private void SelectProcedure(ProcedureType procedure)
        {
            bool wasSelectionOpen = selectionPanel != null ? selectionPanel.activeSelf : _instantiatedChoiceButtons.Count > 0;
            _chosenProcedure = procedure;
            UpdateProcedureButtonHighlights();

            if (selectionPanel != null && !selectionPanel.activeSelf)
            {
                selectionPanel.SetActive(true);
            }

            if (!wasSelectionOpen || _instantiatedChoiceButtons.Count == 0)
            {
                PopulateChoiceButtons();
            }

            EvaluateConfirmButton();
        }

        private void PopulateChoiceButtons()
        {
            ClearChoiceButtons();

            if (_party == null)
            {
                _party = RunSessionManager.CurrentParty;
            }

            if (choiceButtons == null || choiceButtons.Length == 0 || choiceButtons[0] == null)
            {
                Debug.LogError("[RandomProcedureRoomController] No template choice button assigned in choiceButtons array!");
                return;
            }

            Button templateButton = choiceButtons[0];
            Transform container = templateButton.transform.parent;
            templateButton.gameObject.SetActive(false);

            for (int i = 1; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }

            if (_party == null) return;

            for (int i = 0; i < _party.Count; i++)
            {
                var member = _party[i];
                if (member == null || member.character == null) continue;

                Button newBtn = Instantiate(templateButton, container);
                newBtn.gameObject.SetActive(true);

                TextMeshProUGUI txt = newBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = member.character.displayName;
                }

                int index = i;
                newBtn.onClick.RemoveAllListeners();
                newBtn.onClick.AddListener(() => OnMarionetteChoiceClicked(index));
                SetButtonColor(newBtn, i == _selectedMarionetteIndex ? highlightColor : normalColor);

                _instantiatedChoiceButtons.Add(newBtn);
            }
        }

        private void ClearChoiceButtons()
        {
            for (int i = _instantiatedChoiceButtons.Count - 1; i >= 0; i--)
            {
                var btn = _instantiatedChoiceButtons[i];
                if (btn != null)
                {
                    DestroyObjectSafe(btn.gameObject);
                }
            }
            _instantiatedChoiceButtons.Clear();
        }

        /// <summary>
        /// Handles clicking a Marionette button in the Choices Panel.
        /// </summary>
        public void OnMarionetteChoiceClicked(int index)
        {
            if (_party == null || index < 0 || index >= _party.Count || _party[index] == null)
            {
                return;
            }

            _selectedMarionetteIndex = index;
            UpdateChoiceButtonHighlights();
            UpdateInfoPanel(_party[index]);
            EvaluateConfirmButton();
        }

        private void UpdateProcedureButtonHighlights()
        {
            if (procedureButton1 != null)
            {
                SetButtonColor(procedureButton1, _chosenProcedure == ProcedureType.LoseTrait ? highlightColor : normalColor);
            }

            if (procedureButton2 != null)
            {
                SetButtonColor(procedureButton2, _chosenProcedure == ProcedureType.Gamble ? highlightColor : normalColor);
            }
        }

        private void UpdateChoiceButtonHighlights()
        {
            for (int i = 0; i < _instantiatedChoiceButtons.Count; i++)
            {
                if (_instantiatedChoiceButtons[i] != null)
                {
                    SetButtonColor(_instantiatedChoiceButtons[i], i == _selectedMarionetteIndex ? highlightColor : normalColor);
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

        private void UpdateInfoPanel(PartyMemberInfo info)
        {
            if (info == null || info.character == null)
            {
                ClearInfoPanel();
                return;
            }

            var data = info.character;
            if (infoNameText != null)
            {
                infoNameText.text = data.displayName + " - LV " + info.currentLevel;
            }

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

            ClearSpawnedTraitItems();

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
        }

        private void ClearInfoPanel()
        {
            if (infoNameText != null) infoNameText.text = "";
            if (coreStatsText != null) coreStatsText.text = "";
            if (resistancesText != null) resistancesText.text = "";
            ClearSpawnedTraitItems();
        }

        private void ClearSpawnedTraitItems()
        {
            for (int i = _spawnedTraitItems.Count - 1; i >= 0; i--)
            {
                var item = _spawnedTraitItems[i];
                if (item != null)
                {
                    DestroyObjectSafe(item);
                }
            }
            _spawnedTraitItems.Clear();
        }

        private void EvaluateConfirmButton()
        {
            if (confirmButton != null)
            {
                bool hasValidMarionette = _party != null && _selectedMarionetteIndex >= 0 && _selectedMarionetteIndex < _party.Count && _party[_selectedMarionetteIndex] != null;
                confirmButton.interactable = _chosenProcedure != ProcedureType.None && hasValidMarionette;
            }
        }

        /// <summary>
        /// Executes the remembered procedure on the selected Marionette and completes the room.
        /// </summary>
        public void OnConfirmClicked()
        {
            if (_chosenProcedure == ProcedureType.None) return;
            if (_party == null || _selectedMarionetteIndex < 0 || _selectedMarionetteIndex >= _party.Count) return;

            var target = _party[_selectedMarionetteIndex];
            if (target == null) return;

            if (_chosenProcedure == ProcedureType.LoseTrait)
            {
                ApplyLoseTrait(target);
            }
            else if (_chosenProcedure == ProcedureType.Gamble)
            {
                ApplyGamble(target);
            }

            CompleteRoom();
        }

        /// <summary>
        /// Procedure 1: Randomly removes 1 Perfection or 1 Imperfection from the target Marionette.
        /// If both exist, rolls 50/50. If only one exists, removes from that one. If neither exists, does nothing.
        /// </summary>
        public void ApplyLoseTrait(PartyMemberInfo target)
        {
            if (target == null) return;

            bool hasPerfections = target.perfections != null && target.perfections.Count > 0;
            bool hasImperfections = target.imperfections != null && target.imperfections.Count > 0;

            if (!hasPerfections && !hasImperfections)
            {
                return;
            }

            TraitType typeToRemove;
            if (hasPerfections && hasImperfections)
            {
                typeToRemove = _rng.Next(2) == 0 ? TraitType.Perfection : TraitType.Imperfection;
            }
            else if (hasPerfections)
            {
                typeToRemove = TraitType.Perfection;
            }
            else
            {
                typeToRemove = TraitType.Imperfection;
            }

            List<TraitData> list = typeToRemove == TraitType.Perfection ? target.perfections : target.imperfections;
            int removeIndex = _rng.Next(list.Count);
            list.RemoveAt(removeIndex);
        }

        /// <summary>
        /// Procedure 2: Rolls against goodOutcomeChance.
        /// Good Outcome: Removes a random Imperfection (skipped if 0) and adds a random Perfection (skipped if full or no valid candidates).
        /// Bad Outcome: Removes a random Perfection (skipped if 0) and adds a random Imperfection (skipped if full or no valid candidates).
        /// </summary>
        public void ApplyGamble(PartyMemberInfo target)
        {
            if (target == null) return;

            bool isGoodOutcome = _rng.NextDouble() < goodOutcomeChance;
            var traitDb = GameDatabase.Instance != null ? GameDatabase.Instance.TraitDatabase : null;

            if (isGoodOutcome)
            {
                // Step 1: Remove an Imperfection if able
                if (target.imperfections != null && target.imperfections.Count > 0)
                {
                    int removeIndex = _rng.Next(target.imperfections.Count);
                    target.imperfections.RemoveAt(removeIndex);
                }

                // Step 2: Gain a Perfection if able
                if (traitDb != null && traitDb.perfections != null && !target.IsTraitFull(TraitType.Perfection))
                {
                    var validCandidates = GetValidCandidates(target, traitDb.perfections);
                    if (validCandidates.Count > 0)
                    {
                        var chosen = validCandidates[_rng.Next(validCandidates.Count)];
                        target.TryAddTrait(chosen);
                    }
                }
            }
            else
            {
                // Step 1: Remove a Perfection if able
                if (target.perfections != null && target.perfections.Count > 0)
                {
                    int removeIndex = _rng.Next(target.perfections.Count);
                    target.perfections.RemoveAt(removeIndex);
                }

                // Step 2: Gain an Imperfection if able
                if (traitDb != null && traitDb.imperfections != null && !target.IsTraitFull(TraitType.Imperfection))
                {
                    var validCandidates = GetValidCandidates(target, traitDb.imperfections);
                    if (validCandidates.Count > 0)
                    {
                        var chosen = validCandidates[_rng.Next(validCandidates.Count)];
                        target.TryAddTrait(chosen);
                    }
                }
            }
        }

        private static List<TraitData> GetValidCandidates(PartyMemberInfo target, List<TraitData> pool)
        {
            var candidates = new List<TraitData>();
            if (target == null || pool == null) return candidates;

            for (int i = 0; i < pool.Count; i++)
            {
                var trait = pool[i];
                if (trait != null && target.CanAddTrait(trait))
                {
                    candidates.Add(trait);
                }
            }
            return candidates;
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
                Debug.LogError("[RandomProcedureRoomController] CombatUI not found!");
            }
        }

        private static void DestroyObjectSafe(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
    }
}

