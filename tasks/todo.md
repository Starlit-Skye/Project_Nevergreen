# Elite Formations, EliteEncounter Strategy & Trinket Rewards

## Enums and Data Structures
- [x] Add `Elite` to `EnemyEncounterTier.cs`
- [x] Add `eliteFormations` and update `GetFormations()` in `EnemyFormationDatabase.cs`
- [x] Add trinket rarity drop chance fields (`eliteTrinketCommonWeight`, etc.) to `CombatConfig.cs`

## Room Strategies
- [x] Add virtual property `OverrideEncounterTier` to `RoomEffectStrategy.cs`
- [x] Create new file `EliteEncounterRoomEffectStrategy.cs`

## Rewards and Combat Logic
- [x] Update `BattleRewardHandler.cs` to add `RollEliteTrinketReward` (two-step roll) and update `ApplyVictoryRewards`
- [x] Update `BattleSystem.cs` to read `OverrideEncounterTier`, call updated `ApplyVictoryRewards`, and store `TrinketGrantedThisBattle`
- [x] Update `CombatSceneBootstrap.cs` in `SpawnTeams()` to read `OverrideEncounterTier`

## UI Updates
- [x] Update `CombatRewardUI.cs` to support displaying a granted Trinket
- [x] Update `CombatUI.cs` to pass the trinket from `BattleSystem` to `CombatRewardUI`

## Specs and Tests
- [x] Update `SYSTEM_SPEC_ENEMY_FORMATION_RANDOMIZATION.md`
- [x] Update `SYSTEM_SPEC_ROOM_SELECTION.md`
- [x] Update `SYSTEM_SPEC_TRINKETS.md`
- [x] Update `MECHANIC_SPEC_COMBAT_CORE.md`
- [x] Add/Update tests in `EnemyFormationSelectionTests.cs`, `RoomEffectTests.cs`, `CombatSceneBootstrapFormationTests.cs`, `TrinketTests.cs`
