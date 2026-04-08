# Combat Input and Targeting Interaction

Owner: Unknown
Status: draft
Last verified: 2026-04-06
Verified commit: Unknown
Target build: Unity 6000.3.9f1 + Standalone/Android

## Purpose
Define player combat interaction flow for selecting skill/move actions and selecting valid targets.

## Scope
- In scope: skill/move click selection, valid target indicator behavior, hover highlight behavior,
  target click execution trigger, hover-driven stat display updates
- Out of scope: non-combat UI tabs, level-up UI behavior, hotkey/controller bindings

## Source of Truth
- Code: `Unknown` (combat input implementation not provided)
- Tests: `Unknown` (combat input tests not provided)
- Design: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?tab=t.0
  (sections: Inputs and Interactions -> Combat, UI Design -> Combat Screen)
- Data: `Assets/docs/specs/systems/SYSTEM_SPEC_COMBAT_SCREEN.md` (`CombatUIState`), `Assets/docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` (turn/rank legality)
- Issue/ADR: Unknown

## Inputs
- Input action: click a skill or move action during player turn
- Input action: hover over candidate target
- Input action: click a valid target
- Input conditions: only active on player-controlled turn with an available action
- Input buffering: Unknown

## State Model
States:
- `AwaitActionSelection`: waiting for skill/move click
- `ActionSelected`: skill/move is selected and valid targets are visualized
- `TargetHover`: pointer is over a valid target and highlight is active
- `ExecuteAction`: valid target click commits action execution
- `ReturnToAwait`: action resolves and UI resets to waiting state

Transitions:
1. `AwaitActionSelection` -> `ActionSelected` when player clicks a skill/move
2. `ActionSelected` -> `TargetHover` when pointer hovers a valid target
3. `TargetHover` -> `ExecuteAction` when player clicks hovered valid target
4. `ActionSelected` -> `ExecuteAction` when player clicks a valid target without hover dwell
5. `ExecuteAction` -> `ReturnToAwait` when action command is submitted
6. `ReturnToAwait` -> `AwaitActionSelection` when next action phase begins

## Timing Model
- Update domain: event-driven UI interactions during player turn
- Tick rate: per click/hover event
- Order dependencies: action selection must occur before valid target indicators appear; target click
  must pass validity check before action execution

## Determinism
- Deterministic across clients: partial
- Sources of nondeterminism: pointer hover timing/order and human input timing
- Mitigation: deterministic target validity checks from combat state

## Formulas
```txt
# interaction gate
can_select_action = (active_team == player) AND (active_actor_can_act == true)

# target validity
is_valid_target = target_id IN valid_targets(selected_action, combat_state)

# execution trigger
execute = can_select_action AND action_selected AND is_valid_target
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `valid_target_indicator_type` | small 2D sprite | Unknown | Unknown | ui asset | GDD Inputs/Interactions |
| `target_hover_highlight_enabled` | true | false | true | bool | GDD Inputs/Interactions |
| `show_hovered_stats` | true | false | true | bool | GDD Inputs/Interactions |

## Edge Cases
- Clicking a skill/move with no valid targets should not execute action (fallback behavior Unknown).
- Hovering invalid targets should not show valid-target selected highlight.
- Clicking invalid targets should not execute action.
- Hovering characters updates stats panel even before target click.
- Target indicator must anchor at character feet region (not center/head), matching combat mockup.

## Failure Modes
- Action executes without valid target gating
- Valid-target indicators shown for illegal targets
- Hovered stats panel does not match hovered character

## Event Hooks
- Event: `combat_skill_selected`, Trigger: click on skill/move, Payload: actor id, selected action id,
  valid target ids
- Event: `combat_target_hover_changed`, Trigger: hover enter/leave on candidate targets, Payload:
  hovered character id, highlight state, hovered effective stats
- Event: `combat_target_confirmed`, Trigger: click valid target, Payload: actor id, action id,
  target id

## Acceptance Tests
- Automated: Unknown (test paths not provided)
- Playtest: verify skill/move click marks selection, valid target sprites appear at feet of valid
  targets, hover highlight changes color on valid targets, clicking valid target executes action, and
  hovering any character updates stats display section

## Missing Evidence
- Concrete input-layer implementation path and symbols
- Behavior for selected action cancellation/reselection
- Keyboard/controller interaction mapping
- Indicator color/state asset definitions and thresholds

## Validation
- [ ] Facts match current code/content
- [ ] Timing and determinism assumptions are explicit
- [ ] Tuning variables map to actual data/config
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined
