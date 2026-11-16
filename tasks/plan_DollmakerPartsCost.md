# Plan: Dollmaker Parts Cost Not Deducted

## Problem
`DollmakerUIController._partCost` is stored in `Initialize()` but never used. Removing an Imperfection or replacing a Perfection never spends Parts, and Confirm buttons are enabled regardless of balance.

## Design Decisions
- Cost is charged **once per operation**, at the moment the trait change is committed (`TrySpendParts` is atomic, so there is no partial state).
  - Imperfection removal: in `OnConfirmClicked`.
  - Perfection replacement: in `OnConfirmReplacementClicked` (NOT when opening the replace panel, so "Select Another" and cancelling stay free).
- Affordability = `RunSessionManager.Parts >= _partCost`. A single helper `CanAfford()` is the one source of truth.
- Both Confirm buttons (`confirmButton`, `confirmReplacementButton`) are interactable only when an item is selected AND the player can afford the cost.
- Defensive guard: the click handlers still call `TrySpendParts` and abort on failure, in case the button state is stale.
- If the player cannot afford it, they can still press Skip (unchanged).
- Parts changes while the UI is open (`OnPartsChanged`) re-evaluate the buttons. The prefab already contains `EconomyDisplayUI`, so the balance is visible.

## Changes (all in `Assets/Scripts/UI/DollmakerUIController.cs` unless noted)
- [x] 1. Add `private bool CanAfford() => RunSessionManager.Parts >= _partCost;`
- [x] 2. Add `RefreshConfirmButtons()`:
  - `if (confirmButton != null) confirmButton.interactable = _selectedTrait != null && CanAfford();`
  - `if (confirmReplacementButton != null) confirmReplacementButton.interactable = _selectedReplacement != null && CanAfford();`
- [x] 3. Replace direct `confirmButton.interactable = ...` / `confirmReplacementButton.interactable = ...` calls with `RefreshConfirmButtons()`:
  - `Initialize`
  - `OnTraitItemClicked`
  - `ClearSelection`
  - `ShowReplacePanel`
  - `OnReplacementItemClicked`
  - `OnSelectAnotherClicked`
- [x] 4. `OnConfirmClicked`, Imperfection branch: `if (!RunSessionManager.TrySpendParts(_partCost)) return;` before `member.RemoveTrait(...)` (and `if (!CanAfford()) return;` at top of `OnConfirmClicked`).
- [x] 5. `OnConfirmReplacementClicked`: `if (!RunSessionManager.TrySpendParts(_partCost)) return;` after the null/index guard and before `RemoveTrait` / `TryAddTrait`.
- [x] 6. Subscribe to `RunSessionManager.OnPartsChanged` in `OnEnable` and unsubscribe in `OnDisable` / `OnDestroy` to call `RefreshConfirmButtons()`.
- [x] 7. Data check: `RD_Dollmaker.asset` has `partCost: 1` serialized, overriding the C# default of 15. Kept untouched unless instructed by designer.

## Tests (`Assets/Editor/Tests/DollmakerUIControllerTests.cs`)
Set `RunSessionManager.Parts` explicitly in each test. `SetUp` already calls `RunSessionManager.Initialize()`.
- [x] Update existing `ConfirmImperfection_RemovesTrait_SpendsParts_AndCallsCompleteRoom` and `ConfirmReplacement_RemovesOldPerfection_AddsNewPerfection_AndSpendsParts` to grant enough Parts first and assert Parts deduction.
- [x] Update `TraitSelection_AppliesStrikethrough_AndTogglesConfirmButton`, `ConfirmPerfection_ShowsReplacePanel_AndPopulatesOptions`, and `ReplacementSelection_AppliesUnderlineHighlight` to set sufficient Parts.
- [x] New: imperfection removal deducts exactly `partCost` (Parts before and after).
- [x] New: perfection replacement deducts exactly `partCost`.
- [x] New: with `Parts < partCost`, selecting a trait leaves `confirmButton.interactable == false`.
- [x] New: with `Parts < partCost`, selecting a replacement leaves `confirmReplacementButton.interactable == false`.
- [x] New: with `Parts < partCost`, invoking the confirm handler directly does not remove the trait and does not change Parts (guard path).
- [x] New: with `Parts == partCost`, the operation succeeds and Parts ends at 0 (boundary).
- [x] New: opening the replace panel and pressing "Select Another" does not spend Parts.

## Verification
- [x] Run EditMode tests for `DollmakerUIControllerTests` (14/14 passed) and `EconomySystemTests` (8/8 passed) via `tests-run`.
- [x] Verify interactability logic when `Parts < partCost` vs `Parts >= partCost`.
- [x] Verify Skip button remains interactable and costs 0 Parts.

## Review
- **Files Modified**:
  - `Assets/Scripts/UI/DollmakerUIController.cs`: Added `CanAfford()` and `RefreshConfirmButtons()`, wired `RunSessionManager.OnPartsChanged` subscription in `OnEnable`/`OnDisable`/`OnDestroy`, routed all confirm button interactability updates through `RefreshConfirmButtons()`, and added `RunSessionManager.TrySpendParts(_partCost)` checks in `OnConfirmClicked` and `OnConfirmReplacementClicked`.
  - `Assets/Editor/Tests/DollmakerUIControllerTests.cs`: Updated existing tests to configure `RunSessionManager.Parts` and added 5 new unit tests covering insufficient Parts button state, reactive `OnPartsChanged` updates, handler guard clauses, exact balance boundary, and replacement cancellation.
- **Test Results**: All 14 `DollmakerUIControllerTests` and all 8 `EconomySystemTests` passed in EditMode.
