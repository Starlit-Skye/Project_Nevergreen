# Mechanic: Exhaustion

Owner: Combat Engineering Team
Status: active
Last verified: 2026-10-19
Target build: Unity 6000.3.9f1 + Windows

## Purpose
The Exhaustion mechanic limits prolonged stalling and infinite sustain in combat by preventing player-team healing once the combat round count across battles reaches a tier-configured limit. Entering a Healing Room resets the exhaustion counter to zero.

## Scope
- In scope: Round-end exhaustion incrementing, per-tier exhaustion limits configured in `CombatConfig`, battle-to-battle exhaustion persistence, reset upon entering `RD_HealRoom`, player-team heal blocking when exhausted, Life Link revive bypass, non-negative heal guarantees.
- Out of scope: HUD/UI visual indicators (deferred to separate UI task), enemy exhaustion (enemies do not exhaust).

## Source of Truth
- Code:
  - `Assets/Scripts/Data/RunSessionManager.cs` (`ExhaustionRound`, `ExhaustionLimit`, `AdvanceExhaustion`, `RefreshExhaustionLimit`)
  - `Assets/Scripts/Data/CombatConfig.cs` (`earlyExhaustionLimit`, `midExhaustionLimit`, `lateExhaustionLimit`, `bossExhaustionLimit`, `GetExhaustionLimit`)
  - `Assets/Scripts/Combat/CombatCharacter.cs` (`Heal`, `ApplyHeal`, `ReviveFromDowned`)
  - `Assets/Scripts/Combat/Effects/HealEffect.cs` (Actual healed amount tracking)
  - `Assets/Scripts/Combat/Effects/HealGuardianEffect.cs` (Actual healed amount tracking)
  - `Assets/Scripts/Combat/StatusProcessor.cs` (Restore periodic heal tracking)
  - `Assets/Scripts/Data/SaveManager.cs` (`exhaustionRound` DTO persistence)
- Tests: `Assets/Editor/Tests/ExhaustionTests.cs`
- Data: `Assets/Data/CombatConfig.asset`

## Inputs
- Trigger condition: Combat round ends (`BattleSystem.OnRoundEnded`), scene loaded (`RunSessionManager.OnSceneLoaded`), or heal action executed (`CombatCharacter.Heal`).
- Configuration variables: Per-tier limits in `CombatConfig`.

## State Model
States:
- `Fresh / Active`: `ExhaustionRound < ExhaustionLimit` or `ExhaustionLimit <= 0`. Healing functions normally for all characters.
- `Exhausted`: `ExhaustionLimit > 0 && ExhaustionRound >= ExhaustionLimit`. Player team members cannot receive healing (any incoming heal is clamped/converted to 0). Enemy team members continue to heal normally.

Transitions:
1. `Fresh` -> `Exhausted` when `ExhaustionRound` reaches `ExhaustionLimit` at the end of a combat round.
2. `Exhausted` -> `Fresh` when the player enters `RD_HealRoom` (`ExhaustionRound` resets to 0).
3. `Exhausted` persists across standard combat encounters until a Heal Room is entered.

## Timing Model
- Update domain: Turn-based combat round end and scene initialization.
- Round End Hook: Hooked onto `BattleSystem.OnRoundEnded` via `RunSessionManager.SubscribeToBattle`. Increments `ExhaustionRound` by 1 (clamped to `ExhaustionLimit`).
- Scene Load Hook: In `RunSessionManager.OnSceneLoaded("CombatPrototype")`:
  1. If `IsHealRoom(CurrentRoomData)` is true, set `ExhaustionRound = 0`.
  2. Call `RefreshExhaustionLimit()` to derive the limit for the current progression tier and clamp `ExhaustionRound` if limit > 0.
  3. Snapshot `PreCombatExhaustionRound = ExhaustionRound`.

## Determinism
- Deterministic across runs: Yes. Round progression and tier mapping follow deterministic rules based on player choices and encounter progress.

## Formulas
```txt
# advance exhaustion round
ExhaustionRound = min(ExhaustionRound + 1, ExhaustionLimit)

# exhaustion condition
IsExhausted = (ExhaustionLimit > 0) AND (ExhaustionRound >= ExhaustionLimit)

# heal application
if (IsPlayerTeam AND IsExhausted):
    heal_amount = 0
actual_healed = clamp(heal_amount, 0, max(0, maxHP - currentHP))
currentHP = currentHP + actual_healed
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `earlyExhaustionLimit` | 8 | 0 | 99 | rounds | `CombatConfig` (Trivial & EarlyGame shared) |
| `midExhaustionLimit` | 8 | 0 | 99 | rounds | `CombatConfig` |
| `lateExhaustionLimit` | 10 | 0 | 99 | rounds | `CombatConfig` |
| `bossExhaustionLimit` | 12 | 0 | 99 | rounds | `CombatConfig` |

## Edge Cases
- **Trivial and EarlyGame Continuity**: Because there is no Heal Room between Trivial and EarlyGame tiers, they share `earlyExhaustionLimit` and do not reset between transitions.
- **Elite Rooms**: Elite rooms override the formation tier to `Elite`, but the exhaustion limit derives from the progression tier (`CombatConfig.GetEncounterTierForRoom(RoomProgression)`), preserving the zone's base exhaustion cap.
- **Revive From Downed**: Life Link revive restores HP via `ApplyHeal`, bypassing the exhaustion gate so characters can revive from the Downed state even when exhausted.
- **Never-Negative Healing**: All healing in `CombatCharacter.ApplyHeal` is clamped between 0 and missing HP. Status effects like `HealReceivedReduction` exceeding 100% or negative heal calculations can never deal damage to the character.
- **Mid-Battle Saves**: Resuming mid-battle restores `PreCombatExhaustionRound` to preserve run integrity consistent with HP snapshots.
- **Disabled Mechanic**: If a limit is configured as `<= 0`, `IsExhausted` is always `false` and `AdvanceExhaustion()` is a no-op.

## Failure Modes
- Null `CombatConfig` or database: `RefreshExhaustionLimit` safely defaults `ExhaustionLimit` to 0.

## Event Hooks
- Event: `BattleSystem.OnRoundEnded`, Trigger: round concludes, Payload: `roundNumber`
- Event: `CombatCharacter.OnHealed`, Trigger: character health increased, Payload: `character, actualHealed`

## Acceptance Tests
- Automated: `Assets/Editor/Tests/ExhaustionTests.cs`
  - `AdvanceExhaustion_IncrementsAndClampsAtLimit`
  - `AdvanceExhaustion_NoOp_WhenLimitZero`
  - `IsExhausted_False_WhenLimitZero`
  - `BattleSystem_OnRoundEnded_AdvancesExhaustion`
  - `GetExhaustionLimit_TrivialAndEarly_ShareLimit`
  - `GetExhaustionLimit_MapsMidLateBoss`
  - `OnSceneLoaded_HealRoom_ResetsExhaustion`
  - `OnSceneLoaded_CombatRoom_PersistsExhaustionAcrossBattles`
  - `OnSceneLoaded_TrivialToEarly_DoesNotReset`
  - `OnSceneLoaded_EliteRoom_UsesProgressionTierLimit`
  - `Heal_PlayerExhausted_HealsZero`
  - `Heal_EnemyExhausted_StillHeals`
  - `RestoreHOT_PlayerExhausted_NoHeal`
  - `HealGuardianEffect_PlayerExhausted_NoHeal`
  - `ReviveFromDowned_PlayerExhausted_StillRestoresHP`
  - `Heal_NegativeAmount_NeverReducesHP`
  - `HealEffect_HealReductionOver100_NeverDamages`
  - `SaveRun_MidBattle_SavesPreCombatExhaustionRound`
  - `LoadRun_RestoresExhaustionRound`

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
