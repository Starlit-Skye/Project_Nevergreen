# Trinket System Implementation Tasks

- [x] Create Trinket Data Structures & Strategies
  - [x] `TrinketData.cs` - ScriptableObject base
  - [x] `TrinketDatabase.cs` - Registry of trinkets
  - [x] `TrinketEffectStrategy.cs` - Abstract effect base
  - [x] `StatModifierTrinketStrategy.cs` - Stat modifier implementation
  - [x] `LowHpDamageBonusTrinketStrategy.cs` - Target threshold bonus damage strategy
  - [x] `GuaranteedHitTrinketStrategy.cs` - Guaranteed hit strategy
- [x] Create Runtime combat wrapper
  - [x] `TrinketInstance.cs` - Runtime instance container
- [x] Modify core models & serialization
  - [x] `PartyMemberInfo.cs` - Equip/unequip rules, 2 max cap, uniqueness, cannot remove cursed
  - [x] `SaveManager.cs` - Support serializing/deserializing equipped trinkets
  - [x] `GameDatabase.cs` - Include TrinketDatabase register
- [x] Integrate into Combat Runtime
  - [x] `CombatCharacter.cs` - Load and modify stats during combat initialization
  - [x] `BattleSystem.cs` - Event hook for target-specific damage calculation
  - [x] `DamageEffect.cs` - Trigger target-specific event hook during execute stage
- [x] Write Specification & Tests
  - [x] Create spec file `Docs/specs/systems/SYSTEM_SPEC_TRINKETS.md`
  - [x] Create unit tests in `Assets/Editor/Tests/TrinketTests.cs`
  - [x] Run edit mode tests and verify success

## Review & Results

- **Modular Trinket System**: Fully implemented and validated.
- **Unit Testing**: Tests verified capacity limitations (max 2), equipping uniqueness, cursed item locking (cannot unequip), stat calculations under Banker's Rounding, and BattleSystem event mutators (Guaranteed Hit strategy).
- **Editor Menu Alignment**: Trinket Database was moved from `Nevergreen/Data/Trinket Database` to the `Nevergreen/Databases/Trinket Database` menu structure to sit alongside all other databases.
- **UI & Tooltips**: UI hooks, display controllers, and tooltip triggers/panels were created and integrated into the main party management interface.
