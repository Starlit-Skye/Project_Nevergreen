# Status Effect: Stealth

Owner: Combat Engineering Team
Status: active
Last verified: 2026-06-30
Verified commit: HEAD
Target build: Unity 2022.3 + Windows

## Purpose
The Stealth status effect prevents enemies from targeting a character with skills, allowing vulnerable, high-priority, or squishy units to avoid direct enemy aggression temporarily.

## Scope
- In scope: Targeting restriction behavior, status application (via skill execution or on-spawn triggers), dynamic text visual tracking above the health bar, status duration/expiry rules, and explicit removal.
- Out of scope: Sprite transparency or shader-based visual alterations.

## Source of Truth
- Code: 
  - `Assets/Scripts/Combat/CombatCharacter.cs` (`IsStealthed`)
  - `Assets/Scripts/Combat/Effects/StealthStatusInstance.cs` (`StealthStatusInstance`, `StealthTextTracker`)
  - `Assets/Scripts/Combat/BattleSystem.cs` (`GetValidTargets` target filtration)
  - `Assets/Scripts/Combat/StatusEffectOnSpawn.cs` (`ApplyTo` factory logic)
  - `Assets/Scripts/Data/SkillData.cs` (`ignoresStealth` flag)
  - `Assets/Scripts/Combat/Effects/RemoveStealthEffect.cs`
- Tests: 
  - `Assets/Editor/Tests/StatusEffectOnSpawnTests.cs` (verifies spawn application of Stealth status)
- Design: Unknown
- Data: `Assets/Scripts/Data/SkillData.cs` (`StatusType.Stealth` enum value)

## Inputs
- Input action: Skill execution applying `StatusType.Stealth`, or combat initialization with a `StatusEffectOnSpawn` component configured on a prefab/character.
- Input conditions: Target must be a valid, active `CombatCharacter`.
- Input buffering: N/A (Turn-based action execution).

## State Model
States:
- `Normal`: Character does not have an active Stealth status effect (`IsStealthed` evaluates to `false`).
- `Stealthed`: Character has at least one active Stealth status effect (`IsStealthed` evaluates to `true`).

Transitions:
1. `Normal` -> `Stealthed` when a Stealth status effect is applied.
2. `Stealthed` -> `Normal` when the remaining duration of all active Stealth status effects reaches 0, or when they are explicitly removed via `RemoveStealthEffect`.

## Timing Model
- Update domain: Combat turns (for status expiry/decrement) and frame updates (for UI text tracking).
- Tick rate: Per-turn decrement at turn boundaries.
- Order dependencies:
  - Valid target checks occur inside `BattleSystem.GetValidTargets` prior to character action resolution.
  - `StealthTextTracker` runs on Unity frame updates (`Update`), checking for and parenting the tracking UI text onto the character's instantiated `HPBar` once available.

## Determinism
- Deterministic across clients: Yes (targeting eligibility and status expiration are fully bound to turn state).
- Sources of nondeterminism: None.
- Mitigation: N/A.

## Formulas
```csharp
// From BattleSystem.cs: GetValidTargets target filtering logic
if (skill.targetScope == TargetScope.Enemies && c.IsStealthed && !skill.ignoresStealth)
    return false; // Character cannot be targeted
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `Stealth Duration` | 1 | 1 | 99 | turns | Configured on SkillData/StatusEffectOnSpawn |

## Edge Cases
- **HPBar timing on spawn**: When a monster spawns with Stealth, its `HPBar` is not yet instantiated. The `StealthTextTracker` constantly polls in its `Update` function until it finds the matching `HPBar` (via the private `_target` field mapped with Reflection) and parents the UI text.
- **AOE targeting expansion**: While Stealth prevents a character from being selected as the primary target of an enemy skill, it does not protect them from being hit by multi-target/AOE skills if an adjacent ally is selected as the primary target (e.g. `BattleSystem.GetAOETargets` doesn't filter out stealthed characters).
- **Stealth Bypass**: Skills with the `ignoresStealth` flag set to `true` on their `SkillData` asset bypass targeting restrictions and can target stealthed characters directly.

## Failure Modes
- **Orphaned UI elements**: If a character or their health bar is destroyed without proper cleanup, UI elements could remain. The tracker handles this via safety checks in `Cleanup()` called from both `OnRemoved()` and Unity's `OnDestroy()`.

## Event Hooks
- Event: `CombatCharacter.OnStatusApplied`, Trigger: status added, Payload: `StatusType.Stealth`

## Acceptance Tests
- Automated: `StatusEffectOnSpawnTests` (verifies stealth status application on character setup).
- Playtest: Launch combat with a stealthed unit and verify that "In Stealth" text correctly mounts on the unit's health bar, remains positioned as they move, and vanishes immediately when the status expires or is removed.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
