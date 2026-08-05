# Trait System

Owner: Combat Engineering Team
Status: active
Last verified: 2026-06-19
Verified commit: 4134da54597e5d2ec8192eeff46cf998a27bb03c
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define the static definition and runtime lifecycle of the Trait System (Perfections and Imperfections), which provides passive stat modifiers and event hook capabilities for combat characters.

## Scope
- In scope: Trait definitions via ScriptableObjects, inline serialization of effect strategies, runtime activation/deactivation during combat initialization, and dynamic stat modification calculation.
- Out of scope: Specific UI widgets/visual implementations, individual artwork assets for traits, and full list of authored trait content.

## Source of Truth
- Code: `Assets/Scripts/Data/Traits/TraitData.cs` (`Nevergreen.Data.TraitData`), `Assets/Scripts/Data/Traits/TraitEffectStrategy.cs` (`Nevergreen.Data.TraitEffectStrategy`), `Assets/Scripts/Combat/TraitInstance.cs` (`Nevergreen.Combat.TraitInstance`), `Assets/Scripts/Combat/TraitStatModifier.cs` (`Nevergreen.Combat.TraitStatModifier`), `Assets/Scripts/Combat/CombatCharacter.cs` (`Nevergreen.Combat.CombatCharacter`), `Assets/Scripts/Data/PartyMemberInfo.cs` (`Nevergreen.Data.PartyMemberInfo`)
- Tests: `Assets/Editor/Tests/TraitTests.cs` (Trait capacity checks, uniqueness validation, stats modification calculation, and custom event handlers)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0 (sections: Technical -> Combat Character, Stats, Trait Architecture)
- Data: `Assets/Data/Traits/Perfections/TD_P_1.asset` (Sample ScriptableObject configuration)
- Issue/ADR: Unknown

## Responsibilities
- Classify traits as either Perfections (positive passives) or Imperfections (negative passives).
- Store trait metadata (identity, display details) and inline modular effect strategies.
- Enforce slot capacity limits (configured dynamically per type), uniqueness by ID per party member info, and opposite trait co-existence exclusion.
- Manage runtime instantiation and clean teardown of event-driven trait strategies during combat setup and termination.
- Apply flat additions and percentage scales to character stats dynamically during combat resolution.

## Data Model
- Entity/component/object:
  - `TraitData` (ScriptableObject): Contains identity data (`traitId`, `displayName`, `description`), classification (`traitType`), opposite trait reference (`oppositeTrait`), and inline strategies (`effectStrategies` via `[SerializeReference]`).
  - `TraitInstance` (Runtime Wrapper): Holds references to `TraitData`, `owner` (`CombatCharacter`), `battleSystem`, and a generic `extra` dictionary for strategy event closures.
  - `TraitStatModifier` (Accumulator): Aggregates passive modifiers via `flatBonuses` (`Dictionary<StatTarget, int>`) and `percentBonuses` (`Dictionary<StatTarget, float>`).
- Persistence keys: `perfections` and `imperfections` lists inside `PartyMemberInfo`, serialized as references to `TraitData` assets.

## Event Contracts
- Event: `OnBeforeDamageCalculation`
  - Producer: `BattleSystem`
  - Consumers: `RankDamageBonusTraitStrategy` (dynamic damage modifications)
  - Payload schema: `SkillContext` reference
- Event: `OnStatsChanged`
  - Producer: `CombatCharacter`
  - Consumers: Combat UI
  - Payload schema: `CombatCharacter` reference

## Timing Model
- Update domain: Turn-based combat setup/resolution, and run-level party state modification.
- Tick/update order:
  - Wrap and associate traits in `TraitInstance` lists during `CombatCharacter.InitializeForCombat`.
  - Invoke `Activate` on traits when the `BattleSystem` reference is ready, allowing strategies to subscribe to battle events.
  - Call `ModifyStats` on active traits inside `CombatCharacter.GetEffectiveStats` during any stat check request.
  - Call `DeactivateAllTraits` to clean up event subscriptions when a character is destroyed or combat ends.
- Budget: Recalculating stats for all characters (max 4 players + 4 enemies) with traits must execute in under 0.1 ms.

## Determinism
- Required: Yes
- Strategy: Statically defined configurations and deterministic execution of active strategies. Floating point percentage values are rounded via `Mathf.RoundToInt` during character stat calculation.
- Known exceptions: None

## Authority Model
- Single-player/offline: The local client application has full authority to mutate, evaluate, and resolve trait states.
- Multiplayer: Unknown

## Performance Budget
- CPU: Under 0.1 ms per character stat calculation tick.
- Memory: Zero dynamic allocations (GC free) during stat modification loops.
- Entity scale target: Up to 3 active Perfections and 3 active Imperfections per Marionette unit, with up to 4 active player units.

## Error Handling and Recovery
- Duplicate Trait ID: `PartyMemberInfo.TryAddTrait` returns `false` and rejects the addition.
- Opposite Trait Conflict: `PartyMemberInfo.TryAddTrait` returns `false` if `oppositeTrait` relationships conflict with equipped traits.
- Capacity Exceeded: `PartyMemberInfo.TryAddTrait` returns `false` and rejects the addition.
- Null Strategy Reference: Checked and skipped safely during activation, deactivation, and modification loops.
- Recovery strategy: Log warning/error and fallback to baseline character stats on script errors.

## Observability
- Metrics: Count of active perfections and imperfections per unit.
- Logs: Warning logged if trait templates fail validation.
- Traces/profilers: None

## Acceptance Tests
- Automated:
  - `Assets/Editor/Tests/TraitTests.cs`
    - `TryAddTrait_Perfection_Success`: Verifies adding a perfection.
    - `TryAddTrait_Imperfection_Success`: Verifies adding an imperfection.
    - `TryAddTrait_DuplicateId_Ignored`: Verifies uniqueness constraints.
    - `TryAddTrait_CapacityLimit_Enforced`: Verifies slot constraints.
    - `RemoveTrait_Perfection_Success`: Verifies trait cleanup.
    - `ModifyStats_StatModifierStrategy_Applied`: Verifies flat and percentage modifier integration.
    - `TestPerfection_AppliesFlatSpeedPlusTwo`: Verifies custom speed strategies.
- Playtest: Verify spawned Marionettes initialize with exactly one random Perfection and one random Imperfection. Verify that UI stat displays match calculated values after applying trait modifiers.

## Missing Evidence
- Multiplayer authority constraints and replication schema.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Unknowns are explicitly labeled
- [x] Links and paths resolve
- [x] Acceptance tests are defined
