# <Mechanic Name>

Owner: <team-or-person>
Status: draft | active | deprecated
Last verified: YYYY-MM-DD
Verified commit: <sha>
Target build: <engine-version + platform>

## Purpose
<Player-facing intent and design goal>

## Scope
- In scope: <items>
- Out of scope: <items>

## Source of Truth
- Code: `<path>` (<symbol/state>)
- Tests: `<path>` (<scenario>)
- Design: <doc-path-or-url#section>
- Data: `<path>` (<table/key>)
- Issue/ADR: <id>

## Inputs
- Input action: <name>
- Input conditions: <held/tap/cooldown/state constraints>
- Input buffering: <rules or Unknown>

## State Model
States:
- `<state>`: <entry condition>, <exit condition>
- `<state>`: <entry condition>, <exit condition>

Transitions:
1. `<state A>` -> `<state B>` when <condition>
2. `<state B>` -> `<state C>` when <condition>

## Timing Model
- Update domain: <frame/tick/fixed update>
- Tick rate: <value + unit>
- Order dependencies: <systems that must run before/after>

## Determinism
- Deterministic across clients: <yes/no/partial + reason>
- Sources of nondeterminism: <rng/time/input race or None>
- Mitigation: <seed/order/lockstep/snapshot or Unknown>

## Formulas
```txt
# partial
output = base_value * multiplier + bonus
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| <var> | <value> | <value> | <value> | <unit> | <path/key> |

## Edge Cases
- <edge case and expected behavior>
- <edge case and expected behavior>

## Failure Modes
- <failure mode>: <current behavior>

## Event Hooks
- Event: <name>, Trigger: <when>, Payload: <fields>

## Acceptance Tests
- Automated: <test name/path and expected result>
- Playtest: <scenario and success criteria>

## Missing Evidence
- <Unknown claim + missing artifact>

## Validation
- [ ] Facts match current code/content
- [ ] Timing and determinism assumptions are explicit
- [ ] Tuning variables map to actual data/config
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined
