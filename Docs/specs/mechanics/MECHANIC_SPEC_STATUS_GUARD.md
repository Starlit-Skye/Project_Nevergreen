# Status Effect: Guard

Owner: Combat Engineering Team
Status: active
Last verified: 2026-04-28
Verified commit: HEAD
Target build: Unity 2022.3 + Windows

## Purpose
The Guard status effect allows a "Guardian" character to protect an ally by intercepting and
receiving attacks directed at that ally. This enables tank-oriented strategies and protection of
high-value, low-health units like Ceci.

## Scope
- In scope: Damage redirection logic, break conditions (Stun, re-guard, expiry), bypass rules
  (AOE, bypass flags).
- Out of scope: Specific skill animations, visual UI indicators (icons/lines).

## Source of Truth
- Code: `Assets/Scripts/Combat/SkillContext.cs` (`bypassGuard`), `Assets/Scripts/Data/SkillData.cs` (`StatusType.Guard`), `Assets/Scripts/Combat/Effects/GuardStatusInstance.cs`, `Assets/Scripts/Combat/CombatCalculator.cs` (`GetEffectiveTarget`)
- Design: [Google Doc](https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?usp=sharing)
- Data: `Assets/Scripts/Data/SkillData.cs` (`StatusType` enum)

## Inputs
- Input action: Skill execution that applies `StatusType.Guard`.
- Input conditions: Target must be an ally.
- Input buffering: N/A (Turn-based resolution).

## State Model
States:
- `Inactive`: Character is not guarding and not being guarded.
- `Guarding`: Character is currently intercepting attacks for a specific ally.
- `Protected`: Character is currently being shielded by a guardian.

Transitions:
1. `Inactive` -> `Guarding` when a skill applies Guard status to an ally.
2. `Guarding` -> `Inactive` when the guardian is stunned, re-guards, or duration expires.
3. `Protected` -> `Inactive` when the guardian's Guard status breaks or the protected character
   is guarded by a different source (newest Guard takes priority).

## Timing Model
- Update domain: combat tick (per turn/per action).
- Tick rate: Per-turn decrement.
- Order dependencies:
  - Guard redirection occurs **before** Hit/Crit resolution. In `BattleSystem.ExecuteSkill`, `CombatCalculator.GetEffectiveTarget` is called to resolve the final target for effect execution.
  - Guard break checks occur **immediately** via event subscriptions. `GuardStatusInstance` subscribes to the guardian's `OnStatusApplied` (for Stun check) and `OnDefeated` events during its `OnAdded` hook.

## Determinism
- Deterministic across clients: Yes (redirection is based on discrete status state).
- Sources of nondeterminism: None.
- Mitigation: N/A.

## Formulas
```csharp
// CombatCalculator.GetEffectiveTarget implementation
public static CombatCharacter GetEffectiveTarget(CombatCharacter target, SkillContext context)
{
    if (context.bypassGuard) return target;

    // Only redirect hostile actions (targeting Enemies). 
    // Buffs, heals, and other ally-targeted skills bypass guard.
    if (context.skill != null && context.skill.targetScope != TargetScope.Enemies)
        return target;

    // Find the active guard status on the intended target
    var guard = target.statusEffects.OfType<GuardStatusInstance>()
        .FirstOrDefault(s => !s.IsExpired);

    if (guard == null || guard.Source == null || !guard.Source.IsAlive || guard.Source.isStunned) return target;

    // AOE Bypass Check: No redirection if both target and guardian are targeted.
    if (context.targets != null && context.targets.Contains(target) && context.targets.Contains(guard.Source))
        return target;

    return guard.Source;
}
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `guard_duration` | 2 | 1 | 5 | turns | SkillData (Amplitude: 0) |

## Edge Cases
- **Guardian Stunned**: If the guardian receives a Stun status, all active Guard effects they are
  maintaining break immediately.
- **Nested Guarding**: A character cannot be both a guardian and a protected target. If a
  character who is currently guarding an ally receives a Guard status themselves (becoming
  protected), they immediately break their active guard on their ally.
- **Multi-Guard Conflict**: Only one guardian can protect a target at a time. If a second guardian
  attempts to guard an already protected target, the oldest Guard status is removed and the
  newest one is applied ("Last-In-Wins").
- **AOE Bypass**: If an AOE skill targets both the protected character and their guardian, both
  take damage independently; the guard does not redirect the protected character's hit to the
  guardian.
- **Bypass Flag**: Skills with `bypassGuard = true` in `SkillData` ignore the redirection entirely.

## Failure Modes
- **Circular Guarding**: If A guards B and B guards A, the system must resolve to the most recent
  application or break the previous loop to prevent infinite redirection.

## Event Hooks
- Event: `CombatCharacter.OnStatusApplied`, Trigger: Stun application, Payload: type, success
- Event: `CombatCharacter.OnDefeated`, Trigger: HP reaching 0, Payload: character

## Acceptance Tests
- Automated: `Nevergreen.Tests.GuardTests` (Verify redirection, AOE bypass, and Stun break).
- Playtest: Verify visual lines/icons between characters correctly reflect the target swap during
  damage application.

## Missing Evidence
- **Animation Sync**: Behavior for multi-hit skills where the first hit breaks guard (assumption:
  remaining hits still target the guardian for that context execution).

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
- [x] Links and paths resolve
