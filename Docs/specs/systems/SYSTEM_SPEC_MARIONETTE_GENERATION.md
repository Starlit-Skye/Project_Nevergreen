# Marionette Generation System

Owner: Gameplay Engineering Team
Status: active
Last verified: 2026-06-19
Verified commit: 4134da54597e5d2ec8192eeff46cf998a27bb03c
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Handle the complete, encapsulated generation of randomized Marionette units (`PartyMemberInfo`) at run-time, selecting a class template, skill loadout, and Perfection/Imperfection traits according to design rules and global configuration.

## Scope
- In scope: Randomized character class template selection from `MarionetteDatabase` (accessed via `GameDatabase.Instance`); skill loadout generation of up to 4 unique skills from class-specific total skill pool or available skills; allocation of 1 random Perfection trait and 1 random Imperfection trait from `TraitDatabase` (accessed via `GameDatabase.Instance`); validation of active databases.
- Out of scope: UI rendering of selection buttons/panels; addition of generated characters to the player party database/roster (handled by `MarionetteSelectionController`); combat initiation and skill execution mechanics.

## Source of Truth
- Code: `Assets/Scripts/Data/MarionetteGenerator.cs` (`Nevergreen.Data.MarionetteGenerator`), `Assets/Scripts/UI/MarionetteSelectionController.cs` (`Nevergreen.UI.MarionetteSelectionController`), `Assets/Scripts/Data/PartyMemberInfo.cs` (`Nevergreen.Data.PartyMemberInfo`), `Assets/Scripts/Data/GameDatabase.cs` (`Nevergreen.Data.GameDatabase`)
- Tests: `Assets/Editor/Tests/MarionetteGeneratorTests.cs` (`Nevergreen.Tests.MarionetteGeneratorTests`)
- Design: `Docs/specs/systems/SYSTEM_SPEC_MARIONETTE_TEMPLATE.md` (baseline template spec), `Docs/specs/systems/SYSTEM_SPEC_TRAIT_SYSTEM.md` (trait capacity rules)
- Data: `Assets/Scripts/Data/GameDatabase.cs` (`Nevergreen.Data.GameDatabase` centralized registry)
- Issue/ADR: Unknown

## Responsibilities
- Randomly choose a class template (`CharacterData`) from a pool of registered marionette classes inside the database.
- Extract the skill pool from either the template's `totalSkillPool` or fallback to its `availableSkills`.
- Pick exactly 4 unique random skills from this pool using a copy-and-remove random strategy.
- Fallback to equipping fewer than 4 skills if the character's skill pool size is less than 4.
- Randomly assign exactly 1 Perfection trait and 1 Imperfection trait from the provided `TraitDatabase` asset using a candidate pool retry strategy (selecting a new random trait if the chosen one is rejected due to opposite trait conflicts or duplicates).
- Construct a new `PartyMemberInfo` object representing the fully populated, ready-to-use marionette runtime data.

## Data Model
- Entity/component/object:
  - `MarionetteGenerator` (static class): Encapsulates generation logic via `GenerateRandomMarionette`.
  - `PartyMemberInfo` (runtime object): Holds reference to base template (`character`), `equippedSkills` list, `perfections` list, and `imperfections` list.
- Persistence keys: None (generator is a stateless utility; persistence is handled by the party system save/load pipelines).

## Event Contracts
- Event: None (the generator does not publish events, but operates as a synchronous procedural utility invoked by the UI and run controllers).

## Timing Model
- Update domain: Main thread synchronously during run-state transitions (e.g. loading a Marionette selection choice UI panel).
- Tick/update order: Runs on-demand prior to displaying choices or adding members to the team.
- Budget: Synchronous execution time must be under 0.05 ms per marionette generated.

## Determinism
- Required: Yes
- Strategy: Uses a static `System.Random` instance (`_rng`) initialized globally on the main thread.
- Known exceptions: Random selections depend on the state of the shared global `_rng` seed at runtime.

## Authority Model
- Single-player/offline: The local client application has full authority to generate and assign marionettes.
- Multiplayer: Unknown

## Performance Budget
- CPU: Under 0.05 ms per character generation.
- Memory: Low garbage collector allocation footprint (temporary copy list of skills is collected).
- Entity scale target: Typically invoked 3 times sequentially during a selection choice prompt to present 3 choices.

## Error Handling and Recovery
- Null GameDatabase.Instance: Returns `null` and logs an error block using `Debug.LogError`.
- Null/Empty MarionetteDatabase: Returns `null` and logs an error block using `Debug.LogError`.
- Null TraitDatabase: Safely logs a warning `Debug.LogWarning` and skips assigning traits to the generated marionette.
- Capacity limit / Duplicate Trait / Opposite Trait: `PartyMemberInfo.TryAddTrait` returns `false` and generator removes the rejected trait from the candidate pool and picks another random candidate.

## Observability
- Metrics: Choice generation count.
- Logs: Logs errors for missing `GameDatabase.Instance` or `MarionetteDatabase` templates and warnings for missing `TraitDatabase` configurations.
- Traces/profilers: None.

## Acceptance Tests
- Automated:
  - `Assets/Editor/Tests/MarionetteGeneratorTests.cs`
    - `GenerateRandomMarionette_ValidInput_Generates4UniqueSkillsAnd1Perf1Imp`: Validates 4 unique skills, 1 perfection, and 1 imperfection are assigned.
    - `GenerateRandomMarionette_LessThan4SkillsInPool_EquipsAllAvailable`: Validates safety fallback when skill pool is small.
    - `GenerateRandomMarionette_EmptyTraitDb_NoTraitsAssigned`: Validates behavior when trait lists are empty.
    - `GenerateRandomMarionette_NullDatabase_ReturnsNull`: Validates safety check for missing character databases.
- Playtest: Launch the Marionette Selection screen directly or during a run, and verify that the choice buttons display exactly 3 randomly selected classes, each with 4 distinct skills, 1 Perfection, and 1 Imperfection.

## Missing Evidence
- None

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
