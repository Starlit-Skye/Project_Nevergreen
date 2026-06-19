# Character Database System

Owner: Gameplay Engineering Team
Status: active
Last verified: 2026-06-19
Verified commit: 4134da54597e5d2ec8192eeff46cf998a27bb03c
Target build: Unity 6000.3.9f1 Standalone Windows

## Purpose
Define ScriptableObject-backed character data templates for both player units (marionettes) and enemy units, and resolve character stat blocks at runtime per character level.

## Scope
- In scope: Character template schema (`CharacterData`), team categorization, unique ID validation, stat-per-level resolution, skill pools, rank sizing, leaves-pile-on-death rules, and default AI profiles.
- Out of scope: Action scheduling details during turn generation, specific combat formulas using the stats, and custom GUI inspectors for the ScriptableObject.

## Source of Truth
- Code: `Assets/Scripts/Data/CharacterData.cs` (`Nevergreen.Data.CharacterData`), `Assets/Scripts/Data/StatBlockData.cs` (`Nevergreen.Data.StatBlockData`)
- Tests: `Assets/Editor/Tests/MarionetteGeneratorTests.cs` (batch generation class requirements and templating), `Assets/Editor/Tests/MainMenuSkillSelectionTests.cs` (direct prefab spawning logic based on CharacterData).
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0 (Technical -> Character Database)
- Data: `Assets/Data/Characters/` (instances of CharacterData ScriptableObjects).
- Issue/ADR: None.

## Responsibilities
- Define a serialization-friendly template for all combat characters (enemies/player units) using `CharacterData` ScriptableObject.
- Require each template to provide a unique `characterId`, human-readable `displayName`, visual `characterPrefab`, and `teamType`.
- Map level progressions to stats via a list of `StatBlockData` references, where level N maps to index `N - 1`.
- Provide helper methods (`GetStatsForLevel`) that safely clamp level indexing to valid bounds of the stat list.
- Store available/equipped skill lists and the overall selection pool for player customisation.
- Categorize rank sizes (1-4) and toggles for corpse pile generation upon unit death.

## Data Model
- Entity/component/object:
  - `CharacterData` (ScriptableObject): Defines character baseline configuration.
    - `characterId` (`string`): Unique identifier of the template.
    - `displayName` (`string`): User-facing name.
    - `characterPrefab` (`CombatCharacter`): Target visual and component prefab to instantiate.
    - `teamType` (`CharacterTeamType`): Player or Enemy.
    - `actionsPerRound` (`int`): Action count (defaults to 1, clamped to min 1).
    - `statPerLevel` (`List<StatBlockData>`): List of stat assets corresponding to each level.
    - `availableSkills` (`List<SkillData>`): Equipped or active skills.
    - `totalSkillPool` (`List<SkillData>`): Skill pool candidates for skill selection.
    - `size` (`int`): Rank size footprint (1 to 4).
    - `leavesPileOnDeath` (`bool`): True if unit transforms into a pile corpse.
    - `defaultAIProfile` (`EnemyAIProfile`): AI script used if the unit belongs to the enemy team.
    - `deathSFX` (`AudioClip`): Auditory feedback on defeat.
    - `bossMusicOverride` (`AudioClip`): Override background music track for boss fights.
- Persistence keys: Save/Load systems serialize characters by referencing their `characterId` string, resolving details at runtime via the `GameDatabase` registries.

## Event Contracts
- Event: `CharacterData.GetStatsForLevel()` lookup
  - Producer: `CombatCharacter.Initialize()`
  - Consumers: Battle initialization and UI level scaling systems.
  - Payload schema: Returns the `StatBlockData` asset at index `level - 1`.

## Timing Model
- Update domain: Main thread.
- Tick/update order: Read-only lookup occurring during scene setup, team spawning, and level-ups. Data is immutable at runtime.
- Budget: Under 0.1ms per lookup.

## Determinism
- Required: Yes.
- Strategy: Level lookup resolves to `level - 1` clamped to `[0, statPerLevel.Count - 1]` to ensure consistent stats for any level query.
- Known exceptions: None.

## Authority Model
- Single-player/offline: Complete local client authority over lookup tables.
- Multiplayer: Unknown.

## Performance Budget
- CPU: Under 1ms to load and resolve character templates.
- Memory: Under 10KB per character definition asset instance in memory.
- Entity scale target: Up to 20 distinct character definitions loaded in a scene.

## Error Handling and Recovery
- Missing Stat Entries: If `statPerLevel` is null or empty, `GetStatsForLevel` prints a `Debug.LogError` and returns `null`.
- Out-of-bounds Levels: Clamps the level query index to valid ranges (`Mathf.Clamp(level - 1, 0, statPerLevel.Count - 1)`) so query values too low or too high resolve to the closest defined level.

## Observability
- Metrics: None.
- Logs: `[CharacterData] '<displayName>' has no stat entries.` logged as error if query is executed on unconfigured characters.
- Traces/profilers: None.

## Acceptance Tests
- Automated:
  - `MainMenuSkillSelectionTests.cs`:
    - `CombatSceneBootstrap_SpawnTeams_SpawnsDirectCharacterPrefab_WhenConfiguredOnCharacterData`: Verifies team spawner correctly instantiates the character prefab defined in the template.
- Playtest:
  1. In-editor, select a character asset and inspect the `Stat Per Level` configurations.
  2. Start a match, and verify that the stats displayed match the defined level index.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
