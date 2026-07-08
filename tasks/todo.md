# Goal: Decouple Run Save and Meta Progression Save

## Tasks
- [x] 1. Split persistence into `run.dat` and `profile.dat` (SaveManager).
- [x] 2. Implement lazy-loading for `BossFormationChances` in `RunSessionManager`.
- [x] 3. Save profile meta-progression automatically upon boss selection.
- [x] 4. Update unit test setups (`SaveManagerTests`, `EnemyFormationSelectionTests`, `CombatSceneBootstrapFormationTests`, `RoomEffectTests`) to support dual test saves.
- [x] 5. Resolve assertion conflicts and run the test suite to verify success.

## Results
- Run and profile progression successfully separated.
- 388/388 EditMode tests passing successfully.
- Profile persistence verified to survive run clearings and initialization.
