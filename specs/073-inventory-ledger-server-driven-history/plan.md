# Implementation Plan: Inventory Ledger Server-Driven History

**Branch**: `073-inventory-ledger-server-driven-history` | **Date**: 2026-06-19 | **Spec**: `specs/073-inventory-ledger-server-driven-history/spec.md`

**Input**: Feature specification from `specs/073-inventory-ledger-server-driven-history/spec.md`, stakeholder document `StakeholderDocs/Wms/Inventory/073 Inventory Ledger server-driven history.md`, Myrmex Constitution, durable architecture/testing guidance, existing Inventory Balance server-driven list pattern, and implemented Inventory Adjustment Ledger model.

## Summary

Expose the existing immutable Inventory Adjustment Ledger as a read-only, server-driven history capability. The implementation adds an entry-oriented Inventory Ledger list, transaction-oriented details that support multiple entries, and navigation from Inventory Balance rows into filtered ledger history. The design reuses the Inventory Balance server-driven slice pattern: shared list contracts, thin `[AsParameters]` endpoint binding, internal explicit queries, filter/count/sort/page/projection in the WMS module, WebApp API-client query construction, MudDataGrid `ServerData`, and risk-based tests at the lowest owning layer.

The feature does not change the ledger write model, does not create migrations, and does not introduce Transfer, InventoryAccount, export, analytics, user identity, historical reference snapshots, or generic framework behavior.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET 10 solution.

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core SQL Server provider, Blazor WebApp, MudBlazor, existing Myrmex command/query dispatchers, `ServiceResult`/ProblemDetails helpers, `ListResult<T>`, `WmsInventoryApiClient`, xUnit test project.

**Storage**: Existing SQL Server-backed WMS schema through `WmsDbContext`. The current schema already contains `InventoryTransactions` and `InventoryLedgerEntries` from the adjustment ledger slice. No schema changes or migrations are planned for this feature.

**Testing**: Existing xUnit tests. Follow risk-based minimal testing: handler/persistence tests for filtering, count-before-paging, deterministic sorting, bounded projection, inactive references, occurrence range, and multi-entry details; focused endpoint/API-client tests for public binding, route, query string, cancellation, and DTO serialization; manual UI smoke validation for Blazor page/dialog behavior.

**Target Platform**: Existing Myrmex modular-monolith API service and Blazor WebApp.

**Project Type**: Brownfield WMS vertical slice spanning shared contracts, WMS backend read queries/endpoints, WebApp API client and UI page/dialog.

**Performance Goals**: Initial Inventory Ledger page loads newest-first paged history without loading full transaction graphs. Users can open transaction details from a list row in under 10 seconds. Default and requested sorts remain deterministic across repeated paged requests when underlying history has not changed.

**Constraints**: Read-only Ledger history only. Reuse Inventory Balance server-driven list conventions. Apply filters before count and paging. Use bounded EF projections without Include-heavy graphs. Verify inactive-inclusive lookup behavior for SKU, storage location, and warehouse. Exact UTC occurrence range UI maps to inclusive-from/exclusive-to request values. Do not add indexes until actual query shapes are inspected during implementation. Do not run build, tests, EF, app startup, formatters, linters, or infrastructure commands automatically.

**Scale/Scope**: One read-side Inventory Ledger list query, one transaction details query, one endpoint group, several shared contracts, one WebApp client extension, one Ledger page with filters/grid, one details dialog, one Inventory Balance row action, focused tests, and validation guidance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names `InventoryTransaction`, `InventoryLedgerEntry`, entry-oriented history rows, transaction details, before/delta/after invariants, occurrence time, reason, SKU, UoM, warehouse, and storage-location context before implementation mechanics.
- **Modular Monolith Boundaries**: PASS. Public request/response contracts stay in `Myrmex.Shared`; internal queries, handlers, projections, and EF access stay in `Myrmex.Modules.Wms`; UI state and grid mapping stay in `Myrmex.WebApp`.
- **Vertical Slice Delivery**: PASS. The read slice covers shared contracts, endpoint binding, internal queries, handler/projection logic, API client, WebApp grid/dialog/navigation integration, and focused tests. Public transport contracts remain separate from internal queries.
- **Testing Discipline**: PASS with documented UI automation exception below. The plan identifies concrete list/query/client/UI risks and assigns automated coverage to the lowest layer that owns each risk while avoiding duplicate filter/sort matrices at endpoint and client layers.
- **Simplicity and Observability**: PASS. The design reuses the Inventory Balance server-driven list pattern, existing lookup APIs where adequate, existing error conventions, and no new generic ledger/grid/reporting framework.

## Project Structure

### Documentation (this feature)

```text
specs/073-inventory-ledger-server-driven-history/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── inventory-ledger-api-contract.md
│   └── inventory-ledger-ui-contract.md
├── checklists/
│   └── requirements.md
└── spec.md
```

### Source Code (repository root)

```text
Myrmex.Shared/
└── Wms/Inventory/
    ├── ListInventoryLedgerEntriesRequest.cs          # create
    ├── InventoryLedgerEntryDetails.cs                # create
    ├── InventoryLedgerSortBy.cs                      # create
    ├── InventoryTransactionDetails.cs                # create
    └── InventoryTransactionEntryDetails.cs           # create

Myrmex.Modules.Wms/
└── Inventory/
    ├── Features/
    │   └── InventoryLedger/
    │       ├── ListInventoryLedgerEntries.cs         # create
    │       ├── GetInventoryTransactionById.cs        # create
    │       └── InventoryLedgerQueryableExtensions.cs # create
    └── Endpoints/
        ├── InventoryLedgerEndpoints.cs               # create
        └── InventoryEndpoints.cs                     # modify: map ledger endpoints

Myrmex.WebApp/
├── Wms/Inventory/WmsInventoryApiClient.cs            # modify: list ledger + transaction details
├── Components/Layout/NavMenu.razor                   # modify: add Inventory Ledger link
└── Components/Pages/Wms/Inventory/
    ├── InventoryBalancePages/
    │   ├── InventoryBalanceGrid.razor                # modify: add History action
    │   └── Index.razor.cs                            # modify: navigate to filtered ledger
    └── InventoryLedgerPages/                         # create
        ├── Index.razor
        ├── Index.razor.cs
        ├── InventoryLedgerFilters.razor
        ├── InventoryLedgerGrid.razor
        ├── InventoryLedgerGridRequest.cs
        └── InventoryTransactionDetailsDialog.razor

Myrmex.Tests/
└── Wms/Inventory/
    ├── Client/WmsInventoryApiClientTests.cs          # modify/add focused ledger client tests
    ├── Endpoints/InventoryLedgerEndpointTests.cs     # create
    └── Features/InventoryLedger/
        └── InventoryLedgerHandlerTests.cs            # create
```

**Structure Decision**: Add the Inventory Ledger read side inside the existing WMS Inventory capability rather than creating a new module or reporting framework. Keep ledger read contracts beside Inventory Balance contracts in `Myrmex.Shared.Wms.Inventory`; keep backend query/projection code under `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger`; keep Ledger UI under a dedicated WebApp Inventory page folder.

## Architectural Design Notes

- **Domain concepts first**: `InventoryTransaction` is one completed inventory operation; `InventoryLedgerEntry` is one immutable quantity change. The primary list is one row per ledger entry with parent transaction context. Transaction details group all entries for one transaction. The persisted invariant `BalanceAfter = BalanceBefore + QuantityDelta` is displayed, not recalculated from current balance rows.
- **Shared contract boundary**: Add feature-specific transport contracts in `Myrmex.Shared.Wms.Inventory`: list request, entry list/detail DTO, transaction details DTO, and sort-key constants. These contracts cross the backend/client boundary and contain no EF expressions, domain entities, handlers, or UI state.
- **Internal request boundary**: Add internal explicit queries `ListInventoryLedgerEntries.Query` and `GetInventoryTransactionById.Query`. Endpoints map public request values into these queries and dispatch them through the existing query dispatcher.
- **Backend-owned projection**: Validate the request before SQL query construction, normalize paging, create an `InventoryLedgerEntries.AsNoTracking()` base query, apply filters to `IQueryable`, calculate count, sort, skip/take, then project only required scalar/nested DTO fields from `InventoryTransaction`, `StockKeepingUnit`, base UoM, `StorageLocation`, and `Warehouse`. Avoid `Include` and avoid loading full transaction or navigation graphs for the list.
- **Server-driven list behavior**: Reuse Inventory Balance flow: shared request -> `[AsParameters]` endpoint -> internal query -> validate request -> normalize paging -> create base `AsNoTracking` query -> filters -> `CountAsync` -> deterministic sorting -> `Skip`/`Take` -> backend-owned projection -> `ListResult<T>`. Supported filters are SKU, warehouse, storage location, transaction type, occurrence-from UTC, and occurrence-to UTC. Default sort is `OccurredAtUtc` descending, then transaction ID descending, then entry ID descending. Every requested sort must include stable transaction and entry tie-breakers.
- **Client/grid behavior**: Extend `WmsInventoryApiClient` with ledger list and transaction details methods. Add a UI-specific `InventoryLedgerGridRequest` between MudBlazor grid state and the shared API request. Filter changes reset to the first page; refresh reloads current grid state. Initial Ledger load has no filters and uses default newest-first paging. Inventory Balance row history action navigates to the Ledger page with SKU, warehouse, and storage-location query parameters.
- **Inactive lookup behavior**: SKU autocomplete should use existing `LookupStockKeepingUnits` with `SelectableOnly = false`. Storage-location autocomplete should use existing `LookupStorageLocations` with `SelectableOnly = false` and warehouse scoping. Warehouse filter should load warehouses with `IncludeInactive = true`, unlike the current Inventory Balance page that loads only active warehouses. If implementation discovers a gap for inactive historical warehouse lookup, add a small history-appropriate behavior rather than hiding valid history.
- **Exact UTC date/time mapping**: UI controls collect exact UTC boundaries for `OccurredFromUtc` and `OccurredToUtc`. The API request sends `DateTimeOffset` UTC values. Server filtering is `OccurredAtUtc >= OccurredFromUtc` and `OccurredAtUtc < OccurredToUtc`. A range where from is later than to is a validation failure; equal boundaries are valid and represent an empty interval. Do not silently reinterpret stored UTC as unspecified local time.
- **Cancellation and errors**: Propagate cancellation from MudDataGrid/filter lookup through API client, endpoint, dispatcher, and EF queries. Expected cancellation is not shown as an error. Empty list results return empty items and `TotalCount = 0`. Missing transaction details returns NotFound using existing conventions. Unsupported transaction type and invalid occurrence range return validation ProblemDetails.
- **Index policy**: The current schema has occurrence, transaction FK, SKU, and storage-location indexes. Do not add combined indexes during planning or tasking. Implementation should inspect actual query shapes and generated SQL before proposing a later migration for any combined index.
- **Risk-based testing**: Handler/persistence tests own filter semantics, count-before-paging, sorting, deterministic order, projection, inactive reference visibility, occurrence range, and multi-entry details. Endpoint tests cover binding/route/representative JSON only. API-client tests cover URL construction, query omission, route construction, cancellation propagation, and nested deserialization. UI uses manual smoke validation.
- **Existing pattern precedence**: Follow `ListInventoryBalances`, `InventoryBalanceQueryableExtensions`, `InventoryBalanceEndpoints`, `WmsInventoryApiClient`, `InventoryBalanceGrid`, Inventory lookup usage, and `docs/architecture/server-driven-list-slice-pattern.md`.

## Required Design Details

### Public Contract Shape

- `ListInventoryLedgerEntriesRequest`
  - `Skip`, `Take`, `SortBy`, `SortDescending`
  - `StockKeepingUnitId`, `WarehouseId`, `StorageLocationId`
  - `TransactionType`
  - `OccurredFromUtc`, `OccurredToUtc`
- `InventoryLedgerSortBy`
  - Preserve the existing `InventoryBalanceSortBy` public-value convention: constants and public string values are PascalCase, for example `public const string OccurredAtUtc = "OccurredAtUtc";`.
  - `OccurredAtUtc`
  - `TransactionType`
  - `SkuCode`
  - `SkuName`
  - `WarehouseCode`
  - `WarehouseName`
  - `StorageLocationCode`
  - `BalanceBefore`
  - `QuantityDelta`
  - `BalanceAfter`
  - `Reason`
- `InventoryLedgerEntryDetails`
  - `EntryId`, `TransactionId`, `TransactionType`, `Reason`, `OccurredAtUtc`
  - `BalanceBefore`, `QuantityDelta`, `BalanceAfter`
  - nested SKU, UoM, storage-location, and warehouse info
- `InventoryTransactionDetails`
  - transaction header: ID, type, reason, occurrence time, creation time
  - ordered `InventoryTransactionEntryDetails` collection supporting multiple entries
- `InventoryTransactionEntryDetails`
  - `EntryId`, `BalanceBefore`, `QuantityDelta`, `BalanceAfter`
  - nested SKU, UoM, storage-location, and warehouse info

Use feature-specific nested info records unless an existing nested DTO can be reused without coupling Ledger behavior to Inventory Balance transport names.

### API Routes and Endpoint Behavior

- `GET /api/wms/inventory/ledger`
  - Bind `ListInventoryLedgerEntriesRequest` through `[AsParameters]`.
  - Return `ListResult<InventoryLedgerEntryDetails>`.
  - Empty results return success with no items and total count zero.
- `GET /api/wms/inventory/transactions/{transactionId:guid}`
  - Return `InventoryTransactionDetails`.
  - Missing transaction returns NotFound.

No POST, PUT, PATCH, DELETE, correction, reversal, rebuild, export, or analytics endpoints are included.

### Query and Projection Strategy

List handler sequence:

```text
validate request
    -> normalize paging
    -> create base InventoryLedgerEntries.AsNoTracking() query
    -> apply SKU / warehouse / storage-location / transaction-type / occurrence filters
    -> CountAsync
    -> deterministic sorting
    -> Skip / Take
    -> bounded scalar/nested projection
    -> materialize
    -> ListResult<InventoryLedgerEntryDetails>
```

Validation happens before query execution and before unsupported transaction-type values or invalid occurrence ranges participate in SQL construction. Reject `OccurredFromUtc > OccurredToUtc`; keep `OccurredFromUtc == OccurredToUtc` valid as an empty interval.

Transaction details handler sequence:

```text
InventoryTransactions.AsNoTracking()
    -> filter by transaction ID
    -> project transaction header and ordered entry details
    -> return details or NotFound
```

Details entry ordering should be deterministic, using entry ID where no explicit business sequence exists.

Requested-sort ordering is exact:

```text
requested primary sort in requested direction
then InventoryTransactionId ascending
then InventoryLedgerEntryId ascending
```

Default ordering remains:

```text
OccurredAtUtc descending
then InventoryTransactionId descending
then InventoryLedgerEntryId descending
```

### WebApp Behavior

- Add Inventory navigation item "Inventory Ledger" beside "Inventory Balances".
- Ledger page route should support query parameters for `stockKeepingUnitId`, `warehouseId`, and `storageLocationId`.
- Inventory Balance history navigation uses `/wms/inventory/ledger?stockKeepingUnitId={Sku.Id}&warehouseId={StorageLocation.Warehouse.Id}&storageLocationId={StorageLocation.Id}`.
- Routed filter parameters are optional and may be provided independently:
  - `stockKeepingUnitId` only: hydrate and apply the SKU filter.
  - `warehouseId` only: hydrate and apply the warehouse filter.
  - `warehouseId` + `storageLocationId`: hydrate both, verify the storage location belongs to the warehouse, and apply both filters.
  - `storageLocationId` without `warehouseId`: load the exact storage location, derive its warehouse, hydrate both warehouse and storage-location display objects, and apply both filters.
  - `warehouseId` + `storageLocationId` mismatch: show clear page-level validation/error feedback and do not issue a filtered ledger request with the inconsistent pair.
- Any valid subset of routed filters must remain visible and usable after copied-link reload. Do not require all three parameters merely to initialize routed filter state.
- Routed query-state hydration must be exact and must not rely on bounded empty-search autocomplete results:
  1. Bind any present `stockKeepingUnitId`, `warehouseId`, and `storageLocationId`.
  2. Load inactive-inclusive warehouses.
  3. Resolve the selected warehouse when `warehouseId` is present, or derive and resolve it from the exact storage location when only `storageLocationId` is present.
  4. Resolve the exact SKU by ID using existing `WmsCatalogApiClient.GetStockKeepingUnitByIdAsync` when `stockKeepingUnitId` is present.
  5. Resolve the exact storage location by ID using existing `WmsTopologyApiClient.GetStorageLocationByIdAsync` when `storageLocationId` is present, and verify it belongs to the selected or derived warehouse.
  6. Populate selected filter display objects.
  7. Apply the hydrated IDs to the ledger request.
  8. Load the first grid page.
- Reuse existing `WmsTopologyApiClient.GetWarehouseByIdAsync` if the inactive-inclusive warehouse list does not contain the routed warehouse. Existing `GetStockKeepingUnitById`, `GetWarehouseById`, and `GetStorageLocationById` handlers project by ID without `IsActive` filters, so they can restore inactive historical references. If implementation discovers an exact hydration read is missing, add the smallest feature-specific read required; do not create a generic lookup framework.
- When routed filter parameters are present, the page must complete exact-ID hydration before rendering, activating, or otherwise allowing `MudDataGrid.ServerData` to issue its first request. Use an `_isInitializing`, `_isHydratingFilters`, or equivalent repository-consistent guard; do not introduce a state-management framework. After hydration succeeds, issue exactly the intended first filtered page request and avoid an initial unfiltered request followed by a filtered request. Expected hydration cancellation must not appear as an error. When no routed filters are present, the page may load the initial unfiltered newest-first page normally.
- Copied or reloaded Ledger URLs must restore visible SKU, warehouse, and storage-location filter state and request the same filtered history.
- Ledger filters:
  - SKU autocomplete with inactive-inclusive lookup.
  - Warehouse selector loaded inactive-inclusive.
  - Storage-location autocomplete disabled until warehouse is selected, scoped by selected warehouse, inactive-inclusive.
  - Transaction type selector with `Adjustment` for MVP.
  - Occurrence-from UTC and occurrence-to UTC controls.
  - Clear/reset action.
- Ledger grid:
  - Columns: occurred UTC, transaction type, SKU, warehouse, storage location, before, delta, after, reason, details action.
  - Quantity formatting should follow Inventory Balance conventions; delta keeps its sign and must not rely on color alone.
- Details dialog:
  - Show transaction header and all entries.
  - No edit/delete/correction controls.
- Inventory Balance grid:
  - Add a history action to navigate to Ledger with SKU, warehouse, and storage-location filters.

### Test Strategy

| Regression risk | Lowest owning layer | Planned coverage |
|-----------------|---------------------|------------------|
| Filters applied after count or paging | Handler/persistence | Handler test verifies filters, count-before-paging, and page contents |
| Default newest-first order is unstable | Handler/persistence | Handler test verifies `OccurredAtUtc desc`, transaction ID descending, and entry ID descending |
| Supported sort key omits deterministic tie-breakers | Handler/persistence | Sort theory covers requested primary direction, then transaction ID ascending and entry ID ascending |
| Occurrence range boundaries are wrong | Handler/persistence | Boundary test covers inclusive from and exclusive to |
| Inactive references disappear from history | Handler/persistence and lookup review | Projection test with inactive SKU/location/warehouse/UoM still returns list/details; lookup behavior verified with existing inactive-inclusive paths |
| Projection loads full graphs or depends on current balances | Handler/projection review plus tests | Tests verify nested fields and history remains independent of current balance rows |
| Transaction details assume one entry | Handler/persistence | Details test with multi-entry fixture returns all entries in deterministic order |
| Public list binding or route drifts | Endpoint | One endpoint test verifies query binding and representative JSON shape |
| API client query construction drifts | API client | Client tests verify omitted empty query, all filter/sort/date parameters, details route, cancellation |
| UI history navigation and filters regress | Manual smoke | Quickstart covers navigation, partial routed filters, exact query-state hydration, copied-link restoration, initialization guard, clear/change filters, details dialog, cancellation behavior |

Do not duplicate the full filter/sort matrix through endpoint, API-client, and UI tests. Do not add Blazor component-test infrastructure for this feature.

## Project Artifact Plan

### Created Documentation

- `specs/073-inventory-ledger-server-driven-history/research.md`
- `specs/073-inventory-ledger-server-driven-history/data-model.md`
- `specs/073-inventory-ledger-server-driven-history/contracts/inventory-ledger-api-contract.md`
- `specs/073-inventory-ledger-server-driven-history/contracts/inventory-ledger-ui-contract.md`
- `specs/073-inventory-ledger-server-driven-history/quickstart.md`

### Expected Production Files to Create During Implementation

- `Myrmex.Shared/Wms/Inventory/ListInventoryLedgerEntriesRequest.cs`
- `Myrmex.Shared/Wms/Inventory/InventoryLedgerEntryDetails.cs`
- `Myrmex.Shared/Wms/Inventory/InventoryLedgerSortBy.cs`
- `Myrmex.Shared/Wms/Inventory/InventoryTransactionDetails.cs`
- `Myrmex.Shared/Wms/Inventory/InventoryTransactionEntryDetails.cs`
- `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger/ListInventoryLedgerEntries.cs`
- `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger/GetInventoryTransactionById.cs`
- `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger/InventoryLedgerQueryableExtensions.cs`
- `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryLedgerEndpoints.cs`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/Index.razor`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/Index.razor.cs`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/InventoryLedgerFilters.razor`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/InventoryLedgerGrid.razor`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/InventoryLedgerGridRequest.cs`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/InventoryTransactionDetailsDialog.razor`
- `Myrmex.Tests/Wms/Inventory/Features/InventoryLedger/InventoryLedgerHandlerTests.cs`
- `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryLedgerEndpointTests.cs`

### Expected Production Files to Modify During Implementation

- `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs`
- `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- `Myrmex.WebApp/Components/Layout/NavMenu.razor`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`
- `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Files Not Planned for This Feature

- No domain entity changes.
- No EF mapping changes.
- No migration files.
- No `WmsDbContextModelSnapshot` changes.
- No new generic grid, lookup, reporting, analytics, or export framework.

## Phase 0: Research Output

See `research.md`.

## Phase 1: Design Outputs

See `data-model.md`, `contracts/inventory-ledger-api-contract.md`, `contracts/inventory-ledger-ui-contract.md`, and `quickstart.md`.

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define InventoryTransaction, InventoryLedgerEntry, immutable history, entry-oriented list, transaction-oriented details, and before/delta/after invariants.
- **Modular Monolith Boundaries**: PASS. Contracts, backend queries, endpoints, and WebApp UI/client responsibilities stay in their existing project boundaries.
- **Vertical Slice Delivery**: PASS. Design covers public request/response contracts, endpoint behavior, query handlers, projection, API client, WebApp grid/dialog/navigation, and tests.
- **Testing Discipline**: PASS with UI automation exception below. Tests are focused and assigned to owning layers; duplicate endpoint/client/UI matrices are intentionally avoided.
- **Simplicity and Observability**: PASS. The plan reuses local patterns and existing diagnostics/errors; no new service split, generic framework, or speculative indexes are introduced.

No architecture complexity exceptions are requested.

## Complexity Tracking

No architecture complexity exceptions are requested.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| Blazor component automation for Inventory Ledger page, filters, balance navigation, and transaction details dialog | The current test project has no component-test infrastructure; adding one for this read-only page would be disproportionate and cross-cutting. | Handler tests cover list/detail behavior; API-client tests cover URLs and deserialization; endpoint tests cover binding/representative JSON. | Quickstart manual smoke checks for page load, filters, inactive references, details dialog, balance-to-history navigation, cancellation, and no mutation controls. | No. Revisit only if the project adopts component automation broadly. |

## Unresolved Technical Decisions Before `/speckit-tasks`

- None. Planning selects the existing server-driven list pattern, inactive-inclusive lookup behavior, exact UTC range mapping, bounded projection approach, no speculative indexes, and minimal risk-based tests.
