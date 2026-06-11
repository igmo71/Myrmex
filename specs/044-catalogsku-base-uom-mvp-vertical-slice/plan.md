# Implementation Plan: Catalog/SKU Base UoM MVP Vertical Slice

**Branch**: `044-catalogsku-base-uom-mvp-vertical-slice` | **Date**: 2026-06-10 | **Spec**: `specs/044-catalogsku-base-uom-mvp-vertical-slice/spec.md`

**Input**: Feature specification from `specs/044-catalogsku-base-uom-mvp-vertical-slice/spec.md`, GitHub issue #44, `StakeholderDocs/Wms/Catalog/044 Catalog-SKU Base UoM MVP vertical slice.md`, `.specify/memory/constitution.md`, durable Myrmex architecture/testing/API guidance, and existing Catalog/SKU, Catalog/UoM, and Catalog/SKU Barcode implementation and planning artifacts.

## Summary

Implement a narrow WMS Catalog/SKU Base UoM vertical-slice increment by adding a required `BaseUnitOfMeasureId` assignment to the existing `StockKeepingUnit` aggregate and SKU create/update/get/list contracts. A SKU must reference exactly one existing active `UnitOfMeasure` when it is created or updated, and returned SKU details must include the assigned base UoM identity.

This plan modifies the existing Catalog/SKU slice rather than adding a separate Base UoM feature area. It must preserve SKU code/name/description, lifecycle, duplicate-code, search/list, UoM, and SKU Barcode behavior while adding the required SKU-to-UoM relationship. It must not introduce alternative UoMs, conversion factors, packaging, inventory, receiving, LPN, picking/shipping, seed/demo data, new UI screens, or external integration behavior.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET 10 solution.

**Primary Dependencies**: Existing ASP.NET Core Minimal APIs, EF Core, xUnit, `Myrmex.Core`, `Myrmex.AppDispatching`, `Myrmex.AspNetCore`, `Myrmex.Modules.Wms`, `Myrmex.WebApp`, and existing Catalog API/client primitives.

**Storage**: Existing WMS EF Core context using SQL Server in production and SQLite-backed test context. Modify `wms.stock_keeping_units` to add a required `BaseUnitOfMeasureId` column and required relationship to `wms.units_of_measure.Id`. Existing development data does not require production-safe preservation. EF migration generation and database update are developer-controlled.

**Testing**: Existing xUnit test project. Add focused tests for new SKU Base UoM behavior: domain required identity, create/update handler validation for missing, nonexistent, and inactive UoMs, returned details/list projection, persistence relationship/mapping, API/client payload changes, and regression protection for existing SKU, UoM, and SKU Barcode behavior. Do not add endpoint or UI automation infrastructure.

**Target Platform**: Existing Myrmex API service and Blazor web application in the modular-monolith solution. UI screens are out of scope, but the existing WebApp Catalog API client and SKU page models may need contract-compatible updates if required for compile-time consistency.

**Project Type**: Brownfield modular-monolith web application with WMS module vertical slices.

**Performance Goals**: Users can create a valid SKU with an active base UoM and see the assignment in under 1 minute; users can change a SKU's base UoM and verify it through direct retrieval and list results in under 1 minute.

**Constraints**: Keep the slice limited to required SKU Base UoM binding; preserve existing Catalog/SKU, Catalog/UoM, and Catalog/SKU Barcode behavior; use explicit commands and queries through existing internal dispatchers; use existing service result, ProblemDetails, `ApiResult<T>`, and API exception conventions; no new frameworks; no MediatR; no new reference-data table; no conversion model; no UI implementation; no seed/demo data; no build, test, app startup, database update, EF migration generation, or EF migration application commands run automatically.

**Scale/Scope**: One WMS Catalog/SKU increment touching the existing `StockKeepingUnit` aggregate, create/update commands, get/list projections, SKU endpoints, WebApp Catalog API client contracts, persistence configuration, focused tests, and developer-controlled migration generation/application.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names Catalog, SKU, Unit of Measure, `StockKeepingUnit`, `UnitOfMeasure`, required base UoM identity, active-assignment invariant, SKU create/update commands, SKU get/list queries, details contracts, and existing domain event behavior before implementation details.
- **Modular Monolith Boundaries**: PASS. Runtime work stays inside the existing WMS module, Catalog API/client surface, WebApp contract models where needed, and test project boundaries. Cross-boundary behavior uses existing minimal endpoints, typed Catalog API client primitives, and command/query dispatching.
- **Vertical Slice Delivery**: PASS. The slice covers domain model, handlers, persistence mapping and migration, endpoints, request/response contracts, API/client integration, and focused tests. New UI pages are explicitly not applicable in this phase.
- **Testing Discipline**: PASS with documented Principle IV endpoint/UI automation exception below. Focused domain, handler, persistence, API/client, regression, and manual API/client validation are identified before tasks are generated.
- **Simplicity and Observability**: PASS. The design reuses existing Catalog/SKU and Catalog/UoM patterns, avoids conversion and packaging abstractions, and keeps diagnostics to existing validation, missing-UoM, inactive-UoM, missing-SKU, duplicate-code, persistence, ProblemDetails, service-result, `ApiResult<T>`, and API exception behavior.

## Project Structure

### Documentation (this feature)

```text
specs/044-catalogsku-base-uom-mvp-vertical-slice/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── catalog-sku-base-uom-api-contract.md
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
│   │   └── StockKeepingUnitEndpoints.cs
│   └── Features/StockKeepingUnits/
│       ├── CreateStockKeepingUnit.cs
│       ├── GetStockKeepingUnitById.cs
│       ├── ListStockKeepingUnits.cs
│       ├── StockKeepingUnitDetails.cs
│       └── UpdateStockKeepingUnitDetails.cs
├── Infrastructure/Persistence/
│   ├── Configurations/StockKeepingUnitConfiguration.cs
│   ├── Migrations/YYYYMMDDHHMMSS_AddStockKeepingUnitBaseUnitOfMeasure.cs
│   ├── WmsDatabaseNames.cs
│   └── WmsDbContext.cs
└── WmsErrors.cs

Myrmex.WebApp/
└── Wms/Catalog/
    └── WmsCatalogApiClient.cs

Myrmex.Tests/
└── Wms/Catalog/
    ├── Client/WmsCatalogApiClientTests.cs
    ├── Domain/StockKeepingUnitTests.cs
    ├── Features/StockKeepingUnits/
    │   ├── CreateStockKeepingUnitHandlerTests.cs
    │   ├── ListStockKeepingUnitsHandlerTests.cs
    │   ├── GetStockKeepingUnitByIdHandlerTests.cs
    │   └── UpdateStockKeepingUnitDetailsHandlerTests.cs
    └── Persistence/StockKeepingUnitPersistenceTests.cs
```

**Structure Decision**: Extend the existing `StockKeepingUnit` Catalog aggregate and SKU vertical slice in place. Add a required relationship from SKU to the existing `UnitOfMeasure` aggregate. Do not create a Base UoM aggregate, Base UoM endpoints, a conversion module, or a new UI page. Keep any WebApp work limited to request/response/client model compatibility needed by the existing Catalog/SKU client and page flow.

## Phase 0: Research Output

Create `research.md` with decisions for:

- Extending the existing `StockKeepingUnit` aggregate instead of adding a new feature area.
- Required `BaseUnitOfMeasureId` identity on SKU.
- Assignment validation against existing active `UnitOfMeasure`.
- Existing UoM deactivation behavior after assignment.
- SKU create/update command contract changes.
- SKU get/list details projection changes.
- API route compatibility and service-result/ProblemDetails behavior.
- WebApp Catalog API client contract updates without new UI screens.
- EF Core required relationship, column, index, and migration.
- Focused test scope for the new binding.
- Endpoint/UI automated test deferral and manual validation scope.
- Explicit non-goals and rejected broader alternatives.

## Phase 1: Design Outputs

Create `data-model.md` for the updated `StockKeepingUnit` aggregate, required `BaseUnitOfMeasureId`, command/query inputs, details projection, persistence relationship, validation rules, lifecycle interactions, and out-of-scope data. The model must state that the base UoM is an identity reference only in returned SKU contracts for this MVP; no UoM conversion, alternative unit, packaging, inventory, or operational quantity behavior is introduced.

Create `contracts/catalog-sku-base-uom-api-contract.md` for the updated SKU create/update/get/list payloads, failure behavior for missing/nonexistent/inactive base UoM, and WebApp API client request/response model changes. The contract must not define new routes beyond the existing SKU routes and must not define UI pages, navigation, grids, dialogs, or conversion behavior.

Create `quickstart.md` as a validation guide for implementation review. It must include commands to inspect generated artifacts, recommended developer-controlled build/test/migration commands, manual API validation scenarios, and checks that no UI, conversion, packaging, inventory, or seed/demo behavior was added. It must not include implementation code or instruct Codex to run build/test/startup/database/migration commands automatically.

Update `AGENTS.md` between the Spec Kit markers so active issue #44 work points agents to this plan in addition to durable `.specify/memory/myrmex-*.md` guidance.

## Task Generation Guard

Any issue #44 `tasks.md` must be a small Catalog/SKU Base UoM vertical-slice task list and must include test tasks before implementation tasks where Principle IV applies. Tasks may touch only the Catalog/SKU Base UoM implementation areas listed in this plan and supporting WMS registration/persistence files.

Tasks must not include:

- Alternative UoMs.
- UoM conversion factors.
- Packaging.
- Inventory.
- Receiving.
- LPN behavior.
- Picking or shipping behavior.
- Seed or demo data.
- New Blazor pages, navigation, dialogs, grids, forms, or UI component tests.
- New endpoint/UI test frameworks.
- New logging, telemetry, observability, or diagnostics infrastructure.
- MediatR or new architectural frameworks.
- Broad refactoring.
- Moving or rewriting existing WMS Topology API client support types.
- Reworking existing Catalog/SKU, Catalog/UoM, or Catalog/SKU Barcode behavior except where shared SKU contracts must compile with the new required base UoM.

## Developer-Controlled Migration Commands

Migration work is expected because `StockKeepingUnit` needs a required `BaseUnitOfMeasureId` relationship. These commands are recommendations for the developer to run manually after implementation; Codex must not run them automatically.

```powershell
dotnet ef migrations add AddStockKeepingUnitBaseUnitOfMeasure --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext --output-dir Infrastructure\Persistence\Migrations
dotnet ef database update --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext
```

Expected migration artifacts:

- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddStockKeepingUnitBaseUnitOfMeasure.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddStockKeepingUnitBaseUnitOfMeasure.Designer.cs`
- Updated `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define the SKU base UoM invariant, assignment validation, existing UoM relationship, command/query contract changes, returned details, lifecycle interaction, and unchanged domain event boundaries.
- **Modular Monolith Boundaries**: PASS. Contracts and data model keep runtime work inside existing WMS Catalog module, WebApp Catalog API client contract surface, and test project boundaries.
- **Vertical Slice Delivery**: PASS. Design covers domain, handlers, persistence, endpoints, API/client support, and focused tests for create, update, retrieve, and list behavior. New UI screens are explicitly out of scope.
- **Testing Discipline**: PASS with Principle IV exception below. Data model and quickstart identify focused domain, handler, persistence, API/client, build, regression, and manual API validation.
- **Simplicity and Observability**: PASS. Research rejects conversion, alternative-unit, packaging, inventory, seed/demo, and UI expansion; contracts require clear missing/invalid base UoM behavior through existing conventions only.

No architecture complexity exceptions are requested.

## Complexity Tracking

No architecture complexity exceptions are requested.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| SKU Base UoM HTTP endpoint integration automation | Existing repeated Catalog planning allows deferring broad endpoint test-host infrastructure when lower-level coverage protects behavior. SKU routes are existing routes with expanded payloads and validation. | Domain tests, create/update/get/list handler tests, persistence relationship tests, API/client request/response/result tests, and full regression test run. | Manual API checks in `quickstart.md`: create with active base UoM, missing base UoM rejection, missing UoM rejection, inactive UoM rejection, get/list projection, update base UoM, and regression checks. | No. A future cross-cutting test-infrastructure issue may be opened if the project adopts endpoint automation by default. |
| SKU Base UoM UI/component automation | New UI screens are out of scope. Existing SKU UI may need compile-time model updates only if the required request contract demands it, but no new UI workflow is specified by issue #44. | Domain, handler, persistence, endpoint contract, and API client tests protect business behavior; manual checks cover no new UI surface and any existing SKU page compile/runtime compatibility. | Quickstart verifies no new UI page/navigation was added and suggests a developer-controlled manual smoke only if existing SKU UI is updated for request compatibility. | No. A separate UI issue should define any future Base UoM selection experience and UI test strategy. |
