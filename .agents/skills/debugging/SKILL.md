---
name: debugging
description: A structured protocol for finding root causes of errors in Unity (The "Systematic Debugging Protocol").
---

# 🐞 Systematic Debugging Skill (Unity Edition)

**Context:** Use this skill whenever you encounter a console error, a logical bug, or unexpected visual behavior in Unity. **Zero guessing.**

## 1. The Protocol
1.  **Reproduce:** Can you trigger the bug consistently in the Editor? If it only happens in Builds, it's likely a loading/timing issue.
2.  **Isolate:** Disable unrelated GameObjects/Systems. Does the bug persist in a minimal "Clean Scene"?
3.  **Trace:** Use `Debug.Log` with `gameObject.name` and timestamps to track signal flow across systems.
4.  **Hypothesize & Verify:** "I bet the character is null because it hasn't spawned yet." -> Add a null check + log.
5.  **Fix:** Implement the most decoupled solution (e.g., use an Event instead of direct reference).
6.  **Verify:** Check the Console for new warnings and ensure Prefab overrides are applied/reverted correctly.

## 2. Tactics
*   **Rubber Ducking:** Explain the code line-by-line to the user. "Here, I check if `speed` is defined..."
*   **Debug Everything:** Don't just log strings; log `Time.frameCount` to catch multi-frame issues and `this.name` to identify which instance is failing.
*   **Inspector Debug Mode:** Click the "Three Dots" on the Inspector tab and select **Debug** to view private variables and hidden state.
*   **Step-by-Step:** Use the "Pause" and "Step Frame" buttons in the Editor to analyze state precisely at the moment of failure.
*   **Visualize:** Use `Debug.DrawLine` or `OnDrawGizmos` to see raycasts, paths, and collision boundaries that are invisible in-game.

## 3. Common Unity Pitfalls
*   **NullReferenceException:** usually an unassigned field in the Inspector or a component removed from a Prefab variant.
*   **Execution Order:** Script A accessing Script B in `Awake` before B has finished its own initialization. **Standard:** Use `Awake` for internal setup, `Start` for external references.
*   **Frame Race Conditions:** Logic that assumes `Update()` order across different scripts. **Standard:** Use `LateUpdate` or an explicit Manager.
*   **Physics Ghosting:** Triggers not firing because one object lacks a `Rigidbody` or the **Layer Collision Matrix** blocks the interaction.

## 4. Output Format
When reporting a fix, follow this structure:
> **🔍 Debugging Analysis:**
> *   **Symptom:** Character "Double Jump" fires three times.
> *   **Context:** Only happens when clicking rapidly in the air.
> *   **Cause:** Input was being polled in `Update` without a "Handled" flag, re-triggering across multiple frames.
> *   **Fix:** Added a `canJump` boolean flag reset in the `Jump` coroutine.
