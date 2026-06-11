# Tasks: Inventory Balance MVP Vertical Slice

**Input**: Design documents from `specs/048-add-inventory-balance-mvp-vertical-slice/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/inventory-balance-api-contract.md`, `quickstart.md`

**Tests**: Required before implementation for changed domain rules, command/query handlers, persistence mappings, and WebApp API client contracts. Endpoint/UI automation remains deferred per `plan.md`; lower-level automated coverage and manual validation tasks are included instead.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently after shared foundation tasks.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it edits different files and does not depend on incomplete tasks in the same phase.
- **[Story]**: Maps to a user story from `spec.md`.
- Every task includes an exact repository path or feature artifact path.

## Phase 1: Setup (Shared Context)

**Purpose**: Prepare the Inventory Balance folder structure and review existing WMS patterns before behavior changes.

- [X] T001 Create Inventory source and test folders in `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances`, `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances`, `Myrmex.Modules.Wms/Inventory/Endpoints`, `Myrmex.WebApp/Wms/Inventory`, `Myrmex.Tests/Wms/Inventory/Domain`, `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances`, `Myrmex.Tests/Wms/Inventory/Persistence`, and `Myrmex.Tests/Wms/Inventory/Client`
- [X] T002 [P] Review existing Catalog SKU and UoM reference patterns in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`, `Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs`, and `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnit.cs`
- [X] T003 [P] Review existing Topology storage location eligibility patterns in `Myrmex.Modules.Wms/Topology/Domain/StorageLocations/StorageLocation.cs`, `Myrmex.Modules.Wms/Topology/Features/StorageLocations/CreateStorageLocation.cs`, and `Myrmex.Modules.Wms/Topology/Features/StorageLocations/ListStorageLocations.cs`
- [X] T004 [P] Review existing WMS endpoint, client, persistence, and test infrastructure in `Myrmex.Modules.Wms/WmsModule.cs`, `Myrmex.WebApp/Wms/Topology/WmsTopologyApiClient.cs`, `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`, and `Myrmex.Tests/Wms/Topology/Testing/TestWmsDbContext.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the shared Inventory Balance domain, persistence, error, endpoint group, and client foundation required by all user stories.

**Critical**: No user story implementation should begin until this phase is complete.

### Tests for Foundation

- [X] T005 [P] Add Inventory Balance domain tests for required SKU identity, required storage location identity, non-negative quantity, zero quantity, no activation lifecycle, and quantity-only state transition in `Myrmex.Tests/Wms/Inventory/Domain/InventoryBalanceTests.cs`
- [X] T006 [P] Add Inventory Balance persistence mapping tests for required FK metadata, unique SKU/location index, decimal quantity configuration, timestamp mapping, and absence of warehouse/UoM columns in `Myrmex.Tests/Wms/Inventory/Persistence/InventoryBalancePersistenceTests.cs`

### Implementation for Foundation

- [X] T007 [P] Implement `InventoryBalance` aggregate with create factory, quantity update method, SKU/location identity, quantity, timestamps, and no `IActivatable` behavior in `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs`
- [X] T008 [P] Add Inventory Balance domain events for created and quantity-updated changes only in `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalanceEvents.cs`
- [X] T009 [P] Add Inventory Balance validation errors for required identities and non-negative quantity in `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalanceValidationErrors.cs`
- [X] T010 Add Inventory Balance service errors for not found, duplicate SKU/location pair, create failed, update failed, invalid SKU, invalid storage location, inactive storage location type, and inactive storage location status in `Myrmex.Modules.Wms/WmsErrors.cs`
- [X] T011 Add `InventoryBalances` DbSet to the WMS context in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`
- [X] T012 Add Inventory Balance table, primary key, foreign key, unique index, and supporting index constants in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- [X] T013 Configure `InventoryBalance` EF mapping with required SKU/storage location FKs, restrict delete behavior, explicit decimal quantity precision, timestamps, and unique `(StockKeepingUnitId, StorageLocationId)` index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryBalanceConfiguration.cs`
- [X] T014 Map Inventory Balance unique index persistence failures to duplicate-balance service errors in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsPersistenceExceptionMapper.cs`
- [X] T015 Add Inventory endpoint group registration scaffolding in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs` and register it from `Myrmex.Modules.Wms/WmsModule.cs`

**Checkpoint**: Foundation ready; Inventory Balance has a persisted aggregate shape, shared errors, endpoint group, and no out-of-scope movement, lifecycle, delete, conversion, seed, integration, or UI behavior.

---

## Phase 3: User Story 1 - Record Current Stock at a Location (Priority: P1) MVP

**Goal**: A warehouse operations user or upstream workflow records the current known on-hand quantity for one active SKU at one eligible storage location.

**Independent Test**: Create one balance for an existing active SKU with a base UoM and an eligible storage location, confirm quantity is stored in the SKU base unit, reject duplicate SKU/location pairs, and reject negative quantity or invalid references.

### Tests for User Story 1

- [X] T016 [P] [US1] Add create handler tests for valid create, duplicate SKU/location rejection, negative quantity rejection, zero quantity create, missing SKU, inactive SKU, SKU without base UoM, missing storage location, inactive storage location, inactive storage location type/status, and `IsPickable=false` eligibility in `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/CreateInventoryBalanceHandlerTests.cs`
- [X] T017 [P] [US1] Add WebApp Inventory API client create tests for `CreateInventoryBalanceRequest`, success response parsing, validation result, missing-reference result, and duplicate result behavior in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 1

- [X] T018 [US1] Implement `InventoryBalanceDetails` projection with balance, SKU, storage location, warehouse, base UoM, quantity, and timestamp fields in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceDetails.cs`
- [X] T019 [US1] Implement `CreateInventoryBalance` command and handler with domain validation, active SKU/base UoM check, eligible storage location/type/status check, duplicate check, persistence save, and existing result conventions in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/CreateInventoryBalance.cs`
- [X] T020 [US1] Implement create route `POST /api/wms/inventory/balances` with request binding and command dispatch in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs`
- [X] T021 [US1] Register Inventory Balance endpoints in the Inventory route group in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs`
- [X] T022 [US1] Add `WmsInventoryApiClient`, `InventoryBalanceDetails`, `CreateInventoryBalanceRequest`, and `TryCreateInventoryBalanceAsync` in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs` without adding WebApp UI pages

**Checkpoint**: User Story 1 is independently functional and testable as the MVP create flow.

---

## Phase 4: User Story 2 - View Inventory Balance Details (Priority: P2)

**Goal**: A warehouse operations user retrieves a specific inventory balance and sees enough SKU, storage location, warehouse, base UoM, quantity, and timestamp context.

**Independent Test**: Retrieve an existing balance by identifier and confirm the response includes the balance, SKU, storage location, warehouse, base UoM, quantity, created timestamp, and last updated timestamp; retrieve a nonexistent id and confirm not-found behavior.

### Tests for User Story 2

- [X] T023 [P] [US2] Add get-by-id handler tests for existing balance display context, zero quantity display, updated timestamp display, and missing balance not-found behavior in `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/GetInventoryBalanceByIdHandlerTests.cs`
- [X] T024 [P] [US2] Extend WebApp Inventory API client tests for get-by-id success parsing and read/load exception behavior in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 2

- [X] T025 [US2] Implement `GetInventoryBalanceById` query and handler returning active or inactive referenced context through projection joins and Inventory Balance not-found errors in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/GetInventoryBalanceById.cs`
- [X] T026 [US2] Add get route `GET /api/wms/inventory/balances/{inventoryBalanceId:guid}` to `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs`
- [X] T027 [US2] Add `GetInventoryBalanceByIdAsync` to the WebApp Inventory API client in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`

**Checkpoint**: User Stories 1 and 2 work independently; users can create and retrieve one balance with display context.

---

## Phase 5: User Story 3 - Find Balances by Warehouse, Location, or SKU (Priority: P3)

**Goal**: A warehouse operations user lists inventory balances and narrows the list by SKU, storage location, warehouse, or SKU within a warehouse.

**Independent Test**: Create balances across multiple SKUs, storage locations, and warehouses; verify no filters returns available balances including zero balances, and each supported filter returns only matching balances with display context.

### Tests for User Story 3

- [X] T028 [P] [US3] Add list handler tests for bounded no-filter results, zero quantity inclusion, SKU filter, storage location filter, warehouse filter, SKU-within-warehouse filter, empty result behavior, supported sorting, and display context in `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/ListInventoryBalancesHandlerTests.cs`
- [X] T029 [P] [US3] Extend WebApp Inventory API client tests for list query string generation, optional filters, bounded list response parsing, and read/load exception behavior in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 3

- [X] T030 [US3] Implement `ListInventoryBalances` query and handler with bounded paging, optional `StockKeepingUnitId`, `StorageLocationId`, and `WarehouseId` filters, zero quantity inclusion, supported sorting, and display context projection in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/ListInventoryBalances.cs`
- [X] T031 [US3] Add list route `GET /api/wms/inventory/balances` with optional filter binding to `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs`
- [X] T032 [US3] Add `ListInventoryBalancesRequest` and `ListInventoryBalancesAsync` to the WebApp Inventory API client in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`

**Checkpoint**: User Stories 1 through 3 work independently; stock visibility questions can be answered through the list endpoint.

---

## Phase 6: User Story 4 - Update Current Quantity Only (Priority: P4)

**Goal**: A warehouse operations user updates only the current known quantity of an existing balance without changing SKU or storage location.

**Independent Test**: Update an existing balance quantity from `10` to `5`, confirm quantity and updated timestamp changed, confirm SKU and storage location stayed unchanged, confirm zero quantity is accepted, and confirm negative quantity and missing balance fail.

### Tests for User Story 4

- [X] T033 [P] [US4] Add update handler tests for valid quantity update, zero quantity update, missing balance not-found, negative quantity rejection, unchanged SKU/location identity, and updated timestamp behavior in `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/UpdateInventoryBalanceQuantityHandlerTests.cs`
- [X] T034 [P] [US4] Extend WebApp Inventory API client tests for quantity-only update payload, success response parsing, validation result, and not-found result behavior in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 4

- [X] T035 [US4] Implement `UpdateInventoryBalanceQuantity` command and handler with quantity-only input, missing balance handling, non-negative quantity validation, unchanged SKU/location identity, timestamp update, and existing result conventions in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/UpdateInventoryBalanceQuantity.cs`
- [X] T036 [US4] Add quantity update route `PUT /api/wms/inventory/balances/{inventoryBalanceId:guid}/quantity` with request body accepting only `Quantity` in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs`
- [X] T037 [US4] Add `UpdateInventoryBalanceQuantityRequest` and `TryUpdateInventoryBalanceQuantityAsync` to the WebApp Inventory API client in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`

**Checkpoint**: All user stories are independently functional through domain, handler, persistence, endpoint, and client paths; WebApp UI remains out of scope.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Regression, migration handoff, and scope validation across the completed slice.

- [X] T038 [P] Add shared Inventory Balance test data helpers only where they reduce duplication across Inventory tests in `Myrmex.Tests/Wms/Inventory/Testing/InventoryBalanceTestData.cs`
- [X] T039 [P] Review Inventory Balance implementation against API contract requirements in `specs/048-add-inventory-balance-mvp-vertical-slice/contracts/inventory-balance-api-contract.md`
- [X] T040 [P] Review Inventory Balance implementation against data model requirements in `specs/048-add-inventory-balance-mvp-vertical-slice/data-model.md`
- [X] T041 Verify final code diff adds no receiving, putaway, picking, shipping, LPN, reservation, transaction, movement, adjustment, batch/lot, expiry, serial number, conversion, packaging, cycle counting, seed/demo, external integration, WebApp UI, delete, deactivate, reactivate, or zero-balance cleanup behavior across `Myrmex.Modules.Wms/`, `Myrmex.WebApp/`, and `Myrmex.Tests/`
- [X] T042 Stop before EF migration generation and recommend the developer-controlled migration commands documented in `specs/048-add-inventory-balance-mvp-vertical-slice/quickstart.md`
- [X] T043 Recommend developer-controlled validation commands from `specs/048-add-inventory-balance-mvp-vertical-slice/quickstart.md` without running build, tests, app startup, EF migration generation, database update, or migration application automatically

---

## Developer-Controlled Commands

These commands are recommended validation or migration commands for the developer to run manually. Codex must not run them automatically.

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~InventoryBalance|FullyQualifiedName~InventoryBalances|FullyQualifiedName~WmsInventoryApiClient" -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
dotnet ef migrations add AddInventoryBalance --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext --output-dir Infrastructure\Persistence\Migrations
dotnet ef database update --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext
```

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundation; delivers the MVP create flow and shared details/client shape.
- **User Story 2 (Phase 4)**: Depends on US1 details projection and seeded/created balance records.
- **User Story 3 (Phase 5)**: Depends on US1 aggregate, persistence, details projection, and created balance records.
- **User Story 4 (Phase 6)**: Depends on US1 aggregate and persistence; may proceed after create exists.
- **Polish (Phase 7)**: Depends on all implemented user stories.

### User Story Dependencies

- **US1 Record Current Stock at a Location**: MVP. Requires Foundation only.
- **US2 View Inventory Balance Details**: Requires created balance records and `InventoryBalanceDetails`.
- **US3 Find Balances by Warehouse, Location, or SKU**: Requires created balance records across SKUs/locations/warehouses and `InventoryBalanceDetails`.
- **US4 Update Current Quantity Only**: Requires existing balance records and the aggregate quantity update behavior.

### Within Each User Story

- Required tests must be written before implementation tasks in that story.
- Domain and persistence model tasks precede handlers.
- Handlers precede endpoint routes.
- Endpoint routes precede API client methods.
- Manual endpoint/UI validation remains a developer-controlled quickstart activity, not an automated task.
- Migration generation and database update remain developer-controlled and must not be run automatically.

---

## Parallel Opportunities

- Setup review tasks T002, T003, and T004 can run in parallel.
- Foundation test tasks T005 and T006 can run in parallel.
- Foundation implementation tasks T007, T008, and T009 can run in parallel before shared wiring tasks T010 through T015.
- US1 test tasks T016 and T017 can run in parallel after Foundation.
- US2 test tasks T023 and T024 can run in parallel after US1.
- US3 test tasks T028 and T029 can run in parallel after US1.
- US4 test tasks T033 and T034 can run in parallel after US1.
- Polish review tasks T039 and T040 can run in parallel after implementation.

---

## Parallel Example: User Story 1

```text
Task: "T016 Add create handler tests in Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/CreateInventoryBalanceHandlerTests.cs"
Task: "T017 Add WebApp Inventory API client create tests in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "T023 Add get-by-id handler tests in Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/GetInventoryBalanceByIdHandlerTests.cs"
Task: "T024 Extend WebApp Inventory API client get-by-id tests in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "T028 Add list handler tests in Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/ListInventoryBalancesHandlerTests.cs"
Task: "T029 Extend WebApp Inventory API client list tests in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs"
```

## Parallel Example: User Story 4

```text
Task: "T033 Add update handler tests in Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/UpdateInventoryBalanceQuantityHandlerTests.cs"
Task: "T034 Extend WebApp Inventory API client quantity update tests in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundation.
3. Complete Phase 3 User Story 1 tests and implementation.
4. Stop and validate User Story 1 independently through focused tests and the manual quickstart scenario.
5. Do not generate or apply migrations automatically; recommend the developer-controlled commands in `quickstart.md`.

### Incremental Delivery

1. Add foundation and US1 to support recording current stock.
2. Add US2 to support get-by-id review.
3. Add US3 to support stock visibility lists and filters.
4. Add US4 to support quantity-only updates.
5. Complete polish/regression tasks and recommend developer-controlled validation commands.

### Parallel Team Strategy

1. One developer handles foundation/domain/persistence tests while another prepares endpoint/client scaffolding.
2. After US1, US2, US3, and US4 test tasks can be assigned separately by file ownership.
3. Coordinate edits to `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs` and `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs` because multiple stories extend those files.

---

## Notes

- `[P]` tasks are limited to different files or independent review tasks.
- `[US1]`, `[US2]`, `[US3]`, and `[US4]` labels map directly to the user stories in `spec.md`.
- Migration generation and database update are intentionally absent as executable implementation tasks; T042 requires stopping and recommending the documented developer-controlled commands.
- Avoid adding receiving, putaway, picking, shipping, LPN, reservations, transaction history, movement history, adjustment documents, batch/lot, expiry, serial numbers, UoM conversion, packaging, cycle counting, seed/demo data, external integrations, WebApp UI, delete behavior, activation/deactivation behavior, new frameworks, MediatR, or broad refactoring.
