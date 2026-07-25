# Data Persistence & Game State Guidelines

**Version:** 1.0 (Unity-Optimized)
**Target Audience:** Unity Developers, Game Architects, AI Agents

---

## 1. Executive Summary

This document defines the standards for data storage and persistence within Antigravity Unity projects. It prioritizes a hybrid approach: **ScriptableObjects** for static/configuration data and **JSON-based Serialization** for dynamic player state. The goal is to ensure a scalable, debuggable, and performant data layer that integrates seamlessly with Unity's lifecycle.

---

## 2. Architecture & Project Structure

### 2.1. Layered Data Architecture
```
┌──────────────────────────────────────────────────────────────┐
│                    Game Logic / Systems                      │
│ (BattleSystem, InventorySystem, QuestSystem)                 │
└──────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                 Data Access Layer (Repository)               │
│  - DataManager (Singleton/Service)                           │
│  - Save/Load Orchestration                                   │
│  - Encryption / Compression Wrapper                          │
└──────────────────────────────────────────────────────────────┘
            │                                      │
            ▼                                      ▼
┌───────────────────────┐              ┌───────────────────────┐
│  State Layer (JSON)   │              │ Static Layer (SO)     │
│  - PlayerStats        │              │ - SkillDefinitions    │
│  - InventoryState     │              │ - EnemyTemplates      │
│  - ProgressData       │              │ - SystemConfigs       │
└───────────────────────┘              └───────────────────────┘
            │                                      │
            ▼                                      ▼
┌──────────────────────────────────────────────────────────────┐
│                    Persistence Layer                         │
│  - Application.persistentDataPath (Local Files)              │
│  - PlayerPrefs (Settings Only)                               │
│  - Cloud Storage (Steam Cloud, PlayFab, etc.)                │
└──────────────────────────────────────────────────────────────┘
```

### 2.2. Folder Organization
```
Assets/
├── Data/
│   ├── Databases/           # Master ScriptableObject lists
│   ├── Resources/           # Legacy (Avoid if possible, use Addressables)
│   └── Config/              # Global JSON settings
├── ScriptableObjects/       # Template definitions
│   ├── Skills/
│   ├── Characters/
│   └── Items/
└── Scripts/
    ├── Persistence/         # Core persistence logic
    │   ├── Models/          # POCO (Plain Old C# Objects) for JSON
    │   ├── Serialization/   # JSON Converters, Encryption
    │   └── Managers/        # SaveSystem, DataManager
    └── Core/
        └── Repositories/    # High-level data access
```

---

## 3. Naming Conventions

Standardized naming allows AI and developers to quickly identify "static data" vs "dynamic state".

| Element | Convention | Example |
|---------|-----------|---------|
| ScriptableObject Class | PascalCase | `CharacterTemplate`, `SkillData` |
| SO Instances (Assets) | `SO_Type_Name` | `SO_Skill_Fireball`, `SO_Char_Hero` |
| Data Models (Save) | PascalCase + `Data` | `PlayerData`, `InventorySaveData` |
| Save File Names | kebab-case | `save-slot-01.json`, `game-settings.cfg` |
| Manager Scripts | PascalCase + `Manager` | `SaveManager`, `PersistenceManager` |
| Repository Scripts | PascalCase + `Repository`| `SkillRepository`, `EnemyRegistry` |

---

## 4. Data Modeling (Unity)

### 4.1. ScriptableObjects (Static Data)
Use for any data that is **read-only** at runtime (templates, constants, tiers).
```csharp
[CreateAssetMenu(fileName = "NewSkill", menuName = "Combat/Skill")]
public class SkillData : ScriptableObject {
    public string skillId; // Use unique string IDs, not just index
    public int baseDamage;
    public float cooldown;
    public Sprite icon;
}
```

### 4.2. POCO Models (Mutable State)
Use simple C# classes for data that needs to be **saved**. Mark as `[System.Serializable]`.
```csharp
[System.Serializable]
public class CharacterSaveData {
    public string templateId; // Reference back to ScriptableObject ID
    public int currentLevel;
    public int currentExp;
    public List<string> unlockedSkillIds;
}
```

---

## 5. Implementation Patterns

### 5.1. The Registry Pattern
Avoid direct references to concrete ScriptableObjects in save files. Save the `ID` (string) instead, and use a Registry to look it up.
```csharp
public class SkillRegistry : ScriptableObject {
    public List<SkillData> allSkills;
    public SkillData GetSkillById(string id) => allSkills.Find(s => s.skillId == id);
}
```

### 5.2. JSON Serialization (Newtonsoft.Json)
Prefer **Newtonsoft.Json** (Json.NET) over Unity's built-in `JsonUtility` for complex types (Dictionaries, Polymorphism).
```csharp
// Store in PersistencyManager
public string Serialize<T>(T data) {
    return JsonConvert.SerializeObject(data, Formatting.Indented);
}
```

### 5.3. Reactive Data (Unity Atoms / Events)
Don't poll the DataManager. Throw events when data changes.
```csharp
public void UpdateGold(int amount) {
    playerData.gold += amount;
    OnGoldChanged?.Invoke(playerData.gold); // Notify UI/Systems
}
```

---

## 6. Persistence Logic & Managers

### 6.1. Manager Responsibilities
The `SaveManager` should handle orchestration, not business logic.
- **Save**: Fetch data from systems -> Serialize -> Encrypt -> Write to Disk.
- **Load**: Read Disk -> Decrypt -> Deserialize -> Distribute to systems.

### 6.2. Pathing
Always use `Application.persistentDataPath` for save files.
```csharp
private string GetPath(string fileName) {
    return Path.Combine(Application.persistentDataPath, fileName);
}
```

### 6.3. Atomic Saves (Anti-Corruption)
Write to a `.tmp` file first, then use `File.Replace` to swap with the real save file. This prevents data loss during crashes mid-write.

---

## 7. Asynchronous & Addressables

### 7.1. Async I/O
Use `Task.Run` or Unity's `WebRequest` (for cloud) to avoid frame spikes during large I/O.
```csharp
public async Task SaveAsync(PlayerData data) {
    string json = Serialize(data);
    await File.WriteAllTextAsync(path, json);
}
```

### 7.2. Addressables for Data
Load large databases (POs, Textures) via **Addressables** rather than `Resources`.
```csharp
AsyncOperationHandle<SkillRegistry> handle = Addressables.LoadAssetAsync<SkillRegistry>("MainSkillRegistry");
```

---

## 8. Error Handling & Integrity

- **Version Checking**: Every save file MUST include a `version` field. Detect and run migration scripts for old versions.
- **Checksums**: Store a SHA-256 hash at the end of the save file to detect manual tampering or corruption.
- **Backups**: Keep the last 3 successful save files as `.bak0`, `.bak1`, `.bak2`.

---

## 9. Design Philosophy

### 9.1. Separation of Concerns
- **Data Model**: No logic, just fields.
- **Logic System**: Pure logic, doesn't know about file paths.
- **Persistence Manager**: Knows about files/serialization, doesn't know game rules.

### 9.2. DRY (Don't Repeat Yourself)
Use a generic `SaveSystem<T>` to handle serialization for different data types (Settings, PlayerStats, Workflows).

---

## 10. Testing Standards

- **Serialization Tests**: Unit test that a class can be serialized and deserialized back to an identical object.
- **Migration Tests**: Verify that version 1.0 save files can be loaded by version 2.0 system.
- **Stress Tests**: Save/Load 1000 items in a single frame to measure performance impact.

---

## 11. Performance & Memory

- **Profile JSON**: Large JSON strings can cause GC spikes. Reuse `StringBuilder`.
- **Reference Management**: Use string IDs to reference Assets. Never serialize a `GameObject` or `MonoBehaviour` directly into a file.
- **Bit-Packing**: For high-performance networking data, consider `MessagePack` or `FlatBuffers` instead of JSON.

---

## 12. Implementation Checklist & Done

- [ ] Data model is a separate `[Serializable]` POCO class.
- [ ] Static data is correctly stored in `ScriptableObjects`.
- [ ] Save file includes a `version` integer and a `timestamp`.
- [ ] I/O operations are performed asynchronously.
- [ ] Sensitivity Check: Player data is encrypted in production builds.
- [ ] Integrity Check: SHA-256 checksum is validated on Load.
- [ ] Atomic Write implemented (write to .tmp then swap).

### 12.1. Implementation Checklist Discipline

> **CRITICAL:** When implementing from a plan document:
> 1. Read the full checklist BEFORE starting.
> 2. Mark each item `[/]` (in progress) as you work.
> 3. Mark each item `[x]` (complete) AFTER verification.
> 4. Do NOT mark the task done until the save file is verified as valid JSON on disk.
