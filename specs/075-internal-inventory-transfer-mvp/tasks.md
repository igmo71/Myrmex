# Tasks: Internal Inventory Transfer MVP

**Input**: Design documents from `specs/075-internal-inventory-transfer-mvp/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Automated tests are included because the feature changes domain invariants, command behavior, persistence mappings, public endpoint/client contracts, and server-driven list behavior. Each test task names the protected regression risk and the lowest owning layer.

**Implementation Constraint**: Use `InventoryLedgerEntry` as the implementation entity name. Do not introduce an `InventoryLedgerImpact` type.

## Phase 1: Setup

**Purpose**: Create feature folders and shared test scaffolding without changing runtime behavior.

- [X] T001 Create Inventory Transfer shared contract files with empty records/placeholders in `Myrmex.Shared/Wms/Inventory/CreateInventoryTransferRequest.cs`, `Myrmex.Shared/Wms/Inventory/CreateInventoryTransferLineRequest.cs`, `Myrmex.Shared/Wms/Inventory/MoveInventoryTransferLineRequest.cs`, `Myrmex.Shared/Wms/Inventory/PickInventoryTransferLineRequest.cs`, `Myrmex.Shared/Wms/Inventory/PlaceInventoryTransferLineRequest.cs`, `Myrmex.Shared/Wms/Inventory/ListInventoryTransfersRequest.cs`, `Myrmex.Shared/Wms/Inventory/InventoryTransferSortBy.cs`, `Myrmex.Shared/Wms/Inventory/InventoryTransferStatusDetails.cs`, `Myrmex.Shared/Wms/Inventory/InventoryTransferListItem.cs`, `Myrmex.Shared/Wms/Inventory/InventoryTransferDetails.cs`, `Myrmex.Shared/Wms/Inventory/InventoryTransferLineDetails.cs`, and `Myrmex.Shared/Wms/Inventory/InventoryTransferMovementDetails.cs`
- [X] T002 Create Inventory Transfer backend folders and placeholder files in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransfers/`, `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/`, and `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryTransferEndpoints.cs`
- [X] T003 Create Inventory Transfer WebApp folders and placeholder files in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/Index.razor`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/Index.razor.cs`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferFilters.razor`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferGrid.razor`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferGridRequest.cs`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferDetailsDialog.razor`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/CreateInventoryTransferDialog.razor`, and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferMovementDialog.razor`
- [X] T004 Create Inventory Transfer test scaffolding files in `Myrmex.Tests/Wms/Inventory/Testing/InventoryTransferTestData.cs`, `Myrmex.Tests/Wms/Inventory/Domain/InventoryTransferTests.cs`, `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`, `Myrmex.Tests/Wms/Inventory/Persistence/InventoryTransferPersistenceTests.cs`, and `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryTransferEndpointTests.cs`

---

## Phase 2: Foundational

**Purpose**: Shared domain, persistence, and reference-data prerequisites required before user story implementation.

**Critical**: No user story implementation can be completed until this phase is done.

### Tests

- [X] T005 [P] Add persistence test for storage-location type seed regression risk covering `InternalTransit` and `ExternalTransit` in `Myrmex.Tests/Wms/Inventory/Persistence/InventoryTransferPersistenceTests.cs`
- [X] T006 [P] Add domain test for nullable transit and no persisted execution-mode regression risk in `Myrmex.Tests/Wms/Inventory/Domain/InventoryTransferTests.cs`
- [X] T007 [P] Add domain test for movement fact fields and no persisted movement-type/scanner-state regression risk in `Myrmex.Tests/Wms/Inventory/Domain/InventoryTransferTests.cs`
- [X] T008 [P] Add domain test for transfer transaction factory creating exactly two `InventoryLedgerEntry` rows with `InventoryTransactionType.Transfer` in `Myrmex.Tests/Wms/Inventory/Domain/InventoryTransactionTests.cs`

### Implementation

- [X] T009 Add `Transfer = 2` to `InventoryTransactionType` in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransactionType.cs`
- [X] T010 Add a transfer factory that creates one transfer transaction with exactly two `InventoryLedgerEntry` entities in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs`
- [X] T011 Implement `InventoryTransferStatus` in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransfers/InventoryTransferStatus.cs`
- [X] T012 Implement `InventoryTransferLine` with requested quantity and computed progress helpers in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransfers/InventoryTransferLine.cs`
- [X] T013 Implement immutable `InventoryTransferMovement` with `InventoryTransactionId` in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransfers/InventoryTransferMovement.cs`
- [X] T014 Implement `InventoryTransfer` aggregate with nullable `TransitStorageLocationId`, line collection, movement collection, and status behavior in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransfers/InventoryTransfer.cs`
- [X] T015 Extend storage-location type seed data with `INTERNAL_TRANSIT` and `EXTERNAL_TRANSIT` in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StorageLocationTypeConfiguration.cs`
- [X] T016 Add storage-location type seed identifiers for internal and external transit in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsSeedIds.cs`
- [X] T017 Add transfer table, key, foreign-key, and index names in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- [X] T018 Implement transfer EF mapping in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryTransferConfiguration.cs`
- [X] T019 Implement transfer line EF mapping in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryTransferLineConfiguration.cs`
- [X] T020 Implement transfer movement EF mapping in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryTransferMovementConfiguration.cs`
- [X] T021 Add transfer DbSets to `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`
- [X] T022 Create EF migration for transfer tables and transit storage-location type seed changes in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/<timestamp>_AddInventoryTransfers.cs`

**Checkpoint**: Transfer entities, reference data, and persistence mappings are ready for user story slices.

---

## Phase 3: User Story 1 - Create Internal Transfer Document (Priority: P1)

**Goal**: Supervisors can create direct or internal-transit transfer documents with one or more lines.

**Independent Test**: Create transfers with and without transit location and verify header, lines, status `Created`, nullable transit behavior, and allowed movement pattern.

### Tests

- [X] T023 [P] [US1] Add handler test for same-warehouse create-transfer success and external-transfer rejection in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [X] T024 [P] [US1] Add handler test for active SKU, regular source/destination locations, internal transit location, and positive quantity validation in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [X] T025 [P] [US1] Add endpoint test for create-transfer route, body binding, and representative ProblemDetails behavior in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryTransferEndpointTests.cs`
- [X] T026 [P] [US1] Add API-client test for create-transfer request body and write-result mapping in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation

- [X] T027 [US1] Implement create-transfer shared request and transfer details contracts in `Myrmex.Shared/Wms/Inventory/CreateInventoryTransferRequest.cs`, `Myrmex.Shared/Wms/Inventory/CreateInventoryTransferLineRequest.cs`, `Myrmex.Shared/Wms/Inventory/InventoryTransferStatusDetails.cs`, `Myrmex.Shared/Wms/Inventory/InventoryTransferDetails.cs`, `Myrmex.Shared/Wms/Inventory/InventoryTransferLineDetails.cs`, and `Myrmex.Shared/Wms/Inventory/InventoryTransferMovementDetails.cs`
- [X] T028 [US1] Implement create-transfer command, validation, persistence, and details reload in `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/CreateInventoryTransfer.cs`
- [X] T029 [US1] Implement transfer details projection helpers for created transfer responses in `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/InventoryTransferQueryableExtensions.cs`
- [X] T030 [US1] Implement create-transfer endpoint mapping in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryTransferEndpoints.cs`
- [X] T031 [US1] Register transfer endpoints in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs`
- [X] T032 [US1] Add create-transfer API-client method in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T033 [US1] Implement create-transfer dialog with multiple line editing in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/CreateInventoryTransferDialog.razor`
- [X] T034 [US1] Wire create-transfer dialog into the transfer page state in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/Index.razor.cs`

**Checkpoint**: User Story 1 is independently functional and ready for validation before movement execution is added.

---

## Phase 4: User Story 2 - Execute Direct Internal Movement (Priority: P1)

**Goal**: Operators can commit direct source-to-destination movements for transfers without a transit location.

**Independent Test**: Create a direct transfer, commit a partial direct movement, and verify one movement, one transfer transaction, two `InventoryLedgerEntry` rows, balance changes, progress quantities, and status.

### Tests

- [X] T035 [P] [US2] Add handler test for direct movement creating movement, one transfer transaction, two `InventoryLedgerEntry` rows, balance changes, and progress updates in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [X] T036 [P] [US2] Add handler test for direct over-move, insufficient source balance, and wrong pick/place operation rejection on direct transfer in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [X] T037 [P] [US2] Add endpoint test for direct move route, body binding, and representative conflict ProblemDetails behavior in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryTransferEndpointTests.cs`
- [X] T038 [P] [US2] Add API-client test for direct move request body and refreshed transfer details deserialization in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation

- [X] T039 [US2] Implement direct move shared request contract in `Myrmex.Shared/Wms/Inventory/MoveInventoryTransferLineRequest.cs`
- [X] T040 [US2] Implement direct movement command with balance updates, transfer transaction creation, two `InventoryLedgerEntry` entities, and movement persistence in `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/MoveInventoryTransferLine.cs`
- [X] T041 [US2] Implement direct move endpoint mapping in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryTransferEndpoints.cs`
- [X] T042 [US2] Add direct move API-client method in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T043 [US2] Add direct move mode to movement dialog in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferMovementDialog.razor`
- [X] T044 [US2] Wire direct move action visibility and refresh behavior into transfer details UI in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferDetailsDialog.razor`

**US2 implementation note**: This direct-movement slice rejects direct movement on transit transfers. Pick/place command behavior remains deferred to US3.

**Checkpoint**: User Story 2 direct movement works independently for direct transfers.

---

## Phase 5: User Story 3 - Execute Transfer Through Internal Transit (Priority: P1)

**Goal**: Operators can pick inventory from source to internal transit and place it from internal transit to destination.

**Independent Test**: Create a transit transfer, commit pick and place quantities, and verify movement history, one transaction and two `InventoryLedgerEntry` rows per movement, balance changes, in-transit quantities, and status.

### Tests

- [X] T045 [P] [US3] Add handler test for pick creating movement, transfer transaction, two `InventoryLedgerEntry` rows, source/transit balance changes, and picked/in-transit progress in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [X] T046 [P] [US3] Add handler test for place creating movement, transfer transaction, two `InventoryLedgerEntry` rows, transit/destination balance changes, and placed/in-transit progress in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [X] T047 [P] [US3] Add handler test for over-pick, over-place, insufficient source balance, and wrong direct move operation rejection on transit transfer in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [X] T048 [P] [US3] Add API-client test for pick/place request bodies and refreshed transfer details deserialization in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation

- [X] T049 [US3] Implement pick/place shared request contracts in `Myrmex.Shared/Wms/Inventory/PickInventoryTransferLineRequest.cs` and `Myrmex.Shared/Wms/Inventory/PlaceInventoryTransferLineRequest.cs`
- [X] T050 [US3] Implement pick command with balance updates, transfer transaction creation, two `InventoryLedgerEntry` entities, and movement persistence in `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/PickInventoryTransferLine.cs`
- [X] T051 [US3] Implement place command with balance updates, transfer transaction creation, two `InventoryLedgerEntry` entities, and movement persistence in `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/PlaceInventoryTransferLine.cs`
- [X] T052 [US3] Implement pick and place endpoint mappings in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryTransferEndpoints.cs`
- [X] T053 [US3] Add pick and place API-client methods in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T054 [US3] Add pick and place modes to movement dialog in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferMovementDialog.razor`
- [X] T055 [US3] Wire pick/place action visibility and refresh behavior into transfer details UI in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferDetailsDialog.razor`

**Checkpoint**: User Story 3 transit movement works independently for transit transfers.

---

## Phase 6: User Story 4 - Monitor Transfer Progress and History (Priority: P2)

**Goal**: Supervisors can list transfers, open details, inspect computed progress, and review read-only movement history.

**Independent Test**: Create transfers in different states, commit movements, list with filters, open details, and verify progress, status, and read-only movement history.

### Tests

- [X] T056 [P] [US4] Add handler/query test for transfer details progress formulas and read-only movement history projection in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [X] T057 [P] [US4] Add handler/query test for server-driven transfer list filters, count-before-paging, deterministic sorting, and aggregate quantities in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [X] T058 [P] [US4] Add endpoint test for list query binding and transfer details route serialization in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryTransferEndpointTests.cs`
- [X] T059 [P] [US4] Add API-client test for transfer list URL construction, details route, cancellation, and deserialization in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation

- [X] T060 [US4] Implement list/detail shared contracts and sort keys in `Myrmex.Shared/Wms/Inventory/ListInventoryTransfersRequest.cs`, `Myrmex.Shared/Wms/Inventory/InventoryTransferSortBy.cs`, and `Myrmex.Shared/Wms/Inventory/InventoryTransferListItem.cs`
- [X] T061 [US4] Implement get-transfer details query in `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/GetInventoryTransferById.cs`
- [X] T062 [US4] Implement list-transfers query with filters, count-before-paging, deterministic sorting, and bounded projection in `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/ListInventoryTransfers.cs`
- [X] T063 [US4] Complete projection helpers for list items, line progress, movement meaning, and movement history in `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/InventoryTransferQueryableExtensions.cs`
- [X] T064 [US4] Implement list and details endpoint mappings in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryTransferEndpoints.cs`
- [X] T065 [US4] Add list/details API-client methods and URL builder in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T066 [US4] Implement transfer page and grid request mapping in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/Index.razor`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/Index.razor.cs`, and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferGridRequest.cs`
- [X] T067 [US4] Implement transfer filters and grid in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferFilters.razor` and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferGrid.razor`
- [X] T068 [US4] Implement transfer details dialog with read-only movement history in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferDetailsDialog.razor`
- [X] T069 [US4] Add Inventory Transfers navigation item in `Myrmex.WebApp/Components/Layout/NavMenu.razor`

**Checkpoint**: User Story 4 monitoring is independently usable after movement data exists.

---

## Phase 7: User Story 5 - Complete Transfer Automatically (Priority: P2)

**Goal**: Transfers automatically become completed after all requested quantities are placed and reject further movement.

**Independent Test**: Complete all lines through direct or transit movement and verify automatic status transition, hidden UI actions, and backend rejection of additional movement.

### Tests

- [ ] T070 [P] [US5] Add domain/handler test for direct transfer completion after final movement in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [ ] T071 [P] [US5] Add domain/handler test for transit transfer completion only when placed equals requested and in-transit is zero in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`
- [ ] T072 [P] [US5] Add handler test for completed transfer rejecting move, pick, and place without changing balances or movement history in `Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs`

### Implementation

- [ ] T073 [US5] Finalize completion status calculation and read-only guard in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransfers/InventoryTransfer.cs`
- [ ] T074 [US5] Apply completion status update after direct move, pick, and place commands in `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/MoveInventoryTransferLine.cs`, `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/PickInventoryTransferLine.cs`, and `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/PlaceInventoryTransferLine.cs`
- [ ] T075 [US5] Hide movement actions for completed transfers in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/InventoryTransferDetailsDialog.razor`

**Checkpoint**: User Story 5 completion behavior protects finished transfers.

---

## Final Phase: Polish and Cross-Cutting

**Purpose**: Final consistency, validation guidance, and cleanup across all stories.

- [ ] T076 [P] Update quickstart validation notes with any final route or UI naming changes in `specs/075-internal-inventory-transfer-mvp/quickstart.md`
- [ ] T077 [P] Review generated code for forbidden types and fields, especially absence of `InventoryLedgerImpact`, persisted `TransferExecutionMode`, persisted `MovementType`, scanner fields, and transfer-specific fields on `InventoryTransaction` in `Myrmex.Modules.Wms/Inventory/`
- [ ] T078 [P] Review shared contracts for transport-only boundaries and no domain/EF/UI dependencies in `Myrmex.Shared/Wms/Inventory/`
- [ ] T079 [P] Review UI for absence of scanner, package, LPN, batch, serial, expiry, discrepancy, cancellation, correction, approval, route optimization, and external-transfer controls in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/`
- [ ] T080 Document recommended developer-controlled validation commands in `specs/075-internal-inventory-transfer-mvp/quickstart.md`

---

## Dependencies and Execution Order

### Phase Dependencies

- Phase 1 Setup has no dependencies.
- Phase 2 Foundational depends on Phase 1 and blocks all user stories.
- US1, US2, and US3 are all P1, but practical execution order is US1 -> US2/US3 because movement commands require transfer creation and shared transfer entities.
- US4 depends on US1 plus either US2 or US3 data for meaningful movement history.
- US5 depends on US2 and US3 movement behavior.
- Final polish depends on desired user stories being complete.

### User Story Dependencies

- **US1 Create Internal Transfer Document**: starts after Phase 2.
- **US2 Execute Direct Internal Movement**: starts after Phase 2 and benefits from US1 create flow.
- **US3 Execute Transfer Through Internal Transit**: starts after Phase 2 and benefits from US1 create flow.
- **US4 Monitor Transfer Progress and History**: starts after US1; movement-history validation needs US2 or US3.
- **US5 Complete Transfer Automatically**: starts after US2 and US3 movement paths exist.

### Parallel Opportunities

- T005-T008 can run in parallel after test scaffolding exists.
- T011-T014 can be developed together after T009-T010 are understood, but final aggregate integration happens in T014.
- T023-T026 can run in parallel before US1 implementation.
- T035-T038 can run in parallel before US2 implementation.
- T045-T048 can run in parallel before US3 implementation.
- T056-T059 can run in parallel before US4 implementation.
- T070-T072 can run in parallel before US5 implementation.
- US2 and US3 can proceed in parallel after US1 contracts/domain creation are stable.

## Parallel Examples

### User Story 1

```text
Task: "T023 [P] [US1] Add handler test for same-warehouse create-transfer success and external-transfer rejection in Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs"
Task: "T025 [P] [US1] Add endpoint test for create-transfer route, body binding, and representative ProblemDetails behavior in Myrmex.Tests/Wms/Inventory/Endpoints/InventoryTransferEndpointTests.cs"
Task: "T026 [P] [US1] Add API-client test for create-transfer request body and write-result mapping in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs"
```

### User Story 2

```text
Task: "T035 [P] [US2] Add handler test for direct movement creating movement, one transfer transaction, two InventoryLedgerEntry rows, balance changes, and progress updates in Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs"
Task: "T037 [P] [US2] Add endpoint test for direct move route, body binding, and representative conflict ProblemDetails behavior in Myrmex.Tests/Wms/Inventory/Endpoints/InventoryTransferEndpointTests.cs"
```

### User Story 3

```text
Task: "T045 [P] [US3] Add handler test for pick creating movement, transfer transaction, two InventoryLedgerEntry rows, source/transit balance changes, and picked/in-transit progress in Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs"
Task: "T046 [P] [US3] Add handler test for place creating movement, transfer transaction, two InventoryLedgerEntry rows, transit/destination balance changes, and placed/in-transit progress in Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs"
```

### User Story 4

```text
Task: "T056 [P] [US4] Add handler/query test for transfer details progress formulas and read-only movement history projection in Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs"
Task: "T059 [P] [US4] Add API-client test for transfer list URL construction, details route, cancellation, and deserialization in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs"
```

### User Story 5

```text
Task: "T070 [P] [US5] Add domain/handler test for direct transfer completion after final movement in Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs"
Task: "T071 [P] [US5] Add domain/handler test for transit transfer completion only when placed equals requested and in-transit is zero in Myrmex.Tests/Wms/Inventory/Features/InventoryTransfers/InventoryTransferHandlerTests.cs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational.
3. Complete US1 create transfer document.
4. Complete either US2 direct movement or US3 transit movement for the first executable movement path.
5. Validate the selected path using `specs/075-internal-inventory-transfer-mvp/quickstart.md`.

### Incremental Delivery

1. US1 creates valid transfer documents.
2. US2 adds direct movement with ledger/balance effects.
3. US3 adds internal transit pick/place with ledger/balance effects.
4. US4 adds operational list/details/history visibility.
5. US5 hardens automatic completion and completed-transfer read-only behavior.

### Recommended Developer-Controlled Validation

Recommended commands after implementation tasks, not run automatically by planning:

```powershell
dotnet build
dotnet test
```

EF migration generation, database update, application startup, and infrastructure checks remain developer-controlled per repository workflow.
