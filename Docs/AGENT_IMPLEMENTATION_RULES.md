# Agent Implementation Rules

Owner: <team-or-person>
Status: active
Last verified: YYYY-MM-DD
Verified commit: <sha>
Target build: <engine-version + platform>

## Purpose
Define non-negotiable rules for LLM coding agents implementing game mechanics, systems,
and architecture in this repository.

## Source of Truth
- Code: `<path>`
- Tests: `<path>`
- Design: <doc-path-or-url#section>
- Data: `<path>`
- Issue/ADR: <id>

## Hard Constraints
1. Use only repository-grounded facts from code, tests, data, and provided design docs.
2. Do not invent APIs, classes, assets, or config keys.
3. If data is missing, write `Unknown` and list required artifacts.
4. Match engine version and target platform constraints in every change.
5. Preserve existing architecture and naming conventions unless task explicitly requests refactor.
6. For multiplayer code, do not violate documented authority boundaries.

## Implementation Workflow
1. Read the relevant spec template and existing implementation files.
2. Extract required constraints:
   - timing model
   - determinism requirements
   - authority model (if networked)
   - performance budget
3. Implement minimal viable change aligned to spec.
4. Add or update tests.
5. Update documentation evidence links and verification metadata.

## Code Change Requirements
- Include exact file paths and touched symbols in summary.
- Keep changes scoped to the requested mechanic/system.
- Avoid hidden coupling across unrelated modules.
- Add comments only for non-obvious logic.

## Gameplay Requirements
- Declare update domain: `frame`, `fixed update`, or `tick`.
- Keep tuning variables centralized in config/data.
- Include edge-case behavior for invalid input and state conflicts.

## Multiplayer Requirements
- Document ownership for each mutable state field.
- Define prediction and reconciliation behavior for player-facing actions.
- Ensure server-side validation for trust boundaries.

## Validation Requirements
- Automated test coverage for primary path and at least one edge case.
- Performance impact check against stated budget.
- Build/test commands and outcomes recorded in PR/notes.

## Output Format for Agent Delivery
- `Summary`
- `Files Changed`
- `Behavioral Impact`
- `Tests`
- `Open Unknowns`

## Missing Evidence
- <Unknown constraints + missing artifacts>

## Validation
- [ ] Rules align with current engine/project constraints
- [ ] Workflow is followed in implementation tasks
- [ ] Unknowns are explicitly labeled
- [ ] Multiplayer constraints are enforced when applicable
- [ ] Test and performance checks are included

