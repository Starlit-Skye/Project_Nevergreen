# Skills Database System

Owner: Gameplay Engineering Team
Status: active
Last verified: 2026-06-19
Verified commit: 4134da54597e5d2ec8192eeff46cf998a27bb03c
Target build: Unity 6000.3.9f1 Standalone Windows

## Purpose
Define how skill definitions are authored and consumed through ScriptableObject-based data for combat, including targeting constraints, modifiers, and custom effects.

## Scope
- In scope: Skill configuration schema (`SkillData`), stat modifier scaling (`SkillModifier`), target constraints (`TargetScope`), rank boundaries, special combat flags (ignoring defense/dodge, bypassing guard/stealth), multi-hit counting, and battle execution limiters.
- Out of scope: Custom logic of individual skill effects (e.g. status effect application and combat calculation details).

## Source of Truth
- Code: `Assets/Scripts/Data/SkillData.cs` (`Nevergreen.Data.SkillData`), `Assets/Scripts/Data/SkillModifier.cs` (`Nevergreen.Data.SkillModifier`)
- Tests: `Assets/Editor/Tests/TooltipSystemTests.cs` (verifying correct description formatting for UI tooltips), `Assets/Editor/Tests/MainMenuSkillSelectionTests.cs` (verifying skill selection and character inventory management).
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0 (sections: Technical -> Skills Database, Skills Modifier, Skills)
- Data: `Assets/Data/Skills/` (ScriptableObject asset folder).
- Issue/ADR: None.

## Responsibilities
- Store each skill definition as a separate `SkillData` ScriptableObject asset in the project.
- Require each skill to declare a unique `skillId`, user-facing `displayName`, and `description`.
- Manage positioning rules using `useRanks` (valid execution ranks) and `targetRanks` (valid targetable ranks).
- Restrict targeting scopes via `TargetScope` (`Self`, `Allies`, `Enemies`) and `maxTargets` clamps (1-4).
- Bind execution modifiers via `SkillModifier`, representing damage multipliers, heal multipliers, accuracy boosts, and critical rating modifiers.
- Support special behaviors (e.g. `ignoresDefense`, `ignoresDodge`, `guaranteedHit`, `bypassGuard`, `ignoresStealth`).
- Allow limiting execution counts per battle via `maxUsesPerBattle` (-1 indicates infinite uses).

## Data Model
- Entity/component/object:
  - `SkillData` (ScriptableObject): Skill baseline definition.
    - `skillId` (`string`): Unique identifier.
    - `displayName` (`string`): UI name.
    - `description` (`string`): Explanatory text.
    - `modifier` (`SkillModifier`): Baseline stat modifiers.
    - `effects` (`List<ISkillEffect>`): Custom behaviors applied on execution.
    - `useRanks` (`List<int>`): Set of valid source ranks (1 = front, 4 = back).
    - `targetRanks` (`List<int>`): Set of valid target ranks.
    - `targetScope` (`TargetScope`): targeting restrictions (Self, Allies, Enemies).
    - `maxTargets` (`int`): Target count limit (clamped 1-4).
    - `ignoresDefense` (`bool`): Bypasses target defense.
    - `ignoresDodge` (`bool`): Bypasses target dodge stats.
    - `guaranteedHit` (`bool`): Bypasses accuracy resolution.
    - `bypassGuard` (`bool`): Bypasses target guard status.
    - `ignoresStealth` (`bool`): Bypasses target stealth status.
    - `hitCount` (`int`): Hit resolution count per skill activation.
    - `maxUsesPerBattle` (`int`): Maximum execution allowance per match.
    - `sfx` (`AudioClip`): Auditory feedback on activation.
  - `SkillModifier`:
    - `damagePercent` (`float`): Percentage of user attack for damage output.
    - `healPercent` (`float`): Percentage of user attack for healing output.
    - `accuracyMod` (`float`): Additive accuracy adjustment.
    - `criticalMod` (`float`): Additive critical rate adjustment.
- Persistence keys: Save/Load systems persist equipped skills by storing list configurations of skill ID strings.

## Event Contracts
- Event: Skill selection check
  - Producer: `BattleSystem` / Combat UI / AIBrain
  - Consumers: Targeting UI or skill execution engine.
  - Payload schema: Invokes check methods (`CanUseSkillFromRank`, `HasRemainingUses`).

## Timing Model
- Update domain: Main thread.
- Tick/update order: Read-only data fetched during skill slots validation and combat turn resolution.
- Budget: Under 0.05ms per query.

## Determinism
- Required: Yes.
- Strategy: Modifier scaling properties and target scope configurations are static and immutable at runtime.
- Known exceptions: None.

## Authority Model
- Single-player/offline: Local client tables.
- Multiplayer: Unknown.

## Performance Budget
- CPU: Under 0.1ms per query.
- Memory: Under 5KB per skill definition instance.
- Entity scale target: Up to 100 skill definition assets loaded.

## Error Handling and Recovery
- Mutual Exclusivity: GDD defines that `damagePercent` and `healPercent` are mutually exclusive. System relies on authoring checks to verify one is zero.
- Zero Modifiers: Modifier fields with value `0` represent "no modifier" and are ignored by calculation loops and hidden in UI descriptions.

## Observability
- Metrics: None.
- Logs: None.
- Traces/profilers: None.

## Acceptance Tests
- Automated:
  - `TooltipSystemTests.cs`:
    - Tests verifying skill description descriptions and formatted tooltip representations.
  - `MainMenuSkillSelectionTests.cs`:
    - Tests verifying correct skill loading, equipping, and limit checking.
- Playtest:
  1. Start a battle, select Cecilia, and check her skill list.
  2. Verify that hover descriptions show the correct details and that rank limitations prevent selection if moved out of range.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Performance budgets are stated with units
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
