using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Core turn-based combat state machine.
    /// Manages round/turn sequencing, action resolution, and battle lifecycle.
    /// </summary>
    public class BattleSystem : MonoBehaviour
    {
        [Header("Config")]
        public CombatConfig combatConfig;

        [Tooltip("Animation queue processor. Auto-created at runtime if not assigned.")]
        public AnimationQueueProcessor animationQueue;

        // --- Runtime State ---
        public BattleState CurrentState { get; private set; } = BattleState.Inactive;
        public int CurrentRound { get; private set; } = 0;
        public CombatCharacter CurrentActor { get; private set; }

        // Layout configuration (injected by Bootstrap or set in Inspector)
        [HideInInspector] public float playerBaseX = -3f;
        [HideInInspector] public float playerSpacingX = -2f;
        [HideInInspector] public float enemyBaseX = 3f;
        [HideInInspector] public float enemySpacingX = 2f;

        private List<CombatCharacter> _playerTeam = new List<CombatCharacter>();
        private List<CombatCharacter> _enemyTeam = new List<CombatCharacter>();
        private List<TurnEntry> _turnOrder = new List<TurnEntry>();
        private int _currentTurnIndex = 0;
        private System.Random _rng;

        // Player input state
        private bool _waitingForPlayerInput = false;

        // --- Events ---
        public event Action OnBattleStarted;
        public event Action<int> OnRoundStarted; // round number
        public event Action<CombatCharacter> OnTurnStarted;
        public event Action<CombatCharacter, SkillData, SkillContext> OnActionResolved; // actor, skill, context
        public event Action<BattleOutcome> OnBattleEnded;
        public event Action OnWaitingForPlayerInput;
        public event Action<CombatCharacter> OnCharacterDefeated;
        public event Action<CombatCharacter> OnCharacterRemoved;

        public List<CombatCharacter> PlayerTeam => _playerTeam;
        public List<CombatCharacter> EnemyTeam => _enemyTeam;
        public bool IsWaitingForPlayerInput => _waitingForPlayerInput;

        private void Awake()
        {
            // Ensure animation queue exists early so UI can bind to it
            if (animationQueue == null)
            {
                animationQueue = GetComponent<AnimationQueueProcessor>();
                if (animationQueue == null)
                {
                    animationQueue = gameObject.AddComponent<AnimationQueueProcessor>();
                }
            }
        }

        /// <summary>
        /// Start a battle with the given teams.
        /// </summary>
        public void StartBattle(List<CombatCharacter> playerTeam, List<CombatCharacter> enemyTeam)
        {
            _playerTeam = playerTeam;
            _enemyTeam = enemyTeam;
            _rng = new System.Random();
            CurrentRound = 0;

            // Subscribe to defeat events and inject config
            foreach (var c in _playerTeam.Concat(_enemyTeam))
            {
                c.combatConfig = combatConfig;
                c.OnDefeated += HandleCharacterDefeated;
                c.OnStateChanged += HandleCharacterStateChanged;
            }

            CurrentState = BattleState.RoundStart;
            OnBattleStarted?.Invoke();

            StartCoroutine(BattleLoop());
        }

        private IEnumerator BattleLoop()
        {
            while (CurrentState != BattleState.BattleEnd)
            {
                switch (CurrentState)
                {
                    case BattleState.RoundStart:
                        yield return StartRound();
                        break;

                    case BattleState.CharacterTurn:
                        yield return ProcessTurn();
                        break;

                    case BattleState.RoundEnd:
                        EndRound();
                        break;
                }

                yield return null;
            }
        }

        private IEnumerator StartRound()
        {
            CurrentRound++;
            BuildTurnOrder();
            _currentTurnIndex = 0;

            OnRoundStarted?.Invoke(CurrentRound);
            Debug.Log($"[BattleSystem] === Round {CurrentRound} Start === Turn order: " +
                      string.Join(", ", _turnOrder.Select(t => $"{t.character.DisplayName}(spd:{t.speed})")));

            CurrentState = BattleState.CharacterTurn;
            yield return null;
        }

        /// <summary>
        /// Build turn order by Speed. Ties: enemies before players, then front rank first.
        /// </summary>
        private void BuildTurnOrder()
        {
            _turnOrder.Clear();

            foreach (var c in _playerTeam.Where(c => c.IsAlive))
            {
                CombatStats stats = c.GetEffectiveStats();
                for (int a = 0; a < c.characterData.actionsPerRound; a++)
                {
                    _turnOrder.Add(new TurnEntry(c, stats.speed));
                }
            }

            foreach (var c in _enemyTeam.Where(c => c.IsAlive))
            {
                CombatStats stats = c.GetEffectiveStats();
                for (int a = 0; a < c.characterData.actionsPerRound; a++)
                {
                    _turnOrder.Add(new TurnEntry(c, stats.speed));
                }
            }

            // Sort: higher speed first
            // Tie-break 1: enemies before players
            // Tie-break 2: lower rank (front) first
            _turnOrder.Sort((a, b) =>
            {
                int speedCompare = b.speed.CompareTo(a.speed);
                if (speedCompare != 0) return speedCompare;

                // Enemies act before players on tie
                int teamCompare = GetTeamPriority(a.character.team)
                                  .CompareTo(GetTeamPriority(b.character.team));
                if (teamCompare != 0) return teamCompare;

                // Same team: front rank first
                return a.character.rank.CompareTo(b.character.rank);
            });
        }

        private int GetTeamPriority(Team team)
        {
            return team == Team.Enemy ? 0 : 1; // Enemies go first on tie
        }

        private IEnumerator ProcessTurn()
        {
            if (CheckBattleEnd()) yield break;

            if (_currentTurnIndex >= _turnOrder.Count)
            {
                CurrentState = BattleState.RoundEnd;
                yield break;
            }

            TurnEntry entry = _turnOrder[_currentTurnIndex];
            CurrentActor = entry.character;

            // Skip dead characters
            if (!CurrentActor.IsAlive)
            {
                _currentTurnIndex++;
                yield break;
            }

            OnTurnStarted?.Invoke(CurrentActor);
            Debug.Log($"[BattleSystem] Turn: {CurrentActor.DisplayName} (Rank {CurrentActor.rank})");

            // Phase 1: Apply DOT/HOT effects (bleed/blight still hurt stunned characters)
            StatusProcessor.ProcessPeriodicEffects(CurrentActor);

            // Wait for DOT/HOT animations to finish before proceeding
            if (animationQueue != null)
            {
                while (animationQueue.IsBusy) yield return null;
            }

            // Check if died from DOT or if battle ended during animations
            if (CheckBattleEnd()) yield break;

            // Check death from DOT (redundant with CheckBattleEnd but kept for turn index increment)
            if (!CurrentActor.IsAlive)
            {
                StatusProcessor.TickDurations(CurrentActor, combatConfig.stunRecoveryResistBonus);
                _currentTurnIndex++;
                yield break;
            }

            // Check stun (before ticking durations so stun correctly skips the turn)
            if (CurrentActor.isStunned)
            {
                Debug.Log($"[BattleSystem] {CurrentActor.DisplayName} is stunned! Skipping turn.");
                StatusProcessor.TickDurations(CurrentActor, combatConfig.stunRecoveryResistBonus);
                _currentTurnIndex++;
                yield break;
            }

            // Phase 2: Tick status durations and remove expired
            StatusProcessor.TickDurations(CurrentActor, combatConfig.stunRecoveryResistBonus);

            // Player or Enemy action
            if (CurrentActor.IsPlayerTeam)
            {
                yield return WaitForPlayerAction();
            }
            else
            {
                yield return ExecuteEnemyAction();
            }

            // Wait for all queued animations to finish before advancing
            if (animationQueue != null)
            {
                while (animationQueue.IsBusy)
                {
                    yield return null;
                }
            }

            // Tick durations for all Piles after every character action
            foreach (var c in _playerTeam.Concat(_enemyTeam).Where(c => c.IsPile).ToList())
            {
                c.pileDuration--;
                
                // If the Pile has decayed, move to Destroyed state
                if (c.pileDuration <= 0)
                {
                    c.state = LifeState.Destroyed;
                    Debug.Log($"[BattleSystem] {c.DisplayName} Pile has decayed and is now Destroyed.");
                }
            }

            // The battle end is now handled by the OnDefeated event, but we check here 
            // to ensure the coroutine stops if defeat occurred during the action.
            if (CurrentState == BattleState.BattleEnd)
            {
                yield break;
            }

            _currentTurnIndex++;
        }

        private IEnumerator WaitForPlayerAction()
        {
            _waitingForPlayerInput = true;
            OnWaitingForPlayerInput?.Invoke();

            // Wait until player submits an action
            while (_waitingForPlayerInput)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Called by UI when player selects a skill and targets.
        /// </summary>
        public void SubmitPlayerAction(SkillData skill, List<CombatCharacter> targets)
        {
            if (!_waitingForPlayerInput) return;

            ExecuteSkill(CurrentActor, skill, targets);
            _waitingForPlayerInput = false;
        }

        /// <summary>
        /// Called by UI when player uses the Move action. Selects target rank to move to.
        /// </summary>
        public void SubmitMoveAction(CombatCharacter target)
        {
            if (!_waitingForPlayerInput) return;

            ExecuteMoveAndShift(CurrentActor, target.rank);
            _waitingForPlayerInput = false;
        }

        public void ExecuteMoveAndShift(CombatCharacter mover, int targetRank)
        {
            var team = mover.IsPlayerTeam ? _playerTeam : _enemyTeam;
            int maxRank = Mathf.Clamp(team.Count, 1, 4); // Max ranks is 4, but limited by team size
            targetRank = Mathf.Clamp(targetRank, 1, maxRank);
            
            int startRank = mover.rank;
            if (startRank == targetRank) return;

            bool movingForward = targetRank < startRank;

            // 2. Identify characters that will shift to make room
            List<CombatCharacter> shiftingCharacters = new List<CombatCharacter>();
            foreach (var character in team)
            {
                if (character == mover) continue;

                if (movingForward)
                {
                    // Moving from 4 to 1. Ranks 1, 2, 3 must shift back (+1)
                    if (character.rank >= targetRank && character.rank < startRank)
                    {
                        shiftingCharacters.Add(character);
                    }
                }
                else
                {
                    // Moving from 1 to 4. Ranks 2, 3, 4 must shift forward (-1)
                    if (character.rank <= targetRank && character.rank > startRank)
                    {
                        shiftingCharacters.Add(character);
                    }
                }
            }

            // 3. Update logical ranks
            mover.rank = targetRank;
            foreach (var character in shiftingCharacters)
            {
                character.rank += movingForward ? 1 : -1;
            }

            // 4. Create and enqueue tweens
            if (animationQueue != null)
            {
                float moveDuration = 0.5f;
                ParallelStep moveParallel = new ParallelStep($"{mover.DisplayName} MoveAndShift");

                // Tween the mover
                float targetX = GetXPositionForRank(mover.team, mover.rank);
                var tween = DG.Tweening.ShortcutExtensions.DOMoveX(mover.transform, targetX, moveDuration);
                moveParallel.AddStep(new DOTweenStep($"Move_{mover.DisplayName}", tween, moveDuration));

                // Tween each shifting character
                foreach (var character in shiftingCharacters)
                {
                    float shiftTargetX = GetXPositionForRank(character.team, character.rank);
                    var sTween = DG.Tweening.ShortcutExtensions.DOMoveX(character.transform, shiftTargetX, moveDuration);
                    moveParallel.AddStep(new DOTweenStep($"Shift_{character.DisplayName}", sTween, moveDuration));
                }

                animationQueue.Enqueue(moveParallel);
                Debug.Log($"[BattleSystem] {mover.DisplayName} moved to rank {targetRank}. Shifting {shiftingCharacters.Count} allies.");
            }
        }

        public float GetXPositionForRank(Team team, int rank)
        {
            if (team == Team.Player)
            {
                return playerBaseX + (rank - 1) * playerSpacingX;
            }
            else
            {
                return enemyBaseX + (rank - 1) * enemySpacingX;
            }
        }

        /// <summary>
        /// Called by UI when player chooses to pass their turn.
        /// </summary>
        public void SubmitPassAction()
        {
            if (!_waitingForPlayerInput) return;

            Debug.Log($"[BattleSystem] {CurrentActor.DisplayName} passes their turn.");

            // Enqueue visual pass animation length
            if (animationQueue != null)
            {
                animationQueue.Enqueue(new WaitTimerStep(
                    $"{CurrentActor.DisplayName} Pass",
                    0.3f));
            }

            _waitingForPlayerInput = false;
        }

        private IEnumerator ExecuteEnemyAction()
        {
            yield return new WaitForSeconds(0.5f); // Brief delay for readability

            var brain = CurrentActor.GetComponent<Nevergreen.Combat.AI.AIBrain>();
            if (brain == null)
            {
                Debug.LogError($"[BattleSystem] {CurrentActor.DisplayName} is missing an AIBrain component. Passing.");
                yield break;
            }

            var decision = brain.EvaluateTurn(this);
            
            // Record the decision in the character's history
            brain.RecordDecision(decision);

            if (decision.isPass)
            {
                Debug.Log($"[BattleSystem] {CurrentActor.DisplayName} AI decided to pass.");
                
                // Enqueue visual pass animation length
                if (animationQueue != null)
                {
                    animationQueue.Enqueue(new WaitTimerStep($"{CurrentActor.DisplayName} Pass", 0.3f));
                }
            }
            else
            {
                ExecuteSkill(CurrentActor, decision.skill, decision.targets);
            }
        }

        private void ExecuteSkill(CombatCharacter user, SkillData skill, List<CombatCharacter> targets)
        {
            user.RecordSkillUse(skill);

            var ctx = new SkillContext(user, skill, targets, this, _rng);

            Debug.Log($"[BattleSystem] {user.DisplayName} uses {skill.displayName}" +
                      $" on {string.Join(", ", targets.Select(t => t.DisplayName))}");

            // Create parallel container for simultaneous skill animations
            ParallelStep skillAnimParallel = null;
            if (animationQueue != null)
            {
                skillAnimParallel = new ParallelStep($"{user.DisplayName}:{skill.displayName}");
                
                if (user.animator != null)
                {
                    string stateName = (skill.targetScope == TargetScope.Self || skill.targetScope == TargetScope.Allies) 
                        ? "Cast" 
                        : "Attack";
                    skillAnimParallel.AddStep(new AnimatorStep($"{user.DisplayName}:{skill.displayName}_act", user.animator, stateName, 1.0f));
                }
                else
                {
                    // Fallback
                    skillAnimParallel.AddStep(new WaitTimerStep($"{user.DisplayName}:{skill.displayName}_wait", 1.0f));
                }
                
                // Enqueue parallel group now (it will gather steps before starting next frame)
                animationQueue.Enqueue(skillAnimParallel);
            }

            if (animationQueue != null)
            {
                animationQueue.BeginBatch($"{user.DisplayName}:{skill.displayName}_UI_Batch");
            }

            for (int hit = 0; hit < ctx.totalHits; hit++)
            {
                ctx.currentHitIndex = hit;

                foreach (var target in targets)
                {
                    if (!target.IsAlive && !target.IsPile) continue;

                    // 1. Resolve Target
                    CombatCharacter finalTarget = CombatCalculator.GetEffectiveTarget(target, ctx);
                    ctx.primaryTarget = finalTarget;
                    
                    // The pure strategy approach: Execute modular effects
                    foreach (var effect in skill.effects)
                    {
                        if (effect != null)
                        {
                            effect.Execute(ctx, finalTarget);
                        }
                    }
                    
                    // Check if taking damage killed the target or if we need to do UI syncing post-hit
                    if (ctx.didHit && !skill.modifier.IsHeal && skillAnimParallel != null)
                    {
                        // The Guardian always flinches and takes the effects
                        if (finalTarget.animator != null)
                        {
                            skillAnimParallel.AddStep(new AnimatorStep($"hit_{finalTarget.DisplayName}", finalTarget.animator, "TakeDamage", 0.5f));
                        }

                        // The Protected ally also flinches if they were the original target
                        if (finalTarget != target && target.animator != null)
                        {
                            skillAnimParallel.AddStep(new AnimatorStep($"guard_flinch_{target.DisplayName}", target.animator, "TakeDamage", 0.5f));
                        }
                    }
                    
                    // Note: Event emission here could be tied to context data at the end of the effect resolution.
                    // For the sake of the combat ui prototype reacting, we synthesize the event.
                    OnActionResolved?.Invoke(user, skill, ctx);
                }
            }

            if (animationQueue != null)
            {
                animationQueue.EndBatch();
            }
        }



        /// <summary>
        /// Get valid targets for a skill based on scope and rank constraints.
        /// </summary>
        public List<CombatCharacter> GetValidTargets(CombatCharacter user, SkillData skill)
        {
            List<CombatCharacter> pool;

            switch (skill.targetScope)
            {
                case TargetScope.Self:
                    return new List<CombatCharacter> { user };

                case TargetScope.Allies:
                    pool = user.IsPlayerTeam ? _playerTeam : _enemyTeam;
                    break;

                case TargetScope.Enemies:
                default:
                    pool = user.IsPlayerTeam ? _enemyTeam : _playerTeam;
                    break;
            }

            bool isHealingSkill = skill.effects.Any(e => e is HealEffect);

            return pool
                .Where(c => (c.IsAlive || (c.IsPile && !isHealingSkill)) && skill.targetRanks.Contains(c.rank))
                .ToList();
        }

        private bool CheckBattleEnd()
        {
            if (CurrentState == BattleState.BattleEnd) return true;
            // Cecilia (ceci) is the primary character; her defeat is a battle loss.
            bool ceciliaDefeated = _playerTeam.Any(c => c.CharacterId == "ceci" && c.state != LifeState.Alive);
            bool allPlayersDead = _playerTeam.Count == 0 || _playerTeam.All(c => c.state != LifeState.Alive);
            bool allEnemiesDead = _enemyTeam.Count == 0 || _enemyTeam.All(c => c.state != LifeState.Alive);

            if (ceciliaDefeated || allPlayersDead)
            {
                CurrentState = BattleState.BattleEnd;
                string reason = ceciliaDefeated ? "CECILIA DEFEATED" : "ALL PLAYERS DEFEATED";
                Debug.Log($"[BattleSystem] === DEFEAT ({reason}) ===");
                OnBattleEnded?.Invoke(BattleOutcome.Defeat);
                return true;
            }

            if (allEnemiesDead)
            {
                CurrentState = BattleState.BattleEnd;
                Debug.Log("[BattleSystem] === VICTORY ===");
                OnBattleEnded?.Invoke(BattleOutcome.Victory);
                return true;
            }

            return false;
        }

        private void EndRound()
        {
            Debug.Log($"[BattleSystem] === Round {CurrentRound} End ===");
            CurrentState = BattleState.RoundStart;
        }

        private void HandleCharacterDefeated(CombatCharacter character, bool wasCritical)
        {
            Debug.Log($"[BattleSystem] {character.DisplayName} has been defeated!{(wasCritical ? " (CRITICAL KILL)" : "")}");

            // Character is already in Dying state (set by TakeDamage)

            // 1. Enqueue the death animation
            if (animationQueue != null && character.animator != null)
            {
                animationQueue.Enqueue(new AnimatorStep($"{character.DisplayName} Die", character.animator, "Die", 1.5f));
            }

            // 3. Enqueue deferred transition (runs right after death animation finishes)
            if (animationQueue != null)
            {
                animationQueue.Enqueue(new ActionStep($"{character.DisplayName} Spawn Pile", () =>
                {
                    FinalizeCharacterDefeat(character, wasCritical);
                }));
            }
            else
            {
                FinalizeCharacterDefeat(character, wasCritical);
            }

            OnCharacterDefeated?.Invoke(character);
            CheckBattleEnd();
        }

        private void FinalizeCharacterDefeat(CombatCharacter character, bool wasCritical)
        {
            bool canFormPile = character.characterData != null && character.characterData.leavesPileOnDeath;

            if (wasCritical || !canFormPile)
            {
                character.state = LifeState.Destroyed;
                Debug.Log($"[BattleSystem] {character.DisplayName} is destroyed (no Pile formed).");
            }
            else
            {
                character.state = LifeState.Pile;
                character.currentHP = character.baseStats.maxHP / 2;
                character.pileDuration = 4; // Decay after 4 character actions
                
                // Clear all previous status effects (Bleeds, Buffs, etc.)
                character.statusEffects.Clear();

                Debug.Log($"[BattleSystem] {character.DisplayName} has become a Pile. HP: {character.currentHP}, Duration: {character.pileDuration}");
            }
        }

        private void HandleCharacterStateChanged(CombatCharacter character, LifeState newState)
        {
            if (newState == LifeState.Destroyed)
            {
                HandleCharacterDestroyed(character);
            }
        }

        private void HandleCharacterDestroyed(CombatCharacter character)
        {
            Debug.Log($"[BattleSystem] {character.DisplayName} has been destroyed and removed from battle.");
            
            var team = character.IsPlayerTeam ? _playerTeam : _enemyTeam;
            int removedRank = character.rank;

            // 1. Remove from team list
            team.Remove(character);

            // 2. Shift ranks of characters behind
            ShiftRanksAfterRemoval(team, removedRank);

            // 3. Enqueue physical destruction (after small delay for any lingering effects/animations)
            if (animationQueue != null)
            {
                animationQueue.Enqueue(new ActionStep($"{character.DisplayName} Cleanup", () =>
                {
                    if (Application.isPlaying) Destroy(character.gameObject);
                    else DestroyImmediate(character.gameObject);
                }));
            }
            else
            {
                if (Application.isPlaying) Destroy(character.gameObject);
                else DestroyImmediate(character.gameObject);
            }

            // 4. Notify external systems
            OnCharacterRemoved?.Invoke(character);

            // 5. Check battle end (in case this was the last character)
            CheckBattleEnd();
        }

        private void ShiftRanksAfterRemoval(List<CombatCharacter> team, int gapRank)
        {
            List<CombatCharacter> shifting = team.Where(c => c.rank > gapRank).ToList();
            if (shifting.Count == 0) return;

            ParallelStep shiftParallel = new ParallelStep("Rank Shift After Removal");
            float shiftDuration = 0.4f;

            foreach (var character in shifting)
            {
                character.rank -= 1;
                float targetX = GetXPositionForRank(character.team, character.rank);
                
                var tween = DG.Tweening.ShortcutExtensions.DOMoveX(character.transform, targetX, shiftDuration);
                shiftParallel.AddStep(new DOTweenStep($"Shift_{character.DisplayName}", tween, shiftDuration));
            }

            if (animationQueue != null)
            {
                animationQueue.Enqueue(shiftParallel);
            }
        }

        private void OnDestroy()
        {
            foreach (var c in _playerTeam.Concat(_enemyTeam))
            {
                c.OnDefeated -= HandleCharacterDefeated;
                c.OnStateChanged -= HandleCharacterStateChanged;
            }
        }
    }

    public enum BattleState
    {
        Inactive,
        RoundStart,
        CharacterTurn,
        RoundEnd,
        BattleEnd
    }

    public enum BattleOutcome
    {
        Victory,
        Defeat
    }

    /// <summary>
    /// One entry in the turn order list.
    /// </summary>
    public class TurnEntry
    {
        public CombatCharacter character;
        public int speed;

        public TurnEntry(CombatCharacter character, int speed)
        {
            this.character = character;
            this.speed = speed;
        }
    }
}
