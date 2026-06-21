# Plan: Swap Rank Formation in Party Management Panel

## Tasks
- [x] 1. Update `PartyManagementPanelController.cs` to add move feature fields (`moveButton`, `highlightColor`, states) and dynamic fallback binding in `Start()`.
- [x] 2. Refactor `InitializeSlots()` to extract `RefreshSlotUI()` to update buttons and names.
- [x] 3. Implement move highlighting and cancel logic in `PartyManagementPanelController.cs`.
- [x] 4. Implement slot swapping, rank persistence update in `RunSessionManager.CurrentParty`, and save game trigger (`SaveManager.SaveRun()`).
- [x] 5. Add unit tests for the rank swapping feature in `PartyManagementPanelTests.cs`.
- [x] 6. Run all tests to verify correct implementation.
