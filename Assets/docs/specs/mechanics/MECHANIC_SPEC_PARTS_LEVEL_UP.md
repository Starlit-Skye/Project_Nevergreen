# Parts Level-Up Mechanic

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define how `Parts` are spent to increase character levels, including progressive cost and global max
level enforcement for marionettes.

## Scope
- In scope: level-up purchase validation, `Parts` cost consumption, level increment, global level cap
- Out of scope: battle reward generation, scraps spending, UI layout behavior

## Source of Truth
- Code: `Unknown` (level-up implementation not provided)
- Tests: `Unknown` (level-up tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: Economy, Marionettes)
- Data: `Assets/docs/specs/systems/SYSTEM_SPEC_ECONOMY_RUNTIME.md` (`LevelUpCostCurve`), `Assets/docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md` (`global_max_level`)
- Issue/ADR: Unknown

## Inputs
- Input action: player requests level-up for Ceci or selected marionette
- Input conditions: sufficient `Parts`; target level below cap where applicable
- Input buffering: Unknown

## State Model
States:
- `AwaitLevelUpRequest`: no pending purchase
- `ValidateLevelUp`: check `Parts` and level-cap constraints
- `ApplyLevelUp`: consume `Parts`, increment level
- `LevelUpCommitted`: stat/UI update and event emitted

Transitions:
1. `AwaitLevelUpRequest` -> `ValidateLevelUp` when level-up action is invoked
2. `ValidateLevelUp` -> `ApplyLevelUp` when request is valid
3. `ValidateLevelUp` -> `AwaitLevelUpRequest` when request is invalid
4. `ApplyLevelUp` -> `LevelUpCommitted` when spend and level update succeed

## Timing Model
- Update domain: event-driven during run progression
- Tick rate: once per valid level-up action
- Order dependencies: validate cost/cap before mutating level and currency; resolve new stat block after
  level increment using Character Database index rule

## Determinism
- Deterministic across clients: yes (given same input state)
- Sources of nondeterminism: None
- Mitigation: deterministic cost lookup from progression curve

## Formulas
```txt
# level-up eligibility
can_level_up = (parts_current >= parts_cost_for_next_level)

# marionette cap constraint
can_level_up = can_level_up AND (current_level < global_max_level)

# state mutation
parts_after = parts_current - parts_cost_for_next_level
level_after = current_level + 1
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `parts_cost_for_next_level` | Unknown | Unknown | Unknown | parts | GDD Economy |
| `cost_progression_curve` | increasing | Unknown | Unknown | curve | GDD Economy |
| `global_max_level` | Unknown | 1 | Unknown | level | GDD Marionettes |

## Edge Cases
- Level-up request with insufficient `Parts` must not mutate state.
- Marionette level-up request at `global_max_level` must not increase level.
- Repeated rapid level-up requests should not double-apply a single purchase action.

## Failure Modes
- Currency spend occurs without corresponding level increase
- Level increases past `global_max_level` for marionettes
- Incorrect cost curve lookup for next-level price

## Event Hooks
- Event: `level_up_purchased`, Trigger: successful level-up commit, Payload: character id, old level,
  new level, parts spent, parts remaining
- Event: `level_up_rejected`, Trigger: failed validation, Payload: character id, reason (`insufficient_parts`/`level_cap_reached`/Unknown)

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify progressively increasing `Parts` costs, verify blocked level-up when `Parts` are
  insufficient, verify marionette cap enforcement at `global_max_level`, and verify stat update after
  valid level-up

## Missing Evidence
- Concrete progressive cost table/curve values
- Exact cap applicability for Ceci versus marionettes in code
- Idempotency/anti-double-spend handling details
- Runtime binding path for level-up button to economy/progression logic

## Validation
- [ ] Facts match current code/content
- [ ] Timing and determinism assumptions are explicit
- [ ] Tuning variables map to actual data/config
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined

