# Testing & Quality Assurance Guidelines

**Version:** 3.0 (Unity-Optimized)
**Status:** Approved
**Target Audience:** All Developers, AI Agents

---

## 1. Code Coverage Standards

| Layer | Minimum Coverage | Target |
|-------|------------------|--------|
| **Core Systems / Logic** (EditMode) | 80% | 90% |
| **Data Models / Utilities** | 90% | 95% |
| **Game Mechanics** (PlayMode) | 60% | 75% |
| **UI Interaction & Feedback** | 50% | 70% |

---

## 2. Test Organization

Tests must be isolated using **Assembly Definitions (.asmdef)** to prevent test code from being included in production builds.

```
Assets/
├── Scripts/
│   ├── Systems/
│   │   ├── Movement/
│   │   │   ├── MovementController.cs
│   │   │   └── Tests/          # EditMode/Unit Tests
│   │   │       ├── MovementController.Tests.asmdef (Test Assembly)
│   │   │       └── MovementTests.cs
│   │   └── Combat/
│   │       ├── CombatManager.cs
│   │       └── Tests/          # PlayMode/Integration Tests
│   │           ├── Combat.Integration.Tests.asmdef (Test Assembly)
│   │           └── CombatFlowTests.cs
```

---

## 3. EditMode Testing (Unit / Logic)

Use EditMode tests for isolated logic that doesn't requires the Unity Lifecycle or Physics.

```csharp
// MovementTests.cs
using NUnit.Framework;

public class MovementTests {
    [Test]
    public void CalculateVelocity_MaxSpeed_ReturnsClampedValue() {
        // Arrange
        var motor = new MovementMotor(maxSpeed: 10f);
        
        // Act
        var velocity = motor.CalculateVelocity(input: 20f);
        
        // Assert
        Assert.AreEqual(10f, velocity);
    }
}
```

---

## 4. PlayMode Testing (Integration / Mechanics)

Use PlayMode tests for features that involve Physics, Prefabs, or the Unity Lifecycle (`Update`, `Start`, etc.).

```csharp
// CombatFlowTests.cs
using UnityEngine.TestTools;
using System.Collections;
using NUnit.Framework;

public class CombatFlowTests {
    [UnityTest]
    public IEnumerator EnemyTakeDamage_TriggersDeathAnimation() {
        // Arrange
        var enemy = SpawnEnemyPrefab();
        var health = enemy.GetComponent<Health>();
        
        // Act
        health.TakeDamage(999);
        yield return new WaitForSeconds(0.5f); // Wait for death frame
        
        // Assert
        Assert.IsTrue(enemy.GetComponent<Animator>().GetBool("IsDead"));
    }
}
```

---

## 5. Performance Testing

Utilize the **Unity Performance Test Package** for critical frame-rate sensitive systems.

```csharp
[Test, Performance]
public void Pathfinding_StressTest() {
    Measure.Method(() => {
        pathfinder.Search(start, end);
    }).Run();
}
```

---

## 6. Testing Philosophy

We follow **Unity Testing Best Practices**:
- **Test Assemblies**: Always isolate tests in `.asmdef` files.
- **Mocking**: Use dependency injection to mock ScriptableObjects or core Managers.
- **Stability**: PlayMode tests should be deterministic; avoid relying on real timings where possible (use `Time.timeScale` or custom clocks).

---

## 7. CI/CD Integration

All tests must pass in the **Unity Batch Mode** or via **GameCI**.
```bash
# Example CLI Run
unity-editor -batchmode -runTests -testPlatform PlayMode -testResults results.xml
```

---

## 8. Quality Checklist

- [ ] New systems have associated EditMode unit tests.
- [ ] Physics or Prefab-heavy mechanics have PlayMode integration tests.
- [ ] UI screens have "Smoke Tests" (opening/closing without errors).
- [ ] Asset references in tests are handled via `Addressables` or `Resources`.

---

## 9. Common Pitfalls in Unity Testing

| Issue | Solution |
|-------|----------|
| Tests slow down Editor | Use EditMode for logic; minimize Scene loading in PlayMode. |
| NullRefs in Tests | Use `[SetUp]` to initialize MonoBehaviours or Mock objects. |
| Coroutines not running | Use `[UnityTest]` and `yield return` for time-dependent logic. |
| Tests fail in Build | Ensure Test Assemblies are NOT included in the main build. |
| State pollution | Clean up `GameObject.DestroyImmediate` in `[TearDown]`. |

