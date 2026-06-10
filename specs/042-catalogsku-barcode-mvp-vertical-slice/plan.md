# Implementation Plan: Catalog/SKU Barcode MVP Vertical Slice

**Branch**: `042-catalogsku-barcode-mvp-vertical-slice` | **Date**: 2026-06-09 | **Spec**: `specs/042-catalogsku-barcode-mvp-vertical-slice/spec.md`

**Input**: Feature specification from `specs/042-catalogsku-barcode-mvp-vertical-slice/spec.md`, GitHub issue #42, `.specify/memory/constitution.md`, durable Myrmex architecture/testing/API guidance, and the existing Catalog/SKU and Catalog/UoM implementation and planning artifacts.

## Summary

Implement a narrow WMS Catalog/SKU Barcode master-data vertical slice after the established Catalog/SKU and Catalog/UoM slices. Add a concrete `SkuBarcode` Catalog aggregate with create, list, get, update, deactivate, and reactivate behavior; persistence mapping and migration; Catalog endpoints; and focused tests for barcode-specific domain, handler, persistence, and API/client behavior.

The implementation must keep barcode work SKU-specific for this MVP. Use `BarcodeSymbology` and a `Symbology` field for barcode format values, trim barcode values before storing them directly in `Value`, preserve casing, enforce case-sensitive uniqueness after trimming, and avoid `NormalizedValue`. Explicit create/update with `IsPrimary = true` selects the default barcode by clearing other active primary barcodes for the same SKU. Lifecycle operations must not silently choose a default: deactivation clears `IsPrimary` only on the deactivated barcode when needed and does not promote another barcode; reactivation returns the barcode active and non-primary.

This plan must not introduce BarcodeType reference data, a generic Barcode table, Barcode module, OwnerType/OwnerId model, IHasBarcodes abstraction, generic barcode ownership, barcode scanning, printing, labels, GS1 parsing, check digit validation, packaging, SKU/UoM conversion, inventory, receiving, LPN, picking/shipping, integration, or UI screens.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET 10 solution.

**Primary Dependencies**: Existing ASP.NET Core Minimal APIs, EF Core, xUnit, `Myrmex.Core`, `Myrmex.AppDispatching`, `Myrmex.AspNetCore`, `Myrmex.Modules.Wms`, and existing Catalog API/client primitives where relevant. Blazor/MudBlazor UI pages are out of scope for this phase.

**Storage**: Existing WMS EF Core context using SQL Server in production and SQLite-backed test context. Add a WMS `sku_barcodes` table under the existing `wms` schema. Store trimmed barcode values directly in `Value`; do not add `NormalizedValue`. Persist `Symbology` as a string value. Protect `Value` with case-sensitive uniqueness after trimming and add an index on `StockKeepingUnitId`.

**Testing**: Existing xUnit test project. Add focused barcode tests because this slice introduces new behavior beyond repeated code/name reference data: trimming-only value normalization, case-sensitive uniqueness, symbology validation, SKU relationship validation, active primary selection, and lifecycle primary clearing/non-restoration. Add domain tests, handler tests, persistence mapping/index/collation tests, and Catalog API/client route/DTO/result tests where not already protected by SKU/UoM patterns. Do not add endpoint or UI automation infrastructure.

**Target Platform**: Existing Myrmex API service and WMS module in the modular-monolith solution. WebApp UI implementation is out of scope; WebApp API client support may be extended only if it follows existing Catalog client primitives without adding pages, navigation, dialogs, grids, or UI workflows.

**Project Type**: Brownfield modular-monolith web application with WMS module vertical slices.

**Performance Goals**: Users can assign a valid barcode to an existing SKU and see the resulting active barcode details in under 1 minute; users can find barcode assignments for a specific SKU from at least 25 barcode records in under 30 seconds.

**Constraints**: Keep the slice small; preserve existing Catalog/SKU and Catalog/UoM behavior; use explicit commands and queries through existing internal dispatchers; use existing service result, ProblemDetails, `ApiResult<T>`, and API exception conventions; no new frameworks; no MediatR; no new generic barcode abstractions; no UI implementation; no build, test, app startup, database update, EF migration generation, or EF migration application commands run automatically.

**Scale/Scope**: One concrete WMS Catalog/SKU Barcode master-data slice covering one aggregate, six user-facing operations, one database table, one SKU-specific API endpoint group, focused tests, and developer-controlled migration generation/application.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names Catalog, SKU, SKU Barcode, `SkuBarcode`, `StockKeepingUnitId`, `BarcodeSymbology`, `Symbology`, value uniqueness, primary-barcode invariants, lifecycle states, commands, queries, details contracts, and domain events before implementation details.
- **Modular Monolith Boundaries**: PASS. Runtime work stays within the existing WMS module, Catalog API/client surface, and test project boundaries. Cross-boundary behavior uses existing minimal endpoints, typed Catalog API client primitives where needed, and command/query dispatching.
- **Vertical Slice Delivery**: PASS. The slice covers domain model, handlers, persistence mapping and migration, endpoints, request/response contracts, API/client integration where applicable, and tests. UI pages are explicitly not applicable in this phase.
- **Testing Discipline**: PASS with documented Principle IV endpoint/UI automation exception below. Focused domain, handler, persistence, API/client, regression, and manual API validation are identified before tasks are generated.
- **Simplicity and Observability**: PASS. The design reuses Catalog/SKU and Catalog/UoM patterns, avoids generic barcode ownership and new service splits, and keeps diagnostics to existing validation, duplicate-value, missing-SKU, not-found, unsupported-primary-change, persistence, ProblemDetails, service-result, `ApiResult<T>`, and API exception behavior.

## Project Structure

### Documentation (this feature)

```text
specs/042-catalogsku-barcode-mvp-vertical-slice/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── catalog-sku-barcode-api-contract.md
├── checklists/
│   └── requirements.md
└── spec.md
```

### Source Code (repository root)

```text
Myrmex.Modules.Wms/
├── Catalog/
│   ├── Domain/SkuBarcodes/
│   │   ├── BarcodeSymbology.cs
│   │   ├── SkuBarcode.cs
│   │   └── SkuBarcodeEvents.cs
│   ├── Endpoints/
│   │   ├── CatalogEndpoints.cs
│   │   └── SkuBarcodeEndpoints.cs
│   └── Features/SkuBarcodes/
│       ├── CreateSkuBarcode.cs
│       ├── DeactivateSkuBarcode.cs
│       ├── GetSkuBarcodeById.cs
│       ├── ListSkuBarcodes.cs
│       ├── ReactivateSkuBarcode.cs
│       ├── SkuBarcodeDetails.cs
│       └── UpdateSkuBarcodeDetails.cs
├── Infrastructure/Persistence/
│   ├── Configurations/SkuBarcodeConfiguration.cs
│   ├── Migrations/YYYYMMDDHHMMSS_AddSkuBarcodes.cs
│   ├── WmsDatabaseNames.cs
│   └── WmsDbContext.cs
├── WmsErrors.cs
└── WmsModule.cs

Myrmex.WebApp/
└── Wms/Catalog/
    └── WmsCatalogApiClient.cs

Myrmex.Tests/
└── Wms/Catalog/
    ├── Client/WmsCatalogApiClientTests.cs
    ├── Domain/SkuBarcodeTests.cs
    ├── Features/SkuBarcodes/
    │   ├── CreateSkuBarcodeHandlerTests.cs
    │   ├── ListSkuBarcodesHandlerTests.cs
    │   ├── GetSkuBarcodeByIdHandlerTests.cs
    │   ├── UpdateSkuBarcodeDetailsHandlerTests.cs
    │   └── SkuBarcodeLifecycleHandlerTests.cs
    └── Persistence/SkuBarcodePersistenceTests.cs
```

**Structure Decision**: Extend the existing WMS `Catalog` capability that already owns SKU and UoM. Add `SkuBarcode` as a SKU-specific Catalog aggregate with a required `StockKeepingUnitId` relationship, not as a child collection on `StockKeepingUnit` and not as a generic barcode ownership model. Add SKU barcode endpoints under `/api/wms/catalog/sku-barcodes`. Do not add Blazor page components, Catalog navigation, dialogs, grids, or UI smoke tasks in this phase.

## Phase 0: Research Output

Create `research.md` with decisions for:

- `SkuBarcode` as a concrete Catalog aggregate related to `StockKeepingUnit`.
- `BarcodeSymbology` and `Symbology` terminology.
- Minimal SKU barcode data shape and lifecycle.
- Trimming-only value normalization stored directly in `Value`.
- Case-sensitive `Value` uniqueness after trimming.
- Primary barcode selection and lifecycle rules.
- Existing `AggregateRoot`/`EntityBase` usage.
- Domain events only for real changes.
- Command/query handler set.
- EF Core table, relationship, indexes, case-sensitive uniqueness, and migration.
- API route and service-result/ProblemDetails behavior.
- Catalog API client support without UI implementation.
- Focused barcode-specific testing scope.
- Endpoint/UI automated test deferral and manual API validation scope.
- Explicit non-goals and rejected broader alternatives.

## Phase 1: Design Outputs

Create `data-model.md` for the `SkuBarcode` aggregate, `BarcodeSymbology` constrained values, domain events, command/query inputs, details projection, persistence shape, lifecycle transitions, validation rules, and out-of-scope relationships. The model must explicitly state that trimmed barcode value is stored directly in `Value`, no `NormalizedValue` exists, uniqueness is case-sensitive after trimming, `Symbology` is persisted as a string value, lifecycle operations do not choose defaults, and no generic barcode ownership model is introduced.

Create `contracts/catalog-sku-barcode-api-contract.md` for the minimal API surface, payloads, list query parameters including optional SKU filter, error behavior, and API client methods if WebApp client support is included. The contract must not define UI routes, page components, navigation, grids, dialogs, or UI workflows.

Create `quickstart.md` as a validation guide for implementation review. It must include commands to inspect generated artifacts, recommended developer-controlled build/test/migration commands, manual API validation scenarios, and checks that no UI or generic barcode abstractions were added. It must not include implementation code or instruct Codex to run build/test/startup/database/migration commands automatically.

Update `AGENTS.md` between the Spec Kit markers so active issue #42 work points agents to this plan in addition to durable `.specify/memory/myrmex-*.md` guidance.

## Task Generation Guard

Any issue #42 `tasks.md` must be a small Catalog/SKU Barcode vertical-slice task list and must include test tasks before implementation tasks where Principle IV applies. Tasks may touch only the Catalog/SKU Barcode implementation areas listed in this plan and supporting WMS registration/persistence files.

Tasks must not include:

- BarcodeType reference data.
- Generic Barcode table.
- Barcode module.
- OwnerType/OwnerId model.
- IHasBarcodes or generic barcode ownership abstraction.
- Barcode scanning.
- Barcode printing.
- Labels.
- GS1 parsing.
- Barcode check digit validation.
- Packaging.
- SKU/UoM conversion.
- Inventory.
- Receiving.
- LPN behavior.
- Picking or shipping behavior.
- External integration behavior.
- Blazor UI pages, navigation, dialogs, grids, forms, or component tests.
- New endpoint/UI test frameworks.
- New logging, telemetry, observability, or diagnostics infrastructure.
- MediatR or new architectural frameworks.
- Broad refactoring.
- Moving or rewriting existing WMS Topology API client support types.
- Reworking existing Catalog/SKU or Catalog/UoM behavior except where shared Catalog registration must compile.

## Developer-Controlled Migration Commands

Migration work is expected because `SkuBarcode` requires a new persisted table. These commands are recommendations for the developer to run manually after implementation; Codex must not run them automatically.

```powershell
dotnet ef migrations add AddSkuBarcodes --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext --output-dir Infrastructure\Persistence\Migrations
dotnet ef database update --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext
```

Expected migration artifacts:

- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddSkuBarcodes.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddSkuBarcodes.Designer.cs`
- Updated `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define `SkuBarcode` identity, SKU relationship, value uniqueness, symbology, primary selection, lifecycle transitions, and domain events.
- **Modular Monolith Boundaries**: PASS. Contracts and data model keep runtime work inside existing WMS module, Catalog API/client surface, and test project boundaries.
- **Vertical Slice Delivery**: PASS. Design covers domain, handlers, persistence, endpoints, API/client support where applicable, and tests for each user-facing operation. UI is explicitly out of scope.
- **Testing Discipline**: PASS with Principle IV exception below. Data model and quickstart identify focused domain, handler, persistence, API/client, build, regression, and manual API validation.
- **Simplicity and Observability**: PASS. Research rejects broader generic barcode abstractions and future operational features; contracts require clear validation, duplicate-value, missing-SKU, not-found, unsupported-primary-change, and persistence failure behavior through existing conventions only.

No architecture complexity exceptions are requested.

## Complexity Tracking

No architecture complexity exceptions are requested.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| SKU barcode HTTP endpoint integration automation | Existing repeated reference-data planning allows deferring broad endpoint test-host infrastructure when lower-level coverage protects behavior. SKU barcode endpoints follow existing Catalog endpoint patterns, while barcode-specific rules are covered below the endpoint layer. | Domain tests, handler tests, persistence mapping/unique-index/case-sensitivity tests, API/client route/DTO/result tests if client support is included, and full regression test run. | Manual API checks in `quickstart.md`: create, duplicate value, case-sensitive values, SKU filter, get, update primary, deactivate primary, include inactive, reactivate non-primary. | No. A future cross-cutting test-infrastructure issue may be opened if the project adopts endpoint automation by default. |
| SKU barcode UI/component automation | UI implementation is explicitly out of scope for issue #42. | Not applicable beyond domain, handler, persistence, endpoint, and API/client tests. | No UI validation for this phase; quickstart verifies no UI pages or navigation were added. | No. A separate UI issue should define UI behavior and any UI test strategy. |
