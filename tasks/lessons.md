# Lessons Learned

## Communication & Workflow

### 1. Investigation vs. Implementation (CRITICAL)
- **Pattern**: User explicitly requests "investigation" and "proposal" without "implementation".
- **Rule**: STOP after providing the analysis and list of steps. DO NOT call any file-modifying tools (`replace_file_content`, `write_to_file`, etc.) targeting source code until the user gives the "Go ahead."
- **Failure Analysis**: I ignored a pre-existing lesson on this topic. I must re-read `lessons.md` at the start of EVERY new task to internalize current project-specific failures.

### 2. Pre-Edit Scope Check
- **Rule**: Before every `replace_file_content` or `write_to_file` call, verify: "Did the user ask me to make this change, or am I assuming they want it now?"
