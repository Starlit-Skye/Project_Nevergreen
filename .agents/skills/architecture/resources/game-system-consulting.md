---
skill: Game Systems Consulting
description: Translating game design requirements into Unity technical architecture and finding the "Built-in Fit".
---

# 👔 Game Systems Consulting Skill

**Context:** Use this skill when analyzing a Gameplay Feature Request *before* any technical design.

## 1. The "Decoupled Core" Mindset
- **Rule:** Do not hardcode data directly into game logic. Separate Data from Logic.
- **Question:** "Does this data need to live on a GameObject?"
    - *Yes:* Use MonoBehaviour/Component.
    - *No:* Build it as a ScriptableObject or plain C# Class.

## 2. "Standard System" Analysis
- **Challenge:** Avoid "re-inventing the wheel" for features Unity already provides.
- **Response:** Propose the Unity Built-in way first.
    - *Custom:* "Custom physics-based movement."
    - *Standard:* "Use CharacterController or Rigidbody with the New Input System."

## 3. Gameplay Loop Mapping
- Define the Frame-to-Frame steps:
    1.  **Trigger:** User Input vs. Logic Event?
    2.  **Processing:** Calculation, State Change, Physics Update?
    3.  **Result:** Update UI, Play Animation, Spawn FX?

## 4. Design Translation
Translate "Game Design Speak" to "Unity Speak":
- "We need items that persist across scenes." -> **ScriptableObjects** or **Singletons**.
- "This enemy should only chase if close." -> **Physics.OverlapSphere** or **SphereCollider**.
- "Characters have different stats." -> **Data Templates** via ScriptableObjects.
