# Main Menu and Skill Selection System

Owner: Dev Team
Status: active
Last verified: 2026-05-30
Verified commit: 6dab85bd03ad290dba585a16406e625b4dc0a73b
Target build: Unity 6 (6000.3.9f1) + PC/Standalone

## Purpose
Provides the Main Menu and character skill preparation interface prior to entering a combat run. It manages character roster persistence, validation rules for skill configuration, visual grid layouts for available skills, dynamic party initialization, and active enemy encounter database injection for the combat run.

## Scope
- **In scope**:
  - Main menu UI panel and navigation flows.
  - Character skill selection panel layout and logic (equipping exactly 4 skills).
  - 3x4 grid rendering of the selectable skill pool.
  - Persistent run session state holding party roster data across scene transitions.
  - Active enemy formation database pool initialization at run start.
  - Dynamic, configuration-driven player character spawning inside the combat scene.
- **Out of scope**:
  - Equipment/gear selection and stat passive configuration (classes prepared for future expansion but visual UI is out of scope).
  - Character leveling/progression modifications inside the menu.
  - Multi-character roster assembly UI (currently hardcoded to Cecilia preparation).

## Source of Truth
- **Code**:
  - `Assets/Scripts/Data/PartyMemberInfo.cs` (`PartyMemberInfo` data container)
  - `Assets/Scripts/Data/RunSessionManager.cs` (`RunSessionManager` static manager)
  - `Assets/Scripts/UI/CeciliaSkillSelectController.cs` (`CeciliaSkillSelectController` UI component)
  - `Assets/Scripts/Combat/CombatCharacter.cs` (`CombatCharacter.InitializeForCombat` skill loader)
  - `Assets/Scripts/Prototype/CombatSceneBootstrap.cs` (`CombatSceneBootstrap.SpawnTeams` layout spawner)
  - `Assets/Scripts/Data/EnemyFormationDatabase.cs` (`EnemyFormationDatabase` asset)
- **Tests**:
  - `Assets/Editor/Tests/MainMenuSkillSelectionTests.cs` (`MainMenuSkillSelectionTests` test fixture)
  - `Assets/Editor/Tests/EnemyFormationSelectionTests.cs` (random selection and anti-repeat verification)
- **Design**:
  - Main Menu and Skill Selection UI specification layout.
- **Data**:
  - `Assets/Scenes/MainMenu.unity` (scene containing UGUI panels and canvas)
  - `Assets/Prefabs/UI/SkillListItem.prefab` (visual grid cell button)
- **Issue/ADR**: Unknown

## Responsibilities
- Provide a responsive title screen with navigation to character setup.
- Enforce the validation rule that a character must equip exactly 4 skills before a combat run can start.
- Render available skills dynamically from a character's total pool in a 3-column grid.
- Persist the selected roster and skill loadouts in memory using `RunSessionManager`.
- Initialize `RunSessionManager` with the configured active enemy formation database.
- Intercept the standard player team spawning in the combat scene to only instantiate characters listed in the active session roster.

## Data Model
- `RunSessionManager.CurrentParty`: `List<PartyMemberInfo>`
- `RunSessionManager.ActiveFormationDatabase`: `EnemyFormationDatabase`
- `PartyMemberInfo`:
  - `character`: `CharacterData` (ScriptableObject reference containing stats/total pool)
  - `equippedSkills`: `List<SkillData>` (exactly 4 elements when run starts)
- **Persistence keys**: None (in-memory state only; not serialized to persistent disk storage).

## Event Contracts
- Event: `PlayButton.onClick`
  - Producer: UGUI Button
  - Consumers: `CeciliaSkillSelectController`
  - Effect: Opens the skill selection panel and generates skill pool items.
- Event: `CloseButton.onClick`
  - Producer: UGUI Button
  - Consumers: `CeciliaSkillSelectController`
  - Effect: Closes the skill selection panel, returns to main menu screen.
- Event: `StartButton.onClick`
  - Producer: UGUI Button
  - Consumers: `CeciliaSkillSelectController`
  - Effect: Stores active roster, registers active `EnemyFormationDatabase` to `RunSessionManager`, and loads the combat prototype scene.
- Event: `SkillListItemButton.onClick`
  - Producer: UGUI Button (individual skill cell)
  - Consumers: `CeciliaSkillSelectController`
  - Effect: Toggles equipping/unequipping the selected skill.

## Timing Model
- **Update domain**: Main thread MonoBehaviour lifecycle (`Start`, `Update`, and synchronous UGUI callbacks).
- **Tick/update order**: Spawning in `CombatSceneBootstrap.Start()` runs and finishes before `BattleSystem.StartBattle()` initializes the combat loop.
- **Budget**: Unknown (UI refreshes execute within a single frame; CPU overhead is negligible).

## Determinism
- **Required**: Yes, player spawning sequence and layout positioning must match the active roster order.
- **Strategy**: Sequentially traverses `RunSessionManager.CurrentParty` to resolve prefabs and position them at fixed ranks in ascending order.
- **Known exceptions**: None.

## Authority Model
- **Single-player/offline**: Local client has absolute authority. UI and roster state are modified directly in the local memory heap.

## Performance Budget
- **CPU**: <1.0ms per frame for UI state refresh.
- **Memory**: <5MB heap footprint for UI panel loading and dynamic text layouts.
- **Entity scale target**: 1 character selection controller, 4 active equipped slots, up to 12 selectable skills visible in the pool grid.

## Error Handling and Recovery
- **Missing Prefab in Bootstrap**:
  - *Trigger*: Roster has a character that is missing a corresponding prefab in `CombatSceneBootstrap.playerTeamPrefabs`.
  - *Behavior*: Logs a warning (`[Bootstrap] Could not find prefab for character...`) and skips spawning that slot, preventing scene load crash.
- **Empty Active Session**:
  - *Trigger*: Combat scene loaded directly from the Unity Editor (`RunSessionManager.CurrentParty` is empty).
  - *Behavior*: Bootstrapper falls back to spawning the default design-time team prefabs configured in the Inspector.
- **Spawned Character not in Session**:
  - *Trigger*: Character prefab is spawned in combat, but no session data is found for it in `RunSessionManager.CurrentParty`.
  - *Behavior*: Falls back to equipping default skills listed in `CharacterData.availableSkills`.

## Observability
- **Metrics**: Unknown.
- **Logs**:
  - `[Bootstrap] Spawned player: {DisplayName} at rank {rank}` (logged on successful unit initialization).
  - `[Bootstrap] Could not find prefab for character '{Name}'...` (logged on missing prefab mapping).
- **Traces/profilers**: None.

## Acceptance Tests
- **Automated**:
  - `Assets/Editor/Tests/MainMenuSkillSelectionTests.cs`:
    - `RunSessionManager_Clear_ClearsRoster()`: Verifies run session data resets cleanly.
    - `CombatCharacter_InitializeForCombat_UsesSessionSkills_IfMatching()`: Verifies session-equipped skills are applied in combat.
    - `CombatCharacter_InitializeForCombat_FallsBackToDefaultSkills_IfNotInSession()`: Verifies design-time fallback to default skills.
    - `CombatSceneBootstrap_SpawnTeams_SpawnsOnlySessionCharacters_WhenSessionIsActive()`: Verifies that spawning aligns strictly with session configuration.
    - `CombatSceneBootstrap_SpawnTeams_SpawnsAllPrefabs_WhenSessionIsEmpty()`: Verifies default team fallback logic in direct playtesting.
- **Playtest**:
  1. Open the Main Menu scene.
  2. Click the Play button; verify the Skill Selection Panel is shown.
  3. Verify that selecting skills updates the equipped slots and toggling a 5th skill is prevented.
  4. Verify the Start button is interactable if and only if exactly 4 skills are selected.
  5. Click Start; verify the combat scene is loaded and only Cecilia is spawned with the 4 selected skills.

## Missing Evidence
- **Issue/ADR**: Unknown (No architectural decision records are registered for menu flow).
- **Performance metrics**: Unknown (No official timing or memory benchmarks have been performed).

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
