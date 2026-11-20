# Task Management

## Plan: MuralRoomController

Status: COMPLETED. Pattern source: `RubblesEventController.cs` / `RerollPerfectionsRoomController.cs`.

### Requirements & Flow
- **Buttons**: `destroyButton`, `gazeButton`, `leaveButton`.
- **Destroy**: All party members take configurable % max HP damage (clamped at minimum 1 HP; if already <= 1 HP, unchanged). Grants configurable Scraps (`RunSessionManager.GrantScraps`).
- **Gaze**: Increases exhaustion by configurable amount (`RunSessionManager.ExhaustionRound`, clamped to `ExhaustionLimit`; no-op if limit <= 0). Grants each party member a random Perfection from `TraitDatabase.perfections` respecting no-dupes, exclusion, and max cap rules.
- **Button Transition**: After clicking either Destroy or Gaze, hide Destroy and Gaze buttons, and enable the Leave button (disabled by default in the UI; script never disables it). Defaults: damagePercent 0.25, scrapReward 50, exhaustionIncrease 2. Prefab wiring done by user.
- **Leave**: Completes room flow (`gameObject.SetActive(false)`, `CombatUI.ShowRoomSelectionImmediately()`).

### Checklist
- [x] Review & approve plan
- [x] Implement `Assets/Scripts/UI/MuralRoomController.cs`
- [x] Implement `Assets/Editor/Tests/MuralRoomControllerTests.cs`
- [x] Run automated tests and verify 100% pass rate

### Review
- Implemented `Nevergreen.UI.MuralRoomController` with `destroyButton`, `gazeButton`, `leaveButton`, `damagePercent` (`0.25f`), `scrapReward` (`50`), and `exhaustionIncrease` (`2`).
- `Initialize()` leaves `leaveButton` untouched (since it is disabled by default in the UI prefab) and sets `destroyButton` and `gazeButton` interactable.
- `OnDestroyClicked()` deals non-lethal percentage Max HP damage (clamped at minimum 1 HP), grants Scraps via `RunSessionManager.GrantScraps()`, hides `destroyButton` and `gazeButton`, and enables `leaveButton`.
- `OnGazeClicked()` increases `RunSessionManager.ExhaustionRound` clamped to `ExhaustionLimit` (no-op when limit <= 0), grants each party member a random Perfection from `TraitDatabase.perfections` via `TryGrantRandomPerfection` (respecting max cap, duplicate ID, and direct/reverse opposite trait rules), hides `destroyButton` and `gazeButton`, and enables `leaveButton`.
- `OnLeaveClicked()` executes `CompleteRoom()` (`gameObject.SetActive(false)` + `CombatUI.ShowRoomSelectionImmediately()`).
- Verified via `tests-run` (`EditMode`): 15/15 tests in `Nevergreen.Tests.MuralRoomControllerTests` passed, and 775/775 total EditMode tests passed (0 failed, 0 skipped).


## Plan: Harden `AudioManager.MusicLoopRoutine` (minimal guard, revised)

Status: COMPLETED. Prereq done: `runInBackground` enabled. Full plan: see the implementation_plan_audio_hardening.md artifact.

### Problem
`MusicLoopRoutine` inner loop `while (isPlaying && time < triggerTime)` exits when `isPlaying` is false, so `SwapSourcesAndPlay` restarts the track from 0:00.

### Change
Only the inner loop changes, to `while (_bgmSourceMain.time < triggerTime) yield return null;`. No time tracking and no resume logic.

### Known limitation (accepted)
If the source stops and `time` resets to 0 (for example a hitch longer than `fadeDuration`), the routine waits until the next `TransitionToBGM`.

### Checklist
- [x] Review & approve plan
- [x] Read `AudioManagerTests.cs`
- [x] Edit the inner while condition (about 1 line)
- [x] Add test: the source is stopped before `triggerTime` and the track does not restart
- [x] Run `tests-run` + `console-get-logs`
- [ ] Playtest: alt-tab 10+ s, and loop at track end
- [x] Update `SYSTEM_SPEC_AUDIO.md`

### Review
- Changed the inner loop condition in `AudioManager.MusicLoopRoutine` (`Assets/Scripts/Audio/AudioManager.cs`) from `while (_bgmSourceMain.isPlaying && _bgmSourceMain.time < triggerTime)` to `while (_bgmSourceMain.time < triggerTime)` so transient drops in `isPlaying` do not exit early into `SwapSourcesAndPlay(clip)`.
- Added `MusicLoopRoutine_SourceStoppedBeforeTriggerTime_DoesNotRestartOrSwap` in `Assets/Editor/Tests/AudioManagerTests.cs` verifying that pausing/stopping `_bgmSourceMain` before `triggerTime` does not swap or restart the track, and advancing `time` past `triggerTime` triggers the loop swap as expected.
- Verified via `tests-run` (`EditMode`, `AudioManagerTests`): 8/8 tests passed (0 failed).
- Updated `Docs/specs/systems/SYSTEM_SPEC_AUDIO.md` with the `time`-based loop trigger guard, `runInBackground: 1` dependency, and acceptance test coverage.
