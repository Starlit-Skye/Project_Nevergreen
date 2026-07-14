# Trait Tooltip Formatting Implementation Plan

- [x] Modify `TraitEffectStrategy.cs` to add virtual `GetTooltipDescription`
- [x] Implement overrides in:
  - [x] `LowHpStatModifierTraitStrategy.cs`
  - [x] `StatModifierTraitStrategy.cs`
  - [x] `FirstRoundStatModifierTraitStrategy.cs`
  - [x] `RankStatModifierTraitStrategy.cs`
  - [x] `HealReceivedBonusTraitStrategy.cs`
  - [x] `RankDamageBonusTraitStrategy.cs`
  - [x] `HealOutputBonusTraitStrategy.cs`
  - [x] `StatusApplicationBonusTraitStrategy.cs`
- [x] Update `TraitTooltipDisplay.cs` to extract and populate multi-line descriptions
- [x] Update `TraitTooltipTests.cs` with test cases verifying each strategy formatting
- [x] Execute tests to verify all functionality works correctly

## Review & Verification
- [x] Compile project and run EditMode tests.
- [x] Confirm exact tooltip formatting output aligns with specs.
