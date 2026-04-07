# <System Name>

Owner: <team-or-person>
Status: draft | active | deprecated
Last verified: YYYY-MM-DD
Verified commit: <sha>
Target build: <engine-version + platform>

## Purpose
<System responsibilities and gameplay/business outcome>

## Scope
- In scope: <items>
- Out of scope: <items>

## Source of Truth
- Code: `<path>` (<class/module>)
- Tests: `<path>` (<suite/scenario>)
- Design: <doc-path-or-url#section>
- Data: `<path>` (<table/key>)
- Issue/ADR: <id>

## Responsibilities
- <responsibility 1>
- <responsibility 2>

## Data Model
- Entity/component/object: <fields and meanings>
- Persistence keys: <save/load identifiers>

## Event Contracts
- Event: `<name>`
- Producer: `<system>`
- Consumers: `<systems>`
- Payload schema: <fields>

## Timing Model
- Update domain: <frame/tick/fixed update>
- Tick/update order: <before/after constraints>
- Budget: <ms per frame/tick>

## Determinism
- Required: <yes/no>
- Strategy: <ordered iteration, fixed delta, seeded rng, etc.>
- Known exceptions: <if any>

## Authority Model
- Single-player/offline: <rules>
- Multiplayer: <server/client ownership and write permissions>

## Performance Budget
- CPU: <ms budget>
- Memory: <MB budget>
- Entity scale target: <count>

## Error Handling and Recovery
- <error type>: <behavior>
- <recovery strategy>

## Observability
- Metrics: <names and thresholds>
- Logs: <channels/categories>
- Traces/profilers: <tools and markers>

## Acceptance Tests
- Automated: <path + test scenario>
- Playtest: <scenario + expected outcome>

## Missing Evidence
- <Unknown claim + missing artifact>

## Validation
- [ ] Facts match current code/content
- [ ] Timing, authority, and determinism are explicit
- [ ] Performance budgets are stated with units
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined

