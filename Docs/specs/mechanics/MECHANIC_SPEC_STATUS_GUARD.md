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
- Code: `Assets/Scripts/Combat/GuardResolver.cs`, `Assets/Scripts/Combat/Effects/GuardStatusInstance.cs`, `Assets/Scripts/Combat/Effects/StatusInstanceFactory.cs`, `Assets/Scripts/Combat/CombatCalculator.cs` (`GetEffectiveTarget`), `Assets/Scripts/Combat/SkillContext.cs` (`bypassGuard`), `Assets/Scripts/Combat/Effects/StatusEffect.cs`, `Assets/Scripts/Combat/Effects/TargetGuardsUserEffect.cs`, `Assets/Scripts/Combat/Effects/AdjacentAllyStatusEffect.cs`, `Assets/Scripts/Combat/Effects/SelfStatusEffect.cs`, `Assets/Scripts/Combat/Effects/HealGuardianEffect.cs`, `Assets/Scripts/Combat/Effects/ApplyStatusToGuardianEffect.cs`, `Assets/Scripts/UI/StatusTooltipDisplay.cs`, `Assets/Scripts/Data/SkillData.cs` (`StatusType.Guard`)
- Design: [Google Doc](https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?usp=sharing)
- Data: `Assets/Scripts/Data/SkillData.cs` (`StatusType` enum)

## Inputs
- Input action: Skill execution that applies `StatusType.Guard` (via `StatusEffect`, `TargetGuardsUserEffect`, or `AdjacentAllyStatusEffect`).
- Input conditions: Target must be an alive, distinct ally.
- Input buffering: N/A (Turn-based resolution).

## State Model
Ownership model:
- The `GuardStatusInstance` lives on the **Guardian** (`Host == Source == guardian`) and tracks the protected ally via `GuardedTarget`.
- No Guard status or guard-specific reference field is stored on the protected character; the status on the guardian is the single source of truth.
- Lookups ("who guards X?") are resolved on demand via stateless `GuardResolver.GetActiveGuardian` by scanning the team for a non-expired `GuardStatusInstance` with `GuardedTarget == X`.

States:
- `Inactive`: Character is not guarding and not being guarded.
- `Guarding`: Character holds a `GuardStatusInstance` (with `GuardedTarget`) and is currently intercepting attacks for that specific ally.
- `Protected`: Character is referenced as `GuardedTarget` by an active guardian's `GuardStatusInstance`.

Transitions:
1. `Inactive` -> `Guarding` when a skill places `GuardStatusInstance` on the guardian pointing to an ally (existing SkillData assets require no modification; `StatusEffect` and `AdjacentAllyStatusEffect` place Guard on `context.user`, while `TargetGuardsUserEffect` places Guard on `target`).
2. `Guarding` -> `Inactive` when the guardian is stunned, defeated, re-guards another ally, becomes protected by another guardian, the protected ally is defeated, another guardian takes over protecting the same ally, or duration expires.
3. `Protected` -> `Inactive` when the guardian's `GuardStatusInstance` is removed or expires, or when the protected character itself becomes a guardian.

## Timing Model
- Update domain: combat tick (per turn/per action).
- Tick rate: Per-turn decrement on the **Guardian's** turn (`StatusProcessor.TickDurations` runs on the status host).
- Order dependencies:
  - Guard redirection occurs **before** Hit/Crit resolution. In `SkillExecutor.ExecuteSkill`, `CombatCalculator.GetEffectiveTarget` calls `GuardResolver.GetActiveGuardian(target, context)` to resolve the final target for effect execution.
  - Guard break checks occur **immediately** via event subscriptions. `GuardStatusInstance` subscribes to the guardian's (`Host`) `OnStatusApplied` (for Stun check) and `OnDefeated`, as well as `GuardedTarget.OnDefeated`, during its `OnAdded` hook.

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

    // Find the active guardian protecting the intended target
    var guardian = GuardResolver.GetActiveGuardian(target, context);

    if (guardian == null) return target;

    // AOE Bypass Check: No redirection if both target and guardian are targeted.
    if (context.targets != null && context.targets.Contains(target) && context.targets.Contains(guardian))
        return target;

    return guardian;
}
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `guard_duration` | 2 | 1 | 5 | turns | SkillData (Amplitude: 0) |

## Edge Cases
- **Guardian Stunned**: If the guardian receives a Stun status, their active Guard status breaks immediately. A stunned character cannot become a guardian.
- **One Guard Per Guardian (Re-guard)**: A guardian can only protect one ally at a time. If a guardian guards a new target, their previous `GuardStatusInstance` is removed immediately.
- **Mutual Exclusivity (Nested Guarding — Both Directions)**: A character cannot simultaneously be a guardian and a protected target:
  - If a character who is currently guarding an ally becomes protected by another guardian, their maintained guard breaks immediately.
  - If a character who is currently protected becomes a guardian for an ally, the guard protecting them breaks immediately.
- **Multi-Guard Conflict (Last-In-Wins)**: Only one guardian can protect a target at a time. If a second guardian protects an already protected target, the oldest guardian's `GuardStatusInstance` is removed and the newest one remains active.
- **Self-Guard & Adjacent-Ally Guard**: A character cannot guard themselves (`GuardStatusInstance.OnAdded` rejects `host == GuardedTarget`; `SelfStatusEffect` with `StatusType.Guard` logs a warning and skips). `AdjacentAllyStatusEffect` with `StatusType.Guard` causes the user to guard the adjacent ally (placing the status on the user).
- **AOE Bypass**: If an AOE skill targets both the protected character and their guardian, both take damage independently; the guard does not redirect the protected character's hit to the guardian.
- **Bypass Flag**: Skills with `bypassGuard = true` in `SkillData` ignore the redirection entirely.

## Failure Modes
- **Circular Guarding**: If A guards B and B then guards A, mutual exclusivity breaks A's guard on B immediately so only the newest guard (B guarding A) remains active, preventing infinite redirection.

## Event Hooks
- Event: `CombatCharacter.OnStatusApplied` (on Guardian/Host), Trigger: Stun application (`success == true`), Payload: character, type, success, targetStat
- Event: `CombatCharacter.OnDefeated` (on Guardian/Host and `GuardedTarget`), Trigger: HP reaching 0, Payload: character, wasCritical

## Acceptance Tests
- Automated: `Nevergreen.Tests.GuardTests` (redirection, AOE bypass, bypass flag, ally-scope bypass, Stun break, guardian/target death break, re-guard replacement, Last-In-Wins, both directions of mutual exclusivity, circular guard, expiry ticking on guardian's turn, `StatusEffect`/`TargetGuardsUserEffect`/`AdjacentAllyStatusEffect`/`SelfStatusEffect`, `HealGuardianEffect`/`ApplyStatusToGuardianEffect`, `GuardResolver` with/without `BattleSystem`), plus `StatusInstanceFactoryTests`, `StatusIconTests` (tooltip `"Guarding {name} for N turns"`), `RiposteTests`, `StealthTests`, `ExhaustionTests`.
- Playtest: Verify the Guard icon and `"Guarding <Target>"` tooltip appear on the guardian, attacks against the protected ally redirect to the guardian, and stunning the guardian immediately removes the icon.

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
