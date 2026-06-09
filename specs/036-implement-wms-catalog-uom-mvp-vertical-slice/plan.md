# Implementation Plan: WMS Catalog/UoM MVP Vertical Slice

**Branch**: `036-implement-wms-catalog-uom-mvp-vertical-slice` | **Date**: 2026-06-09 | **Spec**: `specs/036-implement-wms-catalog-uom-mvp-vertical-slice/spec.md`

**Input**: Feature specification from `specs/036-implement-wms-catalog-uom-mvp-vertical-slice/spec.md`, GitHub issue #36, `.specify/memory/constitution.md`, durable `.specify/memory/myrmex-*.md` guidance, and the existing Catalog/SKU implementation and planning artifacts.

## Summary

Implement a narrow WMS Catalog Unit of Measure reference-data vertical slice after the established Catalog/SKU slice. Add `UnitOfMeasure` as a Catalog aggregate with create, list, get, update, deactivate, and reactivate behavior; persistence mapping and migration; Catalog endpoints; `WmsCatalogApiClient` support; a minimal `/wms/catalog/uoms` UI; and focused repeated-reference-data tests.

The implementation must follow the accepted SKU reference-data conventions where applicable while deliberately avoiding conversions, base/alternative UoM modeling, SKU-to-UoM binding, packaging, barcode, inventory, receiving, LPN, picking/shipping, integration, provider-specific query branching, `AsEnumerable()` sorting workarounds, new endpoint/UI test frameworks, and new logging/telemetry/observability infrastructure.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET solution.

**Primary Dependencies**: Existing ASP.NET Core Minimal APIs, EF Core, Blazor, MudBlazor, xUnit, `Myrmex.Core`, `Myrmex.AppDispatching`, `Myrmex.AspNetCore`, and `Myrmex.Modules.Wms`.

**Storage**: Existing WMS EF Core context using SQL Server in production and SQLite-backed test context. Add a WMS `units_of_measure` table under the existing `wms` schema. Store normalized UoM code directly in `Code`; do not add `NormalizedCode`.

**Testing**: Existing xUnit test project. Apply issue #34 repeated reference-data guidance with focused UoM tests: domain tests for UoM-specific validation/lifecycle, targeted handler tests where UoM behavior needs entity-specific confidence, persistence tests for mapping/table/required fields/unique `Code`, API client route/DTO/result wiring tests only where not already covered by the representative Catalog client pattern, and manual UI smoke validation.

**Target Platform**: Existing Myrmex API service and Blazor web application in the modular-monolith solution.

**Project Type**: Brownfield modular-monolith web application with WMS module vertical slices.

**Performance Goals**: Users can create a UoM in under 1 minute and find a known UoM by code or name from at least 25 records in under 30 seconds.

**Constraints**: Keep the slice small; preserve WMS Topology and Catalog/SKU behavior; use explicit commands and queries through existing internal dispatchers; use existing service result, ProblemDetails, `ApiResult<T>`, and API exception conventions; keep OE-006 diagnostics inside existing error/result behavior; support only provider-safe sorting fields `code`, `name`, and `isActive` unless the current SKU implementation already proves another provider-safe field; do not add created/updated timestamp sorting; do not add provider-specific query branching; do not use `AsEnumerable()` for sorting; no MediatR; no new frameworks; no new logging, telemetry, observability, endpoint-test, or UI-test infrastructure.

**Scale/Scope**: One repeated WMS Catalog/UoM reference-data slice covering one aggregate, six user-facing operations, one database table, one Catalog web page with dialog-based create/edit, and focused regression tests.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names Catalog, Unit of Measure, `UnitOfMeasure`, lifecycle states, code uniqueness, commands, queries, details contracts, and domain events before implementation details.
- **Modular Monolith Boundaries**: PASS. Runtime work stays within existing WMS module, web app, and test project boundaries. Cross-boundary behavior uses existing minimal endpoints, typed web API clients, and command/query dispatching.
- **Vertical Slice Delivery**: PASS. The slice includes domain model, handlers, persistence mapping and migration, endpoints, request/response contracts, web API client, UI, and tests for UoM reference data.
- **Testing Discipline**: PASS with documented Principle IV endpoint/UI automation exception below. Focused domain, handler, persistence, API client, regression, and manual UI smoke validation are identified before tasks are generated.
- **Simplicity and Observability**: PASS. The design reuses Catalog/SKU patterns, adds no framework or service split, and keeps diagnostics to existing validation, duplicate-code, not-found, unsupported-sort fallback, persistence, ProblemDetails, service-result, `ApiResult<T>`, and API exception behavior.

## Project Structure

### Documentation (this feature)

```text
specs/036-implement-wms-catalog-uom-mvp-vertical-slice/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── catalog-uom-api-and-ui-contract.md
├── checklists/
│   └── requirements.md
└── spec.md
```

### Source Code (repository root)

```text
Myrmex.Modules.Wms/
├── Catalog/
│   ├── Domain/UnitsOfMeasure/
│   │   ├── UnitOfMeasure.cs
│   │   └── UnitOfMeasureEvents.cs
│   ├── Endpoints/
│   │   ├── CatalogEndpoints.cs
│   │   └── UnitOfMeasureEndpoints.cs
│   └── Features/UnitsOfMeasure/
│       ├── CreateUnitOfMeasure.cs
│       ├── DeactivateUnitOfMeasure.cs
│       ├── GetUnitOfMeasureById.cs
│       ├── ListUnitsOfMeasure.cs
│       ├── ReactivateUnitOfMeasure.cs
│       ├── UnitOfMeasureDetails.cs
│       └── UpdateUnitOfMeasureDetails.cs
├── Infrastructure/Persistence/
│   ├── Configurations/UnitOfMeasureConfiguration.cs
│   ├── Migrations/YYYYMMDDHHMMSS_AddUnitsOfMeasure.cs
│   ├── WmsDatabaseNames.cs
│   └── WmsDbContext.cs
├── WmsErrors.cs
└── WmsModule.cs

Myrmex.WebApp/
├── Wms/Catalog/
│   └── WmsCatalogApiClient.cs
├── Components/Layout/
│   └── NavMenu.razor
└── Components/Pages/Wms/Catalog/UomPages/
    ├── Index.razor
    ├── Index.razor.cs
    ├── UomEditDialog.razor
    ├── UomFilters.razor
    └── UomGrid.razor

Myrmex.Tests/
└── Wms/Catalog/
    ├── Client/WmsCatalogApiClientTests.cs
    ├── Domain/UnitOfMeasureTests.cs
    ├── Features/UnitsOfMeasure/
    │   ├── CreateUnitOfMeasureHandlerTests.cs
    │   ├── ListUnitsOfMeasureHandlerTests.cs
    │   ├── UpdateUnitOfMeasureDetailsHandlerTests.cs
    │   └── UnitOfMeasureLifecycleHandlerTests.cs
    └── Persistence/UnitOfMeasurePersistenceTests.cs
```

**Structure Decision**: Extend the existing `Catalog` WMS capability that already owns SKU. Keep UoM under `Myrmex.Modules.Wms/Catalog` as a sibling reference-data slice to `StockKeepingUnits`, extend the existing Catalog endpoint group and `WmsCatalogApiClient`, and add a separate `UomPages` UI folder. Do not move or rewrite existing SKU or Topology code except for shared registration points needed to add UoM.

## Phase 0: Research Output

Create `research.md` with decisions for:

- UoM as a repeated Catalog reference-data slice.
- `UnitOfMeasure` aggregate name and user-facing UoM wording.
- Minimal UoM data shape: code, name, optional symbol, active state, timestamps.
- Normalized `Code` storage without `NormalizedCode`.
- `UpdatedAtUtc` lifecycle behavior.
- Existing `AggregateRoot`/`EntityBase` usage.
- Domain events only for real changes.
- Command/query handler set.
- EF Core table, unique index, required fields, and migration.
- API route and service-result/ProblemDetails behavior.
- Extension of the existing `WmsCatalogApiClient`.
- Minimal MudBlazor UoM page composition.
- Provider-safe sorting limited to `code`, `name`, and `isActive`.
- Focused repeated-slice testing under issue #34.
- Endpoint/UI automated test deferral and manual smoke scope.
- Explicit non-goals and rejected broader alternatives.

## Phase 1: Design Outputs

Create `data-model.md` for the `UnitOfMeasure` aggregate, domain events, command/query inputs, details projection, persistence shape, lifecycle transitions, validation rules, sorting rules, and out-of-scope relationships. The model must explicitly state that normalized UoM code is stored directly in `Code`, `UpdatedAtUtc` is null on create, and no conversion/base-unit/SKU binding model is introduced.

Create `contracts/catalog-uom-api-and-ui-contract.md` for the minimal API surface, payloads, list query parameters, error behavior, web API client methods, and web UI page/dialog expectations.

Create `quickstart.md` as a validation guide for implementation review. It must include commands to inspect generated artifacts, run build/test validation, verify migration/persistence shape, and manually smoke test the UoM API/UI without including implementation code.

Update `AGENTS.md` between the Spec Kit markers so active issue #36 work points agents to this plan in addition to durable `.specify/memory/myrmex-*.md` guidance.

## Task Generation Guard

Any issue #36 `tasks.md` must be a small repeated reference-data vertical-slice task list and must include test tasks before implementation tasks where Principle IV applies. Tasks may touch only the Catalog/UoM implementation areas listed in this plan and supporting WMS registration/persistence files.

Tasks must not include:

- UoM conversion rules.
- Base or alternative UoM model.
- SKU-to-UoM binding.
- Packaging levels.
- Barcode support.
- Inventory quantities.
- Receiving flows.
- LPN behavior.
- Picking or shipping behavior.
- External integration behavior.
- Created/updated timestamp sorting for UoM lists.
- Provider-specific query branching.
- `AsEnumerable()` sorting workarounds.
- New endpoint/UI test frameworks.
- New logging, telemetry, observability, or diagnostics infrastructure.
- MediatR or new architectural frameworks.
- Broad refactoring.
- Moving or rewriting existing WMS Topology API client support types.
- Reworking existing Catalog/SKU behavior except where shared Catalog registration must compile.

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define `UnitOfMeasure` identity, code uniqueness, validation, lifecycle transitions, and domain events.
- **Modular Monolith Boundaries**: PASS. Contracts and data model keep work inside existing WMS module, web app, and test project boundaries.
- **Vertical Slice Delivery**: PASS. Design covers domain, handlers, persistence, endpoints, client, UI, and tests for each user-facing operation.
- **Testing Discipline**: PASS with Principle IV exception below. Data model and quickstart identify focused domain, handler, persistence, API client, build, regression, and manual UI validation.
- **Simplicity and Observability**: PASS. Research rejects broader abstractions and future roadmap features; contracts require clear validation, duplicate-code, not-found, unsupported-sort fallback, and persistence failure behavior through existing conventions only.

No constitution violations.

## Complexity Tracking

No architecture complexity exceptions are requested.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| UoM HTTP endpoint integration automation | Existing coverage pattern for this repeated reference-data slice does not require adding new broad endpoint test-host infrastructure. UoM endpoints mirror the representative Catalog/SKU endpoint pattern. | Domain tests, focused handler tests, persistence mapping/unique-index tests, API client route/DTO/result wiring tests, and full regression build/test run. | Manual API behavior checks in `quickstart.md`: create, duplicate create, list/search/sort, get, update, deactivate, include inactive, reactivate. | No. A future cross-cutting test-infrastructure issue may be opened if the project chooses endpoint automation by default. |
| UoM UI/component automation | Existing project guidance permits manual smoke checks for simple repeated CRUD pages when adding UI automation would require new UI/component test infrastructure. UoM UI mirrors the existing SKU page pattern. | Domain, handler, persistence, and API client tests protect business behavior; manual smoke validates UI wiring and user-visible feedback. | Manual browser smoke in `quickstart.md`: navigation, list, search, include inactive, create/edit dialog, deactivate/reactivate, snackbar/reload behavior, and absence of out-of-scope controls. | No. A future cross-cutting UI automation issue may be opened if the project adopts UI/component test infrastructure. |
