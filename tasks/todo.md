# Current Task: Save/Load System Implementation

## Completed Tasks: Test Architecture & Bug Fixes
- [x] 1. Initialize and clean up GameDatabase.Instance mock database in test setups and teardowns.
- [x] 2. Update remaining test teardowns to destroy ScriptableObject assets with `DestroyImmediate(..., true)`.
- [x] 3. Run EditMode tests via `tests-run` and verify that all 297 tests pass without errors or unauthorized asset destruction warnings.
- [x] 4. Fix Runtime `NullReferenceException` in `BattleSystem.BuildTurnOrder()`:
  - Add explicit file path fallback (`Assets/Data/GameDatabase.asset`) to `GameDatabase.Instance` getter so prototype scenes correctly load the database at runtime during editor testing.
- [x] 5. Ensure `EnemyFormationData` and `RoomData` ID fields match their respective file names.

---

## Plan: Save/Load System

### Phase 1: Core Serialization
- [ ] 1. Create `SaveManager.cs`.
  - [ ] Implement `SaveGame()` and `LoadGame()` using `JsonUtility` and `Application.persistentDataPath`.
  - [ ] Implement `ClearActiveRun()` to wipe `RunSessionData` but retain persistent meta-data.
  - [ ] Implement `HasActiveRun()` check.

### Phase 2: Game State Integration
- [ ] 2. Update `GlobalConfig.cs` to hold a reference to Cecilia's base `CombatCharacterData` (`ceciliaData`), allowing proper reconstruction on load.
- [ ] 3. Update `RunSessionManager.cs` to trigger Auto-Saves:
  - [ ] On `StartRun()`.
  - [ ] On transitioning/entering a new room.
  - [ ] On `OnApplicationQuit()`.
- [ ] 4. Update Defeat Behavior in `RunSessionManager.cs` / `BattleSystem.cs`:
  - [ ] Ensure defeat calls `SaveManager.ClearActiveRun()` and then `SaveManager.SaveGame()` instead of completely deleting the save file.

### Phase 3: Verification
- [ ] 5. Write `SaveManagerTests.cs` (EditMode) to verify JSON serialization works and `ClearActiveRun()` leaves the save file intact.
- [ ] 6. Run all EditMode tests to confirm no regressions.

## Review
- Wait for user approval before beginning implementation.
