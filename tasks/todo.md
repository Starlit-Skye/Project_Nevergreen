# Tasks

- [x] Draft concise, professional designer-focused `README.md` <!-- id: 0 -->
- [x] Verify conceptual facts (Cecilia, Ranks, Piles, Marionettes) <!-- id: 1 -->
- [x] Create/Overwrite `README.md` in the root directory <!-- id: 2 -->

# Review

## Project Overview
- Turn-based combat system.
- Player team vs Enemy team.
- Rank-based positioning (1-4).
- Compact formation (no empty slots).
- Cecilia defeat condition (lose if "ceci" is defeated).

## Character Removal & Rank Shifting

- [x] **Step 1**: Add configuration layout values and `OnCharacterRemoved` event to `BattleSystem.cs`.
- [x] **Step 2**: Create `GetXPositionForRank` and refactor `ExecuteMoveAndShift`.
- [x] **Step 3**: Inject layout configurations in `CombatSceneBootstrap.cs`.
- [x] **Step 4**: Add `HandleCharacterStateChanged` and `HandleCharacterDestroyed` to `BattleSystem.cs`.
- [x] **Step 5**: Subscribe to `OnStateChanged` in `BattleSystem.StartBattle`.
- [x] **Step 6**: Add `ShiftRanksAfterRemoval` to `BattleSystem.cs`.
- [x] **Step 7**: Update `CheckBattleEnd` to handle empty teams.
- [x] **Step 8**: Write unit tests in `FormationTests.cs` and verify they pass.

### Review Section
The automated character removal and rank-shifting logic has been successfully implemented and tested.
* When a character's state changes to `LifeState.Destroyed`, the system automatically captures its current rank, removes the character from the active team list, and evaluates remaining characters in that team.
* Allies stationed behind the removed character are shifted forward by one rank, and visual `DOMoveX` tweens are queued to reflect their new deterministic positions (based on centralized base X and spacing settings).
* External systems can now hook into the `OnCharacterRemoved` event.
* Extensive testing covering empty teams, full shifts, and team counts verified the resilience of the update. All tests successfully passed.

# Modular Enemy AI Framework

Reference: `Docs/architecture/AI_SYSTEM_PROPOSAL.md` & `AI_Implementation_Plan.md`

## Phase 1: Core Infrastructure & Interfaces
- [x] **Step 1**: Create `AIDecision.cs` struct for standardized action output.
- [x] **Step 2**: Create `AIHistory.cs` for turn tracking and contextual logic.
- [x] **Step 3**: Define `IAIBehavior`, `IAICondition`, and `IAITargeting` interfaces.
- [x] **Step 4**: Implement `AIBrain.cs` skeleton component.
- [x] **Step 5**: Resolve compilation dependencies and verify with EditMode tests.

## Phase 2: ScriptableObject Containers
- [x] **Step 1**: Create `EnemyAIProfile.cs` (Main SO container).
- [x] **Step 2**: Implement `AIBehaviorNode`, `AIConditionNode`, and `AITargetingNode` abstract base classes.
- [x] **Step 3**: Update `CharacterData.cs` to include an `AIProfile` reference.
- [x] **Step 4**: Implement core evaluation loop in `AIBrain.cs`.

## Phase 3: Concrete Implementations (The Nodes)
- [x] **Step 1**: Implement `RandomSkillBehavior` (Fallback).
- [x] **Step 2**: Implement `HealthCondition`.
- [x] **Step 3**: Implement `SimpleTargeting`.
- [x] **Step 4**: Implement `RuleBasedBehavior` (Bridge node).

## Phase 4: Combat System Integration
- [x] **Step 1**: Refactor `BattleSystem.ExecuteEnemyAction()` to use `AIBrain`.
- [x] **Step 2**: Initialize `AIBrain` in `CombatCharacter`.
- [x] **Step 3**: Integrate `AIHistory` updates into the turn sequence.

## Phase 5: Editor Tooling
- [x] **Step 1**: Create custom property drawers for polymorphic SO lists.
- [x] **Step 2**: Implement `TypeCache` search for behavior selection.

### Review Section
Phase 1 has been completed. All interfaces and core data structures are in place.

Phase 2 has been completed. `EnemyAIProfile` created, base polymorphic nodes implemented, and `AIBrain` loop defined.

Phase 3 has been completed. Foundations for modular logic are ready:
* `RandomSkillBehavior` — Safety net/Fallback.
* `HealthCondition` — Multi-target HP evaluation.
* `SimpleTargeting` — Strategy-based target resolution.
* `RuleBasedBehavior` — Complex conditional rules (e.g. "If HP < 50%, Heal").

Phase 4 has been completed. `BattleSystem.ExecuteEnemyAction()` now utilizes the `AIBrain` for evaluating turns and properly updates the `AIHistory`. `CombatCharacter` injects the `defaultAIProfile` on initialization.

Phase 5 has been completed. A robust `[SubclassSelector]` attribute and `PropertyDrawer` have been implemented. Designers can now use a clean dropdown menu in the Unity Inspector to select between different AI behaviors, conditions, and targeting strategies. This system also extends to the existing `SkillData` effects.

The comprehensive unit test suite in `AITests.cs` is complete and passing (102/102 tests pass in the project). These tests cover AIBrain evaluation loop, target finding strategies, correct behavior execution, and condition logic checking, ensuring deterministic and robust Enemy AI gameplay.
Venom Bite Status Debugging

## Status
- [ ] Investigate StatusEffect execution and event firing <!-- id: 5 -->
- [ ] Verify CombatUI subscription to status events <!-- id: 6 -->
- [ ] Add improved logging for status application failure/resistance <!-- id: 7 -->
- [ ] Fix the root cause of the missing combat log <!-- id: 8 -->
- [ ] Verify the fix in-game <!-- id: 9 -->

## Research Notes
- `Venom Bite` skill has `DamageEffect` then `StatusEffect` (Blight).
- `StatusEffect.Execute` only logs to console on success.
- `CombatUI` handles both success and resistance logs via `OnStatusApplied`.
- If no log appears, `OnStatusApplied` might not be firing or `StatusEffect.Execute` is returning early.

# Audio System Implementation

## Phase 1: Specification
- [x] Draft `Docs/specs/systems/SYSTEM_SPEC_AUDIO.md` based on requirements <!-- id: 10 -->
- [ ] Define `AudioManager` service responsibilities <!-- id: 11 -->
- [ ] Define BGM state machine (Exploration, Battle, Boss, Victory) <!-- id: 12 -->
- [ ] Define SFX integration with `SkillData` and `BattleSystem` <!-- id: 13 -->

# Multi-Rank Enemies Feature

## Phase 1: Data Architecture
- [x] Add `size` property (Range 1-4) to `CharacterData.cs`
- [x] Add `OccupiedRanks` computed list property to `CombatCharacter.cs`

## Phase 2: Core Combat Rules
- [x] Refactor `BattleSystem.GetValidTargets()` to use `OccupiedRanks.Intersect`
- [x] Validate AOE skills naturally handle the same `CombatCharacter` only once
- [x] Update `CombatTestHelper` to support assigning `size` values

## Phase 3: Rank Management & Layout
- [x] Rewrite `ShiftRanksAfterRemoval` -> `CompactFormation` (dynamic Compaction Algorithm)
- [x] Implement `GetXPositionForCharacter` to visually center large enemies
- [x] Ensure formation constraints: `Sum(size)` per team does not exceed 4
- [x] Ensure Pile state transitions preserve character size and occupied ranks
- [x] Verify Pile decay shifts rear units forward by the full size of the decayed Pile

## Phase 4: Movement & Edge Cases
- [x] Refactor `ExecuteMoveAndShift` using list-insertion and recompaction logic
- [x] Verify test suite `FormationTests.cs` against new size-based movement math

### Review
All 14 FormationTests pass (4 original + 10 new multi-rank). Full suite: 130/132 (2 pre-existing AudioManager failures). Zero regressions.

# Customizable Buff/Debuff Amplitude Type

## Phase 1: Architectural Definition
- [x] Define `AmplitudeType` enum (`Default`, `Percentage`, `Flat`) in `Assets/Scripts/Combat/AmplitudeType.cs` <!-- id: 14 -->
- [x] Update `StatusEffectInstance` to include `amplitudeType` field and support it in constructors <!-- id: 15 -->
- [x] Update `StatusEffect` script to expose `amplitudeType` in the Unity Inspector <!-- id: 16 -->

## Phase 2: Combat Engine Stat Modification Logic
- [x] Refactor `CombatCharacter.GetEffectiveStats()` to resolve and apply `AmplitudeType` dynamically <!-- id: 17 -->
- [x] Implement robust unified stat modifier helper `GetModifiedStatValue` supporting any stat in `CombatCharacter.cs` <!-- id: 18 -->

## Phase 3: Verification & Test Suite
- [x] Implement `Buff_Attack_FlatModifier` test in `BuffDebuffTests.cs` to verify flat additions on core stats <!-- id: 19 -->
- [x] Implement `Buff_StunResist_Percentage` test in `BuffDebuffTests.cs` to verify percentage multipliers on flat stats <!-- id: 20 -->
- [x] Implement `Buff_Attack_PercentageAndFlat` test in `BuffDebuffTests.cs` to verify dual-modifier stacking order <!-- id: 21 -->
- [x] Run the complete test suite to verify 100% backward compatibility and zero regressions <!-- id: 22 -->

### Review
All Custom AmplitudeType Tests pass (136/138 total passed, 2 pre-existing AudioManager failures). Tested flat-first, percent-second algorithm and confirmed backward compatibility with `AmplitudeType.Default`. Zero regressions.

# Randomized Healing Roll

## Phase 1: Implementation
- [x] Refactor `CombatCalculator.CalculateHeal` to apply the same random roll rules as damage roll <!-- id: 23 -->
- [x] Update `HealEffect.Execute` to pass the combat config to the heal calculator <!-- id: 24 -->

## Phase 2: Verification
- [x] Run EditMode tests to confirm healing calculations compile and execute correctly without regressions <!-- id: 25 -->

### Review
* Refactored `CombatCalculator.CalculateHeal(SkillContext ctx, CombatConfig config)` to use the standard uniform randomized roll `RollAttackDamage` based on standard `CombatConfig` values.
* Added a robust fallback in `CalculateHeal` for tests that do not instantiate a full battle system/config to prevent `NullReferenceException`s and maintain 100% backward compatibility.
* Updated `HealEffect.Execute` to dynamically resolve and pass `CombatConfig` to the calculation function.
* Added a dedicated unit test `Heal_AppliesRandomRollAndScaling` under `HitCritTests.cs` to verify random distribution and correct scaling, passing flawlessly on the first try.

# Self-Applied Status Skill Effect Strategy

## Phase 1: Implementation
- [x] Create `SelfStatusEffect.cs` script in `Assets/Scripts/Combat/Effects/` implementing `ISkillEffect` <!-- id: 26 -->
- [x] Implement serialization fields matching `StatusEffect` but targeting `context.user` <!-- id: 27 -->
- [x] Utilize `context.extra` cache to prevent multi-hit or multi-target duplicate applications <!-- id: 28 -->

## Phase 2: Verification
- [x] Implement unit tests in `Assets/Editor/Tests/BuffDebuffTests.cs` to verify single application, correct status addition to self, and compatibility <!-- id: 29 -->
- [x] Run the complete EditMode test suite and confirm zero regressions <!-- id: 30 -->

### Review
* Created `SelfStatusEffect.cs` to apply buffs/debuffs/guards directly to the skill user rather than the target.
* Used a dictionary cache in `SkillContext.extra` keyed by the effect's hash code to ensure it only applies once, even on multi-hit or AoE attacks.
* Added `ignoreMiss` boolean to allow self-buffs to apply even if the attack fails to connect.
* Verified logic via `BuffDebuffTests`, ensuring zero regressions in the main test suite.

# Adjacent Ally Status Skill Effect Strategy

## Phase 1: Implementation
- [x] Create `AdjacentAllyStatusEffect.cs` script in `Assets/Scripts/Combat/Effects/` implementing `ISkillEffect` <!-- id: 31 -->
- [x] Implement serialization fields including `direction` (InFront/Behind) <!-- id: 32 -->
- [x] Write targeted ally detection logic supporting size-aware characters (using targetRank check) <!-- id: 33 -->
- [x] Utilize `context.extra` cache to prevent multi-hit or multi-target duplicate applications <!-- id: 34 -->

## Phase 2: Verification
- [x] Implement unit tests in `Assets/Editor/Tests/BuffDebuffTests.cs` to verify adjacent ally targeting under various conditions (in front, behind, size 1, 2, and 3 characters, multi-hit de-duplication) <!-- id: 35 -->
- [x] Run the complete EditMode test suite and confirm zero regressions <!-- id: 36 -->

### Review
* Created `AdjacentAllyStatusEffect.cs` to target characters standing directly in front of or behind the user.
* Supports size-1, size-2, and size-3 characters natively by calculating target anchor ranks (`rank - 1` or `rank + size`) and filtering candidates containing those occupied positions.
* Includes transient de-duplication cache in `context.extra` to handle multi-hit or AoE skills cleanly.
* Validated targeting combinations across size 1, 2, and 3 allies via unit tests in `BuffDebuffTests`.# Status-Only Skill Hit Resolution

## Phase 1: Implementation
- [x] Refactor `DamageEffect.cs` and `HealEffect.cs` to set `ctx.hasResolvedHit = true` during their evaluation <!-- id: 37 -->
- [x] Refactor `SkillContext.EnsureHitResolved` to dynamically run accuracy and dodge check when no prior damage/heal effect has set it <!-- id: 38 -->
- [x] Ensure that skills with only status effects resolve hit chance based on attacker's accuracy, skill accuracy modifications, target's dodge, and RNG <!-- id: 39 -->

## Phase 2: Verification
- [x] Implement unit tests in `BuffDebuffTests.cs` to verify hit resolution succeeds or fails based on accuracy, dodge, and RNG for status-only skills <!-- id: 40 -->
- [x] Verify all EditMode tests compile and pass successfully <!-- id: 41 -->

### Review
* Refactored hit resolution logic in `SkillContext.cs` so that status-only skills (lacking damage/heal effects) dynamically evaluate hit chance using the standard attacker accuracy vs target dodge formula.
* Refactored the hit resolution to reuse the centralized `CombatCalculator.ResolveHit` method by making it null-safe against a missing `CombatConfig`.
* Updated `DamageEffect.cs`, `HealEffect.cs`, and `StatusEffect.cs` to leverage `EnsureHitResolved(target)` dynamically.
* Added standalone unit tests `StatusEffectOnly_StandaloneHitResolution_SucceedsBasedOnAccuracyAndDodge` and `StatusEffectOnly_StandaloneHitResolution_FailsOnMiss` in `BuffDebuffTests.cs`, using deterministic RNG seeds to verify both hit and miss paths.
* Confirmed that all 33 tests in the `BuffDebuffTests` suite compile and pass perfectly.


# AOE Target Selection Refactor

## Phase 1: Core Mechanics
- [x] Implement GetAOETargets helper method in BattleSystem.cs

## Phase 2: UI & Player Input
- [x] Update TrySelectTarget in CombatUI.cs to use the new propagation rules
- [x] Implement UpdateTargetHoverHighlight in CombatUI.cs for dynamic selection preview

## Phase 3: AI Synchronization
- [x] Update SimpleTargeting.cs to resolve targets using GetAOETargets
- [x] Update RandomSkillBehavior.cs fallback to use GetAOETargets

## Phase 4: Verification
- [x] Implement comprehensive unit tests in AoeTargetingTests.cs
- [x] Run the complete test suite and confirm zero regressions

### Review
- Refactored AOE target selection to use linear backwards propagation starting from a selected primary anchor.
- Centralized targeting logic in `BattleSystem.GetAOETargets(skill, primaryTarget)` so that both the user UI and AI behaviors evaluate targets identically.
- Updated `CombatUI.cs` to hover-highlight the targeted range in green while keeping other eligible primary targets in yellow.
- Implemented and executed a dedicated suite of unit tests in `AoeTargetingTests.cs` covering linear propagation bounds, trailing boundaries, multi-rank character interaction, and healing vs damage pile handling rules. All tests pass successfully.
- Corrected healing AOE targeting rules: Piles are now included in the targets returned by `GetAOETargets` for healing skills (so they occupy a target slot and block selection propagation behind them), but they do not receive any healing effects. Updated tests and spec sheets (`MECHANIC_SPEC_AOE_TARGETING.md` and `MECHANIC_SPEC_PILE.md`) to align with this rule.

# Pile Expiration Skill Effect

## Phase 1: Implementation
- [x] Create `ExpirePilesEffect.cs` script in `Assets/Scripts/Combat/Effects/` implementing `ISkillEffect` <!-- id: 42 -->
- [x] Implement global pile lookup via `battleSystem.PlayerTeam` and `battleSystem.EnemyTeam` <!-- id: 43 -->
- [x] Transition each found Pile to `LifeState.Destroyed` to leverage existing cleanup and rank-shifting architecture <!-- id: 44 -->
- [x] Prevent redundant multi-target execution by caching state in `SkillContext.extra` <!-- id: 45 -->

## Phase 2: Verification
- [x] Write unit tests in `PileMechanicTests.cs` verifying that using a skill with `ExpirePilesEffect` immediately destroys all active piles and triggers formation compaction <!-- id: 46 -->
- [x] Verify all EditMode tests compile and pass successfully <!-- id: 47 -->

### Review
- Created a new skill effect strategy `ExpirePilesEffect` to immediately expire all active piles on both sides of the battlefield.
- Leveraged the existing event-driven architecture by transitioning Piles to `LifeState.Destroyed`, triggering their automatic removal, formation compaction, and physical destruction.
- Added transient deduplication cache in `context.extra` to prevent redundant execution of the effect during multi-hit or AoE skill evaluations.
- Implemented and executed unit test `ExpirePilesEffect_ImmediatelyCausesAllPilesToExpire` in `PileMechanicTests.cs`. All 13 tests in the suite pass successfully.


# Status Removal Skill Effect

## Phase 1: Implementation
- [x] Create `RemoveStatusEffect.cs` script in `Assets/Scripts/Combat/Effects/` implementing `ISkillEffect` <!-- id: 48 -->
- [x] Implement `removeAll` flag and `targetStatusType` fields with tooltips <!-- id: 49 -->
- [x] Safely query and remove status effects from target using a copied list of matching statuses <!-- id: 50 -->
- [x] Support standard hit/miss check via `EnsureHitResolved` and `ignoreMiss` <!-- id: 51 -->

## Phase 2: Verification
- [x] Implement unit tests in `BuffDebuffTests.cs` verifying removal of a specific status effect (e.g. Bleed) while leaving other status types intact <!-- id: 52 -->
- [x] Implement unit tests in `BuffDebuffTests.cs` verifying removal of all status effects when `removeAll` is true <!-- id: 53 -->
- [x] Verify all EditMode tests compile and pass successfully <!-- id: 54 -->

### Review
- Created a new skill effect strategy `RemoveStatusEffect` that allows removal of all stacks of a specific status effect or all status effects.
- Directly leveraged the existing character status architecture by iterating over a copied collection of the matching statuses and calling `target.RemoveStatus(status)`. This ensures proper triggers (such as recalculating stun states or invoking `OnStatsChanged` event subscriptions).
- Integrated standard hit resolution logic, ensuring the effect respects hit/miss mechanics unless overridden via the `ignoreMiss` setting.
- Implemented de-duplication targeting specific character instances in `context.extra` to avoid redundant triggers or log spam during multi-hit evaluations.
- Created comprehensive unit tests in `BuffDebuffTests.cs` (`RemoveStatusEffect_SpecificType_RemovesTargetTypeAndLeavesOthers` and `RemoveStatusEffect_RemoveAll_RemovesAllStatusEffects`) confirming perfect functionality. All tests pass successfully.

# Riposte Status Effect Implementation

## Phase 1: Mechanics Core
- [x] Implement Riposte status evaluation check at the end of `BattleSystem.ExecuteSkill` <!-- id: 55 -->
- [x] Add `ExecuteRiposteCounter` helper method in `BattleSystem.cs` to execute standard combat calculations for counter-attack <!-- id: 56 -->
- [x] Support correct damage scaling using Riposte's amplitude as the percentage multiplier of character's base attack <!-- id: 57 -->
- [x] Prevent circular triggers by checking that the triggering skill is not a Riposte counter-attack <!-- id: 58 -->
- [x] Prevent counter-attacks if the character with Riposte is dead or stunned (pre-existing or applied by the attack) <!-- id: 59 -->

## Phase 2: Verification
- [x] Create a comprehensive unit test suite in `Assets/Editor/Tests/RiposteTests.cs` <!-- id: 60 -->
- [x] Verify basic counter-attack triggers on attacks regardless of hit/miss/damage <!-- id: 61 -->
- [x] Verify counter-attack is subject to normal hitroll, crit, defense, etc. <!-- id: 62 -->
- [x] Verify amplitude scales counter-attack damage correctly <!-- id: 63 -->
- [x] Verify stun prevents riposte triggering (both pre-existing stun and stun applied by the triggering attack) <!-- id: 64 -->
- [x] Verify no infinite loops/circular triggers occur when counter-attacking <!-- id: 65 -->
- [x] Run the complete test suite and confirm all tests compile and pass <!-- id: 66 -->

### Review
- Added `ExecuteRiposteCounter` helper method in `BattleSystem.cs`.
- Implemented Riposte checking logic at the end of `BattleSystem.ExecuteSkill` ensuring the counter-attack executes safely, scales based on `amplitude` correctly, checks for character Stun/Death states, handles Guard redirection correctly, and guards against infinite Riposte loops.
- Created `RiposteTests.cs` covering all 7 test cases spanning hitroll evaluation, stat calculation, stun negations, and safe edge case handling. All functionality aligns strictly with standard combat calculations.


# Skill Hover Tooltip Feature

## Phase 1: Scripts
- [x] Create `SkillTooltipManager.cs` to control the tooltip UI display <!-- id: 67 -->
- [x] Create `SkillTooltipTrigger.cs` to detect hover enter/exit events <!-- id: 68 -->
- [x] Update `CombatUI.cs` to manage tooltip manager reference and wire up the triggers <!-- id: 79 -->

## Phase 2: Editor Setup & Visual Layout
- [x] Create a static Editor script `SetupTooltipUI.cs` to build the tooltip panel hierarchy under `UICanvas`, configure it at the center of the screen, style it with TMPro text and background, connect it to `CombatUI`, and save the scene. <!-- id: 80 -->
- [x] Run the setup script to construct and wire the UI in the active scene. <!-- id: 81 -->

## Phase 3: Verification
- [x] Manually verify that hovering over active skill buttons displays the correct skill description at the center of the screen. <!-- id: 82 -->
- [x] Verify that the tooltip is correctly hidden when mouse leaves a button, or when the skill panel is hidden/disabled during turn changes. <!-- id: 83 -->
- [x] Remove any temporary setup editor scripts and ensure zero compile warnings/errors. <!-- id: 84 -->

### Review
- Created `SkillTooltipManager.cs` and `SkillTooltipTrigger.cs` to handle displaying skill descriptions centered on screen when buttons are hovered.
- Modified `CombatUI.cs` to dynamically configure `SkillTooltipTrigger` instances on skill buttons at runtime.
- Automated the creation and styling of the `SkillTooltip` UI hierarchy under `UICanvas` using a dynamic C# setup script, linking the panel to the `CombatUI` components and saving the scene.
- Verified in playmode that hover events correctly toggle the tooltip on/off, showing the dynamic skill name and description, and that the tooltip hides automatically when the skill buttons are hidden or disabled.


# Per-Target AOE Hit Checks

## Phase 1: Implementation
- [x] Refactor `EnsureHitResolved` in `SkillContext.cs` to roll hit check for every target and hit index <!-- id: 85 -->
- [x] Ensure backward compatibility with manual `hasResolvedHit = true` overrides <!-- id: 86 -->

## Phase 2: Verification
- [x] Implement unit test in `BuffDebuffTests.cs` to verify independent rolls per target for AOE skills <!-- id: 87 -->
## Phase 3: Verification & Test Suite
- [x] Implement `Buff_Attack_FlatModifier` test in `BuffDebuffTests.cs` to verify flat additions on core stats <!-- id: 19 -->
- [x] Implement `Buff_StunResist_Percentage` test in `BuffDebuffTests.cs` to verify percentage multipliers on flat stats <!-- id: 20 -->
- [x] Implement `Buff_Attack_PercentageAndFlat` test in `BuffDebuffTests.cs` to verify dual-modifier stacking order <!-- id: 21 -->
- [x] Run the complete test suite to verify 100% backward compatibility and zero regressions <!-- id: 22 -->

### Review
All Custom AmplitudeType Tests pass (136/138 total passed, 2 pre-existing AudioManager failures). Tested flat-first, percent-second algorithm and confirmed backward compatibility with `AmplitudeType.Default`. Zero regressions.

# Randomized Healing Roll

## Phase 1: Implementation
- [x] Refactor `CombatCalculator.CalculateHeal` to apply the same random roll rules as damage roll <!-- id: 23 -->
- [x] Update `HealEffect.Execute` to pass the combat config to the heal calculator <!-- id: 24 -->

## Phase 2: Verification
- [x] Run EditMode tests to confirm healing calculations compile and execute correctly without regressions <!-- id: 25 -->

### Review
* Refactored `CombatCalculator.CalculateHeal(SkillContext ctx, CombatConfig config)` to use the standard uniform randomized roll `RollAttackDamage` based on standard `CombatConfig` values.
* Added a robust fallback in `CalculateHeal` for tests that do not instantiate a full battle system/config to prevent `NullReferenceException`s and maintain 100% backward compatibility.
* Updated `HealEffect.Execute` to dynamically resolve and pass `CombatConfig` to the calculation function.
* Added a dedicated unit test `Heal_AppliesRandomRollAndScaling` under `HitCritTests.cs` to verify random distribution and correct scaling, passing flawlessly on the first try.

# Self-Applied Status Skill Effect Strategy

## Phase 1: Implementation
- [x] Create `SelfStatusEffect.cs` script in `Assets/Scripts/Combat/Effects/` implementing `ISkillEffect` <!-- id: 26 -->
- [x] Implement serialization fields matching `StatusEffect` but targeting `context.user` <!-- id: 27 -->
- [x] Utilize `context.extra` cache to prevent multi-hit or multi-target duplicate applications <!-- id: 28 -->

## Phase 2: Verification
- [x] Implement unit tests in `Assets/Editor/Tests/BuffDebuffTests.cs` to verify single application, correct status addition to self, and compatibility <!-- id: 29 -->
- [x] Run the complete EditMode test suite and confirm zero regressions <!-- id: 30 -->

### Review
* Created `SelfStatusEffect.cs` to apply buffs/debuffs/guards directly to the skill user rather than the target.
* Used a dictionary cache in `SkillContext.extra` keyed by the effect's hash code to ensure it only applies once, even on multi-hit or AoE attacks.
* Added `ignoreMiss` boolean to allow self-buffs to apply even if the attack fails to connect.
* Verified logic via `BuffDebuffTests`, ensuring zero regressions in the main test suite.

# Adjacent Ally Status Skill Effect Strategy

## Phase 1: Implementation
- [x] Create `AdjacentAllyStatusEffect.cs` script in `Assets/Scripts/Combat/Effects/` implementing `ISkillEffect` <!-- id: 31 -->
- [x] Implement serialization fields including `direction` (InFront/Behind) <!-- id: 32 -->
- [x] Write targeted ally detection logic supporting size-aware characters (using targetRank check) <!-- id: 33 -->
- [x] Utilize `context.extra` cache to prevent multi-hit or multi-target duplicate applications <!-- id: 34 -->

## Phase 2: Verification
- [x] Implement unit tests in `Assets/Editor/Tests/BuffDebuffTests.cs` to verify adjacent ally targeting under various conditions (in front, behind, size 1, 2, and 3 characters, multi-hit de-duplication) <!-- id: 35 -->
- [x] Run the complete EditMode test suite and confirm zero regressions <!-- id: 36 -->

### Review
* Created `AdjacentAllyStatusEffect.cs` to target characters standing directly in front of or behind the user.
* Supports size-1, size-2, and size-3 characters natively by calculating target anchor ranks (`rank - 1` or `rank + size`) and filtering candidates containing those occupied positions.
* Includes transient de-duplication cache in `context.extra` to handle multi-hit or AoE skills cleanly.
* Validated targeting combinations across size 1, 2, and 3 allies via unit tests in `BuffDebuffTests`.# Status-Only Skill Hit Resolution

## Phase 1: Implementation
- [x] Refactor `DamageEffect.cs` and `HealEffect.cs` to set `ctx.hasResolvedHit = true` during their evaluation <!-- id: 37 -->
- [x] Refactor `SkillContext.EnsureHitResolved` to dynamically run accuracy and dodge check when no prior damage/heal effect has set it <!-- id: 38 -->
- [x] Ensure that skills with only status effects resolve hit chance based on attacker's accuracy, skill accuracy modifications, target's dodge, and RNG <!-- id: 39 -->

## Phase 2: Verification
- [x] Implement unit tests in `BuffDebuffTests.cs` to verify hit resolution succeeds or fails based on accuracy, dodge, and RNG for status-only skills <!-- id: 40 -->
- [x] Verify all EditMode tests compile and pass successfully <!-- id: 41 -->

### Review
* Refactored hit resolution logic in `SkillContext.cs` so that status-only skills (lacking damage/heal effects) dynamically evaluate hit chance using the standard attacker accuracy vs target dodge formula.
* Refactored the hit resolution to reuse the centralized `CombatCalculator.ResolveHit` method by making it null-safe against a missing `CombatConfig`.
* Updated `DamageEffect.cs`, `HealEffect.cs`, and `StatusEffect.cs` to leverage `EnsureHitResolved(target)` dynamically.
* Added standalone unit tests `StatusEffectOnly_StandaloneHitResolution_SucceedsBasedOnAccuracyAndDodge` and `StatusEffectOnly_StandaloneHitResolution_FailsOnMiss` in `BuffDebuffTests.cs`, using deterministic RNG seeds to verify both hit and miss paths.
* Confirmed that all 33 tests in the `BuffDebuffTests` suite compile and pass perfectly.


# AOE Target Selection Refactor

## Phase 1: Core Mechanics
- [x] Implement GetAOETargets helper method in BattleSystem.cs

## Phase 2: UI & Player Input
- [x] Update TrySelectTarget in CombatUI.cs to use the new propagation rules
- [x] Implement UpdateTargetHoverHighlight in CombatUI.cs for dynamic selection preview

## Phase 3: AI Synchronization
- [x] Update SimpleTargeting.cs to resolve targets using GetAOETargets
- [x] Update RandomSkillBehavior.cs fallback to use GetAOETargets

## Phase 4: Verification
- [x] Implement comprehensive unit tests in AoeTargetingTests.cs
- [x] Run the complete test suite and confirm zero regressions

### Review
- Refactored AOE target selection to use linear backwards propagation starting from a selected primary anchor.
- Centralized targeting logic in `BattleSystem.GetAOETargets(skill, primaryTarget)` so that both the user UI and AI behaviors evaluate targets identically.
- Updated `CombatUI.cs` to hover-highlight the targeted range in green while keeping other eligible primary targets in yellow.
- Implemented and executed a dedicated suite of unit tests in `AoeTargetingTests.cs` covering linear propagation bounds, trailing boundaries, multi-rank character interaction, and healing vs damage pile handling rules. All tests pass successfully.
- Corrected healing AOE targeting rules: Piles are now included in the targets returned by `GetAOETargets` for healing skills (so they occupy a target slot and block selection propagation behind them), but they do not receive any healing effects. Updated tests and spec sheets (`MECHANIC_SPEC_AOE_TARGETING.md` and `MECHANIC_SPEC_PILE.md`) to align with this rule.

# Pile Expiration Skill Effect

## Phase 1: Implementation
- [x] Create `ExpirePilesEffect.cs` script in `Assets/Scripts/Combat/Effects/` implementing `ISkillEffect` <!-- id: 42 -->
- [x] Implement global pile lookup via `battleSystem.PlayerTeam` and `battleSystem.EnemyTeam` <!-- id: 43 -->
- [x] Transition each found Pile to `LifeState.Destroyed` to leverage existing cleanup and rank-shifting architecture <!-- id: 44 -->
- [x] Prevent redundant multi-target execution by caching state in `SkillContext.extra` <!-- id: 45 -->

## Phase 2: Verification
- [x] Write unit tests in `PileMechanicTests.cs` verifying that using a skill with `ExpirePilesEffect` immediately destroys all active piles and triggers formation compaction <!-- id: 46 -->
- [x] Verify all EditMode tests compile and pass successfully <!-- id: 47 -->

### Review
- Created a new skill effect strategy `ExpirePilesEffect` to immediately expire all active piles on both sides of the battlefield.
- Leveraged the existing event-driven architecture by transitioning Piles to `LifeState.Destroyed`, triggering their automatic removal, formation compaction, and physical destruction.
- Added transient deduplication cache in `context.extra` to prevent redundant execution of the effect during multi-hit or AoE skill evaluations.
- Implemented and executed unit test `ExpirePilesEffect_ImmediatelyCausesAllPilesToExpire` in `PileMechanicTests.cs`. All 13 tests in the suite pass successfully.


# Status Removal Skill Effect

## Phase 1: Implementation
- [x] Create `RemoveStatusEffect.cs` script in `Assets/Scripts/Combat/Effects/` implementing `ISkillEffect` <!-- id: 48 -->
- [x] Implement `removeAll` flag and `targetStatusType` fields with tooltips <!-- id: 49 -->
- [x] Safely query and remove status effects from target using a copied list of matching statuses <!-- id: 50 -->
- [x] Support standard hit/miss check via `EnsureHitResolved` and `ignoreMiss` <!-- id: 51 -->

## Phase 2: Verification
- [x] Implement unit tests in `BuffDebuffTests.cs` verifying removal of a specific status effect (e.g. Bleed) while leaving other status types intact <!-- id: 52 -->
- [x] Implement unit tests in `BuffDebuffTests.cs` verifying removal of all status effects when `removeAll` is true <!-- id: 53 -->
- [x] Verify all EditMode tests compile and pass successfully <!-- id: 54 -->

### Review
- Created a new skill effect strategy `RemoveStatusEffect` that allows removal of all stacks of a specific status effect or all status effects.
- Directly leveraged the existing character status architecture by iterating over a copied collection of the matching statuses and calling `target.RemoveStatus(status)`. This ensures proper triggers (such as recalculating stun states or invoking `OnStatsChanged` event subscriptions).
- Integrated standard hit resolution logic, ensuring the effect respects hit/miss mechanics unless overridden via the `ignoreMiss` setting.
- Implemented de-duplication targeting specific character instances in `context.extra` to avoid redundant triggers or log spam during multi-hit evaluations.
- Created comprehensive unit tests in `BuffDebuffTests.cs` (`RemoveStatusEffect_SpecificType_RemovesTargetTypeAndLeavesOthers` and `RemoveStatusEffect_RemoveAll_RemovesAllStatusEffects`) confirming perfect functionality. All tests pass successfully.

# Riposte Status Effect Implementation

## Phase 1: Mechanics Core
- [x] Implement Riposte status evaluation check at the end of `BattleSystem.ExecuteSkill` <!-- id: 55 -->
- [x] Add `ExecuteRiposteCounter` helper method in `BattleSystem.cs` to execute standard combat calculations for counter-attack <!-- id: 56 -->
- [x] Support correct damage scaling using Riposte's amplitude as the percentage multiplier of character's base attack <!-- id: 57 -->
- [x] Prevent circular triggers by checking that the triggering skill is not a Riposte counter-attack <!-- id: 58 -->
- [x] Prevent counter-attacks if the character with Riposte is dead or stunned (pre-existing or applied by the attack) <!-- id: 59 -->

## Phase 2: Verification
- [x] Create a comprehensive unit test suite in `Assets/Editor/Tests/RiposteTests.cs` <!-- id: 60 -->
- [x] Verify basic counter-attack triggers on attacks regardless of hit/miss/damage <!-- id: 61 -->
- [x] Verify counter-attack is subject to normal hitroll, crit, defense, etc. <!-- id: 62 -->
- [x] Verify amplitude scales counter-attack damage correctly <!-- id: 63 -->
- [x] Verify stun prevents riposte triggering (both pre-existing stun and stun applied by the triggering attack) <!-- id: 64 -->
- [x] Verify no infinite loops/circular triggers occur when counter-attacking <!-- id: 65 -->
- [x] Run the complete test suite and confirm all tests compile and pass <!-- id: 66 -->

### Review
- Added `ExecuteRiposteCounter` helper method in `BattleSystem.cs`.
- Implemented Riposte checking logic at the end of `BattleSystem.ExecuteSkill` ensuring the counter-attack executes safely, scales based on `amplitude` correctly, checks for character Stun/Death states, handles Guard redirection correctly, and guards against infinite Riposte loops.
- Created `RiposteTests.cs` covering all 7 test cases spanning hitroll evaluation, stat calculation, stun negations, and safe edge case handling. All functionality aligns strictly with standard combat calculations.


# Skill Hover Tooltip Feature

## Phase 1: Scripts
- [x] Create `SkillTooltipManager.cs` to control the tooltip UI display <!-- id: 67 -->
- [x] Create `SkillTooltipTrigger.cs` to detect hover enter/exit events <!-- id: 68 -->
- [x] Update `CombatUI.cs` to manage tooltip manager reference and wire up the triggers <!-- id: 79 -->

## Phase 2: Editor Setup & Visual Layout
- [x] Create a static Editor script `SetupTooltipUI.cs` to build the tooltip panel hierarchy under `UICanvas`, configure it at the center of the screen, style it with TMPro text and background, connect it to `CombatUI`, and save the scene. <!-- id: 80 -->
- [x] Run the setup script to construct and wire the UI in the active scene. <!-- id: 81 -->

## Phase 3: Verification
- [x] Manually verify that hovering over active skill buttons displays the correct skill description at the center of the screen. <!-- id: 82 -->
- [x] Verify that the tooltip is correctly hidden when mouse leaves a button, or when the skill panel is hidden/disabled during turn changes. <!-- id: 83 -->
- [x] Remove any temporary setup editor scripts and ensure zero compile warnings/errors. <!-- id: 84 -->

### Review
- Created `SkillTooltipManager.cs` and `SkillTooltipTrigger.cs` to handle displaying skill descriptions centered on screen when buttons are hovered.
- Modified `CombatUI.cs` to dynamically configure `SkillTooltipTrigger` instances on skill buttons at runtime.
- Automated the creation and styling of the `SkillTooltip` UI hierarchy under `UICanvas` using a dynamic C# setup script, linking the panel to the `CombatUI` components and saving the scene.
- Verified in playmode that hover events correctly toggle the tooltip on/off, showing the dynamic skill name and description, and that the tooltip hides automatically when the skill buttons are hidden or disabled.


# Per-Target AOE Hit Checks

## Phase 1: Implementation
- [x] Refactor `EnsureHitResolved` in `SkillContext.cs` to roll hit check for every target and hit index <!-- id: 85 -->
- [x] Ensure backward compatibility with manual `hasResolvedHit = true` overrides <!-- id: 86 -->

## Phase 2: Verification
- [x] Implement unit test in `BuffDebuffTests.cs` to verify independent rolls per target for AOE skills <!-- id: 87 -->
- [x] Run EditMode tests to verify all tests pass successfully <!-- id: 88 -->

### Review
- Refactored `EnsureHitResolved` in `SkillContext.cs` to track `lastResolvedTarget` and `lastResolvedHitIndex` to resolve hits independently for every target and hit index, fixing the bug where AOE skills only rolled hit checks once for the first target.
- Retained backward compatibility with manual `hasResolvedHit = true` overrides by falling back to caching of the current target/hit index upon first query.
- Verified that all 140 EditMode tests pass successfully.

# Turn Order Speed Randomization

## Phase 1: Implementation
- [x] Refactor `BuildTurnOrder` in `BattleSystem.cs` to roll random speed boost between 1-4 for each character and add it to their speed <!-- id: 89 -->

## Phase 2: Verification
- [/] Implement unit tests in `TurnOrderTests.cs` to verify randomized speed calculation and correct sorting <!-- id: 90 -->
- [ ] Run EditMode tests to verify all tests pass successfully <!-- id: 91 -->

# Stealth Status Effect

## Phase 1: Planning
- [x] Create implementation plan for Stealth status effect <!-- id: 92 -->
- [ ] Receive user review and approval on implementation plan <!-- id: 93 -->

## Phase 2: Core Implementation
- [ ] Add Stealth to `StatusType` enum in `SkillData.cs` and add `ignoresStealth` field <!-- id: 94 -->
- [ ] Implement `IsStealthed` helper property and damage-breaking logic in `CombatCharacter.cs` <!-- id: 95 -->
- [ ] Implement `StealthStatusInstance` subclassing `StatusEffectInstance` <!-- id: 96 -->
- [ ] Update status effect instantiation in effects strategies (`StatusEffect.cs`, `SelfStatusEffect.cs`, `AdjacentAllyStatusEffect.cs`, `ApplyStatusToGuardianEffect.cs`) <!-- id: 97 -->
- [ ] Refactor target filtering in `BattleSystem.GetValidTargets()` <!-- id: 98 -->

## Phase 3: Verification
- [ ] Implement `StealthTests.cs` unit tests <!-- id: 99 -->
- [ ] Verify all EditMode tests compile and pass successfully <!-- id: 100 -->
