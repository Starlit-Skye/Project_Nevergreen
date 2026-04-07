# Game Documentation System

This repository uses a source-grounded documentation workflow specialized for game
development and LLM coding agents implementing mechanics, systems, and architecture.

## Quick Start
1. Read `docs/STYLE.md`.
2. Read `docs/SOURCES.md`.
3. Pick a template from `docs/templates/`.
4. Use `docs/prompts/llm-game-doc-writer.md` as the system/task prompt.
5. Fill in `Last verified` and `Verified commit` before merging.

## Core Document Types
- `MECHANIC_SPEC.md`: One mechanic with inputs, state logic, formulas, and tuning.
- `SYSTEM_SPEC.md`: One gameplay/system layer with ownership, data, update loop, and events.
- `ARCHITECTURE_SPEC.md`: Engine and module boundaries, lifecycle, performance budgets.
- `RUNBOOK_GAMEPLAY.md`: Live operations and incident procedures for game services/content.
- `NETWORKING_RULES.md`: Authority, prediction, reconciliation, replication constraints.
- `CONTENT_PIPELINE.md`: Asset conventions, import rules, validation, and CI gates.
- `AGENT_IMPLEMENTATION_RULES.md`: Non-negotiable rules for LLM implementation behavior.

## Required Metadata (All Docs)
- `Owner`
- `Status` (`draft` | `active` | `deprecated`)
- `Last verified` (YYYY-MM-DD)
- `Verified commit` (full or short SHA)
- `Target build` (engine version + platform)

## Global Rules
- No speculation. If a fact is unknown, write `Unknown` and list missing artifacts.
- Every non-trivial claim must be traceable to code, tests, design docs, ADRs, or tasks.
- Explicitly document determinism and timing assumptions for gameplay logic.
- Include acceptance criteria for both automated tests and playtest validation.
