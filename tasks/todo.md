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

