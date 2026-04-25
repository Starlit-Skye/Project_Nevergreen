# Damage Over Time (DoT)

Owner: AI Agent (Antigravity)
Status: active
Last verified: 2026-04-24
Verified commit: 391a465c28dda2ad7c35f1ed2ef5eaffbb6cd336
Target build: Unity 2022.3 (PC)

## Purpose
Provide periodic damage or healing to characters over multiple turns, allowing for attrition-based combat strategies and attrition mitigation.

## Scope
- In scope: Bleed (Damage), Blight (Damage), Restore (Healing).
- In scope: Resistance checks, duration tracking, and turn-start ticking.
- Out of scope: Interaction between different DoT types (e.g. fire + oil), percentage-based DoTs (all current are flat amplitude).

## Source of Truth
- Code: `Assets/Scripts/Combat/Effects/StatusEffect.cs` (Application logic)
- Code: `Assets/Scripts/Combat/CombatCharacter.cs` (Tick and Duration logic)
- Code: `Assets/Scripts/Combat/CombatCalculator.cs` (Probability formulas)
- Data: `Assets/Scripts/Data/StatusType.cs` (Type definitions)

## Inputs
- Trigger: `Execute` call from a `SkillData` effect module.
- Requirements: Character must hit target (unless `ignoreMiss` is true).

## State Model
States:
- `Pending`: Status is being calculated for application.
- `Active`: Status is attached to the character and will tick at turn start.
- `Expired`: Duration reached zero; removed during the turn transition.

Transitions:
1. `Pending` -> `Active`: When `ResolveStatusApplication` returns true.
2. `Active` -> `Expired`: When `TickStatusDurations` is called and `duration` <= 0.

## Timing Model
- Update domain: Combat Turn Loop (`BattleSystem.ProcessTurn`)
- Application: During the Action Resolution phase.
- Tick: `Phase 1` of character's turn start (`ApplyStartOfTurnEffects`).
- Duration Reduction: `Phase 2` of character's turn start (`TickStatusDurations`), following the stun check.

## Determinism
- Deterministic across clients: No (Uses `System.Random` in `SkillContext` which is per-battle instance but not globally synchronized).
- Sources of nondeterminism: `System.Random` roll for application.
- Mitigation: Seeded RNG used within `SkillContext`.

## Formulas
```txt
# Application Chance
final_chance = source_application_chance - target_type_resistance
applied = random(0, 100) < final_chance

# Tick Value
damage_per_tick = sum(active_status_amplitude)
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `applicationChance` | 100 | 0 | 300 | % | `StatusEffect.cs` |
| `duration` | 3 | 1 | 99 | turns | `StatusEffect.cs` |
| `amplitude` | 1 | 1 | 999 | value | `StatusEffect.cs` |

## Edge Cases
- **Stunned Character**: A stunned character still takes DoT damage/healing in Phase 1, but skips their action. Stun duration is ticked in Phase 2.
- **Lethal DoT**: If a character dies from a DoT tick, the turn is immediately skipped and moved to the next actor.
- **Immunity**: Resistance values of 100+ effectively grant immunity to standard 100% chance applications.

## Failure Modes
- **Resisted**: Target resistance exceeds application chance; `OnStatusApplied` fires with `succeeded = false`.

## Event Hooks
- Event: `OnStatusApplied`, Trigger: After application roll, Payload: `target, type, succeeded`
- Event: `OnPeriodicEffectApplied`, Trigger: During tick phase, Payload: `target, type, amount`

## Acceptance Tests
- Automated: `Test_StatusApplication_Resist` (Verify resistance subtraction).
- Playtest: Apply Bleed to player; verify HP bar tweens and log message appears at start of player turn.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
