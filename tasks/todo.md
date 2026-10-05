# Implementation Plan: Flight Status Bug Fix

Full design: see `flight_removal_fix_plan.md` artifact.

## Decisions
- Create `StatusInstanceFactory` to centralize status creation.
- Include `Shuffle` in the factory since it takes `battleSystem` and `rng`.
- Keep the current behaviour for `AdjacentAllyStatusEffect` applying `Guard` (where targetAlly is the guardian).

## Code
- [x] Create `StatusInstanceFactory.cs`.
- [x] Refactor `StatusEffect.cs` to use factory.
- [x] Refactor `SelfStatusEffect.cs` to use factory.
- [x] Refactor `ApplyStatusToGuardianEffect.cs` to use factory.
- [x] Refactor `AdjacentAllyStatusEffect.cs` to use factory.

## Tests
- [x] Implement `StatusInstanceFactoryTests.cs` to verify correct instances and dependencies.
- [x] Rewrite `FlightTests.cs` to use actual skill execution path (via `BattleSystem`).
- [x] Fix `StatusIconTests.cs` to expect "turns" instead of "rounds".
- [x] Run EditMode tests and verify everything passes.

## Tooltip Update
- [x] Update `StatusTooltipDisplay.cs` Flight tooltip format to `"+{aggregateAmplitude} Dodge for {maxDuration} turns. Removed if hit."`.

## Review
- Status instantiation is now DRY.
- Flight bug is resolved as `SelfStatusEffect` now correctly creates `FlightStatusInstance` which subscribes to `BattleSystem.OnActionResolved`.
- Required passing actual `SkillData` instances with valid `skillId` fields to test harnesses to prevent `ArgumentNullException`s in status tracking.
- Flight tooltip string successfully updated and verified against test suite.
