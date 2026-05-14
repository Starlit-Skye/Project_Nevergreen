# Pile Mechanic Implementation

Reference: `Docs/proposals/IMPLEMENTATION_PROPOSAL_PILE.md`

## Implementation Steps

- [x] **Step 1**: Add `LifeState` enum to `CombatCharacter.cs` with `Alive`, `Dying`, `Pile`, `Destroyed`
- [x] **Step 2**: Add `state` field, `IsPile` property; update `IsAlive` to use state
- [x] **Step 3**: Add `leavesPileOnDeath` to `CharacterData.cs`
- [x] **Step 4**: Add `ActionStep` class to `AnimationQueueEntry.cs`
- [x] **Step 5**: Update `TakeDamage` to set Dying state and accept `isCritical`; update `OnDefeated` signature
- [x] **Step 6**: Update `DamageEffect.cs` to pass `isCritical` from context
- [x] **Step 7**: Update `GuardStatusInstance.cs` for new `OnDefeated` signature
- [x] **Step 8**: Update `BattleSystem.HandleCharacterDefeated` with ActionStep for deferred Pile creation
- [x] **Step 9**: Add `FinalizeCharacterDefeat` to `BattleSystem.cs`
- [x] **Step 10**: Update `GetValidTargets` to include Piles
- [x] **Step 11**: Update `ProcessTurn` to tick Pile durations
- [x] **Step 12**: Update `CheckBattleEnd` for LifeState
- [x] **Step 13**: Fix test reflection call in `BattleEndTests.cs`
- [x] **Step 14**: Fix `PeriodicEffectTests.Restore_DoesNotHealDeadCharacter` for new state model
- [x] **Step 15**: Add `state = LifeState.Alive` reset in `InitializeForCombat`
- [x] **Step 16**: Compile and verify — **75/75 tests pass**

## Character Removal & Rank Shifting

- [x] **Step 1**: Add configuration layout values and `OnCharacterRemoved` event to `BattleSystem.cs`.
- [x] **Step 2**: Create `GetXPositionForRank` and refactor `ExecuteMoveAndShift`.
- [x] **Step 3**: Inject layout configurations in `CombatSceneBootstrap.cs`.
- [x] **Step 4**: Add `HandleCharacterStateChanged` and `HandleCharacterDestroyed` to `BattleSystem.cs`.
- [x] **Step 5**: Subscribe to `OnStateChanged` in `BattleSystem.StartBattle`.
- [x] **Step 6**: Add `ShiftRanksAfterRemoval` to `BattleSystem.cs`.
- [x] **Step 7**: Update `CheckBattleEnd` to handle empty teams.
- [x] **Step 8**: Write unit tests in `FormationTests.cs` and verify they pass.

### Review Section
The automated character removal and rank-shifting logic has been successfully implemented and tested.
* When a character's state changes to `LifeState.Destroyed`, the system automatically captures its current rank, removes the character from the active team list, and evaluates remaining characters in that team.
* Allies stationed behind the removed character are shifted forward by one rank, and visual `DOMoveX` tweens are queued to reflect their new deterministic positions (based on centralized base X and spacing settings).
* External systems can now hook into the `OnCharacterRemoved` event.
* Extensive testing covering empty teams, full shifts, and team counts verified the resilience of the update. All tests successfully passed.

# Modular Enemy AI Framework

Reference: `Docs/architecture/AI_SYSTEM_PROPOSAL.md` & `AI_Implementation_Plan.md`

## Phase 1: Core Infrastructure & Interfaces
- [x] **Step 1**: Create `AIDecision.cs` struct for standardized action output.
- [x] **Step 2**: Create `AIHistory.cs` for turn tracking and contextual logic.
- [x] **Step 3**: Define `IAIBehavior`, `IAICondition`, and `IAITargeting` interfaces.
- [x] **Step 4**: Implement `AIBrain.cs` skeleton component.
- [x] **Step 5**: Resolve compilation dependencies and verify with EditMode tests.

## Phase 2: ScriptableObject Containers
- [x] **Step 1**: Create `EnemyAIProfile.cs` (Main SO container).
- [x] **Step 2**: Implement `AIBehaviorNode`, `AIConditionNode`, and `AITargetingNode` abstract base classes.
- [x] **Step 3**: Update `CharacterData.cs` to include an `AIProfile` reference.
- [x] **Step 4**: Implement core evaluation loop in `AIBrain.cs`.

## Phase 3: Concrete Implementations (The Nodes)
- [x] **Step 1**: Implement `RandomSkillBehavior` (Fallback).
- [x] **Step 2**: Implement `HealthCondition`.
- [x] **Step 3**: Implement `SimpleTargeting`.
- [x] **Step 4**: Implement `RuleBasedBehavior` (Bridge node).

## Phase 4: Combat System Integration
- [x] **Step 1**: Refactor `BattleSystem.ExecuteEnemyAction()` to use `AIBrain`.
- [x] **Step 2**: Initialize `AIBrain` in `CombatCharacter`.
- [x] **Step 3**: Integrate `AIHistory` updates into the turn sequence.

## Phase 5: Editor Tooling
- [x] **Step 1**: Create custom property drawers for polymorphic SO lists.
- [x] **Step 2**: Implement `TypeCache` search for behavior selection.

### Review Section
Phase 1 has been completed. All interfaces and core data structures are in place.

Phase 2 has been completed. `EnemyAIProfile` created, base polymorphic nodes implemented, and `AIBrain` loop defined.

Phase 3 has been completed. Foundations for modular logic are ready:
* `RandomSkillBehavior` — Safety net/Fallback.
* `HealthCondition` — Multi-target HP evaluation.
* `SimpleTargeting` — Strategy-based target resolution.
* `RuleBasedBehavior` — Complex conditional rules (e.g. "If HP < 50%, Heal").

Phase 4 has been completed. `BattleSystem.ExecuteEnemyAction()` now utilizes the `AIBrain` for evaluating turns and properly updates the `AIHistory`. `CombatCharacter` injects the `defaultAIProfile` on initialization.

Phase 5 has been completed. A robust `[SubclassSelector]` attribute and `PropertyDrawer` have been implemented. Designers can now use a clean dropdown menu in the Unity Inspector to select between different AI behaviors, conditions, and targeting strategies. This system also extends to the existing `SkillData` effects.

The comprehensive unit test suite in `AITests.cs` is complete and passing (102/102 tests pass in the project). These tests cover AIBrain evaluation loop, target finding strategies, correct behavior execution, and condition logic checking, ensuring deterministic and robust Enemy AI gameplay.
Venom Bite Status Debugging

## Status
- [ ] Investigate StatusEffect execution and event firing <!-- id: 5 -->
- [ ] Verify CombatUI subscription to status events <!-- id: 6 -->
- [ ] Add improved logging for status application failure/resistance <!-- id: 7 -->
- [ ] Fix the root cause of the missing combat log <!-- id: 8 -->
- [ ] Verify the fix in-game <!-- id: 9 -->

## Research Notes
- `Venom Bite` skill has `DamageEffect` then `StatusEffect` (Blight).
- `StatusEffect.Execute` only logs to console on success.
- `CombatUI` handles both success and resistance logs via `OnStatusApplied`.
- If no log appears, `OnStatusApplied` might not be firing or `StatusEffect.Execute` is returning early.
