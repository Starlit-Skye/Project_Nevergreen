# Animation Runtime Implementation

## Plan
Based on `Docs/specs/systems/SYSTEM_SPEC_ANIMATION_RUNTIME.md`

### Phase 1: Data Model
- [x] Create `AnimationQueueEntry.cs` — queue entry struct + queue state struct

### Phase 2: Core Processor
- [x] Create `AnimationQueueProcessor.cs` — FIFO queue, input lock events, safeguards

### Phase 3: Integration
- [x] Modify `BattleSystem.cs` — enqueue skill animations, wait for queue before advancing
- [x] Modify `CombatUI.cs` — subscribe to input lock events, gate buttons
- [x] Modify `HPBar.cs` — enqueue HP change animation into shared queue

### Phase 4: Verification
- [x] Compile check via Unity — PASSED (no errors)
- [x] Review all changes for correctness

### Phase 5: Animator Integration
- [x] Refactor AnimationQueueProcessor to use IAnimationStep interface
- [x] Wire up Attack and Cast AnimatorStep for Combat Skills
- [x] Wire up TakeDamage AnimatorStep for HPBars
