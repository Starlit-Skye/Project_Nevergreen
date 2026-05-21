# Skill Context Runtime System

Owner: Combat Engineering Team
Status: active
Last verified: 2026-05-21
Verified commit: HEAD
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define the runtime `SkillContext` container used to execute one skill action in combat.

## Scope
- In scope: per-execution runtime fields, combat calculation fields, hit/crit flags, special
  interaction flags, status queueing, system references
- Out of scope: permanent character stats storage, static skill catalog storage, full combat pipeline
  orchestration

## Source of Truth
- Code: `Assets/Scripts/Combat/SkillContext.cs`
- Tests: `Assets/Editor/Tests/BuffDebuffTests.cs` (Specifically `StatusEffectOnly_StandaloneHitResolution_SucceedsBasedOnAccuracyAndDodge` and `StatusEffectOnly_StandaloneHitResolution_FailsOnMiss`)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: Technical -> Skill Context, Combat Calculation, Critical System, Hit Resolution)
- Data: `Assets/docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md` (runtime stat source contract)
- Issue/ADR: Unknown

## Responsibilities
- Hold temporary per-execution skill data, rebuilt for each skill use.
- Provide shared mutable runtime values for combat calculations and effect interactions.
- Consume runtime-resolved character stats supplied from Character Database lookups.
- Carry hit/crit resolution fields and special interaction flags for guard/dodge/defense logic.
- Queue pending status applications for ordered post-resolution application.
- Expose lazy, unified hit resolution via `EnsureHitResolved(CombatCharacter target)` (delegated to `CombatCalculator.ResolveHit`) to support standalone hit check evaluation for status-only skills.
- Expose extension storage for edge-case mechanics without changing core fields.

## Data Model
- Entity/component/object: `SkillContext` with `user`, `skill`, `targets`, `primary_target`,
  `base_attack_roll`, `skill_scaling`, `calculated_damage`, `damage_multiplier`, `is_critical`,
  `crit_multiplier`, `final_accuracy`, `did_hit`, `hasResolvedHit`, `ignores_defense`, `ignores_dodge`,
  `guaranteed_hit`, `bypass_guard`, `total_hits`, `current_hit_index`, `pending_statuses`,
  `battle_system_ref`, `rng_ref`, `extra`
- Field explanation: `user` is the acting character for the current skill execution.
- Field explanation: `skill` is the static skill definition selected for execution.
- Field explanation: `targets` is the full target list affected by the skill.
- Field explanation: `primary_target` is the main target used by single-target-priority logic.
- Field explanation: `base_attack_roll` is the randomized attack value before skill scaling.
- Field explanation: `skill_scaling` is the skill multiplier applied to offensive/healing output.
- Field explanation: `calculated_damage` is the pre-application damage value after calculation stages.
- Field explanation: `damage_multiplier` is an additional stackable multiplier used by effects/buffs.
- Field explanation: `is_critical` stores critical-hit outcome for reuse across effect stages.
- Field explanation: `crit_multiplier` is the critical damage multiplier (default from GDD is `1.5`).
- Field explanation: `final_accuracy` is the post-modifier hit chance used for hit/miss resolution.
- Field explanation: `did_hit` is the resolved hit/miss result gate for downstream effects.
- Field explanation: `hasResolvedHit` indicates whether the hit check has already been evaluated and resolved for the current context.
- Field explanation: `ignores_defense` marks whether defense/protection is bypassed.
- Field explanation: `ignores_dodge` marks whether dodge checks are bypassed.
- Field explanation: `guaranteed_hit` marks unconditional hit behavior independent of accuracy.
- Field explanation: `bypass_guard` marks whether guard/redirection is bypassed.
- Field explanation: `total_hits` stores configured multi-hit count for this execution.
- Field explanation: `current_hit_index` stores the currently processed hit in a multi-hit sequence.
- Field explanation: `pending_statuses` queues statuses for ordered application after resolution.
- Field explanation: `battle_system_ref` exposes runtime battle-system context for interactions.
- Field explanation: `rng_ref` is the random source used for reproducible combat rolls.
- Field explanation: `extra` is an extension dictionary for special-case mechanic data.
- Method explanation: `EnsureHitResolved(CombatCharacter target)` performs a lazy/cached hit check using `CombatCalculator.ResolveHit`. Healing/allied/self skills automatically hit, whereas other targets undergo standard accuracy vs dodge calculation.
- Persistence keys: none (runtime-only object by design)

## Event Contracts
- Event: `skill_context_created`
- Producer: skill execution entrypoint
- Consumers: combat calculation and effect subsystems
- Payload schema: actor id, skill id, target ids, turn index

- Event: `skill_hit_resolution_completed`
- Producer: hit-resolution stage
- Consumers: damage/status application stages
- Payload schema: skill id, did hit, is critical, final accuracy, affected targets

- Event: `pending_statuses_queued`
- Producer: skill/status effect stage
- Consumers: status application stage
- Payload schema: skill id, status entries, target ids

## Timing Model
- Update domain: turn-based action execution
- Tick/update order: create context -> calculate hit/crit/damage -> queue statuses -> finalize action
- Budget: Unknown

## Determinism
- Required: partial
- Strategy: deterministic mutation order within one skill resolution flow
- Known exceptions: RNG-driven attack roll and hit/crit outcomes

## Authority Model
- Single-player/offline: context is created and consumed by local combat runtime
- Multiplayer: Unknown

## Performance Budget
- CPU: Unknown
- Memory: Unknown
- Entity scale target: Unknown

## Error Handling and Recovery
- Null or invalid target references: Unknown
- Missing required context field during resolution: Unknown
- Recovery strategy: Unknown

## Observability
- Metrics: context allocations per battle, average modifiers applied per action, miss/crit rates
- Logs: context creation failures, invalid field mutations, unresolved pending statuses
- Traces/profilers: Unknown

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify skills resolve consistently across multi-target, multi-hit, crit, and bypass-guard
  scenarios; verify status queue behavior aligns with expected turn timing

## Missing Evidence
- Concrete runtime class/file path for `SkillContext`
- Confirmed resolution ordering contract between combat subsystems
- Guard/ignore flag precedence rules when multiple flags are set
- Automated test suites for multi-hit and status-queue interactions

## Validation
- [ ] Facts match current code/content
- [ ] Timing, authority, and determinism are explicit
- [ ] Performance budgets are stated with units
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined


