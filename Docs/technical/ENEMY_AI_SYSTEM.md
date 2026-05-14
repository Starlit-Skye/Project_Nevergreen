# Modular Enemy AI Framework

Owner: Combat Team
Status: active
Last verified: 2026-05-09
Verified commit: Unknown
Target build: Unity 6 (6000.3.9f1) + PC

## Purpose
Provides a utility-driven, priority-based evaluation system for determining enemy actions during combat. Eliminates hardcoded randomized turn execution by allowing designers to create modular behavioral profiles composed of conditional rules, target resolution strategies, and fallback mechanics.

## Scope
- In scope: AI turn evaluation, dynamic target selection based on current game state, conditional rule processing (e.g., HP thresholds, rank constraints), historical tracking of previous AI decisions, editor tooling for designer configuration.
- Out of scope: Pathfinding, out-of-combat AI, physics-based movement behaviors.

## Source of Truth
- Code: `Assets/Scripts/Combat/AI/AIBrain.cs` (Core evaluator)
- Code: `Assets/Scripts/Combat/AI/EnemyAIProfile.cs` (Data container)
- Code: `Assets/Scripts/Combat/BattleSystem.cs` (Integration point `ExecuteEnemyAction`)
- Code: `Assets/Scripts/Editor/Drawers/SubclassSelectorDrawer.cs` (Editor tooling)
- Design: `Docs/guides/DESIGNER_GUIDE_ENEMY_AI.md`

## Responsibilities
- Evaluate combat environment and state (health, ranks, active members).
- Select the highest-priority valid action from an assigned profile.
- Filter out actions that cannot be performed due to rank constraints or cooldowns/uses.
- Determine the optimal targets for the selected action based on configured strategies.
- Track decision history to provide context for future decisions.
- Pass the turn gracefully if no actions are valid.

## Data Model
- `EnemyAIProfile` (ScriptableObject): Contains an ordered `List<AIBehaviorNode> behaviors`.
- `AIBehaviorNode` (Abstract): Base for nodes like `RuleBasedBehavior` and `RandomSkillBehavior`.
- `AIConditionNode` (Abstract): Base for rules like `HealthCondition`.
- `AITargetingNode` (Abstract): Base for target resolution like `SimpleTargeting`.
- `AIDecision` (Struct): Contains `SkillData skill`, `List<CombatCharacter> targets`, and `bool isPassTurn`.
- `AIHistory` (Class): Stores a stack of past `AIDecision` records per character.

## Event Contracts
- Event: Turn Execution Hook
- Producer: `BattleSystem`
- Consumers: `AIBrain`
- Payload schema: `EvaluateTurn(BattleSystem battle)` returns `AIDecision`.

## Timing Model
- Update domain: Turn-based synchronous evaluation.
- Tick/update order: Executed strictly during the `ProcessTurn` phase in `BattleSystem` when the active character belongs to the enemy team.
- Budget: < 1ms per decision.

## Determinism
- Required: yes
- Strategy: Ordered iteration of behaviors (top-to-bottom priority). Target resolution strategies (like Lowest/Highest HP) use deterministic comparisons. Random selections (like RandomSkillBehavior or Random targeting) rely on `UnityEngine.Random` which can be seeded for predictable replays.
- Known exceptions: Tie-breaking in simple targeting (e.g., two characters with identical HP) currently resolves based on list index order, which is deterministic but implicitly tied to formation setup.

## Authority Model
- Single-player/offline: Full authority on the local client.
- Multiplayer: N/A (Project is currently single-player focused).

## Performance Budget
- CPU: < 1ms budget per evaluation.
- Memory: < 1MB overhead (Profiles are shared assets, History arrays are pre-allocated or kept small).
- Entity scale target: ~4 active enemy brains concurrently evaluating turns sequentially.

## Error Handling and Recovery
- Null Profile: If `AIBrain` has no profile assigned, it generates an empty `AIDecision` causing the `BattleSystem` to issue a generic pass-turn action.
- Target Resolution Failure: If a behavior selects a skill but cannot find valid targets matching the strategy, the behavior evaluates to false, and the brain proceeds to the next behavior in the list.
- Infinite Loops: Mitigated by strictly sequential list evaluation without recursion.

## Observability
- Metrics: N/A
- Logs: N/A
- Traces/profilers: N/A

## Acceptance Tests
- Automated: Existing combat tests (`Assets/Editor/Tests/`) ensure that the introduction of `AIBrain` does not regress target resolution, damage calculation, or battle sequence integrity.
- Playtest: An enemy with `HealthCondition` set to "Heal when Ally HP < 50%" should ignore the heal skill while all allies are above 50% HP, and should cast the heal skill immediately on their turn once an ally drops below the threshold.

## Missing Evidence
- Specific unit tests for isolated `AIBrain` execution paths and node validation are missing (`Unknown`).

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
