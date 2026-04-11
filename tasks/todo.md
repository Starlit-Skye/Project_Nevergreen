# Implementation Plan: Character System + Combat Core + Test Combat Screen

## Overview
Build the character data system, core combat mechanics, and a prototype combat scene per the GDD and spec docs.

## Phase 1: Data Layer (ScriptableObjects)
- [x] 1.1 Create `StatBlockData` ScriptableObject (Attack, Defense, Accuracy, Dodge, CritChance, Speed, MaxHP)
- [x] 1.2 Create `CharacterData` ScriptableObject (id, displayName, statPerLevel list)
- [x] 1.3 Create `SkillModifier` data class (Damage%, Heal%, Accuracy+, Critical+)
- [x] 1.4 Create `SkillData` ScriptableObject (id, displayName, modifiers, useRanks, targetRanks, maxTargets, targetScope)
- [x] 1.5 Create `CombatConfig` ScriptableObject (tuning variables)

## Phase 2: Runtime Character
- [x] 2.1 Create `CombatStats` runtime class (resolved stats with buff/debuff modifiers)
- [x] 2.2 Create `CombatCharacter` MonoBehaviour (runtime combat entity)
- [x] 2.3 Create `StatusEffectInstance` data model (type, amplitude, duration)

## Phase 3: Combat Core
- [x] 3.1 Create `SkillContext` runtime class (per-execution mutable data container)
- [x] 3.2 Create `BattleSystem` MonoBehaviour (round/turn state machine)
- [x] 3.3 Create `CombatCalculator` static utility (damage/hit/crit formulas)
- [ ] 3.4 Create `ISkillEffect` strategy interface + default implementations (deferred - not needed for prototype)

## Phase 4: Combat Screen Prototype
- [x] 4.1 Create `CombatSceneBootstrap` MonoBehaviour (spawns teams from prefab lists)
- [x] 4.2 Create `CombatPrototype` Unity scene with spawn points and UI
- [x] 4.3 Create `CombatUI` + `HPBar` (skill buttons, stats panel, HP bars, battle log)
- [x] 4.4 Wire input: skill select -> target select -> execute
- [x] 4.5 Enemy AI: random valid skill selection

## Phase 5: Test Data
- [x] 5.1 Create sample ScriptableObject assets (stats, skills, characters)
- [x] 5.2 Create character prefabs with CombatCharacter configured
- [x] 5.3 Set up CombatSceneBootstrap with test prefab lists

## Verification
- [ ] Teams spawn at correct rank positions in play mode
- [x] Player can select skills and targets
- [ ] Combat resolves turns in speed order
- [x] HP changes reflect correctly
- [ ] Battle ends when one side is eliminated
