# Buff and Debuff Status Effects

Owner: Combat Engineering Team
Status: active
Last verified: 2026-10-23
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

# effective stat calculation
# All 12 stats apply flat additions first, then scale by percentage multipliers:
# Formula: (Base + FlatMod) * (1.0 + PercentMod)
net_flat_modifier = sum(buff_flat_amplitudes) - sum(debuff_flat_amplitudes) + trait_flat_bonuses
net_percentage_modifier = (sum(buff_percent_amplitudes) - sum(debuff_percent_amplitudes) + trait_percent_bonuses) / 100.0

raw_stat = round_to_int((base_stat + net_flat_modifier) * (1.0 + net_percentage_modifier))

# Floor clamping:
effective_core_stat = max(0, raw_stat)        # Attack, Accuracy, CritChance, Defense, Dodge, Speed, and all Resistances
effective_max_hp = max(1, raw_stat)          # MaxHP cannot be reduced below 1

# Upper caps (CombatConfig):
effective_defense = min(effective_core_stat, defense_cap)
effective_dodge = min(effective_core_stat, dodge_cap)
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `buff_amplitude` | Varies | 0 | 1000 | percent / flat | `SkillData` |
| `debuff_amplitude` | Varies | 0 | 1000 | percent / flat | `SkillData` |
| `status_duration` | 3 | 1 | 99 | turns | `SkillData` |
| `stat_floor` | 0 | 0 | 0 | integer | System |
| `max_hp_floor` | 1 | 1 | 1 | integer | System |
| `accuracy_cap` | 95 | 0 | 95 | percent | `CombatConfig` |
| `defense_cap` | 80 | 0 | 100 | percent | `CombatConfig` |

## Edge Cases
- **Stat Target Coverage**: Supported targets include Attack, Defense, Accuracy, Dodge, CritChance, Speed, MaxHP, and all Resistances (Bleed, Blight, Stun, Debuff, Move).
- **Additive Stacking**: Two +10% buffs result in +20% (1.2x), not 1.21x (compounded).
- **Floor at Zero**: No stat (`attack`, `accuracy`, `critChance`, `defense`, `dodge`, `speed`, `bleedResist`, `blightResist`, `stunResist`, `debuffResist`, `moveResist`) can be reduced below 0. `maxHP` cannot be reduced below 1.
- **Current HP Clamping**: When effective `maxHP` is reduced by debuffs, `currentHP` is immediately clamped to `effective_max_hp`.
- **Duration 0**: If a status is added with 0 duration, it is considered immediately expired and does not modify stats.
- **Stun Recovery**: Characters receive a `Buff(StunResist, +300, 1)` immediately upon a Stun status expiring to prevent "stun-locking". This is applied as a flat additive bonus.

## Failure Modes
- **Null Reference**: If `CharacterData` or `baseStats` is missing during initialization, stats default to 0/empty and status application handles null gracefully.
- **Stat Underflow Prevention**: Even if net debuffs exceed 100% or flat debuffs exceed base stats, effective stats are floored at 0 (or 1 for MaxHP), preventing negative values or inverted multipliers.

## Event Hooks
- Event: `OnStatusApplied`, Trigger: application resolution, Payload: character, type, success
- Event: `OnStatsChanged`, Trigger: status added/removed or duration tick, Payload: character

## Acceptance Tests
- Automated: `Assets/Editor/Tests/BuffDebuffTests.cs`
  - `Buff_Attack_IncreasesStatByPercentageOfBase`: Verify 10% buff works.
  - `MultipleBuffs_SameStat_StackAdditively`: Verify additive stacking.
  - `Debuff_Resistance_ClampedAtZero`: Verify resistance debuffs clamp at 0.
  - `StatClamping_CorePercentStats_ClampedAtZero`: Verify attack, speed, defense, accuracy, dodge clamp at 0 under heavy debuffs.
  - `StatClamping_MaxHP_PercentDebuff_ClampedAtOne`: Verify MaxHP clamps at 1.
  - `StatClamping_FlatStats_ClampedAtZero`: Verify critChance and all 5 resistances clamp at 0.
  - `StatClamping_MaxHPDebuff_ReclampsCurrentHP`: Verify currentHP is clamped to effective maxHP when debuffed.
  - `StatClamping_DebuffExpiration_RestoresBaseValue`: Verify stat returns to base upon debuff expiration.
  - `DebuffResistance_ReducesApplicationChance`: Verify resist logic.
  - `BuffDuration_ExpiresAfterCorrectTicks`: Verify duration ticking.
- Playtest: Apply "Speed Buff" and verify character moves earlier in the next round's turn order. Apply heavy debuffs and verify no stats display negative values.

## Missing Evidence
- **Maximum Stacking Cap**: Currently, there is no hard cap on the number of buffs/debuffs or the total percentage modifier (other than Defense/Dodge caps in `CombatConfig`).

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Unknowns are explicitly labeled
- [x] Links and paths resolve
- [x] Acceptance tests are defined
