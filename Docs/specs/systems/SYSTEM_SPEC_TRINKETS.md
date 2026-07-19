# SYSTEM_SPEC_TRINKETS

## Overview
The Trinket System provides equippable items that grant passive stat bonuses and dynamic combat effects to player characters (Marionettes). Trinkets expand the customization layer by allowing players to mix and match modular strategies, similar to the Trait system.

## Key Rules
1. **Capacity Cap**: A character can equip a maximum of 2 Trinkets.
2. **Uniqueness**: A character cannot equip multiple instances of the same Trinket (checked via `trinketId`).
3. **Cursed Trinkets**: If a Trinket's `cannotBeRemoved` flag is true, it cannot be unequipped once attached.
4. **Serialization**: Equipped Trinket IDs are serialized in `SaveManager` (`equippedTrinketIds` in `PartyMemberDTO`) and resolved at load time via the `TrinketDatabase`.

## Core Data Structures

### `TrinketData` (ScriptableObject)
Defines the unchanging metadata for a Trinket:
- `trinketId` (string)
- `displayName` (string)
- `description` (string)
- `cannotBeRemoved` (bool)
- `effectStrategies` (List<TrinketEffectStrategy>)

### `TrinketInstance` (Runtime Wrapper)
Instantiated at the start of combat in `CombatCharacter.InitializeForCombat`. Tracks dynamic state such as event closures.

## Trinket Strategies
Trinket behaviors are defined using a polymorphic strategy pattern leveraging `[SerializeReference]`.

### StatModifierTrinketStrategy
Grants flat and percentage-based modifiers to core stats (e.g., +10 MaxHP, +20% Attack). These are aggregated during `CombatCharacter.GetEffectiveStats()` alongside Traits.

### GuaranteedHitTrinketStrategy
Subscribes to `BattleSystem.OnBeforeDamageCalculation` when the owner uses an attack. Overrides the `SkillContext.guaranteedHit` flag to true, ensuring the attack cannot miss.

### LowHpDamageBonusTrinketStrategy
Subscribes to `BattleSystem.OnBeforeDamageCalculationPerTarget`. Applies a bonus multiplier to the attack's damage if the specific target's HP falls below a defined threshold (e.g., +20% damage to targets under 50% HP). The context multiplier is dynamically restored after calculating the individual target to prevent polluting multi-target AOE strikes.
