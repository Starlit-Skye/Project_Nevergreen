---
name: problem-solving
description: Systematic techniques for when you're stuck on game mechanics or engine constraints. Use for complexity spirals, performance bottlenecks, and architectural debt.
---

# 🔧 Problem-Solving Skill (Unity Edition)

**Context:** Use when stuck on a game system or performance issue. Match your symptom to a technique.

## Quick Dispatch

| Symptom | Technique | Action |
|---------|-----------|--------|
| Mechanic has 10+ edge cases | **Simplification** | Find one insight that eliminates multiple cases |
| "Must use Physics for this" | **Inversion** | Flip core assumptions |
| Same issue appears in many places | **Meta-Pattern** | Find the universal pattern |
| Will this work with 1000 NPCs?| **Scale Test** | Test at 1000x and 0.01x |
| HUD/VFX feels "stale" | **Collision Thinking** | Mix unrelated concepts |

## Technique 1: Simplification Cascades

**When:** Code keeps growing, more special cases, "if (isJumping && !isFalling && hasUsedDoubleJump)..."

**Process:**
1. List all special cases
2. Ask: "What single truth would eliminate most of these?"
3. Refactor around that truth

**Unity Example:**
```
PROBLEM: 10 different jump/fall/grounded validation checks

INSIGHT: "Everything is just a transition in a CharacterState machine"

SOLUTION: One StateMachine with 'Transitions' based on input/physics
```

## Technique 2: Inversion Exercise

**When:** Solution feels forced, "must be done with Rigidbodies"

**Process:**
1. Identify core assumption
2. Ask: "What if the opposite were true?"
3. Explore what becomes possible

**Unity Example:**
```
ASSUMPTION: "We must use Raycasts on every frame to detect ground"

INVERT: "What if ground detection was event-based?"

RESULT: Use TriggerColliders on feet to toggle 'isGrounded' flag
```

## Technique 3: Meta-Pattern Recognition

**When:** Same problem in 3+ places, reinventing solutions

**Process:**
1. Identify 3+ similar problems
2. Find common pattern
3. Create reusable abstraction

**Unity Example:**
```
PATTERN: Player, Enemy, and Crate all need Health, Damage, and VFX

SOLUTION: Create an 'IDamageable' interface + 'Health' component
```

## Technique 4: Scale Game

**When:** Unsure about production readiness, framerate risks unclear

**Process:**
1. What if 1000x more entities?
2. What if 0.01x entities (minimal case)?
3. What breaks? What still works?

**Unity Example:**
```
QUESTION: "Will this projectile system work in a bullet-hell?"

TEST 1000x: 5000 Bullets → Massive lag, GC spikes
TEST 0.01x: 1 Bullet → Works but pooling setup is overhead

INSIGHT: Need Object Pooling and perhaps Job System for movement
```

## Technique 5: Collision Thinking

**When:** Visuals or HUD inadequacy, need a breakthrough

**Process:**
1. Take problem domain (A)
2. Pick unrelated concept (B)
3. Ask: "What if we treated A like B?"

**Unity Example:**
```
DOMAIN A: Screen-space damage indicators
CONCEPT B: Topographic weather maps

COLLISION: "What if damage indicators looked like heat-map pressure fronts?"

RESULT: Dynamic UI shader that ripples based on impact intensity
```

## Decision Tree

```
STUCK?
│
├─ Is the logic becoming a "Complexity Machine"? (Edge cases piling up)
│   └─ Yes → SIMPLIFICATION
│
├─ Stuck on a specific engine tool? (Must use Physics, Must use NavMesh)
│   └─ Yes → INVERSION
│
├─ Same logic repeated in multiple scripts/Prefabs?
│   └─ Yes → META-PATTERN
│
├─ Worried about high entity count / Framerate?
│   └─ Yes → SCALE TEST
│
├─ Game feel or HUD needs more "Juice"?
│   └─ Yes → COLLISION THINKING
│
└─ Still unsure → Start with SIMPLIFICATION (most common)
```

## Unity-Specific Stuck Patterns


| Stuck On | Try This |
|----------|----------|
| Update() spaghetti | Simplification → Event-driven architecture |
| Framerate drops | Scale Testing → Profiler + Object Pooling |
| Inspector clutter | Meta-Pattern → Custom PropertyDrawers / Odin |
| Physics glitches | Inversion → Kinematic movement vs Force-based |
| Complex HUD logic | Simplification → UI Toolkit / Responsive layout |
