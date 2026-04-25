# Task: List Mechanics Requiring Testing Suites

## Plan
1. [x] Analyze `Docs` directory for mechanic and system specifications.
2. [x] Read Lead QA agent profile to understand testing suite requirements.
3. [x] Filter out economy-related mechanics (linked to `SYSTEM_SPEC_ECONOMY_RUNTIME.md`).
4. [x] Identify core mechanics requiring automation based on "Acceptance Tests" sections.
5. [x] Generate the final list of mechanics and their testing requirements.

## Results
- **Final List**: [mechanics_testing_suites.md](file:///C:/Users/Admin/.gemini/antigravity/brain/21a3b76b-899f-4af3-ab0f-83f98a5e5c40/mechanics_testing_suites.md)

## Mechanics to be included:
- **Buffs & Debuffs**: Additive percentage multipliers (10% + 20% = 30% of base), duration stacking/ticking.
- **Hit & Crit Chance**: Base accuracy vs dodge, crit chance additives, final damage scaling.
- **Stun Mechanics**: Turn skip behavior, stun resistance checks, post-stun resistance buff (+300%).
- **Damage Over Time (DoT)**: Resistance, duration, turn-start ticks, lethal DoT.
- **Combat Core**: Turn order (Speed), Ranks, Speed ties.
- **Combat Input & Targeting**: Skill selection, target validation, animation locks, safeguards.
- **Skill Context Runtime**: Multi-hit resolution, status queueing.
- **Character Database**: Level-based stat resolution, global max level enforcement.
- **Skills Database**: Rank use/target constraints, target scope, modifier rules.
- **Animation Runtime**: FIFO queueing, input lock consistency, system state gating (pausing events/changes), overflow/overtime safeguards.

## Excluded (Economy-related):
- Battle Reward Drops
- Parts Level-Up
- Economy Runtime System
- Guard Mechanics (Bypass, redirection)

## Test Implementation
- [x] **Buff & Debuff Tests** (14 tests) — `Assets/Editor/Tests/BuffDebuffTests.cs`
  - Percentage-based modification, additive stacking, debuff resistance, duration ticking
  - Implementation fix: refactored `GetEffectiveStats()` from flat addition to percentage multipliers
- [x] **Hit & Crit Tests** (10 tests) — `Assets/Editor/Tests/HitCritTests.cs`
  - Accuracy vs dodge, 95% cap, skill mods, guaranteed hit, crit multiplier, defense pipeline
- [x] **Stun Tests** (9 tests) — `Assets/Editor/Tests/StunTests.cs`
  - Turn skip, duration timing, +300% recovery buff, stun resistance checks

**Result: 33/33 tests passing** ✅
