# Plan: DOT (Bleed / Blight / Burn) Damage Scales With Applier Attack

Status: COMPLETED & VERIFIED (All 647 EditMode tests passing, documentation updated)
Audience: an AI agent implementing this with no further clarification. Everything below is binding unless marked "Agent judgement".

---

## 0. Resolved Decisions (from design review)

| # | Question | Decision |
| --- | --- | --- |
| Q1 | Burn escalation | Keep the flat **+1 to `damagePerTick`** before each tick (same timing as today). |
| Q2 | Re-authoring skill amplitudes | **Out of scope.** The designer re-tunes the 19 skill assets. Do NOT edit any `.asset` file. |
| Q3 | Rounding | Round to nearest, **midpoint rounds away from zero**, then `max(1, result)`. |
| Q4 | Zero attack | Applier with 0 effective attack **still deals 1 per tick** (the min-1 clamp always applies). |
| Q5 | Applier dies before tick | DOT **keeps ticking**. The damage is a frozen snapshot and never re-reads the applier. |
| - | Save/load | Statuses are **not serialized**. No serialization work is needed. |
| - | Call-site audit | Performed. Findings in section 3. |
| R1 | `BleedOnAttackStatusInstance` bypasses the factory | **Refactor it to call `StatusInstanceFactory.Create`** (step S5). After this, NO production code constructs a Bleed/Blight/Burn instance directly except inside the factory itself. |

Transitional note for the designer: until skill assets are re-tuned, existing amplitudes (1-5) read as 1-5% of attack, so DOT damage will collapse to 1 per tick. This is expected, not a bug.

---

## 1. The Rule (single source of truth)

When a Bleed, Blight, or Burn status instance is created:

```txt
effAttack     = applier.GetEffectiveStats().attack              # evaluated at the moment of application
damagePerTick = max(1, RoundAwayFromZero(effAttack * amplitude / 100.0))
```

- `amplitude` is the authored value, now meaning **percent of the applier's effective attack** (30 = 30%).
- `GetEffectiveStats()` already includes traits, trinkets, and active buffs/debuffs on the applier. Do not re-implement any stat math.
- Do NOT use: the 0.8-1.2 attack roll (`CombatCalculator.RollAttackDamage`), `ctx.skillScaling`, `ctx.damageMultiplier`, crit multiplier, skill `accuracyMod`, target defense.
- The result is stored in a new field `damagePerTick` on the status instance and never recomputed.
- Poison = `StatusType.Blight`. The three DOT types are `Bleed`, `Blight`, `Burn`.
- `Restore` (healing over time) is NOT a DOT here. It stays flat, driven by `amplitude`.

Worked examples (use these as test vectors):

| effAttack | amplitude | raw | damagePerTick |
| --- | --- | --- | --- |
| 10 | 30 | 3.0 | 3 |
| 8 | 25 | 2.0 | 2 |
| 10 | 25 | 2.5 | 3 (midpoint away from zero) |
| 0 | 50 | 0.0 | 1 (min clamp) |
| 3 | 10 | 0.3 | 1 (min clamp) |
| 15 (10 base, +50% attack buff) | 40 | 6.0 | 6 (base alone would give 4) |

Rounding must be implemented as `(int)System.Math.Round(effAttack * amplitude / 100.0, System.MidpointRounding.AwayFromZero)`. Do NOT use `Mathf.RoundToInt` here (it rounds midpoints to even, so 2.5 becomes 2).

---

## 2. Current Behaviour (verified in code, with line refs)

| Concern | Location | Today |
| --- | --- | --- |
| DOT application | `Assets/Scripts/Combat/Effects/StatusEffect.cs` L70-L87 | `finalAmplitude = amplitude` (or `amplitude * duration` with the burst key) is passed to `StatusInstanceFactory.Create` |
| Factory | `Assets/Scripts/Combat/Effects/StatusInstanceFactory.cs` L62-L68 | `default:` builds `new StatusEffectInstance(type, targetStat, amplitude, duration, amplitudeType)` then sets `Source = context.user` |
| Instance | `Assets/Scripts/Combat/StatusEffectInstance.cs` | Fields: `type, targetStat, amplitude, remainingDuration, amplitudeType`; props `Source`, `Host` |
| Tick | `Assets/Scripts/Combat/StatusProcessor.cs` L13-L53 | Burn: `amplitude += 1` (L15-L22); then groups periodic types and `TakeDamage(sum(amplitude))`; `Restore` heals via `character.Heal` |
| Periodic type test | `StatusProcessor.IsPeriodicType` L8-L11 | Bleed, Blight, Restore, Burn |
| Tick caller | `Assets/Scripts/Combat/BattleSystem.cs` L231 | `StatusProcessor.ProcessPeriodicEffects(CurrentActor)` |
| Tooltip | `Assets/Scripts/UI/StatusTooltipDisplay.cs` L157-L165 (aggregation) and L172-L174, L197-L198 (text) | Sums `amplitude` for all non-buff types; Bleed/Blight: `"{n} dmg for {d} turns"`; Burn: `"{n}dmg, dmg + 1 each turn, for {d} turns"` |
| Effective stats | `Assets/Scripts/Combat/CombatCharacter.cs` L277-L370 | Merges trait + trinket + status modifiers; attack clamped at >= 0 |
| Tick damage | `character.TakeDamage(n)` | Bypasses defense (unchanged; do not add defense) |

---

## 3. Call-Site Audit Report (performed; this is the factual basis for the steps)

Search: every `new StatusEffectInstance(`, every `: base(StatusType.`, every `AddStatus(`, every `StatusType.Bleed|Blight|Burn` reference under `Assets/`.

### 3.1 Production code that creates DOT instances

| File | Line | Creates DOT? | Action |
| --- | --- | --- | --- |
| `Combat/Effects/StatusInstanceFactory.cs` | L63 (`default:` branch) | YES, the generic path | **Resolve `damagePerTick` here** (covers `StatusEffect`, `SelfStatusEffect`, `AdjacentAllyStatusEffect`, `ApplyStatusToGuardianEffect`, which all call `Create`) |
| `Combat/Effects/BleedOnAttackStatusInstance.cs` | L61 `new StatusEffectInstance(StatusType.Bleed, _bleedAmplitude, _bleedDuration)` | **YES, BYPASSES THE FACTORY. Confirmed finding.** | **Refactor to `StatusInstanceFactory.Create(...)`** (step S5); damage is then resolved by the factory hook with no special code. `ctx.user == Host` is guaranteed here: `OnActionResolved` is raised in exactly one place (`SkillExecutor.cs` L171, `OnActionResolved?.Invoke(user, skill, ctx)`) and the handler returns early unless `actor == Host`. So the factory's `Source = context.user` and applier = `context.user` are both `Host`. No new factory parameter is needed. |
| `Combat/StatusEffectOnSpawn.cs` | L36 generic `new StatusEffectInstance(statusType, ...)` | Only if a designer picks a DOT type. **No current prefab does**: all 6 prefabs using it have `statusType` 10 (Stealth), 17 (LivingFragments), or 18 (LifeLink). | Defensive: resolve with the spawned `character` as applier so a DOT never silently becomes 0 |
| `Combat/StatusProcessor.cs` | L67 | No (creates a stun-recovery `Buff`) | None |

### 3.2 Other `AddStatus` callers (verified NOT DOT)
`ApplySkillBoostEffect`, `LivingFragmentsStatusEffect`, `TargetGuardsUserEffect`, `BleedOnAttackStatusEffect` (adds the `BleedOnAttack` buff, not Bleed; its `bleedAmplitude` is only passed along). No change needed other than the tooltip text in step S10.

### 3.3 Subclasses of `StatusEffectInstance`
`Guard, Flight, HealReceivedDebuff, Shuffle, Stealth, Unraveled, SkillBoost, LivingFragments, LifeLink, BleedOnAttack, Move`: none is Bleed/Blight/Burn. (`BleedOnAttackStatusInstance` has type `BleedOnAttack`, but it spawns Bleed instances; see 3.1.)

### 3.4 Burst key (`StatusBurst_{type}`)
Set by `Data/Trinkets/StatusBurstTrinketStrategy.cs` L28, consumed only in `StatusEffect.cs` L73-L78. It currently multiplies `amplitude` by `duration` for ANY listed type. See step S4 for the new DOT behaviour.

### 3.5 Existing tests that will break (direct construction of DOT instances with flat amplitude)
Tests live in `Assets/Editor/Tests/`. After the change a directly-constructed DOT has `damagePerTick = 0` and would tick for 0 / show a wrong tooltip.

| File | Lines | Must change? |
| --- | --- | --- |
| `PeriodicEffectTests.cs` | L96, L107-L108, L121, L133-L134, L146, L159, L176, L185 (Bleed/Blight/Burn) | **YES.** Set `damagePerTick`. Restore tests (L48, L59-L60, L71, L83, L158, L175, L186, L199) stay unchanged |
| `StatusIconTests.cs` | L296 (Bleed `"10 dmg for 3 turns"`), L315 (Burn text), L420-L421 (grouped Bleed) | **YES.** Set `damagePerTick` to the old amplitude so the expected strings stay valid |
| `TrinketTests.cs` | L365-L410 `StatusBurstTrinket_CompressesDurationAndMultipliesAmplitude` (asserts `amplitude == 9`) | **YES.** Rewrite assertion to `damagePerTick` |
| `BuffDebuffTests.cs` | L611-L614, L650-L652 (cleanse tests), L861 (riposte Bleed) | Review; presence/removal asserts only, expected to need no change |
| `GuardTests.cs` | L288-L373 | Review; presence asserts, expected to need no change |
| `ConditionalDamageEffectTests.cs` L201, `IncreaseBuffDurationTests.cs` L56, `StunTests.cs` L286-L291, `StatusIconTests.cs` L135-L245 (icon tests) | various | Review; amplitude not asserted, expected to need no change |

The agent must re-run `grep -n "StatusType\.\(Bleed\|Blight\|Burn\)" Assets/Editor/Tests` after editing, since the audit output was truncated for some files, and confirm each hit is handled.

---

## 4. Design

1. **New field** `public int damagePerTick;` on `StatusEffectInstance`. Meaningful only for DOT types. Default 0.
2. **`amplitude` is kept** on DOT instances as the authored percent (informational; not used by tick).
3. **One resolver**, placed in `StatusInstanceFactory` (static, public), so all paths share it:
   ```csharp
   public static int ResolveDotDamage(CombatCharacter applier, int amplitudePercent)
   // applier == null  -> treat effAttack as 0 (result 1); never throw
   ```
4. **One DOT predicate** `StatusProcessor.IsDotType(StatusType t)` = Bleed, Blight, or Burn. `IsPeriodicType` remains `IsDotType(t) || t == Restore` (behaviour unchanged).
5. **No fallback** from `damagePerTick` to `amplitude`. A DOT instance with `damagePerTick <= 0` is skipped at tick time (existing `<= 0 continue` guard). The min-1 clamp in the resolver guarantees real applications are never 0.
6. **Snapshot**: the resolver is called once at creation. Nothing later modifies `damagePerTick` except the Burn +1 escalation.
7. **Stacking**: instances of the same type are still summed per tick; each contributes its own `damagePerTick`.

---

## 5. Implementation Steps (execute in order)

Do not modify `.asset`, `.prefab`, or scene files. Preserve unrelated comments/docstrings.

### S1. `StatusEffectInstance.cs`
- Add `public int damagePerTick;` with an XML doc: "Resolved damage dealt each tick for Bleed/Blight/Burn. Snapshot of applier attack * amplitude% at application. Unused by other types."
- No constructor changes.

### S2. `StatusProcessor.cs` (predicate)
- Add `public static bool IsDotType(StatusType type)` returning true for Bleed, Blight, Burn.
- Rewrite `IsPeriodicType` as `IsDotType(type) || type == StatusType.Restore`.

### S3. `StatusInstanceFactory.cs` (resolver and hook)
- Add `ResolveDotDamage(CombatCharacter applier, int amplitudePercent)` per section 1 (null-safe, `MidpointRounding.AwayFromZero`, `Math.Max(1, ...)`).
- In `Create`, after `instance.Source = context.user;` add:
  `if (StatusProcessor.IsDotType(type)) instance.damagePerTick = ResolveDotDamage(context.user, amplitude);`
- The `Guard` early-return path and special subclasses are unaffected (none are DOT).

### S4. `StatusEffect.cs` (burst + log)
- Keep the burst semantic "one lump hit over one turn", now on damage:
  - For **DOT types**: pass the **authored** `amplitude` and `duration` to the factory (do NOT pre-multiply). After `Create`, if burst is active: `instance.damagePerTick *= duration; instance.remainingDuration = 1;`.
  - For **non-DOT types**: leave existing burst behaviour (`amplitude * duration`, duration 1) exactly as is.
- Update the log to print `dmg/tick:{instance.damagePerTick}` for DOT types, keeping the existing format for others.
- Update the `amplitude` field tooltip: "Power/Stack size. For Bleed/Blight/Burn this is the percent of the applier's effective Attack dealt per tick (e.g. 30 = 30%)."

### S5. `BleedOnAttackStatusInstance.cs` (refactor through the factory)
- Replace L61-L62:
  ```csharp
  var bleedInstance = new StatusEffectInstance(StatusType.Bleed, _bleedAmplitude, _bleedDuration);
  bleedInstance.Source = Host;
  ```
  with:
  ```csharp
  // Route through the factory so damagePerTick is resolved from Host's effective attack (ctx.user == Host).
  var bleedInstance = StatusInstanceFactory.Create(
      ctx,
      StatusType.Bleed,
      StatTarget.Speed,        // same targetStat the old 3-arg constructor used
      _bleedAmplitude,
      _bleedDuration,
      AmplitudeType.Default);  // same amplitudeType the old constructor used
  ```
- Keep unchanged: the `actor != Host`, `didHit`, `IsDamage` and `IsAlive` guards, the resistance roll via `CombatCalculator.ResolveStatusApplication`, `ctx.primaryTarget.AddStatus(bleedInstance)` and `TriggerStatusApplied(StatusType.Bleed, applied)`.
- Behaviour preserved: resulting instance has the same `type`, `targetStat`, `amplitude`, `duration`, `amplitudeType` and `Source` (= `ctx.user` = `Host`) as before. New: `damagePerTick` set by the factory. The burst key is NOT applied here (it was not before either; unchanged).
- Do NOT add parameters to `StatusInstanceFactory.Create`. Do NOT change `BleedOnAttackStatusEffect` logic; only update the `bleedAmplitude` tooltip to say it is percent of the host's attack.
- Tests that exercise this class must build the `SkillContext` with `user = Host` (the real flow does).

### S6. `StatusEffectOnSpawn.cs`
- In the generic `else` branch, after creating `instance`: `if (StatusProcessor.IsDotType(statusType)) instance.damagePerTick = StatusInstanceFactory.ResolveDotDamage(character, Mathf.RoundToInt(amplitude));` and set `instance.Source = character`. Add a one-line comment that no current prefab uses a DOT here.

### S7. `StatusProcessor.ProcessPeriodicEffects` (tick)
- Burn escalation: replace `status.amplitude += 1;` with `status.damagePerTick += 1;` (same timing: before damage; only non-expired Burn).
- In the group loop, compute the aggregate by type:
  - DOT types: `group.Sum(s => s.damagePerTick)`
  - `Restore`: `group.Sum(s => s.amplitude)` (unchanged)
- Keep: `if (aggregate <= 0) continue;`, `TakeDamage`/`Heal`, `TriggerPeriodicEffectApplied`, and the `if (!character.IsAlive) break;` guard.
- Do not touch `TickDurations`.

### S8. `StatusTooltipDisplay.cs`
- In the aggregation `else` branch (L157-L165): for DOT types use `Sum(s => s.damagePerTick)` instead of `Sum(s => s.amplitude)`; all other types unchanged.
- Strings in `FormatSingleTooltipText` stay textually identical (`"{n} dmg for {d} turns"`, `"{n}dmg, dmg + 1 each turn, for {d} turns"`) because `n` is now the damage. For DOT cases, the `aggregateAmplitude` parameter already carries that value after the change above. Do NOT add percent text.
- Verify the `BleedOnAttack` branch (L148-L156) is unaffected.

### S9. Editor inspector text
- `grep` for custom inspectors/drawers that label `StatusEffect.amplitude` (e.g. under `Assets/Scripts/Editor/`). If one exists for `StatusEffect`, update its label/help to the "percent of applier Attack" wording for DOT types. If none, skip.

### S10. Tests (see section 6) and docs (see section 7)

---

## 6. Tests (EditMode, in `Assets/Editor/Tests/`)

Conventions (from `tasks/lessons.md`): pass the **short class name** to `tests-run`; use `CombatTestHelper`; never use `DestroyImmediate(obj, true)`; give mock `SkillData` a `skillId`; save open scenes before `tests-run`.

Add a test helper in the new test class: `MakeDot(StatusType t, int dmgPerTick, int duration)` creating an instance and setting `damagePerTick` (used to repair legacy tests too).

### 6.1 Update existing tests
- `PeriodicEffectTests`: for each Bleed/Blight/Burn instance, set `damagePerTick` to the number previously used as amplitude; assertions stay numerically identical. Burn tests: expectation `(old amp + 1)` per instance unchanged (escalation now on `damagePerTick`). Restore tests untouched.
- `StatusIconTests`: set `damagePerTick` equal to the old amplitude on instances at L296, L315, L420-L421 so expected strings still hold.
- `TrinketTests.StatusBurstTrinket_...`: assert `remainingDuration == 1` and `damagePerTick == expectedPerTick * 3` where `expectedPerTick = ResolveDotDamage(_character, 3)`. Rename if needed to say "MultipliesDamagePerTick".

### 6.2 New test class `DotAttackScalingTests`
| Test | Setup | Expected |
| --- | --- | --- |
| Resolver_BaseAttack | attack 10, amp 30 | 3 |
| Resolver_MidpointAwayFromZero | attack 10, amp 25 | 3 |
| Resolver_MinClamp_LowAttack | attack 3, amp 10 | 1 |
| Resolver_ZeroAttack_StillOne | attack 0, amp 50 | 1 |
| Resolver_NullApplier | applier null, amp 50 | 1, no exception |
| Apply_UsesEffectiveAttack_BuffedApplier | attack 10 + `Buff Attack 50 Percentage` on applier, amp 40 | 6 (base alone: 4) |
| Apply_UsesEffectiveAttack_Debuffed | attack 10 with `Debuff Attack 50 Percentage`, amp 40 | 2 |
| Apply_UsesTraitOrTrinketAttack | trait/trinket attack modifier on applier | reflected in damage |
| Apply_IgnoresSkillScaling | skill `damagePercent` 50 vs 300, same amp | identical `damagePerTick` |
| Apply_IgnoresCritAndMultiplier | `ctx.isCritical = true`, `damageMultiplier = 2` | unchanged `damagePerTick` |
| Apply_NoAttackRoll_Deterministic | different RNG seeds | identical `damagePerTick` |
| Snapshot_NotAffectedByLaterBuff | apply, then buff applier attack | existing `damagePerTick` unchanged |
| Snapshot_ApplierDies_TicksContinue | apply, kill applier, process periodic | target still takes `damagePerTick` |
| Stacking_TwoAppliers | A attack 10 amp 30 (=3), B attack 20 amp 30 (=6), both Bleed on one target | one tick deals 9 |
| Burn_EscalatesByOneFlat | attack 10 amp 30 (=3), tick x3 | damage 4, then 5, then 6 |
| Burn_MultiStack_Independent | two Burn stacks (3 and 5) | first tick 4 + 6 = 10 |
| Burst_DotLump | burst key set, amp 30, duration 3, attack 10 | `damagePerTick == 9`, `remainingDuration == 1` |
| Burst_NonDot_Unchanged | burst key set on a Debuff | `amplitude * duration` as before |
| BleedOnAttack_UsesHostAttack | host attack 10, `_bleedAmplitude` 30, `bleedChance` 200, ctx with `user = Host`, raise `OnActionResolved` with a hit damage skill | applied Bleed `damagePerTick == 3`, `Source == Host`, `type == Bleed`, `targetStat == Speed`, `amplitude == 30` |
| BleedOnAttack_ResistanceStillApplies | target `bleedResist` 300 | no Bleed added; `OnStatusApplied` fired with `succeeded == false` |
| BleedOnAttack_ParityWithStatusEffect | same attacker attack and amplitude applied via `BleedOnAttack` and via `StatusEffect` | identical `damagePerTick` |
| Restore_StillFlat | Restore amplitude 15 | heals 15 regardless of attack |
| Tick_IgnoresDefense | target defense 50 | DOT damage unchanged by defense |
| Tooltip_ShowsResolvedDamage | Bleed `damagePerTick` 7, dur 2 | `"7 dmg for 2 turns"` |
| Tooltip_AggregatesDamage | two Bleed (4 and 3) | `"7 dmg for ..."` |
| NoDirectDotConstruction_InProductionCode | scan `Assets/Scripts/**/*.cs` for `new StatusEffectInstance(StatusType.(Bleed\|Blight\|Burn)` | **zero matches** (strict, no exemptions). The generic `new StatusEffectInstance(type, ...)` in the factory `default:` branch and in `StatusEffectOnSpawn` use a variable type, so they do not match the pattern |

### 6.3 Verification gate (must pass before reporting done)
1. Compile clean (no new warnings in touched files).
2. Run EditMode suites via `tests-run`: `PeriodicEffectTests`, `DotAttackScalingTests`, `StatusIconTests`, `TrinketTests`, `BuffDebuffTests`, `GuardTests`, `StunTests`, `ConditionalDamageEffectTests`, `IncreaseBuffDurationTests`, then the whole EditMode suite. All green.
3. Playtest check: apply Bleed from a known-attack character, confirm log line `dmg/tick:` and the HP tween at the next turn start, and that an enemy-applied Burn escalates +1 per turn.

---

## 7. Documentation Updates (after code and tests pass)

Update each spec's "Last verified" / commit fields per project convention.

| File | Change |
| --- | --- |
| `Docs/specs/mechanics/MECHANIC_DAMAGE_OVER_TIME.md` | Remove "percentage-based DoTs (all current are flat amplitude)" from Scope; add Burn to In scope; replace the Tick Value formula with section 1's formulas; add Inputs (applier effective attack at application); add snapshot, min-1, rounding, applier-death rules to Edge Cases; change the `amplitude` tuning row unit to "% of applier Attack"; add `damagePerTick` to the tuning table; mention `Restore` stays flat |
| `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md` | Add a DOT damage formula beside the damage formulas; state DOT ignores skill Damage Percent, roll, crit, and defense |
| `Docs/specs/systems/SYSTEM_SPEC_SKILLS_DATABASE.md` | `StatusEffect.amplitude` semantics for DOT types |
| `Docs/specs/systems/SYSTEM_SPEC_SKILL_CONTEXT_RUNTIME.md` | Status creation resolves applier attack in `StatusInstanceFactory`; burst applies to `damagePerTick` for DOT |
| `Docs/specs/systems/SYSTEM_SPEC_STATUS_TOOLTIP_DISPLAY.md` | DOT tooltips and aggregation use `damagePerTick` |
| `Docs/specs/systems/SYSTEM_SPEC_TRAIT_SYSTEM.md`, `SYSTEM_SPEC_TRINKETS.md` | Attack modifiers also scale DOT; Burst trinket now multiplies `damagePerTick` |
| `Docs/specs/systems/SYSTEM_SPEC_CHARACTER_DATABASE.md`, `SYSTEM_SPEC_ENEMY_TEMPLATE.md`, `SYSTEM_SPEC_MARIONETTE_TEMPLATE.md` | One-line note: `attack` also drives DOT damage; authored DOT amplitudes are percentages |
| `Docs/skills_bleed_blight_burn.md` | Rename the Amplitude column to "Amplitude (% of Attack, pending designer re-tune)"; do not change the numbers |

---

## 8. Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Balance collapse until designer re-tunes assets (all DOT amplitudes 1-5 become ~1 dmg) | Documented in section 0; not code-fixable here; communicate to designer in the final report |
| Late Marionette attack (18-26) makes percent DOT strong | Designer tuning; out of scope |
| A future code path builds a DOT directly and gets 0 damage | Strict `NoDirectDotConstruction_InProductionCode` test (zero exemptions after the S5 refactor) + XML doc on `damagePerTick` |
| S5 refactor changes BleedOnAttack behaviour | Same `targetStat`/`amplitudeType` are passed; parity and resistance tests in 6.2; `ctx.user == Host` proven by the single `OnActionResolved` raise site |
| Burn escalation is flat while base is attack-scaled | Accepted by decision Q1 |
| Hidden legacy tests (audit output was truncated) | Re-run the grep in 3.5 and the full EditMode suite |

---

## 9. Out of Scope
- Editing any skill/enemy/Marionette `.asset` or prefab.
- Restore (HoT) scaling.
- Changing defense interaction, crit, or accuracy for DOT.
- Live (non-snapshot) scaling.
- Serialization.
- Changing the application chance / resistance logic.

---

## 10. Review (fill after implementation)
- [x] All steps S1-S9 complete
- [x] Tests (6.1, 6.2) added/updated and green (26/26 tests in DotAttackScalingTests passing)
- [x] Full EditMode suite green (647/647 tests passing)
- [x] Docs (section 7) updated across all 10 target markdown specs and guides
- [x] Final report lists: files changed, test results, and the designer hand-off note

### Verification Results Summary
- **Unit Tests Added**: `Assets/Editor/Tests/DotAttackScalingTests.cs` (26 tests covering base resolver, rounding, zero-stat clamp, buff/debuff scaling, trait/trinket scaling, skill scaling/crit immunity, determinism, snapshots, applier death, stacking, Burn escalation, burst trinkets, BleedOnAttack procs, Restore independence, defense bypass, tooltips, and static scan for direct DOT construction).
- **Regression Suites**:
  - `DotAttackScalingTests`: 26 / 26 passed
  - `PeriodicEffectTests`: 12 / 12 passed
  - `StatusIconTests`: 10 / 10 passed
  - `TrinketTests`: 19 / 19 passed
  - `BuffDebuffTests`: 51 / 51 passed
  - `GuardTests`: 20 / 20 passed
  - `StunTests`: 16 / 16 passed
  - `ConditionalDamageEffectTests`: 7 / 7 passed
  - `IncreaseBuffDurationTests`: 3 / 3 passed
  - **Full EditMode Suite**: 647 / 647 passed (0 failed, 0 inconclusive)
- **Asset / Prefab Guard**: 0 `.asset` or `.prefab` files modified. All skill amplitudes remain in original states awaiting designer re-tuning.
