# Goal: Implement Status Effect Icons above HP Bars

## Tasks
- [x] 1. Define `StatusIconMapping` struct and configure mappings in `CombatConfig.cs`.
- [x] 2. Update `HPBar.cs` to dynamically instantiate and refresh status icons.
- [x] 3. Update `CombatSceneBuilder.cs` to setup `StatusIconContainer` child and template prefab link on `HPBar` prefab.
- [x] 4. Create default `StatusIcon` prefab under `Assets/Prefabs/UI/`.
- [x] 5. Write unit tests in `StatusIconTests.cs` to cover icon resolution, application, and expiration.
- [x] 6. Run all tests to ensure correctness.
- [x] 7. Document results in `tasks/todo.md`.

## Results
- **Implementation**: The feature is fully implemented. The HPBar correctly instantiates unique, pooled icons based on active `StatusEffectInstance`s.
- **Design Configuration**: `CombatConfig` holds mappings for simple Types and stat-specific Types.
- **Test Results**: All 392 EditMode unit tests (including 3 new icon tests) have successfully passed.
