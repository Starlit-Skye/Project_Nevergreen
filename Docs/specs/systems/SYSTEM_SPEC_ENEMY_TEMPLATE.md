# Enemy Template System

Owner: Gameplay Engineering Team
Status: active
Last verified: 2026-06-19
Verified commit: 4134da54597e5d2ec8192eeff46cf998a27bb03c
Target build: Unity 6000.3.9f1 Standalone Windows

## Purpose
Define the baseline gameplay template and runtime setup for enemy units, including normal enemies, elites, and boss units.

## Scope
- In scope: Enemy templates defined via `CharacterData` with `teamType = CharacterTeamType.Enemy`, elite/boss multi-action capabilities, automatic dynamic binding of `AIBrain` components with specific `EnemyAIProfile` on initialization.
- Out of scope: Specific C# rule logic of the AI system, individual encounter database layout.

## Source of Truth
- Code: `Assets/Scripts/Data/CharacterData.cs` (`Nevergreen.Data.CharacterData`), `Assets/Scripts/Combat/CombatCharacter.cs` (`Nevergreen.Combat.CombatCharacter`), `Assets/Scripts/Combat/AI/AIBrain.cs` (`Nevergreen.Combat.AI.AIBrain`), `Assets/Scripts/Combat/AI/EnemyAIProfile.cs` (`Nevergreen.Combat.AI.EnemyAIProfile`)
- Tests: `Assets/Editor/Tests/AITests.cs` (verifying AI action selection), `Assets/Editor/Tests/CombatSceneBootstrapFormationTests.cs` (verifying enemy team spawning and positioning).
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0 (sections: Enemies, Elites, Bosses, Combat)
- Data: `Assets/Data/Characters/`
- Issue/ADR: None.

## Responsibilities
- Define enemy units using the `CharacterData` ScriptableObject with `teamType` set to `CharacterTeamType.Enemy`.
- Bind unique ID (`characterId`), display name, base stats via levels, and a dedicated `defaultAIProfile` asset.
- Automatically attach and configure an `AIBrain` component on runtime `CombatCharacter` instantiation for all enemy-aligned team units.
- Support scaling actions per round via the `actionsPerRound` property on the template (e.g. 1 for standard units, 2+ for bosses).

## Data Model
- Entity/component/object:
  - `CharacterData`: Target template configuration (with `teamType = CharacterTeamType.Enemy`).
  - `CombatCharacter`: Runtime instance representing the combatant on the field.
  - `AIBrain`: Component added dynamically to the enemy `CombatCharacter` during initialization to evaluate actions.
  - `EnemyAIProfile`: ScriptableObject asset containing the decision rules for AI actions.
- Persistence keys: Enemy formations are represented by lists of `CharacterData` in `EnemyFormationData`.

## Event Contracts
- Event: `AIBrain` initialization
  - Producer: `CombatCharacter.InitializeForCombat()`
  - Consumers: AI action loop.
  - Payload schema: Attaches `AIBrain` and sets `brain.profile = characterData.defaultAIProfile`.

## Timing Model
- Update domain: Main thread.
- Tick/update order: During combat startup within `BattleSystem`, enemy prefabs are spawned and initialized. At the start of an enemy's turn in `BattleSystem`, the `AIBrain` selects and schedules an action.
- Budget: Under 1ms per AI decision tick.

## Determinism
- Required: Yes, for AI action evaluation.
- Strategy: Action selection is evaluated using rule-based priorities inside `AIBrain` matching the AI profile.
- Known exceptions: None.

## Authority Model
- Single-player/offline: Local client AI determines action selections.
- Multiplayer: Unknown.

## Performance Budget
- CPU: Under 0.5ms per AI tick.
- Memory: Minimal transient heap allocations during AI decision-making.
- Entity scale target: Up to 4 active enemy combatants in the scene.

## Error Handling and Recovery
- Missing AI Profile: If an enemy character has no `defaultAIProfile` assigned, a default brain with placeholder action rules is attached.
- Missing Prefab: Spawner falls back or logs an error.

## Observability
- Metrics: None.
- Logs: Logs errors if `characterData` is missing during runtime initialization.
- Traces/profilers: None.

## Acceptance Tests
- Automated:
  - `CombatSceneBootstrapFormationTests.cs`:
    - Tests verifying correct spawning, sizing, and position offset configurations for enemy teams.
  - `AITests.cs`:
    - Tests verifying correct AI selection behavior based on profile.
- Playtest:
  1. In-editor, start a combat encounter containing configured enemies.
  2. Verify that they receive the correct turn allocations (e.g. bosses take multiple turns) and execute actions according to their AI profiles.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
