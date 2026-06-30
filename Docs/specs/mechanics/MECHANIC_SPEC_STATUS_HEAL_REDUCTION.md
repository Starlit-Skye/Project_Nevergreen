# Status Effect: Heal Received Reduction

Owner: Combat Engineering Team
Status: active
Last verified: 2026-06-30
Verified commit: 948d7b7790470ddb96f51de416094b0cdc21456c
Target build: Unity 6000.3.9f1 + Windows

## Purpose
The Heal Received Reduction status effect is a debuff that reduces the incoming healing that a character receives from any healing skills.

## Scope
- In scope: Intercepting incoming healing calculations, applying healing reduction scaling based on status amplitude, debuff resistance checks, and duration ticking.
- Out of scope: Visual indicator rendering, visual effects/animations, and periodic healing effects (e.g., Restore).

## Source of Truth
- Code: `Assets/Scripts/Combat/Effects/HealReceivedDebuffStatusInstance.cs` (Status effect instance logic)
- Code: `Assets/Scripts/Combat/Effects/HealEffect.cs` (Healing calculation and application)
- Code: `Assets/Scripts/Combat/CombatCharacter.cs` (Stat and status container, maps status types to resistance)
- Tests: `Assets/Editor/Tests/BuffDebuffTests.cs` (`HealReceivedReduction_Debuff_ReducesHealAmount`, `HealReceivedReduction_Debuff_ChecksDebuffResistance`)
- Data: `Assets/Scripts/Data/SkillData.cs` (`StatusType.HealReceivedReduction` definition)

## Inputs
- Trigger condition: Execution of a skill effect that applies `StatusType.HealReceivedReduction` to a target.
- Requirements: Character application roll must succeed against the target's debuff resistance.

## State Model
States:
- `Inactive`: Character does not have the `HealReceivedReduction` status effect.
- `Active`: Character has the `HealReceivedReduction` status effect, applying a negative modifier to incoming heals.

Transitions:
1. `Inactive` -> `Active` when a skill executes an effect applying `StatusType.HealReceivedReduction`.
2. `Active` -> `Inactive` when the status duration expires (ticked down via `StatusProcessor.TickDurations` on the character's turn start/end) or is cleansed.

## Timing Model
- Update domain: combat action execution.
- Activation phase: Hooked onto the `BattleSystem.OnBeforeDamageCalculation` event in `OnAdded` and unhooked in `OnRemoved`.
- Tick phase: Ticked down in turn loop phase `TickStatusDurations` on the host's turn.

## Determinism
- Deterministic across clients: Yes, because application chance resolution uses a synchronized pseudo-random number generator (RNG) seed.
- Sources of nondeterminism: None.

## Formulas
1. **Modifier Application**:
   When `BattleSystem.OnBeforeDamageCalculation` fires for a healing skill, the debuff subtracts its amplitude percentage from the healing received key:
   ```csharp
   string key = $"HealReceived_{Host.GetInstanceID()}";
   float current = ctx.extra.ContainsKey(key) ? (float)ctx.extra[key] : 0f;
   ctx.extra[key] = current - (amplitude / 100f);
   ```
2. **Heal Calculation**:
   The final heal amount is computed in `HealEffect.Execute`:
   ```csharp
   int healAmount = CombatCalculator.CalculateHeal(context, config);
   string key = $"HealReceived_{target.GetInstanceID()}";
   if (context.extra.TryGetValue(key, out object bonusObj) && bonusObj is float bonusPercent)
   {
       float multiplier = Mathf.Max(0f, 1f + bonusPercent);
       healAmount = Mathf.RoundToInt(healAmount * multiplier);
   }
   ```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `amplitude` | Varies | 0 | 100 | % | `SkillStatusEntry` |
| `duration` | 3 | 1 | 99 | turns | `SkillStatusEntry` |

## Edge Cases
- **Debuff Resistance**: Since it is a debuff, the application chance is reduced by the target's `debuffResist` stat:
  ```csharp
  int resist = character.GetResistance(StatusType.HealReceivedReduction); // Maps to debuffResist
  ```
- **Cumulative Stacking**: Multiple instances of `HealReceivedReduction` subtract additively from the same key `HealReceived_{Host.GetInstanceID()}`. For example, two 30% reduction debuffs combine to form a 60% reduction.
- **Heal Reduction Cap**: Heal received reduction is capped at 100% (multiplier of 0). If the total heal reduction exceeds 100% (e.g., 120%), the multiplier `1f + bonusPercent` is clamped to `0f`. This ensures healing is reduced to 0 but never becomes negative, avoiding dealing damage to the target.

## Failure Modes
- **Skill Missing Modifier**: If `ctx.skill.modifier` is null during calculation, the handler returns early without modifying the multiplier.

## Event Hooks
- Event: `OnStatusApplied`, Trigger: After application roll, Payload: `target, type, succeeded`

## Acceptance Tests
- Automated:
  - `Assets/Editor/Tests/BuffDebuffTests.cs` -> `HealReceivedReduction_Debuff_ReducesHealAmount`: Verifies that applying a 30% reduction debuff successfully reduces a base 53 heal to 37.
  - `Assets/Editor/Tests/BuffDebuffTests.cs` -> `HealReceivedReduction_Debuff_ChecksDebuffResistance`: Verifies that `HealReceivedReduction` resistance correctly maps to `debuffResist`.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
