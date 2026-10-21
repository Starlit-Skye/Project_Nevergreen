# Task: Exhaustion Mechanic

## Summary
The run keeps an **exhaustion counter**. It goes up by 1 at the end of each combat round and stops at a per-tier limit. Once the counter reaches the limit, **player-team** characters can't heal (skills, Restore HOT, Heal Guardian). Entering the Heal Room resets the counter to 0. Otherwise it carries over from one battle to the next.

Also: no heal path may ever lower HP, whatever modifiers apply (Heal Received Reduction, negative scaling, overheal math).

## Confirmed Design Decisions
| Topic | Decision |
| --- | --- |
| Increment timing | End of each **round** (`BattleSystem.OnRoundEnded`). If the battle ends partway through a round, that round does not count. |
| Cap | `ExhaustionRound = min(ExhaustionRound + 1, ExhaustionLimit)` |
| Exhausted | `ExhaustionLimit > 0 && ExhaustionRound >= ExhaustionLimit`. A limit of `<= 0` turns the mechanic off (safe default). |
| Scope of block | Player team only. Enemies heal as normal. |
| Life Link revive | **Not affected**. A revive is not counted as healing and still restores HP. |
| Elite rooms | Use the limit for the progression tier (`GetEncounterTierForRoom(RoomProgression)`), not the `Elite` override. |
| Trivial + Early | Share one config field (`earlyExhaustionLimit`). There's no Heal Room between them, so the counter carries over. |
| Reset | `ExhaustionRound = 0` when the player enters the Heal Room (`RunSessionManager.IsHealRoom`). |
| UI | Not in scope for now. |

## Architecture
```
CombatConfig.GetExhaustionLimit(tier)  ──►  RunSessionManager.ExhaustionLimit (refreshed on combat scene load)
BattleSystem.OnRoundEnded ──► RunSessionManager.AdvanceExhaustion()  ──► ExhaustionRound (clamped)
CombatCharacter.Heal(amount) ──► if (IsPlayerTeam && RunSessionManager.IsExhausted) heal = 0
                              ──► heal always clamped to [0, missingHP]  (never negative)
CombatCharacter.ReviveFromDowned ──► ApplyHeal (bypasses exhaustion gate, still clamped)
```
All heal paths already go through `CombatCharacter.Heal`: `HealEffect`, `HealGuardianEffect`, `StatusProcessor` Restore, and `ReviveFromDowned`. Putting the gate and the clamp in that one method covers every current and future heal source, so nothing has to be added to each effect.

## Todo List

### 1. Config — `Assets/Scripts/Data/CombatConfig.cs` + `Assets/Data/CombatConfig.asset`
- [x] Add a `[Header("Exhaustion")]` section with `earlyExhaustionLimit` (Trivial + EarlyGame), `midExhaustionLimit`, `lateExhaustionLimit` and `bossExhaustionLimit` (int, tooltips, `Min(0)`).
- [x] Add `int GetExhaustionLimit(EnemyEncounterTier tier)`: Trivial/EarlyGame → early, MidGame → mid, LateGame → late, Boss → boss, anything else (Elite) → 0 (off; callers should never pass it).
- [x] Set the values in `CombatConfig.asset`: `earlyExhaustionLimit = 8`, `midExhaustionLimit = 8`, `lateExhaustionLimit = 10`, `bossExhaustionLimit = 12`.

### 2. Run state — `Assets/Scripts/Data/RunSessionManager.cs`
- [x] Add properties `ExhaustionRound` (int), `ExhaustionLimit` (int), `PreCombatExhaustionRound` (int, a pre-combat snapshot like `preCombatHP`) and `IsExhausted` (computed).
- [x] Add `AdvanceExhaustion()`: increment and clamp; does nothing if the limit is `<= 0`.
- [x] Add `RefreshExhaustionLimit()`: `config.GetExhaustionLimit(config.GetEncounterTierForRoom(RoomProgression))`, then clamp `ExhaustionRound` to the new limit.
- [x] `OnSceneLoaded("CombatPrototype")`: after the progression logic, (a) if `IsHealRoom(CurrentRoomData)` set `ExhaustionRound = 0`, (b) call `RefreshExhaustionLimit()`, (c) snapshot `PreCombatExhaustionRound = ExhaustionRound` next to the `preCombatHP` snapshot. Steps (a) and (b) are idempotent, so they're safe on the resume path too.
- [x] `CompleteRoom()`: sync `PreCombatExhaustionRound = ExhaustionRound`, the same way HP is synced.
- [x] `SubscribeToBattle()`: also subscribe `OnRoundEnded += HandleRoundEnded` (calls `AdvanceExhaustion`). Unsubscribe in `OnBattleEnded`, in `Clear()`, and when a stale subscription is replaced.
- [x] Reset all exhaustion fields in `Initialize()` and `Clear()`.

### 3. Healing gate + never-negative guarantee — `Assets/Scripts/Combat/CombatCharacter.cs`
- [x] Split `Heal` into:
  - `public int Heal(int amount)`: if `IsPlayerTeam && RunSessionManager.IsExhausted`, set amount to 0, then call `ApplyHeal`.
  - `private int ApplyHeal(int amount)`: `actual = Mathf.Clamp(amount, 0, Mathf.Max(0, maxHP - currentHP))`. Fires `OnHealed(this, actual)` (fires with 0, as it does today at full HP) and returns `actual`.
- [x] `ReviveFromDowned` calls `ApplyHeal` (gets past the exhaustion gate, still clamped).
- [x] Bug fixed along the way: today `Heal(-5)` **lowers** HP (`Mathf.Min(-5, missing)` = -5). The clamp prevents that.

### 4. Heal call sites report the actual amount
- [x] `HealEffect` / `HealGuardianEffect`: `context.calculatedValue += target.Heal(healAmount)`, so the combat log shows "heals for 0 HP" while exhausted instead of the raw number. *(Side effect: the log now shows actual HP restored, overheal excluded.)*
- [x] `StatusProcessor.ProcessPeriodicEffects` (Restore): pass the value returned by `Heal` into `TriggerPeriodicEffectApplied`, so a blocked Restore doesn't show a fake heal popup.

### 5. Save persistence — `Assets/Scripts/Data/SaveManager.cs`
- [x] Add `exhaustionRound` (int) to `SaveDataDTO`. `SaveRun` writes `PreCombatExhaustionRound`, so a mid-battle quit restores the pre-combat value (the same rule as HP: a resumed combat restarts from the beginning).
- [x] `LoadRun` restores both `ExhaustionRound` and `PreCombatExhaustionRound` from the DTO. `ExhaustionLimit` is derived and gets recomputed in `OnSceneLoaded`. Old saves without the field default to 0.

### 6. Tests — new `Assets/Editor/Tests/ExhaustionTests.cs` (teardown: `RunSessionManager.ClearAll()`, `GameDatabase.SetInstanceForTesting`, `IsPersistent` guard)
- [x] `AdvanceExhaustion_IncrementsAndClampsAtLimit`
- [x] `AdvanceExhaustion_NoOp_WhenLimitZero` / `IsExhausted_False_WhenLimitZero`
- [x] `GetExhaustionLimit_TrivialAndEarly_ShareLimit`, `GetExhaustionLimit_MapsMidLateBoss`
- [x] `OnSceneLoaded_HealRoom_ResetsExhaustion`
- [x] `OnSceneLoaded_CombatRoom_PersistsExhaustionAcrossBattles`
- [x] `OnSceneLoaded_TrivialToEarly_DoesNotReset`
- [x] `OnSceneLoaded_EliteRoom_UsesProgressionTierLimit`
- [x] `Heal_PlayerExhausted_HealsZero`, `Heal_EnemyExhausted_StillHeals`
- [x] `RestoreHOT_PlayerExhausted_NoHeal`
- [x] `HealGuardianEffect_PlayerExhausted_NoHeal`
- [x] `ReviveFromDowned_PlayerExhausted_StillRestoresHP`
- [x] `Heal_NegativeAmount_NeverReducesHP` (regression)
- [x] `HealEffect_HealReductionOver100_NeverDamages`
- [x] `SaveRun_MidBattle_SavesPreCombatExhaustionRound`, `LoadRun_RestoresExhaustionRound`
- [x] Run the full EditMode suite (`tests-run`). No regressions (557 tests passed last time).

### 7. Documentation
- [x] New `Docs/specs/mechanics/MECHANIC_SPEC_EXHAUSTION.md` (mechanic template: state model, timing, tuning table, edge cases, tests).
- [x] Update `SYSTEM_SPEC_RUN_SESSION_MANAGER.md` (new fields, round subscription, heal-room reset), `SYSTEM_SPEC_SAVE_GAME_PERSISTENCE.md` (`exhaustionRound` DTO field), `MECHANIC_SPEC_STATUS_HEAL_REDUCTION.md` (central clamp in `CombatCharacter.Heal`) and `MECHANIC_SPEC_COMBAT_CORE.md` (round-end exhaustion step).

## Confirmed Parameter Decisions
1. **Limit values** for `CombatConfig.asset`: `early = 8`, `mid = 8`, `late = 10`, `boss = 12`.
2. **Debug full-heal** (`EditorDebugUtility`): leave as-is (respects exhaustion blocking).

## Out of Scope (noted)
- `HealGuardianEffect` doesn't apply the `HealReceived_*` modifier, unlike `HealEffect`. That's an existing inconsistency, left unchanged.
- Exhaustion HUD indicator.

## Review & Verification Results
- **Unit Tests Added**:
  - `Assets/Editor/Tests/ExhaustionTests.cs`: 19 comprehensive unit tests covering counter/limit clamping, disabled mechanic (`limit <= 0`), `BattleSystem.OnRoundEnded` round advances, tier mappings (`Trivial`/`Early` sharing, `Mid`, `Late`, `Boss`), Heal Room resets, cross-battle persistence, Trivial->Early continuity, Elite room progression tier limit fallback, player-only heal blocking, enemy heal immunity, Restore HOT blocking, Heal Guardian blocking, Life Link revive bypass, negative heal clamping prevention, over-100% heal reduction safety, and pre-combat save/load persistence.
- **Suite Execution**:
  - All 19 tests in `ExhaustionTests` passed.
  - Ran the full project EditMode suite (`tests-run`): 581 tests executed, 581 passed (0 failed, 0 skipped).
- **Files Modified/Created**:
  - `Assets/Scripts/Data/CombatConfig.cs`
  - `Assets/Data/CombatConfig.asset`
  - `Assets/Scripts/Data/RunSessionManager.cs`
  - `Assets/Scripts/Combat/CombatCharacter.cs`
  - `Assets/Scripts/Combat/Effects/HealEffect.cs`
  - `Assets/Scripts/Combat/Effects/HealGuardianEffect.cs`
  - `Assets/Scripts/Combat/StatusProcessor.cs`
  - `Assets/Scripts/Data/SaveManager.cs`
  - `Assets/Editor/Tests/ExhaustionTests.cs` (new)
  - `Docs/specs/mechanics/MECHANIC_SPEC_EXHAUSTION.md` (new)
  - `Docs/specs/systems/SYSTEM_SPEC_RUN_SESSION_MANAGER.md`
  - `Docs/specs/systems/SYSTEM_SPEC_SAVE_GAME_PERSISTENCE.md`
  - `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_HEAL_REDUCTION.md`
  - `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md`

# Task: Exhaustion Display UI

## Summary
Implement a simple UI script `ExhaustionDisplayUI` (modeled after `EconomyDisplayUI.cs`) that updates a TextMeshPro component to display exhaustion status in the format `"Exhaustion: exhaustionRound/exhaustionLimit"`. Add `OnExhaustionChanged` event to `RunSessionManager` so the UI automatically updates whenever exhaustion round or limit changes.

## Todo List
- [x] 1. Add `OnExhaustionChanged` event to `RunSessionManager.cs` and back `ExhaustionRound` & `ExhaustionLimit` with backing fields that invoke the event on change <!-- id: 1 -->
- [x] 2. Create `Assets/Scripts/UI/ExhaustionDisplayUI.cs` following `EconomyDisplayUI.cs` pattern <!-- id: 2 -->
- [x] 3. Add unit tests for `OnExhaustionChanged` and `ExhaustionDisplayUI` in `Assets/Editor/Tests/ExhaustionTests.cs` <!-- id: 3 -->
- [x] 4. Run tests and verify with test runner MCP tool (`tests-run`) <!-- id: 4 -->
- [x] 5. Update specification documents (`MECHANIC_SPEC_EXHAUSTION.md`, `SYSTEM_SPEC_RUN_SESSION_MANAGER.md`) <!-- id: 5 -->
- [x] 6. Document results and review in `tasks/todo.md` <!-- id: 6 -->

## Review & Verification Results (Exhaustion Display UI)
- **Unit Tests Added**:
  - `OnExhaustionChanged_FiresOnAdvance`: verifies `RunSessionManager.AdvanceExhaustion()` fires `OnExhaustionChanged`.
  - `OnExhaustionChanged_FiresOnLimitChange`: verifies mutating `RunSessionManager.ExhaustionLimit` fires `OnExhaustionChanged`.
  - `OnExhaustionChanged_DoesNotFire_WhenValueUnchanged`: verifies assigning identical values suppresses duplicate events.
  - `ExhaustionDisplayUI_ShowsFormattedText`: verifies formatting `"Exhaustion: {exhaustionRound}/{exhaustionLimit}"` dynamically on round advance.
  - `ExhaustionDisplayUI_UnsubscribesOnDisable`: verifies `OnDisable` unhooks listener and re-enabling refreshes text.
- **Suite Execution**:
  - `ExhaustionTests`: 24/24 passed (0 failed, 0 skipped).
  - Full EditMode test suite: 586/586 passed (0 failed, 0 skipped).
- **Files Modified/Created**:
  - `Assets/Scripts/Data/RunSessionManager.cs`: added `public static event System.Action OnExhaustionChanged`, backing fields `_exhaustionRound` and `_exhaustionLimit`, property change notifications, and `OnExhaustionChanged = null` reset in `ClearAll()`.
  - `Assets/Scripts/UI/ExhaustionDisplayUI.cs`: created component following `EconomyDisplayUI.cs` lifecycle and formatting.
  - `Assets/Editor/Tests/ExhaustionTests.cs`: added 5 tests for events and UI component with reflection lifecycle invocation.
  - `Docs/specs/mechanics/MECHANIC_SPEC_EXHAUSTION.md`: updated scope, source of truth, event hooks, and acceptance test list.
  - `Docs/specs/systems/SYSTEM_SPEC_RUN_SESSION_MANAGER.md`: documented `OnExhaustionChanged` in scope, source of truth, responsibilities, event contracts, and acceptance tests.

# Task: Debug Utility Exhaustion Features

## Summary
Extend `EditorDebugUtility.cs` to add:
1. Comma (`,`) key to decrease `RunSessionManager.ExhaustionRound` by 1 (clamped to 0).
2. Period (`.`) key to increase `RunSessionManager.ExhaustionRound` by 1.
3. Update `H` key heal utility: check if player team is exhausted (`RunSessionManager.IsExhausted`), if yes, set `IsExhausted` to false (temporarily set `ExhaustionRound = 0`), heal players, and restore `ExhaustionRound` to its original value in a `finally` block so `IsExhausted` becomes true again.
Constraint: Do not modify any code outside of `EditorDebugUtility.cs`.

## Todo List
- [x] 1. Add `,` and `.` key handlers in `EditorDebugUtility.cs` to decrease/increase `RunSessionManager.ExhaustionRound` <!-- id: 1 -->
- [x] 2. Update `H` key handler in `EditorDebugUtility.cs` to bypass exhaustion during heal when exhausted and restore state afterwards <!-- id: 2 -->
- [x] 3. Verify compilation and run test suites with `tests-run` <!-- id: 3 -->
- [x] 4. Verify behavioral execution via `script-execute` simulation <!-- id: 4 -->
- [x] 5. Complete review and results section in `tasks/todo.md` <!-- id: 5 -->

## Review & Verification Results
- **Changes Implemented**:
  - `Assets/Scripts/Prototype/EditorDebugUtility.cs`:
    - Added `Keyboard.current.commaKey.wasPressedThisFrame`: decreases `RunSessionManager.ExhaustionRound` by 1 clamped to 0 via `Mathf.Max(0, ExhaustionRound - 1)`.
    - Added `Keyboard.current.periodKey.wasPressedThisFrame`: increases `RunSessionManager.ExhaustionRound` by 1.
    - Updated `Keyboard.current.hKey.wasPressedThisFrame`: checks `bool wasExhausted = RunSessionManager.IsExhausted`. If true, temporarily sets `RunSessionManager.ExhaustionRound = 0` (causing `IsExhausted` to evaluate to false), executes the healing logic, and restores `RunSessionManager.ExhaustionRound` in a `finally` block so `IsExhausted` returns to true.
- **Verification**:
  - Verified logic execution using Roslyn `script-execute`:
    - Comma decrement from 5 to 4 and clamp at 0 verified.
    - Period increment from 4 to 5 verified.
    - Full heal of an in-combat exhausted player unit verified: `CombatCharacter.Heal` succeeded, restored character HP from 20 to 100, and restored `IsExhausted` back to true with `ExhaustionRound` preserved.
  - Ran full project EditMode test suite via `tests-run`: 586/586 passed (0 failed, 0 skipped).
  - Scope constraint honored: Zero code modified outside of `EditorDebugUtility.cs`.
