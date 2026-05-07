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

## Remaining TODOs (in code)
- `FinalizeCharacterDefeat`: Apply Move Resist +300% and Pile Expiry status (4 turns)
- Visual handling for Destroyed state (hide character)
