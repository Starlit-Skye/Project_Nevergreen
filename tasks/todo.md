# Implementation Plan: Switch Resistance Buffs/Debuffs to Flat Additive

Switch the combat system from treating resistance modifiers (Stun, Bleed, Blight, etc.) as multipliers to treating them as flat additive values (e.g., 10% base + 30% buff = 40% total).

## User Review Required

> [!IMPORTANT]
> This change assumes that ALL resistance-type stats (`BleedResist`, `BlightResist`, `StunResist`, `DebuffResist`, `MoveResist`) AND `CritChance` should be additive. Core stats like `Attack`, `Defense`, and `Speed` will remain multiplicative.

- [x] Confirm if all resistances and CritChance should be additive as planned.

## Proposed Changes

### Documentation Updates

#### [MECHANIC_SPEC_COMBAT_CORE.md](file:///d:/Nevergreen/Project_Nevergreen/Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md)
- Update "Formulas" section to distinguish between core stat multipliers and resistance flat additions.
- Update "Resistances" section to clarify stacking behavior.

#### [MECHANIC_SPEC_STATUS_BUFF_DEBUFF.md](file:///d:/Nevergreen/Project_Nevergreen/Docs/specs/mechanics/MECHANIC_SPEC_STATUS_BUFF_DEBUFF.md)
- Update "Formulas" and "Edge Cases" to document the dual stacking logic (Percentage vs. Flat).

### Core Logic Changes

#### [CombatCharacter.cs](file:///d:/Nevergreen/Project_Nevergreen/Assets/Scripts/Combat/CombatCharacter.cs)
- Modify `GetEffectiveStats()` to:
    - Identify resistance-type `StatTarget` entries.
    - Accumulate resistance modifiers in a `netFlat` dictionary.
    - Apply flat modifiers directly to the cloned stats after percentage calculations.
    - Ensure global caps (e.g., 300% for stun resist) are applied.

### Verification & Testing

#### [BuffDebuffTests.cs](file:///d:/Nevergreen/Project_Nevergreen/Assets/Editor/Tests/BuffDebuffTests.cs)
- Add `Buff_Resistance_AddsFlatValueToBase` test.
- Add `Debuff_Resistance_SubtractsFlatValueFromBase` test.
- Ensure existing core stat tests still pass.

#### [StunTests.cs](file:///d:/Nevergreen/Project_Nevergreen/Assets/Editor/Tests/StunTests.cs)
- Verify stun recovery bonus (+300%) behaves as a flat addition.

## Tasks

- [x] Update `MECHANIC_SPEC_COMBAT_CORE.md` <!-- id: 0 -->
- [x] Update `MECHANIC_SPEC_STATUS_BUFF_DEBUFF.md` <!-- id: 1 -->
- [x] Refactor `CombatCharacter.GetEffectiveStats` <!-- id: 2 -->
- [x] Add/Update tests in `BuffDebuffTests.cs` <!-- id: 3 -->
- [x] Verify `StunTests.cs` compatibility <!-- id: 4 -->
- [x] Run all combat tests to ensure no regressions <!-- id: 5 -->
