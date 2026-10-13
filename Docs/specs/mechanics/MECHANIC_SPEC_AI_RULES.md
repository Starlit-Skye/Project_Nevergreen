# AI Behavioral Rules: Sequencing & Repetition

Owner: Combat Engineering Team
Status: active
Last verified: 2026-05-14
Verified commit: Unknown
Target build: Unity 6 + PC

## Purpose
Extend the modular AI framework with deterministic skill rotations and repetition constraints. This allows for complex combat "patterns" (combos) and prevents unintended skill spamming in randomized profiles, enhancing tactical depth and predictability.

## Scope
- In scope: Stateful skill sequencing, conditional branching within sequence steps, per-brain sequence tracking, consecutive usage limits for random selection, turn passing on constraint failure.
- Out of scope: Global cooldowns across different AI profiles.

## Source of Truth
- Code: `Assets/Scripts/Combat/AI/Nodes/SequenceBehavior.cs` (Sequencing logic)
- Code: `Assets/Scripts/Combat/AI/Nodes/RandomSkillBehavior.cs` (Repetition logic)
- Code: `Assets/Scripts/Combat/AI/AIHistory.cs` (State persistence)
- Tests: `Assets/Editor/Tests/AIRuleTests.cs` (Validation suite)
- Design: `Docs/guides/DESIGNER_GUIDE_ENEMY_AI.md`

## Inputs
- Input action: AI Turn Evaluation (`AIBrain.EvaluateTurn`)
- Input conditions: 
  - For Sequencing: Must have a valid `sequenceId` and non-empty `skillSequence`.
  - For Repetition: `maxConsecutiveUses > 0` in `RandomSkillBehavior`.

## State Model
States:
- `Evaluating`: The brain is processing the behavior list.
- `Executing`: A skill is selected and passed to the `BattleSystem`.
- `Blocked`: A skill is valid for the rank but forbidden by repetition rules.
- `Skipping`: A skill in a sequence is invalid for the current rank and `skipOnFailure` is enabled.

Transitions:
1. `Evaluating` -> `Executing` when a valid skill/target pair is found.
2. `Executing` -> `Evaluating` (Next Turn) when history is updated.
3. `Evaluating` -> `Blocked` when `consecutiveSkillUses >= maxConsecutiveUses`.
4. `Blocked` -> `Executing` (Pass) if no alternative skills exist.

## Timing Model
- Update domain: Turn-based synchronous tick.
- Tick rate: Once per enemy character turn in `BattleSystem.ProcessTurn`.
- Order dependencies: Executes after status effects and stun checks, but before `BattleSystem.ExecuteSkill`.
  - **Important**: Because `StatusProcessor.TickDurations` happens *before* AI evaluation, any status condition checking for a Buff/Debuff requires that effect to have a duration of at least 2 turns (so it survives the initial decrement before the AI reads it).

## Determinism
- Deterministic across clients: Yes. `SequenceBehavior` uses integer indexing stored in `AIHistory`. `RandomSkillBehavior` uses standard `UnityEngine.Random` but filters the available pool deterministically based on usage history.
- Sources of nondeterminism: None (excluding the inherent randomness of `RandomSkillBehavior`'s selection from the *valid* pool).
- Mitigation: All state is tracked in `AIHistory` which can be serialized/replayed.

## Formulas
```txt
# Repetition Filtering
valid_skills = equipped_skills.filter(s => 
    s.IsUsableFrom(current_rank) && 
    (last_skill != s || consecutive_uses < max_consecutive_uses)
)

# Sequence Progression
current_index = history.GetSequenceIndex(sequence_id)
for (attempt = 0; attempt < sequence_length; attempt++) {
    step = steps[(current_index + attempt) % sequence_length]
    if (step.condition_met) chosen_skill = step.skill
    else chosen_skill = step.else_skill

    if (chosen_skill == null) continue // Intentional skip!

    if (chosen_skill.is_usable) {
        history.AdvanceSequenceIndex(sequence_id, attempt + 1)
        execute(chosen_skill)
        break
    } else if (!skipOnFailure) {
        fail_behavior()
    }
}
```

## Tuning Variables
| Variable | Default | Min | Max | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `maxConsecutiveUses` | 0 (Unlimited) | 0 | 99 | uses | `RandomSkillBehavior` |
| `skipOnFailure` | true | false | true | bool | `SequenceBehavior` |

## Target Selection Rules
- **Pile Avoidance**: When AI behaviors or targeting nodes (`SimpleTargeting`, `StatusPrioritizedTargeting`, etc.) evaluate valid targets, they actively prioritize non-pile targets. If active characters (`!c.IsPile`) exist in the valid target pool, all Piles are filtered out from primary target selection. Piles are only selected as primary targets if the entire valid target pool consists of Piles (fallback mode to avoid passing turn). Note that AOE trailing propagation may still hit Piles if they are positioned behind an active primary target.

## Edge Cases
- **1-Skill Repetition**: If a character has only one skill and hits the `maxConsecutiveUses` limit, it will return an `AIDecision.Pass()`. The pass action resets the `consecutiveSkillUses` counter, allowing the skill to be used again on the next turn.
- **Sequence Rank Failure**: If a chosen skill in a sequence is unusable from the current rank:
  - If `skipOnFailure` is `true`: The AI attempts to use the *next* step in the sequence in the same turn.
  - If `skipOnFailure` is `false`: The entire behavior fails, and the brain moves to the next behavior in the profile list.
- **Intentional Skip**: If a sequence step has a condition that evaluates to `false` and there is no `Else Skill`, the sequence will *always* skip this step and advance to the next step immediately, even if `skipOnFailure` is `false`.
- **Persistence**: Multiple enemies using the same AI Profile track their `sequenceIndex` independently via their unique `AIHistory` instance.

## Failure Modes
- **Empty Sequence**: If a `SequenceBehavior` has no skills, it returns `false`, allowing the brain to fall through.
- **Infinite Loop (Skip)**: If all skills in a sequence are unusable and `skipOnFailure` is true, the node will eventually return `false` after checking every skill once to prevent infinite loops.

## Event Hooks
- Event: `RecordDecision`, Trigger: When a decision is finalized, Payload: `AIDecision`. Updates history and sequence indices.

## Acceptance Tests
- Automated: `Assets/Editor/Tests/AIRuleTests.cs`
  - `RandomSkillBehavior_BlocksAtLimitAndPassesIfNoOtherSkills`
  - `SequenceBehavior_UsesSkill_WhenConditionMet`
  - `SequenceBehavior_ConditionFalseNoElse_SkipsStep_EvenWhenSkipOnFailureDisabled`
  - `SequenceBehavior_SelectedBranchUnusable_DoesNotTryOtherBranch`
- Playtest: Verify a "Combo Dancer" enemy uses `Buff` -> `Heavy Attack` -> `Rest`.

## Validation
- [x] Facts match current code/content
- [x] Timing and determinism assumptions are explicit
- [x] Tuning variables map to actual data/config
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
- [x] Links and paths resolve
