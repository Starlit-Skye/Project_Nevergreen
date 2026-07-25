# Battle Audio Fix Tasks

- [x] Setup Resources Assets
  - [x] Move/copy `AudioManager.prefab` to `Assets/Resources/AudioManager.prefab`
  - [x] Move/copy `GlobalAudioConfig.asset` to `Assets/Resources/GlobalAudioConfig.asset`
- [x] Refactor `AudioManager.cs`
  - [x] Implement lazy auto-instantiation in `AudioManager.Instance`
- [x] Refactor `BattleSystem.cs` & `CombatSceneBootstrap.cs`
  - [x] Ensure `BattleMusicController` is attached to `BattleSystem` on `Awake()`
  - [x] Ensure `deathSFX` plays even if character lacks an `Animator`
- [x] Unit Tests & Verification
  - [x] Update `AudioManagerTests.cs` and `BattleMusicControllerTests.cs`
  - [x] Run EditMode tests and verify all 424 tests pass

## Review & Results

- **Resources Auto-Loading**: `AudioManager.prefab` and `GlobalAudioConfig.asset` were added to `Assets/Resources/` so `AudioManager.Instance` can lazily instantiate itself at runtime if no `AudioManager` GameObject exists in the active scene.
- **Auto-Attachment of BattleMusicController**: `BattleSystem.cs` now auto-attaches `BattleMusicController` during `Awake()`, ensuring battle music transitions (`OnBattleStarted`) and victory jingles (`OnBattleEnded`) are always handled.
- **Animator-Independent Death SFX**: Character death sound effects (`deathSFX`) are now queued in `BattleSystem.cs` even if the character has no `Animator` component.
- **Unit Test Suite**: Added tests for lazy auto-instantiation and auto-attachment; verified that all 424 EditMode unit tests pass cleanly.
