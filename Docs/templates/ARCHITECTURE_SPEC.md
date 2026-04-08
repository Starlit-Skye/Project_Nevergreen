# <Architecture Scope Name>

Owner: <team-or-person>
Status: draft | active | deprecated
Last verified: YYYY-MM-DD
Verified commit: <sha>
Target build: <engine-version + platform>

## Purpose
<Architectural goal and constraints>

## Scope
- In scope: <modules/platforms/features>
- Out of scope: <items>

## Source of Truth
- Code: `<path>` (<module/class>)
- Tests: `<path>` (<suite/scenario>)
- Design: <doc-path-or-url#section>
- Data: `<path>` (<table/key>)
- Issue/ADR: <id>

## Module Boundaries
- `<module>` owns <responsibility>
- `<module>` depends on <allowed dependencies>

## Lifecycle and Update Order
1. <startup phase>
2. <runtime phase>
3. <shutdown phase>

## Data Ownership
- Runtime state owner: <module>
- Save-game state owner: <module>
- Network replicated state owner: <module>

## Threading and Jobs
- Main-thread only: <systems>
- Worker/job eligible: <systems>
- Synchronization points: <where and why>

## Authority Model
- Offline: <rules>
- Online: <server-authoritative/client-authoritative per subsystem>

## Performance Budgets
- Frame budget target: <ms>
- Memory budget target: <MB>
- Streaming budget target: <ms or MB/s>

## Fault Boundaries
- <fault domain>: <containment and fallback behavior>

## Migration and Compatibility
- Backward compatibility requirements: <save/game/network version rules>
- Upgrade path: <strategy>

## Acceptance Tests
- Automated: <architecture/lifecycle/perf test paths>
- Playtest: <scenario and success criteria>

## Missing Evidence
- <Unknown claim + missing artifact>

## Validation
- [ ] Facts match current code/content
- [ ] Ownership and boundaries are explicit
- [ ] Timing/threading/authority assumptions are explicit
- [ ] Budgets include units and thresholds
- [ ] Acceptance tests are defined

