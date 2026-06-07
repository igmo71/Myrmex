# Implementation Plan: Capture Myrmex WMS Architecture and Domain Decisions in Spec Kit

**Branch**: `030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit` | **Date**: 2026-06-07 | **Spec**: `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md`

**Input**: Feature specification from `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md`, supported by `.specify/memory/constitution.md`, `StakeholderDocs/issue-30-spec-kit-stabilization.md`, `README.md`, and `AGENTS.md`.

## Summary

Issue #30 is a brownfield stabilization effort to capture accepted Myrmex WMS architecture, workflow, topology, API error-handling, testing, and roadmap decisions in Spec Kit documentation. The implementation approach is documentation-only: create small staged planning artifacts and durable `.specify/memory/myrmex-*.md` documents that future agents and maintainers can use without changing production code, test code, runtime behavior, or domain implementations.

The plan explicitly forbids Catalog/SKU, Inventory, Receiving, and Integration implementation. Roadmap terms are captured as future direction only.

For issue #30, `$speckit-implement` is allowed only as documentation execution from a docs-only `tasks.md`. It does not authorize production code, test code, runtime behavior, migrations, UI, API, persistence, or framework changes.

## Technical Context

**Language/Version**: Markdown documentation in an existing .NET repository. Runtime language remains the existing project stack and is not changed by this feature.

**Primary Dependencies**: Spec Kit artifacts and Codex CLI skills; source inputs are `.specify/memory/constitution.md`, `spec.md`, `StakeholderDocs/issue-30-spec-kit-stabilization.md`, `README.md`, and `AGENTS.md`.

**Storage**: Git-tracked documentation files only. Required durable outputs are `.specify/memory/myrmex-architecture.md`, `.specify/memory/myrmex-development-workflow.md`, `.specify/memory/myrmex-topology-patterns.md`, `.specify/memory/myrmex-api-error-handling.md`, `.specify/memory/myrmex-testing-guidelines.md`, and `.specify/memory/myrmex-roadmap.md`. No database, persistence, migration, or runtime configuration changes.

**Testing**: Documentation review and quickstart validation only. No production tests or test code changes are allowed.

**Target Platform**: Repository documentation consumed by maintainers, reviewers, and Codex CLI agents.

**Project Type**: Brownfield documentation/specification stabilization for a modular-monolith WMS / fulfillment project.

**Performance Goals**: A reviewer can verify in under 5 minutes that issue #30 is documentation-only, that all six durable memory documents exist, and that forbidden runtime work is excluded.

**Constraints**: No production code changes, no test code changes, no Catalog/SKU implementation, no Inventory implementation, no Receiving implementation, no Integration implementation, no GetById/List query handler tests, no broad refactoring, and no new frameworks.

**Scale/Scope**: One Spec Kit feature directory, six durable Myrmex memory documents under `.specify/memory/`, and the managed Spec Kit marker in `AGENTS.md`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names WMS Topology, Warehouse, Zone, StorageLocation, and roadmap vocabulary as documentation context only. No domain invariants, commands, queries, events, or runtime behavior are changed.
- **Modular Monolith Boundaries**: PASS. The plan preserves the existing modular monolith and records accepted boundaries. No module dependency or runtime boundary change is introduced.
- **Vertical Slice Delivery**: PASS with documentation exception. Issue #30 does not deliver user-facing runtime behavior. It documents WMS Topology as the current reference slice and forbids new vertical slice implementation.
- **Testing Discipline**: PASS with documentation exception. Existing issue #28 testing categories are documented as expectations. No new tests are added because the user explicitly forbids test code changes for this issue.
- **Simplicity and Observability**: PASS. The plan records existing local patterns, avoids new frameworks and broad abstractions, and documents accepted API error-handling conventions without changing endpoint behavior.

No constitution violations require Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── documentation-output-contract.md
└── spec.md

.specify/memory/
├── constitution.md
├── myrmex-architecture.md
├── myrmex-development-workflow.md
├── myrmex-topology-patterns.md
├── myrmex-api-error-handling.md
├── myrmex-testing-guidelines.md
└── myrmex-roadmap.md
```

### Source Code (repository root)

```text
Myrmex.ApiService/        # Reference only; no changes
Myrmex.AppDispatching/    # Reference only; no changes
Myrmex.AppHost/           # Reference only; no changes
Myrmex.AspNetCore/        # Reference only; no changes
Myrmex.Core/              # Reference only; no changes
Myrmex.Modules.Wms/       # Reference only; no changes
Myrmex.ServiceDefaults/   # Reference only; no changes
Myrmex.Tests/             # Reference only; no changes
Myrmex.WebApp/            # Reference only; no changes
StakeholderDocs/          # Historical stakeholder input only
.specify/memory/          # Durable operational guidance target
AGENTS.md                 # Managed Spec Kit context pointer
```

**Structure Decision**: Use the existing Spec Kit feature directory for planning artifacts and `.specify/memory/` for durable operational guidance. Do not edit runtime projects, test projects, application services, endpoints, UI components, EF Core mappings, migrations, or framework configuration.

## Phase 0: Research Output

Create `research.md` to resolve planning decisions without unresolved clarification markers. Required decisions:

- Brownfield documentation-only scope.
- Durable `.specify/memory/myrmex-*.md` outputs rather than feature planning artifacts only.
- Codex CLI skill workflow instead of Copilot slash-command assumptions.
- `$speckit-implement` as docs-only execution from a docs-only `tasks.md`.
- WMS Topology as the accepted reference slice.
- Write/action `ApiResult<T>` and read/load exception-based ProblemDetails-aware API error-handling documentation.
- Existing issue #28 testing categories as expectations only.
- GetById/List query handler tests as future work only.
- Roadmap vocabulary as non-implementation language.
- `StakeholderDocs/issue-30-spec-kit-stabilization.md` as historical input superseded operationally by durable memory docs.

## Phase 1: Design Outputs

Create `data-model.md` for documentation entities, not runtime entities. The model must describe Spec Kit artifacts, durable memory documents, architecture decision guidance, workflow memory, topology pattern memory, API error-handling memory, testing guideline memory, roadmap direction memory, task-generation guards, and scope exclusions.

Create `contracts/documentation-output-contract.md` as the downstream planning contract for issue #30. The contract must state that future outputs derived from this issue can create or update documentation/specification files only.

Create `quickstart.md` as a validation guide for reviewers. It must verify scope, artifact completeness, forbidden runtime changes, durable memory document presence, and absence of unresolved clarification markers without running production behavior or adding tests.

Update `AGENTS.md` between the Spec Kit markers so it no longer permanently points only to the issue #30 plan. During active work it may mention the current active plan pattern; durable guidance must point to `.specify/memory/myrmex-*.md`.

## Task Generation Guard

Any issue #30 `tasks.md` must be documentation-only. It must not include tasks for:

- Production code changes.
- Test code changes.
- Runtime behavior changes.
- Migrations.
- UI changes.
- API changes.
- Persistence changes.
- Framework changes.
- Catalog/SKU implementation.
- Inventory implementation.
- Receiving implementation.
- Integration implementation.
- Broad refactoring.
- GetById/List query handler tests.

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts model documentation concepts and reference WMS terms without adding runtime domain rules.
- **Modular Monolith Boundaries**: PASS. Contracts constrain documentation outputs and forbid module changes.
- **Vertical Slice Delivery**: PASS with documentation exception. The reference slice is described, not implemented or modified.
- **Testing Discipline**: PASS with documentation exception. Quickstart validates documentation scope; automated test expansion remains future work only.
- **Simplicity and Observability**: PASS. The design uses simple Markdown artifacts and records current API error-handling conventions without framework additions.

## Complexity Tracking

No constitution violations. No complexity exceptions are requested.
