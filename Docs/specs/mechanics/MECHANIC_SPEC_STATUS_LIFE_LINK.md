# Status Mechanic: Life Link

## Overview
Life Link is a unique status effect primarily applied to specific enemies (e.g., The Weavers) that binds their lifeforces together. Characters with this status do not immediately die when their HP reaches 0, provided that at least one other character with the Life Link status on their team is still alive.

## Behavior
1. **Defeat Interception**: 
   - When a character with Life Link reaches 0 HP, they do not enter the `Dying` state.
   - Instead, if there is another ally on the same team who also has the Life Link status and is `Alive` (HP > 0), the defeated character enters the `Downed` state.
   - If no other allies with Life Link are `Alive`, the character dies normally (enters `Dying`).
2. **The Downed State**:
   - A `Downed` character acts as an empty shell. They have 0 HP and cannot take actions.
   - They cannot be selected as a primary target for single-target skills.
   - They still physically occupy their rank(s) and will absorb a slot in an Area of Effect (AOE) attack without taking any damage or effects.
   - Upon entering the `Downed` state, all other status effects on the character are cleansed.
3. **Round-End Revival**:
   - At the end of every combat round, any character in the `Downed` state who still possesses the Life Link status is revived.
   - They are restored to `Alive` state and healed for a percentage of their Max HP, determined by the Life Link status amplitude.
4. **Link Collapse**:
   - The Life Link is broken if the last standing character with the status is defeated.
   - When the link collapses (either by the last standing character reaching 0 HP, or the status effect expiring on all living members), all `Downed` allies connected to the link immediately process their defeat flow, entering the `Dying` state (and subsequently `Pile` or `Destroyed`).

## Implementation Details
- Added `LifeState.Downed` to `CombatCharacter`.
- Extended `StatusEffectInstance` with `TryInterceptDefeat(bool isCritical)` to allow statuses to override the death flow.
- Added `LifeLinkStatusInstance` to handle the specific logic for intercepting defeat and collapsing the link.
- `BattleSystem.EndRound` specifically checks for downed characters with Life Link to revive them before the next round begins.
- `TargetResolver` logic updated to handle `Downed` units for AOE absorption while preventing them from being valid primary targets.
- `SkillExecutor` skips processing hits on `Downed` units, meaning they take no damage or status effects while in this state.
