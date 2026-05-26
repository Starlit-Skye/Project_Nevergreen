# Skill Effect: Conditional Damage

Owner: Combat Engineering Team
Status: active
Last verified: 2026-05-26
Verified commit: 9b69747
Target build: Unity 2022.3 + Windows

## Purpose
The Conditional Damage skill effect is an execution strategy that deals damage to a target, scaling its damage multiplier upwards if the target currently possesses a specified status effect. This allows designers to create synergies, such as bonus damage against Stunned, Marked, or Bleeding targets.

## Scope
- In scope: Target status checking, dynamic modifications to `SkillContext.skillScaling`, standard damage roll & application flow, and scaling restoration.
- Out of scope: Visual rendering of damage boosts.

## Source of Truth
- Code: `Assets/Scripts/Combat/Effects/ConditionalDamageEffect.cs` (this strategy), `Assets/Scripts/Combat/CombatCalculator.cs` (`CalculateDamage`), `Assets/Scripts/Combat/SkillContext.cs` (`EnsureHitResolved`).
- Data: `Assets/Scripts/Data/SkillData.cs` (Status types and skill definition structures).

## Inputs
- Input action: Execute skill containing `ConditionalDamageEffect`.
- Configuration variables:
  - `requiredStatus`: The `StatusType` required to trigger the scaling bonus.
  - `bonusScaling`: The amount of bonus damage scaling to add to `SkillContext.skillScaling` (e.g. `0.5` represents `+50%` scaling boost).

## State Model
This effect runs transiently and synchronously.
1. Check target's active status effects for `requiredStatus`.
2. Save original `SkillContext.skillScaling`.
3. If target has status, add `bonusScaling` to `SkillContext.skillScaling`.
4. Perform hit resolution, damage calculation, and target health modification.
5. Restore original `SkillContext.skillScaling` in a `finally` block.

## Timing Model
- Update domain: Combat skill resolution.
- Ordering: Executes sequentially for each target in the target list of the skill, and for each hit index in multi-hit skills.

## Determinism
- Deterministic: Yes, utilizes standard battle system calculations and the shared deterministic RNG seed.

## Formulas
```txt
effective_scaling = base_scaling + (has_required_status ? bonus_scaling : 0)
base_damage_roll = round_to_int(base_attack * random_uniform(0.8, 1.2))
scaled_damage = round_to_int(base_damage_roll * effective_scaling)
final_damage = round_to_int(scaled_damage * damage_multiplier * (is_critical ? crit_multiplier : 1))
reduced_damage = round_to_int(final_damage * (1 - defense / 100))
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| requiredStatus | Mark | N/A | N/A | Enum | Designer inspector |
| bonusScaling | 0.0 | -10.0 | 10.0 | Multiplier | Designer inspector |

## Edge Cases
- **No status effect present**: The skill deals damage using its baseline `skillScaling` without modification.
- **Multiple stacks of status effect**: The presence check is binary. A target with 1 stack vs 3 stacks of the required status gets the same `bonusScaling` boost.
- **Accuracy Miss**: If hit check rolls a miss, damage is skipped completely.
- **Multi-target / AOE skills**:
  - Target A has the required status. Target B does not.
  - Target A takes damage with `skillScaling + bonusScaling`.
  - Target B takes damage with the original `skillScaling`.
  - The script must guarantee that modifying `skillScaling` for Target A does not leak to Target B.
- **Multi-hit skills**:
  - Checks status presence and resolves damage calculation for each individual hit.

## Acceptance Tests
- Automated unit tests in `Assets/Editor/Tests/HitCritTests.cs` (or a dedicated file):
  - `ConditionalDamage_ApplyBaseDamage_WhenNoStatus`: Verify baseline damage scaling is applied when target lacks the status.
  - `ConditionalDamage_ApplyBoostedDamage_WhenStatusExists`: Verify boosted scaling is applied when target has the status.
  - `ConditionalDamage_RestoresOriginalScaling_AfterExecution`: Verify context scaling is returned to its base value.
  - `ConditionalDamage_PerTargetResolution`: Verify multi-target executions calculate boosted scaling independently per target.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
