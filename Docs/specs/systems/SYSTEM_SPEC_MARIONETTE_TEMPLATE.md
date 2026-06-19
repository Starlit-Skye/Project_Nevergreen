# Marionette Template System

Owner: Gameplay Engineering Team
Status: active
Last verified: 2026-06-19
Verified commit: 4134da54597e5d2ec8192eeff46cf998a27bb03c
Target build: Unity 6000.3.9f1 Standalone Windows

## Purpose
Define the baseline data model and runtime state management for player-controlled marionette units during a run session.

## Scope
- In scope: Runtime party member attributes (`PartyMemberInfo`), skill slot assignment, trait management (Perfections and Imperfections), health persistence across room boundaries, and capacity limits.
- Out of scope: The procedural generation logic (handled by `MarionetteGenerator`), and UI layout of the character sheet.

## Source of Truth
- Code: `Assets/Scripts/Data/PartyMemberInfo.cs` (`Nevergreen.Data.PartyMemberInfo`), `Assets/Scripts/Data/CharacterData.cs` (`Nevergreen.Data.CharacterData`)
- Tests: `Assets/Editor/Tests/TraitTests.cs` (verifying trait capacity limits and uniqueness logic within `PartyMemberInfo`), `Assets/Editor/Tests/SaveManagerTests.cs` (verifying marionette serialization).
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0 (sections: Marionettes, Marionette Classes, Stats, Skills)
- Data: `Assets/Data/Characters/`
- Issue/ADR: None.

## Responsibilities
- Store runtime progression and state details for individual player units in `PartyMemberInfo`.
- Reference a static `CharacterData` template defining baseline visual assets and growth properties.
- Track persistent health using `currentHP` (active state) and `preCombatHP` (state at start of combat to guard against save manipulation).
- Provide capacity-guarded trait slots using `TryAddTrait` and `RemoveTrait` methods.
- Enforce unique trait constraints (by ID) and capacity limits configured globally in `GlobalConfig` (e.g. `maxPerfections` and `maxImperfections`).

## Data Model
- Entity/component/object:
  - `PartyMemberInfo` (Serializable C# class): Runtime state container.
    - `character` (`CharacterData`): Static baseline template reference.
    - `equippedSkills` (`List<SkillData>`): Current equipped skill list (up to 4).
    - `currentHP` (`int?`): Runtime health state (null represents full health).
    - `preCombatHP` (`int?`): Health state when entering the current room.
    - `perfections` (`List<TraitData>`): List of active perfection traits.
    - `imperfections` (`List<TraitData>`): List of active imperfection traits.
- Persistence keys: Save DTO structures serialize marionette configurations into unique string identifiers (`characterId`, `equippedSkillIds`, `perfectionIds`, `imperfectionIds`) to restore state on continue.

## Event Contracts
- Event: Trait addition query
  - Producer: Trait selection UI or room reward strategy.
  - Consumers: `PartyMemberInfo.TryAddTrait()`
  - Payload schema: Trait instance reference; returns boolean indicating success.

## Timing Model
- Update domain: Main thread.
- Tick/update order: Initialized during run startup, updated when picking rewards or taking damage in combat, serialized on auto-save cycles.
- Budget: Under 0.1ms per operation.

## Determinism
- Required: Yes.
- Strategy: Trait slots and capacity calculations rely on static rules defined in global configurations.
- Known exceptions: None.

## Authority Model
- Single-player/offline: Local client authority.
- Multiplayer: Unknown.

## Performance Budget
- CPU: Under 0.05ms per operation.
- Memory: Under 1KB per unit instance.
- Entity scale target: Up to 4 active party members.

## Error Handling and Recovery
- Limit Exceeded: If trait lists are at capacity, `TryAddTrait` returns `false` safely, ignoring the addition.
- Duplicate Traits: Uniqueness checks prevent adding a trait with an identical `traitId` to the active lists.

## Observability
- Metrics: None.
- Logs: None.
- Traces/profilers: None.

## Acceptance Tests
- Automated:
  - `TraitTests.cs`:
    - Tests verifying `PartyMemberInfo.TryAddTrait` checks uniqueness of IDs and prevents exceeding limits.
- Playtest:
  1. In the main menu skill selection or reward screens, try equipping traits to a marionette.
  2. Verify that duplicate traits are blocked and trait count is capped.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
