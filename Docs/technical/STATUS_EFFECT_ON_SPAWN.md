# Status Effect on Spawn System

Owner: Combat Engineering Team
Status: active
Last verified: 2026-06-30
Verified commit: 948d7b7790470ddb96f51de416094b0cdc21456c
Target build: Unity 6000.3.9f1 + Windows

## Purpose
The Status Effect on Spawn system allows developers to configure status effects directly on character prefabs that automatically apply to the corresponding `CombatCharacter` as soon as they are instantiated and initialized for battle.

## Scope
- In scope:
  - Configuration of status effects (type, duration, amplitude, amplitude type, target stat) via `StatusEffectOnSpawn` components on character prefabs.
  - Automated application during character initialization.
- Out of scope:
  - Visual rendering or VFX animations associated with the status application.
  - Persisting applied spawn status effects across separate combat instances.

## Source of Truth
- Code: `Assets/Scripts/Combat/StatusEffectOnSpawn.cs` (Status effect on spawn MonoBehaviour component)
- Code: `Assets/Scripts/Combat/CombatCharacter.cs` (Hook within `InitializeForCombat`)
- Tests: `Assets/Editor/Tests/StatusEffectOnSpawnTests.cs` (Unit tests verifying application)
- Design: `Docs/specs/architecture/ARCHITECTURE_SPEC_COMBAT_RUNTIME.md` (Combat setup phase sequence)

## Responsibilities
- Hold the design-time configuration of a status effect (type, duration, amplitude, amplitude type, target stat) on a prefab.
- Instantiate a `StatusEffectInstance` with the configured values.
- Apply the status effect to the target `CombatCharacter` during its combat setup phase.

## Data Model
- `StatusEffectOnSpawn` (MonoBehaviour):
  - `StatusType statusType`: The type of status effect to apply.
  - `int duration`: The duration of the status effect in turns.
  - `float amplitude`: The modification value of the status effect.
  - `AmplitudeType amplitudeType`: Method of amplitude application (Percentage or Flat).
  - `StatTarget targetStat`: The specific combat stat targeted by the status effect.

## Event Contracts
- Event: `OnStatusApplied`
  - Producer: `CombatCharacter.AddStatus` (triggered via `ApplyTo`)
  - Consumers: UI systems, combat log, and subscriber systems
  - Payload schema: `(CombatCharacter character, StatusType type, bool succeeded, StatTarget? targetStat)`

## Timing Model
- Update domain: Initialization phase.
- Tick/update order: Processed inside `CombatCharacter.InitializeForCombat` immediately after resetting status effects and prior to trait activation or active combat start.
- Budget: < 0.1ms per component.

## Determinism
- Required: Yes.
- Strategy: Ordered iteration. Components are retrieved using `GetComponents<StatusEffectOnSpawn>()` which returns components in their inspector layout order. Status effects are applied in sequence.
- Known exceptions: None.

## Authority Model
- Single-player/offline: Full authority on the local client.

## Performance Budget
- CPU: Negligible (< 0.1ms total setup time during combat loading).
- Memory: Minimal allocation overhead of `StatusEffectInstance` objects.
- Entity scale target: Up to 8 characters (4 player units, 4 enemy units) applying spawn status effects.

## Error Handling and Recovery
- Invalid status configuration (e.g. invalid type or stat targets): Will compile, but may produce logical issues inside `StatusProcessor`.

## Observability
- Metrics: None.
- Logs: Logs standard `OnStatusApplied` event triggers when applied.
- Traces/profilers: Unity Profiler markers for combat initialization phase.

## Acceptance Tests
- Automated:
  - `Assets/Editor/Tests/StatusEffectOnSpawnTests.cs` -> `InitializeForCombat_AppliesStatusEffectOnSpawn`: Verifies a single status effect is added to the character's list on setup.
  - `Assets/Editor/Tests/StatusEffectOnSpawnTests.cs` -> `InitializeForCombat_AppliesMultipleStatusEffectOnSpawn`: Verifies multiple components are applied in the order they are defined.
- Playtest:
  - Attach a `StatusEffectOnSpawn` component configuring `StatusType.Stealth` to a character prefab. Ensure the character begins combat with the Stealth effect active.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
