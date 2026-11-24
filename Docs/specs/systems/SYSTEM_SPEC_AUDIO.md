# Audio System

Owner: Engineering
Status: active
Last verified: 2026-05-15
Verified commit: 4ea78565e7d646b5a59f1f7d7510b3765ed680ee
Target build: Unity 6000.3.9f1 (Windows)

## Purpose
Provide immersive audio feedback through dynamic background music (BGM) transitions and event-driven sound effects (SFX).

## Scope
- In scope:
  - BGM playback and manual crossfade-looping.
  - Main Menu music transitions via `MainMenuMusicController`.
  - Battle/Exploration music transitions via `BattleMusicController`.
  - Boss-specific music overrides via `CharacterData`.
  - Skill-based and event-based SFX.
  - Victory jingle playback.
  - Debug tools for BGM testing (Track skipping).
- Out of scope:
  - Dynamic MIDI generation.
  - 3D spatial audio (system uses 2D/global sound).
  - Voice acting/dialogue system.

## Source of Truth
- Code: `Assets/Scripts/Audio/AudioManager.cs` (`Nevergreen.Audio.AudioManager`)
- Integration: `Assets/Scripts/Combat/BattleMusicController.cs` (`Nevergreen.Combat.BattleMusicController`), `Assets/Scripts/UI/MainMenuMusicController.cs` (`Nevergreen.UI.MainMenuMusicController`)
- Tests: `Assets/Editor/Tests/AudioManagerTests.cs`, `Assets/Editor/Tests/BattleMusicControllerTests.cs`, `Assets/Editor/Tests/MainMenuMusicTests.cs`
- Design: `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` (Event hooks)
- Data: `Assets/Data/AudioConfig.asset` (BGM/Mixer configuration)

## Responsibilities
- **BGM Management**: Handle seamless transitions using a dual-source overlapping crossfade.
- **Manual Looping**: Detect track end proximity via polling and trigger crossfades to circumvent native Unity loop cuts.
- **SFX Playback**: Execute one-shot sounds for character actions and UI using `PlayOneShot` on a dedicated SFX source.
- **State Control**: React to Main Menu loads via `MainMenuMusicController`, and combat/exploration states via `BattleMusicController` checking `RunSessionManager.RoomCompleted` and reacting to `BattleSystem` events.
- **Audio Mixing**: Convert linear volume (0-1) to logarithmic decibels (-80dB to 0dB) for `AudioMixer` parameters.
- **Persistence**: Save and load volume settings to `PlayerPrefs` via the `AudioConfig` ScriptableObject.

## Data Model
- **AudioConfig (SO)**:
  - `masterVolume`: float (0-1)
  - `bgmVolume`: float (0-1)
  - `sfxVolume`: float (0-1)
  - `mainMixer`: AudioMixer (Reference to Unity AudioMixer asset)
  - `defaultMainMenuMusic`: AudioClip
  - `defaultExplorationMusic`: AudioClip
  - `defaultBattleMusic`: AudioClip
  - `victoryJingle`: AudioClip
- **SkillData (SO Extension)**:
  - `sfx`: AudioClip (Played when user starts skill)
- **CharacterData (SO Extension)**:
  - `deathSFX`: AudioClip
  - `bossMusicOverride`: AudioClip (Used instead of default battle music)

## Event Contracts
- **Event**: `Scene Loaded (Main Menu)`
  - Producer: Unity SceneManager
  - Consumer: `MainMenuMusicController` -> `AudioManager`
  - Payload: N/A
- **Event**: `OnBattleStarted`
  - Producer: `BattleSystem`
  - Consumer: `BattleMusicController` -> `AudioManager`
  - Payload: N/A
- **Event**: `OnBattleEnded`
  - Producer: `BattleSystem`
  - Consumer: `BattleMusicController` -> `AudioManager`
  - Payload: `BattleOutcome`
- **Event**: `PlaySoundStep`
  - Producer: `AnimationQueueProcessor`
  - Consumer: `AudioManager.PlaySFX`
  - Payload: `AudioClip`

## Timing Model
- **Update domain**: `Update` (Input polling), `Coroutine` (Volume fades/Loop monitoring).
- **Loop Trigger**: Triggered when `AudioSource.time >= clip.length - fadeDuration`.
- **Fade Duration**: Default 1500ms (BGM) / Variable (SFX).
- **Update order**: SFX triggered immediately upon animation step execution.

## Determinism
- **Required**: No. Audio is purely cosmetic and does not affect gameplay logic.
- **Strategy**: N/A.

## Authority Model
- Single-player/offline: `AudioManager` is a local singleton service initialized by `CombatSceneBuilder`.

## Performance Budget
- CPU: < 0.2ms per frame for fade calculations and time polling.
- Memory: < 128MB for active audio buffers.
- Entity scale target: Max 16 concurrent SFX voices.

## Error Handling and Recovery
- **Missing AudioClip**: Log `Warning` and skip playback; no execution break.
- **Overlapping Transitions**: `TransitionToBGM` stops existing `_crossfadeRoutine` to prevent volume fluttering.
- **Invalid Mixers**: Fallback to default `AudioSource` output if `AudioMixer` references are null.

## Observability
- Metrics: `active_voices` (Unity Profiler), `current_bgm_track` (Inspector).
- Logs: `[Audio] Transitioning to BattleMusic`, `[Audio] Loop triggered for: <ClipName>`.

## Acceptance Tests
- Automated: `AudioManagerTests` verifying routine cleanup and idempotency.
- Automated: `BattleMusicControllerTests` verifying battle event integration.
- Playtest: Trigger combat and verify exploration BGM fades into battle BGM.
- Playtest: Wait for BGM end and verify seamless overlap-looping.

## Missing Evidence
- **Exploration System Integration**: The "Exploration" state is currently placeholder; integration with a real exploration controller is Unknown.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
