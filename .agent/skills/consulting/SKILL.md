---
name: consulting
description: Game development consulting protocols for requirements gathering, mechanic design, and proposing technical alternatives. Use before implementing any significant system.
---

# 🤝 Consulting Skill (Unity Edition)

**Context:** Use this skill before implementing systems to ensure game design requirements are complete and the technical solution is performant and scalable.

## Core Principle

> **"A good game consultant doesn't just code the mechanic—they ensure the mechanic fits the core loop and the technical constraints."**

## Three Protocols

| Protocol | When | Reference |
|----------|------|-----------|
| Mechanics Gathering | Before any gameplay implementation | `resources/requirements-gathering.md` |
| Technical Consulting | Before architecture/library choices | `resources/technical-consulting.md` |
| Challenge Framework | When proposing simpler/more performant alternatives | `resources/challenge-framework.md` |

## Quick Decision Tree

```
NEW MECHANIC/SYSTEM REQUESTED
│
├─ Is the "Game Feel" defined? (Triggers, Feedback, Timing)
│   ├─ No  → Mechanics Gathering Protocol
│   └─ Yes → Continue
│
├─ Are there technical constraints? (Platform, Framerate, Memory)
│   ├─ Yes → Technical Consulting (Performance Check)
│   └─ No  → Continue
│
├─ Is this the most performant/decoupled approach?
│   ├─ Unsure → Challenge Framework (SO-driven? Observer pattern?)
│   └─ Yes    → Continue
│
└─ IMPLEMENT (with validated requirements & performance plan)
```

## Game Consulting Checklist

Before implementing, verify:

- [ ] **Core Loop Fit** — Does this mechanic support the primary gameplay loop?
- [ ] **Data vs Logic** — Is the data stored in **ScriptableObjects** for easy balancing?
- [ ] **Lifecycle management** — When does this initialize (Awake, Start)? Does it need `OnDestroy` cleanup?
- [ ] **Visual/Audio Feedback** — What happens visually/audibly when this triggers? (Juice!)
- [ ] **Success criteria** — Is it a "Debug.Log" success or a functional visual prototype?
- [ ] **Performance Risk** — Will this cause GC allocations in `Update` or Draw Call spikes?

## Unity-Specific Questions

When gathering requirements for Unity features:

| Feature Type | Questions to Ask |
|--------------|------------------|
| **Core Mechanic** | Input source? Physics or Transform? Networked? |
| **Data System** | Persisted to JSON? Balanceable via SO? Async load needed? |
| **UI/UX** | Screen overlay or World Space? Resolution scaling? DOTween transitions? |
| **System Tool** | Custom Inspector needed? Editor Window or Gizmos? |
| **VFX/SFX** | Particle system or Shader? Layer sorting? Static/Dynamic audio? |

## Bottom Line

1. **Ask** about "Game Feel" before implementation.
2. **Validate** performance costs before building.
3. **Propose** decoupled architectures (Events/SOs).
4. **Challenge** rigid inheritance or high-coupling designs.

**Understand the Loop. Validate the Performance. Challenge the Coupling. Then implement.**
