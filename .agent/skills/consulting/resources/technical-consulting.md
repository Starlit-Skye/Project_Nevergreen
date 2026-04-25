---
name: technical-consulting
description: Unity technical architecture decision framework. Use before choosing between native Unity features, third-party libraries, or custom implementations.
---

# Technical Consulting: Unity Architecture Framework

## Core Principle

> **"Use Unity for what it's good at. Use libraries for what it's missing. Build custom for what the engine can't do."**

## The Decision Ladder (Unity)

Before implementing ANY solution, climb this ladder from bottom to top. **Stop at the first rung that solves your problem efficiently.**

```
Rung 5: Custom Low-Level (Job System, Compute Shaders, Native Plugins)
Rung 4: Trusted 3rd Party Lib (DOTween, Newtonsoft.Json, Shapes, Odin)
Rung 3: Unity Advanced Module (Addressables, New Input System, VFX Graph)
Rung 2: Unity Native Feature (UGUI/UI Toolkit, Animator, Physics, Navigation)
Rung 1: System Already Exists (Logic already in GameDataManager, SkillSystem, etc.)
```

**Every rung you skip MUST be justified.** If you jump to Rung 5 (Custom Engine logic) without checking Rungs 1-4, you are likely over-engineering and increasing maintenance debt.

---

## Protocol: Check Unity Capabilities

Before proposing an approach, verify if Unity already provides a performant solution:

| Need | Unity Answer (Standard) | When to Go Custom / 3rd Party |
|---|---|---|
| **Animation** | Animator / Timeline | When you need procedural tweening (**DOTween**) |
| **Input** | New Input System | When you need ultra-specific low-level raw device access |
| **Logic Flow** | Coroutines / UniTask | When you need heavy multi-threading (Job System) |
| **UI** | UGUI / UI Toolkit | When you need complex vector shapes (**Shapes**) |
| **Data Config** | **ScriptableObjects** | When you need runtime-editable JSON from a server |
| **Asset Management** | **Addressables** | When you have static scenes with no memory constraints |
| **Events** | UnityEvents / C# Actions | When you need de-coupled **ScriptableObject Events** |
| **AI/Pathfinding** | NavMesh | When you need a customized A* pathing (A* Pathfinding Project) |

---

## Decision Patterns: Common Unity Scenarios

### 1. Global State Management

**Wrong:** Hardcoded references between `Player` and `UI` in the Inspector.
**Right:** **ScriptableObject Architecture** (Game Events / Global Variables) for loose coupling.
**Also Right:** **Singleton/Manager pattern** for central orchestration systems.

### 2. High-Frequency Logic (The Update Loop)

**Wrong:** Putting complex logic + `GetComponent` inside `Update()`.
**Right:** Cache references in `Awake`. Use **Events** to trigger logic only when state changes.
**Advanced:** Offload heavy calculations to the **C# Job System**.

### 3. Object Creation

**Wrong:** Constant `Instantiate` and `Destroy` for projectiles or particles.
**Right:** **Object Pooling** (using `UnityEngine.Pool` or custom implementation).

### 4. Serialization

**Wrong:** Manual string parsing of save data.
**Right:** `JsonUtility` for simple data; **Newtonsoft.Json** for complex C# structures (Dictionaries, Inheritance).

---

## Anti-Pattern Detection Checklist

Before committing code, scan for these critical Unity anti-patterns:

| If You See This... | It's Probably Wrong Because... | Fix |
|---|---|---|
| `GetComponent` in `Update` | High CPU overhead every frame | Cache in `Awake` / `Start` |
| `FindObjectOfType` | Extremely slow, logarithmic growth cost | Use Dependency Injection or Singleton |
| Frequent `GameObject.Tag` checks | String comparisons are slow | Use `TryGetComponent` or Layer integer checks |
| `new GameObject()` in loops | Memory fragmentation / GC spikes | Use Object Pooling |
| Singletons for everything | Hard to test, tight coupling | Use ScriptableObject Events |
| Manual `Time.deltaTime` counters | Complexity / Error prone | Use **Coroutines** or **UniTask** for timers |
| Large MonoBehaviours (1000+ lines)| God Object pattern, hard to maintain | Break into smaller, focused Components |

---

## When Custom/Low-Level IS Justified

Document these scenarios explicitly:

1. **Performance Bottleneck:** The Profiler shows Unity native features (like Physics) cannot handle the entity count.
2. **Platform Specifics:** Need to integrate with a specific Console API not covered by Unity.
3. **Unique Core Mechanic:** The mechanic requires a custom deterministic physics engine or a fluid simulation.
4. **Third-party library:** Bridging a custom DLL into C#.

---

## Summary: The Unity Architect Mindset

1. **Analyze performance impact** before choosing a high-level vs low-level approach.
2. **Climb the decision ladder** to minimize "reinventing the wheel."
3. **Favor Data-Driven design** over hardcoded Inspector references.
4. **Profile Early** to ensure your architecture doesn't hit a wall in late production.
5. **Document the "why"** for any deviation from the Unity standard.
