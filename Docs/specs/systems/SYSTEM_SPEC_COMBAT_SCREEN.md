# Combat Screen System

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define the combat-facing screen/system behavior for entering battles, running encounters, and handling
boss-room outcome transitions.

## Scope
- In scope: battle entry/exit flow, route-to-combat loop hooks, combat outcome transition behavior
- Out of scope: visual styling, animation implementation, cutscene implementation internals

## Source of Truth
- Code: `Unknown` (combat screen implementation not provided)
- Tests: `Unknown` (screen flow tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: UI Design -> Combat Screen, Inputs and Interactions -> Combat, Gameloop Flow, Combat,
  Alternative Game Start, Final Boss Fight, Technical)
- Data: `Unknown` (UI flow config/state machine data not provided), `Assets/docs/specs/architecture/ARCHITECTURE_SPEC_COMBAT_RUNTIME.md` (module integration map), `Assets/docs/specs/systems/SYSTEM_SPEC_ECONOMY_RUNTIME.md` (battle and event reward application), `Assets/docs/specs/mechanics/MECHANIC_SPEC_COMBAT_INPUT_INTERACTIONS.md` (combat input interaction flow)
- Issue/ADR: Unknown

## Responsibilities
- Start combat when entering battle rooms.
- Represent team ranks `1-4` with mirrored enemy/player spatial mapping.
- Define combat orientation: player team on left facing right, enemy team on right facing left.
- Define rank semantics: `rank 1` front-most and `rank 4` back-most for each team.
- Display both teams in active rank order with `rank 1` closest to screen center and `rank 4` furthest.
- Display HP bars above each combat character.
- Display bottom combat UI with skill selection section and stats section.
- Show currently acting player character skills in the skill selection section.
- Show hovered character stats in the stats section, including effective values after trinkets,
  perfections, imperfections, buffs, and debuffs.
- Present battle completion routing to one of three route choices where applicable.
- Handle boss-room branch outcome:
  if true-boss criteria are unmet, fade to black, show run statistics, and return to pre-run prep.
- Trigger true boss cutscene path when criteria are met.

## Data Model
- Entity/component/object: `CombatScreenState` with run id, room id, encounter state, route options,
  boss criteria flag, post-boss transition target
- Entity/component/object: `CombatUIState` with active actor id, selected skill/move id,
  selected-target candidates, hovered character id, hovered effective stat snapshot
- Layout model (derived from UI Design combat mockup):
  ```txt
  +----------------------------------------------------------------------------------+
  |                                  COMBAT ARENA                                    |
  |                                                                                  |
  |  [P1_HP][P2_HP][P3_HP][P4_HP]                            [E1_HP][E2_HP][E3_HP][E4_HP]
  |     P1     P2     P3     P4                                 E1     E2     E3     E4
  |    (o)    (o)    (o)    (o)                                (o)    (o)    (o)    (o)
  |   [^]    [^]    [^]    [^]                                [^]    [^]    [^]    [^]
  |   feet target indicator appears here for valid targets                              |
  +----------------------------------------------------------------------------------+
  | [Skill1] [Skill2] [Skill3] [Skill4]            [        Stats Display         ] |
  +----------------------------------------------------------------------------------+
  ```
- Layout semantics:
  player team block is left side, enemy team block is right side; HP bars render above each character;
  skill selection row is bottom-left; stats display panel is bottom-right; valid-target indicator sprite
  renders at feet of valid targets
- Persistence keys: Unknown

## Event Contracts
- Event: `combat_screen_entered`
- Producer: room navigation/run flow system
- Consumers: combat setup, UI controller
- Payload schema: run id, room id, room type

- Event: `combat_resolved`
- Producer: combat system
- Consumers: route selection UI, rewards, run flow
- Payload schema: battle id, battle type, outcome, parts granted, scraps granted, rewards, surviving roster

- Event: `combat_skill_selected`
- Producer: combat input layer
- Consumers: combat UI controller, target-highlight subsystem
- Payload schema: actor id, skill id or move action id, valid target ids

- Event: `combat_target_hover_changed`
- Producer: cursor/hover subsystem
- Consumers: stats panel controller, target highlight controller
- Payload schema: hovered character id, hovered effective stats

- Event: `boss_room_transition`
- Producer: run flow system
- Consumers: screen transition, statistics panel, cutscene trigger
- Payload schema: true-boss-criteria-met, next state id

## Timing Model
- Update domain: event-driven screen state transitions around turn-based combat sessions
- Tick/update order: enter room -> start battle (if present) -> resolve battle -> choose next route
- Budget: Unknown

## Determinism
- Required: partial
- Strategy: deterministic branch based on true-boss criteria flag and room outcome
- Known exceptions: route content/reward generation randomness is Unknown

## Authority Model
- Single-player/offline: local player input selects route and combat actions
- Multiplayer: Unknown

## Performance Budget
- CPU: Unknown
- Memory: Unknown
- Entity scale target: Unknown

## Error Handling and Recovery
- Missing route options after battle: Unknown
- Invalid boss criteria state: Unknown
- Recovery strategy: Unknown

## Observability
- Metrics: time in combat screen states, battle completion rate, boss-branch selection frequency
- Logs: room entry, battle start/end, route selection, boss transition decisions
- Traces/profilers: Unknown

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify first-room marionette reward path, post-battle 1-of-3 route selection, non-true
  boss fade/statistics return flow, true-boss cutscene trigger flow, HP bars above characters,
  active-actor skill list rendering, hovered effective stat display updates, and layout placement
  fidelity against the combat mockup model

## Missing Evidence
- Combat screen implementation path(s) and state machine symbols
- True-boss criteria source and evaluation logic
- Route generation rules and data tables
- Automated tests for room-to-combat-to-route and boss transition branches

## Validation
- [ ] Facts match current code/content
- [ ] Timing, authority, and determinism are explicit
- [ ] Performance budgets are stated with units
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined









