# Tasks: Server-Driven WMS Catalog and Topology Lists

**Input**: Design documents from `/specs/092-server-driven-wms-lists/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, and `quickstart.md`

**Tests**: Focused automated tests are required for changed handler/query behavior, Minimal API request binding, API-client URL construction/shared DTO deserialization, and cancellation propagation. Repeated MudDataGrid interaction uses the developer-controlled smoke scenarios in `quickstart.md`; do not add a new component-test framework.

**Organization**: Tasks are grouped by user story. Shared transport contracts are foundational because backend, endpoint, client, UI, and test work all compile against them.

## Phase 1: Setup

**Purpose**: Confirm existing projects and infrastructure are sufficient.

No setup changes are required. Reuse the existing .NET 10 projects, `Myrmex.Shared`, WMS endpoints and dispatchers, SQL Server-backed test context, in-process endpoint tests, fake-HTTP client tests, and Inventory/Warehouse server-grid patterns. Do not add dependencies, projects, migrations, or test frameworks.

---

## Phase 2: Foundational - Shared Transport Contracts

**Purpose**: Establish dependency-free public types used by every story.

**CRITICAL**: Complete this phase before backend, endpoint, client, UI, or test migration.

- [X] T001 [P] Add plain SKU and Unit of Measure details/create/update transport records in `Myrmex.Shared/Wms/Catalog/StockKeepingUnitDetails.cs`, `Myrmex.Shared/Wms/Catalog/CreateStockKeepingUnitRequest.cs`, `Myrmex.Shared/Wms/Catalog/UpdateStockKeepingUnitDetailsRequest.cs`, `Myrmex.Shared/Wms/Catalog/UnitOfMeasureDetails.cs`, `Myrmex.Shared/Wms/Catalog/CreateUnitOfMeasureRequest.cs`, and `Myrmex.Shared/Wms/Catalog/UpdateUnitOfMeasureDetailsRequest.cs`
- [X] T002 [P] Add nullable feature list requests and PascalCase sort constants in `Myrmex.Shared/Wms/Catalog/ListStockKeepingUnitsRequest.cs`, `Myrmex.Shared/Wms/Catalog/StockKeepingUnitSortBy.cs`, `Myrmex.Shared/Wms/Catalog/ListUnitsOfMeasureRequest.cs`, and `Myrmex.Shared/Wms/Catalog/UnitOfMeasureSortBy.cs`
- [X] T003 [P] Add plain Warehouse, Zone, Storage Location, Type, Status, create, and update transport records in `Myrmex.Shared/Wms/Topology/WarehouseDetails.cs`, `Myrmex.Shared/Wms/Topology/CreateWarehouseRequest.cs`, `Myrmex.Shared/Wms/Topology/UpdateWarehouseDetailsRequest.cs`, `Myrmex.Shared/Wms/Topology/ZoneDetails.cs`, `Myrmex.Shared/Wms/Topology/CreateZoneRequest.cs`, `Myrmex.Shared/Wms/Topology/UpdateZoneDetailsRequest.cs`, `Myrmex.Shared/Wms/Topology/StorageLocationDetails.cs`, `Myrmex.Shared/Wms/Topology/CreateStorageLocationRequest.cs`, `Myrmex.Shared/Wms/Topology/UpdateStorageLocationDetailsRequest.cs`, `Myrmex.Shared/Wms/Topology/StorageLocationTypeDetails.cs`, and `Myrmex.Shared/Wms/Topology/StorageLocationStatusDetails.cs`
- [X] T004 [P] Add Topology list/lookup requests, Warehouse lookup item, and PascalCase sort constants in `Myrmex.Shared/Wms/Topology/ListWarehousesRequest.cs`, `Myrmex.Shared/Wms/Topology/LookupWarehousesRequest.cs`, `Myrmex.Shared/Wms/Topology/WarehouseLookupItem.cs`, `Myrmex.Shared/Wms/Topology/WarehouseSortBy.cs`, `Myrmex.Shared/Wms/Topology/ListZonesRequest.cs`, `Myrmex.Shared/Wms/Topology/ZoneSortBy.cs`, `Myrmex.Shared/Wms/Topology/ListStorageLocationsRequest.cs`, and `Myrmex.Shared/Wms/Topology/StorageLocationSortBy.cs`

**Checkpoint**: Shared contracts match `contracts/catalog-contracts.md` and `contracts/topology-contracts.md`, contain no domain/EF/UI dependencies, and preserve existing JSON property shapes.

---

## Phase 3: User Story 1 - Browse the Complete Catalog (Priority: P1) MVP

**Goal**: Make SKU and Unit of Measure main grids page, search, sort, and count against the complete backend dataset.

**Independent Test**: With more records than one page, find a match outside the initial order, traverse adjacent pages without omissions/duplicates, sort supported fields across the full result, and verify filter changes reset to page one with the full filtered total.

### Tests for User Story 1

> Write these tests first. Each test protects a distinct changed boundary.

- [X] T005 [P] [US1] Extend handler coverage for CreatedAtUtc/UpdatedAtUtc sorting and shared SKU projection shape in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs`
- [X] T006 [P] [US1] Extend handler coverage for CreatedAtUtc/UpdatedAtUtc sorting and shared UoM projection shape in `Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasureHandlerTests.cs`
- [X] T007 [P] [US1] Add in-process endpoint tests proving `[AsParameters]` binds all nullable SKU/UoM list values and serializes shared details without changing routes in `Myrmex.Tests/Wms/Catalog/Endpoints/CatalogListEndpointTests.cs`
- [X] T008 [P] [US1] Update Catalog client tests to use shared successful fixtures and verify SKU/UoM list query strings, null omission, PascalCase sort values, response mapping, and representative cancellation in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Backend and Contract Integration for User Story 1

- [X] T009 [P] [US1] Replace the module-local SKU DTO with an internal mapper/projection that constructs `Myrmex.Shared.Wms.Catalog.StockKeepingUnitDetails`, and migrate create/get/update/deactivate/reactivate/list handlers in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/StockKeepingUnitDetails.cs`, `CreateStockKeepingUnit.cs`, `GetStockKeepingUnitById.cs`, `UpdateStockKeepingUnitDetails.cs`, `DeactivateStockKeepingUnit.cs`, `ReactivateStockKeepingUnit.cs`, and `ListStockKeepingUnits.cs`
- [X] T010 [P] [US1] Replace the module-local UoM DTO with an internal mapper/projection that constructs `Myrmex.Shared.Wms.Catalog.UnitOfMeasureDetails`, and migrate create/get/update/deactivate/reactivate/list handlers in `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/UnitOfMeasureDetails.cs`, `CreateUnitOfMeasure.cs`, `GetUnitOfMeasureById.cs`, `UpdateUnitOfMeasureDetails.cs`, `DeactivateUnitOfMeasure.cs`, `ReactivateUnitOfMeasure.cs`, and `ListUnitsOfMeasure.cs`
- [X] T011 [P] [US1] Add case-insensitive support for `StockKeepingUnitSortBy.CreatedAtUtc` and `UpdatedAtUtc` while preserving all current filters, fallbacks, and ascending ID ties in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnits.cs`
- [X] T012 [P] [US1] Add case-insensitive support for `UnitOfMeasureSortBy.CreatedAtUtc` and `UpdatedAtUtc` while preserving all current filters, fallbacks, and ascending ID ties in `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasure.cs`
- [X] T013 [US1] Replace scalar list parameters and endpoint-private affected mutation records with shared requests, map list requests explicitly to internal queries, and preserve every Catalog route in `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs` and `Myrmex.Modules.Wms/Catalog/Endpoints/UnitOfMeasureEndpoints.cs`
- [X] T014 [US1] Change list and mutation client signatures to shared Catalog contracts, build feature-specific list URLs with encoded nullable query values, propagate cancellation, and remove affected WebApp-local DTO declarations while leaving barcode contracts intact in `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`
- [X] T015 [US1] Update Catalog dialogs/pages and Inventory Ledger consumers to import the shared SKU/UoM DTO and mutation namespaces after client-local declarations are removed in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuEditDialog.razor`, `Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomEditDialog.razor`, and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/Index.razor.cs`

### WebApp Server Grids for User Story 1

- [X] T016 [P] [US1] Add page-local Skip/Take/SortBy/SortDescending transport-independent grid state in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuGridRequest.cs`
- [X] T017 [US1] Convert the SKU grid to single-sort `ServerData`, shared sort tags, explicit Code default, standard page sizes, total count, reload, and reset methods in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuGrid.razor`
- [X] T018 [US1] Map SKU grid requests plus page filters to `ListStockKeepingUnitsRequest`, propagate cancellation, suppress expected cancellation, preserve genuine errors, keep ancillary base-UoM lookup separate, and wire the grid reference in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor.cs` and `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor`
- [X] T019 [P] [US1] Add page-local Skip/Take/SortBy/SortDescending grid state in `Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomGridRequest.cs`
- [X] T020 [US1] Convert the UoM grid to single-sort `ServerData`, shared sort tags, explicit Code default, standard page sizes, total count, reload, and reset methods in `Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomGrid.razor`
- [X] T021 [US1] Map UoM grid requests plus page filters to `ListUnitsOfMeasureRequest`, propagate cancellation, suppress expected cancellation, preserve genuine errors, and wire the grid reference in `Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/Index.razor.cs` and `Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/Index.razor`
- [X] T022 [US1] Statically verify the Catalog main grids contain no fixed `Take = 100`, generic public `ListRequest`, raw supported sort tags, or client-side paging/sorting in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/` and `Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/`

**Checkpoint**: User Story 1 is independently functional; SKU and UoM lists are complete-dataset server grids with focused handler, endpoint, and client protection.

---

## Phase 4: User Story 2 - Browse and Filter the Complete Topology (Priority: P2)

**Goal**: Normalize the Warehouse list contract and make Zone and Storage Location grids server-driven, including complete-dataset type/status filtering and no-Warehouse gating.

**Independent Test**: Use multi-page Warehouse, Zone, and Storage Location data; verify complete paging/search/sort/totals, combined Storage Location filters before count, deterministic boundaries, and no unrestricted location request without a Warehouse.

### Tests for User Story 2

- [X] T023 [P] [US2] Add Storage Location handler tests for Warehouse/Zone/Type/Status/search/inactive filter intersections, count-before-paging, zero-match IDs, and preserved Warehouse/Zone mismatch behavior in `Myrmex.Tests/Wms/Topology/Features/StorageLocations/ListStorageLocationsHandlerTests.cs`
- [X] T024 [P] [US2] Add in-process endpoint tests for Warehouse and Zone feature-list binding plus both Storage Location nested routes, route-bound IDs, type/status filters, paging, sort, and shared serialization in `Myrmex.Tests/Wms/Topology/Endpoints/TopologyListEndpointTests.cs`
- [X] T025 [P] [US2] Update Topology client tests to use shared successful fixtures and verify Warehouse/Zone/Storage Location feature-list URLs, nested routes, type/status filters, null omission, PascalCase sorts, and representative cancellation in `Myrmex.Tests/Wms/Topology/Client/WmsTopologyApiClientTests.cs`

### Backend and Contract Integration for User Story 2

- [X] T026 [P] [US2] Replace module-local Warehouse details with an internal shared-DTO mapper/projection and migrate Warehouse handlers in `Myrmex.Modules.Wms/Topology/Features/Warehouses/WarehouseDetails.cs`, `CreateWarehouse.cs`, `GetWarehouseById.cs`, `UpdateWarehouseDetails.cs`, `DeactivateWarehouse.cs`, `ReactivateWarehouse.cs`, and `ListWarehouses.cs`
- [X] T027 [P] [US2] Replace module-local Zone details with an internal shared-DTO mapper/projection and migrate Zone handlers in `Myrmex.Modules.Wms/Topology/Features/Zones/ZoneDetails.cs`, `CreateZone.cs`, `GetZoneById.cs`, `UpdateZoneDetails.cs`, `DeactivateZone.cs`, `ReactivateZone.cs`, and `ListZones.cs`
- [X] T028 [P] [US2] Replace module-local Storage Location, Type, and Status details with internal shared-DTO mappers/projections and migrate affected handlers in `Myrmex.Modules.Wms/Topology/Features/StorageLocations/StorageLocationDetails.cs`, `StorageLocationTypeDetails.cs`, `StorageLocationStatusDetails.cs`, `CreateStorageLocation.cs`, `GetStorageLocationById.cs`, `UpdateStorageLocationDetails.cs`, `DeactivateStorageLocation.cs`, `ReactivateStorageLocation.cs`, `ListStorageLocations.cs`, `ListStorageLocationTypes.cs`, and `ListStorageLocationStatuses.cs`
- [X] T029 [P] [US2] Bind `ListWarehousesRequest` through `[AsParameters]`, use shared mutation requests, map explicitly to internal commands/queries, and preserve Warehouse routes in `Myrmex.Modules.Wms/Topology/Endpoints/WarehouseEndpoints.cs`
- [X] T030 [P] [US2] Bind route-aware `ListZonesRequest` through `[AsParameters]`, use shared mutation requests, map explicitly to internal commands/queries, and preserve Zone routes in `Myrmex.Modules.Wms/Topology/Endpoints/ZoneEndpoints.cs`
- [X] T031 [P] [US2] Bind `ListStorageLocationsRequest` through `[AsParameters]` on both existing nested routes, use shared details/mutation requests, map route/query values explicitly, and preserve all Storage Location routes in `Myrmex.Modules.Wms/Topology/Endpoints/StorageLocationEndpoints.cs`
- [X] T032 [US2] Add nullable Type/Status IDs to the internal query and apply them before filtered count without changing Warehouse/Zone validation, deterministic sorting, paging, or projection in `Myrmex.Modules.Wms/Topology/Features/StorageLocations/ListStorageLocations.cs`
- [X] T033 [US2] Change Topology list/mutation client signatures to shared contracts, add feature-specific URL builders for Warehouse/Zone/Storage Location requests, propagate cancellation, and remove affected WebApp-local DTO declarations in `Myrmex.WebApp/Wms/Topology/WmsTopologyApiClient.cs`
- [X] T034 [US2] Update Topology dialogs/pages and Inventory consumers to use shared Warehouse/Zone/Storage Location/Type/Status namespaces after client-local DTO removal in `Myrmex.WebApp/Components/Pages/Wms/Topology/WarehousePages/WarehouseEditDialog.razor`, `Myrmex.WebApp/Components/Pages/Wms/Topology/ZonePages/ZoneEditDialog.razor`, `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/StorageLocationEditDialog.razor`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/`, and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/`

### WebApp Server Grids for User Story 2

- [X] T035 [P] [US2] Switch the Warehouse page to `ListWarehousesRequest`, use shared `WarehouseSortBy` defaults/tags including user-visible Active sorting, and preserve current reload/reset/error behavior in `Myrmex.WebApp/Components/Pages/Wms/Topology/WarehousePages/Index.razor.cs`, `WarehouseGrid.razor`, and `WarehouseGridRequest.cs`
- [X] T036 [P] [US2] Add page-local Zone Skip/Take/SortBy/SortDescending grid state in `Myrmex.WebApp/Components/Pages/Wms/Topology/ZonePages/ZoneGridRequest.cs`
- [X] T037 [US2] Convert the Zone grid to single-sort `ServerData`, shared sort tags, explicit Code default, standard page sizes, total count, reload/reset methods, and preserved row actions in `Myrmex.WebApp/Components/Pages/Wms/Topology/ZonePages/ZoneGrid.razor`
- [X] T038 [US2] Replace the fixed Zone collection load with a cancellation-aware server loader, page-reset filter handlers, empty data when Warehouse is absent, current fixed Warehouse selector compatibility, and grid wiring in `Myrmex.WebApp/Components/Pages/Wms/Topology/ZonePages/Index.razor.cs` and `Index.razor`
- [X] T039 [P] [US2] Add page-local Storage Location Skip/Take/SortBy/SortDescending grid state in `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/StorageLocationGridRequest.cs`
- [X] T040 [US2] Convert the Storage Location grid to single-sort `ServerData`, shared sort tags, explicit Code default, standard page sizes, total count, reload/reset methods, lookup display, and preserved row actions in `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/StorageLocationGrid.razor`
- [X] T041 [US2] Replace the fixed/local-filtered Storage Location collection with a cancellation-aware server loader that sends Warehouse/Zone/Type/Status/search/inactive filters, returns empty data without Warehouse, resets on every filter, retains the deferred Zone preload, and reloads current data in `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/Index.razor.cs`
- [X] T042 [US2] Remove `FilteredStorageLocations`, wire server grid/filter callbacks and shared DTO imports, and preserve the no-Warehouse prompt and lookup displays in `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/Index.razor`, `StorageLocationFilters.razor`, and `StorageLocationGrid.razor`
- [X] T043 [US2] Statically verify Warehouse, Zone, and Storage Location main grids use shared feature requests/constants, backend totals, first-page filter reset, and no client-side type/status filtering while documenting only the deferred Zone selector `Take = 100` in `Myrmex.WebApp/Components/Pages/Wms/Topology/WarehousePages/`, `ZonePages/`, and `StorageLocationPages/`

**Checkpoint**: User Story 2 is independently functional with server-driven Topology list pages, correct Storage Location filtered totals, and no unrestricted main-page location load.

---

## Phase 5: User Story 3 - Select a Warehouse Without Preloading All Warehouses (Priority: P3)

**Goal**: Replace fixed Warehouse preloads on Zone and Storage Location pages with bounded, searchable, deterministic, cancellable Topology lookup.

**Independent Test**: With more than 20 Warehouses, search for one outside the initial result set on both pages, select it, rapidly supersede searches without an error, and verify returned options never exceed 20 and remain stable.

### Tests for User Story 3

- [X] T044 [P] [US3] Add handler tests for Code/Name/Description search, active selectable filtering, default/max Take 20, Name/Code/ID deterministic ordering, and cancellation in `Myrmex.Tests/Wms/Topology/Features/Warehouses/LookupWarehousesHandlerTests.cs`
- [X] T045 [P] [US3] Extend the Topology endpoint suite to verify `/warehouses/lookup` request binding, shared item serialization, route selection before GUID details, and cancellation token dispatch in `Myrmex.Tests/Wms/Topology/Endpoints/TopologyListEndpointTests.cs`
- [X] T046 [P] [US3] Extend Topology client tests for encoded Warehouse lookup search, Take/selectableOnly query values, shared response mapping, and caller cancellation in `Myrmex.Tests/Wms/Topology/Client/WmsTopologyApiClientTests.cs`

### Implementation for User Story 3

- [X] T047 [US3] Implement internal bounded Warehouse lookup normalization, active selectability, current Code/Name/Description search, Name/Code/ID ordering, shared projection, and cancellation in `Myrmex.Modules.Wms/Topology/Features/Warehouses/LookupWarehouses.cs`
- [X] T048 [US3] Map `GET /warehouses/lookup` before the Warehouse GUID detail route and map `LookupWarehousesRequest` to the internal query in `Myrmex.Modules.Wms/Topology/Endpoints/WarehouseEndpoints.cs`
- [X] T049 [US3] Add `LookupWarehousesAsync`, encoded nullable query construction, shared result mapping, and cancellation propagation in `Myrmex.WebApp/Wms/Topology/WmsTopologyApiClient.cs`
- [X] T050 [US3] Replace the Zone page Warehouse `MudSelect` and fixed preload with `MudAutocomplete<WarehouseLookupItem>`, resolve query-supplied Warehouse IDs through the existing detail read, retain URL/filter reset behavior, and suppress only expected lookup cancellation in `Myrmex.WebApp/Components/Pages/Wms/Topology/ZonePages/ZoneFilters.razor`, `Index.razor`, and `Index.razor.cs`
- [X] T051 [US3] Replace the Storage Location page Warehouse `MudSelect` and fixed preload with `MudAutocomplete<WarehouseLookupItem>`, resolve query-supplied Warehouse IDs, clear dependent Zone state on change, retain no-Warehouse gating, and suppress only expected lookup cancellation in `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/StorageLocationFilters.razor`, `Index.razor`, and `Index.razor.cs`

**Checkpoint**: User Story 3 is independently functional; both selectors find Warehouses beyond any fixed preload and remain bounded/cancellation-aware.

---

## Phase 6: User Story 4 - Continue Managing Reference Data (Priority: P4)

**Goal**: Preserve create, edit, deactivate, reactivate, refresh, cancellation, and genuine-error behavior after all list migrations.

**Independent Test**: Perform each existing mutation from every affected page and verify the authoritative current grid/total reloads; superseded reads stay silent; genuine read/write failures remain visible through existing conventions.

### Implementation and Regression Protection for User Story 4

- [X] T052 [US4] Update existing Catalog and Topology mutation client tests first to construct shared request/response fixtures and confirm routes, request bodies, `ApiResult<T>`, and ProblemDetails behavior remain unchanged in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs` and `Myrmex.Tests/Wms/Topology/Client/WmsTopologyApiClientTests.cs`
- [X] T053 [P] [US4] Route SKU and UoM refresh/create/edit/deactivate/reactivate success paths through current-grid server reload without resetting valid page state, while preserving existing localized snackbars and `ApiResult<T>` errors in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor.cs` and `Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/Index.razor.cs`
- [X] T054 [P] [US4] Route Warehouse, Zone, and Storage Location refresh/create/edit/deactivate/reactivate success paths through current-grid server reload without stale collections, while preserving navigation, localized snackbars, and `ApiResult<T>` errors in `Myrmex.WebApp/Components/Pages/Wms/Topology/WarehousePages/Index.razor.cs`, `ZonePages/Index.razor.cs`, and `StorageLocationPages/Index.razor.cs`
- [X] T055 [US4] Audit every affected grid/lookup loader so only `OperationCanceledException` tied to the supplied cancelled token is suppressed and genuine exceptions clear stale rows and populate existing error UI in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor.cs`, `UomPages/Index.razor.cs`, `Myrmex.WebApp/Components/Pages/Wms/Topology/WarehousePages/Index.razor.cs`, `ZonePages/Index.razor.cs`, and `StorageLocationPages/Index.razor.cs`

**Checkpoint**: All four stories work together; list modernization does not regress reference-data management or error conventions.

---

## Phase 7: Polish & Cross-Cutting Validation

**Purpose**: Enforce architectural boundaries, remove obsolete duplicates, and prepare developer-controlled validation.

- [X] T056 [P] Remove obsolete affected module/WebApp DTO declarations and generic list-request imports, then verify all remaining consumers reference `Myrmex.Shared.Wms.Catalog` or `Myrmex.Shared.Wms.Topology` in `Myrmex.Modules.Wms/`, `Myrmex.WebApp/`, and `Myrmex.Tests/`
- [X] T057 [P] Verify shared contracts depend only on BCL/other shared types and contain no domain, EF Core, dispatcher, infrastructure, Blazor, or MudBlazor references in `Myrmex.Shared/Myrmex.Shared.csproj`, `Myrmex.Shared/Wms/Catalog/`, and `Myrmex.Shared/Wms/Topology/`
- [X] T058 Verify route strings, search fields, paging normalization, count-before-paging, deterministic ID ties, backend projection, and the documented deferred Zone selector against `specs/092-server-driven-wms-lists/contracts/catalog-contracts.md`, `specs/092-server-driven-wms-lists/contracts/topology-contracts.md`, and the affected endpoint/handler files under `Myrmex.Modules.Wms/Catalog/` and `Myrmex.Modules.Wms/Topology/`
- [X] T059 Review localized UI usage and ensure any newly introduced Warehouse autocomplete text uses existing keys or matching entries in `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `SharedResource.ru-RU.resx`, and `SharedResource.en-US.resx`

### Developer-Controlled Validation

Do not run these automatically. The developer may execute the focused validation documented in `specs/092-server-driven-wms-lists/quickstart.md`:

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --filter "FullyQualifiedName~ListStockKeepingUnitsHandlerTests|FullyQualifiedName~ListUnitsOfMeasureHandlerTests|FullyQualifiedName~ListWarehousesHandlerTests|FullyQualifiedName~LookupWarehousesHandlerTests|FullyQualifiedName~ListZonesHandlerTests|FullyQualifiedName~ListStorageLocationsHandlerTests|FullyQualifiedName~CatalogListEndpointTests|FullyQualifiedName~TopologyListEndpointTests|FullyQualifiedName~WmsCatalogApiClientTests|FullyQualifiedName~WmsTopologyApiClientTests"
dotnet test Myrmex.Tests/Myrmex.Tests.csproj
dotnet build Myrmex.slnx --no-restore
dotnet run --project Myrmex.AppHost/Myrmex.AppHost.csproj
```

Manual validation covers all five grids, both Warehouse autocompletes, mutations, cancellation, genuine errors, and representative 35,000/50,000-record performance. Do not generate/apply migrations, update databases, or run infrastructure commands for this feature.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: No changes required.
- **Foundational**: Starts immediately and blocks all story implementation/tests.
- **User Story 1**: Starts after T001-T004; no dependency on another story.
- **User Story 2**: Starts after T001-T004; no dependency on User Story 1.
- **User Story 3**: Backend/client tasks start after T003-T004; UI tasks T050-T051 should follow the corresponding User Story 2 page migrations T038 and T041-T042 to avoid conflicting edits.
- **User Story 4**: Depends on completion of User Stories 1-3 because it validates mutation reload and error behavior across their final grids/lookups.
- **Polish**: Depends on all selected stories.

### User Story Dependencies

```text
Foundational T001-T004
|-- US1 Catalog complete-dataset lists
|-- US2 Topology complete-dataset lists
`-- US3 Warehouse lookup backend/client
      `-- US3 page integration after matching US2 page migrations

US1 + US2 + US3 -> US4 mutation/error preservation -> Polish
```

### Within Each Story

- Write the focused tests before their protected implementation.
- Shared DTOs remain separate from internal commands, queries, handlers, and projections.
- Handler order remains filters, count, deterministic sort, paging, projection, materialization.
- Complete client/backend behavior before wiring the corresponding grid.
- Reset page only for filter/context changes; reload current server data for refresh and successful mutations.

### Parallel Opportunities

- T001-T004 create disjoint shared contract files and can run in parallel.
- US1 and US2 backend/client work can proceed in parallel after foundational contracts.
- Within US1, SKU and UoM tests, mappings, sort changes, and grid-request creation can proceed by slice.
- Within US2, Warehouse, Zone, and Storage Location mappings/endpoints can proceed by slice; Zone and Storage Location UI chains are independent until shared client integration.
- US3 handler, endpoint, and client tests can be written in parallel; lookup backend/client work can overlap US1 and non-conflicting US2 work.
- T052 and T053 preserve mutations in disjoint Catalog and Topology page files.

---

## Parallel Example: User Story 1

```text
Test wave: T005 SKU handler | T006 UoM handler | T007 endpoints | T008 client
Backend wave: T009+T011 SKU | T010+T012 UoM
UI preparation: T016 SKU request | T019 UoM request
UI chains: T017 -> T018 and T020 -> T021
```

## Parallel Example: User Story 2

```text
Test wave: T023 Storage handler | T024 endpoints | T025 client
Projection wave: T026 Warehouse | T027 Zone | T028 Storage Location
Endpoint wave: T029 Warehouse | T030 Zone | T031 Storage Location
UI chains: T035 Warehouse | T036 -> T037 -> T038 Zone | T039 -> T040 -> T041 -> T042 Storage Location
```

## Parallel Example: User Story 3

```text
Test wave: T044 handler | T045 endpoint | T046 client
Implementation chain: T047 -> T048 and T047 -> T049
Page integration after US2: T050 Zone | T051 Storage Location
```

## Parallel Example: User Story 4

```text
T052 shared-fixture mutation regression tests first
Then T053 Catalog mutation reload preservation | T054 Topology mutation reload preservation
Then T055 cancellation/error audit
```

---

## Implementation Strategy

### MVP First: User Story 1

1. Complete T001-T004.
2. Complete T005-T022.
3. Stop and request the developer-controlled focused Catalog tests and SKU/UoM smoke scenarios.
4. The MVP fixes the currently demonstrated large-SKU correctness defect and normalizes UoM with the same pattern.

### Incremental Delivery

1. **US1**: Complete Catalog lists and validate independently.
2. **US2**: Complete Topology lists/type-status filtering and validate independently.
3. **US3**: Replace fixed Warehouse selectors and validate bounded search/cancellation.
4. **US4**: Verify mutations, refresh, cancellation, and errors across all final pages.
5. **Polish**: Enforce boundaries/scope and hand off full validation.

## Notes

- `[P]` means different files and no dependency on an unfinished task in the same wave.
- Tests intentionally protect only changed risks; do not duplicate the complete matrix at every layer.
- Existing feature 090 deterministic ID tie-breakers are prerequisites to preserve, not reimplement.
- Keep the Storage Location Zone selector limitation visible as deferred follow-up; do not add Zone autocomplete here.
- Builds, tests, startup, database actions, migrations, and infrastructure commands remain developer-controlled.
