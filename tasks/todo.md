# Implementation Plan: Unraveled Status Effect

Full design: see `unraveled_status_effect_plan.md` artifact.

## Decisions
- Add `StatusType.Unraveled` to `SkillData.cs`.
- Create `UnraveledStatusInstance` to handle `BattleSystem.OnBeforeDamageCalculationPerTarget`.
- Wire `StatusInstanceFactory.cs` to create `UnraveledStatusInstance`.
- Add UI tooltip in `StatusTooltipDisplay.cs` (`"+{aggregateAmplitude}% damage taken for {maxDuration} turns"`).

## Code
- [x] Add `StatusType.Unraveled` to `SkillData.cs`.
- [x] Create `UnraveledStatusInstance.cs`.
- [x] Update `StatusInstanceFactory.cs` to instantiate `UnraveledStatusInstance`.
- [x] Update `StatusTooltipDisplay.cs` for `Unraveled` tooltips.

## Tests
- [x] Create `UnraveledTests.cs` covering normal attacks, Riposte attacks, non-damaging skills, stacking, and duration expiration.
- [x] Run EditMode tests and verify clean pass.

## Review
- Successfully implemented `Unraveled` as an additive percentage damage multiplier.
- `UnraveledStatusInstance` leverages `BattleSystem.OnBeforeDamageCalculationPerTarget` efficiently.
- Test harness `CombatTestHelper` relies on `GameDatabase.Instance.CombatConfig` rather than independent `config` fields for deterministic combat rolls.
- EditMode tests fully cover Riposte interactions, stacking, and duration expiration.
