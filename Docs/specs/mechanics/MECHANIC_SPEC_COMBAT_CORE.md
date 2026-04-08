# Combat Core Mechanic

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define the baseline turn-based combat mechanic used for player team versus enemy team battles.

## Scope
- In scope: turn order, rank-based positioning, actions, hit/crit flow, core status interactions,
  round progression
- Out of scope: skill catalog details, class-specific skill formulas, UI art/layout implementation

## Source of Truth
- Code: `Unknown` (combat runtime implementation not provided)
- Tests: `Unknown` (combat test artifacts not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: Combat, Stats, Resistances, Statuses, Skills, Technical)
- Data: `Assets/docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md` (runtime base stat source), `Assets/docs/specs/mechanics/MECHANIC_SPEC_COMBAT_INPUT_INTERACTIONS.md` (input targeting interaction flow)
- Issue/ADR: Unknown

## Inputs
- Input action: choose one equipped skill, or use move action
- Input conditions: active only on player-controlled character turn; rank constraints apply to both
  skill use and target eligibility
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
- Order dependencies: status start-of-turn effects resolve before active action (partial evidence);
  full execution order is Unknown
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
# attack damage range
rolled_attack = round_to_int(base_attack * random_uniform(0.8, 1.2))

# hit chance
final_hit_chance_percent = min(95, attacker_accuracy_percent - target_dodge_percent)

# crit damage
crit_damage = base_damage * 1.5

# status application
final_status_chance_percent = source_status_chance_percent - target_resistance_percent
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `party_size_max` | 4 | 1 | 4 | characters | GDD Combat |
| `attack_roll_min` | 0.8 | 0.8 | 1.2 | multiplier | GDD Stats |
| `attack_roll_max` | 1.2 | 0.8 | 1.2 | multiplier | GDD Stats |
| `accuracy_cap` | 95 | 0 | 95 | percent | GDD Stats |
| `crit_damage_multiplier` | 1.5 | 1.5 | Unknown | multiplier | GDD Stats |
| `stun_recovery_resist_bonus` | 300 | 300 | Unknown | percent | GDD Statuses |
| `rank_count_per_team` | 4 | 4 | 4 | ranks | GDD Combat |
| `max_targets_per_skill` | 4 | 1 | 4 | targets | GDD Skills |

## Edge Cases
- If a character is stunned, it skips its turn.
- After stun expires, target gains `+300%` stun resistance.
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
- Event: `character_action_resolved`, Trigger: action completion, Payload: actor, skill, target(s),
  hit/miss, crit, status results
- Event: `battle_ended`, Trigger: combat end, Payload: battle type, outcome, rounds elapsed, casualties, parts granted, scraps granted

## Acceptance Tests
- Automated: Unknown (combat test paths not provided)
- Playtest: verify round flow, turn order by Speed, mirrored rank behavior, move action adjacent-rank
  swap, speed-tie behavior (enemy before player; same-team front-most rank first), stun skip
  behavior,
  and guard bypass behavior for AOE cases

## Missing Evidence
- Combat implementation path(s) and symbols
- Test suite path(s) for turn order, hit chance, and status resolution
- Data/config table for core constants and rank mapping
- Character runtime instantiation path that binds level to `current_level - 1` stat lookup
- Determinism and RNG seeding policy

## Validation
- [ ] Facts match current code/content
- [ ] Timing and determinism assumptions are explicit
- [ ] Tuning variables map to actual data/config
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined




