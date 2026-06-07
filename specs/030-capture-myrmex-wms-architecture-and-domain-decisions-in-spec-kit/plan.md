# Implementation Plan: Capture Myrmex WMS Architecture and Domain Decisions in Spec Kit

**Branch**: `30-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit` | **Date**: 2026-06-07 | **Spec**: `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md`

**Input**: Feature specification from `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md`, supported by `.specify/memory/constitution.md`, `StakeholderDocs/issue-30-spec-kit-stabilization.md`, `README.md`, and `AGENTS.md`.

## Summary

Issue #30 is a brownfield stabilization effort to capture accepted Myrmex WMS architecture, workflow, topology, API error-handling, testing, and roadmap decisions in Spec Kit documentation. The implementation approach is documentation-only: create small staged planning artifacts that future agents and maintainers can use without changing production code, test code, runtime behavior, or domain implementations.

The plan explicitly forbids Catalog/SKU, Inventory, Receiving, and Integration implementation. Roadmap terms are captured as future direction only.

## Technical Context

**Language/Version**: Markdown documentation in an existing .NET repository. Runtime language remains the existing project stack and is not changed by this feature.

**Primary Dependencies**: Spec Kit artifacts and Codex CLI skills; source inputs are `.specify/memory/constitution.md`, `spec.md`, `StakeholderDocs/issue-30-spec-kit-stabilization.md`, `README.md`, and `AGENTS.md`.

**Storage**: Git-tracked documentation files only. No database, persistence, migration, or runtime configuration changes.

**Testing**: Documentation review and quickstart validation only. No production tests or test code changes are allowed.

**Target Platform**: Repository documentation consumed by maintainers, reviewers, and Codex CLI agents.

**Project Type**: Brownfield documentation/specification stabilization for a modular-monolith WMS / fulfillment project.

**Performance Goals**: A reviewer can verify in under 5 minutes that issue #30 is documentation-only and that forbidden runtime work is excluded.

**Constraints**: No production code changes, no test code changes, no Catalog/SKU implementation, no Inventory implementation, no Receiving implementation, no Integration implementation, no broad refactoring, and no new frameworks.

**Scale/Scope**: One Spec Kit feature directory plus the existing agent-context marker in `AGENTS.md`. Future architecture memory documents may be staged as small documentation outputs only.

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
StakeholderDocs/          # Reference source for issue #30
.specify/memory/          # Existing architecture governance source
AGENTS.md                 # Update only the Spec Kit plan pointer
```

**Structure Decision**: Use the existing Spec Kit feature directory for all new planning artifacts. Do not edit runtime projects, test projects, application services, endpoints, UI components, EF Core mappings, or migrations.

## Phase 0: Research Output

Create `research.md` to resolve planning decisions without unresolved clarification markers. Required decisions:

- Brownfield documentation-only scope.
- Codex CLI skill workflow instead of Copilot slash-command assumptions.
- WMS Topology as the accepted reference slice.
- Write/action `ApiResult<T>` and read/load exception-based ProblemDetails-aware API error-handling documentation.
- Existing issue #28 testing categories as expectations only.
- Roadmap vocabulary as non-implementation language.

## Phase 1: Design Outputs

Create `data-model.md` for documentation entities, not runtime entities. The model must describe Spec Kit artifacts, architecture decision guidance, workflow memory, topology pattern memory, API error-handling memory, testing expectation memory, roadmap direction memory, and scope exclusions.

Create `contracts/documentation-output-contract.md` as the downstream planning contract for issue #30. The contract must state that future outputs derived from this issue can create or update documentation/specification files only.

Create `quickstart.md` as a validation guide for reviewers. It must verify scope, artifact completeness, forbidden runtime changes, and absence of unresolved clarification markers without running production behavior or adding tests.

Update `AGENTS.md` between the Spec Kit markers to point to this plan file.

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts model documentation concepts and reference WMS terms without adding runtime domain rules.
- **Modular Monolith Boundaries**: PASS. Contracts constrain documentation outputs and forbid module changes.
- **Vertical Slice Delivery**: PASS with documentation exception. The reference slice is described, not implemented or modified.
- **Testing Discipline**: PASS with documentation exception. Quickstart validates documentation scope; automated test expansion remains future work only.
- **Simplicity and Observability**: PASS. The design uses simple Markdown artifacts and records current API error-handling conventions without framework additions.

## Complexity Tracking

No constitution violations. No complexity exceptions are requested.
