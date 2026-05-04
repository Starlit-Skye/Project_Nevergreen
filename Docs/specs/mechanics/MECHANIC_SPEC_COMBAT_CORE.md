# Combat Core Mechanic

Owner: Combat Engineering Team
Status: active
Last verified: 2026-04-28
Verified commit: HEAD
Target build: Unity 2022.3 + Windows

## Purpose
Define the baseline turn-based combat mechanic used for player team versus enemy team battles.

## Scope
- In scope: turn order, rank-based positioning, actions, hit/crit flow, core status interactions,
  round progression
- Out of scope: skill catalog details, class-specific skill formulas, UI art/layout implementation

## Source of Truth
- Code: `Assets/Scripts/Combat/BattleSystem.cs`, `Assets/Scripts/Combat/CombatCharacter.cs`, `Assets/Scripts/Combat/CombatCalculator.cs`, `Assets/Scripts/Combat/StatusProcessor.cs`
- Tests: `Assets/Editor/Tests/GuardTests.cs`, `Assets/Editor/Tests/StunTests.cs`, `Assets/Editor/Tests/BuffDebuffTests.cs`, `Assets/Editor/Tests/HitCritTests.cs`
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
- Data: `Assets/Scripts/Data/CombatConfig.cs`, `Assets/Scripts/Data/SkillData.cs`, `Assets/Scripts/Data/CharacterData.cs`
- Guard: `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_GUARD.md`

## Inputs
- Input action: choose one equipped skill, use move action, or use pass action
- Input conditions: active only on player-controlled character turn; rank constraints apply to both
  skill use and target eligibility; pass action ends turn immediately without other effect
- Input buffering: Unknown

## State Model
States:
- `RoundStart`: initialize round and determine turn order using Speed
- `CharacterTurn`: active character selects/executes action
- `RoundEnd`: all turns consumed, round closes, next round begins
- `BattleEnd`: one side has no remaining active combatants

Transitions:
1. `RoundStart` -> `CharacterTurn` when ordered turn list is generated
2. `CharacterTurn` -> `CharacterTurn` when current turn resolves and next turn exists
3. `CharacterTurn` -> `RoundEnd` when all turns in round are resolved
4. `RoundEnd` -> `RoundStart` when battle continues
5. `CharacterTurn` -> `BattleEnd` when victory/defeat condition is reached

## Timing Model
- Update domain: tick (turn/round steps in a turn-based loop)
- Tick rate: per turn event, duration in real-time units is Unknown
- Order dependencies: at the start of each character's turn (in `BattleSystem.ProcessTurn`), processing occurs in order:
  (1) Apply DOT/HOT effects (`StatusProcessor.ProcessPeriodicEffects`),
  (2) Check death from DOT, 
  (3) Check stun (`CurrentActor.isStunned`) — skip turn if stunned,
  (4) Tick all status durations down by 1 and remove expired (`StatusProcessor.TickDurations`),
  (5) Character takes action (`ExecuteSkill` or `ExecuteEnemyAction`).
  Duration ticking occurs after the stun check so stun correctly skips the turn before expiring.
- Code: `Assets/Scripts/Combat/StatusProcessor.cs`, `Assets/Scripts/Combat/BattleSystem.cs`
- Spatial orientation: player team is on the left side of screen facing right; enemy team is on the
  right side of screen facing left
- Rank definition: `rank 1` is front-most, `rank 4` is back-most
- Speed tie resolve rule: enemies act before player characters on equal Speed; if tied characters are
  on the same team, the front-most rank acts first

## Determinism
- Deterministic across clients: no (multiplayer behavior is not specified; random enemy skill picks
  are documented)
- Sources of nondeterminism: enemy random skill selection, attack damage range roll
- Mitigation: Unknown

## Formulas
```txt
# attack damage calculation
1. base_roll = round_to_int(effective_attack * random_uniform(0.8, 1.2))
2. scaled_base = round_to_int(base_roll * skill_scaling_multiplier)
3. buffed_damage = round_to_int(scaled_base * active_damage_multipliers)
4. critted_damage = is_critical ? round_to_int(buffed_damage * crit_multiplier) : buffed_damage
5. final_damage = ignores_defense ? critted_damage : round_to_int(critted_damage * (1.0 - target_defense / 100.0))

# hit chance
final_hit_chance_percent = min(95, attacker_accuracy_percent - target_dodge_percent)

# status application
final_status_chance_percent = source_status_chance_percent - target_resistance_percent

# effective stat calculation (percentage stacking)
# all active buffs and debuffs for a stat are summed as a flat percentage of the base stat
stat_multiplier = 1.0 + sum(buff_amplitudes_percent) - sum(debuff_amplitudes_percent)
effective_stat = round_to_int(base_stat * stat_multiplier)
```

## Resistances
Each combat character has a resistance value (percent) for every debuff category. The resistance
is subtracted from the source's application chance when resolving status effects (see formula above).

| Resistance | Opposes Status | Notes |
| --- | --- | --- |
| `move_resist` | Move | Resists forced rank-swap / repositioning effects |
| `bleed_resist` | Bleed | Resists damage-over-time bleed applications |
| `blight_resist` | Blight | Resists damage-over-time blight applications |
| `stun_resist` | Stun | Resists stun; `Buff(StunResist, +300%, 1 turn)` applied after stun expires |
| `debuff_resist` | Debuff | Resists generic stat-lowering debuffs |

- Source: `StatBlockData` fields (`moveResist`, `bleedResist`, `blightResist`, `stunResist`,
  `debuffResist`)
- Code: `Assets/Scripts/Data/StatBlockData.cs`, `Assets/Scripts/Combat/CombatCharacter.cs`
  (`GetResistance`)

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `party_size_max` | 4 | 1 | 4 | characters | GDD Combat |
| `attack_roll_min` | 0.8 | 0.8 | 1.2 | multiplier | GDD Stats |
| `attack_roll_max` | 1.2 | 0.8 | 1.2 | multiplier | GDD Stats |
| `accuracy_cap` | 95 | 0 | 95 | percent | GDD Stats |
| `crit_damage_multiplier` | 1.5 | 1.5 | Unknown | multiplier | GDD Stats |
| `stun_recovery_resist_bonus` | 300 | 300 | Unknown | percent | GDD Statuses |
| `rank_count_max` | 4 | 1 | 4 | ranks | GDD Combat |
| `max_targets_per_skill` | 4 | 1 | 4 | targets | GDD Skills |

## Edge Cases
- **Compact Formation**: Combat ranks are always contiguous from Rank 1 to Rank `team.Count`. The system does not allow "empty" slots between characters. If a character moves or dies, the formation shifts to maintain this state.
- If a character is stunned, it skips its turn.
- After stun expires, target receives a `Buff` status effect targeting `StunResist` (`+300%`,
  1-turn duration). Buff/Debuff statuses use the `StatTarget` enum to specify which stat they
  modify, applied as additive percentage multipliers of the base stat.
- Buff/Debuff Stacking: Multiple modifiers to the same stat are added together before being applied
  to the base stat (e.g., a +10% buff and a +20% buff result in a 1.3x multiplier, not 1.32x).
- Guard breaks when guardian is stunned, re-guards, or guard target changes.
- AOE that hits both guarded and guardian bypasses guard redirect.
- Some enemies can act more than once per round; each action still enters turn order.
- Ranks are mirrored between teams; rank-target legality must resolve against mirrored mapping.
- Skill usage is unavailable when current rank is outside allowed use ranks.
- Front/back checks always use team-facing orientation (player-facing-right, enemy-facing-left).
- On equal Speed between enemy and player entries, enemy entry resolves first.
- On equal Speed among characters on the same team, the front-most rank resolves first.

## Failure Modes
- Missing combat formulas per skill: `Unknown`
- Missing full operation order for status vs hit vs death checks: `Unknown`

## Event Hooks
- Event: `battle_started`, Trigger: room enters combat, Payload: run id, room id, team size
- Event: `round_started`, Trigger: new combat round, Payload: round index, turn list snapshot
- Event: `character_moved_rank`, Trigger: move action resolves, Payload: actor, old rank, new rank
- Event: `character_passed_turn`, Trigger: pass action resolves, Payload: actor
- Event: `character_action_resolved`, Trigger: action completion, Payload: actor, skill, target(s),
  hit/miss, crit, status results
- Event: `battle_ended`, Trigger: combat end, Payload: battle type, outcome, rounds elapsed, casualties, parts granted, scraps granted

## Acceptance Tests
- Automated: `Assets/Editor/Tests/` (`GuardTests`, `StunTests`, `BuffDebuffTests`, `HitCritTests`, `MoveTests`)
- Playtest: verify round flow, turn order by Speed, mirrored rank behavior, move action adjacent-rank
  swap, speed-tie behavior (enemy before player; same-team front-most rank first), stun skip
  behavior, and guard bypass behavior for AOE cases. Verify compact formation logic in small teams.

## Missing Evidence
- **Multi-Hit Stun Interaction**: Behavior if a character is stunned midway through a multi-hit skill.
- **RNG Seeding**: Centralized deterministic seeding for battle replays.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
- [x] Links and paths resolve
