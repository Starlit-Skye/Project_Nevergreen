# TODO: Defeat Button Refactor & Test Fixes

- [x] Fix GameDatabase testing singleton behavior to fix existing 2 test failures
  - [x] Modify `GameDatabase.cs` to add `_bypassAutoDiscovery` logic
  - [x] Run EditMode tests to confirm that all existing tests pass
- [x] Implement Defeat Button Refactor in `CombatUI.cs`
  - [x] Modify `CombatUI.Initialize` to reset button onClick listeners to avoid leakage across initialization/scenes
  - [x] Modify `CombatUI.HandleBattleEnded` for `BattleOutcome.Defeat`:
    - Set `nextRoomButton` text component to "Back to Main Menu"
    - Set `nextRoomButton` onClick listener to load the "MainMenu" scene
    - Activate `nextRoomButton` (set active to true)
  - [x] Modify `CombatUI.HandleBattleEnded` for `BattleOutcome.Victory`:
    - Ensure `nextRoomButton` text is reset to "Next Room" when Victory path is chosen
- [x] Update Combat UI tests to match new expected behavior
  - [x] Update `HandleBattleEnded_HidesNextRoomButton_OnDefeat` in `CombatUITests.cs` to assert that the next room button is ACTIVE on Defeat (since it is repurposed as the Back to Main Menu button)
  - [x] Add a test verifying that `nextRoomButton` text is "Back to Main Menu" on Defeat
- [x] Run EditMode tests and verify that everything compiles and passes cleanly

## Review
The refactoring is complete and all 293 tests successfully pass. The GameDatabase auto-discovery bypass successfully isolated the edit-mode testing environment. The defeat button now appropriately changes text and functionality.
