# Status Effect: Bleed on Attack

Owner: Combat Engineering Team
Status: active
Last verified: 2026-06-30
Verified commit: 948d7b7790470ddb96f51de416094b0cdc21456c
Target build: Unity 6000.3.9f1 + Windows

## Purpose
The Bleed on Attack status effect is a buff that gives the host a chance to apply the Bleed status effect to targets on any successful damage-dealing hits (including Riposte counters).

## Scope
- In scope: Intercepting resolved combat actions, verifying host and hit criteria, checking target bleed resistance, applying Bleed status with configured duration and amplitude, and duration ticking.
- Out of scope: Visual rendering of status effects/animations.

## Source of Truth
- Code: `Assets/Scripts/Combat/Effects/BleedOnAttackStatusInstance.cs` (Status effect instance logic)
- Code: `Assets/Scripts/Combat/Effects/BleedOnAttackStatusEffect.cs` (Skill effect that applies this status)
- Code: `Assets/Scripts/Combat/CombatCalculator.cs` (`ResolveStatusApplication`)
- Tests: `Assets/Editor/Tests/BuffDebuffTests.cs` (`BleedOnAttack_AppliesBleed_OnAttackHit`, `BleedOnAttack_NoBleed_OnAttackMiss`, `BleedOnAttack_RiposteCounter_AppliesBleed`, `BleedOnAttack_RespectsResistance`)
- Data: `Assets/Scripts/Data/SkillData.cs` (`StatusType.BleedOnAttack` definition)

## Inputs
- Trigger condition: Successful damage-dealing action resolved where the actor matches the host of this status effect.
- Requirements: The skill must have a damage modifier (`skill.modifier.IsDamage == true`).

## State Model
States:
- `Inactive`: Character does not have the Bleed on Attack buff.
- `Active`: Character has the Bleed on Attack buff and listens to resolved combat actions to apply Bleed.

Transitions:
1. `Inactive` -> `Active` when a skill executes a `BleedOnAttackStatusEffect`.
2. `Active` -> `Inactive` when the status duration expires (ticked down via `StatusProcessor.TickDurations` on the character's turn start/end) or is cleansed.

## Timing Model
- Update domain: combat action execution.
- Activation phase: Hooks onto the `BattleSystem.OnActionResolved` event in `OnAdded` and unhooks in `OnRemoved`.
- Execution timing: Evaluated at action resolution (`OnActionResolved` event) immediately after the action completes and hit calculations are finalized.

## Determinism
- Deterministic across clients: Yes, provided the RNG seed for `ResolveStatusApplication` is synchronized.
- Sources of nondeterminism: RNG roll during application if the chance is < 100%.
- Mitigation: Synchronized random seed at the start of battle.

## Formulas
1. **Application Chance**:
   The chance to apply Bleed is resolved using `CombatCalculator.ResolveStatusApplication` with the target's Bleed resistance subtracted:
   ```csharp
   final_application_chance = bleed_chance - target_bleed_resistance
   applied = random(0, 100) < final_application_chance
   ```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `duration` | 3 | 1 | 99 | turns | `SkillStatusEntry` (Buff duration) |
| `bleedAmplitude` | Varies | 1 | 999 | value | `BleedOnAttackStatusEffect` (Bleed tick damage) |
| `bleedDuration` | Varies | 1 | 99 | turns | `BleedOnAttackStatusEffect` (Bleed duration) |
| `bleedChance` | 100 | 0 | 100 | % | `BleedOnAttackStatusEffect` (Bleed application chance) |

## Edge Cases
- **Riposte Counters**: Because counter-attacks trigger the `OnActionResolved` event and are categorized as damage skills (where `skill.modifier.IsDamage` is true and `ctx.didHit` is true), the Bleed on Attack status successfully procs on Riposte counters.
- **Missed Attacks**: If the attack misses (`ctx.didHit` is false), the handler returns early and no Bleed is applied.
- **Non-Damage Skills**: If the skill is not a damage-dealing skill (e.g., status effects, buffs, heals), Bleed on Attack will not trigger.
- **Bleed Resistance**: The applied Bleed effect respects the target's individual Bleed resistance. A target with 100%+ Bleed resistance is immune.

## Failure Modes
- **Null References**: If the combat context target is null or dead, the application is skipped.

## Event Hooks
- Event: `OnStatusApplied`, Trigger: After application roll, Payload: `target, type, succeeded`

## Acceptance Tests
- Automated:
  - `Assets/Editor/Tests/BuffDebuffTests.cs` -> `BleedOnAttack_AppliesBleed_OnAttackHit`: Verifies that Bleed is applied on a hit.
  - `Assets/Editor/Tests/BuffDebuffTests.cs` -> `BleedOnAttack_NoBleed_OnAttackMiss`: Verifies that Bleed is not applied on a miss.
  - `Assets/Editor/Tests/BuffDebuffTests.cs` -> `BleedOnAttack_RiposteCounter_AppliesBleed`: Verifies that counter-attacks correctly trigger Bleed application.
  - `Assets/Editor/Tests/BuffDebuffTests.cs` -> `BleedOnAttack_RespectsResistance`: Verifies that 100% Bleed resistance prevents application.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
