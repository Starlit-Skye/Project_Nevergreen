# Reduce Marionette Skill Heal Percent to 70%

## Tasks
- [x] 1. Run EditMode tests to confirm everything is passing as a baseline.
- [x] 2. Run Unity Editor C# script via `script-execute` in dry-run mode to list all affected SkillData assets and their current vs. target heal percent values.
- [x] 3. Run the Unity Editor C# script in modify mode to update the `healPercent` values of those SkillData assets.
- [x] 4. Run `git diff` to verify the asset modifications and roundings.
- [x] 5. Run EditMode tests again to ensure no regressions.

## Review
- [x] Verify that modified heal percents match expected calculations.
- [x] Verify that tests compile and pass.
