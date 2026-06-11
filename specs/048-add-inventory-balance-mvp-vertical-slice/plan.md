# Implementation Plan: Inventory Balance MVP Vertical Slice

**Branch**: `048-add-inventory-balance-mvp-vertical-slice` | **Date**: 2026-06-11 | **Spec**: `specs/048-add-inventory-balance-mvp-vertical-slice/spec.md`

**Input**: Feature specification from `specs/048-add-inventory-balance-mvp-vertical-slice/spec.md`, `StakeholderDocs/Wms/Inventory/048 Add Inventory Balance MVP vertical slice.md`, `.specify/memory/constitution.md`, durable Myrmex workflow guidance, and existing WMS Catalog, Topology, persistence, API, and client patterns.

## Summary

Introduce the first WMS Inventory capability by adding an `InventoryBalance` aggregate that represents the current known non-negative quantity of one active SKU at one eligible storage location. The slice includes create, get by id, list with SKU/location/warehouse filters, and quantity-only update behavior. Quantity is always interpreted in the SKU base unit of measure; the balance stores SKU and storage location identities only and derives warehouse and base UoM display context through existing Catalog and Topology records.

This plan adds a new Inventory area inside the existing WMS module rather than creating a new service or broad inventory ledger. It explicitly excludes receiving, putaway, picking, shipping, LPN, reservations, movements, transactions, adjustments, UoM conversion, delete behavior, activation/deactivation, seed/demo data, external integrations, and WebApp UI.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET 10 solution.

**Primary Dependencies**: Existing ASP.NET Core Minimal APIs, EF Core 10, xUnit, `Myrmex.Core`, `Myrmex.AppDispatching`, `Myrmex.AspNetCore`, `Myrmex.Modules.Wms`, `Myrmex.WebApp`, existing WMS Catalog and Topology slices, and existing service-result/API-client primitives.

**Storage**: Existing `WmsDbContext` using SQL Server in production and SQLite-backed test context. Add `wms.inventory_balances` with required SKU and storage location foreign keys, non-negative decimal quantity, timestamps, and a unique key on `(StockKeepingUnitId, StorageLocationId)`. EF migration generation and database update are developer-controlled.

**Testing**: Existing xUnit test project. Add focused tests for the Inventory Balance domain rules, create/get/list/update handlers, validation of active SKU/base UoM and eligible storage location/type/status, uniqueness, zero quantity, persistence mapping/FKs/unique index, API/client request and response contracts, and regression protection for referenced Catalog and Topology behavior. Do not add broad endpoint or UI automation infrastructure.

**Target Platform**: Existing Myrmex API service and Blazor WebApp in the modular-monolith solution. WebApp UI is out of scope; a WebApp API client contract may be added only for typed client compatibility and future UI integration.

**Project Type**: Brownfield modular-monolith web application with WMS module vertical slices.

**Performance Goals**: A user can create a valid inventory balance in under 2 minutes, retrieve one balance by id in one result, answer all four MVP lookup questions by SKU/location/warehouse/SKU-within-warehouse, and update an existing quantity to any non-negative decimal value.

**Constraints**: Keep the slice limited to current inventory balance state; use existing explicit commands/queries and dispatchers; use existing service result, ProblemDetails, `ApiResult<T>`, API exception, diagnostics, and persistence conventions; do not duplicate warehouse or base UoM as mutable balance state; no new service split; no new framework; no inventory ledger or movement abstraction; no WebApp UI; no build, test, app startup, database update, EF migration generation, EF migration application, or infrastructure commands run automatically.

**Scale/Scope**: One WMS Inventory vertical slice touching a new `Inventory` area under `Myrmex.Modules.Wms`, shared WMS persistence registration, WMS module endpoint registration, optional WebApp WMS Inventory API client contracts, focused tests, and developer-controlled migration generation/application.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names Inventory Balance, SKU, storage location, warehouse, base UoM, quantity, SKU/location uniqueness, create/get/list/update commands and queries, validation rules, and the excluded inventory movement concepts before implementation details.
- **Modular Monolith Boundaries**: PASS. Runtime work stays inside the existing WMS module and uses explicit commands, queries, endpoint registration, EF configuration, and API/client contracts. Cross-capability dependencies read existing Catalog and Topology records through the shared WMS persistence boundary.
- **Vertical Slice Delivery**: PASS. The slice covers domain model, request/response contracts, handlers, persistence mapping and migration, endpoint registration, API/client integration where applicable, and focused tests for create, retrieve, list, and quantity update behavior. WebApp UI is explicitly not applicable in this phase.
- **Testing Discipline**: PASS with documented Principle IV endpoint/UI automation exception below. Focused domain, handler, persistence, API/client, regression, and manual API validation are identified before task generation.
- **Simplicity and Observability**: PASS. The design reuses current WMS patterns, avoids movement ledger and conversion abstractions, and keeps operational diagnostics to existing validation, not-found, duplicate, persistence, ProblemDetails, service-result, `ApiResult<T>`, and API exception behavior.

## Project Structure

### Documentation (this feature)

```text
specs/048-add-inventory-balance-mvp-vertical-slice/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── inventory-balance-api-contract.md
├── checklists/
│   └── requirements.md
└── spec.md
```

### Source Code (repository root)

```text
Myrmex.Modules.Wms/
├── Inventory/
│   ├── Domain/InventoryBalances/
│   │   ├── InventoryBalance.cs
│   │   ├── InventoryBalanceEvents.cs
│   │   └── InventoryBalanceValidationErrors.cs
│   ├── Endpoints/
│   │   ├── InventoryEndpoints.cs
│   │   └── InventoryBalanceEndpoints.cs
│   └── Features/InventoryBalances/
│       ├── CreateInventoryBalance.cs
│       ├── GetInventoryBalanceById.cs
│       ├── InventoryBalanceDetails.cs
│       ├── ListInventoryBalances.cs
│       └── UpdateInventoryBalanceQuantity.cs
├── Infrastructure/Persistence/
│   ├── Configurations/InventoryBalanceConfiguration.cs
│   ├── Migrations/YYYYMMDDHHMMSS_AddInventoryBalance.cs
│   ├── WmsDatabaseNames.cs
│   └── WmsDbContext.cs
├── WmsErrors.cs
└── WmsModule.cs

Myrmex.WebApp/
└── Wms/Inventory/
    └── WmsInventoryApiClient.cs

Myrmex.Tests/
└── Wms/Inventory/
    ├── Client/WmsInventoryApiClientTests.cs
    ├── Domain/InventoryBalanceTests.cs
    ├── Features/InventoryBalances/
    │   ├── CreateInventoryBalanceHandlerTests.cs
    │   ├── GetInventoryBalanceByIdHandlerTests.cs
    │   ├── ListInventoryBalancesHandlerTests.cs
    │   └── UpdateInventoryBalanceQuantityHandlerTests.cs
    └── Persistence/InventoryBalancePersistenceTests.cs
```

**Structure Decision**: Add a new `Inventory` area under the existing WMS module and keep Catalog/Topology as referenced capabilities. Register Inventory endpoints through `WmsModule.MapWmsModule()` alongside existing Catalog and Topology endpoint groups. Do not create a separate service, a generic inventory ledger module, a WebApp UI page, or new reference-data areas.

## Phase 0: Research Output

Create `research.md` with decisions for:

- Creating a new WMS Inventory area for `InventoryBalance`.
- Modeling `InventoryBalance` as current state, not reference data or a lifecycle entity.
- Storing SKU/location/quantity/timestamps while deriving warehouse and base UoM context.
- Validating active SKU with base UoM and eligible storage location/type/status at create time.
- Enforcing SKU/location uniqueness in domain/application behavior and persistence.
- Supporting zero quantity and retaining zero balances in list results.
- Using a quantity-only update contract.
- Providing get/list response context through joins/projections without persisting duplicate warehouse or base UoM state.
- Reusing existing service-result/ProblemDetails diagnostics and adding no new observability framework.
- Developer-controlled EF migration workflow.
- Focused automated test scope and endpoint/UI automation deferral.
- Explicit non-goals and rejected broader alternatives.

## Phase 1: Design Outputs

Create `data-model.md` for `InventoryBalance`, referenced `StockKeepingUnit`, `StorageLocation`, `Warehouse`, `UnitOfMeasure`, create/update commands, list/get queries, details projection, persistence shape, validation rules, state transitions, and out-of-scope data. The model must state that `InventoryBalance` has no activation lifecycle, does not store warehouse or unit of measure as independent business state, and supports only quantity changes after creation.

Create `contracts/inventory-balance-api-contract.md` for the new Inventory API route group, create/get/list/update payloads, filters, failure behavior, and optional WebApp API client contract. The contract must not define delete, deactivate/reactivate, movement, reservation, adjustment, UoM conversion, external integration, or WebApp UI behavior.

Create `quickstart.md` as a validation guide for implementation review. It must include artifact checks, scope-boundary checks, recommended developer-controlled build/test/migration commands, manual API validation scenarios, and no-UI/no-ledger/no-conversion checks. It must not include implementation code or instruct Codex to run build/test/startup/database/migration commands automatically.

Update `AGENTS.md` between the Spec Kit markers so active issue #48 work points agents to this plan in addition to durable `.specify/memory/` guidance.

## Task Generation Guard

Any issue #48 `tasks.md` must be a small Inventory Balance vertical-slice task list and must include test tasks before implementation tasks where Principle IV applies. Tasks may touch only the Inventory Balance implementation areas listed in this plan plus supporting WMS registration, persistence, errors, API client, and tests.

Tasks must not include:

- Receiving, putaway, picking, shipping, LPN, reservations, transaction history, movement history, adjustment documents, or inventory ledger behavior.
- Batch/lot, expiry, serial number, packaging, cycle counting, unit conversion, alternative UoM, or availability allocation behavior.
- Delete, deactivate, reactivate, or cleanup behavior for inventory balances.
- Seed or demo data.
- External integrations.
- New Blazor pages, navigation, dialogs, grids, forms, or UI component tests.
- New endpoint/UI test frameworks.
- New logging, telemetry, observability, or diagnostics infrastructure.
- MediatR or new architectural frameworks.
- Broad refactoring of Catalog, Topology, WebApp API support, or WMS persistence beyond what is required for the Inventory Balance slice.

## Developer-Controlled Migration Commands

Migration work is expected because the feature adds a persisted `inventory_balances` table. These commands are recommendations for the developer to run manually after implementation; Codex must not run them automatically.

```powershell
dotnet ef migrations add AddInventoryBalance --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext --output-dir Infrastructure\Persistence\Migrations
dotnet ef database update --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext
```

Expected migration artifacts:

- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddInventoryBalance.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddInventoryBalance.Designer.cs`
- Updated `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define the Inventory Balance aggregate, quantity invariant, SKU/location identity, derived warehouse/base UoM context, SKU/location uniqueness, create/get/list/update behavior, and excluded movement/lifecycle semantics.
- **Modular Monolith Boundaries**: PASS. Contracts and data model keep runtime work inside the WMS module and use existing Catalog/Topology records as dependencies without moving ownership or introducing service boundaries.
- **Vertical Slice Delivery**: PASS. Design covers domain, handlers, persistence, endpoints, API/client support, and focused tests for create, retrieve, list, and quantity update behavior. WebApp UI screens are explicitly out of scope.
- **Testing Discipline**: PASS with Principle IV exception below. Data model and quickstart identify focused domain, handler, persistence, API/client, build, regression, and manual API validation.
- **Simplicity and Observability**: PASS. Research rejects movement, ledger, conversion, delete/lifecycle, seed/demo, integration, and UI expansion; contracts require clear errors through existing conventions only.

No architecture complexity exceptions are requested.

## Complexity Tracking

No architecture complexity exceptions are requested.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| Inventory Balance HTTP endpoint integration automation | Existing WMS planning allows deferring broad endpoint test-host infrastructure when lower-level coverage protects behavior. This feature adds new routes, but the behavior is mediated by explicit handlers and existing result-to-HTTP conventions. | Domain tests, create/get/list/update handler tests, persistence mapping tests, API/client request/response/result tests, and full regression test run. | Manual API checks in `quickstart.md`: create valid balance, reject invalid references, reject duplicate pair, get by id, list filters, update quantity, zero quantity, and not-found behavior. | No. A future cross-cutting test-infrastructure issue may be opened if the project adopts endpoint automation by default. |
| Inventory Balance UI/component automation | WebApp UI is explicitly out of scope. The plan may add only a typed WebApp API client for future UI integration and compile-time contract coverage. | Domain, handler, persistence, endpoint contract, and API client tests protect business behavior; manual checks verify no UI surface was added. | Quickstart verifies no new Inventory UI page/navigation/grid/dialog/form was added. | No. A separate WebApp UI issue should define any future screen and UI test strategy. |
