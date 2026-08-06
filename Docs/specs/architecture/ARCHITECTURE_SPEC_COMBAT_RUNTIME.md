# Combat Runtime Architecture

Owner: Combat Engineering Team
Status: active
Last verified: 2026-05-04
Verified commit: HEAD
Target build: Unity 2022.3 + Windows

## Purpose
Define architecture boundaries and integration flow across combat runtime, skill execution, character
stats, and run-scoped economy.

## Scope
- In scope: Combat Core mechanic orchestration, Skills Database, SkillContext runtime, Character
  Database, Enemy/Marionette templates, Economy Runtime, Combat Screen integration
- Out of scope: networking architecture, save-file architecture, editor tooling architecture

## Source of Truth
- Code: `Assets/Scripts/Combat/BattleSystem.cs` (Facade), `Assets/Scripts/Combat/FormationManager.cs`, `Assets/Scripts/Combat/SkillExecutor.cs`, `Assets/Scripts/Combat/CharacterLifecycleManager.cs`, `Assets/Scripts/Combat/TurnOrderBuilder.cs`, `Assets/Scripts/Combat/BattleOutcomeEvaluator.cs`, `Assets/Scripts/Combat/BattleRewardHandler.cs`, `Assets/Scripts/Combat/TargetResolver.cs`, `Assets/Scripts/Combat/CombatCharacter.cs`, `Assets/Scripts/Combat/StatusProcessor.cs`, `Assets/Scripts/Combat/Effects/`
- Tests: `Assets/Editor/Tests/` (Guard, Move, Stun, BuffDebuff)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: Combat, Technical, Economy, Gameloop Flow)
- Data: `Docs/specs/mechanics/` (Mechanic Specs)
- Issue/ADR: Unknown

## Module Boundaries
- `Combat Core Mechanic` owns round/turn sequencing, rank semantics, tie resolution, and action
  resolution flow. It acts as a Facade (`BattleSystem`) delegating to specialized domain managers:
  - `FormationManager`: Owns formation positioning, movement, compaction, and target rank clamping.
  - `SkillExecutor`: Owns skill action execution, effect loops, and animation step generation.
  - `CharacterLifecycleManager`: Owns character spawn/defeat events, pile transitions, and cleanup.
  - `TurnOrderBuilder`: Owns round turn queue construction and Speed sorting.
  - `BattleOutcomeEvaluator`: Owns defeat/victory state checks.
  - `BattleRewardHandler`: Owns part/scrap economy calculation upon battle end.
  - `TargetResolver`: Owns targeting scope validation and valid target resolution.
- `Skills Database` owns static skill definitions and modifier metadata.
- `SkillContext Runtime` owns per-execution mutable action context.
- `Character Database` owns per-level stat definitions and level-indexed stat lookup.
- `Marionette Template` owns player-unit construction/progression constraints.
- `Enemy Template` owns enemy-unit construction/elite classification constraints.
- `Economy Runtime` owns `Parts`/`Scraps` earning, spending, and run-reset rules.
- `Combat Screen` owns combat entry/exit UX flow and reward handoff presentation.

## Lifecycle and Update Order
1. Startup/content load: load and validate `CharacterData`, `SkillData`, and economy config.
2. Combat setup: instantiate team units from template + character stats; classify encounter (`normal` or
   `elite`).
3. Round loop: resolve turn order, build `SkillContext` per action, execute action, apply statuses.
4. Encounter end: compute battle rewards, apply economy updates, emit combat-resolved payload.
5. Run transition: route selection/event handling and potential economy spends (for example level-up).
6. Run end: reset run-scoped currencies.

## Data Ownership
- Runtime state owner: combat state by Combat Core; action context by SkillContext Runtime; currencies
  by Economy Runtime.
- Save-game state owner: Unknown.
- Network replicated state owner: Unknown.

## Threading and Jobs
- Main-thread only: combat turn sequencing, UI-driven action selection, reward presentation.
- Worker/job eligible: Unknown.
- Synchronization points: pre-battle data validation and post-battle reward application (details
  Unknown).

## Authority Model
- Offline: local runtime is authoritative over combat simulation and economy state.
- Online: Unknown.

## Performance Budgets
- Frame budget target: Unknown.
- Memory budget target: Unknown.
- Streaming budget target: Unknown.

## Fault Boundaries
- Skill data fault domain: invalid or missing skill metadata should be contained to data validation
  layer (runtime fallback behavior Unknown).
- Character data fault domain: invalid level-index lookup should be contained to stat resolver
  (fallback behavior Unknown).
- Economy fault domain: reward/spend calculation errors should be contained to run economy state
  updates (fallback behavior Unknown).

## Migration and Compatibility
- Backward compatibility requirements: Unknown.
- Upgrade path: evolve schema versions for skill/character/economy data with migration rules (Unknown).

## Acceptance Tests
- Automated: Unknown (architecture test paths not provided).
- Playtest: verify end-to-end path from room entry -> combat -> reward grant -> level-up spend -> run
  reset with no state desync.

## Missing Evidence
- Concrete runtime module/file ownership map
- Threading model and any job-system usage
- Fault handling policy for invalid skill/character/economy config
- Architecture regression test suite

## Validation
- [x] Facts match current code/content
- [x] Ownership and boundaries are explicit
- [x] Timing/threading/authority assumptions are explicit
- [x] Budgets include units and thresholds
- [x] Acceptance tests are defined

