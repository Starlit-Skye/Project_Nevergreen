---
name: brainstorm
description: Structured ideation with brutal honesty for game systems. Use for architecture decisions, mechanic exploration, and technical feasibility.
---

# 🧠 Brainstorming Skill (Unity Edition)

**Context:** Use when exploring solutions BEFORE implementing. Debate mechanics, challenge assumptions, and find the most performant approach.

## Core Principles

- **YAGNI** — You Aren't Gonna Need It (Avoid "Future-Proof" bloat)
- **KISS** — Keep It Simple, Stupid (Prefer simple Tweens over complex Animators if possible)
- **DRY** — Don't Repeat Yourself (Use Prefabs and ScriptableObjects)

## ❌ DO NOT

- Implement solutions (only brainstorm and advise)
- Accept the first idea without exploring performance trade-offs
- Over-engineer for hypothetical player behaviors

## ✅ DO

- Ask clarifying questions about "Game Feel" and "Feedback"
- Challenge user assumptions about physics vs transforms
- Present 2-3 options with performance and maintenance trade-offs
- Be brutally honest about technical debt

## Brainstorming Process

```
1. SCOUT        → Understand current state (read guidelines, check existing Prefabs/Scripts)
2. CLARIFY      → Ask questions about "Game Feel", target framerate, and platform
3. RESEARCH     → Check Unity standards, existing patterns (SO Architecture, DOTween)
4. ANALYZE      → Evaluate 2-3 approaches using "Engine-Native" thinking
5. DEBATE       → Present options, challenge decoupling/coupling, find optimal
6. DOCUMENT     → Create summary with agreed solution + performance rationale
```

## Unity-Specific Questions

| Area | Questions to Explore |
|------|---------------------|
| **Mechanic Design**| Physics-based or Transform-based? Event-driven or Polling? |
| **Data Storage** | ScriptableObjects (static) vs JSON (dynamic)? Managed via Singleton? |
| **Performance** | Update loop or Coroutine? Object Pooling needed? Draw call counts? |
| **Integration** | Event Manager or Direct References? Interface-based systems? |
| **Visuals** | Shader-based or Particle-based? UGUI or UI Toolkit? |

## Trade-off Framework

For each option, evaluate:

| Dimension | Consider |
|-----------|----------|
| **Effort** | Development time, Prefab setup difficulty |
| **Complexity** | Code maintainability, "Spaghetti" coupling |
| **Performance** | Frame time (ms), Memory/GC allocations (B) |
| **Scalability** | Entity count (10 vs 1000), Scene complexity |
| **Native-Standard**| Follows Unity best practices? (Standard vs Hacky) |
| **Flexibility** | Balanceability via Inspector, future extensions |

## Output Template

```markdown
## Brainstorm: [Mechanic/System]

### Problem Statement
[What part of the game loop/experience are we solving?]

### Options Evaluated

#### Option A: [Name]
- **Effort:** Low/Medium/High
- **Pros:** [List]
- **Cons:** [e.g., Performance cost, GC spikes]

#### Option B: [Name]
- **Effort:** Low/Medium/High
- **Pros:** [List]
- **Cons:** [e.g., High maintenance, rigid coupling]

### Recommendation
[Chosen option] because [rationale].

### Questions
1. [Clarifying question]
2. [Clarifying question]

### Next Steps
1. [Action item]
2. [Action item]
```

## Example (Unity)

```
USER: "Should we use a Singleton Manager or ScriptableObject Events for player damage?"

BRAINSTORM RESPONSE:
"Let me explore options:

**Option A: HealthManager Singleton**
- ✅ Centralized control, easy to check state from anywhere
- ❌ Tight coupling (everything needs to know about HealthManager)
- ❌ Harder to scale for multiple damageable entity types

**Option B: ScriptableObject Game Events**
- ✅ Extremely decoupled (Player raises 'OnDamage' event, UI/VFX listen)
- ✅ Scalable (Any entity can use the same event system)
- ❌ harder to 'trace' logic in the Inspector for beginners

**Recommendation:** Option B — follows our decoupled Unity Architecture standards, reduces technical debt, and allows for easier secondary effects (VFX/SFX) without touching the Health code.

**Questions:**
1. Do you need multiple listeners (VFX, UI, Camera) for the damage event?
2. Are you comfortable with a slight learning curve for ScriptableObject-driven events?

**Next Steps:**
1. Create a `GameEvent` ScriptableObject template.
2. Implement the `Raise()` and `RegisterListener()` logic.
```
