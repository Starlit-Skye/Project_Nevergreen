---
name: testing
description: A comprehensive suite for ensuring software quality in Unity, spanning EditMode, PlayMode, and Performance.
---

# 🛡️ Quality Assurance & Testing Skill

**Context:** Use this skill package for all testing activities.

## 1. The Testing Pyramid Strategy
1.  **EditMode (NUnit/UTF):** Fast, isolated. Test individual classes and C# logic (e.g., math, data parsing) without the Unity lifecycle.
2.  **PlayMode (Integration):** Test Prefabs, Physics, and MonoBehaviours (Update/Start) in the simulated game engine.
3.  **Systems/Smoke Tests:** Validating full gameplay loops (e.g., "Start Game" -> "Combat" -> "Death").
4.  **Performance:** Frame-rate profiling, memory leaks, and draw call analysis.

Specific "How-To" guides for Unity testing:

- **[Unity Test Framework (UTF)](../guidelines/testing-guidelines.md)**: Standard for EditMode/PlayMode setup.
- **[ASMDEF Isolation](../guidelines/testing-guidelines.md#2-test-organization)**: Keeping test code out of production builds.
- **[Performance Profiling](../guidelines/testing-guidelines.md#5-performance-testing)**: Using the Unity Performance API.

## 3. When to use what?
- **Isolated Logic?** -> Write **EditMode** Test.
- **Physics/Prefabs/Animations?** -> Write **PlayMode** Test.
- **Feature Done?** -> Run **Smoke Test** in a dedicated scene.
- **Frame Drops?** -> Use the **Unity Profiler** + Performance Tests.
- **Bug Fix?** -> Write a regression test (EditMode preferred for speed).

## 4. Critical Learnings (2026-01)

### Unity Testing (UTF)
- **ASMDEF Isolation:** Tests MUST be in a separate assembly. Ensure the "Test Assemblies" checkbox is checked in the Unity Test Runner.
- **Scene Cleanup:** Always use `GameObject.DestroyImmediate` in `[TearDown]` to prevent object leakage between tests.
- **Timing:** Use `yield return new WaitForSecondsRealtime()` in PlayMode tests if you modify `Time.timeScale`.
- **Addressables:** When testing Prefabs, ensure you handle asynchronous loading/unloading to avoid memory leaks.

### Physics & Input
- **Physics Update:** Use `Physics.Simulate()` for deterministic physics tests if needed, or wait for `WaitForFixedUpdate`.
- **Mocking Input:** Use the **Input System Package's** `InputTestFixture` to simulate keyboard/mouse events without real hardware.
- **Layer Matrix:** If triggers aren't firing in tests, check the **Physics Layer Collision Matrix**; tests use the project's global physics settings.
- **Null Checks:** MonoBehaviours in tests often lack the full Scene context; always verify `GetComponent` results before usage.


