# Todo: Fix Combat Log Calculations for Multi-Effect Skills

## Tasks
- [x] 1. Reset `calculatedValue` and `isCritical` per target iteration in `BattleSystem.cs`.
- [x] 2. Update `CombatCalculator.cs` to return calculated values without overwriting `ctx.calculatedValue`, and remove crit roll (now done by BattleSystem).
- [x] 3. Update `DamageEffect.cs` and `ConditionalDamageEffect.cs` to accumulate `context.calculatedValue`.
- [x] 4. Update `HealEffect.cs` and `HealGuardianEffect.cs` to accumulate `context.calculatedValue`.
- [x] 5. Fix existing `HitCritTests` that relied on `CalculateDamage` rolling crit.
- [x] 6. Write new `CumulativeDamageTests` verifying accumulation, resets, and shared crit.
- [x] 7. Run all unit tests to confirm success — 340/340 passed.
