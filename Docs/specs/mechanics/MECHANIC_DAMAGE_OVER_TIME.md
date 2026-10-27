# Damage Over Time (DoT)

Owner: AI Agent (Antigravity)
Status: active
Last verified: 2026-10-26
Verified commit: 391a465c28dda2ad7c35f1ed2ef5eaffbb6cd336
Target build: Unity 2022.3 (PC)

## Purpose
Provide periodic damage or healing to characters over multiple turns, allowing for attrition-based combat strategies and attrition mitigation.

## Scope
- In scope: Bleed (Damage), Blight (Damage), Burn (Damage), Restore (Healing).
- In scope: Resistance checks, duration tracking, turn-start ticking, and applier attack scaling for damage DoTs (Bleed, Blight, Burn). Restore healing remains flat amplitude.
- Out of scope: Interaction between different DoT types (e.g. fire + oil).

## Source of Truth
- Code: `Assets/Scripts/Combat/Effects/StatusEffect.cs` (Application logic)
- Code: `Assets/Scripts/Combat/Effects/StatusInstanceFactory.cs` (Snapshot damagePerTick resolution)
- Code: `Assets/Scripts/Combat/CombatCharacter.cs` (Tick and Duration logic)
- Code: `Assets/Scripts/Combat/StatusProcessor.cs` (Periodic effect execution)
- Code: `Assets/Scripts/Combat/CombatCalculator.cs` (Probability formulas)
- Data: `Assets/Scripts/Data/StatusType.cs` (Type definitions)

## Inputs
- Trigger: `Execute` call from a `SkillData` effect module or proc from `BleedOnAttackStatusInstance`.
- Requirements: Character must hit target (unless `ignoreMiss` is true).
- Applier Attack: Evaluated via `applier.GetEffectiveStats().attack` at the moment of status application.

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

# Snapshot Damage Per Tick (at Application time for Bleed, Blight, Burn)
eff_attack = applier.GetEffectiveStats().attack
damage_per_tick = max(1, RoundAwayFromZero(eff_attack * amplitude / 100.0))

# Periodic Tick Value (at Turn Start Phase 1)
# Burn increments its own damage_per_tick by +1 before damage resolution
damage_per_tick_aggregate = sum(active_dot_damage_per_tick)
heal_per_tick_aggregate = sum(active_restore_amplitude) # Restore remains flat amplitude
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `applicationChance` | 100 | 0 | 300 | % | `StatusEffect.cs` |
| `duration` | 3 | 1 | 99 | turns | `StatusEffect.cs` |
| `amplitude` | 1 | 1 | 999 | % of applier Attack (DoT) / flat HP (Restore) | `StatusEffect.cs` |
| `damagePerTick` | 1 | 1 | 9999 | flat damage | `StatusEffectInstance.cs` |

## Edge Cases
- **Snapshot Scaling**: `damagePerTick` is frozen at application time from the applier's effective stats. Subsequent changes (buffs/debuffs) to the applier do not alter already applied DoTs.
- **Dead Applier**: If the applier dies or is defeated mid-combat, existing applied DoTs continue ticking normally until their duration expires.
- **Minimum Floor**: DoT damage is clamped at a minimum of 1 per tick (`max(1, ...)`). An applier with 0 effective attack still inflicts 1 damage per tick.
- **Defense Bypass**: Periodic DoT ticks bypass target defense directly (via `TakeDamage`).
- **Burn Escalation**: Each non-expired Burn status increments its `damagePerTick` by +1 flat prior to each periodic damage tick.
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
