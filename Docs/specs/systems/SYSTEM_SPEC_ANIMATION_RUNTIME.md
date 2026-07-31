# Combat Animation Runtime System

Owner: Unknown
Status: draft
Last verified: 2026-04-12
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define runtime queueing, sequencing, and safeguard behavior for combat skill animations and
combat UI update animations.

## Scope
- In scope: animation queue ordering, queue item identity, input lock behavior, queue-length
  safeguard, lock-time safeguard, unlock conditions
- Out of scope: animation asset authoring, tween curve tuning, VFX/SFX content creation, cutscenes

## Source of Truth
- Code: `Unknown` (animation queue runtime implementation not provided), `Assets/Scripts/Prototype/CombatUI.cs`
  (current combat input flow), `Assets/Scripts/Combat/BattleSystem.cs` (current action submission flow)
- Tests: `Unknown` (animation queue tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.2id7yg1ohd4o
  (sections: Technical -> Animations, Inputs and Interactions -> Animations)
- Data: `Unknown` (animation queue config asset not provided),
  `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_INPUT_INTERACTIONS.md` (input gating and selection flow)
- Issue/ADR: Unknown

## Responsibilities
- Store queued animations as ordered entries with animation id and animation name.
- Resolve skill execution animations via `SkillData.animationClip` reference, using `clip.name` and `clip.length` (logging an error and falling back to generic `Cast`/`Attack` if unassigned).
- Resolve target flinch (TakeDamage) animations via `CharacterData.takeDamageClip` reference, using `clip.name` and `clip.length` (falling back to `"TakeDamage"` and `0.5f` if unassigned).
- Play queued animations in FIFO order.
- Lock combat inputs while queue size is greater than `0`.
- Dequeue current entry when its animation finishes.
- Process chained animations without unlocking input between items.
- Apply queue-length safeguard:
  if queue size reaches `15`, clear queue immediately and unlock inputs.
- Apply lock-time safeguard:
  track expected animation length; if actual lock time exceeds expected length by `5 s`, clear queue
  and unlock inputs.
- Reset expected animation length and lock-time tracker whenever inputs unlock.
- Ensure lock-time tracker does not run while queue size is `0`.

## Data Model
- Entity/component/object: `AnimationQueueEntry` with `animationId`, `animationName`, `durationSeconds`
- Entity/component/object: `AnimationQueueState` with `queueCount`, `isInputLocked`, `expectedLengthSeconds`,
  `lockElapsedSeconds`
- Persistence keys: Unknown

## Event Contracts
- Event: `combat_animation_enqueued`
- Producer: combat action resolution and combat UI update pipeline
- Consumers: animation queue runtime, input lock gate
- Payload schema: animation id, animation name, duration seconds, queue size after enqueue

- Event: `combat_animation_finished`
- Producer: animation playback runtime
- Consumers: animation queue runtime
- Payload schema: animation id, animation name, elapsed seconds

- Event: `combat_input_lock_changed`
- Producer: animation queue runtime
- Consumers: combat input interaction layer
- Payload schema: is locked, queue count, expected length seconds, lock elapsed seconds

- Event: `combat_animation_safeguard_triggered`
- Producer: animation queue runtime
- Consumers: observability/logging, combat input interaction layer
- Payload schema: safeguard type (`queue_cap` or `lock_overtime`), queue count, expected length seconds,
  lock elapsed seconds

## Timing Model
- Update domain: runtime animation queue processing during combat action resolution
- Tick/update order: enqueue skill animation -> enqueue UI update animations -> process queue in order
  -> dequeue each finished animation -> unlock input when queue reaches `0`
- Budget: Unknown

## Determinism
- Required: yes
- Strategy: FIFO queue order and deterministic lock/unlock conditions from queue state
- Known exceptions: exact clip completion callback timing precision is Unknown

## Authority Model
- Single-player/offline: local runtime owns queue, lock state, and safeguard execution
- Multiplayer: Unknown

## Performance Budget
- CPU: Unknown
- Memory: Unknown
- Entity scale target: queue cap `15` entries

## Error Handling and Recovery
- Queue overflow (>= `15`): clear queue, unlock input, reset expected length and lock timer trackers
- Lock overtime (> expected + `5 s`): clear queue, unlock input, reset expected length and lock timer trackers
- Missing animation id/name mapping: Unknown

## Observability
- Metrics: queue size over time, lock duration, safeguard trigger count by type
- Logs: enqueue/dequeue events, lock state transitions, safeguard triggers
- Traces/profilers: Unknown

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify skill execution locks input; verify chained skill and UI update animations stay
  locked until all queued animations complete; verify queue-cap safeguard clears queue and unlocks;
  verify lock-overtime safeguard unlocks when lock exceeds expected length plus `5 s`; verify lock
  timer does not progress while queue is empty

## Missing Evidence
- Concrete animation queue runtime implementation path and symbols
- Queue storage type and update loop integration point
- Canonical source for animation id/name and duration data
- Automated tests for safeguard behavior and lock transition ordering

## Validation
- [ ] Facts match current code/content
- [ ] Timing, authority, and determinism are explicit
- [ ] Performance budgets are stated with units
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined
