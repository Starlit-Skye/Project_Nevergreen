# Plan: Specific Character Targeting Strategy in AI Rule-Based Behaviour

## Tasks
- [x] 1. Create `SpecificCharacterTargeting.cs` subclassing `AITargetingNode` inside the namespace `Nevergreen.Combat.AI.Nodes`.
- [x] 2. Implement `TryResolveTargets` scanning the target pool for a matching character ID.
- [x] 3. Add unit tests in `AIRuleTests.cs` to verify successful targeting, missing character, and null reference handling.
- [x] 4. Run `mcp_ai-game-developer_tests-run` to verify compilation and that the new tests pass.
- [x] 5. Review results and document in walkthrough.
