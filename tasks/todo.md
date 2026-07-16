# Canvas Prioritization Fix Plan

- [x] Modify `MarionetteRoomEffectStrategy.cs` to search for and prioritize a canvas named "UICanvas"
- [x] Modify `BossRoomEffectStrategy.cs` to search for and prioritize a canvas named "UICanvas"
- [x] Save all modified scenes (if open) and run EditMode tests to verify compilation and test suite status

## Review & Verification
- [x] Ensure all modified strategy scripts compile cleanly.
- [x] All 403 EditMode tests pass (or 402 passing and transition test failure remains constant).
