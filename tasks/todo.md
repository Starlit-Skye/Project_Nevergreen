# Task List: Healing Room Effect Strategy & UI

- [ ] Create `HealRoomEffectStrategy.cs` to instantiate `MarionetteHealChoice.prefab` <!-- id: 0 -->
- [ ] Create `MarionetteHealChoiceController.cs` to bind UI buttons (`MarionetteButton1-4` & `HealAllButton`) <!-- id: 1 -->
- [ ] Implement single-target heal (999 HP, capped at Max HP) and group heal (exactly 25% of Max HP) math <!-- id: 2 -->
- [ ] Implement room completion & save triggers (`ShowRoomSelectionImmediately` & `SaveRun`) <!-- id: 3 -->
- [ ] Assign `HealRoomEffectStrategy` to `RD_HealRoom.asset` <!-- id: 4 -->
- [ ] Add unit tests in `RoomEffectTests.cs` and run full EditMode test suite <!-- id: 5 -->
