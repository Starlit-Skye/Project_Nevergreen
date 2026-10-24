# Task: Stat System Clamping Refactor

## Summary
Refactor the combat stat system so that stats cannot be reduced below 0. Specifically:
- `attack`, `accuracy`, `critChance`, `defense`, `dodge`, `speed`, `bleedResist`, `blightResist`, `stunResist`, `debuffResist`, and `moveResist` are clamped at a minimum of 0.
- `maxHP` is clamped at a minimum of 1.
- In `StatBlockData.cs`, add `[Min(0)]` attribute to `attack`, `maxHP`, and `speed` to prevent entering negative numbers in base assets.
- If effective `maxHP` is reduced below `currentHP`, re-clamp `currentHP` to the new effective `maxHP`.
- Align spec formulas with the implemented codebase formula: `(base + flat) * (1 + percent)`.
- Update design specs and write comprehensive unit tests.

## Todo List

### 1. Base Data Hardening (`StatBlockData.cs`)
- [x] Add `[Min(0)]` attribute to `attack`, `maxHP`, and `speed` in `StatBlockData.cs`.

### 2. Stat Clamping in Runtime Math (`CombatCharacter.cs`)
- [x] In `GetModifiedStatValue`, clamp derived stat value at 0, or at 1 if `target == StatTarget.MaxHP`.
- [x] Add `ClampCurrentHPToEffectiveMax()` and invoke it on stat change (`AddStatus`, `RemoveStatus`, `TriggerStatsChanged`).
- [x] Update `ApplyHeal` to calculate missing HP using effective `maxHP` (`GetEffectiveStats().maxHP`) instead of `baseStats.maxHP`.

### 3. UI Alignment (`HPBar.cs`)
- [x] In `HPBar.cs`, update `UpdateStateConfig` to use `GetEffectiveStats().maxHP` instead of `baseStats.maxHP`.
- [x] In `HandleStatsChanged`, call `UpdateStateConfig()` before `Refresh()`.

### 4. Unit Tests (`BuffDebuffTests.cs`)
- [x] Test: Oversized debuffs on core percent stats (Attack, Speed, Defense, Accuracy, Dodge) clamp at 0.
- [x] Test: Oversized debuff on MaxHP clamps at 1.
- [x] Test: Oversized flat debuffs on flat stats (CritChance, 5 resistances) clamp at 0.
- [x] Test: Mixed buffs and debuffs evaluate additively and clamp at boundary (e.g. +50% and -150% -> 0).
- [x] Test: Debuff expiration properly restores stat back to base value.
- [x] Test: MaxHP debuff re-clamps `currentHP` to new effective `maxHP`.
- [x] Test: Pile innate `moveResist` bonus (+300) applies on top of clamped base.
- [x] Run full test suite to guarantee 0 regressions.

### 5. Documentation & Specification Updates
- [x] `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_BUFF_DEBUFF.md`:
  - Update formula to `(base + flat) * (1 + percent)` and add floor clamping rules (`max(0, ...)` / `max(1, maxHP)`).
  - Update Failure Modes ("Stat Underflow" resolved by clamping).
  - Add tuning variables for stat floor.
  - Update acceptance tests and last verified date.
- [x] `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md`:
  - Update effective stat formula section with floor clamping.
  - Add edge case notes for 0-clamping and maxHP 1-floor.
- [x] `Docs/specs/systems/SYSTEM_SPEC_MARIONETTE_TEMPLATE.md`:
  - Add invariant notes regarding base stats >= 0 and runtime clamping.
- [x] `Docs/specs/systems/SYSTEM_SPEC_ENEMY_TEMPLATE.md`:
  - Add invariant notes regarding base stats >= 0 and runtime clamping.
- [x] `Docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md`:
  - Add notes regarding stat constraints and runtime flooring.

### 6. Review & Verification
- [x] Run all EditMode unit tests.
- [x] Verify test results and document them in `tasks/todo.md`.

## Review & Verification Results
- **Unit Tests**:
  - Updated `Debuff_Resistance_CanGoBelowZero` -> `Debuff_Resistance_ClampedAtZero` to verify that resistances clamp at 0 instead of allowing negative values.
  - Added 8 new unit tests in `BuffDebuffTests.cs`:
    - `StatClamping_CorePercentStats_ClampedAtZero`: Oversized debuffs on Attack, Speed, Defense, Accuracy, Dodge clamp at 0.
    - `StatClamping_MaxHP_PercentDebuff_ClampedAtOne`: Oversized percent debuffs on MaxHP clamp at 1.
    - `StatClamping_MaxHP_FlatDebuff_ClampedAtOne`: Oversized flat debuffs on MaxHP clamp at 1.
    - `StatClamping_FlatStats_ClampedAtZero`: Oversized flat debuffs on CritChance, BleedResist, BlightResist, StunResist, DebuffResist, MoveResist clamp at 0.
    - `StatClamping_MixedBuffAndDebuff_EvaluatesAdditivelyAndClamps`: Mixed buffs and debuffs calculate additively and clamp at boundary.
    - `StatClamping_DebuffExpiration_RestoresBaseValue`: Expiration cleanly restores base stats.
    - `StatClamping_MaxHPDebuff_ReclampsCurrentHP`: `currentHP` is automatically re-clamped when effective `maxHP` drops, and healing cannot exceed the new effective max HP.
    - `StatClamping_PileMoveResistBonus_AppliedOnTopOfClampedBase`: Pile innate move resistance (+300) applies on top of clamped base.
- **Suite Execution**:
  - Full EditMode test run: 615 executed, 615 passed, 0 failed, 0 skipped.
- **Files Modified**:
  - `Assets/Scripts/Data/StatBlockData.cs` (`[Min(0)]` added to `attack`, `maxHP`, `speed`)
  - `Assets/Scripts/Combat/CombatCharacter.cs` (clamped stats at 0 or 1 for maxHP in `GetModifiedStatValue`, added `ClampCurrentHPToEffectiveMax`, updated `ApplyHeal` to use effective `maxHP`)
  - `Assets/Scripts/Prototype/HPBar.cs` (updated `_currentMaxHP` to use effective `maxHP` and refresh on stats changed)
  - `Assets/Editor/Tests/BuffDebuffTests.cs` (updated legacy test and added new clamping test suite)
  - `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_BUFF_DEBUFF.md`
  - `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md`
  - `Docs/specs/systems/SYSTEM_SPEC_MARIONETTE_TEMPLATE.md`
  - `Docs/specs/systems/SYSTEM_SPEC_ENEMY_TEMPLATE.md`
  - `Docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md`
