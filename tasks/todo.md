# Goal: Aggregating Status Effect Amplitudes in Tooltips

## Tasks
- [x] 1. Implement amplitude/customAmplitude aggregation helper `GetAggregateAmplitude` in `StatusTooltipDisplay.cs`.
  - [x] Support Bleed, Blight, Burn, Restore, HealReceivedReduction aggregation (by status type).
  - [x] Support SkillBoostStatusInstance aggregation (by targetSkillId).
  - [x] Support Buff, Debuff aggregation (by type and targetStat).
- [x] 2. Update `FormatTooltipText` in `StatusTooltipDisplay.cs` to format tooltips using the aggregated amplitude values.
- [x] 3. Add Riposte tooltip case returning `"Counter when attacked"`.
- [x] 4. Write unit tests in `StatusIconTests.cs` to verify aggregation behavior for Bleed, Blight, Burn, Restore, HealReceivedReduction (same type), SkillBoostStatusInstance (same targetSkillId), Buff/Debuff (same targetStat), and Riposte tooltip.
- [x] 5. Run all tests to ensure compilation and correctness.
- [x] 6. Document results.
