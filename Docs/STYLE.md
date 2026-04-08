# Game Documentation Style Guide

## Purpose
Define deterministic Markdown standards for game engineering documentation used by humans
and LLM coding agents.

## Non-Negotiable Rules
1. Use repository-grounded facts only.
2. Do not infer mechanics/system behavior without evidence.
3. If uncertain, write `Unknown` and add a `Missing Evidence` entry.
4. Keep section order identical to each selected template.
5. Use fenced code blocks with explicit language tags.
6. Every mechanic/system doc must declare timing assumptions.
7. Every multiplayer doc must declare authority boundaries.

## Writing Standards
- Voice: concise, technical, direct.
- Tense: present tense for current behavior.
- Terms: use exact code identifiers and content names used in project files.
- Units: always include units (`ms`, `Hz`, `m`, `deg`, `frames`, `ticks`).
- Paths: use repo-relative paths in backticks.

## Markdown Conventions
- Single `#` title per file.
- H2 for major structure.
- Flat bullet lists only.
- Tables allowed for tuning variables, state transitions, and event contracts.
- Line length target: 100 characters.

## Required Frontmatter Block
Include this block under the title:

```md
Owner: <team-or-person>
Status: draft | active | deprecated
Last verified: YYYY-MM-DD
Verified commit: <sha>
Target build: <engine-version + platform>
```

## Game-Specific Required Sections
All gameplay specs must include:
- `Determinism`
- `Timing Model`
- `Tuning Variables`
- `Edge Cases`
- `Acceptance Tests`

Multiplayer specs must also include:
- `Authority Model`
- `Prediction/Reconciliation`
- `Replication Rules`

## Validation Checklist (Required)
Every doc must end with:

```md
## Validation
- [ ] Facts match current code/content
- [ ] Timing and determinism assumptions are explicit
- [ ] Unknowns are explicitly labeled
- [ ] Links and paths resolve
- [ ] Acceptance tests are defined
```
