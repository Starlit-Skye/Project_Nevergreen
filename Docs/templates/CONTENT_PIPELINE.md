# <Content Pipeline Scope Name>

Owner: <team-or-person>
Status: draft | active | deprecated
Last verified: YYYY-MM-DD
Verified commit: <sha>
Target build: <engine-version + platform>

## Purpose
<How content is authored, validated, packaged, and delivered>

## Scope
- In scope: <asset/content types>
- Out of scope: <items>

## Source of Truth
- Code: `<path>` (<importer/validator>)
- Tests: `<path>` (<pipeline test>)
- Design: <doc-path-or-url#section>
- Data: `<path>` (<table/key>)
- Issue/ADR: <id>

## Asset Types and Ownership
- `<type>`: owner <team>, source format <format>, runtime format <format>

## Directory and Naming Conventions
- Root paths: `<paths>`
- Naming: <pattern and forbidden patterns>
- Versioning tags: <rules>

## Import Rules
- Import settings: <compression/mips/flags>
- Validation rules: <required checks>
- Failure behavior: <block warning/fail build>

## Data Validation
- Schema location: `<path>`
- Required fields: <list>
- Referential integrity rules: <rules>

## Build and Packaging
- Build steps: <ordered list>
- Packaging rules: <platform-specific differences>
- Delivery channel: <bundle/patch/live content>

## Performance Constraints
- Memory constraints per asset class: <values>
- Streaming constraints: <values>
- Runtime budget checks: <rules>

## Rollback Strategy
- Content rollback mechanism: <version pin, package rollback, etc.>

## Acceptance Tests
- Automated: <validation script/test path>
- Manual: <editor/in-game smoke checks>

## Missing Evidence
- <Unknown claim + missing artifact>

## Validation
- [ ] Conventions match current repo and tooling
- [ ] Import and validation rules are executable
- [ ] Packaging and rollback are documented
- [ ] Unknowns are explicitly labeled
- [ ] Acceptance tests are defined

