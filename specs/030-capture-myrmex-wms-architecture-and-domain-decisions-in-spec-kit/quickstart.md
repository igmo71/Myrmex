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

## 2. Confirm No Clarification Markers Remain

```powershell
rg -n "NEEDS CLARIFICATION" `
  specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\plan.md `
  specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\research.md `
  specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\data-model.md `
  specs\030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit\contracts\documentation-output-contract.md
```

Expected outcome: no matches.

## 3. Confirm Scope Stayed Documentation-Only

```powershell
git status --short
```

Expected outcome: changed files are limited to `AGENTS.md` and files under `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/`.

## 4. Review Forbidden Work Boundaries

Review `contracts/documentation-output-contract.md` and confirm it forbids:

- Production code changes.
- Test code changes.
- Catalog/SKU implementation.
- Inventory implementation.
- Receiving implementation.
- Integration implementation.
- Broad refactoring.
- New frameworks.

Expected outcome: all forbidden categories are present and described as out of scope.

## 5. Review Required Decisions

Review `research.md`, `data-model.md`, and `plan.md` and confirm they capture:

- Modular monolith, Clean Architecture and DDD-inspired structure, vertical slices, no MediatR, internal dispatching, and simple explicit code.
- Codex CLI skill invocation as the supported Spec Kit workflow.
- WMS Topology as the current reference slice.
- Warehouse, Zone, and StorageLocation as reference concepts.
- Write/action `ApiResult<T>` and read/load exception-based ProblemDetails-aware API error handling.
- Issue #28 testing expectations without new tests.
- Catalog, SKU, Barcode, UoM, Packaging, Inventory, Receiving, and Integration as roadmap direction only.

Expected outcome: all decisions are documented without runtime implementation instructions.

## 6. Confirm Agent Context Points To The Plan

```powershell
Get-Content AGENTS.md
```

Expected outcome: the Spec Kit marker block points agents to `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/plan.md`.
