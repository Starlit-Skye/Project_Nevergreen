# Implementation Plan: Living Fragments Status Effect

Full design: see `living_fragments_status_effect_plan.md` artifact.

## Decisions
- Add `StatusType.LivingFragments` to `SkillData.cs`.
- Implement `LivingFragmentsStatusEffect` (`ISkillEffect`) for skill asset authoring with prefab list.
- Implement `LivingFragmentsStatusInstance` (`StatusEffectInstance`) listening to `host.OnStateChanged` / `host.OnDefeated` for `LifeState.Destroyed`.
- Execute 5-step Mid-Combat Summoning Pipeline placing summoned units into frontmost positions (`ExecuteMoveAndShift` to rank 1).
- Wire `StatusInstanceFactory.cs` and `StatusTooltipDisplay.cs`.

## Code
- [x] Add `StatusType.LivingFragments` to `SkillData.cs`.
- [x] Create `LivingFragmentsStatusEffect.cs`.
- [x] Create `LivingFragmentsStatusInstance.cs`.
- [x] Update `StatusInstanceFactory.cs` for `LivingFragments`.
- [x] Update `StatusTooltipDisplay.cs` for `LivingFragments` tooltip string.

## Tests
- [x] Create `LivingFragmentsTests.cs` covering host destruction summoning, frontmost rank positioning, max team capacity limit, and status expiration behavior.
- [x] Run EditMode tests and verify clean pass.

