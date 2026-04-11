# Refactor Notes

Planned architectural improvements to revisit when the time is right.

---

## Status Effect System → Self-Processing Statuses

**Priority:** Low (refactor when adding new status types or implementing ISkillEffect Phase 3.4)
**Files affected:** `CombatCharacter.cs`, `StatusEffectInstance.cs`, `BattleSystem.cs`

### Problem

`CombatCharacter` currently owns all status processing logic via switch statements in
`ApplyStartOfTurnEffects()` and `GetEffectiveStats()`. Adding a new status type requires modifying
CombatCharacter in multiple places (Open/Closed Principle violation).

### Proposed Solution

Make statuses self-processing via an interface:

```csharp
public interface IStatusEffect
{
    void OnTurnStart(CombatCharacter target);     // bleed → deal damage, restore → heal
    void ApplyModifier(CombatStats stats);         // buff → add to stat, debuff → subtract
}
```

CombatCharacter then delegates to each status instead of switching on type:

```csharp
public void ApplyStartOfTurnEffects()
{
    foreach (var status in statusEffects)
        status.OnTurnStart(this);
}

public CombatStats GetEffectiveStats()
{
    var effective = baseStats.Clone();
    foreach (var status in statusEffects)
        status.ApplyModifier(effective);
    return effective;
}
```

### Benefits

- **No switch statements** — each status type encapsulates its own behavior
- **Open/Closed** — adding a new status type requires zero changes to existing code
- **Consistency** — mirrors the `ISkillEffect` strategy pattern already planned for skills

### When to Do It

- When implementing `ISkillEffect` strategies (Phase 3.4 in todo.md) — natural moment to
  apply the same pattern to statuses
- Or when adding the 3rd+ new status type that requires touching both switch statements

### Why Not Now

The system has ~10 status types and works correctly at current scale. Refactoring now would
slow down getting the combat loop playable without adding gameplay value.
