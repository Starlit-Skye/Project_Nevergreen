# Economy Runtime System

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define runtime currency earning and spending rules for `Parts` and `Scraps` during a run.

## Scope
- In scope: battle-end drops, event rewards, elite reward differentiation, level-up spending, run reset
- Out of scope: shop inventory design, event catalog design, exact numeric drop formulas

## Source of Truth
- Code: `Unknown` (economy runtime implementation not provided)
- Tests: `Unknown` (economy tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: Economy, Gameloop Flow)
- Data: `Assets/docs/specs/mechanics/MECHANIC_SPEC_BATTLE_REWARD_DROPS.md` (battle reward mechanic contract), `Assets/docs/specs/mechanics/MECHANIC_SPEC_PARTS_LEVEL_UP.md` (level-up spend mechanic contract)
- Issue/ADR: Unknown

## Responsibilities
- Maintain two run-scoped currencies: `Parts` and `Scraps`.
- Grant `Parts` and `Scraps` at battle end with slight randomization.
- Grant additional small currency rewards from qualifying events.
- Apply higher battle-end currency rewards for elite battles versus normal battles.
- Spend `Parts` for leveling Ceci or a selected marionette with progressively increasing cost.
- Spend `Scraps` for miscellaneous uses (for example shops and event payments).
- Reset `Parts` and `Scraps` at run end (no carry-over to next run).

## Data Model
- Entity/component/object: `RunEconomyState` with `parts`, `scraps`, `run_id`
- Entity/component/object: `BattleRewardProfile` with `battle_type` (`normal`/`elite`), reward ranges
- Entity/component/object: `LevelUpCostCurve` for progressive `Parts` costs
- Persistence keys: none across runs (run-scoped only)

## Event Contracts
- Event: `battle_rewards_granted`
- Producer: battle resolution/reward subsystem
- Consumers: run economy state, reward UI, progression systems
- Payload schema: battle id, battle type, parts granted, scraps granted, random roll context

- Event: `event_rewards_granted`
- Producer: event resolution subsystem
- Consumers: run economy state, event UI
- Payload schema: event id, parts granted, scraps granted

- Event: `level_up_purchased`
- Producer: level-up/economy subsystem
- Consumers: character progression, UI
- Payload schema: character id, old level, new level, parts spent, parts remaining

- Event: `run_economy_reset`
- Producer: run lifecycle subsystem
- Consumers: run setup, UI
- Payload schema: previous run id, parts reset from, scraps reset from

## Timing Model
- Update domain: event-driven during run flow
- Tick/update order: resolve battle/event outcome -> grant rewards -> update UI/state -> allow spending
- Budget: Unknown

## Determinism
- Required: partial
- Strategy: deterministic application of configured reward logic and cost curves
- Known exceptions: slight randomization in battle-end drop amounts

## Authority Model
- Single-player/offline: local runtime owns currency state updates
- Multiplayer: Unknown

## Performance Budget
- CPU: Unknown
- Memory: Unknown
- Entity scale target: Unknown

## Error Handling and Recovery
- Negative reward or spend values: Unknown
- Insufficient `Parts` for level-up purchase: block behavior is partially defined (no level-up)
- Recovery strategy: Unknown

## Observability
- Metrics: parts/scraps earned per run, elite-vs-normal reward delta, level-up purchases per run
- Logs: reward grants, spend actions, reset events
- Traces/profilers: Unknown

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify battle-end random rewards for `Parts`/`Scraps`, verify elite rewards are higher than
  normal rewards, verify event rewards, verify progressive `Parts` level-up costs, and verify full
  currency reset at run end

## Missing Evidence
- Runtime code path for reward generation and application
- Numeric reward ranges/distributions for normal and elite battles
- Event reward tables and eligibility rules
- Level-up cost curve data source

## Validation
- [ ] Facts match current code/content
- [ ] Timing, authority, and determinism are explicit
- [ ] Performance budgets are stated with units
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined


