# Combat Testing Workflow Guide

Owner: Lead QA Automation Engineer
Status: active
Last verified: 2026-04-25
Verified commit: HEAD
Target build: Unity 2022.3 + Windows

## Purpose
Provide a standardized workflow for engineers to extend the combat testing suite. This ensures all tests remain headless, deterministic, and compatible with the project's current assembly structure.

## Scope
- In scope:
  - EditMode NUnit testing for combat logic.
  - Mocking patterns for `ScriptableObject` data.
  - Character and environment setup/cleanup.
- Out of scope:
  - PlayMode integration tests.
  - UI-driven testing.

## Source of Truth
- Helper: `Assets/Editor/Tests/CombatTestHelper.cs`
- Existing Suites: `Assets/Editor/Tests/` (`BuffDebuffTests.cs`, `HitCritTests.cs`, `StunTests.cs`)

## Assembly & Location Constraints
- **Directory**: All combat tests MUST be placed in `Assets/Editor/Tests/`.
- **Reasoning**: The project does not currently use `.asmdef` files for the main runtime. Placing tests in the `Editor` folder allows them to compile into `Assembly-CSharp-Editor`, giving them full access to `Assembly-CSharp` types without circular dependency or isolation errors.
- **Namespace**: Use `Nevergreen.Tests`.

## Core Testing Pattern: "Headless Mocking"
To maintain speed and avoid side effects, we do not load assets from `Resources` or `Addressables` during tests. Instead, we use "Headless Mocking":
1.  **Instantiate Data**: Use `ScriptableObject.CreateInstance<T>()`.
2.  **Assign Fields**: Manually set the values required for the test scenario.
3.  **Initialize**: Pass the mocked data into the system being tested.

## Procedure: Writing a New Test Suite

### 1. Boilerplate Setup
Every test suite must track GameObjects created during the test to prevent memory leaks and hierarchy clutter in the Editor.

```csharp
[TestFixture]
public class MyNewMechanicTests {
    private List<GameObject> _cleanup;
    private CombatConfig _config;

    [SetUp]
    public void SetUp() {
        _cleanup = new List<GameObject>();
        _config = CombatTestHelper.CreateDefaultConfig();
    }

    [TearDown]
    public void TearDown() {
        foreach (var go in _cleanup) {
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
```

### 2. Character Creation
Use `CombatTestHelper` to create characters. It handles the boilerplate of creating `CharacterData`, `StatBlockData`, and attaching components.

```csharp
private CombatCharacter CreateHero(string id, int attack = 100) {
    var cc = CombatTestHelper.CreateCombatCharacter(id, Team.Player, rank: 1, attack: attack, config: _config);
    _cleanup.Add(cc.gameObject);
    return cc;
}
```

### 3. Implementing the Test
Focus on a single behavior per test. Use `Assert` for verification.

```csharp
[Test]
public void MyMechanic_ExpectedBehavior_WhenTriggered() {
    // Arrange
    var character = CreateHero("hero");
    
    // Act
    // (e.g., character.AddStatus(...), StatusProcessor.TickDurations(...))
    
    // Assert
    // Assert.AreEqual(expectedValue, actualValue);
}
```

## Handling Determinism
For any logic involving random rolls (Hit, Crit, Status Application), always use the deterministic RNG helper:
- **Call**: `CombatTestHelper.CreateFixedRng(seed: 42)`
- **Usage**: Pass this `System.Random` instance into `CombatCalculator` or `SkillContext`.

## Current Test Registry

| Suite | File | Coverage |
| :--- | :--- | :--- |
| **Buff & Debuff** | `BuffDebuffTests.cs` | Percentage-based stat mods, additive stacking logic (+10% + +20% = +30%), net calculations, duration ticking, and debuff resistance gating. |
| **Hit & Crit** | `HitCritTests.cs` | Accuracy vs Dodge calculation, 95% hard caps, Skill accuracy/crit modifiers, guaranteed hit flags, defense-based damage reduction, and "Ignore Defense" logic. |
| **Stun** | `StunTests.cs` | Turn-skip flag validation, stun duration vs turn-tick timing, and the +300% StunResist recovery buff applied on stun expiry. |

## Detailed Test List (33 Cases)

### BuffDebuffTests (14)
- `Buff_Attack_IncreasesStatByPercentageOfBase`
- `Debuff_Attack_DecreasesStatByPercentageOfBase`
- `Buff_Defense_IncreasesStatByPercentageOfBase`
- `Buff_Speed_IncreasesStatByPercentageOfBase`
- `MultipleBuffs_SameStat_StackAdditively`
- `MultipleDebuffs_SameStat_StackAdditively`
- `BuffAndDebuff_SameStat_NetToCorrectPercentage`
- `MultipleBuffs_DifferentStats_ApplyIndependently`
- `DebuffResistance_ReducesApplicationChance`
- `DebuffResistance_EqualToChance_BlocksAll`
- `DebuffResistance_ExceedsChance_BlocksAll`
- `BuffDuration_ExpiresAfterCorrectTicks`
- `DebuffDuration_ExpiresAfterOneTick`
- `ExpiredBuffs_DoNotAffectStats`

### HitCritTests (10)
- `HitChance_IsAccuracyMinusDodge_CappedAt95`
- `HitChance_CappedAt95`
- `AccuracyMod_FromSkill_IsApplied`
- `GuaranteedHit_AlwaysHits`
- `CritDamage_Applies1Point5xMultiplier`
- `NoCrit_WhenCritChanceIsZero`
- `CritMod_FromSkill_AddsToBaseCritChance`
- `Defense_ReducesDamage`
- `IgnoresDefense_BypassesReduction`
- `SkillScaling_MultipliesBaseRoll`

### StunTests (9)
- `StunnedCharacter_IsMarkedAsStunned`
- `StunnedCharacter_RemainsStunned_UntilExpiry`
- `PostStunRecovery_Applies300PercentStunResistBuff`
- `PostStunRecovery_BuffExpiresAfterOneTick`
- `PostStunRecovery_IncreasesEffectiveStunResist`
- `StunTiming_1TurnStun_SkipsThenExpires`
- `StunTiming_2TurnStun_SkipsTwoTurns`
- `StunResistance_ReducesApplicationChance`
- `StunResistance_100Percent_BlocksStun`

## Verification
1.  Open the **Test Runner** window (`Window > General > Test Runner`).
2.  Select **EditMode** tab.
3.  Find `Nevergreen.Tests` and click **Run All**.

## Validation
- [x] Procedure matches current helper methods
- [x] Directory and assembly constraints are explicit
- [x] Mocking pattern is defined
- [x] Test registry reflects current coverage
- [x] Determinism strategy is stated
