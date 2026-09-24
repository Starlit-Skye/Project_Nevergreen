# Task List: Implement Shop Room Data Effect Strategy and Shop UI Controller

- [ ] Plan & Architectural Specs (Plan Phase)
  - [x] Incorporate strategy chance fields for common and uncommon trinkets (`commonTrinketWeight`, `uncommonTrinketWeight`)
  - [x] Prioritize `UICanvas` search in `ShopRoomEffectStrategy.ExecuteRoomEffect()`
  - [x] Use pre-existing `TrinketInventoryDropHandler` on shop item slots for displaying for-sale trinkets
  - [x] Remove economy currency text (`scrapsText`, `partsText`) handling from `ShopUIController` (handled by `EconomyDisplayUI`)
  - [x] Remove Buy buttons; implement **Drag-and-Drop Purchase Flow** (dragging from shop slot to equip slot)
  - [x] Ensure Scraps are deducted **IF AND ONLY IF** equipping/swapping succeeds and player has sufficient balance
  - [x] Track per-slot `isPurchased` state so swapping/moving trinkets back and forth after purchase does NOT re-deduct Scraps
  - [x] Strikethrough price text (`FontStyles.Strikethrough`) upon successful purchase to show the slot no longer costs money

- [x] Step 1: Create room effect strategy that also instantiates the UI (`ShopRoomEffectStrategy.cs`)
  - [x] Implement configuration fields (`commonTrinketWeight`, `uncommonTrinketWeight`, `minCostCommon`, `maxCostCommon`, `minCostUncommon`, `maxCostUncommon`, `shopUiPrefab`)
  - [x] Implement `ExecuteRoomEffect()` canvas search (prioritizing `UICanvas`), instantiation under canvas, `controller.Initialize(...)` call, and `ExecuteSilently()` fallback

- [x] Step 2: Create UI controller, and ONLY implement the function to display trinkets as well as price tag to shop (`ShopUIController.cs`)
  - [x] Implement `Initialize(...)` with two-stage tier rolling (common vs uncommon weight roll -> trinket selection & cost within bounds)
  - [x] Populate `trinketInventoryDropHandlers` slots with `TrinketUIItem` prefabs and set `priceTexts` labels

- [ ] Step 3: Add ONLY functionality to UI controller that manages `trinketEquipPanels`
  - [ ] Implement `RefreshEquipPanels()` and `SetupTrinketSlot()` to render marionette equipment slots and equipped trinkets for current party

- [ ] Step 4: Add purchasing flow and confirmation, including keeping track if a slot is purchased or not and applying strikethrough to price tag
  - [ ] Implement drag-and-drop purchase validation: check Scraps balance, execute equip/swap, deduct Scraps **if and only if** equip succeeds, set per-slot `isPurchased = true`, apply `FontStyles.Strikethrough` to `priceTexts`, and save run via `SaveManager.SaveRun()`
  - [ ] Ensure previously purchased slots allow moving/swapping trinkets without further Scraps deduction

- [ ] Step 5: Implement `OnLeaveShopClicked`
  - [ ] Implement `OnLeaveShopClicked()` button listener to save run, deactivate shop UI panel (`gameObject.SetActive(false)`), and trigger room completion via `CombatUI.ShowRoomSelectionImmediately()` / `RunSessionManager.CompleteRoom()`

- [ ] Verification & Automated Tests (`ShopRoomEffectTests.cs`)
  - [ ] Test strategy parameter passing and `UICanvas` prioritization
  - [ ] Test two-stage tier-weighted rolling and price text display
  - [ ] Test party equip panel populating
  - [ ] Test drag-and-drop purchase transaction, Scraps deduction on successful equip, no re-deduction for purchased slots, and price strikethrough styling
  - [ ] Test `OnLeaveShopClicked()` room completion flow
