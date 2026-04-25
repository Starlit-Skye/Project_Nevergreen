---
name: sequential-thinking
description: A cognitive framework for step-by-step problem solving and self-correction optimized for Unity systems.
---

# 🧠 Sequential Thinking Skill (Unity Edition)

**Context:** Use this skill when designing Game Systems, implementing complex Mechanics, or debugging runtime lifecycle errors.

## 1. The Protocol
Before providing a final implementation or ScriptableObject design, you must:
1.  **Deconstruct:** Break the mechanic into atomic parts (Data, Input, Logic, Visuals).
2.  **Hypothesize:** Formulate 2-3 approaches (e.g., Interface-based vs Observer Pattern).
3.  **Critique:** Check against Unity constraints (Memory/GC, Main Thread, MonoBehaviour Lifecycle).
4.  **Select:** Choose the most performant and decoupled path (favor composition over inheritance).

## 2. Mental Sandbox (Internal Monologue)
Simulate the Unity execution loop in your "scratchpad" before writing code.
*   *Thought:* "If I put this logic in `Update()`, will it cause performance drops with 100+ entities?"
*   *Check:* "Yes, `GetComponent` in `Update` is expensive."
*   *Correction:* "I must cache the component in `Awake` or use a Manager to batch the updates."

## 3. The "5 Whys" Root Cause Analysis
If debugging Unity systems:
1.  **Why** did the logic fail? -> "The Character didn't take damage."
2.  **Why** no damage? -> "The Trigger event wasn't fired."
3.  **Why** no Trigger? -> "The Collider was set to 'Is Trigger' but no Rigidbody existed."
4.  **Why** no Rigidbody? -> "It was removed from the Prefab override."
5.  **Root Cause:** Component dependency missing on the base Prefab. 

## 4. Usage in Conversation
When using this skill, start your response with a structured analysis:
> **🤔 Thought Process:**
> 1.  **Goal:** Implement a smooth HP Bar transition.
> 2.  **Constraint:** Must be performant and wait for the battle state.
> 3.  **Plan:** I will use **DOTween** to animate the `Slider.value` and tie it to the `OnHealthChanged` UnityEvent.

This transparency ensures we don't build "Spaghetti Code" and helps handle the complexity of Unity's lifecycle.
