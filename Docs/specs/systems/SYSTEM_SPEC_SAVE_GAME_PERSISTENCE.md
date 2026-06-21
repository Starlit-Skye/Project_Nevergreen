# Save Game Persistence System

Owner: Gameplay Engineering Team
Status: active
Last verified: 2026-06-21
Verified commit: 45e55deb5bc7a5783c252a40f7b4d3cafde13e18
Target build: Unity 6000.3.9f1 Standalone Windows

## Purpose
Serialize and persist the active run session progress, parts count, party state (including levels, traits, and equipped skills), HP, room completion status, next room selection choices, and enemy encounter seed to disk with local cryptographic obfuscation. Ensure the player can quit and resume runs cleanly, without state leakage, duplicate room progressions, or consecutive duplicate enemy formations.

## Scope
- In scope: Encryption and decryption of save file state using AES-256 with static pre-shared key (PSK) and initialization vector (IV). Disk persistence of party composition, individual party member levels, equipped skills, traits (perfections/imperfections), current/pre-combat HP, room progression number, parts count, current next room choices, room completed flag, and last selected enemy formation. Auto-restoring the `RunSessionManager` state on load, including resolving GUID/string references to database assets using the `GameDatabase` singleton. Short-circuiting combat team spawning and battle setup inside `CombatSceneBootstrap` if the loaded save state indicates the room was already completed.
- Out of scope: Cloud saves; auto-saving on every frame or input; persistence of meta-progression across independent runs; serialization of full combat visual/entity states mid-battle (resuming mid-combat loads the combat from the start using the pre-combat health).

## Source of Truth
- Code: `Assets/Scripts/Data/SaveManager.cs` (`Nevergreen.Data.SaveManager`), `Assets/Scripts/Prototype/CombatSceneBootstrap.cs` (`Nevergreen.Prototype.CombatSceneBootstrap`), `Assets/Scripts/Data/RunSessionManager.cs` (`Nevergreen.RunSessionManager`)
- Tests: `Assets/Editor/Tests/SaveManagerTests.cs` (`Nevergreen.Tests.SaveManagerTests` including `SaveRun_PersistsPartyMemberLevel` and others)
- Design: `Docs/prompts/llm-game-doc-writer.md` (formatting & requirements document)
- Data: `Assets/Scripts/Data/GameDatabase.cs` (reference resolver database)
- Issue/ADR: Unknown

## Responsibilities
- Serialize `RunSessionManager` state to `SaveDataDTO` and write to `save.dat` in `Application.persistentDataPath` using AES-256 encryption.
- Read, decrypt, and deserialize `SaveDataDTO` on startup or run continue request, populating `RunSessionManager` properties.
- Resolve character, skill, trait, and room references dynamically from the read DTO strings using the `GameDatabase` singleton.
- Synchronize pre-combat and current HP values to prevent mid-combat health manipulation and ensure HP integrity across saves.
- Track room completion status via `roomCompleted` and next room selections via `nextRoomChoices` to resume directly to the room selection UI when loading a completed room.
- Clear or apply the `ShouldUseSavedFormation` bypass rule conditionally on load to prevent enemy encounter leakage between rooms.
- Persist and restore parts count and each party member's level across saves.

## Data Model
- Entity/component/object:
  - `SaveDataDTO`: Root JSON-serializable DTO for persistence:
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
- Persistence keys: File-based persistence under `save.dat` in `Application.persistentDataPath`.

## Event Contracts
- Event: `SaveManager.SaveRun()`
  - Producer: `RunSessionManager.CompleteRoom()`, `RunSessionManager.OnSceneLoaded()`
  - Consumers: File system (`save.dat`)
  - Payload schema: AES-256 encrypted JSON payload of `SaveDataDTO`.
- Event: `SaveManager.LoadRun()`
  - Producer: Main Menu Continue Button (`CeciliaSkillSelectController.OnContinueClicked()`)
  - Consumers: `RunSessionManager` state properties
  - Payload schema: Deserialized fields from `SaveDataDTO`.

## Timing Model
- Update domain: Main thread synchronously.
- Tick/update order:
  - Saved on scene load after progression increment (`OnSceneLoaded`).
  - Saved immediately on room completion (`CompleteRoom`).
  - Loaded synchronously during the transition from the Main Menu.
- Budget: N/A (occurs during scene transitions).

## Determinism
- Required: Yes, for restoring the encounter anti-repeat state.
- Strategy: Restores the `LastSelectedFormation` reference on load so that subsequent random generation calls in the next room avoid selecting the same formation, while setting `ShouldUseSavedFormation` to `false` if `roomCompleted` is true to prevent spawning the same encounter in the next room.
- Known exceptions: None.

## Authority Model
- Single-player/offline: Complete write and read authority resides locally on the player's client device.
- Multiplayer: Unknown

## Performance Budget
- CPU: Under 5ms for standard JSON parsing and AES-256 encryption/decryption of a small state payload (typically < 10KB).
- Memory: Under 50KB temporary allocations per save/load cycle.
- Entity scale target: Up to 4 party members and up to 3 next room choices.

## Error Handling and Recovery
- Missing Save File: `LoadRun()` returns `false` safely, and the continue button is disabled or hidden.
- Database Misses: If a character, skill, trait, or room ID cannot be resolved using the `GameDatabase`, the reference is skipped safely with a logged warning, preventing crashes on stale saves.
- Corrupted Save File: Decryption or parsing exceptions are caught, returning `false` safely and logging an error.

## Observability
- Metrics: `SaveDataDTO` properties read/write state.
- Logs: Logs informational warnings on failed lookups and errors on file load/decryption failures under the `[SaveManager]` tag.
- Traces/profilers: None.

## Acceptance Tests
- Automated:
  - `Assets/Editor/Tests/SaveManagerTests.cs`:
    - `SaveRun_CreatesEncryptedFile`: Verifies that saving creates an encrypted file.
    - `HasSavedRun_ReturnsTrue_IfActiveRunExists`: Verifies standard run existence checking.
    - `ClearActiveRun_SetsHasActiveRunToFalse`: Verifies cleanup behavior.
    - `LoadRun_DeserializesPrimitiveState_EvenWithoutGameDatabase`: Verifies that room progression and basic states are deserialized.
    - `LoadRun_SetsShouldUseSavedFormation_WhenFormationIsLoaded`: Verifies that combat uses the saved formation upon resumption of an incomplete room.
    - `SaveRun_MidBattle_SavesPreCombatHP`: Verifies pre-combat HP integrity.
    - `SaveRun_RoomCompleted_PersistsSelectionAndState`: Verifies that room completed flag and generated choices are persisted.
    - `LoadRun_RoomCompleted_ClearsShouldUseSavedFormation_ButRestoresLastSelectedFormation`: Verifies that loading a completed room clears team bootstrap bypass but retains anti-repeat history.
    - `SaveRun_PersistsPartyMemberLevel`: Verifies that individual party member levels are persisted and restored.
- Playtest:
  1. Start a new run, clear Room 1, and wait for the room selection choices to appear.
  2. Quit the game, then click Continue from the Main Menu.
  3. Verify that you load directly into the room selection UI with Room 1 cleared and the same choices displayed.
  4. Select a room, enter combat, and verify that the progression count is incremented to 2.
  5. Upgrade a marionette, verify that parts count decreases, quit game, click Continue, and verify that parts count and marionette level are correctly loaded back.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
