# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]

**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by `/speckit-plan`.

## Summary

[Primary requirement and the smallest repository-aligned technical approach]

## Technical Context

**Language/Version**: [.NET 10 or a user-approved alternative]

**Primary Dependencies**: [Existing Myrmex dependencies used by this feature]

**Storage**: [SQL Server/EF Core impact, another existing store, or N/A]

**Verification**: [Independent outcomes and concise developer-performed manual checks]

**Target Platform**: [Existing Myrmex host or an explicit user-supplied target]

**Project Type**: [Affected Myrmex projects/modules]

**Performance/Scale Requirements**: [User-supplied or accepted requirements, otherwise N/A]

**Constraints**: [Applicable domain, security, operational, or compatibility constraints]

## Constitution Check

*GATE: Must pass before design and be re-checked after any material plan change.*

- [ ] Domain invariants and state transitions remain in domain/application code;
      state mutations are validated and atomic.
- [ ] The design stays within the owning module and vertical slice; cross-module
      interactions use explicit contracts rather than shared persistence.
- [ ] Commands, queries, DTOs, endpoints, and UI responsibilities are explicit and thin.
- [ ] Acceptance scenarios remain independently verifiable without automated tests.
- [ ] The design is the smallest adequate solution; new abstractions and dependencies
      have a current, documented use case.
- [ ] Security, configuration, persistence impact, health checks, and diagnostics are
      addressed where applicable.
- [ ] Build, migration, commit, and pull request operations appear only as
      developer-controlled handoff notes.

## Supporting Artifacts

`plan.md` is required. Create each optional artifact only when it adds concrete value:

- `research.md`: material repository-specific unknowns cannot be resolved from existing code.
- `data-model.md`: the feature introduces or materially changes persistent domain data.
- `contracts/`: an external or cross-module contract is introduced or changed.
- `quickstart.md`: concise developer-performed manual verification needs a reusable guide.

Record created artifacts and briefly justify omitted ones. Do not create empty or
ceremonial artifacts.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── spec.md
├── plan.md
├── research.md          # optional
├── data-model.md        # optional
├── contracts/           # optional
├── quickstart.md        # optional
└── tasks.md             # created by /speckit-tasks
```

### Source Code (repository root)

```text
Myrmex.Modules.Wms/<Capability>/
Myrmex.Shared/<Capability>/
Myrmex.WebApp/Components/Pages/<Capability>/
Myrmex.Identity/
Myrmex.Integrations/
```

**Structure Decision**: [List only affected existing paths and justified new paths]

## Persistence & Migration Handoff

[Describe model/schema impact and migration considerations, or state that none exist.
Migration generation, review, and application are developer-controlled.]

## Developer Actions

[Include only applicable non-executable handoff notes. Do not use task IDs or checkboxes.]

- Build the affected projects or solution.
- Generate and review EF Core migrations.
- Apply database migrations.
- Perform manual acceptance.
- Create the Git commit.
- Create or publish the pull request.

## Complexity Tracking

> **Fill ONLY when a Constitution Check exception requires justification.**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [specific exception] | [current need] | [why the simpler option is insufficient] |
