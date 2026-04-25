---
name: docs-seeker
description: Strategies for efficiently navigating the internal Unity Knowledge Base and external technical documentation.
---

# 📚 Documentation Seeker Skill (Unity Edition)

**Context:** Use this skill when you are unsure about a Game Architecture, Unity API, or Project Rule.

## 1. Internal Knowledge Base Strategy
Don't guess project rules. Search the `.agent/guidelines/` folder for established patterns.
*   **Data & Save Systems?** -> `guidelines/data-guidelines.md` (ScriptableObjects, JSON, Persistence).
*   **UI/UX Standards?** -> `guidelines/ui-design-guidelines.md` (Canvas, TMP, DOTween).
*   **General Rules?** -> `master_rules.md` and `rules/`.
*   **Project Roster?** -> `general concepts/agent-team-roster.md`.

## 2. External Search Strategy (Unity / C# / DOTween)
1.  **Keyword optimization:** Don't search "how to move player". Search "Unity CharacterController.Move vs Rigidbody.velocity performance".
2.  **Authoritative Sources:** Prioritize `docs.unity3d.com` (Manual/API), `learn.unity.com`, and `dotween.demigiant.com`.
3.  **Context Window:** When you find a solution, summarize the *relevant* snippets into the chat. Do not copy-paste entire boilerplate files.

## 3. When to Search?
*   **Before Planning:** "Do we have a naming convention for ScriptableObjects?"
*   **During Implementation:** "What is the exact syntax for `DOTween.Sequence()` nesting?"
*   **During Performance Tuning:** "What are the common causes of Canvas Rebuild spikes?"

## 4. Interaction Output
> **📖 Docs Check:**
> *   **Source:** `guidelines/data-guidelines.md`
> *   **Finding:** "Always use `Application.persistentDataPath` for local save files."
> *   **Application:** I will update the `SaveManager.cs` to use this path instead of a hardcoded string.
