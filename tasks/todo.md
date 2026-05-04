# Task: Implement "Move" Status Effect Information Gathering

## Status
- [x] Research "Move" Status Effect from Google Doc <!-- id: 0 -->
- [x] Analyze existing status effect system for integration <!-- id: 1 -->
- [x] Create specification for "Move" Status Effect <!-- id: 2 -->
- [x] Implement "Move" Status Effect logic <!-- id: 3 -->
- [x] Verify implementation with tests <!-- id: 4 -->

## Research Notes
- Google Doc URL: https://docs.google.com/document/d/1DN-fIr9PG38hDRrMWJ5NrbWfTY-V7gf5Dz2cwSw3qUo/edit?usp=sharing
- **Move Status Effect Details:**
    - **Effect:** Moves a character forward or backward in ranks.
    - **Amplitude:** Number of spaces to move.
    - **Duration:** 0 (Instantaneous).
    - **Resistance:** `MoveResist` stat.
    - **Interaction:** `Final Chance = Application Chance - Move Resist`.
    - **Special Case:** "Pile" (corpse) has 300% Move Resistance.
    - **Context:** Used by skills to disrupt formations (e.g., Novellite's Eldritch Grasp, Final Boss's Spirit Chaser attacks).

## Implementation Plan
1. Create `MoveEffect` scriptable object effect (implementing `ISkillEffect`).
2. Update `BattleSystem` or `CombatCharacter` to handle rank swapping/reordering.
3. Ensure Move Resistance is correctly factored in `CombatCalculator`.
4. Add unit tests for moving allies/enemies and resistance checks.

## Review Section
*TBD*
