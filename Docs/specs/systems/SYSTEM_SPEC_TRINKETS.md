# SYSTEM_SPEC_TRINKETS

Owner: Gameplay Engineering
Status: active
Last verified: 2026-07-25
Verified commit: 6d3a08a7d925fa1492604370b270ca1fb51517bd
Target build: Unity 2022.3 (Windows)

## Purpose
Provides equippable items (Trinkets) that grant passive stat modifications and dynamic event-driven combat effect strategies to Marionette party members.

## Scope
- In scope: Trinket data definitions, capacity rules, equipment constraints (cursed trinkets, uniqueness cap), runtime effect lifecycle (`OnActivate`, `OnDeactivate`), stat modifier aggregation, combat event subscriptions (`OnBeforeDamageCalculation`, `OnBeforeDamageCalculationPerTarget`), and persistence serialization (`PartyMemberDTO`).
- Out of scope: UI rendering details for inventory management (handled by `PartyManagementPanelController`).

## Source of Truth
- Code: `Assets/Scripts/Data/PartyMemberInfo.cs` (Equipment constraints)
- Code: `Assets/Scripts/Combat/CombatCharacter.cs` (Lifecycle, Stat aggregation)
- Code: `Assets/Scripts/Data/Trinkets/TrinketData.cs` (`TrinketData` definition)
- Code: `Assets/Scripts/Data/Trinkets/TrinketEffectStrategy.cs` (Strategy base class)
- Code: `Assets/Scripts/Combat/TrinketInstance.cs` (Runtime instance wrapper)
- Code: `Assets/Scripts/Data/TrinketDatabase.cs` (Trinket database registry)
- Tests: `Assets/Editor/Tests/TrinketTests.cs` (Unit test suite)
- Data: `Assets/Data/Trinket/` (ScriptableObject assets)

## Responsibilities
- Enforce character equipment constraints (max 2 trinkets per character, no duplicate trinket IDs on the same character, non-removable cursed trinkets).
- Instantiate runtime `TrinketInstance` objects during character combat initialization.
- Hook into `BattleSystem` events (`OnBeforeDamageCalculation`, `OnBeforeDamageCalculationPerTarget`) to dynamically alter combat resolution via `SkillContext`.
- Aggregate flat and percentage-based stat modifiers into `CombatStats` during `CombatCharacter.GetEffectiveStats()`.

## Data Model
- `TrinketData`: ScriptableObject containing `trinketId` (string), `displayName` (string), `description` (string), `cannotBeRemoved` (bool), and `effectStrategies` (`List<TrinketEffectStrategy>`).
- `TrinketInstance`: Runtime wrapper containing `data` (`TrinketData`), `owner` (`CombatCharacter`), `battleSystem` (`BattleSystem`), and `extra` (`Dictionary<string, object>`).
- `PartyMemberInfo`: Holds `equippedTrinkets` (`List<TrinketData>`) representing character inventory state.
- `PartyMemberDTO`: Serializes `equippedTrinketIds` (`List<string>`) for save/load persistence.

## Event Contracts
- Subscribed Event: `BattleSystem.OnBeforeDamageCalculation`
  - Producer: `BattleSystem`
  - Consumers: `GuaranteedHitTrinketStrategy`, `HealOutputBonusTrinketStrategy`, `HealReceivedBonusTrinketStrategy`, `StatusApplicationBonusTrinketStrategy`, `CritDamageMultiplierBonusTrinketStrategy`, `DamageOutputBonusTrinketStrategy`, `StatusBurstTrinketStrategy`, `SingleTargetCritAoeHitTrinketStrategy`, `SelfDamageOnAttackTrinketStrategy`
  - Payload schema: `SkillContext`
- Subscribed Event: `BattleSystem.OnBeforeDamageCalculationPerTarget`
  - Producer: `BattleSystem` / `DamageEffect`
  - Consumers: `LowHpDamageBonusTrinketStrategy`, `DamageReceivedBonusTrinketStrategy`, `StatusUnresistableTrinketStrategy`, `SingleTargetCritAoeHitTrinketStrategy`
  - Payload schema: `SkillContext`, `CombatCharacter` (target)

## Timing Model
- Update domain: Event-driven during combat turn execution (`ExecuteSkill` in `BattleSystem`).
- Tick/update order: Trinkets hook into `OnBeforeDamageCalculation` (prior to hit/damage resolution per skill) and `OnBeforeDamageCalculationPerTarget` (prior to individual target math resolution). Stat modifications are computed lazily on call to `CombatCharacter.GetEffectiveStats()`.
- Budget: < 1 ms per frame (event-driven callbacks).

## Determinism
- Required: Yes
- Strategy: Synchronous sequential execution of event callbacks registered on `BattleSystem`. Deterministic mutation of `SkillContext`.
- Known exceptions: None

## Authority Model
- Single-player/offline: Local authority managed by `BattleSystem` and `PartyMemberInfo`.
- Multiplayer: Out of scope (Offline single-player architecture).

## Performance Budget
- CPU: < 0.1 ms per skill execution.
- Memory: < 50 KB total allocated memory across active trinket instances.
- Entity scale target: 4 player characters + 4 enemy characters max in combat.

## Error Handling and Recovery
- Null Trinket reference in equipment: `TryEquipTrinket` and `TryUnequipTrinket` guard against null input and return false.
- Unregistered Trinket ID during deserialization: `TrinketDatabase.GetTrinket` returns null if ID is not found, leaving slot empty.
- Missing BattleSystem reference: Strategies check `instance.battleSystem == null` before subscribing/unsubscribing to prevent NREs outside active combat.

## Observability
- Metrics: None
- Logs: Debug logs outputted during strategy execution (e.g. `SelfDamageOnAttackTrinketStrategy` logs self-damage taken).
- Traces/profilers: Standard Unity Profiler markers on `BattleSystem.ExecuteSkill`.

## Acceptance Tests
- Automated: `Assets/Editor/Tests/TrinketTests.cs` (19 passing EditMode unit tests verifying capacity caps, uniqueness, cursed locks, stat calculations, skill context mutations, status application, burst compression, resistance bypass, AOE targeting expansion, and edge cases).
- Playtest: Equip trinkets in Party Management UI, enter combat, and verify tooltips and damage/heal/status calculations reflect equipped trinkets.

## Missing Evidence
- None

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
