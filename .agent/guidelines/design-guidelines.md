# Antigravity Design System & Technical Guidelines

**Version:** 4.0 (Unity URP + Modular Architecture)
**Status:** Approved
**Target Audience:** Unity Developers, Game Architects, AI Agents

---

## 1. Executive Summary

This document serves as the **Single Source of Truth** for building game systems and assets within the Antigravity ecosystem. We prioritize **Performance**, **Modularity**, and **Scalability**. Our core tech stack is built on the **Unity engine** using the **Universal Render Pipeline (URP)**.

---

## 2. Technology Stack Standards

### Core Engine & Scripting
| Library | Version | Purpose |
|---------|---------|---------|
| Unity | 2022.3 LTS+ | Primary Game Engine |
| C# | 9.0+ | Programming Language |
| URP | 14.x+ | Render Pipeline (Universal) |
| Addressables | 1.21+ | Asset Management & Loading |

### Architecture & Logic
| Tool / Pattern | Purpose |
|----------------|---------|
| **ScriptableObjects** | Data containers & config-driven logic |
| **Unity Atoms / Events** | Decoupled communication (Event-driven) |
| **UniTask** | Efficient async/await for Unity |
| **DOTween** | High-performance tweening & sequences |

### Visualization & Audio
| Library | Purpose |
|---------|---------|
| **TextMeshPro** | Advanced SDF Typography |
| **Shapes** | Vector-based visual elements |
| **FMOD / AudioGraph** | Advanced audio orchestration |

---

## 3. Project Hierarchy & Naming

### 3.1. Directory Structure
```
Assets/
├── _Project/           # Core game content
│   ├── Art/            # Textures, Models, Shaders
│   ├── Audio/          # SFX, Music
│   ├── Data/           # ScriptableObject instances
│   ├── Prefabs/        # Reusable systems & entities
│   ├── Scenes/         # Levels & Environments
│   └── Scripts/       # C# Code (Namespaced)
├── Plugins/            # 3rd party libraries
└── Settings/           # URP, Input, and Project configs
```

### 3.2. Naming Conventions
- **Scripts**: PascalCase (e.g., `PlayerController.cs`)
- **Prefabs**: PascalCase (e.g., `Hero_Warrior.prefab`)
- **Assets**: `T_` (Texture), `M_` (Material), `AC_` (AnimatorController)
- **ScriptableObjects**: `SO_` (e.g., `SO_Item_HealthPotion.asset`)

---

## 4. Architectural Patterns

### 4.1. Layered Responsibility
1.  **Data Layer (SO)**: Pure attributes, no logic (e.g., `ItemData`).
2.  **Logic Layer (Mono)**: Systems that process data (e.g., `InventoryManager`).
3.  **View Layer (UI)**: Dispalys state to user (e.g., `InventoryView`). *See [UI Design Guidelines](ui-design-guidelines.md)*.

### 4.2. Decoupling with Events
Avoid direct references between systems. Use `GameEvent` ScriptableObjects to notify listeners of changes (e.g., `OnPlayerHurt`, `OnEncounterStarted`).

---

## 5. Performance & Optimization

### 5.1. Memory Management
- Use **Addressables** for all non-essential assets to keep memory footprint low.
- Profile frequently for **GC Allocations**. Avoid `new` in `Update()` or frequently called methods.

### 5.2. Rendering
- **Draw Call Batching**: Use Sprite Atlases and Static Batching where possible.
- **Overdraw**: Minimize transparent layers, especially in UI and particle systems.
- **LOD**: Implement Level of Detail for all complex 3D assets.

---

## 6. Visual Fidelity Standards

### 6.1. Lighting & Post-Processing
- Use **Global Volumes** for color grading, bloom, and vignettes.
- Baked lighting for environments; real-time shadows for dynamic entities.

### 6.2. Typography
All text MUST use **TextMeshPro** with standardized SDF Font Assets. Never use the legacy Unity Text component.

---

## 7. Animation & Game Feel

### 7.1. Micro-interactions
- Use **DOTween** for UI and simple object transitions.
- Implement **Juice**: Screenshake, hit-stop, and particle bursts on impact.

---

## 8. Implementation Checklist

1. [ ] **Version Check**: Unity 2022.3 LTS or higher.
2. [ ] **Assets**: Organized into `_Project/` subfolders.
3. [ ] **Performance**: No `GameObject.Find` or `GetComponent` in `Update`.
4. [ ] **Data**: Constants and configurations moved to `ScriptableObjects`.
5. [ ] **Optimization**: Sprite Atlases created for UI and 2D assets.
6. [ ] **UI Integration**: Follows [UI Design Guidelines](ui-design-guidelines.md) standards.
7. [ ] **Code Quality**: Follows [Testing Guidelines](testing-guidelines.md) and coding standards.
