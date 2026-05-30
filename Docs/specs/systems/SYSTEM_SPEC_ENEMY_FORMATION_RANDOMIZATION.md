# Enemy Formation and Randomization System

Owner: Dev Team
Status: active
Last verified: 2026-05-30
Verified commit: 6dab85bd03ad290dba585a16406e625b4dc0a73b
Target build: Unity 6 (6000.3.9f1) + PC/Standalone

## Purpose
Allows designers to configure specific enemy team formations as asset configurations, and randomizes encounter configurations per room transition during a run session. It enforces anti-repeat mechanics to prevent consecutive duplicate encounters and manages spacing layouts for multi-rank characters.

## Scope
- **In scope**:
  - `EnemyFormationData` ScriptableObject defining an ordered lineup of enemy prefabs.
  - `EnemyFormationDatabase` ScriptableObject holding a registry pool of available formations.
  - `RunSessionManager` tracking the active database and last selected formation state.
  - Anti-repeat selection algorithm (preventing consecutive repeats when database has multiple formations).
  - `CombatSceneBootstrap` dynamic spawning logic, resolving team lineups from `RunSessionManager`.
  - Spacing calculations for multi-rank enemy positioning.
  - UGUI Next Room flow in `CombatUI` to trigger scene reload upon victory.
- **Out of scope**:
  - Save/load serialization of run history to persistent disk storage (in-memory state only).
  - Mid-combat reinforcement spawning or formation shifts from the database.

## Source of Truth
- **Code**:
  - `Assets/Scripts/Data/EnemyFormationData.cs` (`EnemyFormationData` ScriptableObject)
  - `Assets/Scripts/Data/EnemyFormationDatabase.cs` (`EnemyFormationDatabase` ScriptableObject)
  - `Assets/Scripts/Data/RunSessionManager.cs` (`RunSessionManager` static manager)
  - `Assets/Scripts/Prototype/CombatSceneBootstrap.cs` (`CombatSceneBootstrap.SpawnTeams`)
  - `Assets/Scripts/Prototype/CombatUI.cs` (`CombatUI` Next Room click callback)
  - `Assets/Scripts/UI/CeciliaSkillSelectController.cs` (`CeciliaSkillSelectController` entry point injection)
- **Tests**:
  - `Assets/Editor/Tests/EnemyFormationSelectionTests.cs` (selection logic and anti-repeat constraints)
  - `Assets/Editor/Tests/CombatSceneBootstrapFormationTests.cs` (dynamic spawning integration and position centering)
  - `Assets/Editor/Tests/CombatUITests.cs` (victory state transition and Next Room button toggle checks)
- **Design**:
  - Architecture spec for Enemy Formations and Run Loop Randomization.
- **Data**:
  - `Assets/Scenes/MainMenu.unity`
  - `Assets/Scenes/CombatPrototype.unity`
- **Issue/ADR**: Unknown

## Responsibilities
- Author custom enemy teams as discrete `EnemyFormationData` assets.
- Pool active encounters inside `EnemyFormationDatabase` assets.
- Select random encounters without consecutive duplicate selections using `RunSessionManager.GetNextRandomFormation()`.
- Spawn and align enemy teams in the combat scene, calculating centered coordinates for multi-rank units.
- Reload the combat scene upon Victory click to progress to the next encounter room.

## Data Model
- `EnemyFormationData`:
  - `enemyPrefabs`: `List<GameObject>` (where index maps to rank slot)
- `EnemyFormationDatabase`:
  - `formations`: `List<EnemyFormationData>`
- `RunSessionManager.ActiveFormationDatabase`: `EnemyFormationDatabase`
- `RunSessionManager.LastSelectedFormation`: `EnemyFormationData`
- **Persistence keys**: None (in-memory state only).

## Event Contracts
- Event: `nextRoomButton.onClick`
  - Producer: UGUI Button in `CombatUI`
  - Consumers: `CombatUI.OnNextRoomClicked()`
  - Payload schema: None
  - Effect: Triggers `SceneManager.LoadScene` for the active scene, reloading the room.

## Timing Model
- **Update domain**: Main thread MonoBehaviour lifecycle.
- **Tick/update order**: Spawning in `CombatSceneBootstrap.Start()` executes and completes before the combat engine initializes the round queue in `BattleSystem.StartBattle()`.
- **Budget**: CPU overhead is negligible (<1.0ms) since selection and instantiation occur only on scene initialization.

## Determinism
- **Required**: Yes, layout offsets must calculate deterministically based on character size.
- **Strategy**: Random selection utilizes `UnityEngine.Random.Range`. Position offset calculation computes X coordinates sequentially:
  ```csharp
  float sum = 0f;
  for (int r = 0; r < charSize; r++)
      sum += enemyRankSpacing * (nextEnemyRank - 1 + r);
  posX += sum / charSize;
  ```
- **Known exceptions**: Random selection is non-deterministic between runs as it depends on Unity's global pseudo-random number generator state.

## Authority Model
- **Single-player/offline**: Local client authority. Roster selection and run progression are computed in local memory.

## Performance Budget
- **CPU**: <1.0ms during startup spawn evaluation.
- **Memory**: <1MB heap footprint for configuration assets.
- **Entity scale target**: Supports up to 4 ranks occupied per team.

## Error Handling and Recovery
- **No Database Active**:
  - *Trigger*: `RunSessionManager.ActiveFormationDatabase` is null (e.g. combat scene loaded directly).
  - *Behavior*: Falls back to the hardcoded Inspector list `enemyTeamPrefabs` in `CombatSceneBootstrap`.
- **Single-Entry Database**:
  - *Trigger*: Database contains only 1 formation.
  - *Behavior*: Anti-repeat check is bypassed to guarantee the single available formation is always returned.
- **Null Prefab in Formation**:
  - *Trigger*: `EnemyFormationData` list has a null reference at an index.
  - *Behavior*: Spawning skips the index and attempts to place the next character in the next rank.

## Observability
- **Metrics**: Unknown
- **Logs**:
  - `[RunSessionManager] Initialized with database: {name}` (logged on database assignment)
  - `[RunSessionManager] Selected formation: {name}` (logged on dynamic selection)
  - `[Bootstrap] Spawning enemy {Name} at rank {rank} (size {size})` (logged during setup)
- **Traces/profilers**: None

## Acceptance Tests
- **Automated**:
  - `EnemyFormationSelectionTests`:
    - `GetNextRandomFormation_NeverReturnsConsecutiveDuplicates`: Asserts anti-repeat behavior over 100 iterations.
    - `GetNextRandomFormation_ReturnsNull_WhenNoDatabaseInitialized`: Verifies null safety.
  - `CombatSceneBootstrapFormationTests`:
    - `SpawnTeams_SpawnsFromDatabase_WhenDatabaseIsActive`: Confirms database overrides default inspector prefabs.
    - `SpawnTeams_HandlesMultiRankSpacingAndRanksCorrectly`: Verifies coordinate centering for size-2 units.
  - `CombatUITests`:
    - `HandleBattleEnded_ShowsNextRoomButton_OnVictory`: Verifies button is active on victory, inactive on defeat.
- **Playtest**:
  1. Open the Main Menu scene.
  2. Select 4 skills and click play.
  3. Win the first encounter.
  4. Verify the "Next Room" button displays, while the base battle end panel says "VICTORY!".
  5. Click "Next Room"; verify the scene reloads and spawns a new randomized formation.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
