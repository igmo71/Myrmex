# Implementation Plan: Normalize WMS List Conventions

**Branch**: `090-normalize-wms-list-contracts` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/090-normalize-wms-list-contracts/spec.md`

> **Phase note (2026-07-02)**: This plan records the completed Phase 1 audit. `spec.md` now defines the focused Phase 2 deterministic-ordering scope and is ready for a refreshed implementation plan. `research.md` remains the decision base.

## Summary

Perform a static, evidence-backed audit of nine WMS list slices against the durable server-driven list convention. Produce a decision report covering contract ownership, backend pipeline order, sorting and paging, WebApp grids, cancellation/errors, test protection, and a risk-ranked normalization sequence. This planning phase changes documentation artifacts only.

## Technical Context

**Language/Version**: C# / .NET 10 repository; Markdown planning output  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core, Blazor, MudBlazor, xUnit; no dependency changes  
**Storage**: PostgreSQL-backed EF Core modules inspected statically; no database access  
**Testing**: Existing xUnit suites inspected only; builds and tests are not executed  
**Target Platform**: Myrmex backend and Blazor WebApp repository  
**Project Type**: Modular .NET WebApp/API solution  
**Performance Goals**: Preserve bounded, server-driven list behavior; audit count-before-paging and backend projection  
**Constraints**: Audit-only; no application, test, resource, contract, route, schema, migration, runtime, or infrastructure changes  
**Scale/Scope**: Nine WMS slices across catalog, topology, and inventory

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain-first design**: PASS. The audit does not move behavior into UI or transport types and checks domain/module ownership.
- **Modular boundaries**: PASS. Shared contracts are checked for transport-only dependencies; EF projection remains module-owned.
- **Explicit vertical slices**: PASS. Findings trace endpoint-to-handler-to-client-to-grid flow per slice.
- **Behavior-protecting tests**: PASS. Recommendations target the lowest layer owning each risk and avoid duplicate matrices.
- **Simplicity and evidence**: PASS. Existing local patterns take precedence; no new abstraction is introduced.
- **Post-design re-check**: PASS. Phase 0 and Phase 1 artifacts preserve these boundaries and the audit-only scope.

## Project Structure

### Documentation (this feature)

```text
specs/090-normalize-wms-list-contracts/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
`-- checklists/
    `-- requirements.md
```

No `contracts/` artifact is created because this feature audits existing interfaces and designs no external contract.

### Source Code (repository root)
```text
Myrmex.Shared/Common/                         # Shared list primitives
Myrmex.Shared/Wms/                            # Public WMS contracts and sort constants
Myrmex.Core/Application/Queries/              # Paging normalization policy
Myrmex.Modules.Wms/Catalog/Features/          # Catalog handlers/projections
Myrmex.Modules.Wms/Topology/Features/         # Topology handlers/projections
Myrmex.Modules.Wms/Inventory/Features/        # Inventory handlers/projections
Myrmex.WebApp/Wms/                            # Clients, grid requests, pages, grids
Myrmex.Tests/Wms/                             # Existing behavioral/boundary tests
```

**Structure Decision**: Keep the audit in the feature directory. Use durable memory as the normative baseline and the detailed architecture document as supporting context. Cite inspected paths without modifying them.

## Architectural Design Notes

- **Domain concepts first**: Audit catalog, topology, balances, ledger, transfers, and counts without changing domain rules.
- **Shared boundary**: Verify cross-boundary list contracts are transport-only and owned by `Myrmex.Shared`; identify legacy WebApp-local DTO duplication.
- **Internal boundary**: Preserve explicit module-owned queries/handlers distinct from public requests.
- **Projection**: Verify EF expressions stay in the owning module and DTO projection occurs before materialization.
- **Server lists**: Verify filter, count, deterministic sort, normalized `Skip`/`Take`, projection, and `ListResult<T>` order.
- **Client/grids**: Compare server-driven grids with bounded client-side legacy grids, including defaults and reload/reset semantics.
- **Cancellation/errors**: Trace cancellation and preserve read-list exception/ProblemDetails conventions.
- **Testing**: Recommend only behavior-owning tests for unstable paging, sorting, binding, URLs, and cancellation.
- **Pattern precedence**: Inventory lists and Warehouse grid provide accepted local examples; no generalized framework is proposed.

## Phase Outputs

- **Phase 0**: [research.md](./research.md), the evidence-backed audit and prioritization.
- **Phase 1**: [data-model.md](./data-model.md) defines report records; [quickstart.md](./quickstart.md) defines static review and handoff.
- **Agent context**: the managed `AGENTS.md` section points active work at this feature plan.

## Validation Plan

- Confirm all nine slices appear in compact and detailed findings.
- Confirm material findings cite repository paths and distinguish absent from unreviewed boundaries.
- Confirm deterministic sorting requires explicit tie-breakers.
- Confirm recommendations preserve contracts, behavior, imported data, and module boundaries.
- Search for unresolved template markers.
- Review the Git diff for documentation-only scope.
- Do not run builds, tests, WebApp, AppHost, Docker, infrastructure, migrations, or database updates.

## Complexity Tracking

No constitution violations or test exceptions are introduced. The audit recommends convergence on accepted patterns rather than a generalized list framework.
