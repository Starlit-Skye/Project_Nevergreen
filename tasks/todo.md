# Implementation Plan: Conditional Skill Branching in SequenceBehavior

Full design: see `sequence_behavior_extension_plan.md` artifact (v2).

## Decisions
- Replace `skillSequence` with `steps` (migrate 2 assets + tests)
- Condition false + no `elseSkill` → always skip step (ignores `skipOnFailure`)
- Buff/Debuff conditions stay tied to one stat (no change to Has/NotHasStatusCondition)

## Code
- [x] Add `SequenceStep { skill, condition, elseSkill }` in `SequenceBehavior.cs`
- [x] Replace `skillSequence` with `List<SequenceStep> steps`
- [x] Rewrite `TryGetDecision` per the v2 algorithm (context-skill `IsMet`, no switching branches, intentional skip)

## Assets
- [x] Re-author `AI_overheating_golem.asset` (2 unconditional steps, skipOnFailure = 0)
- [x] Clear `AI_screeching_corvus.asset` steps (was unconfigured)
- [x] Verify both load with no serialization warnings

## Docs
- [x] `MECHANIC_SPEC_AI_RULES.md`: scope, data model, pseudocode, timing rule (duration >= 2), Event Hooks fix, tests list
- [x] `DESIGNER_GUIDE_ENEMY_AI.md`: Sequence section + branching example + gotchas

## Tests (`AIRuleTests.cs`)
- [x] `Steps(...)` helper; migrate the 5 existing sequence tests
- [x] UsesSkill_WhenConditionMet
- [x] UsesElseSkill_WhenConditionNotMet
- [x] ConditionFalseNoElse_SkipsStep_EvenWhenSkipOnFailureDisabled
- [x] SelectedBranchUnusable_DoesNotTryOtherBranch
- [x] ConditionReevaluatedEachCycle
- [x] IndexWrapsCorrectly_WhenLastStepSkipped
- [x] ExpiredStatus_TreatedAsAbsent
- [x] BuffOnDifferentStat_DoesNotMatch
- [x] Run AIRuleTests + AITests (EditMode); check console

## Review
_(fill in after implementation)_
