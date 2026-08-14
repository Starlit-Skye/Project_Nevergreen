# Task List: Prevent Room Progression Increment on Healing Room Entry

- [x] Add `IsHealRoom` helper (checking `roomId == "RD_HealRoom"`) and `CurrentRoomData` property to `RunSessionManager.cs` <!-- id: 0 -->
- [x] Update `OnSceneLoaded` in `RunSessionManager.cs` to skip `RoomProgression++` for Heal Rooms <!-- id: 1 -->
- [x] Update `SpawnRoomChoiceButtons` in `CombatUI.cs` to handle post-Heal Room choices correctly <!-- id: 2 -->
- [x] Add unit tests in `RoomEffectTests.cs` for Heal Room progression exemption <!-- id: 3 -->
- [x] Update `SYSTEM_SPEC_ROOM_SELECTION.md` and `SYSTEM_SPEC_RUN_SESSION_MANAGER.md` <!-- id: 4 -->
- [x] Execute EditMode unit tests and verify correctness <!-- id: 5 -->
