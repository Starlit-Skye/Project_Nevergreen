# Implementation Plan: Flight Status Effect

## Specs & Design
- [x] Create `Docs/specs/mechanics/MECHANIC_SPEC_STATUS_FLIGHT.md` detailing Flight status mechanics, formulas, triggers, and edge cases
- [x] Update `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` status list references

## Data & Enums
- [x] Add `Flight` entry to `StatusType` enum in `Assets/Scripts/Data/SkillData.cs`

## Combat System & Status Effects
- [x] Create `FlightStatusInstance.cs` in `Assets/Scripts/Combat/Effects/` inheriting from `StatusEffectInstance`
  - [x] Implement `OnAdded` / `OnRemoved` event subscriptions to `BattleSystem.OnActionResolved`
  - [x] Implement `HandleActionResolved` logic: check `target == Host`, `ctx.didHit`, `skill.modifier.IsDamage`, and `ctx.calculatedValue > 0`
  - [x] Trigger `Host.RemoveStatus(this)` when valid damaging attack hit occurs
- [x] Update `CombatCharacter.cs` `GetEffectiveStats()` to aggregate `StatusType.Flight` amplitude into flat `Dodge` bonus (`netFlat[StatTarget.Dodge]`)
- [x] Update `StatusEffect.cs` `Execute()` strategy to instantiate `FlightStatusInstance` when `statusType == StatusType.Flight`

## Unit Tests
- [x] Create `Assets/Editor/Tests/FlightTests.cs` verifying:
  - [x] `Flight_GrantsFlatDodgeIncrease`: Dodge increases by amplitude when status is active
  - [x] `Flight_RemovedWhenHitByDamageSkill`: Damaging attack hit removes Flight status
  - [x] `Flight_NotRemovedWhenAttackMisses`: Missed attack does not remove Flight
  - [x] `Flight_NotRemovedWhenHitByNonDamageSkill`: Non-damaging skill hit (e.g. debuff/stun) does not remove Flight
  - [x] `Flight_NotRemovedByDamageOverTime`: Periodic DoT damage does not remove Flight
  - [x] `Flight_RemovedByRiposteDamageHit`: Riposte counter-attack hit that deals damage removes Flight

