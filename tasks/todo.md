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
