---
name: requirements-gathering
description: Protocol for validating and completing game mechanic and system requirements before implementation.
---

# Mechanics Gathering Protocol (Unity Edition)

Before implementing any game system or mechanic, ensure requirements are complete using this protocol to avoid "feature creep" and technical debt.

## The 5W Framework (Game Design focus)

For every mechanic request, validate:

| Question | Purpose | Red Flag if Missing |
|----------|---------|---------------------|
| **WHO** | Who/what triggers this? (Player, NPC, System event) | 🔴 Ambiguous input source |
| **WHAT** | What exactly happens? (Input -> Logic -> Feedback) | 🔴 Vague "game feel" |
| **WHEN** | Frame timing? Coroutine vs Task? State dependency? | 🔴 Race conditions |
| **WHERE** | Local object space vs World space vs Inventory? | 🔴 Coordinate system mismatch |
| **WHY** | Which part of the Core Loop does this serve? | 🔴 Feature bloat |

## Requirement Completeness Checklist

### Mechanic & Logic Requirements
- [ ] **Input Source** identified (New Input System, Legacy, or Event?)
- [ ] **State Machine** transitions defined (Idle -> Active -> Cooldown)
- [ ] **Physics vs Transform** decided (Rigidbody physics or direct position?)
- [ ] **Parameters** identified for balancing (Speed, Force, Damage)
- [ ] **Dependency** list (Does it need a CharacterController? A Health component?)

### Data & Persistence Requirements
- [ ] **ScriptableObject** template defined for configuration
- [ ] **Save State** identified (Does this variable need to persist between sessions?)
- [ ] **Serialization** method clear (JSON, Binary, PlayerPrefs?)
- [ ] **Runtime modifications** handled (Does it reset on scene load?)

### Visual & Audio (Juice) Requirements
- [ ] **Visual Feedback** triggers (Particle FX, Animator parameters, Material swaps)
- [ ] **Audio Hooks** defined (SFX on trigger, loop behavior)
- [ ] **Tweening** behavior specified (DOTween ease type, duration)
- [ ] **Camera** impact (Screenshake? Field of View shift?)

### Performance & Platform Requirements
- [ ] **Memory Impact** (GC allocations in `Update`? Object Pooling needed?)
- [ ] **Draw Call** impact (New dynamic lights? Heavy batching changes?)
- [ ] **Platform Targets** (Mobile safe areas? Console controller remapping?)

## Gap Identification Questions

When requirements seem incomplete, ask:

### Scope Gaps
- "What's explicitly OUT of scope for this feature?"
- "Which related features should we NOT touch?"
- "Is this a complete feature or phase 1 of something larger?"

### System Integration Gaps
- "Does this mechanic need to trigger events in other systems (e.g., Audio, Inventory, Quests)?"
- "How does this system communicate with the **Save Manager** or **Game Overlord**?"
- "Are there any shared resources (e.g., Addressables, Object Pools) this system needs to access?"

### "Game Feel" Gaps

- "What happens if the player triggers this while in [specific state]?"
- "Is there a 'coyote time' or buffer needed for this input?"
- "What is the specific 'easing' for the movement (Smooth vs Snappy)?"

### Edge Case Gaps (Unity Specific)
- "What happens if the component is disabled mid-process?"
- "How should we handle a scene transition during this action?"
- "What if the target object is destroyed while the coroutine is running?"

### Performance Gaps
- "Will this run efficiently if we have [X] instances in one scene?"
- "Can this logic be processed in a Job System or is it Main-Thread dependent?"

## Output: Mechanics Summary

Before implementing, create a brief summary:

```markdown
## Mechanics Summary: [System/Mechanic Name]

### Validated Logic
- **Input:** [e.g., Spacebar (New Input System)]
- **Logic:** [e.g., Apply upward force to Rigidbody2D]
- **Juice:** [e.g., Instantiate 'Jump' particles at feet, Play 'boing' SFX]

### Assumptions & Technical Choices
- Using **DOTween** for the squash/stretch effect.
- Data driven via `JumpConfig` **ScriptableObject**.
- Assuming [X] because [reason]

### Performance Constraints
- Using **Object Pooling** for particles to avoid GC spikes.

### Out of Scope
- [What we're NOT doing]

### Questions Still Open
- [Any remaining unknowns]
```
