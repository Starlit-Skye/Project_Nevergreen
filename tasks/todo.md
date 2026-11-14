# Task Management

## Plan: RubblesEventController

Status: COMPLETED. Pattern source: `SafeRoomController.cs` / `RecycleRoomController.cs`.

### Confirmed Requirements & Decisions
- **Go Around**: Increases `RunSessionManager.ExhaustionRound` by configured amount (default: 2), clamped to `ExhaustionLimit`. If limit <= 0, no-op (no penalty).
- **Clear Rubble**: Deals damage to all members in party by percentage of max HP (default: 0.25 = 25%). Leaves members at a minimum of 1 HP if damage would kill them. If already <= 1 HP, unchanged.
- **Percentage mapping**: `0.10 = 10%`, `0.25 = 25%`, `0.50 = 50%`, `1.00 = 100%`.
- **Room Completion Flow**: Deactivates self, calls `FindFirstObjectByType<CombatUI>().ShowRoomSelectionImmediately()` (which persists state via `RunSessionManager.CompleteRoom()`).
- **Scope**: Script (`RubblesEventController.cs`) and EditMode tests (`RubblesEventControllerTests.cs`). Prefab creation & wiring will be handled by the user. Spec updates are skipped.

### Checklist
- [x] Implement `Assets/Scripts/UI/RubblesEventController.cs`
- [x] Implement `Assets/Editor/Tests/RubblesEventControllerTests.cs`
- [x] Run automated tests and verify all test cases pass

### Review
- Implemented `Nevergreen.UI.RubblesEventController` with `goAroundButton`, `clearRubbleButton`, configurable `exhaustionIncrease` (default `2`) and `damagePercent` (default `0.25f`, with tooltip examples `0.10 = 10%`, `0.25 = 25%`, `0.50 = 50%`, `1.00 = 100%`).
- `OnGoAroundClicked` disables both buttons, increases `RunSessionManager.ExhaustionRound` clamped to `RunSessionManager.ExhaustionLimit` (no-op when `ExhaustionLimit <= 0`), and invokes `CompleteRoom()`.
- `OnClearRubbleClicked` disables both buttons, applies percentage max-HP damage via pure helper `ComputeDamagedHP` (flooring at 1 HP and preserving members already <= 1 HP, storing `null` when at full HP), and invokes `CompleteRoom()`.
- `CompleteRoom()` deactivates the event GameObject and calls `CombatUI.ShowRoomSelectionImmediately()`.
- Verified via `tests-run` (`EditMode`): 12/12 tests in `Nevergreen.Tests.RubblesEventControllerTests` passed and 760/760 total EditMode tests passed (0 failed, 0 skipped).
