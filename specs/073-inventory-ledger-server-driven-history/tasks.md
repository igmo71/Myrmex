# Tasks: Inventory Ledger Server-Driven History

**Input**: Design documents from `specs/073-inventory-ledger-server-driven-history/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Risk-based automated tests are included only where they protect handler/persistence, endpoint binding/serialization, or API-client URL/deserialization behavior. UI validation remains manual smoke testing through `quickstart.md`; do not add Blazor component-test infrastructure.

**Batches**:

- **Batch 1**: Shared contracts plus backend list/filter/sort.
- **Batch 2**: Transaction details backend/client.
- **Batch 3**: Ledger page orchestration plus filters/grid/dialog and routed navigation.

## Phase 1: Setup and Shared Contracts

**Purpose**: Define public transport contracts that all backend, client, and UI work depends on.

- [x] T001 [P] Create list request contract with paging, sort, SKU, warehouse, storage-location, transaction-type, and occurrence UTC filters in `Myrmex.Shared/Wms/Inventory/ListInventoryLedgerEntriesRequest.cs`
- [x] T002 [P] Create PascalCase sort constants `OccurredAtUtc`, `TransactionType`, `SkuCode`, `SkuName`, `WarehouseCode`, `WarehouseName`, `StorageLocationCode`, `BalanceBefore`, `QuantityDelta`, `BalanceAfter`, and `Reason` in `Myrmex.Shared/Wms/Inventory/InventoryLedgerSortBy.cs`
- [x] T003 [P] Create entry-oriented list-row DTO without unqualified `CreatedAtUtc` in `Myrmex.Shared/Wms/Inventory/InventoryLedgerEntryDetails.cs`
- [x] T004 [P] Create transaction header DTO with `IReadOnlyList<InventoryTransactionEntryDetails>` in `Myrmex.Shared/Wms/Inventory/InventoryTransactionDetails.cs`
- [x] T005 [P] Create separate transaction-detail entry DTO containing only entry-owned values and reference context in `Myrmex.Shared/Wms/Inventory/InventoryTransactionEntryDetails.cs`

**Checkpoint**: Shared contracts compile conceptually and preserve entry-oriented list plus transaction-oriented detail shape.

---

## Phase 2: Foundational Backend Wiring

**Purpose**: Add the Inventory Ledger endpoint surface and reusable test data without changing domain entities, EF mappings, migrations, or indexes.

- [x] T006 [P] Create focused ledger test-data helpers for transactions, entries, inactive references, and multi-entry fixtures in `Myrmex.Tests/Wms/Inventory/Testing/InventoryLedgerTestData.cs`
- [x] T007 Create the `InventoryLedgerEndpoints` endpoint-group shell, route constants, and repository-consistent structure without query-type references in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryLedgerEndpoints.cs`

**Checkpoint**: The endpoint file exists without references to not-yet-created queries; user-story work can add real endpoint mappings when their query types exist.

---

## Phase 3: User Story 1 - Browse Inventory Ledger History (Priority: P1) - MVP / Batch 1

**Goal**: Users can open Inventory Ledger and see a paged, server-driven list where each row represents one `InventoryLedgerEntry` enriched with parent transaction context.

**Independent Test**: With existing adjustment history, the list returns entry rows with occurrence time, transaction type, SKU, warehouse, storage location, before quantity, delta, after quantity, and reason; empty results return an empty list with total count zero.

### Tests for User Story 1

- [x] T008 [P] [US1] Add grouped handler/persistence tests for default newest-first ordering, count-before-paging, empty result, bounded projection, and independence from current balance rows in `Myrmex.Tests/Wms/Inventory/Features/InventoryLedger/InventoryLedgerHandlerTests.cs`
- [x] T009 [P] [US1] Add endpoint test for list query binding and representative nested list JSON serialization in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryLedgerEndpointTests.cs`
- [x] T010 [P] [US1] Add API-client test for empty ledger list URL without trailing `?` and nested list deserialization in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 1

- [x] T011 [US1] Implement `ListInventoryLedgerEntries.Query` and handler sequence through validation, paging normalization, `AsNoTracking`, count, default deterministic sorting, paging, projection, materialization, and `ListResult<InventoryLedgerEntryDetails>` in `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger/ListInventoryLedgerEntries.cs`
- [x] T012 [US1] Implement projection helpers and default deterministic ordering `OccurredAtUtc desc`, `InventoryTransactionId desc`, `InventoryLedgerEntryId desc` in `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger/InventoryLedgerQueryableExtensions.cs`
- [x] T013 [US1] Map GET `/api/wms/inventory/ledger` request values to `ListInventoryLedgerEntries.Query` in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryLedgerEndpoints.cs`
- [x] T014 [US1] Register the ledger endpoint group from the existing inventory route group now that the real list endpoint mapping exists in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs`
- [x] T015 [US1] Add `ListInventoryLedgerEntriesAsync` to build ledger list query strings and deserialize `ListResult<InventoryLedgerEntryDetails>` in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`

**Checkpoint**: The backend/API-client list path is independently usable for unfiltered, paged Ledger history.

---

## Phase 4: User Story 2 - Filter and Sort Ledger History (Priority: P2) - Batch 1

**Goal**: Users can filter by SKU, warehouse, storage location, transaction type, and exact UTC occurrence range while server-side sorting and paging remain deterministic.

**Independent Test**: Applying each supported filter and supported sort returns server-filtered totals and stable page contents, with validation feedback for unsupported transaction type and invalid occurrence ranges.

### Tests for User Story 2

- [x] T016 [P] [US2] Extend grouped handler theories for SKU, warehouse, storage-location, transaction-type, occurrence-range, inactive-reference, and no-match filters in `Myrmex.Tests/Wms/Inventory/Features/InventoryLedger/InventoryLedgerHandlerTests.cs`
- [x] T017 [P] [US2] Extend grouped handler sort theory for every supported PascalCase sort key, requested primary direction, `InventoryTransactionId asc`, and `InventoryLedgerEntryId asc` tie-breakers in `Myrmex.Tests/Wms/Inventory/Features/InventoryLedger/InventoryLedgerHandlerTests.cs`
- [x] T018 [P] [US2] Add focused handler validation tests proving unsupported transaction type and `OccurredFromUtc > OccurredToUtc` fail before filtered query construction while equal boundaries are a valid empty interval in `Myrmex.Tests/Wms/Inventory/Features/InventoryLedger/InventoryLedgerHandlerTests.cs`
- [x] T019 [P] [US2] Add API-client query construction test covering paging, PascalCase sort values, sort direction, all filters, and occurrence UTC parameters in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 2

- [x] T020 [US2] Add request validation before EF query construction for transaction type and occurrence range in `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger/ListInventoryLedgerEntries.cs`
- [x] T021 [US2] Add SKU, warehouse, storage-location, transaction-type, and inclusive-from/exclusive-to occurrence filters in `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger/InventoryLedgerQueryableExtensions.cs`
- [x] T022 [US2] Add requested-sort mapping for all PascalCase `InventoryLedgerSortBy` values with `InventoryTransactionId asc` then `InventoryLedgerEntryId asc` tie-breakers in `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger/InventoryLedgerQueryableExtensions.cs`
- [x] T023 [US2] Pass all filter, sort, and occurrence range values from the endpoint into the internal query in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryLedgerEndpoints.cs`
- [x] T024 [US2] Include all non-empty ledger filter, sort, paging, and occurrence range query parameters in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`

**Checkpoint**: Ledger history can be filtered and sorted server-side with deterministic paging and validation-first request handling.

---

## Phase 5: User Story 3 - Inspect Transaction Details (Priority: P3) - Batch 2

**Goal**: Users can open a list row and inspect the parent transaction header plus all entries belonging to that transaction.

**Independent Test**: Opening details for an adjustment transaction or a multi-entry fixture returns the transaction header once and every entry as `InventoryTransactionEntryDetails`; missing transactions return NotFound.

### Tests for User Story 3

- [x] T025 [P] [US3] Add handler/persistence tests for transaction header, multi-entry detail projection, deterministic entry ordering, and missing transaction NotFound in `Myrmex.Tests/Wms/Inventory/Features/InventoryLedger/InventoryLedgerHandlerTests.cs`
- [x] T026 [P] [US3] Add endpoint test for transaction details route, nested detail JSON, and NotFound mapping in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryLedgerEndpointTests.cs`
- [x] T027 [P] [US3] Add focused API-client tests for details route construction and nested `InventoryTransactionEntryDetails` deserialization in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`
- [x] T028 [P] [US3] Add one representative Ledger API-client cancellation test using the repository's reliable cancellation helper/pattern, verifying the request receives a cancellable token and caller cancellation cancels or is observed by the request without requiring exact token equality in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 3

- [x] T029 [US3] Implement `GetInventoryTransactionById.Query` with transaction header projection and ordered `InventoryTransactionEntryDetails` collection in `Myrmex.Modules.Wms/Inventory/Features/InventoryLedger/GetInventoryTransactionById.cs`
- [x] T030 [US3] Map GET `/api/wms/inventory/transactions/{transactionId:guid}` to the details query in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryLedgerEndpoints.cs`
- [x] T031 [US3] Add `GetInventoryTransactionByIdAsync` to `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [ ] T032 [P] [US3] Create read-only transaction details dialog showing header, reason, timestamps, and all detail entries in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/InventoryTransactionDetailsDialog.razor`

**Checkpoint**: Transaction details are transaction-oriented and support multiple entries without repeating header data per entry.

---

## Phase 6: User Story 4 - Open Filtered History from Inventory Balance (Priority: P4) - Batch 3

**Goal**: Users can navigate from an Inventory Balance row to Ledger with routed filters, and copied routed links hydrate exactly before the first grid request.

**Independent Test**: Balance navigation and copied URLs restore visible SKU, warehouse, and storage-location filter state; valid partial routes work; mismatched warehouse/location routes show clear feedback and block contradictory list requests.

### Tests for User Story 4

- [ ] T033 [P] [US4] Review and align manual smoke coverage with the implemented route hydration and orchestration behavior in `specs/073-inventory-ledger-server-driven-history/quickstart.md`

### Implementation for User Story 4

- [ ] T034 [US4] Create UI-specific grid request record carrying skip, take, PascalCase sort key, and sort direction in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/InventoryLedgerGridRequest.cs`
- [ ] T035 [P] [US4] Create Ledger filter component with inactive-inclusive SKU lookup, inactive-inclusive warehouse selector, warehouse-scoped storage-location lookup, transaction type, occurrence UTC controls, and clear/reset action callbacks in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/InventoryLedgerFilters.razor`
- [ ] T036 [P] [US4] Create Ledger grid with PascalCase sort tags, `MudDataGrid.ServerData`, details action callback, reset/reload methods, and no activation before the parent page allows loading in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/InventoryLedgerGrid.razor`
- [ ] T037 [US4] Create Ledger page route, query parameters, exact-ID hydration state, `_isInitializing` or `_isHydratingFilters` guard, and first-request blocking so `MudDataGrid.ServerData` is not activated before hydration completes in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/Index.razor.cs`
- [ ] T038 [US4] Implement page orchestration that maps filter state and `MudDataGrid` state to `InventoryLedgerGridRequest`, maps it to `ListInventoryLedgerEntriesRequest`, calls `WmsInventoryApiClient.ListInventoryLedgerEntriesAsync`, returns `GridData<InventoryLedgerEntryDetails>`, handles loading state and page-level errors, suppresses expected cancellation errors, resets the grid to page zero when filters change, reloads current grid state when appropriate, loads transaction details through `GetInventoryTransactionByIdAsync`, opens `InventoryTransactionDetailsDialog`, and avoids duplicate or unfiltered requests during routed hydration in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/Index.razor.cs`
- [ ] T039 [US4] Implement partial routed-filter semantics in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/Index.razor.cs`: SKU-only, warehouse-only, warehouse/location match, storage-location-only deriving warehouse, and mismatch route state blocking all Ledger list requests until corrected
- [ ] T040 [US4] Use exact get-by-id hydration through `WmsCatalogApiClient.GetStockKeepingUnitByIdAsync`, `WmsTopologyApiClient.GetWarehouseByIdAsync`, and `WmsTopologyApiClient.GetStorageLocationByIdAsync` in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/Index.razor.cs`
- [ ] T041 [US4] Create Ledger page markup that connects filters, grid, details dialog action flow, page-level validation/error feedback, and delayed grid activation while routed hydration or mismatch feedback is active in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryLedgerPages/Index.razor`
- [ ] T042 [US4] Add Inventory Ledger navigation link beside Inventory Balances in `Myrmex.WebApp/Components/Layout/NavMenu.razor`
- [ ] T043 [US4] Add History action to balance rows in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor`
- [ ] T044 [US4] Navigate from balance history action to `/wms/inventory/ledger?stockKeepingUnitId={Sku.Id}&warehouseId={StorageLocation.Warehouse.Id}&storageLocationId={StorageLocation.Id}` in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`

**Checkpoint**: The Ledger page is linkable, route-hydrated, protected from contradictory route state, and reachable from Inventory Balance.

---

## Phase 7: Polish and Developer-Controlled Validation

**Purpose**: Final consistency checks and validation instructions without running developer-controlled commands automatically.

- [ ] T045 Review `specs/073-inventory-ledger-server-driven-history/quickstart.md` against implemented behavior and keep build, test, app startup, EF, runtime validation, and smoke testing developer-controlled
- [ ] T046 Verify scope guardrails by inspecting changed paths under `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/`, `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/`, `Myrmex.Modules.Wms/Inventory/Domain/`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/`, and `Myrmex.Tests/Wms/Inventory/`

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1** must complete before backend, endpoint, client, or UI tasks use shared contracts.
- **Phase 2** creates only endpoint shell structure and shared fixtures; real endpoint mappings are added in the user-story phases only after their query types exist.
- **US1** can start after Phases 1 and 2 and is the MVP backend list increment.
- **US2** depends on US1 list handler/client structure.
- **US3** depends on shared transaction detail contracts from Phase 1 and endpoint group shell from Phase 2; its endpoint mapping is added with the real details query task.
- **US4** depends on list client behavior from US1/US2 and details client/dialog behavior from US3. Filters, grid, details dialog, route hydration, and page orchestration must be connected before the page checkpoint is considered complete.
- **Polish** depends on all selected implementation batches.

### Proposed Implementation Batches

1. **Batch 1 - Shared contracts plus backend list/filter/sort**: T001-T024.
2. **Batch 2 - Transaction details backend/client**: T025-T032.
3. **Batch 3 - Ledger page orchestration plus filters/grid/dialog and routed navigation**: T033-T044.

### Parallel Opportunities

- T001-T005 can run in parallel because they create separate shared contract files.
- T006 can run in parallel with T007 once route names are agreed.
- T008-T010 can run in parallel because they touch handler, endpoint, and client test files.
- T016-T019 can run in parallel because they extend grouped tests at distinct boundaries.
- T025-T028 can run in parallel because they protect handler, endpoint, client details, and representative cancellation risks.
- T035-T036 can run in parallel after T034 establishes the grid request shape.
- T037, T038, T039, and T040 all touch `Index.razor.cs` and should be sequenced together rather than parallelized.
- T042-T043 can run in parallel with Ledger page work, then T044 wires the balance navigation action.

---

## Risk-Based Test Groups

- **Handler/persistence list group**: filters, count-before-paging, default sort, requested sort tie-breakers, validation-before-query, equal occurrence boundaries, inactive references, required nested fields, empty results, and current-balance independence in `Myrmex.Tests/Wms/Inventory/Features/InventoryLedger/InventoryLedgerHandlerTests.cs`.
- **Handler/persistence details group**: multi-entry transaction details, header once, detail entry shape, deterministic entry ordering, and missing transaction NotFound in `Myrmex.Tests/Wms/Inventory/Features/InventoryLedger/InventoryLedgerHandlerTests.cs`.
- **Endpoint boundary group**: `[AsParameters]` list binding, representative list JSON, details route JSON, and NotFound mapping in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryLedgerEndpointTests.cs`.
- **API-client group**: ledger list URL construction, no trailing `?`, all query parameters, details route, one representative cancellable request behavior test without exact token equality, and nested DTO deserialization in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`.
- **Manual UI smoke group**: route hydration variants, mismatch blocking, no initial unfiltered request, copied-link reload, inactive references, details dialog, and no mutation controls in `specs/073-inventory-ledger-server-driven-history/quickstart.md`.

---

## Scope Guardrails

- Do not create migrations, indexes, EF mappings, or domain-model changes.
- Do not add Blazor component-test infrastructure.
- Do not introduce generic lookup, routing, state-management, grid, reporting, export, analytics, or observability frameworks.
- Do not add ledger create, update, delete, correction, reversal, rebuild, transfer, or InventoryAccount behavior.
- Treat bounded projection and absence of `Include`-heavy graph loading as implementation/code-review requirements; do not add LINQ expression-tree, reflection, member-absence, or `Include`-absence structural tests.
- Keep build, tests, application startup, EF, formatter, linter, runtime validation, and smoke testing developer-controlled.
