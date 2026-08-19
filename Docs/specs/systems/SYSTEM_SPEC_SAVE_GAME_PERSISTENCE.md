# Save Game Persistence System

Owner: Gameplay Engineering Team
Status: active
Last verified: 2026-08-18
Verified commit: df2835d860cbde7e4b5db8e5ea71934423f9dd4a
Target build: Unity 6000.3.9f1 Standalone Windows

## Purpose
Serialize and persist the active run session progress, parts count, scraps count, party state (including levels, traits, and equipped skills), HP, room completion status, next room selection choices, and enemy encounter seed to disk with local cryptographic obfuscation. Additionally, manage meta-progression across runs (like boss encounter selection probabilities) independently of the active run lifecycle. Ensure the player can quit and resume runs cleanly, without state leakage, duplicate room progressions, or consecutive duplicate enemy formations, while preserving meta-progression adjustments across subsequent run sessions.

## Scope
- In scope:
  - Encryption and decryption of save file state using AES-256 with static pre-shared key (PSK) and initialization vector (IV).
  - Disk persistence of active run state (party composition, individual party member levels, equipped skills, traits (perfections/imperfections), current/pre-combat HP, room progression number, parts count, scraps count, current next room choices, room completed flag, and last selected enemy formation) to `run.dat`.
  - Disk persistence of permanent meta-progression (boss encounter probability decay / boss chances) to a separate `profile.dat`.
  - Auto-restoring the `RunSessionManager` state on load, including resolving GUID/string references to database assets using the `GameDatabase` singleton.
  - Lazy-loading profile meta-progression on demand from `profile.dat` and auto-saving whenever meta-progression properties are updated.
  - Short-circuiting combat team spawning and battle setup inside `CombatSceneBootstrap` if the loaded save state indicates the room was already completed.
- Out of scope: Cloud saves; auto-saving on every frame or input; serialization of full combat visual/entity states mid-battle (resuming mid-combat loads the combat from the start using the pre-combat health).

## Source of Truth
- Code: `Assets/Scripts/Data/SaveManager.cs` (`Nevergreen.Data.SaveManager`), `Assets/Scripts/Prototype/CombatSceneBootstrap.cs` (`Nevergreen.Prototype.CombatSceneBootstrap`), `Assets/Scripts/Data/RunSessionManager.cs` (`Nevergreen.RunSessionManager`)
- Tests: `Assets/Editor/Tests/SaveManagerTests.cs` (`Nevergreen.Tests.SaveManagerTests`), `Assets/Editor/Tests/EnemyFormationSelectionTests.cs` (`Nevergreen.Tests.EnemyFormationSelectionTests`)
- Design: `Docs/prompts/llm-game-doc-writer.md` (formatting & requirements document)
- Data: `Assets/Scripts/Data/GameDatabase.cs` (reference resolver database)
- Issue/ADR: Unknown

## Responsibilities
- Serialize `RunSessionManager` active run state to `SaveDataDTO` and write to `run.dat` in `Application.persistentDataPath` using AES-256 encryption.
- Read, decrypt, and deserialize `SaveDataDTO` from `run.dat` on startup or run continue request, populating active `RunSessionManager` properties.
- Serialize meta-progression state (boss chances) to `ProfileSaveDataDTO` and write to `profile.dat` in `Application.persistentDataPath` using AES-256 encryption.
- Lazy-load meta-progression (boss chances) from `profile.dat` upon first query to `RunSessionManager.BossFormationChances`, caching the data and writing changes immediately when altered.
- Resolve character, skill, trait, and room references dynamically from the read DTO strings using the `GameDatabase` singleton.
- Synchronize pre-combat and current HP values to prevent mid-combat health manipulation and ensure HP integrity across saves.
- Track room completion status via `roomCompleted` and next room selections via `nextRoomChoices` to resume directly to the room selection UI when loading a completed room.
- Clear or apply the `ShouldUseSavedFormation` bypass rule conditionally on load to prevent enemy encounter leakage between rooms.
- Persist and restore parts count and each party member's level across saves.

## Data Model
- Entity/component/object:
  - `SaveDataDTO`: Root JSON-serializable DTO for active run persistence:
    - `hasActiveRun` (`bool`): Flag indicating if an active run session can be resumed.
    - `roomProgression` (`int`): The current progression count.
    - `parts` (`int`): The current parts count balance.
    - `nextRoomId` (`string`): The room ID of the currently active/next room.
    - `lastSelectedFormationId` (`string`): The ID of the last enemy formation fought.
    - `roomCompleted` (`bool`): True if the current room has been cleared and the selection panel is active.
    - `nextRoomChoices` (`List<string>`): List of room IDs representing the generated next room selection choices.
    - `party` (`List<PartyMemberDTO>`): List of active party members.
  - `PartyMemberDTO`: JSON-serializable representation of a party unit:
    - `characterId` (`string`): Reference to character data.
    - `currentHP` (`int`): Current/pre-combat HP (-1 represents null/max HP).
    - `level` (`int`): Level of the party unit.
    - `equippedSkillIds` (`List<string>`): IDs of equipped skills.
    - `perfectionIds` (`List<string>`): IDs of active perfections.
    - `imperfectionIds` (`List<string>`): IDs of active imperfections.
  - `ProfileSaveDataDTO`: Root JSON-serializable DTO for meta-progression profile persistence:
    - `bossChances` (`List<BossProbabilityDTO>`): List of saved probabilities for boss formations.
  - `BossProbabilityDTO`: JSON-serializable representation of a boss formation probability mapping:
    - `formationId` (`string`): Reference to boss formation ID.
    - `chance` (`float`): The cached choice probability weight [0.0 - 1.0].
- Persistence keys:
  - File-based persistence under `run.dat` (active run data) and `profile.dat` (meta-progression data) in `Application.persistentDataPath`.

## Event Contracts
- Event: `SaveManager.SaveRun()`
  - Producer: `RunSessionManager.CompleteRoom()`, `RunSessionManager.OnSceneLoaded()`, `RunSessionManager.Initialize()`
  - Consumers: File system (`run.dat`)
  - Payload schema: AES-256 encrypted JSON payload of `SaveDataDTO`.
- Event: `SaveManager.LoadRun()`
  - Producer: Main Menu Continue Button (`CeciliaSkillSelectController.OnContinueClicked()`)
  - Consumers: `RunSessionManager` active run properties
  - Payload schema: Deserialized fields from `SaveDataDTO`.
- Event: `SaveManager.SaveProfile()`
  - Producer: `RunSessionManager.SelectBossFormationWeighted()`, manual updates
  - Consumers: File system (`profile.dat`)
  - Payload schema: AES-256 encrypted JSON payload of `ProfileSaveDataDTO`.
- Event: `SaveManager.LoadProfile()`
  - Producer: Lazy-loaded getter `RunSessionManager.BossFormationChances`
  - Consumers: `RunSessionManager` meta-progression variables
  - Payload schema: Deserialized fields from `ProfileSaveDataDTO`.

## Timing Model
- Update domain: Main thread synchronously.
- Tick/update order:
  - Run data saved on scene load after progression increment (`OnSceneLoaded`).
  - Run data saved immediately on room completion (`CompleteRoom`).
  - Run data loaded synchronously during the transition from the Main Menu.
  - Profile meta-progression data is lazy-loaded synchronously on first access of `RunSessionManager.BossFormationChances`.
  - Profile meta-progression data is auto-saved synchronously immediately after updating probabilities during weighted boss selection.
- Budget: N/A (occurs during scene transitions or isolated probability evaluations).

## Determinism
- Required: Yes, for restoring the encounter anti-repeat state and persisting boss choice probability shifts.
- Strategy: Restores the `LastSelectedFormation` reference on load so that subsequent random generation calls in the next room avoid selecting the same formation, while setting `ShouldUseSavedFormation` to `false` if `roomCompleted` is true to prevent spawning the same encounter in the next room. Persists updated boss probabilities to profile data immediately to enforce decaying weight distributions across successive runs.
- Known exceptions: None.

## Authority Model
- Single-player/offline: Complete write and read authority resides locally on the player's client device.
- Multiplayer: Unknown

## Performance Budget
- CPU: Under 5ms for standard JSON parsing and AES-256 encryption/decryption of small state payloads (typically < 10KB).
- Memory: Under 50KB temporary allocations per save/load cycle.
- Entity scale target: Up to 4 party members, up to 3 next room choices, and 2 boss selection chances.

## Error Handling and Recovery
- Missing Run Save File: `LoadRun()` returns `false` safely, and the continue button is disabled or hidden.
- Missing Profile Save File: `LoadProfile()` returns `null` safely, falling back to default equal weights (50/50).
- Database Misses: If a character, skill, trait, or room ID cannot be resolved using the `GameDatabase`, the reference is skipped safely with a logged warning, preventing crashes on stale saves.
- Corrupted Save File: Decryption or parsing exceptions are caught, returning `false`/`null` safely and logging an error.

## Observability
- Metrics: `SaveDataDTO` and `ProfileSaveDataDTO` properties read/write state.
- Logs: Logs informational warnings on failed lookups and errors on file load/decryption failures under the `[SaveManager]` tag.
- Traces/profilers: None.

## Acceptance Tests
- Automated:
  - `Assets/Editor/Tests/SaveManagerTests.cs`:
    - `SaveRun_CreatesEncryptedFile`: Verifies that saving creates an encrypted `run.dat` file.
    - `HasSavedRun_ReturnsTrue_IfActiveRunExists`: Verifies run existence checking for the active run.
    - `ClearActiveRun_SetsHasActiveRunToFalse`: Verifies `run.dat` cleanup behavior while leaving `profile.dat` intact.
    - `LoadRun_DeserializesPrimitiveState_EvenWithoutGameDatabase`: Verifies that room progression and basic states are deserialized.
    - `LoadRun_SetsShouldUseSavedFormation_WhenFormationIsLoaded`: Verifies that combat uses the saved formation upon resumption of an incomplete room.
    - `SaveRun_MidBattle_SavesPreCombatHP`: Verifies pre-combat HP integrity.
    - `SaveRun_RoomCompleted_PersistsSelectionAndState`: Verifies that room completed flag and generated choices are persisted.
    - `LoadRun_RoomCompleted_ClearsShouldUseSavedFormation_ButRestoresLastSelectedFormation`: Verifies that loading a completed room clears team bootstrap bypass but retains anti-repeat history.
    - `SaveRun_PersistsPartyMemberLevel`: Verifies that individual party member levels are persisted and restored.
    - `SaveProfile_CreatesEncryptedProfileFile`: Verifies profile save creates encrypted `profile.dat`.
    - `ClearActiveRun_DoesNotDeleteOrAlterProfileFile`: Verifies clearing active run leaves `profile.dat` untouched.
    - `LoadProfile_RestoresBossChances_WithoutActiveRun`: Verifies loading profile data successfully restores boss chances even without an active run save.
  - `Assets/Editor/Tests/EnemyFormationSelectionTests.cs`:
    - `BossFormationChances_PersistAcrossRuns`: Verifies boss formation chances persist and lazy-load properly across simulated restarts.
- Playtest:
  1. Start a new run, clear Room 1, and wait for the room selection choices to appear.
  2. Quit the game, then click Continue from the Main Menu.
  3. Verify that you load directly into the room selection UI with Room 1 cleared and the same choices displayed.
  4. Select a room, enter combat, and verify that the progression count is incremented to 2.
  5. Upgrade a marionette, verify that parts count decreases, quit game, click Continue, and verify that parts count and marionette level are correctly loaded back.
  6. Reach the boss room, verify that one of the two bosses is chosen, then exit the run/game. Check that the probability offset (e.g. 40%/60%) is preserved and loaded on a subsequent new run session.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
