# Rank Shift Animation Implementation

## Plan
Based on the current architecture and user requirements.

### Phase 1: Logic
- [x] Add `ExecuteMoveAndShift(CombatCharacter mover, int targetRank)` method in `BattleSystem.cs`.
- [x] Implement snapshot of current rank X-axis positions.
- [x] Implement shifting logic (incrementing/decrementing ranks) to make room.
- [x] Calculate target X-axis positions for the mover and all shifting characters.

### Phase 2: Animation
- [x] Change `WaitTimerStep` in `BattleSystem.SubmitMoveAction` to a `ParallelStep`.
- [x] Add `DOTweenStep` wrapper for each character that receives a `DOMoveX` tween.

### Phase 3: Integration
- [x] Update `BattleSystem.SubmitMoveAction(CombatCharacter swapTarget)` to correctly capture the intent of a move and redirect to `ExecuteMoveAndShift`. 
- [x] Update `CombatUI.cs` to submit the desired target rank, or change `SubmitMoveAction` to infer the target rank from the clicked target.

### Phase 4: Verification
- [x] Validate changes via Unity recompilation check.
- [x] Ensure HP Bars automatically follow characters (they should).
