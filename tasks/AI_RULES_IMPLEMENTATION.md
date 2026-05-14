# 🧠 AI Rule Expansion: Repetition & Sequencing

This document tracks the implementation of history-based AI rules and deterministic skill sequencing for the Nevergreen AI Framework.

## 📋 Task List

### Phase 1: Repetition Control
- [x] **Contextual Conditions**: Update `AIConditionNode.IsMet` to accept `SkillData contextSkill`.
- [x] **Implementation**: Create `RepetitionCondition.cs` in `Nodes/`.
- [x] **Validation**: Verify `RuleBasedBehavior` correctly passes its `skillToUse` to the condition.

### Phase 2: Sequence Logic
- [x] **History Update**: Add `sequenceIndex` tracking to `AIHistory.cs`.
- [x] **Implementation**: Create `SequenceBehavior.cs` in `Nodes/`.
- [x] **Logic Integration**: Ensure `sequenceIndex` increments correctly after a successful sequence action.

### Phase 3: Testing & Polish
- [x] **Unit Tests**: Create `AIRuleTests.cs` to verify both features. (13/13 pass)
- [x] **Regression**: Existing `AITests` (6/6 pass) — zero regressions.
- [x] **Documentation**: Update `DESIGNER_GUIDE_ENEMY_AI.md` with the new rule types.

---

## 🛠️ Technical Specifications

### RepetitionCondition
- **Goal**: Prevent a skill from being used more than `N` times in a row.
- **Node Type**: `AIConditionNode`
- **Fields**: `int maxConsecutiveUses`
- **Logic**: Returns `false` if `brain.History.lastSkillUsed == contextSkill` AND `brain.History.consecutiveSkillUses >= maxConsecutiveUses`.

### SequenceBehavior
- **Goal**: Cycle through a fixed skill order (A → B → C → A → ...).
- **Node Type**: `AIBehaviorNode`
- **Fields**: `List<SkillData> skillSequence`, `AITargetingNode targeting`, `string sequenceId`, `bool skipOnFailure`
- **State**: `sequenceIndex` tracked per-brain in `AIHistory` via `Dictionary<string, int>`.
- **Logic**: On each turn, attempts the skill at `sequenceIndex`. On success, increments index (wraps around). On failure with `skipOnFailure`, tries next index.

---

## ✅ Review

All phases complete. Changes are backward-compatible:
- `IAICondition` gained a default interface method `IsMet(brain, battle, contextSkill)` that falls through to the original `IsMet`.
- `AIConditionNode` has a virtual `IsMet` overload — existing subclasses (`HealthCondition`) are unaffected.
- `RuleBasedBehavior` now calls the contextual overload, which defaults to the old behavior for non-context-aware conditions.
