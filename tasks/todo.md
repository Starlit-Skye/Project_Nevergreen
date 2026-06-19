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
- [x] 1. Create `SaveManager.cs`.
  - [x] Implement `SaveGame()` and `LoadGame()` using `JsonUtility` and `Application.persistentDataPath`.
  - [x] Implement `ClearActiveRun()` to wipe `RunSessionData` but retain persistent meta-data.
  - [x] Implement `HasActiveRun()` check.

### Phase 2: Game State Integration
- [x] 2. Update `GlobalConfig.cs` to hold a reference to Cecilia's base `CombatCharacterData` (`ceciliaData`), allowing proper reconstruction on load.
- [x] 3. Update `RunSessionManager.cs` to trigger Auto-Saves:
  - [x] On `StartRun()`.
  - [x] On transitioning/entering a new room.
  - [x] On `OnApplicationQuit()`.
- [x] 4. Update Defeat Behavior in `RunSessionManager.cs` / `BattleSystem.cs`:
  - [x] Ensure defeat calls `SaveManager.ClearActiveRun()` and then `SaveManager.SaveGame()` instead of completely deleting the save file.

### Phase 3: Verification
- [x] 5. Write `SaveManagerTests.cs` (EditMode) to verify JSON serialization works and `ClearActiveRun()` leaves the save file intact.
- [x] 6. Run all EditMode tests to confirm no regressions.

### Phase 4: Main Menu Continue Button Integration
- [x] 7. Add `IsResumingRun` flag to `RunSessionManager.cs` to prevent room-skipping on load.
- [x] 8. Add `playButton` field, `Start()` logic, and `OnContinueClicked()` to `CeciliaSkillSelectController.cs`.
- [x] 9. Wire `playButton` field in `MainMenu.unity` and remove inspector-assigned listener.
- [x] 10. Add/Update tests and manually verify both Play and Continue flows.

## Review
- Wait for user approval before beginning implementation.
