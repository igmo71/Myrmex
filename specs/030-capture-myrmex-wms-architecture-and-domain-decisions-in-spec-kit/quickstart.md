# Quickstart: Validate Issue #30 Planning Artifacts

Use this guide to validate that issue #30 remains documentation/specification only.

## Prerequisites

- Run commands from the repository root.
- Stay on branch `30-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit`.
- Do not run production behavior or add tests for this validation.

## 1. Confirm Required Artifacts Exist

```powershell
Test-Path specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\plan.md
Test-Path specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\research.md
Test-Path specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\data-model.md
Test-Path specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\contracts\documentation-output-contract.md
Test-Path specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\quickstart.md
```

Expected outcome: every command returns `True`.

## 2. Confirm Durable Memory Docs Exist

```powershell
Test-Path .specify\memory\myrmex-architecture.md
Test-Path .specify\memory\myrmex-development-workflow.md
Test-Path .specify\memory\myrmex-topology-patterns.md
Test-Path .specify\memory\myrmex-api-error-handling.md
Test-Path .specify\memory\myrmex-testing-guidelines.md
Test-Path .specify\memory\myrmex-roadmap.md
```

Expected outcome: every command returns `True`.

## 3. Confirm No Clarification Markers Remain

```powershell
rg -n "NEEDS CLARIFICATION" `
  specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\plan.md `
  specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\research.md `
  specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\data-model.md `
  specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\contracts\documentation-output-contract.md `
  .specify\memory\myrmex-architecture.md `
  .specify\memory\myrmex-development-workflow.md `
  .specify\memory\myrmex-topology-patterns.md `
  .specify\memory\myrmex-api-error-handling.md `
  .specify\memory\myrmex-testing-guidelines.md `
  .specify\memory\myrmex-roadmap.md
```

Expected outcome: no matches.

## 4. Confirm Scope Stayed Documentation-Only

```powershell
git status --short
```

Expected outcome: changed files are limited to `AGENTS.md`, `.specify/memory/myrmex-*.md`, and files under `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/`.

## 5. Review Forbidden Work Boundaries

Review `contracts/documentation-output-contract.md` and confirm it forbids:

- Production code changes.
- Test code changes.
- Catalog/SKU implementation.
- Inventory implementation.
- Receiving implementation.
- Integration implementation.
- Broad refactoring.
- New frameworks.
- GetById/List query handler tests.
- Runtime behavior, migration, UI, API, persistence, or framework changes.

Expected outcome: all forbidden categories are present and described as out of scope.

## 6. Review Required Decisions

Review `research.md`, `data-model.md`, `plan.md`, and `.specify/memory/myrmex-*.md` and confirm they capture:

- Modular monolith, Clean Architecture and DDD-inspired structure, vertical slices, no MediatR, internal dispatching, and simple explicit code.
- Codex CLI skill invocation as the supported Spec Kit workflow.
- `$speckit-implement` as documentation execution only from a docs-only `tasks.md`.
- WMS Topology as the current reference slice.
- Warehouse, Zone, and StorageLocation as reference concepts.
- Write/action `ApiResult<T>` and read/load exception-based ProblemDetails-aware API error handling.
- Issue #28 testing expectations without new tests.
- GetById/List query handler tests as future work only.
- Catalog, SKU, Barcode, UoM, Packaging, Inventory, Receiving, and Integration as roadmap direction only.
- `StakeholderDocs/issue-30-spec-kit-stabilization.md` as historical input superseded operationally by durable memory docs.

Expected outcome: all decisions are documented without runtime implementation instructions.

## 7. Confirm Agent Context Uses Durable Guidance

```powershell
Get-Content AGENTS.md
```

Expected outcome: the Spec Kit marker block points agents to durable `.specify/memory/myrmex-*.md` guidance and allows reading the current active plan without permanently pinning issue #30 as the project context.
