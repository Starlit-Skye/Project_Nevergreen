# Tasks

- [x] Modify `BattleSystem.CheckBattleEnd` to include Cecilia defeat check <!-- id: 0 -->
- [x] Update `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` <!-- id: 1 -->
- [x] Verify compilation and logic via `BattleEndTests.cs` <!-- id: 2 -->
- [x] Update `BattleSystem.CheckBattleEnd` with early exit <!-- id: 3 -->
- [x] Call `CheckBattleEnd()` in `BattleSystem.HandleCharacterDefeated` <!-- id: 4 -->
- [x] Refactor `BattleSystem.ProcessTurn` with start-of-turn and post-DOT checks <!-- id: 5 -->
- [x] Add verification tests in `BattleEndTests.cs` <!-- id: 6 -->
## Review
The lose condition has been updated to trigger if Cecilia (CharacterId: "ceci") is defeated. This is handled as a primary defeat condition in `BattleSystem.CheckBattleEnd`, with a fallback to "all players dead" for robustness. The change was verified with a new unit test suite `BattleEndTests.cs` and the technical documentation was updated accordingly.
