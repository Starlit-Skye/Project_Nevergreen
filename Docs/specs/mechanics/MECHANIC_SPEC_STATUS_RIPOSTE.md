# Status Effect: Riposte

Owner: Combat Engineering Team
Status: proposed
Last verified: 2026-05-22
Target build: Unity 2022.3 + Windows

## Purpose
The Riposte status effect allows a character to automatically counter-attack any attacker who targets them with a hostile action, regardless of whether the attack hits or misses, or deals damage.

## Scope
- In scope: Counter-attack trigger conditions, standard combat calculation scaling based on status amplitude, stun checks (both pre-existing and applied by the triggering attack), and prevention of circular counter-attacks.
- Out of scope: Visual rendering of status icons.

## Source of Truth
- Code: `Assets/Scripts/Combat/BattleSystem.cs` (Riposte execution loop), `Assets/Scripts/Data/SkillData.cs` (`StatusType.Riposte`), `Assets/Scripts/Combat/Effects/StatusEffect.cs` (standard application rules).
- Data: `Assets/Scripts/Data/SkillData.cs` (`StatusType` enum contains `Riposte`)

## Inputs
- Trigger condition: Character is targeted by an attack skill (where `skill.targetScope == TargetScope.Enemies` and `skill.skillId != "riposte_counter"`).
- Target of counter-attack: The original attacker.

## State Model
States:
- `Inactive`: Character does not have the Riposte status effect.
- `Active`: Character has the Riposte status effect and will counter-attack when targeted.

Transitions:
1. `Inactive` -> `Active` when a skill executes a `StatusEffect` that applies `StatusType.Riposte`.
2. `Active` -> `Inactive` when the Riposte status duration expires (ticked down via `StatusProcessor.TickDurations` on the character's turn).

## Timing Model
- Update domain: combat action resolution.
- Trigger timing: Counter-attacks are evaluated and queued at the end of the attacker's skill execution in `BattleSystem.ExecuteSkill`, after all hits and skill effects have been fully resolved.
- Animation sequencing: Riposte counter-attacks are queued sequentially in the `BattleSystem.animationQueue` after the triggering attack's animation batch completes, ensuring clear visual separation.

## Determinism
- Deterministic: Yes, the counter-attack resolves using the standard battle system and deterministic RNG seed.

## Formulas & Calculations
1. **Damage Scaling**:
   The counter-attack is executed as a dynamically generated `SkillData` instance with `skillId = "riposte_counter"`.
   ```csharp
   riposteSkill.modifier = new SkillModifier
   {
       damagePercent = amplitude / 100f // Amplitude is percentage scaling of base attack
   };
   ```
2. **Combat Calculation**:
   The counter-attack uses standard formulas from `CombatCalculator`:
   - **Hit Check**: `ResolveHit` evaluates the counter-attack's hit chance based on the counter-attacker's accuracy and the target's dodge.
   - **Damage Roll**: Uses standard attack roll (`0.8` to `1.2` multiplier of Attack stat) scaled by the skill modifier's `damagePercent`.
   - **Crit Check**: Evaluated against the counter-attacker's critical strike chance.
   - **Defense Reduction**: Reduced by the target's defense stat (unless the skill ignores defense).

## Edge Cases
- **Stunned State**:
  - If a character is stunned *before* the attack, the counter-attack does not trigger.
  - If the attack itself applies Stun to the character, the stun is applied during the skill's effects phase (prior to the Riposte check). The check will detect the newly applied stun (`isStunned == true`) and skip the counter-attack.
- **Dead State**:
  - If the character dies from the attack, they cannot counter-attack.
  - If the original attacker dies before the counter-attack executes (e.g. from a separate effect or another counter-attack), the counter-attack fails safely.
- **Circular Counter-Attacks**:
  - Riposte counter-attacks use a skill with `skillId = "riposte_counter"`.
  - The Riposte trigger checks `skill.skillId != "riposte_counter"`. This guarantees that a counter-attack cannot trigger a secondary counter-attack, preventing infinite loops.
- **Guarded Targets**:
  - If target A has Riposte and is guarded by B, and B has Riposte:
    - B intercepts the attack (becomes the `primaryTarget`).
    - B receives the attack, and B triggers Riposte (if B is not stunned/dead). A does not trigger Riposte, as A was not the final target of the attack.
  - If the counter-attack targets the original attacker, and that attacker is currently guarded, the counter-attack can be redirected to the guardian like a normal attack.

## Acceptance Tests
- Automated unit tests in `Assets/Editor/Tests/RiposteTests.cs`:
  - `Riposte_TriggersCounterAttack_OnAttackTargeted`: Verifies that a character with Riposte counter-attacks the attacker, applying correct damage scaling.
  - `Riposte_DoesNotTrigger_IfStunned`: Verifies that a character with Riposte does not counter-attack if they are already stunned.
  - `Riposte_DoesNotTrigger_IfStunnedByAttack`: Verifies that if the incoming attack applies Stun, the counter-attack is skipped.
  - `Riposte_SubjectToHitCritDefense`: Verifies that the counter-attack can miss/crit and is reduced by defense.
  - `Riposte_NoCircularTrigger`: Verifies that counter-attacks do not trigger further counter-attacks.
  - `Riposte_GuardRedirection`: Verifies that if a guarded ally is targeted, only the guardian triggers Riposte (if they have Riposte).
