# Enemy Template System

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define the baseline gameplay template for enemy units, including normal enemies, elites, and boss
patterns described in the design source.

## Scope
- In scope: enemy combat role template, elite behavior patterns, multi-action behavior, spawn notes
- Out of scope: full numeric stat tables, full skill database, AI implementation internals

## Source of Truth
- Code: `Unknown` (enemy runtime implementation not provided)
- Tests: `Unknown` (enemy behavior tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: Enemies, Elites, Bosses, Final Boss Fight, Combat, Combat Characters, Stats, Statuses,
  Technical)
- Data: `Assets/docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md` (`CharacterData`/`StatBlockData` schema contract), `Assets/docs/specs/systems/SYSTEM_SPEC_ECONOMY_RUNTIME.md` (elite-vs-normal reward differentiation), `Assets/docs/specs/mechanics/MECHANIC_SPEC_BATTLE_REWARD_DROPS.md` (elite reward mechanic)
- Issue/ADR: Unknown

## Responsibilities
- Provide hostile combatants for turn-based encounters.
- Require both a stable unique enemy id and a developer-customizable display name per enemy template.
- Support enemy action selection per turn, including multi-action enemies.
- Enforce enemy skill identity and ownership (skills are unique to a monster/class definition).
- Resolve enemy stat blocks from Character Database using `current_level - 1` index lookup.
- Support elite mechanics with explicit conditional behavior chains.
- Mark battle type (`normal`/`elite`) for Economy Runtime reward calculation.
- Support boss fight phase progression where defined.

## Data Model
- Entity/component/object: `Enemy` with `enemy_id` (unique), `display_name` (developer editable),
  archetype, HP, core stats, resistances, rank, skill list (up to 4), action count per round,
  status state
- Rank semantics: `rank 1` is front-most and `rank 4` is back-most; enemies are on right side of
  screen facing left
- Persistence keys: Unknown

## Event Contracts
- Event: `enemy_spawned`
- Producer: combat encounter setup
- Consumers: combat turn system, UI, rewards system
- Payload schema: enemy id, display name, archetype id, encounter id, starting slot, starting status

- Event: `enemy_action_selected`
- Producer: enemy AI selector
- Consumers: combat resolver, telemetry
- Payload schema: enemy id, action id, target selection, turn index

- Event: `enemy_defeated`
- Producer: combat resolver
- Consumers: rewards/economy, encounter progression
- Payload schema: enemy id, encounter id, cause, round index

## Timing Model
- Update domain: turn-based round loop
- Tick/update order: enemy turns are scheduled by Speed in shared turn order; on Speed ties, enemy
  entries resolve before player entries; when tied characters are on the same team, front-most rank
  resolves first
- Budget: Unknown

## Determinism
- Required: partial
- Strategy: rule-driven behavior for named elite mechanics
- Known exceptions: enemy random skill selection is explicitly documented

## Authority Model
- Single-player/offline: enemy actions are controlled by game AI
- Multiplayer: Unknown

## Performance Budget
- CPU: Unknown
- Memory: Unknown
- Entity scale target: up to 4 active enemies per team context (baseline combat team size)

## Error Handling and Recovery
- Missing enemy skill list: Unknown
- Invalid phase transition state: Unknown
- Recovery strategy: Unknown

## Observability
- Metrics: encounter completion rate, enemy action distribution, elite wipe rate (thresholds Unknown)
- Logs: spawn events, selected actions, phase transitions, defeat events
- Traces/profilers: Unknown

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify elite patterns (Jailbird, Novellite, Pebble, Ruffian), multi-action scheduling,
  and boss phase transitions and special actions

## Missing Evidence
- Runtime code path and symbols for enemy AI/action selection
- Encounter composition tables and spawn weighting data
- Implementation details for elite and boss mechanics
- Test coverage for phase transitions and split/revive edge cases

## Validation
- [ ] Facts match current code/content
- [ ] Timing, authority, and determinism are explicit
- [ ] Performance budgets are stated with units
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined







