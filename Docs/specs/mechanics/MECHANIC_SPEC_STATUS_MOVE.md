# Status Effect: Move

Owner: Combat Engineering Team
Status: active
Last verified: 2026-05-04
Verified commit: HEAD
Target build: Unity 2022.3 + Windows

## Purpose
The Move status effect allows characters to disrupt enemy formations or reposition allies by
displacing them across combat ranks. This is a tactical utility used to pull vulnerable
targets forward or push dangerous front-line threats to less effective positions.

## Scope
- In scope: Instantaneous rank displacement, resistance calculations, formation shifting,
  rank clamping based on team size, and immediate status expiration.
- Out of scope: Visual UI indicators (arrows/icons), specific skill animations, or post-move
  AI pathfinding.

## Source of Truth
- Code: `Assets/Scripts/Data/SkillData.cs` (`StatusType.Move`), `Assets/Scripts/Combat/CombatCharacter.cs` (`rank`), `Assets/Scripts/Combat/BattleSystem.cs` (`ExecuteMoveAndShift`), `Assets/Scripts/Combat/Effects/MoveStatusInstance.cs`, `Assets/Scripts/Combat/Effects/StatusEffect.cs`
- Design: [Google Doc](https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?usp=sharing)
- Data: `StatTarget.MoveResist` and `StatusType.Move` enums in `Assets/Scripts/Data/SkillData.cs`.

## Inputs
- Input action: Skill execution containing a `Move` status effect or manual player "Move" action.
- Input conditions: Target must be a character in a valid rank (1 to `team.Count`).

## State Model
States:
- `Stationary`: Default state where the character occupies a specific rank.
- `Displacing`: Transient state during rank reordering and visual tweening.

Transitions:
1. `Stationary` -> `Displacing` when a Move status is applied.
2. `Displacing` -> `Stationary` once logical rank is updated and visual tween completes.

## Timing Model
- Update domain: combat tick (per action).
- Tick rate: Instantaneous.
- Duration: 0 (configured in `MoveStatusInstance`).
- Lifecycle:
  - Triggered in `MoveStatusInstance.OnAdded`.
  - Immediately calls `BattleSystem.ExecuteMoveAndShift`.
  - Immediately calls `host.RemoveStatus(this)` to ensure it never persists in the character's status list.

## Determinism
- Deterministic across clients: Yes (rank indices are discrete integers 1-4).
- Sources of nondeterminism: RNG during resistance check.
- Mitigation: Seeded RNG in `SkillContext`.

## Formulas
```txt
# Application Success Check
FinalChance = SkillApplicationChance - TargetMoveResistance

# Target Rank Calculation
RawTargetRank = CurrentRank + Amplitude
ClampedTargetRank = clamp(RawTargetRank, 1, team.Count)

# Rank Shifting (Forward: Pull)
If ClampedTargetRank < CurrentRank:
    Characters in [ClampedTargetRank, CurrentRank - 1] shift back (+1 rank)

# Rank Shifting (Backward: Push)
If ClampedTargetRank > CurrentRank:
    Characters in [CurrentRank + 1, ClampedTargetRank] shift forward (-1 rank)
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `Move Amplitude` | 1 | -3 | 3 | ranks | SkillData (Amplitude) |
| `Move Resistance` | Variable | 0 | 500 | % | CombatStats (MoveResist) |
| `Pile Move Resist`| 300 | 300 | 300 | % | CombatConfig/GDD |

## Edge Cases
- **Team-Size Clamping**: Characters cannot be moved to a rank higher than the current number of
  active members on their team (max 4). For example, in a 3-member team, a push of amplitude 5
  will clamp the target to Rank 3.
- **Compact Formation**: The system maintains a compact formation by shifting other characters
  to fill gaps. There are never "empty" ranks between Rank 1 and Rank `team.Count`.
- **Corpse (Pile) Movement**: Corpses are included in the team count for rank calculation and
  occupy rank space, but they have high Move Resistance (300%).
- **Position Anchoring**: `BattleSystem` captures the X-coordinates of all characters (alive and
  dead) before shifting to ensure smooth transitions between valid rank positions.

## Failure Modes
- **Invalid BattleSystem**: If `BattleSystem.Instance` is null during application (e.g., in some
  test environments), the move logic is bypassed to prevent crashes.

## Event Hooks
- Event: `CombatCharacter.OnStatsChanged`, Trigger: When rank changes.
- Event: `BattleSystem.OnActionResolved`, Trigger: After move completes.

## Acceptance Tests
- Automated: `Assets/Editor/Tests/MoveTests.cs`
  - `Move_RankDisplacement_PullForward`: Verify pull logic.
  - `Move_RankDisplacement_PushBackward`: Verify push logic.
  - `Move_Resistance_HighResistBlocksMove`: Verify 300% resist behavior.
  - `InstantExpiration_MoveStatusExpiresImmediately`: Verify status removal.
  - `Clamping_DoesNotExceedMaxRanks`: Verify clamping to team size.
  - `BugReproduction_SmallTeam_LargeAmplitude`: Verify correct shifting in small teams.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
- [x] Links and paths resolve
