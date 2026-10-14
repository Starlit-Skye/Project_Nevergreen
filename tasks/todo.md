# Task: Fix Defeat Panel and Return to Main Menu Button Display

## Problem Summary
When the player is defeated (Ceci downed or all allies dead), `BattleEndPanel` and the "Back to Main Menu" button do not appear if `RunSessionManager.NextRoomData` has `activationType == RoomActivationType.OnCombatVictory`. This happens because `CombatUI.HandleBattleEnded` suppressed `BattleEndPanel` when `hasOnVictoryRoomEffect` was true regardless of whether the battle outcome was Victory or Defeat.

## Todo List
- [x] 1. Write regression unit tests in `Assets/Editor/Tests/CombatUITests.cs` reproducing the bug (checking defeat panel visibility when NextRoomData has OnCombatVictory) <!-- id: 1 -->
- [x] 2. Update `Assets/Scripts/Prototype/CombatUI.cs` to ensure `battleEndPanel` is displayed on `BattleOutcome.Defeat` regardless of `hasOnVictoryRoomEffect` <!-- id: 2 -->
- [x] 3. Run unit tests using `tests-run` MCP tool to verify fix and ensure no regressions <!-- id: 3 -->
- [x] 4. Document results and review in `tasks/todo.md` <!-- id: 4 -->

## Review & Verification Results
- **Unit Tests Added**:
  - `HandleBattleEnded_ShowsBattleEndPanel_OnDefeat_EvenWhenNextRoomHasOnCombatVictoryEffect`: Successfully reproduced the issue prior to code fix (failed with expected `BattleEndPanel` active = true, but was false), and passed after code fix.
  - `HandleBattleEnded_HidesBattleEndPanel_OnVictory_WhenNextRoomHasOnCombatVictoryEffect`: Confirmed that the early panel suppression for victory reward / pending room effect sequence remains functional without regression.
- **Suite Execution**: Ran all 557 EditMode tests in the project (`ai-game-developer:tests-run`). All 557 tests passed (0 failures, 0 skipped).
- **Files Modified**:
  - `Assets/Scripts/Prototype/CombatUI.cs`
  - `Assets/Editor/Tests/CombatUITests.cs`

