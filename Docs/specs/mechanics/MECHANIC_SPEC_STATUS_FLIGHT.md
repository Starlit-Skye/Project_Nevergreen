# Status Effect: Flight

Owner: Combat Engineering Team
Status: active
Last verified: 2026-10-03
Target build: Unity 6000.3.9f1 Standalone Windows

## Purpose
The Flight status effect grants a character an increased chance to dodge incoming attacks by providing a flat boost to their Dodge stat equal to the status amplitude. It acts as an evasive maneuver that persists across turns until the character is grounded (struck by a damaging attack).

## Scope
- In scope: Flat Dodge boost, event-driven removal triggered by taking damage from an attack skill (including Riposte counter-attacks).
- Out of scope: Visual rendering of flying animations, specific skill animations.

## Source of Truth
- Code: 
  - `Assets/Scripts/Data/SkillData.cs` (`StatusType.Flight`)
  - `Assets/Scripts/Combat/Effects/FlightStatusInstance.cs` (Lifecycle and removal logic)
  - `Assets/Scripts/Combat/CombatCharacter.cs` (`GetEffectiveStats` Dodge calculation)
  - `Assets/Scripts/Combat/Effects/StatusEffect.cs` (Instantiation strategy)
- Tests: `Assets/Editor/Tests/FlightTests.cs`
- Data: `Assets/Scripts/Data/SkillData.cs` (`StatusType` enum contains `Flight`)

## Inputs
- Trigger condition: Character is targeted by an attack skill that hits them and deals damage.
- Stat modification: The character's Dodge stat receives a flat addition.

## State Model
States:
- `Grounded`: Character does not have the Flight status effect.
- `Airborne`: Character has the Flight status effect, boosting Dodge.

Transitions:
1. `Grounded` -> `Airborne` when a skill executes a `StatusEffect` that applies `StatusType.Flight`.
2. `Airborne` -> `Grounded` when the Flight status duration expires.
3. `Airborne` -> `Grounded` when the character is hit by an attack that deals damage.

## Timing Model
- Update domain: Combat action resolution and character stat recalculation.
- Removal timing: Flight is removed in `BattleSystem.OnActionResolved` event handler. The check evaluates after hit calculation and damage application.
- Stat update timing: `CombatCharacter.GetEffectiveStats()` calculates the Dodge bonus dynamically whenever stats are queried.

## Determinism
- Deterministic: Yes, the removal relies on discrete combat system hit/damage results.

## Formulas & Calculations
1. **Dodge Scaling**:
   `Effective Dodge = Base Dodge + Flat Modifiers (Buffs/Debuffs) + Flight Amplitude`
   The final value is hard capped at `CombatConfig.dodgeCap` (default 95%).

## Edge Cases
- **Missed Attacks**: If an attack targets a character with Flight and misses (`didHit == false`), Flight is NOT removed.
- **Non-Damaging Skills**: Skills that do not deal damage (e.g. debuffs, stun-only skills with `IsDamage == false`) do NOT remove Flight, even if they hit the target.
- **Damage Over Time (DoT)**: Taking damage from periodic effects like Bleed, Blight, or Burn does NOT remove Flight, as they bypass attack action resolution.
- **Riposte**: A Riposte counter-attack executes as a standard attack skill. If it hits and deals damage, it WILL remove Flight.
- **Multiple Stacks**: Since statuses are handled as a list of instances, multiple Flight effects stack their Dodge boosts. Any valid damage hit will remove all active Flight instances that subscribed to the event.

## Acceptance Tests
- Automated unit tests in `Assets/Editor/Tests/FlightTests.cs`:
  - `Flight_GrantsFlatDodgeIncrease`: Verifies Dodge increases by amplitude when active.
  - `Flight_RemovedWhenHitByDamageSkill`: Verifies damaging attack hit removes Flight status.
  - `Flight_NotRemovedWhenAttackMisses`: Verifies missed attack does not remove Flight.
  - `Flight_NotRemovedWhenHitByNonDamageSkill`: Verifies non-damaging skill hit does not remove Flight.
  - `Flight_NotRemovedByDamageOverTime`: Verifies periodic DoT damage does not remove Flight.
  - `Flight_RemovedByRiposteDamageHit`: Verifies Riposte counter-attack hit that deals damage removes Flight.

## Validation
- [x] Facts match current code/content
- [x] Timing, authority, and determinism are explicit
- [x] Unknowns are explicitly labeled
- [x] Acceptance tests are defined
