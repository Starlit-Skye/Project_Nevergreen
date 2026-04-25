# Unity UI Design System & Technical Guidelines

**Version:** 1.0 (UGUI + DOTween + TMP)
**Status:** Approved
**Target Audience:** UI/UX Designers, Unity Developers, AI Agents

---

## 1. Executive Summary

This document defines the standards for building high-fidelity User Interfaces within the Antigravity Unity ecosystem. It prioritizes **TextMeshPro** for typography, **UGUI/UI Toolkit** for layout, and **DOTween** for micro-interactions. The goal is a responsive, accessible, and visual-first UI that maintains "Premium Aesthetic" standards.

---

## 2. Technology Stack Standards

### Core UI Framework
| Library | Version | Purpose |
|---------|---------|---------|
| **Unity UGUI** | Standard | Default UI system (Canvas-based) |
| **UI Toolkit**| Latest | XML/USS-based UI for complex layouts/tools |
| **TextMeshPro**| 3.x+ | Advanced SDF-based typography |
| **DOTween**| Latest | High-performance tweening & animations |

### Visual Enhancement
| Library | Purpose |
|---------|---------|
| **Shapes** | Vector-based UI elements (SDF circles, lines) |
| **UI Extensions** | Specialized components (Accordions, Reorderable Lists) |
| **Post-Processing**| UI Blur, Bloom, and Color Grading |
| **Addressables** | On-demand loading of heavy UI textures/atlases |

### State & Logic
| Library | Purpose |
|---------|---------|
| **Unity Atoms** | Event-driven UI updates (Variable/Event patterns) |
| **UniTask** | Async UI logic and sequencing |
| **Zoran** | Localized text and dynamic string formatting |

---

## 3. UI Component Setup

### 3.1. Prefab-Based Architecture
**All UI elements must be Prefabs.** Use **Prefab Variants** for different themes (e.g., `Button_Primary`, `Button_Secondary`).

### 3.2. TextMeshPro Defaults
- **Main Font**: Inter or Roboto (SDF).
- **Auto Size**: Enabled for dynamic text, with strict min/max constraints.
- **Material Presets**: Use presets for Glow, Outline, and Underlay instead of modifying material instances at runtime.

### 3.3. Canvas Configuration
- **Render Mode**: `Screen Space - Overlay` for HUD, `World Space` for in-game labels.
- **Canvas Scaler**: `Scale With Screen Size` (Reference Resolution: 1920x1080).
- **Screen Match Mode**: `Match Width Or Height` (0.5 for general, 1.0 for landscape mobile).

---

## 4. Visual Layout Standards

### 4.1. Safe Area Handling
Every main screen must have a `SafeArea` component to handle notches and rounded corners on modern mobile/wide devices.

**Hierarchy Structure:**
```
Canvas
└── SafeArea (Script)
    └── Background (Image)
    └── Content (RectTransform)
        ├── Header
        ├── Body
        └── Footer
```

### 4.2. Layout Components
- **Auto Layout**: Use `HorizontalLayoutGroup`, `VerticalLayoutGroup`, and `GridLayoutGroup` for lists.
- **Padding/Spacing**: Always use consistent modular intervals (e.g., 8px, 16px, 32px).
- **Content Size Fitter**: Only use on leaf nodes or containers where content is strictly controlled to avoid "Layout Calculation Hell".

---

## 5. Component Standards

### 5.1. Buttons (Interactive)
All buttons must have a `UIButton` wrapper script that handles:
- **Hover/Down Tweens**: Scale 1.0 -> 0.95 and subtle brightness shifts.
- **Sound Effects**: Play on Click/Hover.
- **Raycast Target**: Optimized boundaries.

### 5.2. HP Bars & Gauges
- **Component**: Utilize the built-in Unity Slider component for logic and layout synchronization.
- **Visuals**: Use Soft Masks or SDF Shaders for smooth edges.
- **Feedback**: Damage "Ghosting" effect (a white bar that trails behind the primary health bar).
- **Animation**: DOTween `DOValue` with `Ease.OutExpo`.

### 5.3. Tooltips
- **Trigger**: `OnPointerEnter` / `OnPointerExit`.
- **Pivot**: Dynamic pivoting based on screen quadrant to avoid clipping.
- **Delay**: 0.5s delay before showing to prevent visual noise.

---

## 6. Standard Game UI Patterns

### 6.1. Core Gameplay HUD
- **Persistent Status**: Primary vitals (e.g., Health, Resource), mini-map, or current objective.
- **Transient Alerts**: Contextual notifications, floating text (World Space), and status markers.
- **Interaction Cues**: Dynamic button prompts and contextual menus that respond to player proximity or state.

### 6.2. Dialogue System
- **Typewriter Effect**: Reveal text character-by-character.
- **Speaker Portraits**: Use `framer-motion` style entry transitions (slide + fade).
- **Choice Buttons**: List-based layout with keyboard/controller support.

### 6.3. Inventory & Grids
- **Infinite Scrolling**: Use recycled-item lists for 100+ items.
- **Drag & Drop**: Implement via `IDragHandler`, using a proxy icon on the top-most Canvas layer.

---

## 7. Animation & Feedback

### 7.1. View Transitions
- **Fade**: 0.3s `CanvasGroup.alpha` tween.
- **Slide**: Modular slide distances (e.g., slide up 100px with fade).
- **Blur**: Animate `UniversalRenderPipeline` blur depth during menu opening.

### 7.2. Micro-interactions
- **Success**: Green flash + Subtle scale "bounce" (1.05 -> 1.0).
- **Error**: Red "shake" animation + Haptic feedback.
- **Wait**: Indeterminate progress spinner or shimmering "Skeleton" states for loading assets.

---

## 8. Implementation Checklist

1. [ ] **Canvas Scaler**: Set to 1920x1080, Match 0.5.
2. [ ] **Optimization**: `Raycast Target` disabled on all non-interactive images/text.
3. [ ] **Safe Area**: Screen content wrapped in a `SafeArea` container.
4. [ ] **Typography**: All text uses `TextMeshProUGUI` with SDF fonts.
5. [ ] **Navigation**: Screen supports Controller/Keyboard navigation (Explicit navigation links).
6. [ ] **Transitions**: All screens have Enter/Exit DOTween sequences.
7. [ ] **Audio**: All buttons hooked into `AudioManager` for click sounds.

---

## 9. Performance & Memory

- **Atlas Management**: Pack UI textures into Spritesheets based on screen/feature.
- **Overdraw**: Minimize transparent layers; use opaque backgrounds where possible.
- **Batching**: Group elements with identical materials/textures to reduce Draw Calls.
- **Dirty Layouts**: Avoid moving Layout elements every frame. Use `Canvas.ForceUpdateCanvases()` sparingly.

---

## 10. Designer-Developer-AI Loop

> **CRITICAL:** When implementing UI from a Mockup:
> 1. Export assets as `.png` (2x scale) or `.svg`.
> 2. Use `d:\.agent\guidelines\data-guidelines.md` for hooking UI to State.
> 3. Verify Font sizes match the Design System (12pt, 16pt, 24pt, 32pt).
