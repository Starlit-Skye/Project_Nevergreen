# Status Icon UI & Tooltip Aggregation System

Owner: UI Team
Status: active
Last verified: 2026-07-13
Verified commit: 3169c6a881d7c986bc690e3a4a1a85ec7fb9f92d
Target build: Unknown

## Purpose
To consolidate active status effects into visually distinct groups on a character's HP bar (Buffs, Debuffs, Bleed, etc.) and accurately aggregate their underlying values inside multi-line tooltips. Reduces UI clutter while preserving full statistical visibility.

## Scope
- In scope: Grouping HP Bar status icons by `StatusType`, retrieving generic icon mappings, aggregating identical status instances inside tooltips (summing amplitudes and displaying maximum durations).
- Out of scope: The underlying combat and simulation logic for executing status effects.

## Source of Truth
- Code: `Assets/Scripts/UI/StatusTooltipDisplay.cs` (StatusTooltipDisplay), `Assets/Scripts/Prototype/HPBar.cs` (HPBar)
- Tests: `Assets/Editor/Tests/StatusIconTests.cs` (StatusIconTests)
- Design: Unknown
- Data: `Assets/Scripts/Data/CombatConfig.cs` (GetStatusIcon)
- Issue/ADR: Unknown

## Responsibilities
- Group all active `StatusEffectInstance` objects solely by their `StatusType` when generating icons on the HP bar.
- Display exactly one generic mapped icon per active status type per character.
- Format detailed multi-line tooltips summarizing total amplitudes, custom amplitudes (for Skill Boosts), proc chances, and longest remaining durations.

## Data Model
- Entity/component/object: 
  - `StatusEffectInstance`: Core fields including `type`, `targetStat`, `amplitude`, `remainingDuration`, and `Host`.
  - `SkillBoostStatusInstance`: Inherits and adds `targetSkillId`, `targetSkillDisplayName`, and `customAmplitude`.
  - `BleedOnAttackStatusInstance`: Inherits and adds `BleedChance`.
- Persistence keys: None (In-memory battle state).

## Event Contracts
- Event: `PointerEnter` (UI Hover)
- Producer: Unity EventSystem
- Consumers: `StatusIconTooltipTrigger`, `StatusTooltipDisplay`
- Payload schema: Triggers tooltip generation using the `StatusEffectInstance` bound to the hovered icon.

## Timing Model
- Update domain: Asynchronous UI Event
- Tick/update order: Updates immediately upon Pointer Enter; refreshes display data on demand based on current `statusEffects` collection.
- Budget: Unknown

## Determinism
- Required: No
- Strategy: N/A (UI visualization logic reacting to underlying combat state)
- Known exceptions: None

## Authority Model
- Single-player/offline: Read-only client UI.
- Multiplayer: Read-only client UI reflecting replicated authoritative `StatusEffectInstance` data.

## Performance Budget
- CPU: Unknown
- Memory: Unknown
- Entity scale target: ~10 status icons per character max.

## Error Handling and Recovery
- Null Host: If a status effect lacks a bound `Host` (e.g. testing context or raw UI preview), tooltip formatting gracefully falls back to non-aggregated, single-effect formatting.
- Missing Icon: If `config.GetStatusIcon` returns null, the HP bar silently skips instantiating an icon for that group.

## Observability
- Metrics: None
- Logs: None
- Traces/profilers: None

## Acceptance Tests
- Automated: `Assets/Editor/Tests/StatusIconTests.cs`
  - Scenario `HPBar_Refresh_GroupsDifferentBuffStatsIntoSingleIcon`: Ensures different stats under the same status type render as 1 icon.
  - Scenario `StatusTooltipDisplay_FormatsGroupedTooltipText_Correctly`: Ensures strings aggregate and newline correctly.
  - Scenario `StatusTooltipDisplay_FormatTooltipText_AggregatesAmplitudesCorrectly`: Ensures flat and percentage amplitudes sum correctly.
- Playtest: Apply multiple buffs/debuffs of different target stats, and identical debuffs with different durations in combat. Hover over the single grouped icon and verify multiline aggregated stats accurately reflect active modifiers and max remaining rounds.

## Missing Evidence
- Unknown target build version.
- Unknown design document link.
- Unknown exact CPU/Memory performance budget.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
