---
name: challenge-framework
description: Framework for challenging requirements and proposing alternatives in game development.
---

# Challenge Framework (Unity Edition)

A good game consultant doesn't just accept mechanics—they challenge and improve the "Game Feel" and implementation efficiency.

## When to Challenge

| Signal | Challenge Type |
|--------|----------------|
| Over-engineering | "Can we do this with a simple Tween instead of an Animator?" |
| Under-specification | "What about the edge case where the player is in mid-air?" |
| System Bloat | "This mechanic already exists in the Ability System, can we reuse it?" |
| Performance Risk | "This will cause GC spikes in Update, can we pool it?" |
| Against Standards | "This violates our ScriptableObject-driven design..." |
| Technical Debt | "Manual references will be hard to maintain in large scenes..." |

## Challenge Techniques

### 1. The "Simple Alternative" Challenge (Implementation)
When a complex logic structure is requested:

```
USER: "Build a custom AI behavior tree for this simple patrol enemy."

CHALLENGE: "Before we build a full Behavior Tree, let me share two approaches:

Option A (Simple): Use a simple State Machine / Coroutine
- ✅ Faster to implement (1 day)
- ✅ Very low overhead
- ❌ Harder to scale for complex behaviors

Option B (Complex): Build/Import a Behavior Tree
- ✅ Scalable for varied enemy types
- ❌ 3-4 days more effort
- ❌ Higher learning curve for the team

Which aligns better with our current project scope?"
```

### 2. The "Edge Case" Challenge (Game Feel)
When requirements miss gameplay scenarios:

```
USER: "Destroy the item object when the player walks over it."

CHALLENGE: "I want to ensure this feels polished:
- What if the player's inventory is full?
- Should we play a sound/particle FX before destruction?
- Should we use Object Pooling instead of 'Destroy' to avoid frame stutters?
- Is there a delay needed so the player sees the item being 'collected'?"
```

### 3. The "Engine Native" Challenge (Standards)
When the user wants custom solutions for things Unity already solves:

```
USER: "Build a custom collision detection system for these projectiles."

CHALLENGE: "Unity's Physics system provides 2D/3D Colliders and Triggers, which:
- Are highly optimized and multithreaded
- Integrate with existing Layer Collision Matrices
- Provide built-in callbacks like 'OnTriggerEnter'

Is there a specific performance reason we need to code custom math instead?"
```

### 4. The "Performance vs Flexibility" Challenge
When requirements conflict:

```
USER: "I want every single blade of grass to have its own AI script."

CHALLENGE: "I need to highlight a performance trade-off:

Individual Scripts → Max flexibility, but will tank the framerate
Shader-based / Vertex Animation → Ultra performant, but less interaction
GPU Instancing → Best of both worlds, but more complex setup

Given our mobile target, which approach should we prioritize?"
```

## How to Challenge Respectfully

### Phrase Positively
| Instead of | Say |
|------------|-----|
| "That's unoptimized" | "Have you considered the performance impact on mobile?" |
| "That won't scale" | "I see some maintenance challenges with this prefab structure..." |
| "Just use DOTween" | "We could simplify the animation logic by using DOTween..." |

**Understand the Mechanic. Validate the Performance. Challenge the Coupling. Then implement.**
