# Skills Database System

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define how skill definitions are authored and consumed through ScriptableObject-based data for combat.

## Scope
- In scope: skill data schema, skill identifiers, modifier schema, targeting/rank constraints, strategy
  reference for custom execution
- Out of scope: full class/monster skill catalogs, custom effect script implementations, editor tooling
  implementation details

## Source of Truth
- Code: `Unknown` (skill data runtime/editor implementation not provided)
- Tests: `Unknown` (skill data validation tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: Technical -> Skills Database, Skills Modifier, Skills, Combat)
- Data: `Assets/docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md` (character stat source dependency)
- Issue/ADR: Unknown

## Responsibilities
- Store each skill as one ScriptableObject definition.
- Require each skill to include a unique id and a display-name string.
- Store rank-use constraints, rank-target constraints, target scope constraints (`self`, `allies`,
  `enemies`), and max target count.
- Store a modifier list used during skill execution.
- Ensure stat-scaling modifiers (`Damage`, `Heal`, `Accuracy`, `Critical`) evaluate against runtime
  character stats resolved from Character Database.
- Link skill definitions to custom scripted execution behavior via strategy pattern.

## Data Model
- Entity/component/object: `SkillData` with `skill_id`, `display_name`, `modifiers`, `use_ranks`,
  `target_ranks`, `max_targets`, `target_scope` (self/allies/enemies), `custom_effect_strategy_ref`
- Rank mapping contract for `use_ranks` and `target_ranks`: `rank 1` is front-most and `rank 4` is
  back-most; player team occupies left side facing right, enemy team occupies right side facing left
- Modifier schema (defined in GDD):
  `Damage` (percent multiplier of user Attack),
  `Heal` (percent multiplier of user Attack),
  `Accuracy` (additive to base Accuracy),
  `Critical` (additive to base Critical Chance)
- Modifier rules: `Damage` and `Heal` are mutually exclusive in one modifier set; modifier entries
  with value `0` are not displayed in UI
- Persistence keys: Unknown

## Event Contracts
- Event: `skill_data_loaded`
- Producer: skill database/bootstrap loader
- Consumers: combat UI, AI selectors, combat resolution system
- Payload schema: skill id, display name, owning class/monster id, targeting constraints

- Event: `skill_selected_for_execution`
- Producer: player input or enemy AI
- Consumers: combat resolution pipeline
- Payload schema: actor id, skill id, selected targets, turn index

- Event: `skill_data_validation_failed`
- Producer: data validation pass
- Consumers: content pipeline/QA
- Payload schema: skill id, validation rule id, error message

## Timing Model
- Update domain: data load at startup/content load; read-only access during turn-based combat
- Tick/update order: load/validate before combat; consume on each action selection/resolution
- Budget: Unknown

## Determinism
- Required: yes (static skill data lookup)
- Strategy: resolve skill behavior from deterministic data fields and explicit strategy reference
- Known exceptions: runtime random branches inside custom effect scripts are Unknown

## Authority Model
- Single-player/offline: skill definitions are authored by developers and consumed by local runtime
- Multiplayer: Unknown

## Performance Budget
- CPU: Unknown
- Memory: Unknown
- Entity scale target: Unknown

## Error Handling and Recovery
- Duplicate `skill_id`: Unknown
- Missing or invalid strategy reference: Unknown
- Recovery strategy: Unknown

## Observability
- Metrics: skill load success/failure counts, invalid skill definitions, per-skill usage frequency
- Logs: data validation errors for skill ids, missing references, invalid target constraints
- Traces/profilers: Unknown

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify each selected skill enforces rank requirements, target limits, and target side
  constraints (`self`, `allies`, `enemies`); verify display names match expected combat UI labels;
  verify modifier behavior and rules (`Damage`/`Heal` mutual exclusivity, zero-value hidden in UI)

## Missing Evidence
- Runtime code path for skill ScriptableObject loader and validators
- Asset path conventions for skill data
- Rule set for duplicate ids and conflicting modifiers
- Serialization/persistence behavior for skill data assets

## Validation
- [ ] Facts match current code/content
- [ ] Timing, authority, and determinism are explicit
- [ ] Performance budgets are stated with units
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined


