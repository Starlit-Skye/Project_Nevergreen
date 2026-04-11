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

        // Input state
        private SkillData _selectedAction;
        private List<CombatCharacter> _validTargets = new List<CombatCharacter>();
        private bool _selecting = false;

        // Log
        private List<string> _logLines = new List<string>();
        private const int MAX_LOG_LINES = 8;

        public void Initialize(BattleSystem system, List<CombatCharacter> playerTeam,
                                List<CombatCharacter> enemyTeam)
        {
            _battleSystem = system;
            _playerTeam = playerTeam;
            _enemyTeam = enemyTeam;

            // Subscribe to events
            _battleSystem.OnBattleStarted += HandleBattleStarted;
            _battleSystem.OnRoundStarted += HandleRoundStarted;
            _battleSystem.OnTurnStarted += HandleTurnStarted;
            _battleSystem.OnWaitingForPlayerInput += HandleWaitingForInput;
            _battleSystem.OnActionResolved += HandleActionResolved;
            _battleSystem.OnBattleEnded += HandleBattleEnded;
            _battleSystem.OnCharacterDefeated += HandleCharacterDefeated;

            // Create HP bars
            CreateHPBars();

            // Setup skill buttons
            for (int i = 0; i < skillButtons.Length; i++)
            {
                int idx = i;
                if (skillButtons[i] != null)
                {
                    skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(idx));
                    skillButtons[i].gameObject.SetActive(false);
                }
            }

            if (moveButton != null)
            {
                moveButton.onClick.AddListener(OnMoveButtonClicked);
                moveButton.gameObject.SetActive(false);
            }

            if (passButton != null)
            {
                passButton.onClick.AddListener(OnPassButtonClicked);
                passButton.gameObject.SetActive(false);
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
                    bar.SetTarget(c);
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

        private void ShowSkillButtons(CombatCharacter actor)
        {
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (i < actor.equippedSkills.Count)
                {
                    var skill = actor.equippedSkills[i];
                    skillButtons[i].gameObject.SetActive(true);
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
                    skillButtons[i].gameObject.SetActive(false);
                }
            }

            if (moveButton != null)
                moveButton.gameObject.SetActive(true);
            if (passButton != null)
                passButton.gameObject.SetActive(true);
        }

        private void HideSkillButtons()
        {
            foreach (var btn in skillButtons)
            {
                if (btn != null) btn.gameObject.SetActive(false);
            }
            if (moveButton != null) moveButton.gameObject.SetActive(false);
            if (passButton != null) passButton.gameObject.SetActive(false);
        }

        private void OnSkillButtonClicked(int index)
        {
            CombatCharacter actor = _battleSystem.CurrentActor;
            if (index >= actor.equippedSkills.Count) return;

            _selectedAction = actor.equippedSkills[index];
            _validTargets = _battleSystem.GetValidTargets(actor, _selectedAction);

            if (_validTargets.Count == 0)
            {
                AddLog("No valid targets!");
                return;
            }

            // If single target or self, auto-execute on single valid target
            if (_selectedAction.targetScope == TargetScope.Self ||
                (_validTargets.Count == 1 && _selectedAction.maxTargets == 1))
            {
                _battleSystem.SubmitPlayerAction(_selectedAction, _validTargets);
                HideSkillButtons();
                return;
            }

            // If AOE hits all valid targets
            if (_selectedAction.maxTargets >= _validTargets.Count)
            {
                _battleSystem.SubmitPlayerAction(_selectedAction, _validTargets);
                HideSkillButtons();
                return;
            }

            // Need target selection
            _selecting = true;
            HighlightValidTargets(true);
            AddLog($"Select target for {_selectedAction.displayName}...");
        }

        private void OnMoveButtonClicked()
        {
            CombatCharacter actor = _battleSystem.CurrentActor;

            // Find adjacent ally to swap with
            var allies = actor.IsPlayerTeam ? _playerTeam : _enemyTeam;
            var adjacent = allies
                .Where(a => a.IsAlive && a != actor &&
                            Mathf.Abs(a.rank - actor.rank) == 1)
                .FirstOrDefault();

            if (adjacent != null)
            {
                _battleSystem.SubmitMoveAction(adjacent);
                HideSkillButtons();
                AddLog($"{actor.DisplayName} swaps with {adjacent.DisplayName}");
            }
            else
            {
                AddLog("No adjacent ally to swap with!");
            }
        }

        private void OnPassButtonClicked()
        {
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
                    var selected = new List<CombatCharacter> { clicked };
                    _battleSystem.SubmitPlayerAction(_selectedAction, selected);
                    _selecting = false;
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
        }
    }
}
