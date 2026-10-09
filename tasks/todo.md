# Implementation Plan - Option A: Living Fragments on StatusEffectOnSpawn + Custom Inspector

**Status:** ✅ **COMPLETED**
- Changes applied to `StatusEffectOnSpawn.cs` and `LivingFragmentsStatusInstance.cs`.
- `StatusEffectOnSpawnEditor.cs` created.
- Tests updated in `StatusEffectOnSpawnTests.cs` and successfully passed (552/552).

Allow level designers and developers to specify which prefabs to summon when configuring a `LivingFragments` status effect on `StatusEffectOnSpawn`, while keeping the Unity Inspector clean and context-sensitive.

## Proposed Changes

### 1. `Assets/Scripts/Combat/StatusEffectOnSpawn.cs`
- Add `public List<GameObject> livingFragmentPrefabs = new List<GameObject>();`
- In `ApplyTo(CombatCharacter character)`:
  - Add branch for `statusType == StatusType.LivingFragments` creating `new LivingFragmentsStatusInstance(null, livingFragmentPrefabs, duration)`.

### 2. `Assets/Scripts/Combat/Effects/LivingFragmentsStatusInstance.cs`
- In `ExecuteSummons()`:
  - Add fallback lazy lookup: `if (_battleSystem == null) _battleSystem = UnityEngine.Object.FindFirstObjectByType<BattleSystem>();`
  - Ensures on-spawn applied status instances can successfully find the `BattleSystem` when the host dies during combat.

### 3. `Assets/Scripts/Editor/StatusEffectOnSpawnEditor.cs` (New Custom Inspector)
- Create Custom Inspector for `StatusEffectOnSpawn`.
- Dynamically show/hide fields based on `statusType`:
  - When `LivingFragments`: display `statusType`, `duration`, and `livingFragmentPrefabs`. Hide `amplitude`, `amplitudeType`, `targetStat`.
  - When `Stealth`: display `statusType`, `duration`. Hide `amplitude`, `amplitudeType`, `targetStat`, `livingFragmentPrefabs`.
  - Otherwise: display standard `statusType`, `duration`, `amplitude`, `amplitudeType`, `targetStat`. Hide `livingFragmentPrefabs`.

### 4. `Assets/Editor/Tests/StatusEffectOnSpawnTests.cs`
- Add test verifying `StatusEffectOnSpawn` with `StatusType.LivingFragments` creates a `LivingFragmentsStatusInstance` on the target character.
- Add test verifying lazy `BattleSystem` lookup works on host destruction when initialized via `StatusEffectOnSpawn`.

## Verification Plan

### Automated Tests
- Run `StatusEffectOnSpawnTests` and all combat tests via Unity Test Framework.
- Ensure all 551+ existing tests plus new tests pass.

### Manual Verification
- Inspect a GameObject with `StatusEffectOnSpawn` in Unity Editor to verify the custom inspector properly toggles field visibility when changing `statusType`.
