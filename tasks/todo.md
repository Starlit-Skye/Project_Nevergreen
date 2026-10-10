# Implementation Plan - Status Effect: Life Link

**Status:** ✅ **COMPLETED**

Define and implement the "Life Link" status effect mechanic for linked combatants (primarily enemies).

## Overview & Design Decisions (Confirmed with User)
1. **Downed State**: When a character with Life Link reaches 0 HP while other allies with Life Link are alive (>0 HP), it does not die or become a pile. It enters `LifeState.Downed` (cannot act, immune to damage, other non-LifeLink statuses cleared).
2. **Targeting**: Downed characters cannot be targeted by single-target skills, but occupy an AOE slot (absorbing propagation budget) without taking damage or effects.
3. **Round-End Revive**: At round end, any downed character with Life Link revives and heals for a configurable % of max HP (stored in `amplitude`, e.g. 30 = 30% max HP).
4. **Link Collapse**: When all linked characters reach 0 HP (or if the last standing linked ally's status expires), the link collapses and all downed characters execute normal death flow (`LifeState.Dying` -> `OnDefeated` as non-critical death, becoming Piles if `leavesPileOnDeath == true`).

---

## Proposed Changes

### 1. Core State Model & Enums
- **`Assets/Scripts/Data/SkillData.cs`**:
  - Add `LifeLink` to `StatusType` enum.
- **`Assets/Scripts/Combat/CombatCharacter.cs`**:
  - Add `LifeState.Downed` to `LifeState` enum.
  - Add `public bool IsDowned => state == LifeState.Downed;`.
  - In `TakeDamage()`:
    - Early return if `state == LifeState.Downed`.
    - When `currentHP <= 0` and `state != LifeState.Pile`, check if any active status intercepts defeat via `TryInterceptDefeat(isCritical)`.
    - If intercepted, skip transitioning to `LifeState.Dying` and skip `OnDefeated`.
  - In `Heal()`:
    - Early return if `state == LifeState.Downed` (normal heals cannot target or revive downed units).
  - Add helper methods:
    - `EnterDownedState()`: sets `currentHP = 0`, sets `state = LifeState.Downed`, clears non-LifeLink status effects, triggers `OnStateChanged` and `OnStatsChanged`.
    - `ReviveFromDowned(int healAmount)`: sets `currentHP = healAmount`, sets `state = LifeState.Alive`, triggers `OnHealed`, `OnStateChanged`, and `OnStatsChanged`.

### 2. Extensible Status Interception & Life Link Status Class
- **`Assets/Scripts/Combat/StatusEffectInstance.cs`**:
  - Add `public virtual bool TryInterceptDefeat(bool isCritical) => false;`.
- **`Assets/Scripts/Combat/Effects/LifeLinkStatusInstance.cs`** (New File):
  - Extends `StatusEffectInstance`.
  - Holds reference to `BattleSystem` (with lazy fallback lookup).
  - In `TryInterceptDefeat(bool isCritical)`:
    - Query team for other allies with active `LifeLinkStatusInstance` where `currentHP > 0` and `state == LifeState.Alive`.
    - If one or more exist: Host enters Downed state via `Host.EnterDownedState()`, return `true`.
    - If none exist: Link collapsed! Trigger collapse on any existing Downed linked allies (they transition to `Dying` and invoke `OnDefeated(c, false)`), then return `false` so the host dies normally.
  - In `OnRemoved()`:
    - If duration expires on the last standing linked ally, check if any remaining allies are in `Downed` state. If no standing linked allies remain, trigger collapse on all downed allies.
  - Subscribe to `BattleSystem.OnRoundEnded`:
    - At round end, if `Host.state == LifeState.Downed`, calculate heal amount: `Mathf.Max(1, Mathf.RoundToInt(Host.baseStats.maxHP * (amplitude / 100f)))`.
    - Call `Host.ReviveFromDowned(healAmount)`.

### 3. Targeting & Turn Flow Adjustments
- **`Assets/Scripts/Combat/TargetResolver.cs`**:
  - In `GetAOETargets`: Include `c.state == LifeState.Downed` in `sortedTeam` so downed units absorb an AOE slot.
  - In `IsValidReceiver`: Downed characters return `false`, preventing single-target selection.
- **`Assets/Scripts/Combat/SkillExecutor.cs`**:
  - In target iteration loop: Allow `target.state == LifeState.Downed` to be included so AOE propagation succeeds, but guard damage flinch animation so downed characters do not flinch.
- **`Assets/Scripts/Combat/TurnOrderBuilder.cs` & `BattleSystem.cs`**:
  - Already filters `c => c.IsAlive`, so downed characters naturally skip turn generation and turn execution.

### 4. Authoring, Factory & UI
- **`Assets/Scripts/Combat/Effects/StatusInstanceFactory.cs`**:
  - Add `case StatusType.LifeLink`: instantiate `LifeLinkStatusInstance(context.battleSystem, amplitude, duration)`.
- **`Assets/Scripts/Combat/StatusEffectOnSpawn.cs`**:
  - Add branch for `statusType == StatusType.LifeLink` creating `LifeLinkStatusInstance`.
- **`Assets/Scripts/Editor/StatusEffectOnSpawnEditor.cs`**:
  - Add branch for `StatusType.LifeLink`: show `duration`, show `amplitude` (with descriptive tooltip for Revive Heal %), hide `targetStat` and `livingFragmentPrefabs`.
- **`Assets/Scripts/UI/StatusTooltipDisplay.cs`**:
  - Add formatting for `StatusType.LifeLink`.
- **`Assets/Scripts/Prototype/HPBar.cs`**:
  - Ensure HP bar remains active when `state == LifeState.Downed` (`gameObject.SetActive(_target.IsAlive || _target.IsPile || _target.state == LifeState.Downed)`).

### 5. Documentation
- Create **`Docs/specs/mechanics/MECHANIC_SPEC_STATUS_LIFE_LINK.md`** following the team's standard specification format.

---

## Verification Plan

### Automated Unit Tests (`Assets/Editor/Tests/LifeLinkTests.cs`)
1. **Downed Interception**: When a Life-Linked character reaches 0 HP while another linked ally has >0 HP, state becomes `LifeState.Downed` and `currentHP == 0`.
2. **Damage Immunity**: Downed character takes 0 damage and remains in `Downed` state if hit.
3. **Turn Skipping**: Downed character is omitted from turn order and skips turn if already scheduled.
4. **Targeting Rules**:
   - Downed character cannot be chosen as a primary single-target.
   - Downed character takes up an AOE slot in `GetAOETargets` without taking damage.
5. **Round-End Revive**: At end of round, downed character revives to `LifeState.Alive` with `maxHP * (amplitude / 100f)` health.
6. **Total Link Collapse (0 HP)**: When the last standing linked ally reaches 0 HP, all previously downed linked allies transition to `Dying` / `OnDefeated` and form Piles (if `leavesPileOnDeath` is true).
7. **Duration Expiry Collapse**: When the last standing linked ally's Life Link duration ticks to 0 while others are downed, the downed allies collapse and die.
