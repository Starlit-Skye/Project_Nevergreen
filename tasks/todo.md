# Add Boss Tier to Enemy Encounters and Formation Database

## Tasks
- [x] 1. Modify `EnemyEncounterTier.cs` to append `Boss` tier.
- [x] 2. Modify `EnemyFormationDatabase.cs` to add `bossFormations` list and update `GetFormations` method.
- [x] 3. Modify `CombatConfig.cs` to handle default fallback for roomCount >= 8 to return `Boss` tier.
- [x] 4. Update unit tests in `EnemyFormationSelectionTests.cs` to mock and verify the Boss tier.
- [x] 5. Run tests to verify all changes function correctly and there are no regressions.

## Review
- [x] Verify that `EnemyEncounterTier.Boss` is placed at the end of the enum.
- [x] Verify that test suite passes for formation selection and bootstrapping.
