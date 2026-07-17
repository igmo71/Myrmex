# Tasks: Reactive and On-Demand Reference Data Synchronization

**Input**: Design documents from `specs/109-reference-data-synchronization/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: The specification requires automated coverage for behavior introduced or changed by Feature #109. The tasks below use representative or parameterized coverage, retain one compact same-version smoke test for each explicit import handler, and avoid duplicating Feature #104 queue, retry, abandoned-processing, and lifecycle suites.

**Execution boundary**: These tasks may modify domain/application code, tests, and EF mappings. They do not authorize generating, creating, or editing EF migration files or model snapshots, and they do not authorize running build, tests, migration-generation, database update, AppHost, Docker, application startup, or other environment-changing commands; migration work and command execution remain developer-controlled.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish only the feature-specific locations needed by the approved design.

- [X] T001 Create the reference synchronization source and test directory structure at `Myrmex.Integrations/OneC/References/` and `Myrmex.Tests/Integrations/OneC/References/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared persistence, versioning, source projection, and existing-handler behavior required by every user story.

**Critical**: Complete this phase before implementing any user story.

- [X] T002 [P] Add focused `ExternalImportState` tests for nullable legacy versions, content equality, and defensive copying of binary version buffers in `Myrmex.Tests/Wms/Domain/ExternalImportStateTests.cs`
- [X] T003 [P] Add persistence-model tests proving exact column names, nullable `ExternalDataVersion`, preserved non-null external-identity uniqueness, and mappings for all three aggregates in `Myrmex.Tests/Wms/Infrastructure/Persistence/ExternalImportStatePersistenceTests.cs`
- [X] T004 [P] Add the Warehouse same-current-`DataVersion` smoke case (`Unchanged`, no timestamp mutation, no aggregate mutation or domain event) plus only representative legacy-version, changed-version, and lifecycle cases in `Myrmex.Tests/Wms/Topology/Features/Imports/ImportWarehousesHandlerTests.cs`
- [X] T005 [P] Add the Unit of Measure same-current-`DataVersion` smoke case (`Unchanged`, no timestamp mutation, no aggregate mutation or domain event) and only symbol-specific versioning coverage in `Myrmex.Tests/Wms/Catalog/Features/Imports/ImportUnitsOfMeasureHandlerTests.cs`
- [X] T006 [P] Add the Stock Keeping Unit same-current-`DataVersion` smoke case (`Unchanged`, no timestamp mutation, no aggregate mutation or domain event) and only base-UoM-specific versioning coverage in `Myrmex.Tests/Wms/Catalog/Features/Imports/ImportStockKeepingUnitsHandlerTests.cs`
- [X] T007 [P] Extend full-import source-projection tests to require `DataVersion` for Warehouse, Unit of Measure, and Stock Keeping Unit while retaining the existing Warehouse/SKU `IsFolder eq false` filters and adding no UoM folder semantics in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`
- [X] T008 Implement the owned `ExternalImportState` value object with nullable legacy version support, UTC import timestamp, content-based binary equality, and defensive buffer copies in `Myrmex.Modules.Wms/Domain/ExternalImportState.cs`
- [X] T009 [P] After T008, replace Warehouse external import scalars with owned state and implement legacy, same-version, changed-version, and source lifecycle semantics without externally mutable version buffers in `Myrmex.Modules.Wms/Topology/Domain/Warehouses/Warehouse.cs`
- [X] T010 [P] After T008, replace Unit of Measure external import scalars with owned state and implement legacy, same-version, changed-version, and source lifecycle semantics without externally mutable version buffers in `Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs`
- [X] T011 [P] After T008, replace Stock Keeping Unit external import scalars with owned state and implement legacy, same-version, changed-version, and source lifecycle semantics without externally mutable version buffers in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`
- [X] T012 [P] After T009, map the Warehouse owned state to `ExternalRefKey`, nullable `ExternalDataVersion`, and `LastImportedAtUtc` while preserving the existing filtered unique external-identity index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/WarehouseConfiguration.cs`
- [X] T013 [P] After T010, map the Unit of Measure owned state to `ExternalRefKey`, nullable `ExternalDataVersion`, and `LastImportedAtUtc` while preserving the existing filtered unique external-identity index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/UnitOfMeasureConfiguration.cs`
- [X] T014 [P] After T011, map the Stock Keeping Unit owned state to `ExternalRefKey`, nullable `ExternalDataVersion`, and `LastImportedAtUtc` while preserving the existing filtered unique external-identity index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`
- [X] T015 Add `Unchanged` accounting and preserve result-count invariants in `Myrmex.Modules.Wms/Catalog/Features/Imports/ReferenceImportBatchResult.cs`
- [X] T016 [P] After T015 and T009, extend the existing Warehouse import item and handler to consume `DataVersion`, return `Unchanged` before mutation, preserve legacy-version and deletion behavior, and keep folder information out of import command items in `Myrmex.Modules.Wms/Topology/Features/Imports/ImportWarehouses.cs`
- [X] T017 [P] After T015 and T010, extend the existing Unit of Measure import item and handler to consume `DataVersion`, return `Unchanged` before mutation, preserve legacy-version behavior, and add no folder semantics in `Myrmex.Modules.Wms/Catalog/Features/Imports/ImportUnitsOfMeasure.cs`
- [X] T018 [P] After T015 and T011, extend the existing Stock Keeping Unit import item and handler to consume `DataVersion`, return `Unchanged` before mutation, preserve legacy-version, deletion, and base-UoM behavior, and keep folder information out of import command items in `Myrmex.Modules.Wms/Catalog/Features/Imports/ImportStockKeepingUnits.cs`
- [X] T019 [P] Add `DataVersion` to the three explicit 1C reference transport DTOs and full-import mappings, preserve Warehouse/SKU full-import OData filtering with `IsFolder eq false`, retain `IsFolder` only as Warehouse/SKU transport information needed by single-object reads, and add no UoM folder field in `Myrmex.Integrations/OneC/Transport/Catalog_Склады.cs`, `Myrmex.Integrations/OneC/Transport/Catalog_УпаковкиЕдиницыИзмерения.cs`, `Myrmex.Integrations/OneC/Transport/Catalog_Номенклатура.cs`, and `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`

**Checkpoint**: All three existing import handlers share the approved version-aware behavior and persistence contract. Command-based validation may be requested separately by the developer.

---

## Phase 3: User Story 1 - Apply Reference Changes Reactively (Priority: P1) — MVP

**Goal**: Route accepted Feature #104 notifications for Warehouse, Unit of Measure, and Stock Keeping Unit through one-object synchronization that reuses the existing import handlers and lifecycle.

**Independent Test**: Submit eligible notifications for each supported type and verify that the current source object is applied, unchanged, controlled-skipped, or failed during that processing attempt. Reuse the foundational import-handler smoke cases for same-version no-mutation and no-event assertions instead of repeating them at service level.

### Tests for User Story 1

- [X] T020 [P] [US1] Add representative current-object read tests for the three explicit entity sets, stable-key filtering, cardinality, and required shape, including Warehouse/SKU folder detection data and no UoM folder semantics, while reusing existing timeout and cancellation transport coverage in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`
- [X] T021 [P] [US1] Extend the valid-notification theory with the three reference routes and stable entity-type mappings without duplicating Feature #104 authentication or validation suites in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCNotificationEndpointTests.cs`
- [X] T022 [P] [US1] Add the sole parameterized synchronize-one outcome suite for `Applied`, `Unchanged`, Warehouse/SKU folder `ControlledSkip` before import-command dispatch, unlinked-deletion skip, `NotFound`, `Busy`, transient failure, and permanent failure without repeating handler-owned no-mutation assertions in `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs`
- [X] T023 [P] [US1] Add thin-handler mapping tests proving `NotFound` maps to Feature #104 `PermanentFailure` and internal operation outcomes remain distinct from durable statuses without duplicating processor cancellation, retry, or recovery coverage in `Myrmex.Tests/Integrations/OneC/References/ReferenceSynchronizationHandlerTests.cs`
- [X] T024 [P] [US1] Add the sole focused gate suite proving concurrent manual import retains its existing fail-fast/409 behavior, reactive and on-demand work return `Busy`, all three entry points use the same per-reference-type lease, different types remain independent, and the SKU manual lease spans every page and batch in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`
- [X] T025 [P] [US1] Verify only that `OperationCanceledException` propagates when the processor stopping token is cancelled and the durable request remains `Processing`, relying on existing Feature #104 tests for abandoned-processing recovery algorithms in `Myrmex.Tests/Integrations/OneC/Synchronization/IntegrationSynchronizationCancellationTests.cs`

### Implementation for User Story 1

- [X] T026 [US1] Add explicit current-object read contracts and implementations for Warehouse, Unit of Measure, and Stock Keeping Unit using stable keys and full required projections, retaining Warehouse/SKU folder information for pre-dispatch detection and adding no UoM folder semantics, in `Myrmex.Integrations/OneC/Transport/IOneCODataClient.cs` and `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`
- [X] T027 [US1] Extend the singleton per-reference-type in-process gate with the shared lease and non-waiting acquisition so concurrent manual import preserves fail-fast/409 behavior, reactive and on-demand work return `Busy`, manual leases cover the whole operation, and different reference types remain independent in `Myrmex.Integrations/OneC/Imports/OneCImportGate.cs`
- [X] T028 [US1] Define the narrow internal synchronize-one result model with `Applied`, `Unchanged`, controlled skip, `NotFound`, `Busy`, transient failure, and permanent failure outcomes plus structured diagnostics in `Myrmex.Integrations/OneC/References/ReferenceSynchronizationResult.cs`
- [X] T029 [US1] Implement three explicit one-object synchronization paths that acquire the type gate before source read, return `ControlledSkip` for Warehouse/SKU folders before creating or dispatching an import command, dispatch other objects through the existing import handler, retain the lease through application commit, add no UoM folder semantics, and classify non-shutdown failures without generalized provider abstractions in `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`
- [X] T030 [US1] Add stable Warehouse, Unit of Measure, and Stock Keeping Unit entity-type constants and thin Feature #104 handlers that map internal outcomes to existing durable request statuses in `Myrmex.Integrations/Synchronization/SynchronizationEntityTypes.cs` and `Myrmex.Integrations/OneC/References/ReferenceSynchronizationHandlers.cs`
- [X] T031 [US1] Add the three reference notification routes through the existing Feature #104 validation, persistence, and response contract in `Myrmex.Integrations/OneC/Endpoints/OneCNotificationEndpoints.cs`
- [X] T032 [US1] Register the narrow reference synchronization service, explicit handlers, and stable entity-type resolution through the existing integration module and processor lifecycle in `Myrmex.Integrations/OneC/OneCIntegrationModule.cs`
- [X] T033 [US1] Rethrow `OperationCanceledException` as shutdown cancellation only when the processor stopping token is cancelled; leave source timeouts and non-shutdown failures on normal classification paths and add no durable cancelled status in `Myrmex.Integrations/Synchronization/Processing/SynchronizationProcessor.cs`

**Checkpoint**: Reactive synchronization is independently functional for all three reference types using Feature #104 infrastructure and the single-instance per-type gate.

---

## Phase 4: User Story 2 - Synchronize One Required Reference On Demand (Priority: P2)

**Goal**: Expose an internal synchronize-one operation and bounded SKU-to-UoM repair without adding a public endpoint, recursive dependency resolver, or parallel synchronization lifecycle.

**Independent Test**: Call the internal operation by supported type and key to verify direct dispatch and caller-facing cancellation, then verify a missing SKU base UoM repair synchronizes at most one UoM followed by at most one additional SKU apply. Reuse T022 for outcome classification.

### Tests for User Story 2

- [ ] T034 [P] [US2] Add direct internal-call tests only for supported type/key dispatch and propagation of caller cancellation, leaving all outcome classification to T022, in `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs`
- [ ] T035 [P] [US2] Add one successful and one failed SKU base-UoM repair test proving at most one UoM synchronization, at most one additional SKU apply, and no recursive dependency resolution in `Myrmex.Tests/Integrations/OneC/References/StockKeepingUnitReferenceRepairTests.cs`

### Implementation for User Story 2

- [ ] T036 [US2] Finalize the internal synchronize-one contract and implement the explicit bounded SKU-to-UoM repair path without a public endpoint or generalized dependency abstraction in `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`

**Checkpoint**: Internal callers can synchronize one supported reference and SKU repair is bounded, explicit, and independently testable.

---

## Phase 5: User Story 3 - Reconcile Through Existing Full Imports (Priority: P3)

**Goal**: Preserve existing manual import routes and behavior while adding DataVersion-aware `Unchanged` reporting and holding each type lease for the whole manual operation.

**Independent Test**: Run one representative full import twice with the same current `DataVersion` and verify the second service result reports `Unchanged`; use the foundational handler smoke tests for no timestamp, aggregate, or domain-event mutation assertions, while existing route, authorization, error, paging, and partial-SKU behavior remains intact.

### Tests for User Story 3

- [ ] T037 [P] [US3] Add focused full-import service tests only for `DataVersion` mapping, `Unchanged` aggregation, one representative repeated import, partial SKU accounting, and manual caller cancellation, leaving gate coverage to T024 and no-mutation/event assertions to the import-handler tests, in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`
- [ ] T038 [P] [US3] Extend existing endpoint contract tests with additive `Unchanged` counts while retaining current routes, authorization, and error shapes in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCEndpointTests.cs`
- [ ] T039 [P] [US3] Add shared-fixture client tests that deserialize nonzero `Unchanged` counts for all three existing manual import routes in `Myrmex.Tests/Integrations/OneC/Web/OneCIntegrationApiClientTests.cs`

### Implementation for User Story 3

- [ ] T040 [US3] Add the backward-compatible `Unchanged` count to the public manual-import response contract in `Myrmex.Shared/Integrations/OneC/OneCImportResponse.cs`
- [ ] T041 [US3] Map source `DataVersion`, aggregate `Unchanged`, preserve existing logging, paging, errors, partial SKU results, and caller cancellation, and hold each per-type lease for the entire manual operation in `Myrmex.Integrations/OneC/Imports/OneCImportService.cs`
- [ ] T042 [P] [US3] Add the shared `Common.Unchanged` localization entry in `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`
- [ ] T043 [US3] Display the additive `Unchanged` count in existing manual-import results without adding new controls or changing workflow in `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`

**Checkpoint**: Existing full imports remain compatible and deterministically report same-version records as unchanged.

---

## Phase 6: User Story 4 - Protect 1C-Owned Reference Fields (Priority: P4)

**Goal**: Reject actual local changes to source-owned values on linked records while allowing unchanged resubmission; permit WMS-owned `Description` edits only for Warehouse and SKU, preserve UoM as a no-op-only resubmission with no WMS-owned Description field, and keep external import state inaccessible to normal local edits.

**Independent Test**: For linked records, verify actual source-owned value or lifecycle changes are rejected; identical values are accepted as a no-op; Warehouse and SKU may change their WMS-owned `Description`, while UoM has no planned WMS-owned Description edit; external import state cannot be supplied and unlinked behavior remains unchanged.

### Tests for User Story 4

- [ ] T044 [P] [US4] Add linked-Warehouse tests proving an actual `Name` change is rejected, unchanged `Name` resubmission permits `Description` changes, and unlinked edits remain unchanged in `Myrmex.Tests/Wms/Topology/Features/Warehouses/UpdateWarehouseDetailsHandlerTests.cs`
- [ ] T045 [P] [US4] Add linked-Unit-of-Measure tests only for rejection of actual `Name` or `Symbol` changes, identical resubmission as a no-op, and preserved unlinked behavior in `Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/UpdateUnitOfMeasureDetailsHandlerTests.cs`
- [ ] T046 [P] [US4] Add linked-SKU tests proving actual `Name` or base-UoM changes are rejected, identical resubmission skips redundant base-UoM validation and permits `Description` changes, and unlinked edits remain unchanged in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetailsHandlerTests.cs`
- [ ] T047 [P] [US4] Add the representative lifecycle-guard coverage in linked-Warehouse tests, proving actual deactivate/reactivate transitions are rejected while redundant no-op requests are accepted, in `Myrmex.Tests/Wms/Topology/Features/Warehouses/DeactivateWarehouseHandlerTests.cs` and `Myrmex.Tests/Wms/Topology/Features/Warehouses/ReactivateWarehouseHandlerTests.cs`

### Implementation for User Story 4

- [ ] T048 [P] [US4] After T044 and T047, enforce Warehouse source ownership by comparing requested source-owned values and lifecycle state before rejecting, while excluding external import state from local edit contracts in `Myrmex.Modules.Wms/Topology/Domain/Warehouses/Warehouse.cs`, `Myrmex.Modules.Wms/Topology/Features/Warehouses/UpdateWarehouseDetails.cs`, `Myrmex.Modules.Wms/Topology/Features/Warehouses/DeactivateWarehouse.cs`, and `Myrmex.Modules.Wms/Topology/Features/Warehouses/ReactivateWarehouse.cs`
- [ ] T049 [P] [US4] After T045, enforce Unit of Measure source ownership by rejecting actual linked `Name`, `Symbol`, or lifecycle changes, permitting identical resubmission only as a no-op, preserving unlinked behavior, excluding external import state from local edit contracts, and introducing no WMS-owned Description edit in `Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs`, `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/UpdateUnitOfMeasureDetails.cs`, `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/DeactivateUnitOfMeasure.cs`, and `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/ReactivateUnitOfMeasure.cs`
- [ ] T050 [P] [US4] After T046, enforce Stock Keeping Unit source ownership by rejecting only actual source-owned value or lifecycle changes, checking equality before base-UoM validation, allowing WMS-owned edits, and excluding external import state from local edit contracts in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`, `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetails.cs`, `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/DeactivateStockKeepingUnit.cs`, and `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ReactivateStockKeepingUnit.cs`

**Checkpoint**: Linked-record ownership is enforced only for actual source-owned changes, with normal WMS-owned edits preserved.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational; this is the MVP synchronization slice.
- **User Story 2 (Phase 4)**: Depends on the synchronize-one service established by User Story 1.
- **User Story 3 (Phase 5)**: Depends on Foundational and can proceed in parallel with User Stories 1 and 4 after shared contracts stabilize.
- **User Story 4 (Phase 6)**: Depends on Foundational and can proceed in parallel with User Stories 1 and 3.

### User Story Dependency Graph

```text
Setup
  -> Foundational
       -> US1 (MVP) -> US2
       -> US3
       -> US4
```

### Within Each User Story

- Write the focused tests before their corresponding implementation changes.
- Stabilize internal result or public response contracts before their consumers.
- Complete T008 before T009-T011; after T008, the Warehouse, UoM, and SKU aggregate slices may proceed independently.
- Complete each aggregate before its EF mapping: T009 before T012, T010 before T013, and T011 before T014.
- Complete the shared result T015 and the corresponding aggregate before each import-handler task: T016 depends on T015 and T009, T017 depends on T015 and T010, and T018 depends on T015 and T011.
- For US4, complete T044 and the representative lifecycle test T047 before T048; complete T045 before T049; and complete T046 before T050. T049 implements UoM lifecycle protection without adding UoM deactivate/reactivate test suites.
- Preserve Feature #104 durable lifecycle ownership; do not create queue, processor, retry, recovery, or status alternatives.
- Keep the shared per-reference-type gate in-process and single-instance only: concurrent manual import retains fail-fast/409 behavior, reactive and on-demand work return `Busy`, and no distributed or cross-process locking is introduced.
- Treat operation outcomes as internal results and map them explicitly to existing durable request statuses only at the reactive handler boundary.

## Parallel Opportunities

- Foundational tests T002-T007 can be authored in parallel because they touch distinct test concerns.
- Aggregate changes T009-T011 can proceed in parallel after T008; EF tasks T012-T014 can proceed in parallel only after their corresponding aggregate task completes.
- Import-handler tasks T016-T018 can proceed in parallel by reference type only after T015 and their corresponding aggregate task; transport task T019 is independent after its test shape is established in T007.
- User Story 1 tests T020-T025 can be split by transport, endpoint, service outcome, handler mapping, gate, and processor-cancellation concern.
- User Story 2 tests T034 and T035 can proceed in parallel.
- User Story 3 tests T037-T039 can proceed in parallel; localization T042 can proceed once the response wording is stable.
- User Story 4 tests T044-T047 may proceed independently. After their stated prerequisite tests complete, T048-T050 may proceed independently as Warehouse, UoM, and SKU implementation slices.

## Parallel Example: User Story 1

```text
T020: Current-object transport reads
T021: Notification route mapping
T022: Synchronize-one outcome semantics
T023: Thin reactive-handler status mapping
T024: Per-type gate coordination
T025: Shutdown cancellation propagation
```

## Implementation Strategy

### MVP First

1. Complete Setup and Foundational phases.
2. Complete User Story 1 to connect the approved reference types to the existing Feature #104 lifecycle.
3. Hand the completed MVP slice to the developer for separately requested command-based validation.

### Incremental Delivery

1. Add User Story 2 for internal on-demand synchronization and bounded SKU repair.
2. Add User Story 3 for manual-import compatibility and `Unchanged` reporting.
3. Add User Story 4 for linked-record edit protection.

## Notes

- `[P]` marks tasks that can proceed concurrently when their stated prerequisites are satisfied and their files do not overlap.
- `[US1]` through `[US4]` map tasks to independently testable specification stories.
- The same-version smoke coverage is deliberately limited to one compact test in each explicit import-handler test file; shared behavior uses representative or parameterized coverage.
- Feature #104 synchronization-foundation coverage is reused, not reproduced.
- Full-import reads preserve the existing Warehouse/SKU `IsFolder eq false` filter; synchronize-one detects Warehouse/SKU folders before command dispatch; import command items and UoM have no folder semantics.
- Planning artifacts record the expected developer-generated migration as exactly three nullable `ExternalDataVersion` columns with no changes to existing `ExternalRefKey` or `LastImportedAtUtc` columns or their indexes; no task generates, creates, or edits migration files or the model snapshot.
- No task authorizes execution of build, tests, migrations, database updates, AppHost, Docker, or application startup.
