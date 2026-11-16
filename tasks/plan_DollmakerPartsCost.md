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
- [ ] 1. Add `private bool CanAfford() => RunSessionManager.Parts >= _partCost;`
- [ ] 2. Add `RefreshConfirmButtons()`:
  - `confirmButton.interactable = _selectedTrait != null && CanAfford();`
  - `confirmReplacementButton.interactable = _selectedReplacement != null && CanAfford();`
- [ ] 3. Replace the direct `confirmButton.interactable = true/false` assignments with `RefreshConfirmButtons()`:
  - `Initialize` (L99)
  - `OnTraitItemClicked` (L185)
  - `ClearSelection` (L197)
  - `ShowReplacePanel` (L249)
  - `OnReplacementItemClicked` (L367)
  - `OnSelectAnotherClicked` (L391)
- [ ] 4. `OnConfirmClicked`, Imperfection branch: `if (!RunSessionManager.TrySpendParts(_partCost)) return;` before `member.RemoveTrait(...)`.
- [ ] 5. `OnConfirmReplacementClicked`: `if (!RunSessionManager.TrySpendParts(_partCost)) return;` after the null/index guard and before `RemoveTrait` / `TryAddTrait`.
  - Edge case: `TryAddTrait` can return false (capacity or opposite conflict). `ShowReplacePanel` already filters candidates, so it should not fail. Verify during implementation. If it can fail, check `TryAddTrait` first or refund via `GrantParts`.
- [ ] 6. (Optional) Subscribe to `RunSessionManager.OnPartsChanged` in `OnEnable` and unsubscribe in `OnDisable` to call `RefreshConfirmButtons()`.
- [ ] 7. Check `RD_Dollmaker.asset`: `partCost: 1` is the serialized value and overrides the C# default of 15. Confirm with the designer whether 1 is intended. This is a data question, so no change is made without confirmation.

## Tests (`Assets/Editor/Tests/DollmakerUIControllerTests.cs`)
Set `RunSessionManager.Parts` explicitly in each test. `SetUp` already calls `RunSessionManager.Initialize()`.
- [ ] Update existing `ConfirmImperfection_RemovesTrait_AndCallsCompleteRoom` and `ConfirmReplacement_RemovesOldPerfection_AndAddsNewPerfection` to grant enough Parts first. Otherwise they fail under the new rules.
- [ ] Update `TraitSelection_AppliesStrikethrough_AndTogglesConfirmButton` the same way, since confirm now requires Parts.
- [ ] New: imperfection removal deducts exactly `partCost` (Parts before and after).
- [ ] New: perfection replacement deducts exactly `partCost`.
- [ ] New: with `Parts < partCost`, selecting a trait leaves `confirmButton.interactable == false`.
- [ ] New: with `Parts < partCost`, selecting a replacement leaves `confirmReplacementButton.interactable == false`.
- [ ] New: with `Parts < partCost`, invoking the confirm handler anyway does not remove the trait and does not change Parts (guard path).
- [ ] New: with `Parts == partCost`, the operation succeeds and Parts ends at 0 (boundary).
- [ ] New: opening the replace panel and pressing "Select Another" does not spend Parts.

## Docs
- [ ] No Dollmaker spec exists (`git grep doll Docs/` returned nothing). Consider adding a short `SYSTEM_SPEC_DOLLMAKER_ROOM.md` covering the cost rules, or noting the Dollmaker as a `TrySpendParts` consumer in `SYSTEM_SPEC_ECONOMY_RUNTIME.md`.

## Verification
- [ ] Run EditMode tests for `DollmakerUIControllerTests` and `EconomySystemTests` via `tests-run`, and check the console for errors.
- [ ] Playtest, with Parts below cost: Confirm is greyed out for both removal and replacement.
- [ ] Playtest, with Parts at or above cost: Confirm works, Parts drops by the cost, and `EconomyDisplayUI` updates.
- [ ] Playtest: Skip is always available and costs nothing.

## Risks
- Existing tests silently depend on free operations, so they need the Parts setup above.
- `partCost: 1` in the asset versus the default of 15 may hide the real balance impact during playtesting.

