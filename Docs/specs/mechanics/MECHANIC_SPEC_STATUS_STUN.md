# Status Effect: Stun

Owner: Combat Engineering Team
Status: active
Last verified: 2026-10-24
Verified commit: HEAD
Target build: Unity 2022.3 + Windows

## Purpose
The Stun status effect forces an afflicted character to skip their action on their next turn(s).
To prevent "stun-locking", a character that recovers from Stun automatically receives a large,
short-lived Stun Resistance bonus.

## Scope
- In scope: Stun application (resistance roll), turn-skip behavior, duration ticking while stunned,
  post-stun recovery buff, stacking of multiple Stun instances, interactions with Guard and Riposte.
- Out of scope: Specific skill animations, UI icon art, per-skill stun chances and durations
  (defined in skill data), Buff/Debuff stat math (see Buff/Debuff spec).

## Source of Truth
- Code: `Assets/Scripts/Combat/Effects/StatusEffect.cs` (application roll),
  `Assets/Scripts/Combat/Effects/StatusInstanceFactory.cs` (instance creation),
  `Assets/Scripts/Combat/StatusEffectInstance.cs` (instance data),
  `Assets/Scripts/Combat/CombatCharacter.cs` (`AddStatus`, `RemoveStatus`, `isStunned`, `GetResistance`),
  `Assets/Scripts/Combat/StatusProcessor.cs` (`TickDurations`, recovery buff),
  `Assets/Scripts/Combat/BattleSystem.cs` (`ProcessTurn` stun check),
  `Assets/Scripts/Combat/CombatCalculator.cs` (`ResolveStatusApplication`)
- Tests: `Assets/Editor/Tests/StunTests.cs`
- Design: [Google Doc](https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?usp=sharing)
- Data: `Assets/Scripts/Data/SkillData.cs` (`StatusType.Stun`), `Assets/Scripts/Data/CombatConfig.cs`
  (`stunRecoveryResistBonus`)
- Related: `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md`,
  `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_BUFF_DEBUFF.md`,
  `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_GUARD.md`,
  `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_RIPOSTE.md`

## Inputs
- Input action: Skill execution containing a `StatusEffect` with `statusType = Stun`.
- Input conditions: The skill action must have hit (unless the effect sets `ignoreMiss = true`).
  Application then rolls against the target's effective Stun Resistance.
- Input buffering: N/A (Turn-based resolution).

## State Model
States:
- `NotStunned`: `isStunned == false`; no active (non-expired) Stun instance on the character.
- `Stunned`: `isStunned == true`; exactly one active (non-expired) Stun instance is present.
- `Recovering`: Character has just left `Stunned` and holds the 1-turn `Buff(StunResist)` recovery
  bonus (a sub-state of `NotStunned`, tracked by the Buff status, not a dedicated flag).

Transitions:
1. `NotStunned` -> `Stunned` when `CombatCharacter.AddStatus` receives a Stun instance.
2. `Stunned` -> `Stunned` when an additional Stun is attempted on an already stunned character:
   the new application is completely ignored (no new instance, no duration extension/refresh,
   no `OnStatusApplied` event fired).
3. `Stunned` -> `Recovering` when the active Stun instance expires in
   `StatusProcessor.TickDurations`.
4. `Recovering` -> `NotStunned` when the recovery buff (duration 1) expires on the next tick.

## Timing Model
- Update domain: combat tick (per turn/per action).
- Tick rate: Per-turn decrement, on the afflicted character's own turn.
- Order dependencies (in `BattleSystem.ProcessTurn`):
  1. Periodic effects (DoT/HoT) resolve first - stunned characters still take Bleed/Blight damage
     and receive Restore healing.
  2. Death-from-DoT check.
  3. Stun check: if `CurrentActor.isStunned`, the turn is skipped and `StatusProcessor.TickDurations`
     is called, then the turn index advances.
  4. If not stunned, durations tick normally before the character acts.
  Stun is checked **before** duration ticking, so a 1-turn Stun skips exactly one turn before
  expiring.
- Applying Stun to a character whose turn has not yet occurred this round causes the skip on that
  upcoming turn. Applying it after their turn has passed causes the skip next round.
- Defeated characters also run `TickDurations` (and may therefore generate the recovery buff);
  this has no gameplay effect while they remain defeated.

## Determinism
- Deterministic across clients: Yes, given the same RNG seed (`context.rng`).
- Sources of nondeterminism: Application roll in `CombatCalculator.ResolveStatusApplication`.
- Mitigation: Shared seeded `System.Random` via `SkillContext`. If target is already stunned,
  execution returns before invoking the RNG roll, preserving RNG stream determinism.

## Formulas
```csharp
// Active Stun check
public bool HasActiveStatus(StatusType type)
{
    return statusEffects.Any(s => s.type == type && !s.IsExpired);
}

// Application check in StatusEffect.Execute:
if (statusType == StatusType.Stun && target.HasActiveStatus(StatusType.Stun))
{
    return; // Completely ignored: no RNG roll consumed, no instance created, no event fired
}

// Application roll (StatusEffect.Execute)
finalChance = applicationChance + StatusChanceBonus_Stun (from traits/trinkets, via context.extra);
resistance  = target.GetResistance(StatusType.Stun);   // effective stunResist (>= 0)
// A StatusUnresistable_Stun_<id> flag in context.extra sets resistance = 0.

// CombatCalculator.ResolveStatusApplication
float finalChance = sourceChance - targetResistance;
if (finalChance <= 0f) return false;                   // fully resisted
bool applied = (rng.NextDouble() * 100f) < finalChance;

// CombatCharacter.AddStatus (central guard)
if (status.type == StatusType.Stun && HasActiveStatus(StatusType.Stun))
{
    return false; // Rejected
}

// Recovery (StatusProcessor.TickDurations), on Stun expiration
character.AddStatus(new StatusEffectInstance(StatusType.Buff,
    StatTarget.StunResist, stunRecoveryResistBonus, 1));

// Stun-state refresh
isStunned = HasActiveStatus(StatusType.Stun);
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `applicationChance` | 100 | 0 | 300 | percent | `StatusEffect` (per skill) |
| `duration` | 3 (field default) | 1 | Unknown | turns | `StatusEffect` (per skill) |
| `stunResist` | per character | 0 | Unknown | percent points | `StatBlockData` / Buff-Debuff |
| `stun_recovery_resist_bonus` | 300 | 300 | Unknown | flat resist points | `CombatConfig.stunRecoveryResistBonus` |
| `stun_recovery_duration` | 1 | 1 | 1 | turns | `StatusProcessor` (hardcoded) |

> Amplitude is not used by Stun. Stun instances created through `StatusInstanceFactory` use the
> default `StatusEffectInstance` and ignore `targetStat`.

## Edge Cases
- **Stunning an Already-Stunned Character**: When a character already has an active (non-expired)
  Stun status (`HasActiveStatus(StatusType.Stun)`), any subsequent Stun application is completely
  ignored:
  - In `StatusEffect.Execute` (and sibling effects `SelfStatusEffect`, `AdjacentAllyStatusEffect`,
    and `ApplyStatusToGuardianEffect`), the effect aborts before rolling against resistance,
    consuming no RNG values, creating no `StatusEffectInstance`, and firing no `OnStatusApplied` event.
  - In `CombatCharacter.AddStatus`, attempting to add a Stun instance when an active Stun is
    already present returns `false` without adding the instance, without modifying duration, and
    without triggering `OnStatsChanged`.
- **No Stacking or Duration Extension**: Characters never hold more than one active Stun instance.
  Incoming Stun effects cannot overwrite or extend the remaining duration of an existing Stun.
- **Single Recovery Buff**: Because duplicate Stun instances cannot exist, an afflicted character
  receives exactly one `Buff(StunResist, +300, 1)` upon Stun expiration.
- **Recovery Buff Bypass**: Skills flagged `StatusUnresistable_Stun_<id>` ignore Stun Resistance
  (including the recovery buff) for that target.
- **Resistance Floor**: Effective `stunResist` is clamped at 0 (see Buff/Debuff spec); negative
  resistance cannot boost application chance above `applicationChance`.
- **Full Resistance**: If `applicationChance + bonus - stunResist <= 0`, Stun cannot apply and no
  RNG roll is consumed.
- **DoT While Stunned**: Bleed/Blight/Restore still tick before the stun check.
- **Guard**: A successful Stun on a guardian immediately breaks all Guard effects they maintain
  (via `OnStatusApplied`). A stunned guardian cannot intercept attacks or have Guard applied.
- **Riposte**: A stunned character does not counter-attack. A Stun applied by the incoming
  attack takes effect before the Riposte check, so the counter is skipped.
- **Stun on Self-Turn**: A Stun applied to the acting character during their own turn (e.g., via
  a reactive effect) does not cancel the action in progress; the skip occurs on their next turn.
- **Duration 0**: A Stun created with duration 0 is immediately expired (`IsExpired`).
  `HasActiveStatus(StatusType.Stun)` evaluates to `false` for expired instances. Treat 0-duration
  Stun as unsupported content.

## Failure Modes
- **Stun-Lock**: Prevented by the post-stun Stun Resistance recovery buff (+300 for 1 turn).
- **Stale `isStunned` Flag**: `isStunned` is recomputed using `HasActiveStatus(StatusType.Stun)` on
  `RemoveStatus` and at the end of `TickDurations`.
- **Null Game Database**: `BattleSystem` reads `GameDatabase.Instance.CombatConfig`; a missing
  config would break recovery buff generation.

## Event Hooks
- Event: `CombatCharacter.OnStatusApplied`, Trigger: status application attempt
  (`TriggerStatusApplied`), Payload: character, type, success, targetStat. Note: completely ignored
  Stuns on already-stunned characters do not fire this event.
- Event: `CombatCharacter.OnStatsChanged`, Trigger: Stun added/removed, recovery buff added,
  duration tick, Payload: character
- UI: `StatusTooltipDisplay` shows "Skips the next turn" for Stun.

## Acceptance Tests
- Automated: `Assets/Editor/Tests/StunTests.cs`
  - `StunnedCharacter_IsMarkedAsStunned`: Verify `isStunned` after application.
  - `StunnedCharacter_RemainsStunned_UntilExpiry`: Verify multi-turn duration.
  - `StunTiming_1TurnStun_SkipsThenExpires` / `StunTiming_2TurnStun_SkipsTwoTurns`: Verify skip count.
  - `PostStunRecovery_Applies300PercentStunResistBuff`: Verify recovery buff values.
  - `PostStunRecovery_BuffExpiresAfterOneTick`: Verify buff duration.
  - `PostStunRecovery_IncreasesEffectiveStunResist`: Verify effective stat (10 + 300 = 310).
  - `StunResistance_ReducesApplicationChance` / `StunResistance_100Percent_BlocksStun`: Verify roll.
  - `StunnedCharacter_OtherStatusEffects_TickDownCorrectlyDuringSkip`: Verify concurrent ticking.
  - `Stun_AppliedToAlreadyStunned_DirectAddStatus_ReturnsFalseAndCreatesNoInstance`: Verify direct `AddStatus` rejection.
  - `Stun_AppliedToAlreadyStunned_ViaSkillEffect_IsCompletelyIgnored`: Verify skill effect aborts with no event and no RNG consumed.
  - `Stun_AppliedToAlreadyStunned_DoesNotExtendDuration`: Verify duration is not extended.
  - `Stun_AppliedToAlreadyStunned_YieldsSingleRecoveryBuffOnExpiry`: Verify exactly 1 recovery buff granted on expiry.
  - `Stun_AppliedAfterPreviousExpired_AppliesNormally`: Verify re-application after recovery works normally.
  - `NonStunStatus_LikeBleed_StillStacks`: Verify stacking for other status types is unaffected.
- Related automated: `GuardTests` (Stun breaks Guard), `RiposteTests` (stunned/stunned-by-attack).
- Playtest: Verify the stunned character's turn is visibly skipped and DoT still plays first.

## Missing Evidence
- **Multi-Hit Skills**: Behavior if a target is stunned midway through a multi-hit skill
  (also listed in the Combat Core spec).
- **Max Duration / Max Stun Resist**: No upper bounds are enforced in code.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
- [x] Links and paths resolve
