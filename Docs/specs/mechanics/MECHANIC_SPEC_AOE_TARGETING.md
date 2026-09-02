# AOE Targeting System

Owner: Combat/Design
Status: active
Last verified: 2026-05-22
Verified commit: 3e0600f27257db51bedeb5b0f14c92b5ff1bc151
Target build: Unity 6000.3.9f1 + Windows PC

## Purpose
The Area of Effect (AOE) targeting system uses a rank-based linear propagation model. When a player or AI selects an AOE skill, they specify a single primary "anchor" target, and the skill automatically propagates to affect characters behind that anchor within the target formation. This prevents the selection of arbitrary target sets, adds spatial tactical depth, and simplifies visual targeting feedback.

## Scope
- **In scope**:
  - Centralized target selection logic via `BattleSystem.GetAOETargets`.
  - Dynamic target hover-preview highlight in the UI during targeting.
  - Linear backwards propagation from the anchor target to subsequent ranks behind them.
  - Handling of multi-rank (size 2/3) characters and deceased units (piles).
- **Out of scope**:
  - Custom target patterns (e.g. cross, split, or random secondary targets).
  - Manual player selection of secondary targets.
  - Friendly fire targeting rules.

## Source of Truth
- **Code**:
  - `Assets/Scripts/Combat/BattleSystem.cs` (`GetAOETargets`, `GetValidTargets`)
  - `Assets/Scripts/Prototype/CombatUI.cs` (`UpdateTargetHoverHighlight`, `TrySelectTarget`)
  - `Assets/Scripts/Combat/AI/Nodes/SimpleTargeting.cs` (`TryResolveTargets`)
  - `Assets/Scripts/Combat/AI/Nodes/RandomSkillBehavior.cs` (`TryResolveTargets`)
- **Tests**:
  - `Assets/Editor/Tests/AoeTargetingTests.cs` (covers simple linear hits, trailing bounds, large size handling, piles in damaging vs healing skills)
  - `Assets/Editor/Tests/FormationTests.cs` (`GetValidTargets_MultiRankEnemy_NotDuplicatedByAOE`)

## Inputs
- **Input action**: Hovering and left-clicking on a character during targeting mode.
- **Input conditions**: Active character must have selected a skill targeting the opponent/ally team, and the cursor must hover over a valid primary target.
- **Input buffering**: None.

## State Model
Targeting selection is a transient UI state managed via `CombatUI.cs`.
- `Selecting`: Entered when a skill button is pressed and the skill requires target selection. Exited when a valid target is clicked (`TrySelectTarget`), or selection is cancelled (e.g. clicking away).

## Timing Model
- **Update domain**: UI hover checks run on the game thread inside `CombatUI.Update` when targeting is active.
- **Tick rate**: Screen-space raycasting runs at the display frame rate (`Update` loop).
- **Order dependencies**: `UpdateTargetHoverHighlight` executes after `Physics2D.Raycast` resolves the hovered character collider.

## Determinism
- **Deterministic across clients**: Yes. The contiguous target resolution algorithm relies entirely on the combat formation layout, character rank values, and the deterministic `GetAOETargets` function.
- **Sources of nondeterminism**: None for player selection. AI uses standard random selection when picking a random primary target from the valid pool.

## Formulas
No complex algebraic formulas. Target propagation is resolved by:
1. Sorting the target team pool by rank (front to back).
2. Finding the index of the primary target.
3. Extracting a sub-segment starting at the primary target's index up to the skill's `maxTargets` limit.

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `maxTargets` | 1 | 1 | 4 | targets | `SkillData.maxTargets` |
| `targetRanks` | [1, 2, 3, 4] | 1 | 4 | ranks | `SkillData.targetRanks` |

## Edge Cases
- **Trailing Boundary (Limit)**: If the primary target is near the back of the team formation and there are fewer units behind them than `maxTargets`, the list is clamped to only return the available units.
- **Multi-Rank Characters**: Large enemies (size 2/3) only count as a single target in the result list (preventing double-hitting), but they consume multiple rank-slots of the AOE budget proportional to their size. A size-2 character consumes 2 of the `maxTargets` rank budget. This means an AOE skill with `maxTargets = 2` targeting a size-2 enemy will only hit that single enemy. The primary target is always included, even if its size exceeds the total `maxTargets` budget.
- **Piles (Deceased Units)**: Piles are treated as valid targets and count towards the trailing target limit for both damaging and healing skills. However, healing skills do not apply any healing or positive effects to them (as piles refuse healing).
- **AOE Healing/Status on Piles**: For AOE skills (`maxTargets > 1`), a Pile can be selected as the primary anchor target for a healing or status-only skill **if and only if** the resulting AOE range includes at least one active, living target. If no living targets exist behind the Pile in the AOE range, the Pile is rejected as an anchor.

## Failure Modes
- **No Valid Targets**: If `GetValidTargets` returns zero eligible targets, targeting mode cannot be completed. The AI will fall back to passing the turn.

## Event Hooks
- None.

## Acceptance Tests
- **Automated**:
  - `AoeTargetingTests.GetAOETargets_SimpleLinearHits_ReturnsPrimaryAndBehind`
  - `AoeTargetingTests.GetAOETargets_TrailingLimit_ReturnsOnlyAvailable`
  - `AoeTargetingTests.GetAOETargets_LargeSizeHandling_HitsSize2AndNext`
  - `AoeTargetingTests.GetAOETargets_DamagingSkill_IncludesPiles`
  - `AoeTargetingTests.GetAOETargets_HealingSkill_IncludesPilesButDoesNotHeal`
  - `AoeTargetingTests.GetValidTargets_AOEHealingSkill_AllowsPileAnchorIfLivingUnitInAOERange`
  - `AoeTargetingTests.GetValidTargets_AOEHealingSkill_RejectsPileAnchorIfNoLivingUnitInAOERange`
- **Playtest**:
  - Enter combat, select an AOE skill with `maxTargets = 2`. Hovering over rank 1 enemy must highlight rank 1 (green) and rank 2 (green), while remaining valid targets (rank 3, 4) highlight yellow. Hovering over a pile with a healing skill must highlight it (since it is targeted) but applying the skill will not heal it.

## Missing Evidence
- None.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
