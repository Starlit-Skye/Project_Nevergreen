# Character Database System

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define ScriptableObject-backed character data for marionettes and enemies, including per-level stat
resolution at runtime.

## Scope
- In scope: character data schema, unique identifiers, display names, stat-per-level mapping,
  runtime stat lookup rule
- Out of scope: combat formulas, skill execution logic, editor tooling implementation details

## Source of Truth
- Code: `Unknown` (character database runtime/editor implementation not provided)
- Tests: `Unknown` (character data validation/runtime tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (section: Technical -> Character Database)
- Data: `Unknown` (character ScriptableObject asset paths not provided)
- Issue/ADR: Unknown

## Responsibilities
- Store each character definition (marionette or enemy) as ScriptableObject-backed data.
- Require each character definition to include a unique id and display-name string.
- Store a per-level stat list where each index maps to one character level.
- Define and expose a global maximum level used by runtime level progression systems.
- Resolve runtime stat block by `current_level - 1` index (0-based indexing).
- Keep ScriptableObject data immutable at runtime lookup points.

## Data Model
- Entity/component/object: `CharacterData` with `character_id`, `display_name`, `stat_per_level_list`
  (`List<StatBlockData>`)
- Entity/component/object: `StatBlockData` ScriptableObject with numeric values for character stats
  used by combat systems
- Global config/object: `GlobalLevelConfig` with `global_max_level`
- Persistence keys: Unknown

## Event Contracts
- Event: `character_data_loaded`
- Producer: character database/bootstrap loader
- Consumers: combat setup, AI setup, stat presentation
- Payload schema: character id, display name, max level entries

- Event: `character_stats_resolved_for_level`
- Producer: runtime stat resolver
- Consumers: combat turn system, UI, skill execution context builders
- Payload schema: character id, current level, resolved index, stat block reference

- Event: `character_data_validation_failed`
- Producer: data validation pass
- Consumers: content pipeline/QA
- Payload schema: character id, validation rule id, error message

## Timing Model
- Update domain: data load during startup/content load; read-only stat resolution during combat/runtime
- Tick/update order: load/validate character data before battle assembly; resolve stats when character
  instance level is known
- Budget: Unknown

## Determinism
- Required: yes (static data lookup by deterministic index rule)
- Strategy: resolve stats strictly with `resolved_index = current_level - 1`
- Known exceptions: behavior when level is out of list bounds is Unknown

## Authority Model
- Single-player/offline: character definitions are authored by developers and consumed by local runtime
- Multiplayer: Unknown

## Performance Budget
- CPU: Unknown
- Memory: Unknown
- Entity scale target: Unknown

## Error Handling and Recovery
- Duplicate `character_id`: Unknown
- Out-of-range level index (`current_level - 1` invalid): Unknown
- Level-up request above `global_max_level`: reject clamp behavior is Unknown
- Recovery strategy: Unknown

## Observability
- Metrics: character data load success/failure counts, stat resolution failure count by character id
- Logs: invalid ids, invalid per-level list lengths, out-of-range index lookups
- Traces/profilers: Unknown

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify marionette/enemy stats change correctly by level, verify display names match combat
  UI, verify runtime lookup follows `current_level - 1` mapping, verify level-up is blocked at global
  maximum level

## Missing Evidence
- Runtime code path for character data loader and stat resolver
- Asset path conventions for `CharacterData` and `StatBlockData`
- Validation rules for min/max supported levels and missing stat entries
- Runtime handling contract for out-of-range level indices
- Source and ownership of `global_max_level` configuration

## Validation
- [ ] Facts match current code/content
- [ ] Timing, authority, and determinism are explicit
- [ ] Performance budgets are stated with units
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined


