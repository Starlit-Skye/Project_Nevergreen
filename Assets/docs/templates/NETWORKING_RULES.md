# <Networking Scope Name>

Owner: <team-or-person>
Status: draft | active | deprecated
Last verified: YYYY-MM-DD
Verified commit: <sha>
Target build: <engine-version + platform>

## Purpose
<Networking model goals for gameplay correctness, responsiveness, and security>

## Scope
- In scope: <subsystems/entities>
- Out of scope: <items>

## Source of Truth
- Code: `<path>` (<net module/class>)
- Tests: `<path>` (<multiplayer scenario>)
- Design: <doc-path-or-url#section>
- Data: `<path>` (<config table/key>)
- Issue/ADR: <id>

## Authority Model
- Server authoritative domains: <list>
- Client authoritative domains: <list>
- Ownership transfer rules: <conditions>

## Prediction and Reconciliation
- Predicted actions: <list>
- Reconciliation trigger: <condition>
- Correction strategy: <snap/lerp/replay>

## Replication Rules
- Replicated state: <fields/entities>
- Replication frequency: <rate>
- Interest management: <rules>

## Determinism and Simulation Timing
- Tick rate: <value + unit>
- Fixed-step requirements: <yes/no + details>
- RNG policy: <seed source and sync behavior>

## Lag and Loss Handling
- Jitter buffer: <settings>
- Timeout/disconnect thresholds: <values>
- Graceful degradation: <behavior>

## Anti-Cheat Constraints
- Trust boundaries: <what clients can/cannot author>
- Validation checks: <server-side checks>
- Audit logging: <events and fields>

## Performance Budget
- Bandwidth budget: <KB/s per client target>
- CPU budget: <ms per tick target>

## Acceptance Tests
- Automated: <network test path/scenario>
- Playtest: <latency packet-loss matrix + expected behavior>

## Missing Evidence
- <Unknown claim + missing artifact>

## Validation
- [ ] Authority boundaries are explicit
- [ ] Prediction/reconciliation behavior is explicit
- [ ] Replication payload and frequency are explicit
- [ ] Security constraints are explicit
- [ ] Acceptance tests are defined

