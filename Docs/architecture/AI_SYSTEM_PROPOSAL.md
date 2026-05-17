# 🤖 System Architecture Proposal: Modular Enemy AI Framework

**Author:** System Architect
**Context:** The current implementation in `BattleSystem.ExecuteEnemyAction()` relies on hardcoded random skill and target selection. To support rich, tactical combat, we need a flexible, data-driven AI system that allows Game Designers to build complex enemy behaviors without writing new code.

## 📐 1. Architectural Philosophy

We will build a **Utility-Driven Rules Engine** using Unity's `ScriptableObject` system and Polymorphic Serialization (`[SerializeReference]`). 

**Key Principles:**
- **Data-Driven:** Designers configure AI behaviors in the Inspector using reusable Rules and Targeting Strategies.
- **Composition over Inheritance:** Instead of creating a `GoblinAI.cs` and `BossAI.cs`, we create generic conditions (e.g., `TargetHasDebuffCondition`) and compose them inside an `AIProfile`.
- **Decoupled:** The AI system will not execute actions. It strictly *evaluates* the current board state and returns a `Decision` (Skill + Targets) to the `BattleSystem`.

---

## 🏗️ 2. Core Architecture

### A. The AI Brain (Runtime Component)
A new component, `AIBrain`, is attached to the Enemy GameObject (or managed inside `CombatCharacter`). It holds state (turn counters, history) and a reference to an `EnemyAIProfile`.

```csharp
public class AIBrain : MonoBehaviour 
{
    public EnemyAIProfile profile;
    private AIHistory history; // Tracks past actions, consecutive skill uses, turns taken
    
    // Called by BattleSystem.ExecuteEnemyAction()
    public AIDecision EvaluateTurn(BattleSystem battleContext, CombatCharacter self); 
}
```

### B. Enemy AI Profile (ScriptableObject)
The `EnemyAIProfile` is a ScriptableObject that defines the "Personality" of an enemy. It contains a prioritized list of **Behaviors**. The Brain evaluates these top-down and executes the first valid one.

```csharp
[CreateAssetMenu(menuName = "Nevergreen/AI/Enemy Profile")]
public class EnemyAIProfile : ScriptableObject 
{
    [SerializeReference] 
    public List<IAIBehavior> behaviors; // Evaluated top-to-bottom
}
```

---

## 🧩 3. Behavior Building Blocks

To support the wide variety of requirements (Random, Patterns, Explicit Rules), we implement different types of `IAIBehavior` nodes using Unity's `[SerializeReference]` attribute, which allows drawing polymorphic lists in the Inspector.

### Concept 1: The Rule-Based Behavior (Explicit Rules)
A behavior that evaluates a set of rules, and if true, executes a specific skill against a calculated target.

- **Conditions (`IAICondition`)**: Define *IF* the skill can be used.
  - `TargetHasStatusCondition` (e.g., Target has Bleed).
  - `SelfRankCondition` (e.g., Am I in rank 1?).
  - `HistoryCondition` (e.g., Have I used this skill 3 times in a row? If yes, fail).
  - `HPCondition` (e.g., Target HP < 30%).
  
- **Targeting Strategy (`IAITargeting`)**: Defines *WHO* to hit.
  - `HighestPriorityTarget` (evaluates a score for all valid targets).
  - `TargetWithStatus` (e.g., Find the character with the Mark debuff).
  - `RandomTarget` (Fallback).

### Concept 2: The Pattern Behavior (Specific Defined Patterns)
Used for bosses or strict enemies. This behavior ignores pure logic and follows a strict sequence based on a turn counter.

- **Pattern Sequence**: An array of `PatternStep`.
  - Step 1: Use `Buff Skill` on `Self`
  - Step 2: Use `AoE Attack` on `All Enemies`
  - Step 3: Loop back to Step 1.

### Concept 3: The Fallback Behavior (Random Skills & Targets)
The lowest priority behavior in every `EnemyAIProfile` (acting as a safety net).
- Filters `CombatCharacter.equippedSkills` by validity (cooldown, rank).
- Picks a random skill.
- Picks a random valid target.
- **Pass Turn Case**: If no skills can be used due to rank constraints, or all usable skills have no valid targets, the behavior returns a "Pass" decision to prevent soft-locks.
- Exactly matches the current logic in `BattleSystem`, but wrapped in the new framework.

---

## 🔄 4. The Evaluation Pipeline

When `BattleSystem` calls `AIBrain.EvaluateTurn()`:

1. **Update History**: `AIBrain` increments its turn counter and updates memory.
2. **Iterate Behaviors**: Loop through the `EnemyAIProfile.behaviors` by priority.
3. **Check Conditions**: For the current behavior, check all `IAICondition`s against the current battle state.
4. **Resolve Targets**: If conditions pass, run the `IAITargeting` strategy. If it finds valid targets, the behavior is successfully resolved.
5. **Return Decision**: Package the chosen `SkillData` and `List<CombatCharacter>` into an `AIDecision` struct and return it.
6. **Execution**: `BattleSystem` receives the decision and processes it exactly like a Player's `SubmitPlayerAction`.

---

## 🛠️ 5. Implementation Roadmap (Next Steps)

1. **Infrastructure**: Create the base interfaces (`IAIBehavior`, `IAICondition`, `IAITargeting`) and the `AIBrain` / `EnemyAIProfile` classes.
2. **Serialization**: Implement a Custom Property Drawer for `[SerializeReference]` (or use Unity's experimental `SerializeReference` UI features) so designers can easily add conditions/behaviors from a dropdown menu in the Unity Inspector without writing code.
3. **Core Nodes**: Implement the most critical nodes:
   - `RandomBehavior` (to maintain current parity and prevent game-breaking loops).
   - `RuleBasedBehavior` (for explicit rules and debuff targeting).
   - `SequenceBehavior` (for rigid turn patterns).
4. **Integration**: Replace the hardcoded random logic in `BattleSystem.ExecuteEnemyAction()` to invoke `AIBrain.EvaluateTurn()`.
5. **Validation**: Create Unit Tests to simulate board states and assert that specific `EnemyAIProfiles` output the mathematically correct `AIDecision`.

## 📌 Summary of Benefits
- **Zero-Code Enemy Creation**: Designers can build a "Sniper" enemy that inherently targets marked players, or a "Berserker" that spams an attack 3 times before resting.
- **Highly Modular**: Adding a new targeting rule (e.g., "Target lowest defense") is as simple as creating one new `IAITargeting` script.
- **Clean Architecture**: `BattleSystem` remains ignorant of *how* enemies think; it just asks for a `Decision` and executes it.
