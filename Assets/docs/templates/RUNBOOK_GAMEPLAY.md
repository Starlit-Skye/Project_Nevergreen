# <Gameplay Runbook Name>

Owner: <team-or-person>
Status: draft | active | deprecated
Last verified: YYYY-MM-DD
Verified commit: <sha>
Target build: <engine-version + platform>

## Purpose
<Operational task or incident this runbook addresses>

## Scope
- In scope: <items>
- Out of scope: <items>

## Source of Truth
- Code: `<path>` (<module/function>)
- Tests: `<path>` (<scenario>)
- Design: <doc-path-or-url#section>
- Data: `<path>` (<table/key>)
- Issue/ADR: <id>

## Preconditions
- Access required: <roles/tools>
- Environment: <dev/stage/prod/live-ops>
- Safety checks: <required confirmations>

## Trigger Conditions
- Alert/signature: <what indicates this runbook should be used>

## Procedure
1. <step with exact command/action>
2. <step with expected result>
3. <step with decision branch>

## Verification
- <metric/log/query to confirm success>
- <in-game validation scenario>

## Rollback
1. <rollback step>
2. <rollback verification>

## Escalation
- Primary: <team/channel>
- Secondary: <team/channel>
- Escalate when: <conditions>

## Post-Incident Tasks
- <RCA/update docs/add regression test/add monitoring>

## Missing Evidence
- <Unknown claim + missing artifact>

## Validation
- [ ] Procedure matches current tools and build
- [ ] Verification steps are measurable
- [ ] Rollback is tested or explicitly marked Unknown
- [ ] Unknowns are explicitly labeled
- [ ] Escalation path is current

