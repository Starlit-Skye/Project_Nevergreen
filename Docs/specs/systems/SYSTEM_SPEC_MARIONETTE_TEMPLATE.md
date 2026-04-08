# Marionette Template System

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define the baseline gameplay template for marionette units the player collects and uses in combat.

## Scope
- In scope: marionette baseline attributes, spawn construction rules, progression hooks, class role map
- Out of scope: full per-class skill lists, authored stat tables, UI widget implementation details

## Source of Truth
- Code: `Unknown` (marionette runtime implementation not provided)
- Tests: `Unknown` (marionette tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: Marionettes, Marionette Classes, Combat Characters, Stats, Skills, Economy, Technical)
- Data: `Assets/docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md` (`CharacterData`/`StatBlockData` schema contract), `Assets/docs/specs/systems/SYSTEM_SPEC_ECONOMY_RUNTIME.md` (`Parts`/`Scraps` runtime rules), `Assets/docs/specs/mechanics/MECHANIC_SPEC_PARTS_LEVEL_UP.md` (`Parts` level-up behavior)
- Issue/ADR: Unknown

## Responsibilities
- Define marionette as a player combat unit with HP, shared core stats, and up to 4 battle skills.
- Enforce marionette spawn template: 3-4 class skills, 1 random perfection, 1 random imperfection.
- Ensure marionette skills are selected from class-unique skill pools (no cross-class shared skills).
- Track marionette level progression and class-specific base/growth behavior.
- Resolve marionette stat blocks from Character Database using `current_level - 1` index lookup.
- Enforce global level cap: marionette level cannot increase past `global_max_level`.
- Apply level-up costs in `Parts` via Economy Runtime rules.
- Provide destruction behavior when HP is depleted.

## Data Model
- Entity/component/object: `Marionette` with class, level, HP, core stats, resistances, skill loadout
  (up to 4 equipped from class-unique pool), perfections, imperfections, trinket slots
- Rank semantics: `rank 1` is front-most and `rank 4` is back-most; marionettes/player team are on
  left side of screen facing right
- Persistence keys: Unknown

## Event Contracts
- Event: `marionette_spawned`
- Producer: marionette generation/room reward system
- Consumers: party roster, combat setup, stat screen
- Payload schema: class id, level, selected skills (3-4), perfection id, imperfection id

- Event: `marionette_destroyed`
- Producer: combat resolution
- Consumers: battle state machine, run progression systems
- Payload schema: marionette id, battle id, turn index, cause

- Event: `marionette_level_changed`
- Producer: leveling/economy system
- Consumers: stat calculation, UI
- Payload schema: marionette id, old level, new level, parts spent

## Timing Model
- Update domain: turn-based combat for battle interactions; run-level progression for leveling
- Tick/update order: marionette actions resolve in combat turn order by Speed; on Speed ties against
  enemies, enemy entries resolve first; when tied characters are on the same team, front-most rank
  resolves first
- Budget: Unknown

## Determinism
- Required: partial
- Strategy: deterministic application of explicit rules; random spawn selections are documented
- Known exceptions: random skill/perfection/imperfection assignment at spawn

## Authority Model
- Single-player/offline: player controls marionette actions on marionette turns
- Multiplayer: Unknown

## Performance Budget
- CPU: Unknown
- Memory: Unknown
- Entity scale target: up to 4 active marionettes in player team

## Error Handling and Recovery
- Missing class data: Unknown
- Invalid spawn selection set: Unknown
- Level-up request above global level cap: reject/clamp behavior is Unknown
- Recovery strategy: Unknown

## Observability
- Metrics: marionette survival rate per room, average marionette level per run (thresholds Unknown)
- Logs: spawn payload, destruction events, level-up events
- Traces/profilers: Unknown

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify spawn composition (3-4 class skills + 1 perfection + 1 imperfection), destruction
  on HP depletion, leveling via Parts, global level cap enforcement, and role diversity across classes

## Missing Evidence
- Runtime code path and symbol for marionette spawn generation
- Runtime code path and symbol for level/stat growth
- Class stat tables and skill pools per class
- Persistence schema for marionette state
- Global level cap value and config path

## Validation
- [ ] Facts match current code/content
- [ ] Timing, authority, and determinism are explicit
- [ ] Performance budgets are stated with units
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined








