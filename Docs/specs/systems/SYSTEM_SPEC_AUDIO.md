# Audio System

Owner: Engineering
Status: active
Last verified: 2026-05-15
Verified commit: 11ffc2162f9f059c644a723e608adfd24364f503
Target build: Unity 6000.3.9f1 (Windows)

## Purpose
Provide immersive audio feedback through dynamic background music (BGM) transitions and event-driven sound effects (SFX).

## Scope
- In scope:
  - BGM playback and crossfading.
  - Battle/Exploration music transitions.
  - Boss-specific music overrides.
  - Skill-based and event-based SFX.
  - Victory jingle playback.
- Out of scope:
  - Dynamic MIDI generation.
  - 3D spatial audio (system uses 2D/global sound).
  - Voice acting/dialogue system.

## Source of Truth
- Code: `Nevergreen.Audio.AudioManager` (Proposed)
- Tests: `Tests/Editor/AudioSystemTests.cs` (Proposed)
- Design: `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` (Event hooks)
- Data: `Assets/Data/AudioConfig.asset` (Proposed)

## Responsibilities
- **BGM Management**: Handle seamless transitions between exploration and combat music.
- **SFX Playback**: Execute one-shot sounds for character actions and UI.
- **State Control**: React to `BattleSystem` events to trigger state-appropriate music.
- **Audio Mixing**: Route audio through specific channels (Master, BGM, SFX) for balanced output.
- **Settings Management**: Provide logarithmic volume control and persistent player preferences.

## Data Model
- **AudioConfig (SO)**:
  - `masterVolume`: float (0-1)
  - `bgmVolume`: float (0-1)
  - `sfxVolume`: float (0-1)
  - `mainMixer`: AudioMixer (Reference to the Unity AudioMixer asset)
  - `defaultExplorationMusic`: AudioClip
  - `defaultBattleMusic`: AudioClip
  - `victoryJingle`: AudioClip
- **SkillData (SO Extension)**:
  - `sfx`: AudioClip (Played when user starts skill)
- **CharacterData (SO Extension)**:
  - `deathSFX`: AudioClip
  - `bossMusicOverride`: AudioClip (If set, used instead of default battle music)

## Event Contracts
- **BGM Transition Trigger**:
  - Producer: `BattleSystem` (`OnBattleStarted`, `OnBattleEnded`)
  - Consumer: `AudioManager`
  - Payload: `BattleOutcome` (for victory jingle)
- **SFX Trigger**:
  - Producer: `BattleSystem.ExecuteSkill`, `CombatCharacter.HandleCharacterDefeated`
  - Consumer: `AnimationQueueProcessor` via `PlaySoundStep`

## Audio Mixing & Settings
### AudioMixer Hierarchy
- **Master Group**: Top-level gain control.
- **BGM Group**: Routes all music tracks.
- **SFX Group**: Routes all one-shot sound effects.

### Volume Scaling
The system uses **Logarithmic Scaling** for volume control to match human auditory perception. Linear slider values (0.0 to 1.0) are converted to decibels (-80dB to 0dB) before being applied to the AudioMixer.

### Persistence
Volume settings are saved to `PlayerPrefs` via the `AudioConfig` ScriptableObject upon modification, ensuring user preferences persist between sessions.

## Timing Model
- **Update domain**: `Update` for volume fades.
- **Fade Duration**: Default 1500ms for crossfades.
- **SFX Latency**: < 16ms (triggered immediately on animation step start).

## Determinism
- **Required**: No. Audio is purely cosmetic and does not affect gameplay logic.
- **Strategy**: N/A.

## Authority Model
- Single-player/offline: `AudioManager` is a local singleton/service.

## Performance Budget
- CPU: < 0.2ms per frame (fade calculations).
- Memory: < 64MB for active audio buffers.
- Entity scale target: Max 16 concurrent SFX voices.

## Error Handling and Recovery
- **Missing AudioClip**: Log warning and skip playback; do not break execution.
- **AudioDevice Lost**: Unity native handling; `AudioManager` continues state tracking.

## Observability
- Metrics: `active_voices`, `current_bgm_track`
- Logs: `[Audio] Transitioning to BattleMusic`, `[Audio] Playing SFX: Cast_Fireball`

## Acceptance Tests
- Automated: `AudioTransitionTests` verifying `BGM_Source.clip` changes on `BattleSystem` events.
- Playtest: Start battle and verify exploration music fades out while battle music fades in.

## Validation
- [ ] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
