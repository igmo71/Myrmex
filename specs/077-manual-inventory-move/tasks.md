# Tasks: Manual Inventory Move

**Input**: Design documents from `/specs/077-manual-inventory-move/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Automated tasks protect the identified balance, concurrency, atomicity, API-boundary, and client-transport risks. UI behavior follows an existing MudBlazor dialog/grid pattern and uses the manual smoke validation in `quickstart.md`.

**Organization**: Tasks are grouped by user story so each story has an independently verifiable outcome. Shared move behavior is implemented in the earliest story that needs it and reused by the audit story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it changes a different file and has no dependency on an incomplete task
- **[Story]**: Maps the task to a user story from `spec.md`
- Every task includes an exact repository file path

## Phase 1: Setup (Shared Contracts)

**Purpose**: Establish the public write contract used by the endpoint, API client, and UI.

- [X] T001 [P] Define nullable identifiers, positive quantity input, reason, and expected source rowversion in `Myrmex.Shared/Wms/Inventory/MoveInventoryBalanceRequest.cs`
- [X] T002 [P] Define the authoritative move outcome with both `InventoryBalanceDetails` snapshots, moved quantity, before/after quantities, and occurrence time in `Myrmex.Shared/Wms/Inventory/MoveInventoryBalanceResult.cs`

---

## Phase 2: Foundational (Test Data)

**Purpose**: Provide reusable SQL Server-backed fixtures for all move and lookup story tests without duplicating warehouse topology setup.

**Critical**: Complete this phase before handler tests.

- [X] T003 Extend `Myrmex.Tests/Wms/Inventory/Testing/InventoryBalanceTestData.cs` with builders for two regular locations in one warehouse, a second warehouse, transit locations, inactive SKU/location/type/status references, and optional destination balances

**Checkpoint**: Shared contracts and reusable test fixtures are ready.

---

## Phase 3: User Story 1 - Move Inventory from a Balance Row (Priority: P1) MVP

**Goal**: Move a positive quantity from a selected source balance to an eligible destination and show authoritative before/after results in the Inventory Balances UI.

**Independent Test**: Open Move from an existing balance row, select a different eligible location in the same warehouse, submit quantity/reason/current version, and verify the result plus refreshed source and destination quantities.

### Tests for User Story 1

- [X] T004 [US1] Write SQL Server handler tests for existing-destination success, missing-destination creation, full-source zero retention, returned before/after details, validation failures, missing-reference 404 results, stale/missing/insufficient source conflicts, destination concurrency, and no partial balance changes in `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/MoveInventoryBalanceHandlerTests.cs`
- [X] T005 [P] [US1] Add focused POST route/body binding, success serialization, representative validation/not-found/conflict ProblemDetails, and cancellation dispatch tests in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryBalanceEndpointTests.cs`
- [X] T006 [P] [US1] Add API-client tests for the exact move route, serialized request body, shared result deserialization, cancellation propagation, and representative 409 mapping in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 1

- [X] T007 [US1] Implement `MoveInventoryBalance.Command` and handler validation, reference eligibility, source rowversion/quantity checks, destination update-or-create behavior, one `CreateTransfer` transaction, one atomic save, concurrency/duplicate mapping, result reload, and cancellation in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/MoveInventoryBalance.cs`
- [X] T008 [US1] Map `POST /balances/move` to the internal command and `ApiResult<MoveInventoryBalanceResult>` HTTP convention in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs`
- [X] T009 [US1] Add `TryMoveInventoryBalanceAsync` using `/api/wms/inventory/balances/move` and propagate cancellation in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T010 [US1] Build the MudBlazor move dialog with read-only source context, bounded warehouse-scoped non-transit destination lookup, source exclusion, quantity/reason validation, stale-conflict lockout, and success summary in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/MoveInventoryBalanceDialog.razor`
- [X] T011 [US1] Add a labeled Move row action and `MoveRequested` callback beside History and Adjust in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor`
- [X] T012 [US1] Add dialog orchestration, success/conflict handling, snackbar feedback, and grid reload behavior in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`
- [X] T013 [US1] Wire the grid `MoveRequested` event to the page handler in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor`

**Checkpoint**: The operator workflow moves inventory, reports authoritative results, and refreshes the grid.

---

## Phase 4: User Story 2 - Preserve Auditable Inventory History (Priority: P1)

**Goal**: Ensure each successful move creates balanced transfer history and every rejected move remains atomic.

**Independent Test**: Complete one move and verify one `Transfer` transaction, exactly two opposing entries with the trimmed reason, no Inventory Transfer or adjustment records, and no persisted effects for rejected moves.

### Tests for User Story 2

- [ ] T014 [US2] Extend `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/MoveInventoryBalanceHandlerTests.cs` with focused assertions for one Transfer transaction, exactly two balanced source/destination entries, shared occurrence time/reason, no Inventory Transfer or adjustment artifacts, and atomic rollback for concurrency or persistence failure

### Implementation for User Story 2

- [ ] T015 [US2] Add structured success and rejection diagnostics containing SKU, source, destination, quantity, created transaction identity when available, and safe rejection reason in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/MoveInventoryBalance.cs`

**Checkpoint**: Successful and rejected moves are auditable without transfer-document or adjustment side effects.

---

## Phase 5: User Story 3 - Look Up Balance by SKU and Location (Priority: P2)

**Goal**: Return the current balance and version for an exact SKU/location pair without applying move eligibility filters.

**Independent Test**: Request an existing active or inactive-reference pair and receive `InventoryBalanceDetails`; request a missing pair and receive not found.

### Tests for User Story 3

- [ ] T016 [US3] Write handler tests for exact-pair details, current balance version, inactive SKU/location/type/status visibility, missing-pair not found, and cancellation in `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/GetInventoryBalanceBySkuAndStorageLocationHandlerTests.cs`
- [ ] T017 [P] [US3] Add focused GET query binding, shared details serialization, 404 ProblemDetails, and cancellation dispatch tests in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryBalanceEndpointTests.cs`
- [ ] T018 [P] [US3] Add API-client tests for the exact encoded lookup URL, shared details mapping, cancellation propagation, and read/load not-found behavior in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 3

- [ ] T019 [US3] Implement the exact-pair `AsNoTracking` query, existing details projection, post-materialization rowversion conversion, and not-found result in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/GetInventoryBalanceBySkuAndStorageLocation.cs`
- [ ] T020 [US3] Map `GET /balances/lookup?skuId={skuId}&storageLocationId={storageLocationId}` before the `{inventoryBalanceId:guid}` route and dispatch the internal query in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs`
- [ ] T021 [US3] Add `GetInventoryBalanceBySkuAndStorageLocationAsync` with exact query construction and cancellation propagation in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`

**Checkpoint**: Scanner-ready lookup reports actual persisted balance state independently of move eligibility.

---

## Phase 6: Polish & Cross-Cutting Validation

**Purpose**: Validate the integrated feature and guard the explicitly excluded scope.

- [ ] T022 Execute and record the lookup, existing/missing destination, full move, concurrency, missing-reference, eligibility, and UI smoke scenarios in `specs/077-manual-inventory-move/quickstart.md`
- [ ] T023 Record developer-provided validation results for dotnet build and dotnet test with `MYRMEX_WMS_TEST_CONNECTION` targeting a dedicated `_test` database in `specs/077-manual-inventory-move/quickstart.md`
- [ ] T024 Review the final diff for bounded point queries, one move save, no migration, and no Inventory Transfer, adjustment, scanner UI, inter-warehouse, or transit workflow changes; record the review result in `specs/077-manual-inventory-move/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1** has no dependencies.
- **Phase 2** depends on Phase 1 and blocks handler-test work.
- **US1 (Phase 3)** depends on Phases 1-2.
- **US2 (Phase 4)** depends on US1's move handler because it verifies and instruments the shared persisted operation.
- **US3 (Phase 5)** depends on Phase 2 only, but endpoint/client edits should be sequenced with US1 to avoid file conflicts.
- **Polish (Phase 6)** depends on all selected user stories.

### User Story Dependency Graph

```text
Setup -> Foundation -> US1 -> US2
                    \-> US3

US1 and US3 are behaviorally independent after Foundation.
US2 verifies and instruments the audit effects produced by US1.
```

### Within Each User Story

- Write the identified risk-based automated tests before the implementation they protect and confirm they fail for the expected missing behavior.
- Keep public request/result types in `Myrmex.Shared` and internal commands, queries, projections, and EF behavior in `Myrmex.Modules.Wms`.
- Complete handler behavior before endpoint mapping, and endpoint mapping before UI integration.
- Use one relational save for source, destination, transaction, and ledger entries.
- Do not add a migration unless a separately approved schema need is discovered.
- Builds, tests, startup, database, migration, and infrastructure commands remain developer-controlled.

### Parallel Opportunities

- T001 and T002 can run in parallel.
- T005 and T006 can run in parallel after T001-T002.
- T017 and T018 can run in parallel after the shared test fixtures are ready.
- US3 handler/query work can proceed in parallel with US1 UI work if endpoint/client file ownership is coordinated.
- T022 and T024 can be prepared in parallel after implementation; T023 remains developer-controlled.

---

## Parallel Example: User Story 1

```text
Task T005: Add focused move endpoint boundary tests in Myrmex.Tests/Wms/Inventory/Endpoints/InventoryBalanceEndpointTests.cs
Task T006: Add move API-client transport tests in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs
```

## Parallel Example: User Story 3

```text
Task T017: Add lookup endpoint boundary tests in Myrmex.Tests/Wms/Inventory/Endpoints/InventoryBalanceEndpointTests.cs
Task T018: Add lookup API-client transport tests in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs
```

---

## Implementation Strategy

### MVP First

1. Complete Setup and Foundation.
2. Complete US1.
3. Complete US2 before production release so audit and atomicity requirements are explicitly verified.
4. Perform the US1/US2 quickstart scenarios.

### Incremental Delivery

1. Deliver US1 and US2 together as the operational manual-move MVP.
2. Add US3 as the independently testable scanner-ready read boundary.
3. Complete cross-cutting validation and record developer-controlled build/test evidence.

### Review Boundaries

- Commit or review shared contracts separately from backend behavior.
- Review handler tests and handler implementation together.
- Review endpoint/client boundary changes separately from the MudBlazor UI.
- Treat any migration, new abstraction, or Inventory Transfer coupling as a scope change requiring plan review.

## Notes

- `[P]` means different files and no dependency on an incomplete task.
- Existing `InventoryTransaction.CreateTransfer` domain tests already protect the factory invariants; do not duplicate them unless the factory changes.
- Destination lookup reuses the existing Topology API with `SelectableOnly = true` and `ExcludeTransitTypes = true`; server validation remains authoritative.
- UI component automation is intentionally deferred because the repository has no component-test framework for this repeated pattern.
