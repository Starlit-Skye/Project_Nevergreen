# Implementation Plan: Extend SequenceBehavior with Conditional Skill Branching

## Goal
Extend `SequenceBehavior` in the enemy AI framework to allow checking conditions (specifically checking if the AI character currently has a specified status condition like a Buff) to dynamically choose which skill to execute at a given sequence step (e.g. `Skill 1 -> Skill 2A if Has Buff else Skill 2B -> Skill 3`).

## Specs & Documentation Updates
- [ ] Update `Docs/specs/mechanics/MECHANIC_SPEC_AI_RULES.md`:
  - Document `SequenceStep` data model (Primary Skill, Polymorphic `AIConditionNode`, Fallback Skill).
  - Document conditional sequence step evaluation flow and failure handling.
  - Document backward compatibility strategy for existing `skillSequence` asset data.

## Code Architecture (`Assets/Scripts/Combat/AI/Nodes/SequenceBehavior.cs`)
- [ ] Define `SequenceStep` serializable class inside or alongside `SequenceBehavior.cs`:
  - `SkillData skill`: Primary skill (executed unconditionally if `condition == null`, or when `condition` is met / true).
  - `[SerializeReference] [SubclassSelector] AIConditionNode condition`: Polymorphic condition node (e.g. `HasStatusCondition` with `target = Self`).
  - `SkillData fallbackSkill`: Fallback skill (executed when `condition != null` and condition evaluates to false).
- [ ] Update `SequenceBehavior` fields:
  - Add `public List<SequenceStep> steps = new List<SequenceStep>();` for structured conditional sequence steps.
  - Retain `public List<SkillData> skillSequence = new List<SkillData>();` for backward compatibility with existing AI profile assets.
- [ ] Update `SequenceBehavior.TryGetDecision`:
  - Resolve effective step count from `steps` (or fallback to legacy `skillSequence`).
  - For each evaluation attempt:
    - Determine active step.
    - Evaluate step condition: if `condition` is met (or null), select primary `skill`; otherwise select `fallbackSkill`.
    - If selected skill is null or fails rank/usage/targeting checks, apply `skipOnFailure` logic to advance sequence attempt.
    - On decision success, advance `AIHistory` sequence index by total steps evaluated.

## Verification & Unit Testing (`Assets/Editor/Tests/AIRuleTests.cs`)
- [ ] Add unit test `SequenceBehavior_ExecutesPrimarySkill_WhenConditionIsMet`:
  - Apply Buff status effect to AI character, verify sequence chooses `Skill 2A`.
- [ ] Add unit test `SequenceBehavior_ExecutesFallbackSkill_WhenConditionIsNotMet`:
  - Ensure AI character lacks Buff status effect, verify sequence chooses `Skill 2B`.
- [ ] Add unit test `SequenceBehavior_SkipsStep_WhenConditionNotMetAndNoFallback`:
  - When condition is false and fallback is null with `skipOnFailure = true`, verify sequence skips to the next step.
- [ ] Add unit test `SequenceBehavior_BackwardCompatibility_LegacySkillSequence`:
  - Verify existing sequence profiles using `skillSequence` list evaluate correctly without configuration changes.
