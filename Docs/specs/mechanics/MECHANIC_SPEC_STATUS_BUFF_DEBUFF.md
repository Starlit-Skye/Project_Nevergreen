# Buff and Debuff Status Effects

Owner: Combat Engineering Team
Status: active
Last verified: 2026-05-05
Verified commit: HEAD
Target build: Unity 2022.3 + Windows

## Purpose
Provide a standardized mechanism for temporary stat modifications (increases or decreases) applied to combatants. These effects allow skills to influence character power, survivability, and utility dynamically during battle.

## Scope
- In scope: calculation of effective stats using additive percentage stacking, application and resistance resolution, duration management, and expiration logic.
- Out of scope: visual effects/animations, specific skill implementations that use these statuses, and periodic damage/healing effects (see Bleed/Blight).

## Source of Truth
- Code: `Assets/Scripts/Combat/CombatCharacter.cs` (`GetEffectiveStats`), `Assets/Scripts/Combat/StatusProcessor.cs` (`TickDurations`), `Assets/Scripts/Combat/CombatCalculator.cs` (`ResolveStatusApplication`)
- Tests: `Assets/Editor/Tests/BuffDebuffTests.cs` (All scenarios)
- Design: `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` (Formulas section)
- Data: `Assets/Scripts/Data/SkillData.cs` (`StatTarget` enum, `StatusType` enum)

## Inputs
- Input action: application via `CombatCharacter.AddStatus(StatusEffectInstance)`
- Input conditions: must be triggered by a skill effect or system event (e.g., stun recovery bonus); requires a valid `StatTarget` and `StatusType` (Buff or Debuff).
- Input buffering: None (applied immediately upon resolution).

## State Model
States:
- `Active`: The status effect is stored in the character's `statusEffects` list and contributes to stat calculations.
- `Expired`: The status effect's duration has reached 0; it no longer affects stats and is marked for removal.

Transitions:
1. `Non-existent` -> `Active` when a status application resolves successfully.
2. `Active` -> `Expired` when `StatusProcessor.TickDurations` reduces duration to 0.
3. `Expired` -> `Non-existent` when `CombatCharacter.RemoveStatus` is called.

## Timing Model
- Update domain: tick (turn-based events)
- Tick rate: 1 tick per character turn.
- Order dependencies:
  - (1) Status ticking occurs in `BattleSystem.ProcessTurn` after the stun check.
  - (2) Effective stats are recalculated on-demand via `GetEffectiveStats()` whenever a stat is accessed (e.g., during damage calculation).
  - (3) Application occurs during skill execution resolution in `BattleSystem.ExecuteSkill`.

## Determinism
- Deterministic across clients: Yes, provided the RNG seed for `ResolveStatusApplication` is synced.
- Sources of nondeterminism: RNG roll during application if the chance is < 100%.
- Mitigation: Synchronized random seed at the start of battle.

## Formulas
```txt
# status application resolution
final_application_chance = source_chance - target_resistance

# effective stat calculation (additive percentage stacking)
# all active buffs and debuffs for a specific stat target are summed first
net_percentage_modifier = sum(buff_amplitudes) - sum(debuff_amplitudes)
stat_multiplier = 1.0 + (net_percentage_modifier / 100.0)

# final rounding
effective_stat = round_to_int(base_stat * stat_multiplier)
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `buff_amplitude` | Varies | 0 | 1000 | percent | `SkillData` |
| `debuff_amplitude` | Varies | 0 | 1000 | percent | `SkillData` |
| `status_duration` | 3 | 1 | 99 | turns | `SkillData` |
| `accuracy_cap` | 95 | 0 | 95 | percent | `CombatConfig` |
| `defense_cap` | 80 | 0 | 100 | percent | `CombatConfig` |

## Edge Cases
- **Stat Target Coverage**: Supported targets include Attack, Defense, Accuracy, Dodge, CritChance, Speed, MaxHP, and all Resistances (Bleed, Blight, Stun, Debuff, Move).
- **Additive Stacking**: Two +10% buffs result in +20% (1.2x), not 1.21x (compounded).
- **Duration 0**: If a status is added with 0 duration, it is considered immediately expired and does not modify stats.
- **Stun Recovery**: Characters receive a `Buff(StunResist, +300%, 1)` immediately upon a Stun status expiring to prevent "stun-locking".

## Failure Modes
- **Null Reference**: If `CharacterData` is missing during initialization, stats default to 0 and status application may fail.
- **Stat Underflow**: If debuffs exceed 100% total, the stat multiplier becomes negative; current implementation does not explicitly cap the multiplier at 0, which may lead to negative stats (though `RoundToInt` and subsequent logic usually handle 0+).

## Event Hooks
- Event: `OnStatusApplied`, Trigger: application resolution, Payload: character, type, success
- Event: `OnStatsChanged`, Trigger: status added/removed or duration tick, Payload: character

## Acceptance Tests
- Automated: `Assets/Editor/Tests/BuffDebuffTests.cs`
  - `Buff_Attack_IncreasesStatByPercentageOfBase`: Verify 10% buff works.
  - `MultipleBuffs_SameStat_StackAdditively`: Verify additive stacking.
  - `DebuffResistance_ReducesApplicationChance`: Verify resist logic.
  - `BuffDuration_ExpiresAfterCorrectTicks`: Verify duration ticking.
- Playtest: Apply "Speed Buff" and verify character moves earlier in the next round's turn order.

## Missing Evidence
- **Maximum Stacking Cap**: Currently, there is no hard cap on the number of buffs/debuffs or the total percentage modifier (other than Defense/Dodge caps in `CombatConfig`).

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Unknowns are explicitly labeled
- [x] Links and paths resolve
- [x] Acceptance tests are defined
