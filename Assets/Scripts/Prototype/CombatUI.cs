using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// Prototype combat UI. Manages skill buttons, HP bars, stats panel,
    /// target selection, and the player input flow.
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject skillPanel;
        public GameObject statsPanel;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI battleLogText;
        public TextMeshProUGUI statsDisplayText;
        public GameObject battleEndPanel;
        public TextMeshProUGUI battleEndText;

        [Header("Skill Buttons")]
        public Button[] skillButtons = new Button[4];
        public TextMeshProUGUI[] skillButtonLabels = new TextMeshProUGUI[4];
        public Button moveButton;
        public Button passButton;

        [Header("HP Bar Prefab")]
        public GameObject hpBarPrefab;
        public Canvas worldSpaceCanvas;

        private BattleSystem _battleSystem;
        private List<CombatCharacter> _playerTeam;
        private List<CombatCharacter> _enemyTeam;
        private Dictionary<CombatCharacter, HPBar> _hpBars = new Dictionary<CombatCharacter, HPBar>();
        private AnimationQueueProcessor _animationQueue;

        // Input state
        private SkillData _selectedAction;
        private List<CombatCharacter> _validTargets = new List<CombatCharacter>();
        private bool _selecting = false;
        private bool _isSelectingMove = false;

        // Log
        private List<string> _logLines = new List<string>();
        private const int MAX_LOG_LINES = 8;

        public void Initialize(BattleSystem system, List<CombatCharacter> playerTeam,
                                List<CombatCharacter> enemyTeam)
        {
            _battleSystem = system;
            _playerTeam = playerTeam;
            _enemyTeam = enemyTeam;

            // Cache animation queue reference
            _animationQueue = system.animationQueue;

            // Subscribe to events
            _battleSystem.OnBattleStarted += HandleBattleStarted;
            _battleSystem.OnRoundStarted += HandleRoundStarted;
            _battleSystem.OnTurnStarted += HandleTurnStarted;
            _battleSystem.OnWaitingForPlayerInput += HandleWaitingForInput;
            _battleSystem.OnActionResolved += HandleActionResolved;
            _battleSystem.OnBattleEnded += HandleBattleEnded;
            _battleSystem.OnCharacterDefeated += HandleCharacterDefeated;

            // Subscribe to animation queue lock state
            if (_animationQueue != null)
            {
                _animationQueue.OnInputLockChanged += HandleAnimationLockChanged;
            }

            // Create HP bars
            CreateHPBars();

            // Setup skill buttons
            for (int i = 0; i < skillButtons.Length; i++)
            {
                int idx = i;
                if (skillButtons[i] != null)
                {
                    skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(idx));
                    skillButtons[i].gameObject.SetActive(true);
                    skillButtons[i].interactable = false;
                }
            }

            if (moveButton != null)
            {
                moveButton.onClick.AddListener(OnMoveButtonClicked);
                moveButton.gameObject.SetActive(true);
                moveButton.interactable = false;
            }

            if (passButton != null)
            {
                passButton.onClick.AddListener(OnPassButtonClicked);
                passButton.gameObject.SetActive(true);
                passButton.interactable = false;
            }

            if (battleEndPanel != null)
                battleEndPanel.SetActive(false);

            ClearLog();
        }

        private void CreateHPBars()
        {
            if (hpBarPrefab == null || worldSpaceCanvas == null) return;

            foreach (var c in _playerTeam.Concat(_enemyTeam))
            {
                GameObject barGo = Instantiate(hpBarPrefab, worldSpaceCanvas.transform);
                HPBar bar = barGo.GetComponent<HPBar>();
                if (bar != null)
                {
                    bar.Initialize(c, _animationQueue);
                    _hpBars[c] = bar;
                }
            }
        }

        private void Update()
        {
            // Update HP bar positions
            foreach (var kvp in _hpBars)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    kvp.Value.UpdatePosition();
                }
            }

            // Handle target click during selection
            if (_selecting && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TrySelectTarget();
            }

            // Handle hover for stats display
            UpdateStatsHover();
        }

        private void HandleBattleStarted()
        {
            AddLog("Battle started!");
        }

        private void HandleRoundStarted(int round)
        {
            if (roundText != null)
                roundText.text = $"Round {round}";
        }

        private void HandleTurnStarted(CombatCharacter actor)
        {
            if (turnText != null)
                turnText.text = $"{actor.DisplayName}'s Turn";

            // Failsafe: Refresh all HP bars at start of turn
            foreach (var bar in _hpBars.Values)
            {
                if (bar != null) bar.Refresh();
            }
        }

        private void HandleWaitingForInput()
        {
            CombatCharacter actor = _battleSystem.CurrentActor;
            ShowSkillButtons(actor);
        }

        private void HandleActionResolved(CombatCharacter actor, SkillData skill,
                                          List<CombatCharacter> targets, bool hit, bool crit, int value)
        {
            string targetNames = string.Join(", ", targets.Select(t => t.DisplayName));

            if (skill.modifier.IsHeal)
            {
                AddLog($"{actor.DisplayName} heals {targetNames} for {value} HP");
            }
            else if (hit)
            {
                string critStr = crit ? " (CRIT!)" : "";
                AddLog($"{actor.DisplayName} -> {targetNames}: {value} dmg{critStr}");
            }
            else
            {
                AddLog($"{actor.DisplayName} -> {targetNames}: MISS!");
            }
        }

        private void HandleCharacterDefeated(CombatCharacter character)
        {
            AddLog($"{character.DisplayName} defeated!");

            // Grey out defeated character
            var sr = character.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            HideSkillButtons();

            if (battleEndPanel != null)
            {
                battleEndPanel.SetActive(true);
                if (battleEndText != null)
                    battleEndText.text = outcome == BattleOutcome.Victory ? "VICTORY!" : "DEFEAT";
            }

            AddLog($"Battle ended: {outcome}");
        }

        /// <summary>
        /// Handles the animation queue locking/unlocking input.
        /// When locked: disable all combat buttons.
        /// When unlocked: re-show buttons if we're still waiting for player input.
        /// </summary>
        private void HandleAnimationLockChanged(AnimationQueueState state)
        {
            if (state.isInputLocked)
            {
                HideSkillButtons();
            }
            else if (_battleSystem.IsWaitingForPlayerInput)
            {
                ShowSkillButtons(_battleSystem.CurrentActor);
            }
        }

        private void ShowSkillButtons(CombatCharacter actor)
        {
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == null) continue;

                skillButtons[i].gameObject.SetActive(true);

                if (i < actor.equippedSkills.Count)
                {
                    var skill = actor.equippedSkills[i];
                    skillButtons[i].interactable =
                        actor.CanUseSkillFromRank(skill) && actor.HasRemainingUses(skill);

                    if (skillButtonLabels[i] != null)
                    {
                        string dmgInfo = "";
                        if (skill.modifier.IsDamage)
                            dmgInfo = $" ({skill.modifier.damagePercent * 100f:F0}% ATK)";
                        else if (skill.modifier.IsHeal)
                            dmgInfo = $" (Heal {skill.modifier.healPercent * 100f:F0}%)";

                        skillButtonLabels[i].text = $"{skill.displayName}{dmgInfo}";
                    }
                }
                else
                {
                    skillButtons[i].interactable = false;
                    if (skillButtonLabels[i] != null)
                    {
                        skillButtonLabels[i].text = "";
                    }
                }
            }

            if (moveButton != null)
                moveButton.interactable = true;
            if (passButton != null)
                passButton.interactable = true;
        }

        private void HideSkillButtons()
        {
            foreach (var btn in skillButtons)
            {
                if (btn != null) btn.interactable = false;
            }
            if (moveButton != null) moveButton.interactable = false;
            if (passButton != null) passButton.interactable = false;
        }

        private void OnSkillButtonClicked(int index)
        {
            if (_selecting) HighlightValidTargets(false);
            _isSelectingMove = false;

            CombatCharacter actor = _battleSystem.CurrentActor;
            if (index >= actor.equippedSkills.Count) return;

            _selectedAction = actor.equippedSkills[index];
            _validTargets = _battleSystem.GetValidTargets(actor, _selectedAction);

            if (_validTargets.Count == 0)
            {
                AddLog("No valid targets!");
                return;
            }

            // Need target selection
            _selecting = true;
            HighlightValidTargets(true);
            AddLog($"Select target for {_selectedAction.displayName}...");
        }

        private void OnMoveButtonClicked()
        {
            if (_selecting) HighlightValidTargets(false);
            
            CombatCharacter actor = _battleSystem.CurrentActor;

            // Find all alive allies to swap with/move to
            var allies = actor.IsPlayerTeam ? _playerTeam : _enemyTeam;
            _validTargets = allies
                .Where(a => a.IsAlive && a != actor)
                .ToList();

            if (_validTargets.Count > 0)
            {
                _selecting = true;
                _isSelectingMove = true;
                HighlightValidTargets(true);
                AddLog($"Select an ally to move to their rank...");
            }
            else
            {
                AddLog("No ally to swap with!");
            }
        }

        private void OnPassButtonClicked()
        {
            if (_selecting) HighlightValidTargets(false);
            _selecting = false;
            _isSelectingMove = false;
            
            _battleSystem.SubmitPassAction();
            HideSkillButtons();
            AddLog($"{_battleSystem.CurrentActor.DisplayName} passes.");
        }

        private void TrySelectTarget()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                CombatCharacter clicked = hit.collider.GetComponent<CombatCharacter>();
                if (clicked != null && _validTargets.Contains(clicked))
                {
                    if (_isSelectingMove)
                    {
                        _battleSystem.SubmitMoveAction(clicked);
                    }
                    else
                    {
                        List<CombatCharacter> selected;
                        if (_selectedAction.maxTargets > 1)
                        {
                            selected = new List<CombatCharacter>(_validTargets);
                            if (selected.Count > _selectedAction.maxTargets)
                            {
                                selected.Remove(clicked);
                                selected = selected.Take(_selectedAction.maxTargets - 1).ToList();
                                selected.Insert(0, clicked);
                            }
                        }
                        else
                        {
                            selected = new List<CombatCharacter> { clicked };
                        }
                        _battleSystem.SubmitPlayerAction(_selectedAction, selected);
                    }
                    
                    _selecting = false;
                    _isSelectingMove = false;
                    HighlightValidTargets(false);
                    HideSkillButtons();
                }
            }
        }

        private void HighlightValidTargets(bool highlight)
        {
            foreach (var t in _validTargets)
            {
                var sr = t.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = highlight ? Color.yellow : Color.white;
                }
            }
        }

        private void UpdateStatsHover()
        {
            if (statsDisplayText == null) return;

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                CombatCharacter hovered = hit.collider.GetComponent<CombatCharacter>();
                if (hovered != null)
                {
                    CombatStats stats = hovered.GetEffectiveStats();
                    statsDisplayText.text =
                        $"{hovered.DisplayName} (Lv{hovered.currentLevel})\n" +
                        $"HP: {hovered.currentHP}/{stats.maxHP}\n" +
                        $"ATK: {stats.attack}  DEF: {stats.defense}%\n" +
                        $"ACC: {stats.accuracy}%  DOD: {stats.dodge}%\n" +
                        $"CRIT: {stats.critChance}%  SPD: {stats.speed}\n" +
                        $"Rank: {hovered.rank}  Team: {hovered.team}";
                    return;
                }
            }

            statsDisplayText.text = "Hover over a character to see stats";
        }

        private void AddLog(string message)
        {
            _logLines.Add(message);
            while (_logLines.Count > MAX_LOG_LINES)
                _logLines.RemoveAt(0);

            if (battleLogText != null)
                battleLogText.text = string.Join("\n", _logLines);
        }

        private void ClearLog()
        {
            _logLines.Clear();
            if (battleLogText != null)
                battleLogText.text = "";
        }

        private void OnDestroy()
        {
            if (_battleSystem != null)
            {
                _battleSystem.OnBattleStarted -= HandleBattleStarted;
                _battleSystem.OnRoundStarted -= HandleRoundStarted;
                _battleSystem.OnTurnStarted -= HandleTurnStarted;
                _battleSystem.OnWaitingForPlayerInput -= HandleWaitingForInput;
                _battleSystem.OnActionResolved -= HandleActionResolved;
                _battleSystem.OnBattleEnded -= HandleBattleEnded;
                _battleSystem.OnCharacterDefeated -= HandleCharacterDefeated;
            }

            if (_animationQueue != null)
            {
                _animationQueue.OnInputLockChanged -= HandleAnimationLockChanged;
            }
        }
    }
}
