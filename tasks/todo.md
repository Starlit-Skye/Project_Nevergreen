# Increase Late Game Monster HP by 10%

## Tasks
- [x] 1. Run EditMode tests to confirm everything is passing as a baseline.
- [x] 2. Run Unity Editor C# script via `script-execute` in dry-run mode to verify the target HP calculation for each of the 12 Late Game monster assets.
- [x] 3. Run the Unity Editor C# script in modify mode to update the `maxHP` field of the Late Game monster assets.
- [x] 4. Run `git diff` to verify the asset modifications and roundings.
- [x] 5. Update the Late Game section of `monster_stat_blocks.md` with the new HP values.
- [x] 6. Run EditMode tests again to ensure no regressions.

## Review
- [x] Verify that modified HPs match expected calculations.
- [x] Verify that tests compile and pass.
