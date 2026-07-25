---
name: frontend-development
description: Expert capabilities for building React Applications with shadcn/ui and SAP Fiori-inspired design.
---

# 🎨 Unity UI Development Skill

**Context:** Use this skill package for all UI/UX work within the Unity Editor and UI codebase.

## 1. The Core Stack

### Systems & Frameworks
| Library | Purpose |
|---------|---------|
| **Unity UGUI** | Standard Canvas-based UI system |
| **UI Toolkit** | Modern XML/USS-based UI (UXML) |
| **TextMeshPro** | High-fidelity SDF typography |
| **DOTween** | High-performance tweening & sequences |

### Layout & Responsiveness
| Component | Purpose |
|-----------|---------|
| **Canvas Scaler** | Screen resolution & aspect ratio adaptation |
| **Layout Groups** | Dynamic list and grid positioning |
| **Safe Area Handler**| Notch and rounded corner support |
| **RectTransform** | Anchor and pivot management |

### Graphics & Optimization
| Tool | Purpose |
|------|---------|
| **Sprite Atlas** | Draw call batching & texture merging |
| **Addressables** | On-demand UI asset loading |
| **Shapes** | Procedural vector UI elements |
| **Universal RP** | UI Blur, Bloom, and Post-processing |

---

## 2. Component Architecture

### Prefab-First Workflow
All UI elements must be **Prefabs**. Use **Prefab Variants** for themed components (e.g., `Btn_Primary`, `Btn_Secondary`).

### Component Wrapping
Always use a wrapper class (e.g., `UIButton`, `UIText`) to centralize logic for:
- Sound effects on click/hover.
- Default DOTween scale-bounce animations.
- Theming and color preset application.

---

## 3. Reference Modules

- **[UI Design Guidelines](../../guidelines/ui-design-guidelines.md)**: Standard component references, layouts, and theming.
- **[Data Guidelines](../../guidelines/data-guidelines.md)**: Connecting UI to data via ScriptableObjects/Events.
- **[Unity UI Extension Support](./resources/ui-extensions.md)**: Advanced UGUI components.

---

## 4. Key Patterns

### State-Driven UI (Events)
Decouple UI from game logic using Action events or ScriptableObject events.
```csharp
private void OnEnable() => GameEvents.OnHealthChanged += UpdateHealthBar;
private void OnDisable() => GameEvents.OnHealthChanged -= UpdateHealthBar;

void UpdateHealthBar(int current, int max) {
    healthSlider.DOValue((float)current / max, 0.3f);
}
```

### Recycled Lists (Infinite Scrolling)
For large inventories, only instantiate the visible items and reuse them as the user scrolls.

---

## 5. Best Practices

- **Layout Rebuilds:** Disable `LayoutGroup` components after the initial layout if the content is static to save CPU time.
- **Raycast Targets:** Disable `Raycast Target` on all non-interactive images and text.
- **Batching:** Ensure elements using the same material/texture are grouped in the hierarchy to allow batching into a single Draw Call.
- **Mobile Safe Area:** Always wrap main UI content in a `SafeArea` container to avoid clipping on modern devices.
- **Optimistic UI:** Show feedback (ghosting, sound, animation) instantly while the game backend processes the action.
- **60fps+ Animations:** Prefer DOTween over the Unity Animator for simple UI transitions to minimize overhead.
