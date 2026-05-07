# Lessons Learned

## Communication & Workflow

### 1. Investigation vs. Implementation (CRITICAL)
- **Pattern**: User explicitly requests "investigation" and "proposal" without "implementation".
- **Rule**: STOP after providing the analysis and list of steps. DO NOT call any file-modifying tools (`replace_file_content`, `write_to_file`, etc.) targeting source code until the user gives the "Go ahead."
- **Failure Analysis**: I ignored a pre-existing lesson on this topic. I must re-read `lessons.md` at the start of EVERY new task to internalize current project-specific failures.

### 2. Pre-Edit Scope Check
- **Rule**: Before every `replace_file_content` or `write_to_file` call, verify: "Did the user ask me to make this change, or am I assuming they want it now?"

## Combat & Formation Mechanics

### 3. Implicit Anchoring (Pile Mechanic)
- **Pattern**: Assuming dead characters should be ignored by spatial systems (ranks, formations).
- **Rule**: In a formation-based game, corpses often occupy slots and should be subject to displacement. Don't assume `!IsAlive` means "ignore for movement logic" unless explicitly required.
- **Verification**: When implementing formation-related mechanics, always test with a mix of alive and dead characters to ensure the formation remains coherent.

### 4. IsAlive Over-usage
- **Rule**: Avoid using `!character.IsAlive` as a blanket filter for turn logic, status logic, AND spatial logic. Spatial logic should usually consider all entities in the formation to maintain spatial integrity.
