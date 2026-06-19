# Room Selection and Effect Strategies System

Owner: Gameplay Programming
Status: active
Last verified: 2026-06-19
Verified commit: 4134da54597e5d2ec8192eeff46cf998a27bb03c
Target build: Unity 6000.3.9f1 + Standalone

## Purpose
Decouple room definition and metadata from execution behavior using ScriptableObjects and polymorphic execution strategies. Enable designers to configure room choice pools and activation timings (immediate, continuous combat, or post-battle victory), replacing static flow buttons with randomized choice buttons on victory.

## Scope
- In scope: room metadata representation, Strategy pattern serialization via `[SerializeReference]` and `[SubclassSelector]`, configurable room selection UI, run session next-room persistence, bootstrap interception and battle setup bypass.
- Out of scope: multiplayer synchronization, custom animations for selection, specific combat character layout mapping.

## Source of Truth
- Code:
  - `Assets/Scripts/Data/RoomData.cs` (`Nevergreen.Data.RoomData`)
  - `Assets/Scripts/Data/RoomActivationType.cs` (`Nevergreen.Data.RoomActivationType`)
  - `Assets/Scripts/Data/RoomEffectStrategy.cs` (`Nevergreen.Data.RoomEffectStrategy`)
  - `Assets/Scripts/Data/MarionetteRoomEffectStrategy.cs` (`Nevergreen.Data.MarionetteRoomEffectStrategy`)
  - `Assets/Scripts/Data/RoomDatabase.cs` (`Nevergreen.Data.RoomDatabase`)
  - `Assets/Scripts/Data/GlobalConfig.cs` (`Nevergreen.Data.GlobalConfig`)
  - `Assets/Scripts/Data/RunSessionManager.cs` (`Nevergreen.Data.RunSessionManager`)
  - `Assets/Scripts/Prototype/CombatSceneBootstrap.cs` (`Nevergreen.Prototype.CombatSceneBootstrap`)
  - `Assets/Scripts/Prototype/CombatUI.cs` (`Nevergreen.Prototype.CombatUI`)
- Tests:
  - `Assets/Editor/Tests/RoomEffectTests.cs` (`Nevergreen.Tests.RoomEffectTests`)
  - `Assets/Editor/Tests/CombatUITests.cs` (`Nevergreen.Tests.CombatUITests`)
- Design:
  - `Docs/specs/architecture/ARCHITECTURE_SPEC_COMBAT_RUNTIME.md`
  - `Docs/specs/systems/SYSTEM_SPEC_COMBAT_SCREEN.md`

## Responsibilities
- Encapsulate room details (name, description, activation timing, strategy) inside `RoomData`.
- Dynamically instantiate room choice buttons upon battle victory based on `GlobalConfig.roomChoiceCount` and `RoomDatabase.availableRooms` accessed via `GameDatabase.Instance`.
- Maintain the next-room state across scene transitions in `RunSessionManager.NextRoomData`.
- Intercept scene load transitions in `CombatSceneBootstrap.Start()` to handle immediate activation (`OnRoomLoaded`) and early return (bypassing combat).
- Execute lingering strategies during continuous combat (`ContinuousCombat`) after team initialization.
- Listen to battle outcome events (`OnBattleEnded`) in `RunSessionManager` to execute victory-activated strategies (`OnCombatVictory`).
- Clear the next-room state upon invocation to ensure a clean run progression.
- Unsubscribe from battle system events dynamically on battle resolution to avoid resource leaks.

## Data Model
- `RoomActivationType`: enum containing `OnRoomLoaded`, `ContinuousCombat`, `OnCombatVictory`.
- `RoomEffectStrategy`: abstract base class decorated with `[Serializable]`, defining the abstract `ExecuteRoomEffect()` method.
- `MarionetteRoomEffectStrategy`: concrete implementation subclassing `RoomEffectStrategy`, storing a reference to `marionetteSelectionPrefab` and locating a Screen-Space Canvas to spawn it.
- `RoomData`: ScriptableObject asset containing `roomName` (string), `description` (TextArea string), `activationType` (RoomActivationType), and `strategy` (RoomEffectStrategy) serialized with `[SerializeReference]` and `[SubclassSelector]`.
- `RoomDatabase`: ScriptableObject containing `availableRooms` (List of RoomData).
- `GlobalConfig`: ScriptableObject containing `roomChoiceCount` (int).
- `RunSessionManager`: static class containing static property `NextRoomData` (RoomData) and static reference `_activeBattleSystem` (BattleSystem) for tracking.

## Event Contracts
- Event: `BattleSystem.OnBattleEnded`
  - Producer: `BattleSystem`
  - Consumers: `RunSessionManager`
  - Payload schema: `BattleOutcome` (enum: `Victory`, `Defeat`)
- Event: Room Choice Button Click
  - Producer: Dynamic UI Buttons spawned in `CombatUI`
  - Consumers: `RunSessionManager` (assigns `NextRoomData`), Scene Manager (triggers reload of the current combat scene)

## Timing Model
- Update domain: Event-driven and initialization-based.
- Order of operations:
  1. Scene Load: `CombatSceneBootstrap.Start` runs.
  2. Check `NextRoomData.activationType == OnRoomLoaded`. If so, execute strategy immediately, clear `NextRoomData`, and return (bypassing spawning and initialization).
  3. If not, call `SpawnTeams()` and `InitializeBattle()`.
  4. Check `NextRoomData.activationType == ContinuousCombat`. If so, execute strategy immediately and clear `NextRoomData`.
  5. During initialization, `CombatSceneBootstrap` calls `RunSessionManager.SubscribeToBattle()`.
  6. Battle proceeds.
  7. Battle outcome determined: `BattleSystem` fires `OnBattleEnded`.
  8. `RunSessionManager` intercepts event, unsubscribes immediately, and checks if outcome is `Victory`.
  9. If `Victory` and `NextRoomData.activationType == OnCombatVictory`, execute strategy and clear `NextRoomData`.
  10. `CombatUI.HandleBattleEnded` triggers: if `Victory`, clears previous buttons, picks random rooms from `availableRooms`, instantiates dynamic UI buttons, and hooks click events.

## Determinism
- Required: Yes, for room selection UI randomized generation.
- Strategy: Uses standard System.Random inside `PickRandomRooms()` method to ensure a pseudo-random selection from the available pool.

## Authority Model
- Single-player/offline: All room selection state, strategy executions, and UI prefab instantiations occur locally on the client machine.

## Performance Budget
- CPU: Strategy execution should execute within a single frame (< 16ms).
- Memory: UI Prefab instantiation should avoid allocations exceeding 1MB per room choice generation.

## Error Handling and Recovery
- Strategy is null: logged as warning inside `RoomData.ActivateEffect()`, runs without executing any side-effects.
- No Canvas found in scene: logged as error inside `MarionetteRoomEffectStrategy.ExecuteRoomEffect()`, exits cleanly.
- Prefab is null: logged as error inside `MarionetteRoomEffectStrategy.ExecuteRoomEffect()`, exits cleanly.
- Missing Config/Rooms: `CombatUI` falls back to the default `nextRoomButton` button.

## Observability
- Logs: Warns on missing strategy reference (`[RoomData] Room '{roomName}' has no strategy assigned`), errors on missing Canvas (`No Screen-Space Canvas found`) or missing UI Prefab (`marionetteSelectionPrefab is not assigned!`), logs successful instantiation (`Marionette Selection UI instantiated`).

## Acceptance Tests
- Automated:
  - Tests in `Assets/Editor/Tests/RoomEffectTests.cs`:
    - `RoomData_ActivateEffect_InvokesStrategy`
    - `RoomData_ActivateEffect_NullStrategy_DoesNotThrow`
    - `RunSessionManager_NextRoomData_DefaultIsNull`
    - `RunSessionManager_NextRoomData_SetAndGet`
    - `RunSessionManager_Clear_ResetsNextRoomData`
    - `RunSessionManager_ActivateCurrentRoomEffect_InvokesStrategy`
    - `RunSessionManager_ActivateCurrentRoomEffect_NullNextRoom_DoesNotThrow`
    - `SubscribeToBattle_Victory_ActivatesOnCombatVictoryRoom`
    - `SubscribeToBattle_Defeat_DoesNotActivateRoom`
    - `SubscribeToBattle_Victory_OnRoomLoadedType_DoesNotActivate`
    - `SubscribeToBattle_Unsubscribes_AfterBattleEnded`
    - `GlobalConfig_RoomChoiceCount_DefaultIs3`
    - `RoomDatabase_AvailableRooms_DefaultIsEmpty`
  - Tests in `Assets/Editor/Tests/CombatUITests.cs`:
    - `HandleBattleEnded_ShowsNextRoomButton_OnVictory`
    - `HandleBattleEnded_HidesNextRoomButton_OnDefeat`
    - `Initialize_HidesNextRoomButton_AndRegistersListener`
- Playtest:
  - Create a test `RoomData` asset using **Create → Nevergreen → Data → Room Data**. Select a strategy, configure its properties, and set its timing.
  - Set the number of choices in the game's `GlobalConfig` and available rooms in the game's `RoomDatabase`.
  - Win a combat session to verify the UI spawns selection buttons instead of a static "Next Room" button.

## Missing Evidence
None.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Unknowns are explicitly labeled
- [x] Links and paths resolve
- [x] Acceptance tests are defined
