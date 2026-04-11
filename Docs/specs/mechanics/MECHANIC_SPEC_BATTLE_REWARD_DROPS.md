# Battle Reward Drop Mechanic

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define how `Parts`, `Scraps`, and `Trinkets` are granted when a battle ends, including
randomization, elite reward differentiation, and guaranteed trinket drops from elite encounters.

## Scope
- In scope: battle-end currency grant, normal-vs-elite reward difference, guaranteed elite trinket
  drop, reward event emission
- Out of scope: shop spending, event-specific reward mechanics, level-up spending logic

## Source of Truth
- Code: `Unknown` (battle reward implementation not provided)
- Tests: `Unknown` (battle reward tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (section: Economy)
- Data: `Assets/docs/specs/systems/SYSTEM_SPEC_ECONOMY_RUNTIME.md` (`BattleRewardProfile`)
- Issue/ADR: Unknown

## Inputs
- Input action: battle ends with resolved encounter type (`normal` or `elite`)
- Input conditions: encounter must have valid battle type classification
- Input buffering: Unknown

## State Model
States:
- `AwaitBattleEnd`: no reward processed yet
- `ComputeReward`: determine `Parts` and `Scraps` from profile plus randomization
- `ApplyReward`: update run economy totals
- `RewardCommitted`: grant complete and event emitted

Transitions:
1. `AwaitBattleEnd` -> `ComputeReward` when battle outcome is finalized
2. `ComputeReward` -> `ApplyReward` when reward values are resolved
3. `ApplyReward` -> `RewardCommitted` when run economy state is updated

## Timing Model
- Update domain: event-driven on battle completion
- Tick rate: once per resolved battle
- Order dependencies: reward computation must occur after combat outcome finalization and before route
  transition UI consumes reward payload

## Determinism
- Deterministic across clients: partial
- Sources of nondeterminism: slight randomization in battle drop amounts
- Mitigation: centralized RNG source and recorded roll context (exact policy Unknown)

## Formulas
```txt
# abstract battle reward model
parts_granted   = Randomized(BaseParts[battle_type])
scraps_granted  = Randomized(BaseScraps[battle_type])
trinket_granted = 1   if battle_type == elite
                  0   otherwise

# constraint
BaseParts[elite]  > BaseParts[normal]
BaseScraps[elite] > BaseScraps[normal]
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `battle_type` | normal | normal | elite | enum | GDD Economy |
| `parts_drop_randomization` | slight | Unknown | Unknown | qualitative | GDD Economy |
| `scraps_drop_randomization` | slight | Unknown | Unknown | qualitative | GDD Economy |
| `elite_reward_multiplier_or_delta` | Unknown | Unknown | Unknown | multiplier/delta | GDD Economy |

## Edge Cases
- Reward should not be applied more than once for the same battle id.
- If battle type is missing/invalid, reward behavior is Unknown.
- Elite reward outputs must remain higher than normal reward outputs.
- Elite battles always grant exactly 1 trinket. Normal battles never grant trinkets.
- If the trinket pool is empty or exhausted, behavior is Unknown.

## Failure Modes
- Duplicate reward application for same battle
- Invalid battle type classification
- Negative or zero currency grant from invalid profile data

## Event Hooks
- Event: `battle_rewards_granted`, Trigger: reward commit, Payload: battle id, battle type, parts
  granted, scraps granted, trinket granted (id or null), random roll context

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify end-of-battle reward grant for normal and elite battles, verify elite > normal,
  verify slight variance between runs with same battle type, and verify elite battles always grant
  exactly 1 trinket while normal battles grant 0

## Missing Evidence
- Numeric reward ranges for normal and elite encounters
- Randomization distribution definitions
- Duplicate-grant prevention implementation details
- Concrete battle-type classification source path

## Validation
- [ ] Facts match current code/content
- [ ] Timing and determinism assumptions are explicit
- [ ] Tuning variables map to actual data/config
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined

