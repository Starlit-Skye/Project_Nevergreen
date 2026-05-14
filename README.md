# Project Nevergreen

Project Nevergreen is a tactical turn-based RPG centered on rank-based positioning and formation management. This repository contains the combat engine, data definitions, and technical documentation for the game.

## Game Concept

### Overview
Nevergreen is a turn-based roguelike. Players control **Cecilia** and lead a team of three **Marionettes** of varying classes. The objective is to progress through a run, navigating encounters to reach and defeat the final boss. Combat uses a rank-based system where both player and enemy teams occupy positions 1 through 4, with Rank 1 representing the front-most position.

### Core Loop
Combat revolves around protecting **Cecilia** (`ceci`) while managing the Marionette team. If Cecilia is defeated, the run ends in failure.

### Key Mechanics
- **Rank-Based Positioning**: Characters occupy Ranks 1 to 4. Skill availability and target eligibility are determined by these positions.
- **Formation Integrity (Piles)**: Defeated units leave behind a **Pile** to maintain rank spacing. **Critical Hits** bypass this, causing immediate formation collapse and rank-shifting.
- **Marionettes**: Collectible player units characterized by unique **Perfections** and **Imperfections** (traits).

## Repository Architecture for Designers

This project is structured to isolate game content and tuning from core engine code.

### Content & Data
- `Assets/Scripts/Data/`: Definitions for `CharacterData`, `StatBlockData` (stats), and `SkillData` (abilities).
- `Assets/Scripts/Data/CombatConfig.cs`: Global combat tuning and mechanical constants.

### Logic & Systems
- `Assets/Scripts/Combat/`: Core simulation logic including the `BattleSystem` state machine and `StatusProcessor`.
- `Assets/Scripts/Combat/Effects/`: Implementation of specific skill behaviors and status effects.

### Documentation
- `Docs/specs/`: Detailed mechanical and system specifications.
- `Docs/technical/`: Testing guides and technical implementation notes.

## Core Systems Summary

### Positioning
- **Ranks**: 1 (Front) through 4 (Back).
- **Rank-Shifting**: Automated forward movement when a rank is vacated (and no Pile exists).

### Status Effects
- **Control**: Stun (skip turn), Move (forced repositioning).
- **Protection**: Guard (intercept attacks).
- **Attrition**: Bleed, Blight (damage over time).

## Getting Started
To understand the underlying design logic, refer to `Docs/specs/mechanics/MECHANIC_SPEC_COMBAT_CORE.md`. Data values can be modified via Unity ScriptableObjects linked to the `Assets/Scripts/Data/` folder.
