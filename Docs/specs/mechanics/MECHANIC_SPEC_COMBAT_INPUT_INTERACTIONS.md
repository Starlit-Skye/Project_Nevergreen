# Combat Input and Targeting Interaction

Owner: Unknown
Status: draft
Last verified: 2026-04-12
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define player combat interaction flow for selecting skill/move actions, selecting valid targets,
and respecting animation-driven input locks during combat resolution.

## Scope
- In scope: skill/move click selection, valid target indicator behavior, hover highlight behavior,
  target click execution trigger, hover-driven stat display updates, animation-chain input locking
- Out of scope: non-combat UI tabs, level-up UI behavior, hotkey/controller bindings

## Source of Truth
- Code: `Assets/Scripts/Prototype/CombatUI.cs` (`OnSkillButtonClicked`, `TrySelectTarget`,
  `UpdateStatsHover`), `Assets/Scripts/Combat/BattleSystem.cs` (`WaitForPlayerAction`,
  `SubmitPlayerAction`, `SubmitMoveAction`, `SubmitPassAction`)
- Tests: `Unknown` (combat input tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.2id7yg1ohd4o
  (sections: Inputs and Interactions -> Combat, Inputs and Interactions -> Animations,
  Technical -> Animations, UI Design -> Combat Screen)
- Data: `Docs/specs/systems/SYSTEM_SPEC_COMBAT_SCREEN.md` (`CombatUIState`),
  `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` (turn/rank legality),
  `Docs/specs/systems/SYSTEM_SPEC_ANIMATION_RUNTIME.md` (animation queue and lock safeguards)
- Issue/ADR: Unknown

## Inputs
- Input action: click a skill or move action during player turn
- Input action: hover over candidate target
- Input action: click a valid target
- Input conditions: only active on player-controlled turn with an available action and unlocked input
- Input buffering: Unknown

## State Model
States:
- `AwaitActionSelection`: waiting for skill/move click
- `ActionSelected`: skill/move is selected and valid targets are visualized
- `TargetHover`: pointer is over a valid target and highlight is active
- `ExecuteActionRequested`: valid target click commits action execution request
- `AnimationLocked`: skill and UI update animations are resolving and input is locked
- `ReturnToAwait`: action resolves, queue drains, and UI returns to waiting state

Transitions:
1. `AwaitActionSelection` -> `ActionSelected` when player clicks a skill/move
2. `ActionSelected` -> `TargetHover` when pointer hovers a valid target
3. `TargetHover` -> `ExecuteActionRequested` when player clicks hovered valid target
4. `ActionSelected` -> `ExecuteActionRequested` when player clicks a valid target without hover dwell
5. `ExecuteActionRequested` -> `AnimationLocked` when animation queue has at least one animation
6. `AnimationLocked` -> `ReturnToAwait` when animation queue size reaches `0`
7. `ReturnToAwait` -> `AwaitActionSelection` when next player action phase begins

## Timing Model
- Update domain: event-driven UI interactions and animation completion callbacks during combat
- Tick rate: per click/hover event and per animation-completion event
- Order dependencies: action selection must occur before valid target indicators appear; target click
  must pass validity check before action execution; after execution, skill animation resolves before
  UI update animations; turn flow advances only after all queued animations complete

## Determinism
- Deterministic across clients: partial
- Sources of nondeterminism: pointer hover timing/order and human input timing
- Mitigation: deterministic target validity checks from combat state, FIFO animation queue ordering,
  and lock-until-empty queue rule

## Formulas
```txt
# interaction gate
can_select_action = (active_team == player) AND (active_actor_can_act == true) AND (input_locked == false)

# target validity
is_valid_target = target_id IN valid_targets(selected_action, combat_state)

# execution trigger
execute = can_select_action AND action_selected AND is_valid_target

# input lock
input_locked = (animation_queue_count > 0)
unlock_input = (animation_queue_count == 0)
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `valid_target_indicator_type` | small 2D sprite | Unknown | Unknown | ui asset | GDD Inputs/Interactions |
| `target_hover_highlight_enabled` | true | false | true | bool | GDD Inputs/Interactions |
| `show_hovered_stats` | true | false | true | bool | GDD Inputs/Interactions |
| `max_animation_queue_size` | 15 | 1 | Unknown | animations | GDD Technical |
| `animation_lock_overrun_grace` | 5 | 0 | Unknown | s | GDD Technical |

## Edge Cases
- Clicking a skill/move with no valid targets should not execute action (fallback behavior Unknown).
- Hovering invalid targets should not show valid-target selected highlight.
- Clicking invalid targets should not execute action.
- Hovering characters updates stats panel even before target click.
- Target indicator must anchor at character feet region (not center/head), matching combat mockup.
- Input must remain locked across chained animations and must not unlock between chained items.
- If queue length reaches `15`, queue should be cleared and input should unlock immediately.
- If lock time exceeds expected animation length by `5 s`, queue should be cleared and input unlocked.

## Failure Modes
- Action executes without valid target gating
- Valid-target indicators shown for illegal targets
- Hovered stats panel does not match hovered character
- Input remains locked after queue is drained
- Input unlocks early while queued animation chain still has pending entries

## Event Hooks
- Event: `combat_skill_selected`, Trigger: click on skill/move, Payload: actor id, selected action id,
  valid target ids
- Event: `combat_target_hover_changed`, Trigger: hover enter/leave on candidate targets, Payload:
  hovered character id, highlight state, hovered effective stats
- Event: `combat_target_confirmed`, Trigger: click valid target, Payload: actor id, action id,
  target id
- Event: `combat_animation_lock_state_changed`, Trigger: animation queue transitions between empty
  and non-empty, Payload: queue size, lock state
- Event: `combat_animation_queue_safeguard_triggered`, Trigger: queue overflow or lock overtime
  threshold, Payload: reason, queue size, expected length, elapsed lock time

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify skill/move click marks selection, valid target sprites appear at feet of valid
  targets, hover highlight changes color on valid targets, clicking valid target executes action, and
  hovering any character updates stats display section; verify input remains locked throughout chained
  skill and UI update animations and unlocks only after all animations complete; verify both
  safeguards (`15` queue cap and expected-length-plus-`5 s` overrun) unlock input

## Missing Evidence
- Behavior for selected action cancellation/reselection
- Keyboard/controller interaction mapping
- Indicator color/state asset definitions and thresholds
- Concrete animation queue runtime implementation path and symbols
- Mapping table from skill id to animation id and clip duration values
- Automated tests for lock/unlock sequencing and safeguard triggers

## Validation
- [ ] Facts match current code/content
- [ ] Timing and determinism assumptions are explicit
- [ ] Tuning variables map to actual data/config
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined
