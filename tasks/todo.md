# Implementation Plan: Map Shuffle to Move Resistance

Full design: see `shuffle_resistance_mapping_plan.md` artifact.

## Decisions
- Map `StatusType.Shuffle` to `eff.moveResist` in `CombatCharacter.GetResistance()`.

## Code
- [x] Update `GetResistance` in `CombatCharacter.cs` to map `StatusType.Shuffle` to `eff.moveResist`.

## Tests
- [x] Add unit test in `BuffDebuffTests.cs` to verify `GetResistance(StatusType.Shuffle)` returns `eff.moveResist`.
- [x] Update existing `ShuffleTests.cs` to verify `StatusType.Shuffle` respects target `moveResist`.
- [x] Run EditMode tests and verify clean pass (548/548 passed).

## Review
- `GetResistance` in `CombatCharacter.cs` now maps `StatusType.Shuffle` to `eff.moveResist`.
- Updated test suite so `Shuffle_AppliedViaStatusEffect_RespectsMoveResistance` and `GetResistance_Shuffle_ReturnsMoveResist` both pass cleanly.


