# 🧠 Designer Guide: Enemy AI System

Welcome to the **Nevergreen Modular AI Framework**. This system is designed to allow designers to create complex, tactical enemy behaviors without writing a single line of code.

---

## 1. The Core Concept: "The Decision Loop"
The AI works on a **Priority-Based Selection** model. Every turn, the AI looks at a list of behaviors you've defined and asks each one: *"Can you do something right now?"*
*   The AI checks behaviors from **top to bottom**.
*   The **first** behavior that says "Yes" (meaning its conditions are met and it found a target) will be executed.
*   Everything below that successful behavior is ignored for that turn.

> [!TIP]
> Always put your most specific "Special" rules (like healing at low health) at the top, and your "Generic" rules (like attacking random targets) at the bottom.

---

## 2. Setting Up a New AI

### Step 1: Create the Profile
1.  Right-click in your Project window: `Create > Nevergreen > AI > Enemy AI Profile`.
2.  Name it something descriptive (e.g., `AI_Goblin_Shaman`).

### Step 2: Assign to a Character
1.  Open any `CharacterData` asset.
2.  Drag your new AI Profile into the **Default AI Profile** slot.

---

## 3. Building Behaviors
Inside the AI Profile, you'll see a **Behaviors** list. Click the **(+)** button to add a new entry, then use the **Dropdown Menu** to select a type.

### 🛡️ Rule Based Behavior (The Workhorse)
This is the most common node. It allows you to define a specific "If-Then" rule.
*   **Skill To Use**: Which skill should fire if this rule is met.
*   **Conditions**: A list of requirements (e.g., "Self HP is below 30%"). All must be true.
*   **Targeting**: How to pick who to hit (e.g., "Lowest HP Ally").

### 🔄 Sequence Behavior (The Combo Chain)
Cycles through a fixed list of skills in order: A → B → C → A → ...
*   **Sequence ID**: A unique name for this sequence (e.g., `shaman_combo`). Different enemies using the same AI Profile will track their position independently.
*   **Skill Sequence**: The ordered list of skills to cycle through.
*   **Targeting**: How to pick who to hit (shared across all skills in the sequence).
*   **Skip On Failure**: If checked (default), the AI will skip to the next skill in the sequence if the current one can't be used (e.g., wrong rank). If unchecked, the entire behavior fails and the AI falls through to the next behavior.

### 🎲 Random Skill Behavior (The Safety Net)
Usually placed at the very bottom of your list. It will look at all valid skills for the current rank and pick one at random. If no skills are usable, the enemy will pass their turn.

---

## 4. Conditions & Targeting
When using a **Rule Based Behavior**, you can fine-tune exactly when and who it targets.

### Conditions (When?)
*   **Health Condition**: Checks HP percentages.
    *   **Source**: Self, Any Ally, or Any Enemy.
    *   **Threshold**: The % value (0 to 1).
    *   **Operator**: Less than, Greater than, etc.
*   **Repetition Condition**: Prevents a skill from being spammed.
    *   **Max Consecutive Uses**: The maximum number of times this skill can be used in a row before the condition blocks it. (e.g., set to `2` to allow the skill twice, then force the AI to do something else).

### Targeting (Who?)
*   **Simple Targeting**: 
    *   **Strategy**: Random, Lowest HP, Highest HP.
    *   **Rank Bias**: Prefer Front (Rank 1-2) or Back (Rank 3-4).

---

## 5. Examples & Recipes

### The "Healer" Logic
You want a priest who heals an ally if they are below 50% health, otherwise just attacks.
1.  **Behavior 1 (Top)**: `RuleBasedBehavior`
    *   Skill: `Heal_Skill`
    *   Condition: `HealthCondition` (Target: AnyAlly, Operator: LessThan, Threshold: 0.5)
    *   Targeting: `SimpleTargeting` (Strategy: LowestHP)
2.  **Behavior 2 (Bottom)**: `RandomSkillBehavior`

### The "Surgical Sniper"
An archer who always hunts the weakest player character.
1.  **Behavior 1**: `RuleBasedBehavior`
    *   Skill: `Shot_Skill`
    *   Targeting: `SimpleTargeting` (Strategy: LowestHP)

### The "Burst Limiter"
A boss who uses a powerful nuke skill, but never more than 2 turns in a row.
1.  **Behavior 1 (Top)**: `RuleBasedBehavior`
    *   Skill: `Nuke_Skill`
    *   Condition: `RepetitionCondition` (MaxConsecutiveUses: 2)
    *   Targeting: `SimpleTargeting` (Strategy: Random)
2.  **Behavior 2 (Bottom)**: `RandomSkillBehavior`

### The "Combo Dancer"
An enemy that follows a strict pattern: Buff Self → Heavy Attack → Rest.
1.  **Behavior 1**: `SequenceBehavior`
    *   Sequence ID: `dancer_combo`
    *   Skill Sequence: [`Buff_Self`, `Heavy_Attack`, `Rest_Skill`]
    *   Targeting: `SimpleTargeting` (Strategy: Random)
    *   Skip On Failure: ✓

---

## 6. Pro-Tips for Designers
*   **Rank Matters**: The AI will automatically skip a rule if the `Skill To Use` cannot be used from the enemy's current rank.
*   **Null Checks**: If you set a rule but leave the "Targeting" empty, the rule will fail and the AI will move to the next behavior in the list.
*   **Hierarchy**: You can see which behavior "won" by looking at the **AI History** in the Inspector during PlayMode (Advanced).
