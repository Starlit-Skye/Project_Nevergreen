# Economy Runtime System

> **Owner:** Gameplay Engineering Team | **Last Updated:** 2026-08-18 | **Status:** Active

## Purpose
Define runtime currency earning, event broadcasting, and spending rules for `Parts` and `Scraps` during a run session.

## Scope
- In scope: battle-end drops, encounter tier reward scaling (`TierRewardProfile`), economy state broadcasting (`OnPartsChanged`, `OnScrapsChanged`), transactional spending (`TrySpendParts`, `TrySpendScraps`), level-up spending, UI display (`EconomyDisplayUI`), run reset
- Out of scope: shop inventory design, event catalog design, exact visual assets

## Source of Truth
- Code: `Assets/Scripts/Data/RunSessionManager.cs`, `Assets/Scripts/Data/CombatConfig.cs`, `Assets/Scripts/Combat/BattleRewardHandler.cs`, `Assets/Scripts/UI/EconomyDisplayUI.cs`
- Tests: `Assets/Editor/Tests/EconomySystemTests.cs`
- Design: `Docs/specs/systems/SYSTEM_SPEC_RUN_SESSION_MANAGER.md`, `Docs/specs/systems/SYSTEM_SPEC_SAVE_GAME_PERSISTENCE.md`

## Responsibilities
- Maintain two run-scoped currencies: `Parts` and `Scraps`.
- Provide transactional methods (`GrantParts`, `TrySpendParts`, `GrantScraps`, `TrySpendScraps`) to safely alter currency balances.
- Broadcast static events (`OnPartsChanged`, `OnScrapsChanged`) when balances are granted or spent.
- Grant `Parts` and `Scraps` at battle end scaled per encounter difficulty tier using `TierRewardProfile` mappings in `CombatConfig`.
- Support UI component binding (`EconomyDisplayUI`) via events and initial state queries on `Start()` and `OnEnable()`.
- Spend `Parts` for leveling Marionettes / Party members.
- Spend `Scraps` for crafting, events, or shop purchases.
- Reset `Parts` and `Scraps` balance and clear static events upon run reset (`ClearAll()`).

## Data Model
- `RunSessionManager` (static class): Tracks `Parts` and `Scraps`.
- `TierRewardProfile`: Defines `minParts`, `maxParts`, `minScraps`, `maxScraps` per `EnemyEncounterTier`.
- `SaveDataDTO`: Persists `parts` and `scraps` in `run.dat`.

## Event Contracts
- Event: `RunSessionManager.OnPartsChanged`
  - Producer: `RunSessionManager` (`GrantParts`, `TrySpendParts`)
  - Consumers: `EconomyDisplayUI` and other UI/progression components
- Event: `RunSessionManager.OnScrapsChanged`
  - Producer: `RunSessionManager` (`GrantScraps`, `TrySpendScraps`)
  - Consumers: `EconomyDisplayUI` and other UI/progression components

## Acceptance Tests
- Automated: `Assets/Editor/Tests/EconomySystemTests.cs`:
  - `Scraps_Initialize_SetsToZero`
  - `GrantScraps_IncreasesBalance`
  - `TrySpendScraps_SucceedsIfEnoughBalance`
  - `TrySpendScraps_FailsIfNotEnoughBalance`
  - `SaveManager_SavesAndRestoresScraps`
  - `CombatConfig_GetRewardRanges_ReturnsProfileValues`
  - `CombatConfig_GetRewardRanges_FallsBackToDefaults`
  - `BattleRewardHandler_ApplyVictoryRewards_GrantsScrapsBasedOnTier`



