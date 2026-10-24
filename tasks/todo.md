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

---

# Task: Ignore Stun Applied To Already-Stunned Character

## Summary
Currently a successful Stun on an already-stunned target appends a second `StatusEffectInstance`
(no refresh, concurrent ticking, stacked +300 recovery buffs). New behavior: if the target already has
a non-expired Stun, the new application is **completely ignored**: no new instance, no duration
refresh/extension, no extra recovery buff, no `OnStatusApplied` event, no "afflicted" log.
PLAN ONLY - no code or spec has been modified yet.

## Design Decisions (proposed)
- **Central guard in `CombatCharacter.AddStatus`** (single choke point; all effect paths funnel through it:
  `StatusEffect`, `SelfStatusEffect`, `AdjacentAllyStatusEffect`, `ApplyStatusToGuardianEffect`,
  `StatusEffectOnSpawn`, `LivingFragmentsStatusEffect`). Prevents future callers from re-introducing stacking.
  Change signature `void` -> `bool` (true = added) so callers can react; existing call sites compile unchanged.
- **Early-out in `StatusEffect.Execute` (before the RNG roll)** so an ignored Stun does not consume an RNG value,
  and does not fire `TriggerStatusApplied` / log "afflicted". Same pre-check for the other effect classes that
  can apply Stun (Self/AdjacentAlly/Guardian) via a shared helper.
- **"Already stunned" predicate**: new `CombatCharacter.HasActiveStatus(StatusType)` =
  `statusEffects.Any(s => s.type == type && !s.IsExpired)`. Do not rely on the `isStunned` flag (can be stale,
  e.g. duration-0 stun).
- **Scope**: Stun only (type-specific, e.g. a `StatusType`-based "non-stackable" set so other types like Bleed
  keep stacking). Guard has its own Last-In-Wins logic and is untouched.
- **Existing instance wins**: remaining duration of the existing stun is unchanged (longer incoming stun does NOT extend it).

## Todo List

### 1. Pre-flight
- [x] Re-read `tasks/lessons.md` for relevant patterns.
- [x] Confirm with user: user approved proceeding with plan.
- [x] Grep tests that add 2 Stun instances to the same character (`BuffDebuffTests` ~L614/652, `GuardTests`
      ~L88/207, `CumulativeDamageTests` ~L233 duration-0 stun) and confirm none rely on stacking.

### 2. Runtime (`CombatCharacter.cs`)
- [x] Add `HasActiveStatus(StatusType type)` helper.
- [x] Add non-stackable guard at top of `AddStatus`: if `status.type == StatusType.Stun && HasActiveStatus(Stun)`
      -> return false without `OnAdded`, flag change, clamp or `OnStatsChanged`.
- [x] Make `AddStatus` return `bool`.

### 3. Effect call sites
- [x] `StatusEffect.Execute`: after hit check, early return if `statusType == Stun` and target already has active Stun
      (before chance/resistance/RNG).
- [x] Apply same early-out in `SelfStatusEffect`, `AdjacentAllyStatusEffect`, `ApplyStatusToGuardianEffect`.
- [x] Ensure "afflicted" log and `TriggerStatusApplied(..., applied)` use the AddStatus return value.

### 4. Tests (`StunTests.cs`)
- [x] `Stun_AppliedToAlreadyStunned_DirectAddStatus_ReturnsFalseAndCreatesNoInstance`: count of Stun instances stays 1.
- [x] `Stun_AppliedToAlreadyStunned_ViaSkillEffect_IsCompletelyIgnored`: no OnStatusApplied event and no RNG consumed.
- [x] `Stun_AppliedToAlreadyStunned_DoesNotExtendDuration`: existing 1-turn stun + incoming 3-turn -> still 1 remaining.
- [x] `Stun_AppliedToAlreadyStunned_YieldsSingleRecoveryBuffOnExpiry`: after expiry exactly one `Buff(StunResist,+300,1)`.
- [x] `Stun_AppliedAfterPreviousExpired_AppliesNormally`: re-stun after recovery still works.
- [x] `NonStunStatus_LikeBleed_StillStacks`: Bleed still stacks.
- [x] Run full EditMode suite (baseline: 615 passed; now 621 passed).

### 5. Docs
- [x] `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_STUN.md`: updated State Model, Formulas, Edge Cases, Event Hooks, Acceptance Tests; removed stacking items from Missing Evidence.
- [x] `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md`: added note under stun edge cases.
- [x] `MECHANIC_SPEC_STATUS_GUARD.md`: verified Guard breakage on first Stun unchanged.

### 6. Verification
- [x] All new + existing tests green (621/621 passed).
- [x] Verified zero console compilation errors.

## Review & Verification Results
- **Unit Tests**:
  - Added 6 new unit tests in `Assets/Editor/Tests/StunTests.cs`:
    - `Stun_AppliedToAlreadyStunned_DirectAddStatus_ReturnsFalseAndCreatesNoInstance`
    - `Stun_AppliedToAlreadyStunned_ViaSkillEffect_IsCompletelyIgnored`
    - `Stun_AppliedToAlreadyStunned_DoesNotExtendDuration`
    - `Stun_AppliedToAlreadyStunned_YieldsSingleRecoveryBuffOnExpiry`
    - `Stun_AppliedAfterPreviousExpired_AppliesNormally`
    - `NonStunStatus_LikeBleed_StillStacks`
  - Full suite EditMode tests: **621 passed, 0 failed, 0 skipped**.
- **Files Modified**:
  - `Assets/Scripts/Combat/CombatCharacter.cs` (`HasActiveStatus(StatusType)`, `AddStatus` returns `bool` and blocks duplicate Stun, `RemoveStatus` uses `HasActiveStatus`)
  - `Assets/Scripts/Combat/StatusProcessor.cs` (`HasActiveStatus(StatusType.Stun)` used in `TickDurations`)
  - `Assets/Scripts/Combat/Effects/StatusEffect.cs` (early return before resistance/RNG check if target already has active Stun)
  - `Assets/Scripts/Combat/Effects/SelfStatusEffect.cs` (early return if user already has active Stun)
  - `Assets/Scripts/Combat/Effects/AdjacentAllyStatusEffect.cs` (early return if target ally already has active Stun)
  - `Assets/Scripts/Combat/Effects/ApplyStatusToGuardianEffect.cs` (early return if guardian already has active Stun)
  - `Assets/Editor/Tests/StunTests.cs` (6 new unit tests)
  - `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_STUN.md` (updated spec)
  - `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` (updated Combat Core spec)

