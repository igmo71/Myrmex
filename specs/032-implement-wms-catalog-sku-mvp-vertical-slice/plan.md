# Implementation Plan: WMS Catalog/SKU MVP Vertical Slice

**Branch**: `032-implement-wms-catalog-sku-mvp-vertical-slice` | **Date**: 2026-06-08 | **Spec**: `specs/032-implement-wms-catalog-sku-mvp-vertical-slice/spec.md`

**Input**: Feature specification from `specs/032-implement-wms-catalog-sku-mvp-vertical-slice/spec.md`, supported by `.specify/memory/constitution.md`, durable `.specify/memory/myrmex-*.md` guidance, and existing WMS Topology domain, handlers, endpoints, API client, UI, and tests.

## Summary

Implement a small WMS Catalog/SKU vertical slice that lets users create, list, search, retrieve, update, deactivate, and reactivate SKU reference data. The slice will introduce `StockKeepingUnit` as the domain aggregate inside the existing WMS module, with command/query handlers, persistence mapping and migration, minimal API endpoints, a typed web API client, minimal MudBlazor UI, and focused regression tests.

The implementation must mirror the accepted WMS Topology patterns while keeping Catalog/SKU separate from topology concepts. It must store normalized SKU codes directly in `Code`, leave `UpdatedAtUtc` null on create, emit domain events only for real state changes, and use existing `EntityBase`/`AggregateRoot` patterns. It must not implement Inventory, Barcode, UoM, Packaging, Receiving, LPN contents, Picking, Shipping, Integration, MediatR, new frameworks, broad refactoring, or any `Myrmex.Core\Domain\Entity.cs` base type.

## Technical Context

**Language/Version**: C# on the existing .NET solution.

**Primary Dependencies**: Existing ASP.NET Core Minimal APIs, EF Core, Blazor, MudBlazor, xUnit, `Myrmex.Core`, `Myrmex.AppDispatching`, `Myrmex.AspNetCore`, and `Myrmex.Modules.Wms`.

**Storage**: Existing WMS EF Core context using SQL Server in production and SQLite-backed test context. Add a WMS `stock_keeping_units` table through EF Core configuration and migration. Store normalized SKU code directly in `Code`; do not add `NormalizedCode`.

**Testing**: Existing xUnit test project. Add domain tests, handler tests, practical SQLite/EnsureCreated persistence tests for mapping/table creation and unique `Code` index, and web API client error-handling tests for Catalog/SKU. Do not require SQL Server-specific migration execution tests.

**Target Platform**: Existing Myrmex web application and API service in the modular-monolith solution.

**Project Type**: Brownfield modular-monolith web application with WMS module vertical slices.

**Performance Goals**: Users can create a SKU in under 1 minute and find a known SKU by code or name from at least 25 records in under 30 seconds.

**Constraints**: Keep the slice small; preserve WMS Topology behavior; use explicit commands and queries through the existing internal dispatchers; use existing service result and ProblemDetails conventions; use existing `EntityBase` and `AggregateRoot` domain base classes; no new frameworks; no MediatR; no `Entity.cs`; no future roadmap areas beyond SKU reference data.

**Scale/Scope**: One WMS Catalog/SKU reference-data slice covering one aggregate, six user-facing operations, one database table, one web list page with dialog-based create/edit, and focused regression tests.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names Catalog, SKU, `StockKeepingUnit`, lifecycle states, SKU-code uniqueness, commands, queries, details contracts, and domain events before implementation details.
- **Modular Monolith Boundaries**: PASS. All runtime work stays within existing WMS, web, and test projects. Cross-boundary behavior uses existing minimal endpoints, typed web API clients, and command/query dispatching.
- **Vertical Slice Delivery**: PASS. The slice includes domain model, handlers, persistence mapping and migration, endpoints, request/response contracts, web API client, UI, and tests.
- **Testing Discipline**: PASS. Required domain, handler, persistence, API client, and UI smoke/manual validation coverage are identified before implementation tasks are generated.
- **Simplicity and Observability**: PASS. The design reuses WMS Topology patterns, adds no framework or service split, and includes explicit validation, duplicate-code, not-found, and persistence failure errors.

No constitution violations require Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/032-implement-wms-catalog-sku-mvp-vertical-slice/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── catalog-sku-api-and-ui-contract.md
├── checklists/
│   └── requirements.md
└── spec.md
```

### Source Code (repository root)

```text
Myrmex.Modules.Wms/
├── Catalog/
│   ├── Domain/StockKeepingUnits/
│   │   ├── StockKeepingUnit.cs
│   │   └── StockKeepingUnitEvents.cs
│   ├── Endpoints/
│   │   ├── CatalogEndpoints.cs
│   │   └── StockKeepingUnitEndpoints.cs
│   └── Features/StockKeepingUnits/
│       ├── CreateStockKeepingUnit.cs
│       ├── DeactivateStockKeepingUnit.cs
│       ├── GetStockKeepingUnitById.cs
│       ├── ListStockKeepingUnits.cs
│       ├── ReactivateStockKeepingUnit.cs
│       ├── StockKeepingUnitDetails.cs
│       └── UpdateStockKeepingUnitDetails.cs
├── Infrastructure/Persistence/
│   ├── Configurations/StockKeepingUnitConfiguration.cs
│   ├── Migrations/[timestamp]_AddStockKeepingUnits.cs
│   ├── WmsDatabaseNames.cs
│   └── WmsDbContext.cs
├── WmsErrors.cs
└── WmsModule.cs

Myrmex.WebApp/
├── Wms/Catalog/
│   ├── ApiException.cs
│   ├── ApiResult.cs
│   └── WmsCatalogApiClient.cs
└── Components/Pages/Wms/Catalog/SkuPages/
    ├── Index.razor
    ├── Index.razor.cs
    ├── SkuEditDialog.razor
    ├── SkuFilters.razor
    └── SkuGrid.razor

Myrmex.Tests/
└── Wms/Catalog/
    ├── Client/WmsCatalogApiClientTests.cs
    ├── Domain/StockKeepingUnitTests.cs
    ├── Features/StockKeepingUnits/
    │   ├── CreateStockKeepingUnitHandlerTests.cs
    │   ├── DeactivateStockKeepingUnitHandlerTests.cs
    │   ├── ReactivateStockKeepingUnitHandlerTests.cs
    │   └── UpdateStockKeepingUnitDetailsHandlerTests.cs
    └── Persistence/StockKeepingUnitPersistenceTests.cs
```

**Structure Decision**: Add `Catalog` as a sibling WMS capability to `Topology` inside `Myrmex.Modules.Wms`. Keep the web API client and UI under `Wms/Catalog` and `Components/Pages/Wms/Catalog` so Catalog/SKU does not get mixed into Topology. Keep the small API result/exception support types local to Catalog for this MVP if needed. Do not move, rewrite, or otherwise refactor existing Topology API client infrastructure. Keep tests under `Myrmex.Tests/Wms/Catalog` while reusing existing test infrastructure patterns.

## Phase 0: Research Output

Create `research.md` with decisions for:

- WMS Catalog capability placement.
- `StockKeepingUnit` aggregate name and user-facing SKU wording.
- Minimal SKU data shape and lifecycle.
- Normalized `Code` storage without a separate `NormalizedCode` column.
- `UpdatedAtUtc` lifecycle behavior.
- Existing `EntityBase` and `AggregateRoot` domain base class usage.
- Command/query handler set.
- EF Core table, unique index, and migration.
- API route and ProblemDetails/service-result behavior.
- Separate WMS Catalog web API client.
- Minimal MudBlazor SKU page composition.
- Regression test scope and exclusions.
- Excluded roadmap areas and rejected broader alternatives.

## Phase 1: Design Outputs

Create `data-model.md` for the `StockKeepingUnit` aggregate, domain events, command/query inputs, details projection, persistence shape, lifecycle transitions, validation rules, and out-of-scope relationships. The model must explicitly state that normalized SKU code is stored directly in `Code`, `UpdatedAtUtc` is null on create, and no new domain base type is introduced.

Create `contracts/catalog-sku-api-and-ui-contract.md` for the minimal API surface, payloads, list query parameters, error behavior, and web UI page/dialog expectations.

Create `quickstart.md` as a validation guide for implementation review. It must include commands to inspect generated artifacts, run build/test validation, and manually verify the Catalog/SKU UI without including implementation code.

Update `AGENTS.md` between the Spec Kit markers so active issue #32 work points agents to this plan in addition to durable `.specify/memory/myrmex-*.md` guidance.

## Task Generation Guard

Any issue #32 `tasks.md` must be a small vertical-slice task list and must include test tasks before implementation tasks where Principle IV applies. Tasks may touch only the Catalog/SKU implementation areas listed in this plan and supporting WMS registration/persistence files.

Tasks must not include:

- Inventory.
- Barcode model.
- UoM model or conversion.
- Packaging hierarchy.
- Receiving.
- LPN contents.
- Picking.
- Shipping.
- Integration.
- MediatR.
- New frameworks.
- Broad refactoring.
- Reworking existing WMS Topology behavior except where required to keep shared WMS registration compiling.
- Moving or rewriting existing WMS Topology API client support types.
- Creating `Myrmex.Core\Domain\Entity.cs` or any new domain base type.

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define `StockKeepingUnit` identity, code uniqueness, validation, lifecycle transitions, and domain events.
- **Modular Monolith Boundaries**: PASS. Contracts and data model keep work inside existing WMS module, web app, and test project boundaries.
- **Vertical Slice Delivery**: PASS. Design covers domain, handlers, persistence, endpoints, client, UI, and tests for each user-facing operation.
- **Testing Discipline**: PASS. Data model and quickstart identify domain, handler, persistence, API client, build, and manual UI validation.
- **Simplicity and Observability**: PASS. Research rejects broader abstractions and future roadmap features; contracts require clear validation, duplicate-code, not-found, and fallback error behavior.

No constitution violations. No complexity exceptions are requested.

## Complexity Tracking

No constitution violations. No complexity exceptions are requested.
