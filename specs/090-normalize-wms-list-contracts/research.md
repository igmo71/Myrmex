# WMS List Contract, Sorting, Paging, and Grid Audit

## Executive Summary

All nine backend lists use explicit module queries, filter before `CountAsync`, normalize paging through `ListQuery`, project before materialization, and return `ListResult<T>`. Domain entities are not serialized. `Myrmex.Shared` remains free of domain, EF Core, Blazor, MudBlazor, handler, and infrastructure dependencies.

The principal split is at the public/API and WebApp boundaries. Inventory lists use shared slice-specific requests, shared DTOs and sort constants, `[AsParameters]`, and `MudDataGrid.ServerData`. Warehouses use a server-driven grid but retain the older generic `ListRequest`, primitive endpoint binding, and duplicated backend/WebApp DTOs. Zones, Storage Locations, SKUs, and UoM retain the older contract shape and fetch at most 100 rows for client-side sorting/paging, so results beyond the first bounded fetch cannot appear.

Deterministic paging is explicit for Warehouse and all inventory lists. Zones, Storage Locations, SKUs, and UoM omit a stable secondary tie-breaker. Sort-key casing and ownership are inconsistent: topology/catalog handlers accept lower-case raw strings; most inventory constants use PascalCase values; Inventory Count constants use camelCase. Transfer and Count display warehouse name but expose only warehouse-code sorting, leaving their warehouse columns non-sortable. Balance explicitly defaults its grid to SKU code while the backend fallback is ID.

Normalization should proceed slice-by-slice using existing Warehouse/inventory implementations as references. Contract migration, server-driven grid conversion, and default/sort changes require focused protection; purely mechanical reuse should follow those decisions. No runtime changes are part of this audit.

## Compact Slice Comparison

| Slice | Public request / response | Endpoint | Backend default / deterministic | WebApp grid | Material gap |
|---|---|---|---|---|---|
| Warehouses | generic shared `ListRequest`; duplicated details DTO | primitive parameters | Name, then ID / yes | server data, Name | contract/binding generation differs |
| Zones | generic request; duplicated DTO | primitive parameters | Code / no tie | bounded client-side | only first 100; unstable ties |
| Storage Locations | generic request; duplicated DTO | primitive parameters | Code / no tie | bounded client-side | only first 100; unstable ties |
| SKUs | generic request; duplicated DTO | primitive parameters | Code / no tie | bounded client-side | only first 100; unstable ties |
| UoM | generic request; duplicated DTO | primitive parameters | Code / no tie | bounded client-side | only first 100; unstable ties |
| Inventory Balances | shared slice request/DTO/sorts | `[AsParameters]` | ID / yes | server data, SKU Code | backend/UI fallback mismatch |
| Inventory Ledger | shared slice request/DTO/sorts | `[AsParameters]` | Occurred desc / yes | server data, same | aligned |
| Inventory Transfers | shared slice request/DTO/sorts | `[AsParameters]` | Created desc / yes | server data, same | no WarehouseName sort |
| Inventory Counts | shared slice request/DTO/sorts | `[AsParameters]` | Created desc / yes | server data, same | camelCase keys; no WarehouseName sort |

## Shared Foundations

- `Myrmex.Shared/Common/ListRequest.cs` defaults to skip 0 and take 20 and carries generic search, sort, direction, and active-state fields. `Myrmex.Shared/Common/ListResult.cs` owns the page envelope.
- `Myrmex.Core/Application/Queries/ListQuery.cs` owns runtime normalization: negative skip becomes 0, non-positive take becomes 20, and take is capped at 200. The shared default and core default duplicate the value across intentionally independent boundaries.
- `Myrmex.WebApp/Wms/Api/WmsApiUrls.cs` builds generic list query strings, omitting blank search/sort values while emitting paging, direction, and active-state values.
- The normative flow is `.specify/memory/server-driven-list-slice-pattern.md`; `docs/architecture/server-driven-list-slice-pattern.md` remains the detailed reference.

## Detailed Findings by Slice

### Warehouses

- **Boundaries**: `Myrmex.Modules.Wms/Topology/Features/Warehouses/ListWarehouses.cs`, `Myrmex.Modules.Wms/Topology/Endpoints/WarehouseEndpoints.cs`, `Myrmex.WebApp/Wms/Topology/WmsTopologyApiClient.cs`, and `Myrmex.WebApp/Components/Pages/Wms/Topology/WarehousePages/WarehouseGrid.razor`.
- **Contracts**: client input is generic `ListRequest`; response details are duplicated between module-owned `WarehouseDetails` and a WebApp-local record. The endpoint binds primitives rather than `[AsParameters]`, then maps to an explicit internal `Query`.
- **Pipeline**: active/search filters precede count; paging is normalized; supported ordering is applied before `Skip`/`Take`; `WarehouseDetails.Projection` runs before materialization; normalized metadata is returned.
- **Sorting**: handler accepts raw `code`, `name`, `createdatutc`, `updatedatutc`, and `isactive`; every branch adds `ThenBy(Id)`, and fallback is Name then ID. `Myrmex.Shared/Wms/Topology/WarehouseSortBy.cs` exposes only Name, CreatedAtUtc, and UpdatedAtUtc using PascalCase values. The grid explicitly defaults to Name.
- **WebApp**: `ServerData` maps through a UI-specific grid request, resets to page zero on filter changes, reloads current state on refresh/mutation, propagates cancellation, and displays warehouse name only.
- **Tests**: `Myrmex.Tests/Wms/Topology/Features/Warehouses/ListWarehousesHandlerTests.cs` protects default Name/ID order. There is no matching endpoint-binding test and no focused Warehouse list API-client test.

### Zones

- **Boundaries**: `Myrmex.Modules.Wms/Topology/Features/Zones/ListZones.cs`, `Myrmex.Modules.Wms/Topology/Endpoints/ZoneEndpoints.cs`, `Myrmex.WebApp/Wms/Topology/WmsTopologyApiClient.cs`, and `Myrmex.WebApp/Components/Pages/Wms/Topology/ZonePages/ZoneGrid.razor`.
- **Contracts**: generic shared request at the client, primitive endpoint parameters, explicit internal query, and duplicated module/WebApp response records.
- **Pipeline**: warehouse validation and filters precede count; normalization, sorting, paging, and module projection are correctly ordered; result metadata is normalized.
- **Sorting**: raw lower-case Code, Name, CreatedAtUtc, UpdatedAtUtc, and IsActive variants are supported; fallback is Code. No branch adds an ID tie-breaker, so paging is not deterministic for duplicate primary values.
- **WebApp**: the page requests `Take = 100`; `ZoneGrid` receives `Items` and uses local multi-sort/paging. It is not a server-driven grid and cannot reveal records beyond the initial 100. Warehouse references display names.
- **Tests**: no Zone list handler test was found; existing Zone tests cover creation. `WmsTopologyApiClientTests.cs` checks a Zone list URL/mapping but not server grid or deterministic ordering behavior.

### Storage Locations

- **Boundaries**: `Myrmex.Modules.Wms/Topology/Features/StorageLocations/ListStorageLocations.cs`, `Myrmex.Modules.Wms/Topology/Endpoints/StorageLocationEndpoints.cs`, `Myrmex.WebApp/Wms/Topology/WmsTopologyApiClient.cs`, and `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/StorageLocationGrid.razor`.
- **Contracts**: the same generic-request, primitive-binding, and duplicated-response pattern as Zones.
- **Pipeline**: optional warehouse/zone validation and filtering precede count; normalization, sort, page, projection, materialization, and result metadata are correctly ordered.
- **Sorting**: raw lower-case Code, Name, IsPickable, CreatedAtUtc, UpdatedAtUtc, and IsActive variants are supported; default is Code; no stable secondary order is applied.
- **WebApp**: bounded `Take = 100` plus an `Items` grid performs local sorting/paging. Warehouse display is name-only, but the list is incomplete above the bound.
- **Tests**: no list-handler test was found. Lookup tests protect a separate bounded lookup operation, not this list. Topology client tests do not provide focused Storage Location list coverage.

### SKUs

- **Boundaries**: `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnits.cs`, `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`, `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`, and `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuGrid.razor`.
- **Contracts**: generic shared request and shared result envelope, primitive endpoint binding, explicit internal query, and duplicated response records.
- **Pipeline**: active/search filters, normalized paging, filtered count, sort, page, backend projection, and materialization follow the required order.
- **Sorting**: raw Code, Name, and IsActive keys are normalized in the handler; fallback is Code; no ID tie-breaker is present.
- **WebApp**: the page fetches 100 records and supplies `Items` to a local grid, so client sorting/paging is bounded and not server-driven.
- **Tests**: `ListStockKeepingUnitsHandlerTests.cs` covers active/search, paging normalization and total, supported sorts, direction, and fallback, but not duplicate-value tie stability. `WmsCatalogApiClientTests.cs` covers the list URL and mapping.

### Units of Measure

- **Boundaries**: `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasure.cs`, `Myrmex.Modules.Wms/Catalog/Endpoints/UnitOfMeasureEndpoints.cs`, `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`, and `Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomGrid.razor`.
- **Contracts and WebApp**: match the SKU generation: generic request, primitive binding, duplicated DTO, bounded 100-row fetch, and local grid operations.
- **Pipeline**: active/search filters (Code, Name, Symbol), count, normalization, sort, page, and backend projection are correctly sequenced.
- **Sorting**: Code, Name, and IsActive are supported; fallback is Code; no stable tie-breaker exists.
- **Tests**: handler tests cover activity, search, supported sorts, and fallback but not count-before-page normalization or tie stability. Client tests cover URL/mapping.

### Inventory Balances

- **Boundaries**: shared request/DTO/sorts under `Myrmex.Shared/Wms/Inventory/`; `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/ListInventoryBalances.cs` and `InventoryBalanceQueryableExtensions.cs`; `InventoryBalanceEndpoints.cs`; corresponding WebApp client, grid request, and grid.
- **Contracts**: slice-specific transport types live in `Myrmex.Shared`; endpoint uses `[AsParameters]` and maps to an explicit internal query; EF projection remains in the module.
- **Pipeline**: SKU/location/warehouse filters precede count; normalization, deterministic sorting, paging, projection data, materialization, DTO mapping, and `ListResult` follow the required flow.
- **Sorting**: Quantity, SkuCode, SkuName, SkuBaseUomSymbol, StorageLocationCode, WarehouseCode, and WarehouseName constants use PascalCase values. All supported sorts add ID as tie-breaker. Backend fallback is ID; grid default is SkuCode ascending, so direct requests omitting sort do not share the grid default.
- **WebApp/tests**: server-data flow, reset/reload, cancellation, name-only warehouse display, endpoint binding, client query omission/construction, ProblemDetails, filtered totals, projection, and deterministic sorts are strongly covered.

### Inventory Ledger

- **Boundaries**: shared inventory contracts; `ListInventoryLedgerEntries.cs`, `InventoryLedgerQueryableExtensions.cs`, endpoint, client, grid request, and server-data grid.
- **Pipeline**: validated filters, filtered count, normalized paging, deterministic sort, projection, and result construction conform to the pattern.
- **Sorting**: PascalCase constants cover occurrence, transaction, SKU, warehouse code/name, location, balances, delta, and reason. Supported sorts add transaction ID and entry ID ties; default is occurrence descending with descending transaction/entry ties.
- **WebApp/tests**: grid default matches backend fallback. Cancellation and read-list error handling are preserved. Handler, endpoint, and client tests cover distinct persistence, binding/serialization, and query/error risks.

### Inventory Transfers

- **Boundaries**: shared slice contracts; `ListInventoryTransfers.cs`, `InventoryTransferQueryableExtensions.cs`, endpoint, client, UI grid request, and server-data grid.
- **Pipeline**: warehouse/status/date/code/source/destination/SKU/transit filters precede count; sorting, paging, module projection, DTO mapping, and normalized result conform.
- **Sorting**: PascalCase constants cover Code, Status, WarehouseCode, CreatedAtUtc, and requested/picked/placed/in-transit totals. Each supported order has an ID tie; fallback is CreatedAtUtc descending then ID descending.
- **WebApp**: default aligns. Warehouse displays name only, but the public sort contract has WarehouseCode rather than WarehouseName, so the visible warehouse column is not sortable by its displayed value.
- **Tests**: one handler test protects a combined filter/count/aggregate-sort scenario but not every key/tie. Endpoint binding and client query/cancellation have focused coverage.

### Inventory Counts

- **Boundaries**: shared slice contracts; `ListInventoryCounts.cs`, `InventoryCountQueryableExtensions.cs`, endpoint, client, UI grid request, and server-data grid.
- **Pipeline**: validated warehouse/status/date filters, count, normalization, deterministic sort, page, projection, mapping, and result conform.
- **Sorting**: constants use camelCase values (`createdAtUtc`, `status`, `warehouseCode`), unlike other inventory contracts. Default is CreatedAtUtc descending then ID descending; supported branches use stable direction-consistent ID ties.
- **WebApp**: default aligns and warehouse displays name only. The visible warehouse column lacks a WarehouseName sort contract.
- **Tests**: handler tests cover filters/progress, default order, equal-time stability, validation, and cancellation. Endpoint and client tests protect binding, query construction, cancellation, and read errors; no full sort matrix is needed unless those keys change.

## Cross-Cutting Inconsistencies

1. **Contract generations**: topology/catalog duplicate response records and bind primitives; inventory owns public contracts in `Myrmex.Shared` and uses `[AsParameters]`.
2. **Grid generations**: four catalog/topology grids are bounded client-side lists; five grids are server-driven. The former silently exclude records beyond 100.
3. **Determinism**: Zone, Storage Location, SKU, and UoM sorts have no explicit ID tie-breaker.
4. **Sort contracts**: raw strings, incomplete constants, PascalCase, and camelCase coexist. Some handlers accept undocumented keys.
5. **Visible-value sorting**: Transfer and Count show warehouse name but cannot request WarehouseName sorting. Balance and Ledger can.
6. **Default alignment**: Balance grid defaults to SkuCode while backend fallback is ID. Other server grids align with backend fallback.
7. **Paging values**: server grids repeat `[10, 25, 50, 100]`; lookup/fetch bounds repeat 100. `ListQuery` correctly remains the backend normalization authority; identical values across independent boundaries are not automatically abstraction defects.
8. **Tests**: inventory coverage generally separates handler, endpoint, and client risks. Legacy slices have material deterministic-paging and server-grid gaps, but broad duplicate matrices would add little value.

## Prioritized Normalization

### Safe Mechanical Work

- After a target contract is approved, replace raw grid sort tags with shared constants and keep MudBlazor types in WebApp-only grid requests.
- Reuse the established API URL-building and page-size convention where ownership is clear; do not create a cross-layer constants package solely for equal numeric values.
- Keep warehouse display name-only and preserve Code/ExternalRefKey in existing contracts and integration behavior.

### Requires Focused Tests

- Add stable ID tie-breakers to Zone, Storage Location, SKU, and UoM handler ordering; protect duplicate-primary-value paging at the handler/persistence layer.
- Convert their four bounded `Items` grids to `ServerData`; protect handler paging/count and API-client query/cancellation, adding endpoint binding tests only when binding changes.
- Migrate legacy public list requests/responses to shared slice contracts and `[AsParameters]`; use focused serialization/binding tests for the changed transport boundary.
- Add WarehouseName sort support for Transfers and Counts if their visible columns become sortable; test query ordering where EF owns the risk.
- Align Balance backend and grid defaults through an explicit product decision, then protect the chosen fallback in handler and grid-request mapping tests.
- Normalize sort-key casing only as an explicitly compatibility-reviewed contract change; additive aliases or staged migration may be required.

### Deferred

- A universal list framework, reflection-driven sort registry, or shared MudBlazor transport abstraction. Existing explicit slices are clearer and respect boundaries.
- Removal of legacy sort aliases or warehouse-code fields. Those may be contract/integration relevant and need separate compatibility analysis.
- Consolidating every occurrence of 20, 100, or page-size arrays across unrelated boundaries without demonstrated drift risk.

## Non-Goals

- No application, test, resource, project, API contract, route, schema, migration, import, or runtime change.
- No WebApp/AppHost execution, build, test, Docker, infrastructure, migration, or database command.
- No removal or reinterpretation of warehouse Code or ExternalRefKey.
- No historical implementation-status notes and no commitment to a universal abstraction.

## Suggested Implementation Phases

1. Stabilize backend ordering for the four legacy lists with focused duplicate-value tests.
2. Define/migrate one legacy slice’s shared request, response, and sort contract; verify binding and client URL behavior.
3. Convert that slice’s grid to server data and validate reset/reload/cancellation behavior; use it as the local migration example.
4. Repeat the proven slice migration for the remaining three lists.
5. Address Balance default alignment and Transfer/Count WarehouseName sorting as separate behavior decisions.
6. Consider casing cleanup and purely mechanical consolidation only after compatibility decisions are explicit.

## Risks

- Sort-key casing or removal can break bookmarked URLs or clients even when C# constants compile.
- Adding tie-breakers changes order among equal primary values; it is correctness work but still user-visible and needs focused tests.
- Grid conversion can regress filter page reset, mutation refresh, cancellation, and error visibility if treated as markup-only work.
- Shared-contract migration can accidentally leak EF expressions or UI types; projection and grid state must remain in their owning layers.
- Default-order alignment is a product behavior choice, not a mechanical refactor.
- Over-testing the same sort matrix at handler, endpoint, and client layers would increase maintenance without protecting distinct risks.
