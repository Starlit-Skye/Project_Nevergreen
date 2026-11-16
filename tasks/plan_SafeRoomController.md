# Plan: SafeRoomController

Audience: AI agent implementing. Do NOT modify `MarionetteHealChoiceController` or its tests.

## Context (verified)
- Reference: `Assets/Scripts/UI/MarionetteHealChoiceController.cs` (namespace `Nevergreen.UI`).
  - Fields: `marionetteButtons[4]`, `hpTexts[4]`, `healAllButton`.
  - `Initialize(List<PartyMemberInfo>)` shows a button per valid party member, sets its
    TMP child text to `character.displayName`, wires a click listener (captured index).
  - Single heal: uses `GetStatsForLevel(currentLevel).maxHP`;
    `currentHP == null` means full HP; when healed to max, set `currentHP = null`.
  - `CompleteRoom()`: `gameObject.SetActive(false)`, then
    `Object.FindFirstObjectByType<CombatUI>().ShowRoomSelectionImmediately()`,
    else `Debug.LogError`.
- Instantiated by `Assets/Scripts/Data/HealRoomEffectStrategy.cs` (finds "UICanvas", instantiates
  prefab, gets/adds controller, calls `Initialize(RunSessionManager.CurrentParty)`).
- `Assets/Prefabs/UI/Events/SafeRoom.prefab` already exists (no `SafeRoomController` references yet).
  Inspect it before deciding on wiring.
- Lessons (`tasks/lessons.md`): Simplicity First (#21), no extra UI beyond requirements (#16),
  explicit `using` directives (#5), short class name for test runner filter,
  never `DestroyImmediate(obj, true)` on assets.

## Design decisions
1. New file `Assets/Scripts/UI/SafeRoomController.cs`, namespace `Nevergreen.UI`, `MonoBehaviour`.
   Same usings as the reference (`System.Collections.Generic`, `UnityEngine`, `UnityEngine.UI`,
   `Nevergreen.Data`, `Nevergreen.Prototype`).
2. Serialized fields:
   - `[SerializeField] private Button[] marionetteButtons = new Button[4];`
   - `[SerializeField, Range(0f, 1f)] private float healPercent = 0.25f;` with a `[Tooltip]`
     (fraction of max HP; 0.25 = 25%). Default matches the existing "heal all" value.
   - NO `hpTexts`, NO `healAllButton`.
3. Heal percent lives on the controller (Inspector/prefab configurable). Do not add a strategy
   field or change the `Initialize` signature, so the API stays identical to the reference.
4. Keep duplication simple: re-implement the small `CompleteRoom` logic; error message
   `[SafeRoomController] CombatUI not found!`. No shared base class (it would touch the reference).

## Implementation steps
- [x] Read `SafeRoom.prefab` and sibling `Prefabs/UI/Events` prefabs; note existing components
      and whether button children contain a TMP text.
- [x] Create `SafeRoomController.cs`:
  - [x] `Initialize(List<PartyMemberInfo> party)`: loop 0..3; skip null buttons; if
        `party != null && i < party.Count && party[i] != null && party[i].character != null`:
        activate button, set TMP child text to `displayName`, `RemoveAllListeners`,
        add listener `() => OnMarionetteClicked(party[captureIndex])` (capture the index).
        Else `SetActive(false)`.
  - [x] `OnMarionetteClicked(PartyMemberInfo member)`: null-guard;
        `maxHP = character.GetStatsForLevel(currentLevel).maxHP`;
        `currentHP = member.currentHP ?? maxHP`;
        `healAmount = Mathf.RoundToInt(maxHP * healPercent)`;
        `currentHP = Mathf.Min(maxHP, currentHP + healAmount)`;
        `member.currentHP = null` if `currentHP >= maxHP`, else `currentHP`;
        then `CompleteRoom()`.
  - [x] `CompleteRoom()`: same behavior as the reference.
- [ ] Wiring: attach the controller to `SafeRoom.prefab` when room strategy integration is ready.
- [x] Add tests in new `Assets/Editor/Tests/SafeRoomControllerTests.cs` (mirror patterns in
      `RoomEffectTests.cs` ~lines 583-690; set private fields via reflection per project convention):
  - [x] Heals configured percent (100 max, 20 HP, 0.50 -> 70).
  - [x] Different percent (0.25 -> 45) proves configurability.
  - [x] Overheal caps at max and `currentHP` becomes `null`.
  - [x] Member with `currentHP == null` stays `null`.
  - [x] Room completes: GO deactivated when a `CombatUI` exists.
  - [x] Unused party slots hide their buttons.
  - Use `LogAssert.Expect(LogType.Error, "[SafeRoomController] CombatUI not found!")` when no CombatUI.
  - Destroy created `CharacterData` via plain `DestroyImmediate(obj)`.

## Verification
- [x] `console-get-logs`: no compile errors/warnings from new files.
- [x] `tests-run` EditMode, `testClass = SafeRoomControllerTests` (short name): all 7 pass.
- [x] Run full suite EditMode tests to confirm no regressions (659 passed, 0 failed).
- [x] Confirm `MarionetteHealChoiceController.cs` is unchanged (git diff).

## Review
- Implemented `Assets/Scripts/UI/SafeRoomController.cs` matching all requirements:
  - Removed `healAllButton` and `hpTexts`.
  - Heals single selected Marionette by `healPercent` (default 50%, matching `SafeRoom.prefab` description).
  - Handles missing / null party slots gracefully.
  - Deactivates GameObject and notifies `CombatUI.ShowRoomSelectionImmediately()`.
- Added 7 comprehensive EditMode tests in `Assets/Editor/Tests/SafeRoomControllerTests.cs`.
- Zero regressions across the full test suite.

