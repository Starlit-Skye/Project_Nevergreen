# Run Session Manager System

> **Owner:** Gameplay Engineering Team | **Last Updated:** 2026-08-31 | **Status:** Active

## Purpose
Maintain the active run state, party composition, encounter databases, economy balances (`Parts` & `Scraps`), and progression statistics across scenes and battles. Provide core session lifetime initialization, room progression, currency management, and battle-victory room effect execution hooks.

## Scope
- In scope: Static preservation of current run status including `CurrentParty` (list of `PartyMemberInfo`), economy balances (`Parts`, `Scraps`), economy transaction methods (`GrantParts`, `TrySpendParts`, `GrantScraps`, `TrySpendScraps`), economy change events (`OnPartsChanged`, `OnScrapsChanged`), room effect data (`RoomData`), last selected enemy formation, and room progression count. Accessing global database configurations (`EnemyFormationDatabase`, `TraitDatabase`, `RoomDatabase`) through `GameDatabase.Instance`. Auto-incrementation of room progression count when loading `"CombatPrototype"` during an active run. Subscribing to battle systems to execute room victory effects. Querying and filtering enemy formations by progression-derived difficulty tiers (`Trivial`, `EarlyGame`, `MidGame`, `LateGame`), and supporting room generation logic (e.g. forced Heal Room on tier transitions) by tracking `RoomProgression`.
- Out of scope: Scene loading triggers and scene transition logic (handled by room selection UI or controllers); saving/loading run data to disk; direct rendering of room progression stats in the UI.

## Source of Truth
- Code: `Assets/Scripts/Data/RunSessionManager.cs` (`Nevergreen.RunSessionManager`), `Assets/Scripts/Prototype/CombatSceneBootstrap.cs` (`Nevergreen.Prototype.CombatSceneBootstrap`), `Assets/Scripts/Data/RoomData.cs` (`Nevergreen.Data.RoomData`)
- Tests: `Assets/Editor/Tests/RoomEffectTests.cs`, `Assets/Editor/Tests/EnemyFormationSelectionTests.cs`, `Assets/Editor/Tests/EconomySystemTests.cs`
- Design: `Docs/specs/systems/SYSTEM_SPEC_ROOM_SELECTION.md`, `Docs/specs/systems/SYSTEM_SPEC_ECONOMY_RUNTIME.md`
- Data: `Assets/Scripts/Data/EnemyFormationDatabase.cs` (`Nevergreen.Data.EnemyFormationDatabase` schema)
- Issue/ADR: Unknown

## Responsibilities
- Store the player's active party members (`CurrentParty`) and persist them across scenes.
- Manage economy balances (`Parts`, `Scraps`) via `Grant` and `TrySpend` methods.
- Access the active databases (`EnemyFormationDatabase`, `TraitDatabase`, `RoomDatabase`) via `GameDatabase.Instance`.
- Increment the run's `RoomProgression` count by 1 automatically whenever the `"CombatPrototype"` scene is loaded with an active party, unless the next room is a Healing Room (`roomId == "RD_HealRoom"`).
- Subscribe to the current `BattleSystem`'s outcome event (`OnBattleEnded`) to handle victory outcome persistence.
- Provide `TriggerPendingVictoryRoomEffect()` to activate the selected room's effect (`NextRoomData`) when the post-combat reward UI is closed if room activation timing is set to `OnCombatVictory`.
- Prevent duplicate consecutive enemy formations within the requested tier via `GetNextRandomFormation(EnemyEncounterTier tier)` (with a fallback logic up to 100 random pick attempts).
- Provide a `Clear()` method to clean up all party data, room progression stats, and unsubscribe from event loops.

## Data Model
- Entity/component/object:
  - `RunSessionManager` (static class): Roster and database coordinator.
  - `CurrentParty`: `List<PartyMemberInfo>` (active party units).
  - `GameDatabase.Instance`: `GameDatabase` (provides global read-only access to `EnemyFormationDatabase`, `MarionetteDatabase`, `TraitDatabase`, and `RoomDatabase`).
  - `LastSelectedFormation`: `EnemyFormationData` (cache of last encountered formation).
  - `NextRoomData`: `RoomData` (selected room template to resolve after battle).
  - `RoomProgression`: `int` (number of completed/entered rooms in the current run).
  - `Parts`: `int` (current parts balance).
- Persistence keys: Delegated to `SaveManager` (`Docs/specs/systems/SYSTEM_SPEC_SAVE_GAME_PERSISTENCE.md`), which serializes the active run progression, parts count, party member levels, room completed state, and room choices to `save.dat`.

## Event Contracts
- Event: `UnityEngine.SceneManagement.SceneManager.sceneLoaded`
  - Producer: Unity `SceneManager`
  - Consumers: `RunSessionManager` (via internal static callback `OnSceneLoaded`)
  - Payload schema: `Scene` name and `LoadSceneMode`.
- Event: `BattleSystem.OnBattleEnded`
  - Producer: `BattleSystem`
  - Consumers: `RunSessionManager` (via static callback `OnBattleEnded`)
  - Payload schema: `BattleOutcome` (Victory or Defeat enum values).

## Timing Model
- Update domain: Main thread synchronously.
- Tick/update order:
  - `OnSceneLoaded` is called on scene load initialization.
  - `SubscribeToBattle` is called at the start of a combat scene during battle setup.
  - `OnBattleEnded` is triggered on battle resolution.
- Budget: N/A (runs during transitions, not active frame tick).

## Determinism
- Required: Yes for formation randomization.
- Strategy: Uses static `System.Random` instance (`_rng`) initialized globally.
- Known exceptions: Selections depend on global random seed state.

## Authority Model
- Single-player/offline: The local client application has full authority over the run session state and room progression.
- Multiplayer: Unknown

## Performance Budget
- CPU: Extremely low overhead, running only on-demand during transition events (scene loads, battle endings, random formation picks).
- Memory: Zero dynamic allocations except when initializing lists/random bounds.
- Entity scale target: Single static instance tracking up to 4 party units, 1 active formation database, and 1 next room.

## Error Handling and Recovery
- Null EnemyFormationDatabase or empty tier formations: `GetNextRandomFormation(EnemyEncounterTier tier)` returns `null` safely.
- Single formation in resolved database tier: `GetNextRandomFormation(EnemyEncounterTier tier)` returns that single formation immediately without looping.
- Consecutive formation check: Limits duplicate check attempts to 100 to avoid infinite loops, returning the picked formation if a unique one cannot be found within the budget.
- Stale battle system subscription: `SubscribeToBattle` automatically unsubscribes from the previous `_activeBattleSystem` before registering to a new one.

## Observability
- Metrics: Room progression count (`RoomProgression`).
- Logs: None.
- Traces/profilers: None.

## Acceptance Tests
- Automated:
  - `Assets/Editor/Tests/RoomEffectTests.cs`:
    - `RoomProgression_StartsAtZero`: Verifies default state.
    - `RoomProgression_ClearResetsToZero`: Verifies reset behavior.
    - `RoomProgression_InitializeResetsToZero`: Verifies initialization behavior.
    - `RoomProgression_OnSceneLoaded_IncrementsWhenCombatSceneAndPartyExists`: Verifies progression increments under active run conditions.
    - `RoomProgression_OnSceneLoaded_DoesNotIncrementWhenNotCombatScene`: Verifies scene filter safety.
    - `RoomProgression_OnSceneLoaded_DoesNotIncrementWhenPartyEmpty`: Verifies empty run safety.
    - `RoomEffect_OnCombatVictory_TriggersEffect`: Verifies battle outcome triggers the room effect.
  - `Assets/Editor/Tests/EnemyFormationSelectionTests.cs`:
    - `CombatConfig_GetEncounterTierForRoom_ResolvesCorrectTier`: Verifies progression room-to-tier mappings in CombatConfig.
    - `GetNextRandomFormation_ReturnsNull_WhenNoDatabaseInitialized`: Verifies null-safety of the selection system.
    - `GetNextRandomFormation_ReturnsFormation_WhenDatabaseHasOneEntry`: Verifies single formation fallback.
    - `GetNextRandomFormation_NeverReturnsConsecutiveDuplicates`: Verifies consecutive duplicate prevention within a tier.
    - `GetNextRandomFormation_ReturnsFromSpecifiedTier`: Verifies correct tier selection.
    - `Initialize_ResetsLastSelectedFormation`: Verifies state cleanup.
- Playtest: Initiate a new run from the Main Menu, complete a combat encounter, select the next room, and verify that the progression count is incremented upon entering the next combat scene and the selected room's effect is correctly applied.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
