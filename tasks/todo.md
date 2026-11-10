# Task: Stat System Clamping Refactor

## Summary
Refactor the combat stat system so that stats cannot be reduced below 0. Specifically:
- `attack`, `accuracy`, `critChance`, `defense`, `dodge`, `speed`, `bleedResist`, `blightResist`, `stunResist`, `debuffResist`, and `moveResist` are clamped at a minimum of 0.
- `maxHP` is clamped at a minimum of 1.
- In `StatBlockData.cs`, add `[Min(0)]` attribute to `attack`, `maxHP`, and `speed` to prevent entering negative numbers in base assets.
- If effective `maxHP` is reduced below `currentHP`, re-clamp `currentHP` to the new effective `maxHP`.
- Align spec formulas with the implemented codebase formula: `(base + flat) * (1 + percent)`.
- Update design specs and write comprehensive unit tests.

## Todo List

### 1. Base Data Hardening (`StatBlockData.cs`)
- [x] Add `[Min(0)]` attribute to `attack`, `maxHP`, and `speed` in `StatBlockData.cs`.

### 2. Stat Clamping in Runtime Math (`CombatCharacter.cs`)
- [x] In `GetModifiedStatValue`, clamp derived stat value at 0, or at 1 if `target == StatTarget.MaxHP`.
- [x] Add `ClampCurrentHPToEffectiveMax()` and invoke it on stat change (`AddStatus`, `RemoveStatus`, `TriggerStatsChanged`).
- [x] Update `ApplyHeal` to calculate missing HP using effective `maxHP` (`GetEffectiveStats().maxHP`) instead of `baseStats.maxHP`.

### 3. UI Alignment (`HPBar.cs`)
- [x] In `HPBar.cs`, update `UpdateStateConfig` to use `GetEffectiveStats().maxHP` instead of `baseStats.maxHP`.
- [x] In `HandleStatsChanged`, call `UpdateStateConfig()` before `Refresh()`.

### 4. Unit Tests (`BuffDebuffTests.cs`)
- [x] Test: Oversized debuffs on core percent stats (Attack, Speed, Defense, Accuracy, Dodge) clamp at 0.
- [x] Test: Oversized debuff on MaxHP clamps at 1.
- [x] Test: Oversized flat debuffs on flat stats (CritChance, 5 resistances) clamp at 0.
- [x] Test: Mixed buffs and debuffs evaluate additively and clamp at boundary (e.g. +50% and -150% -> 0).
- [x] Test: Debuff expiration properly restores stat back to base value.
- [x] Test: MaxHP debuff re-clamps `currentHP` to new effective `maxHP`.
- [x] Test: Pile innate `moveResist` bonus (+300) applies on top of clamped base.
- [x] Run full test suite to guarantee 0 regressions.

### 5. Documentation & Specification Updates
- [x] `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_BUFF_DEBUFF.md`:
  - Update formula to `(base + flat) * (1 + percent)` and add floor clamping rules (`max(0, ...)` / `max(1, maxHP)`).
  - Update Failure Modes ("Stat Underflow" resolved by clamping).
  - Add tuning variables for stat floor.
  - Update acceptance tests and last verified date.
- [x] `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md`:
  - Update effective stat formula section with floor clamping.
  - Add edge case notes for 0-clamping and maxHP 1-floor.
- [x] `Docs/specs/systems/SYSTEM_SPEC_MARIONETTE_TEMPLATE.md`:
  - Add invariant notes regarding base stats >= 0 and runtime clamping.
- [x] `Docs/specs/systems/SYSTEM_SPEC_ENEMY_TEMPLATE.md`:
  - Add invariant notes regarding base stats >= 0 and runtime clamping.
- [x] `Docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md`:
  - Add notes regarding stat constraints and runtime flooring.

### 6. Review & Verification
- [x] Run all EditMode unit tests.
- [x] Verify test results and document them in `tasks/todo.md`.

## Review & Verification Results
- **Unit Tests**:
  - Updated `Debuff_Resistance_CanGoBelowZero` -> `Debuff_Resistance_ClampedAtZero` to verify that resistances clamp at 0 instead of allowing negative values.
  - Added 8 new unit tests in `BuffDebuffTests.cs`:
    - `StatClamping_CorePercentStats_ClampedAtZero`: Oversized debuffs on Attack, Speed, Defense, Accuracy, Dodge clamp at 0.
    - `StatClamping_MaxHP_PercentDebuff_ClampedAtOne`: Oversized percent debuffs on MaxHP clamp at 1.
    - `StatClamping_MaxHP_FlatDebuff_ClampedAtOne`: Oversized flat debuffs on MaxHP clamp at 1.
    - `StatClamping_FlatStats_ClampedAtZero`: Oversized flat debuffs on CritChance, BleedResist, BlightResist, StunResist, DebuffResist, MoveResist clamp at 0.
    - `StatClamping_MixedBuffAndDebuff_EvaluatesAdditivelyAndClamps`: Mixed buffs and debuffs calculate additively and clamp at boundary.
    - `StatClamping_DebuffExpiration_RestoresBaseValue`: Expiration cleanly restores base stats.
    - `StatClamping_MaxHPDebuff_ReclampsCurrentHP`: `currentHP` is automatically re-clamped when effective `maxHP` drops, and healing cannot exceed the new effective max HP.
    - `StatClamping_PileMoveResistBonus_AppliedOnTopOfClampedBase`: Pile innate move resistance (+300) applies on top of clamped base.
- **Suite Execution**:
  - Full EditMode test run: 615 executed, 615 passed, 0 failed, 0 skipped.
- **Files Modified**:
  - `Assets/Scripts/Data/StatBlockData.cs` (`[Min(0)]` added to `attack`, `maxHP`, `speed`)
  - `Assets/Scripts/Combat/CombatCharacter.cs` (clamped stats at 0 or 1 for maxHP in `GetModifiedStatValue`, added `ClampCurrentHPToEffectiveMax`, updated `ApplyHeal` to use effective `maxHP`)
  - `Assets/Scripts/Prototype/HPBar.cs` (updated `_currentMaxHP` to use effective `maxHP` and refresh on stats changed)
  - `Assets/Editor/Tests/BuffDebuffTests.cs` (updated legacy test and added new clamping test suite)
  - `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_BUFF_DEBUFF.md`
  - `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md`
  - `Docs/specs/systems/SYSTEM_SPEC_MARIONETTE_TEMPLATE.md`
  - `Docs/specs/systems/SYSTEM_SPEC_ENEMY_TEMPLATE.md`
  - `Docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md`

---

# Task: Ignore Stun Applied To Already-Stunned Character

## Summary
Currently a successful Stun on an already-stunned target appends a second `StatusEffectInstance`
(no refresh, concurrent ticking, stacked +300 recovery buffs). New behavior: if the target already has
a non-expired Stun, the new application is **completely ignored**: no new instance, no duration
refresh/extension, no extra recovery buff, no `OnStatusApplied` event, no "afflicted" log.
PLAN ONLY - no code or spec has been modified yet.

## Design Decisions (proposed)
- **Central guard in `CombatCharacter.AddStatus`** (single choke point; all effect paths funnel through it:
  `StatusEffect`, `SelfStatusEffect`, `AdjacentAllyStatusEffect`, `ApplyStatusToGuardianEffect`,
  `StatusEffectOnSpawn`, `LivingFragmentsStatusEffect`). Prevents future callers from re-introducing stacking.
  Change signature `void` -> `bool` (true = added) so callers can react; existing call sites compile unchanged.
- **Early-out in `StatusEffect.Execute` (before the RNG roll)** so an ignored Stun does not consume an RNG value,
  and does not fire `TriggerStatusApplied` / log "afflicted". Same pre-check for the other effect classes that
  can apply Stun (Self/AdjacentAlly/Guardian) via a shared helper.
- **"Already stunned" predicate**: new `CombatCharacter.HasActiveStatus(StatusType)` =
  `statusEffects.Any(s => s.type == type && !s.IsExpired)`. Do not rely on the `isStunned` flag (can be stale,
  e.g. duration-0 stun).
- **Scope**: Stun only (type-specific, e.g. a `StatusType`-based "non-stackable" set so other types like Bleed
  keep stacking). Guard has its own Last-In-Wins logic and is untouched.
- **Existing instance wins**: remaining duration of the existing stun is unchanged (longer incoming stun does NOT extend it).

## Todo List

### 1. Pre-flight
- [x] Re-read `tasks/lessons.md` for relevant patterns.
- [x] Confirm with user: user approved proceeding with plan.
- [x] Grep tests that add 2 Stun instances to the same character (`BuffDebuffTests` ~L614/652, `GuardTests`
      ~L88/207, `CumulativeDamageTests` ~L233 duration-0 stun) and confirm none rely on stacking.

### 2. Runtime (`CombatCharacter.cs`)
- [x] Add `HasActiveStatus(StatusType type)` helper.
- [x] Add non-stackable guard at top of `AddStatus`: if `status.type == StatusType.Stun && HasActiveStatus(Stun)`
      -> return false without `OnAdded`, flag change, clamp or `OnStatsChanged`.
- [x] Make `AddStatus` return `bool`.

### 3. Effect call sites
- [x] `StatusEffect.Execute`: after hit check, early return if `statusType == Stun` and target already has active Stun
      (before chance/resistance/RNG).
- [x] Apply same early-out in `SelfStatusEffect`, `AdjacentAllyStatusEffect`, `ApplyStatusToGuardianEffect`.
- [x] Ensure "afflicted" log and `TriggerStatusApplied(..., applied)` use the AddStatus return value.

### 4. Tests (`StunTests.cs`)
- [x] `Stun_AppliedToAlreadyStunned_DirectAddStatus_ReturnsFalseAndCreatesNoInstance`: count of Stun instances stays 1.
- [x] `Stun_AppliedToAlreadyStunned_ViaSkillEffect_IsCompletelyIgnored`: no OnStatusApplied event and no RNG consumed.
- [x] `Stun_AppliedToAlreadyStunned_DoesNotExtendDuration`: existing 1-turn stun + incoming 3-turn -> still 1 remaining.
- [x] `Stun_AppliedToAlreadyStunned_YieldsSingleRecoveryBuffOnExpiry`: after expiry exactly one `Buff(StunResist,+300,1)`.
- [x] `Stun_AppliedAfterPreviousExpired_AppliesNormally`: re-stun after recovery still works.
- [x] `NonStunStatus_LikeBleed_StillStacks`: Bleed still stacks.
- [x] Run full EditMode suite (baseline: 615 passed; now 621 passed).

### 5. Docs
- [x] `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_STUN.md`: updated State Model, Formulas, Edge Cases, Event Hooks, Acceptance Tests; removed stacking items from Missing Evidence.
- [x] `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md`: added note under stun edge cases.
- [x] `MECHANIC_SPEC_STATUS_GUARD.md`: verified Guard breakage on first Stun unchanged.

### 6. Verification
- [x] All new + existing tests green (621/621 passed).
- [x] Verified zero console compilation errors.

## Review & Verification Results
- **Unit Tests**:
  - Added 6 new unit tests in `Assets/Editor/Tests/StunTests.cs`:
    - `Stun_AppliedToAlreadyStunned_DirectAddStatus_ReturnsFalseAndCreatesNoInstance`
    - `Stun_AppliedToAlreadyStunned_ViaSkillEffect_IsCompletelyIgnored`
    - `Stun_AppliedToAlreadyStunned_DoesNotExtendDuration`
    - `Stun_AppliedToAlreadyStunned_YieldsSingleRecoveryBuffOnExpiry`
    - `Stun_AppliedAfterPreviousExpired_AppliesNormally`
    - `NonStunStatus_LikeBleed_StillStacks`
  - Full suite EditMode tests: **621 passed, 0 failed, 0 skipped**.
- **Files Modified**:
  - `Assets/Scripts/Combat/CombatCharacter.cs` (`HasActiveStatus(StatusType)`, `AddStatus` returns `bool` and blocks duplicate Stun, `RemoveStatus` uses `HasActiveStatus`)
  - `Assets/Scripts/Combat/StatusProcessor.cs` (`HasActiveStatus(StatusType.Stun)` used in `TickDurations`)
  - `Assets/Scripts/Combat/Effects/StatusEffect.cs` (early return before resistance/RNG check if target already has active Stun)
  - `Assets/Scripts/Combat/Effects/SelfStatusEffect.cs` (early return if user already has active Stun)
  - `Assets/Scripts/Combat/Effects/AdjacentAllyStatusEffect.cs` (early return if target ally already has active Stun)
  - `Assets/Scripts/Combat/Effects/ApplyStatusToGuardianEffect.cs` (early return if guardian already has active Stun)
  - `Assets/Editor/Tests/StunTests.cs` (6 new unit tests)
  - `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_STUN.md` (updated spec)
  - `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` (updated Combat Core spec)

---

# Task: GenericEventRoomEffectStrategy Multi-Prefab Selection

## Summary
Modify `GenericEventRoomEffectStrategy.cs` to accept a list of UI prefabs (`eventUiPrefabs`) and randomly select one when activated, keeping the implementation simple and clean.

## Todo List

### 1. Refactor `GenericEventRoomEffectStrategy.cs`
- [x] Replace single `eventUiPrefab` with `public List<GameObject> eventUiPrefabs = new List<GameObject>();`
- [x] Implement simple `GetRandomPrefab()` helper method (`UnityEngine.Random.Range(0, eventUiPrefabs.Count)`).
- [x] Update `ExecuteRoomEffect()` to instantiate the randomly chosen prefab.
- [x] Update `RD_TheatreRoom.asset` and `RD_EventRoom.asset` to serialize `eventUiPrefabs`.

### 2. Unit Testing
- [x] Create `GenericEventRoomEffectStrategyTests.cs` in `Assets/Editor/Tests/` verifying:
  - Returns null if list is empty or null.
  - Chooses single prefab if list has 1 item.
  - Chooses among prefabs when multiple are provided.
  - Executes instantiation onto Canvas if valid Canvas exists.
- [x] Run all EditMode tests using `tests-run` MCP tool to verify 0 regressions.

### 3. Review & Verification
- [x] Verify Unity compilation and logs.
- [x] Document results in `tasks/todo.md`.
- [x] Record Lesson 21 in `tasks/lessons.md` to avoid over-engineering migration shims.

## Review & Verification Results
- **Unit Tests**:
  - `Assets/Editor/Tests/GenericEventRoomEffectStrategyTests.cs` (5 tests):
    - `GetRandomPrefab_WhenPrefabsListIsEmptyOrNull_ReturnsNull`
    - `GetRandomPrefab_WhenPrefabsListHasSingleItem_ReturnsThatItem`
    - `GetRandomPrefab_WhenPrefabsListHasMultipleItems_ReturnsOneOfItems`
    - `ExecuteRoomEffect_WhenNoPrefabsAssigned_LogsErrorAndDoesNotThrow`
    - `ExecuteRoomEffect_WhenValidPrefabAndCanvasExist_InstantiatesUnderCanvas`
  - Full suite EditMode test run: **652 passed, 0 failed, 0 skipped**.
- **Files Modified**:
  - `Assets/Scripts/Data/GenericEventRoomEffectStrategy.cs` (simplified to clean list + random index helper)
  - `Assets/Data/RoomData/RD_TheatreRoom.asset` (updated field to `eventUiPrefabs`)
  - `Assets/Data/RoomData/RD_EventRoom.asset` (updated field to `eventUiPrefabs`)
  - `Assets/Editor/Tests/GenericEventRoomEffectStrategyTests.cs` (unit test suite)

---

# Task: Implement SafeRoomController

## Summary
Create a new MonoBehaviour script `SafeRoomController.cs` in `Assets/Scripts/UI/` based on `MarionetteHealChoiceController.cs`.
- Removes `healAllButton` and `hpTexts`.
- Heals the selected Marionette by a configurable percentage of her max HP (`healPercent`).
- Everything else (party slot buttons, displayName text setting, room completion via CombatUI) behaves identically.
- Add comprehensive EditMode unit tests in `Assets/Editor/Tests/SafeRoomControllerTests.cs`.

## Todo List

### 1. Script Implementation (`SafeRoomController.cs`)
- [x] Create `Assets/Scripts/UI/SafeRoomController.cs` in namespace `Nevergreen.UI`.
- [x] Add `marionetteButtons` array and configurable `healPercent` field.
- [x] Implement `Initialize(List<PartyMemberInfo> party)` to setup buttons with display names and click listeners.
- [x] Implement `OnMarionetteClicked(PartyMemberInfo member)` to calculate percentage healing, cap at maxHP, set `currentHP = null` if full, and complete room.
- [x] Implement `CompleteRoom()` deactivating gameObject and notifying `CombatUI`.

### 2. Unit Testing (`SafeRoomControllerTests.cs`)
- [x] Create `Assets/Editor/Tests/SafeRoomControllerTests.cs`.
- [x] Test: Selected Marionette heals by configured percentage (e.g. 25% or 50%).
- [x] Test: Heal configurability (setting a different percentage heals accordingly).
- [x] Test: Overheal clamps to maxHP and sets `currentHP` to `null`.
- [x] Test: Already full HP (`currentHP == null`) remains `null`.
- [x] Test: Room completion deactivates UI and calls `CombatUI.ShowRoomSelectionImmediately()`.
- [x] Test: Buttons for empty or missing party slots are deactivated.
- [x] Run all EditMode tests via `tests-run` MCP tool.

### 3. Review & Verification
- [x] Verify Unity console logs for zero compilation errors or warnings.
- [x] Verify all tests pass with zero regressions.
- [x] Update `tasks/todo.md` with verification results.

## Review & Verification Results
- **Unit Tests**:
  - `Assets/Editor/Tests/SafeRoomControllerTests.cs` (7 tests):
    - `SafeRoom_SingleMarionetteSelected_HealsConfiguredPercentOfMaxHP`
    - `SafeRoom_HealPercentIsConfigurable`
    - `SafeRoom_OverhealCapsAtMaxHP_AndSetsCurrentHPToNull`
    - `SafeRoom_AlreadyFullHP_RemainsNull`
    - `SafeRoom_CompletesRoom_DeactivatesGameObjectAndNotifiesCombatUI`
    - `SafeRoom_MissingOrEmptyPartySlots_DeactivatesButtonsAndUpdatesLabels`
    - `SafeRoom_NullPartyMemberOrNullCharacter_DeactivatesButton`
  - Full suite EditMode test run: **659 passed, 0 failed, 0 skipped**.
- **Files Created**:
  - `Assets/Scripts/UI/SafeRoomController.cs` (controller implementing single marionette healing by percentage, no heal-all button, no hpTexts)
  - `Assets/Editor/Tests/SafeRoomControllerTests.cs` (unit test suite)
- **Unmodified References**:
  - `Assets/Scripts/UI/MarionetteHealChoiceController.cs` left completely intact.

---

# Task: Implement WritingMonsterRoomButtons

## Summary
Create a new MonoBehaviour script `WritingMonsterRoomButtons.cs` in `Assets/Scripts/UI/` with 3 public methods for button OnClick assignment:
1. `HelpMonster()`: reduces exhaustion by configurable `exhaustionReduction` (default 1), clamped at 0.
2. `AttackMonster()`: grants configurable `partsReward` (default 1) Parts to player team.
3. `Close()`: performs `CompleteRoom()` flow deactivating UI and triggering `CombatUI.ShowRoomSelectionImmediately()`.
- Implements one-choice rule: `_hasChosen` flag ensures only the first choice (Help or Attack) is applied during a visit. `Close()` is always available.
- Unit tests in `Assets/Editor/Tests/WritingMonsterRoomButtonsTests.cs`.

## Todo List

### 1. Script Implementation (`WritingMonsterRoomButtons.cs`)
- [x] Create `Assets/Scripts/UI/WritingMonsterRoomButtons.cs` in namespace `Nevergreen.UI`.
- [x] Add serialized configurable fields `exhaustionReduction` and `partsReward` (both defaulting to 1).
- [x] Add `_hasChosen` flag for one-choice-per-visit enforcement.
- [x] Implement `HelpMonster()` to reduce `ExhaustionRound` (clamped at 0).
- [x] Implement `AttackMonster()` to grant `partsReward` via `RunSessionManager.GrantParts()`.
- [x] Implement `Close()` to deactivate GameObject and notify `CombatUI`.

### 2. Unit Testing (`WritingMonsterRoomButtonsTests.cs`)
- [x] Create `Assets/Editor/Tests/WritingMonsterRoomButtonsTests.cs`.
- [x] Test: `HelpMonster` reduces `ExhaustionRound` by configured amount.
- [x] Test: `HelpMonster` clamps at 0.
- [x] Test: `HelpMonster` triggers `OnExhaustionChanged` when value changes.
- [x] Test: `HelpMonster` works as normal when `ExhaustionLimit <= 0`.
- [x] Test: `AttackMonster` grants configured Parts and triggers `OnPartsChanged`.
- [x] Test: Field defaults are 1.
- [x] Test: One-choice rule blocks subsequent Help or Attack calls.
- [x] Test: `Close` deactivates GameObject and notifies `CombatUI`.
- [x] Test: `Close` logs error when `CombatUI` is not found.
- [x] Run all EditMode tests via `tests-run` MCP tool.

### 3. Review & Verification
- [x] Verify Unity console logs for zero compilation errors or warnings.
- [x] Verify all tests pass with zero regressions.
- [x] Update `tasks/todo.md` with verification results.

## Review & Verification Results
- **Unit Tests**:
  - `Assets/Editor/Tests/WritingMonsterRoomButtonsTests.cs` (13 tests):
    - `WritingMonsterRoomButtons_Defaults_AreSetToOne`
    - `HelpMonster_ReducesExhaustionRound_ByConfiguredAmount`
    - `HelpMonster_ClampsAtZero_WhenReductionExceedsCurrent`
    - `HelpMonster_FiresOnExhaustionChanged_OnlyWhenValueChanged`
    - `HelpMonster_WorksNormally_WhenExhaustionLimitIsZero`
    - `AttackMonster_GrantsParts_AndFiresOnPartsChanged`
    - `AttackMonster_ZeroReward_DoesNotChangeParts`
    - `OneChoiceRule_HelpThenAttack_BlocksAttack`
    - `OneChoiceRule_AttackThenHelp_BlocksHelp`
    - `OneChoiceRule_RepeatedCalls_OnlyApplyOnce`
    - `Close_DeactivatesGameObject_AndNotifiesCombatUI`
    - `Close_WhenCombatUIMissing_LogsErrorAndStillDeactivates`
    - `Close_WorksRegardlessOfHasChosen`
  - Full suite EditMode test run: **672 passed, 0 failed, 0 skipped**.
- **Files Created**:
  - `Assets/Scripts/UI/WritingMonsterRoomButtons.cs`
  - `Assets/Editor/Tests/WritingMonsterRoomButtonsTests.cs`
- **Zero Modifications to Existing Code**:
  - `RunSessionManager.cs`, `MarionetteHealChoiceController.cs`, and `CombatUI.cs` left intact.

---

# Task: Implement ArmamentBuilderRoomController

## Summary
Create `ArmamentBuilderRoomController.cs` in `Assets/Scripts/UI/` based on `TreasureUIController.cs`.
- Takes `recvEquipmentButton`, `leaveButton1`, `leaveButton2`, `rewardPanel`, `rewardText`.
- Exposes `commonTrinketWeight` (default 70) and `uncommonTrinketWeight` (default 30) for Common/Uncommon drop chance.
- Exposes `scrapsCost` (default 20) to set Scraps cost.
- Implements `Initialize(List<PartyMemberInfo> party = null)` following the Treasure flow (only trinket reward).
- Updates `recvEquipmentButton` text to `"Spend [ScrapCost] Scraps, receive a random equipment."`.
- When clicking `recvEquipmentButton`, rolls a random trinket, spends Scraps, and displays reward in `rewardPanel`.
- When clicking either Leave button, completes room via the `WritingMonsterRoomButtons` flow.
- Implements `ITrinketEquipContainerUI` to support drag-and-drop trinket equipping.
- Unit tests in `Assets/Editor/Tests/ArmamentBuilderRoomControllerTests.cs`.

## Todo List

### 1. Script Implementation (`ArmamentBuilderRoomController.cs`)
- [x] Create `Assets/Scripts/UI/ArmamentBuilderRoomController.cs` implementing `ITrinketEquipContainerUI`.
- [x] Add serialized UI fields: `recvEquipmentButton`, `leaveButton1`, `leaveButton2`, `rewardPanel`, `rewardText`, `trinketUIItemPrefab`, `testTrinketDroppedContainer`, `trinketEquipPanels`.
- [x] Add configurable fields: `commonTrinketWeight` (70), `uncommonTrinketWeight` (30), `scrapsCost` (20).
- [x] Implement `OnEnable` / `OnDisable` listener wiring.
- [x] Implement `Initialize(List<PartyMemberInfo> party = null)` setting up UI state and button text.
- [x] Implement `OnRecvEquipmentClicked()` handling roll, spend, and reward panel activation.
- [x] Implement `RollTrinketReward()` matching weighted two-stage selection.
- [x] Implement `OnLeaveClicked()` / `CompleteRoom()` flow.
- [x] Implement `ITrinketEquipContainerUI` members (`RefreshTrinketUI`, `OnUnassignedTrinketChanged`, `RefreshPanels`, `SetupTrinketSlot`).

### 2. Unit Testing (`ArmamentBuilderRoomControllerTests.cs`)
- [x] Create `Assets/Editor/Tests/ArmamentBuilderRoomControllerTests.cs`.
- [x] Test: `Initialize` sets button text with configured `scrapsCost`.
- [x] Test: `Initialize` hides reward panel and activates receive button.
- [x] Test: Click receive with sufficient Scraps spends Scraps, reveals reward panel, hides receive button, displays trinket.
- [x] Test: Click receive with insufficient Scraps does nothing.
- [x] Test: Roll respects weights (pure Common vs pure Uncommon).
- [x] Test: Roll with 0 total weight or empty pool does not spend Scraps and logs warning.
- [x] Test: Receive cannot be triggered multiple times.
- [x] Test: `leaveButton1` and `leaveButton2` both complete room.
- [x] Test: Leave flow without CombatUI logs error and still deactivates.
- [x] Test: Listeners properly unregistered on `OnDisable`.
- [x] Test: `OnUnassignedTrinketChanged` updates internal rolled trinket.
- [x] Run all EditMode tests via `tests-run` MCP tool.

### 3. Review & Verification
- [x] Verify Unity console logs for zero compilation errors or warnings.
- [x] Verify all tests pass with zero regressions.
- [x] Update `tasks/todo.md` with verification results.

## Review & Verification Results
- **Unit Tests**:
  - `Assets/Editor/Tests/ArmamentBuilderRoomControllerTests.cs` (12 tests):
    - `Initialize_SetsVisualStateAndCostText`
    - `OnRecvEquipmentClicked_WithSufficientScraps_SpendsScrapsAndShowsReward`
    - `OnRecvEquipmentClicked_WithInsufficientScraps_DoesNotSpendOrShowReward`
    - `OnRecvEquipmentClicked_OnlyExecutesOnce`
    - `RollTrinketReward_WeightsPureCommon_ReturnsCommon`
    - `RollTrinketReward_WeightsPureUncommon_ReturnsUncommon`
    - `OnRecvEquipmentClicked_WhenNoTrinketAvailable_DoesNotSpendScraps`
    - `LeaveButton1_DeactivatesGameObjectAndNotifiesCombatUI`
    - `LeaveButton2_DeactivatesGameObjectAndNotifiesCombatUI`
    - `LeaveButton_WhenCombatUIMissing_LogsErrorAndStillDeactivates`
    - `OnDisable_RemovesListeners`
    - `OnUnassignedTrinketChanged_UpdatesRolledTrinket`
  - Full suite EditMode test run: **684 passed, 0 failed, 0 skipped**.
- **Files Created**:
  - `Assets/Scripts/UI/ArmamentBuilderRoomController.cs`
  - `Assets/Editor/Tests/ArmamentBuilderRoomControllerTests.cs`
- **Zero Modifications to Existing Code**:
  - `TreasureUIController.cs`, `TreasureRoomEffectStrategy.cs`, `WritingMonsterRoomButtons.cs`, and `CombatUI.cs` left intact.

---

# Task: Implement UpgradeEquipController & Extract TrinketEquipPanelHelper

## Summary
Two-phase task:
1. Extract duplicated trinket equip panel discovery and refresh logic (`RefreshPanels` / `SetupTrinketSlot`) into `TrinketEquipPanelHelper.cs`, refactor `TreasureUIController`, `ArmamentBuilderRoomController`, and `EliteRewardUIController` to use it, and write unit tests in `TrinketEquipPanelHelperTests.cs`.
2. Implement `UpgradeEquipController.cs` in `Assets/Scripts/UI/` based on `ArmamentBuilderRoomController` using the helper:
   - Takes `reforgePanel`, `reforgeButton`, `leaveButton`, `testTrinketDroppedContainer`, `trinketUIItemPrefab`, `trinketEquipPanels`.
   - `Initialize()` hides `reforgePanel` and refreshes panels.
   - Reforge checks for a `TrinketUIItem` in `testTrinketDroppedContainer`.
   - Replaces the trinket in-place with a random trinket from the next rarity tier (Common -> Uncommon, Uncommon -> Rare, Rare -> different Rare).
   - If no upgrade is possible, logs a warning and does nothing.
   - On successful reforge, changes `reforgeButton` text to `"Leave"` and transitions it to complete room.
   - `leaveButton` runs the `CompleteRoom()` flow.
   - Implements `ITrinketEquipContainerUI`.
   - Write comprehensive unit tests in `UpgradeEquipControllerTests.cs`.

## Todo List

### 1. Phase 1: Shared Equip Panel Helper (`TrinketEquipPanelHelper.cs`)
- [x] Create `Assets/Scripts/UI/TrinketEquipPanelHelper.cs` with `FindEquipPanels`, `RefreshPanels`, and `SetupTrinketSlot`.
- [x] Refactor `TreasureUIController.cs` to use `TrinketEquipPanelHelper`.
- [x] Refactor `ArmamentBuilderRoomController.cs` to use `TrinketEquipPanelHelper`.
- [x] Refactor `EliteRewardUIController.cs` to use `TrinketEquipPanelHelper`.
- [x] Create `Assets/Editor/Tests/TrinketEquipPanelHelperTests.cs` and verify helper behavior.
- [x] Run existing tests (`TreasureUIControllerTests`, `ArmamentBuilderRoomControllerTests`, etc.) to verify 0 regressions.

### 2. Phase 2: Script Implementation (`UpgradeEquipController.cs`)
- [x] Create `Assets/Scripts/UI/UpgradeEquipController.cs` implementing `ITrinketEquipContainerUI`.
- [x] Add serialized fields: `reforgePanel`, `reforgeButton`, `leaveButton`, `testTrinketDroppedContainer`, `trinketUIItemPrefab`, `trinketEquipPanels`.
- [x] Implement `Initialize(List<PartyMemberInfo> party = null)` hiding `reforgePanel` and calling `RefreshPanels()`.
- [x] Implement `OnEnable` / `OnDisable` for button listeners.
- [x] Implement `OnReforgeClicked()`:
  - If `_hasReforged`: invoke `CompleteRoom()`.
  - Look for first child `TrinketUIItem` with non-null `TrinketData`.
  - Determine tier from `TrinketDatabase` (Common, Uncommon, Rare).
  - Select target pool (Common -> Uncommon, Uncommon -> Rare, Rare -> different Rare).
  - If no valid upgrade available, log warning and return.
  - Re-initialize `TrinketUIItem` in-place with new trinket, set `_hasReforged = true`, update `reforgeButton` text to `"Leave"`.
- [x] Implement `OnLeaveClicked()` / `CompleteRoom()`.
- [x] Implement `ITrinketEquipContainerUI` members (`RefreshTrinketUI`, `OnUnassignedTrinketChanged`).

### 3. Phase 3: Unit Testing & Verification (`UpgradeEquipControllerTests.cs`)
- [x] Create `Assets/Editor/Tests/UpgradeEquipControllerTests.cs`.
- [x] Test: `Initialize` hides `reforgePanel` and resets state.
- [x] Test: Reforge with empty dropper does nothing.
- [x] Test: Reforge Common rolls Uncommon.
- [x] Test: Reforge Uncommon rolls Rare.
- [x] Test: Reforge Rare rolls a different Rare.
- [x] Test: Reforge Rare with only 1 Rare in pool logs warning and does not change.
- [x] Test: Reforge with empty target pool logs warning and does not change.
- [x] Test: Reforge with trinket not in any pool logs warning and does not change.
- [x] Test: Successful reforge updates button label to "Leave" and modifies dropper item in-place.
- [x] Test: Clicking reforge button after reforge completes room.
- [x] Test: Leave button completes room before and after reforge.
- [x] Test: CompleteRoom logs error when CombatUI missing and deactivates.
- [x] Test: `OnDisable` unhooks button listeners.
- [x] Run full EditMode suite via `tests-run` MCP tool.
- [x] Update `tasks/todo.md` with verification results.

## Review & Verification Results

- **Unit Tests**:
  - `TrinketEquipPanelHelperTests.cs`: 6 new tests added and verified.
    - `FindEquipPanels_WhenRootNull_ReturnsEmptyList`
    - `FindEquipPanels_ReturnsPanelsWithMatchingChildNames`
    - `RefreshPanels_WhenPartyNull_DoesNothing`
    - `RefreshPanels_ConfiguresActivePanelsAndHidesExcess`
    - `SetupTrinketSlot_CreatesDropHandlerAndInstantiatesItem`
    - `SetupTrinketSlot_WhenTrinketSlotEmpty_ClearsChildrenAndCreatesNoItem`
  - `UpgradeEquipControllerTests.cs`: 13 new tests added and verified.
    - `Initialize_HidesReforgePanel_AndResetsReforgedState`
    - `OnReforgeClicked_WhenDropperEmpty_DoesNothing`
    - `OnReforgeClicked_WhenDropperItemHasNullData_DoesNothing`
    - `OnReforgeClicked_WithCommonTrinket_UpgradesToUncommon`
    - `OnReforgeClicked_WithUncommonTrinket_UpgradesToRare`
    - `OnReforgeClicked_WithRareTrinket_UpgradesToDifferentRare`
    - `OnReforgeClicked_WithRareTrinket_WhenOnlySameRareInPool_LogsWarningAndDoesNothing`
    - `OnReforgeClicked_WhenTargetPoolEmpty_LogsWarningAndDoesNothing`
    - `OnReforgeClicked_WhenTrinketNotInAnyPool_LogsWarningAndDoesNothing`
    - `OnReforgeClicked_WhenAlreadyReforged_CompletesRoomFlow`
    - `OnLeaveClicked_CompletesRoomFlow`
    - `CompleteRoom_WhenCombatUIMissing_LogsErrorAndStillDeactivates`
    - `OnDisable_RemovesListeners`
  - Full suite EditMode test run: **703 passed, 0 failed, 0 skipped**.
- **Files Created**:
  - `Assets/Scripts/UI/TrinketEquipPanelHelper.cs`
  - `Assets/Editor/Tests/TrinketEquipPanelHelperTests.cs`
  - `Assets/Scripts/UI/UpgradeEquipController.cs`
  - `Assets/Editor/Tests/UpgradeEquipControllerTests.cs`
- **Files Refactored to Shared Helper**:
  - `Assets/Scripts/UI/TreasureUIController.cs` (eliminated duplicate equip panel discovery and slot setup)
  - `Assets/Scripts/UI/ArmamentBuilderRoomController.cs` (eliminated duplicate equip panel discovery and slot setup)
  - `Assets/Scripts/UI/EliteRewardUIController.cs` (eliminated duplicate equip panel discovery and slot setup)

---

# Task: Implement RandomProcedureRoomController

## Summary
Implement `RandomProcedureRoomController.cs` in `Assets/Scripts/UI/` and unit tests in `Assets/Editor/Tests/RandomProcedureRoomControllerTests.cs`:
1. Two procedure buttons:
   - Procedure 1 (`LoseTrait`): Randomly lose 1 Perfection or 1 Imperfection (50/50 if both exist; if 0 of one, pick the other; if neither, do nothing).
   - Procedure 2 (`Gamble`): Configurable `goodOutcomeChance` (default 0.5). Bad outcome adds a random Imperfection (respecting `IsTraitFull` and `CanAddTrait`). Good outcome removes a random Imperfection (skipped if 0) and adds a random Perfection (respecting `IsTraitFull` and `CanAddTrait`).
2. Remembers which procedure button the player clicked (`ChosenProcedure`), highlights the chosen procedure button, keeps the two-button view active, and activates `selectionPanel`.
3. Marionette Selection UI (`selectionPanel`):
   - Populates choice buttons in Choices Panel from active party members (all selectable, including Cecilia).
   - Clicking a Marionette button updates Info Panel (Name + Level, Core Stats, Resistances, Perfections, Imperfections with `TraitTooltipTrigger`).
4. Clicking Confirm executes the remembered procedure on the selected Marionette and runs `CompleteRoom()`.

## Todo List

### 1. Script Implementation (`RandomProcedureRoomController.cs`)
- [x] Create `Assets/Scripts/UI/RandomProcedureRoomController.cs`.
- [x] Add serialized fields for Procedure UI, Selection UI, Info Panel, Styling, and Configuration (`goodOutcomeChance`).
- [x] Implement `Initialize(List<PartyMemberInfo> party = null)` and `OnEnable` / `OnDisable` button listeners.
- [x] Implement `OnProcedure1Clicked()` and `OnProcedure2Clicked()`, keeping two-button view visible, activating `selectionPanel`, and populating Marionette choice buttons.
- [x] Implement `OnMarionetteChoiceClicked(int index)`, `UpdateInfoPanel(PartyMemberInfo info)`, `ClearInfoPanel()`, and `EvaluateConfirmButton()`.
- [x] Implement `ApplyLoseTrait(PartyMemberInfo target)` and `ApplyGamble(PartyMemberInfo target)`.
- [x] Implement `OnConfirmClicked()` and `CompleteRoom()`.

### 2. Unit Testing & Verification (`RandomProcedureRoomControllerTests.cs`)
- [x] Create `Assets/Editor/Tests/RandomProcedureRoomControllerTests.cs`.
- [x] Test: `Initialize` resets state, disables confirm button, hides selection panel, keeps procedure buttons visible.
- [x] Test: Clicking Procedure 1 or Procedure 2 stores choice, highlights button, activates selection panel, and populates party member choice buttons (including Cecilia).
- [x] Test: Clicking a Marionette choice button updates Info Panel (name, stats, resistances, perfections, imperfections) and enables confirm button.
- [x] Test: Procedure 1 (`LoseTrait`) with only Perfections, only Imperfections, both (50/50 via deterministic RNG), and neither.
- [x] Test: Procedure 2 (`Gamble`) Good Outcome removes Imperfection and adds Perfection (skipping removal if 0 Imperfections, skipping addition if Perfections full, respecting `CanAddTrait` duplicates/opposite-traits).
- [x] Test: Procedure 2 (`Gamble`) Bad Outcome adds Imperfection (skipping if Imperfections full, respecting `CanAddTrait` duplicates/opposite-traits).
- [x] Test: `OnConfirmClicked` applies procedure to selected Marionette and runs `CompleteRoom()`.
- [x] Test: `CompleteRoom` logs error when `CombatUI` missing and still deactivates.
- [x] Test: `OnDisable` removes listeners.
- [x] Run full EditMode test suite via `tests-run` MCP tool.
- [x] Update `tasks/todo.md` with review and verification results.

## Review & Verification Results
- **Unit Tests**:
  - `RandomProcedureRoomControllerTests.cs`: 16 new unit tests added and verified.
    - `Initialize_SetsDefaultState_AndKeepsProcedurePanelVisible`
    - `ProcedureButtons_StoreRememberedChoice_KeepTwoButtonViewVisible_AndShowSelectionPanel`
    - `PopulateChoiceButtons_IncludesAllPartyMembers_IncludingCecilia`
    - `SelectingMarionette_PopulatesInfoPanel_AndEnablesConfirmButton`
    - `ApplyLoseTrait_WhenNeitherTraitPresent_DoesNothing`
    - `ApplyLoseTrait_WhenOnlyPerfectionPresent_RemovesPerfection`
    - `ApplyLoseTrait_WhenOnlyImperfectionPresent_RemovesImperfection`
    - `ApplyLoseTrait_WhenBothPresent_Uses5050Roll`
    - `ApplyGamble_BadOutcome_AddsImperfection_RespectingDuplicateAndOppositeRules`
    - `ApplyGamble_BadOutcome_WhenImperfectionsFull_DoesNothing`
    - `ApplyGamble_GoodOutcome_RemovesImperfectionAndGainsPerfection`
    - `ApplyGamble_GoodOutcome_SkipsRemovalWhenNoImperfections_AndStillGainsPerfection`
    - `ApplyGamble_GoodOutcome_RemovesImperfection_AndSkipsGainWhenPerfectionsFull`
    - `OnConfirmClicked_AppliesRememberedProcedureToSelectedMarionette_AndCompletesRoom`
    - `CompleteRoom_WhenCombatUIMissing_LogsErrorAndStillDeactivates`
    - `OnDisable_RemovesButtonListeners`
  - Full suite EditMode test run: **719 passed, 0 failed, 0 skipped**.
- **Files Created**:
  - `Assets/Scripts/UI/RandomProcedureRoomController.cs`
  - `Assets/Editor/Tests/RandomProcedureRoomControllerTests.cs`







