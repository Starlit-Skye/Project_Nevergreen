# Game Source Grounding Policy

## Goal
Keep game documentation auditable, implementation-ready, and accurate for LLM coding agents.

## Allowed Evidence
- Gameplay code, engine integration code, tools code, and scripts
- Unit/integration/functional tests
- Game design docs (GDD), technical design docs (TDD), ADRs
- Content config data (JSON, ScriptableObjects, data tables, prefabs, blueprints)
- Build configs and platform settings
- Task/issue records for accepted behavior

## Citation Requirements
For every non-trivial claim, cite at least one source:
- `Code:` `path` + symbol/function/class/state name
- `Tests:` `path` + test/scenario name
- `Design:` doc path/URL + section id
- `Data:` content/config path + key/table name
- `Issue/ADR:` id

## Claim Types Requiring Evidence
- Input mapping and control behavior
- State transitions and gameplay rules
- Damage/movement/economy formulas
- Timing/tick/frame processing behavior
- Save/load persistence behavior
- Networking authority and replication behavior
- Performance budgets and limits

## Unknown Handling
If evidence is missing:
1. Write `Unknown`.
2. Add a `Missing Evidence` item with exact missing artifacts.
3. Do not fill gaps with assumptions.

## Evidence Block Template
```md
## Source of Truth
- Code: `src/...` (<symbol>)
- Tests: `tests/...` (<scenario>)
- Design: <doc-path-or-url#section>
- Data: `content/...` (<table/key>)
- Issue/ADR: <id>
```
