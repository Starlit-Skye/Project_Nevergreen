# Goal: Implement Status Effect Icons above HP Bars

## Tasks
- [x] 1. Expose `BleedChance` on `BleedOnAttackStatusInstance`.
- [x] 2. Format tooltip texts dynamically in `StatusTooltipDisplay.cs`.
- [x] 3. Include `[Skillboosted] + [amplitude]% dmg` formatting for `SkillBoostStatusInstance`.
- [x] 4. Run tests to ensure correctness.
- [x] 5. Document results.

## Results
- **Implementation**: The HPBar status icons now display dynamic, data-driven tooltip strings matching GDD format. Skillboost tooltips also dynamically display the boosted skill's name using game databases/character references to look up the string dynamically.
- **Test Results**: All 395 EditMode unit tests have successfully passed.
