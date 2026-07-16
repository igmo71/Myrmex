# Tasks: Reactive and On-Demand Reference Data Synchronization

**Input**: Design documents from `specs/109-reference-data-synchronization/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: The specification requires automated coverage for behavior introduced or changed by Feature #109. The tasks below use representative or parameterized coverage, retain one compact same-version smoke test for each explicit import handler, and avoid duplicating Feature #104 queue, retry, abandoned-processing, and lifecycle suites.

**Execution boundary**: These tasks may create or modify source code, tests, EF mappings, and migration source files. They do not authorize running build, tests, migration-generation, database update, AppHost, Docker, application startup, or other environment-changing commands; execution remains developer-controlled.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish only the feature-specific locations needed by the approved design.

- [ ] T001 Create the reference synchronization source and test directory structure at `Myrmex.Integrations/OneC/References/` and `Myrmex.Tests/Integrations/OneC/References/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared persistence, versioning, source projection, and existing-handler behavior required by every user story.

**Critical**: Complete this phase before implementing any user story.

- [ ] T002 [P] Add focused `ExternalImportState` tests for nullable legacy versions, content equality, and defensive copying of binary version buffers in `Myrmex.Tests/Wms/Domain/ExternalImportStateTests.cs`
- [ ] T003 [P] Add persistence-model tests proving exact column names, nullable `ExternalDataVersion`, preserved non-null external-identity uniqueness, and mappings for all three aggregates in `Myrmex.Tests/Wms/Infrastructure/Persistence/ExternalImportStatePersistenceTests.cs`
- [ ] T004 [P] Add the Warehouse same-current-`DataVersion` smoke case (`Unchanged`, no timestamp mutation, no aggregate mutation or domain event) plus only representative legacy-version, changed-version, and lifecycle cases in `Myrmex.Tests/Wms/Topology/Features/Imports/ImportWarehousesHandlerTests.cs`
- [ ] T005 [P] Add the Unit of Measure same-current-`DataVersion` smoke case (`Unchanged`, no timestamp mutation, no aggregate mutation or domain event) and only symbol-specific versioning coverage in `Myrmex.Tests/Wms/Catalog/Features/Imports/ImportUnitsOfMeasureHandlerTests.cs`
- [ ] T006 [P] Add the Stock Keeping Unit same-current-`DataVersion` smoke case (`Unchanged`, no timestamp mutation, no aggregate mutation or domain event) and only base-UoM-specific versioning coverage in `Myrmex.Tests/Wms/Catalog/Features/Imports/ImportStockKeepingUnitsHandlerTests.cs`
- [ ] T007 [P] Extend full-import source-projection tests to require `DataVersion` for Warehouse, Unit of Measure, and Stock Keeping Unit without duplicating transport failure suites in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`
- [ ] T008 Implement the owned `ExternalImportState` value object with nullable legacy version support, UTC import timestamp, content-based binary equality, and defensive buffer copies in `Myrmex.Modules.Wms/Domain/ExternalImportState.cs`
- [ ] T009 [P] Replace Warehouse external import scalars with owned state and implement legacy, same-version, changed-version, and source lifecycle semantics without externally mutable version buffers in `Myrmex.Modules.Wms/Topology/Domain/Warehouses/Warehouse.cs`
- [ ] T010 [P] Replace Unit of Measure external import scalars with owned state and implement legacy, same-version, changed-version, and source lifecycle semantics without externally mutable version buffers in `Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs`
- [ ] T011 [P] Replace Stock Keeping Unit external import scalars with owned state and implement legacy, same-version, changed-version, and source lifecycle semantics without externally mutable version buffers in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`
- [ ] T012 [P] Map the Warehouse owned state to `ExternalRefKey`, nullable `ExternalDataVersion`, and `LastImportedAtUtc` while preserving the existing filtered unique external-identity index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/WarehouseConfiguration.cs`
- [ ] T013 [P] Map the Unit of Measure owned state to `ExternalRefKey`, nullable `ExternalDataVersion`, and `LastImportedAtUtc` while preserving the existing filtered unique external-identity index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/UnitOfMeasureConfiguration.cs`
- [ ] T014 [P] Map the Stock Keeping Unit owned state to `ExternalRefKey`, nullable `ExternalDataVersion`, and `LastImportedAtUtc` while preserving the existing filtered unique external-identity index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`
- [ ] T015 Prepare the additive nullable-version migration source, metadata, and model snapshot changes without executing migration generation or database commands in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/20260716000000_AddReferenceExternalDataVersion.cs`, `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/20260716000000_AddReferenceExternalDataVersion.Designer.cs`, and `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`
- [ ] T016 Add `Unchanged` accounting and preserve result-count invariants in `Myrmex.Modules.Wms/Catalog/Features/Imports/ReferenceImportBatchResult.cs`
- [ ] T017 [P] Extend the existing Warehouse import item and handler to consume `DataVersion`, return `Unchanged` before mutation, preserve legacy-version import behavior, treat source folders as controlled skips, and apply deletion state through existing business logic in `Myrmex.Modules.Wms/Topology/Features/Imports/ImportWarehouses.cs`
- [ ] T018 [P] Extend the existing Unit of Measure import item and handler to consume `DataVersion`, return `Unchanged` before mutation, preserve legacy-version import behavior, and avoid introducing folder handling in `Myrmex.Modules.Wms/Catalog/Features/Imports/ImportUnitsOfMeasure.cs`
- [ ] T019 [P] Extend the existing Stock Keeping Unit import item and handler to consume `DataVersion`, return `Unchanged` before mutation, preserve legacy-version import behavior, treat source folders as controlled skips, and keep base-UoM validation within existing business logic in `Myrmex.Modules.Wms/Catalog/Features/Imports/ImportStockKeepingUnits.cs`
- [ ] T020 [P] Add `DataVersion` to the three explicit 1C reference DTO projections and full-import mappings without adding folder support to reference types that do not expose folders in `Myrmex.Integrations/OneC/Transport/Catalog_Склады.cs`, `Myrmex.Integrations/OneC/Transport/Catalog_УпаковкиЕдиницыИзмерения.cs`, `Myrmex.Integrations/OneC/Transport/Catalog_Номенклатура.cs`, and `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`

**Checkpoint**: All three existing import handlers share the approved version-aware behavior and persistence contract. Command-based validation may be requested separately by the developer.

---

## Phase 3: User Story 1 - Apply Reference Changes Reactively (Priority: P1) — MVP

**Goal**: Route accepted Feature #104 notifications for Warehouse, Unit of Measure, and Stock Keeping Unit through one-object synchronization that reuses the existing import handlers and lifecycle.

**Independent Test**: Submit eligible notifications for each supported type and verify that the current source object is applied, unchanged, controlled-skipped, or failed during that processing attempt; repeated same-version delivery causes no timestamp, aggregate, or domain-event mutation.

### Tests for User Story 1

- [ ] T021 [P] [US1] Add representative current-object read tests for the three explicit entity sets, stable-key filtering, cardinality, and required shape while reusing existing timeout and cancellation transport coverage in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`
- [ ] T022 [P] [US1] Extend the valid-notification theory with the three reference routes and stable entity-type mappings without duplicating Feature #104 authentication or validation suites in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCNotificationEndpointTests.cs`
- [ ] T023 [P] [US1] Add parameterized synchronize-one outcome coverage for `Applied`, `Unchanged`, applicable folder skip, unlinked deletion skip, `NotFound`, `Busy`, transient failure, and permanent failure in `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs`
- [ ] T024 [P] [US1] Add thin-handler mapping tests proving `NotFound` maps to Feature #104 `PermanentFailure`, internal operation outcomes are not durable statuses, and shutdown cancellation propagates without duplicating processor retry or recovery coverage in `Myrmex.Tests/Integrations/OneC/References/ReferenceSynchronizationHandlerTests.cs`
- [ ] T025 [P] [US1] Add focused gate coverage proving same-type manual/reactive work returns `Busy`, different reference types remain independent, and the SKU manual lease spans every page and batch in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`
- [ ] T026 [P] [US1] Update cancellation coverage so `OperationCanceledException` is rethrown only when the processor stopping token is cancelled and the request remains recoverable through existing abandoned-processing behavior in `Myrmex.Tests/Integrations/OneC/Synchronization/IntegrationSynchronizationCancellationTests.cs`

### Implementation for User Story 1

- [ ] T027 [US1] Add explicit current-object read contracts and implementations for Warehouse, Unit of Measure, and Stock Keeping Unit using stable keys and full required projections in `Myrmex.Integrations/OneC/Transport/IOneCODataClient.cs` and `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`
- [ ] T028 [US1] Extend the singleton per-reference-type in-process gate with non-waiting acquisition for reactive and on-demand work while preserving whole-operation manual leases and type independence in `Myrmex.Integrations/OneC/Imports/OneCImportGate.cs`
- [ ] T029 [US1] Define the narrow internal synchronize-one result model with `Applied`, `Unchanged`, controlled skip, `NotFound`, `Busy`, transient failure, and permanent failure outcomes plus structured diagnostics in `Myrmex.Integrations/OneC/References/ReferenceSynchronizationResult.cs`
- [ ] T030 [US1] Implement three explicit one-object synchronization paths that acquire the type gate before source read, dispatch a single mapped item through the existing import handler, retain the lease through application commit, and classify non-shutdown failures without generalized provider abstractions in `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`
- [ ] T031 [US1] Add stable Warehouse, Unit of Measure, and Stock Keeping Unit entity-type constants and thin Feature #104 handlers that map internal outcomes to existing durable request statuses in `Myrmex.Integrations/Synchronization/SynchronizationEntityTypes.cs` and `Myrmex.Integrations/OneC/References/ReferenceSynchronizationHandlers.cs`
- [ ] T032 [US1] Add the three reference notification routes through the existing Feature #104 validation, persistence, and response contract in `Myrmex.Integrations/OneC/Endpoints/OneCNotificationEndpoints.cs`
- [ ] T033 [US1] Register the narrow reference synchronization service, explicit handlers, and stable entity-type resolution through the existing integration module and processor lifecycle in `Myrmex.Integrations/OneC/OneCIntegrationModule.cs`
- [ ] T034 [US1] Rethrow `OperationCanceledException` as shutdown cancellation only when the processor stopping token is cancelled; leave source timeouts and non-shutdown failures on normal classification paths and add no durable cancelled status in `Myrmex.Integrations/Synchronization/Processing/SynchronizationProcessor.cs`

**Checkpoint**: Reactive synchronization is independently functional for all three reference types using Feature #104 infrastructure and the single-instance per-type gate.

---

## Phase 4: User Story 2 - Synchronize One Required Reference On Demand (Priority: P2)

**Goal**: Expose an internal synchronize-one operation and bounded SKU-to-UoM repair without adding a public endpoint, recursive dependency resolver, or parallel synchronization lifecycle.

**Independent Test**: Call the internal operation by supported type and key and verify each defined outcome, caller-facing cancellation, and a missing SKU base UoM repair that synchronizes at most one UoM followed by at most one additional SKU apply.

### Tests for User Story 2

- [ ] T035 [P] [US2] Add direct internal-call tests for supported type/key dispatch, all caller-visible outcomes, and propagation of caller cancellation in `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs`
- [ ] T036 [P] [US2] Add one successful and one failed SKU base-UoM repair test proving at most one UoM synchronization, at most one additional SKU apply, and no recursive dependency resolution in `Myrmex.Tests/Integrations/OneC/References/StockKeepingUnitReferenceRepairTests.cs`

### Implementation for User Story 2

- [ ] T037 [US2] Finalize the internal synchronize-one contract and implement the explicit bounded SKU-to-UoM repair path without a public endpoint or generalized dependency abstraction in `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`

**Checkpoint**: Internal callers can synchronize one supported reference and SKU repair is bounded, explicit, and independently testable.

---

## Phase 5: User Story 3 - Reconcile Through Existing Full Imports (Priority: P3)

**Goal**: Preserve existing manual import routes and behavior while adding DataVersion-aware `Unchanged` reporting and holding each type lease for the whole manual operation.

**Independent Test**: Run each existing import handler twice with the same current `DataVersion`; the second run reports `Unchanged` with no timestamp, aggregate, or domain-event mutation while existing route, authorization, error, paging, and partial-SKU behavior remains intact.

### Tests for User Story 3

- [ ] T038 [P] [US3] Add focused full-import service tests for `DataVersion` mapping, `Unchanged` aggregation, repeated representative import behavior, whole-operation type leases, partial SKU results, and caller cancellation in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`
- [ ] T039 [P] [US3] Extend existing endpoint contract tests with additive `Unchanged` counts while retaining current routes, authorization, and error shapes in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCEndpointTests.cs`
- [ ] T040 [P] [US3] Add shared-fixture client tests that deserialize nonzero `Unchanged` counts for all three existing manual import routes in `Myrmex.Tests/Integrations/OneC/Web/OneCIntegrationApiClientTests.cs`

### Implementation for User Story 3

- [ ] T041 [US3] Add the backward-compatible `Unchanged` count to the public manual-import response contract in `Myrmex.Shared/Integrations/OneC/OneCImportResponse.cs`
- [ ] T042 [US3] Map source `DataVersion`, aggregate `Unchanged`, preserve existing logging, paging, errors, partial SKU results, and caller cancellation, and hold each per-type lease for the entire manual operation in `Myrmex.Integrations/OneC/Imports/OneCImportService.cs`
- [ ] T043 [P] [US3] Add the shared `Common.Unchanged` localization entry in `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`
- [ ] T044 [US3] Display the additive `Unchanged` count in existing manual-import results without adding new controls or changing workflow in `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`

**Checkpoint**: Existing full imports remain compatible and deterministically report same-version records as unchanged.

---

## Phase 6: User Story 4 - Protect 1C-Owned Reference Fields (Priority: P4)

**Goal**: Reject actual local changes to source-owned values on linked records while allowing unchanged resubmission and WMS-owned edits; keep external import state inaccessible to normal local edits.

**Independent Test**: For linked Warehouse, Unit of Measure, and Stock Keeping Unit records, verify actual source-owned changes are rejected, identical source-owned values can be resubmitted while changing `Description`, and external import state cannot be supplied; verify unlinked records retain existing behavior.

### Tests for User Story 4

- [ ] T045 [P] [US4] Add linked-Warehouse tests proving an actual `Name` change is rejected, unchanged `Name` resubmission permits `Description` changes, and unlinked edits remain unchanged in `Myrmex.Tests/Wms/Topology/Features/Warehouses/UpdateWarehouseDetailsHandlerTests.cs`
- [ ] T046 [P] [US4] Add linked-Unit-of-Measure tests proving actual `Name` or `Symbol` changes are rejected, identical resubmission permits `Description` changes, and unlinked edits remain unchanged in `Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/UpdateUnitOfMeasureDetailsHandlerTests.cs`
- [ ] T047 [P] [US4] Add linked-SKU tests proving actual `Name` or base-UoM changes are rejected, identical resubmission skips redundant base-UoM validation and permits `Description` changes, and unlinked edits remain unchanged in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetailsHandlerTests.cs`
- [ ] T048 [P] [US4] Add representative linked-Warehouse lifecycle tests proving actual deactivate/reactivate transitions are rejected while redundant no-op requests are accepted in `Myrmex.Tests/Wms/Topology/Features/Warehouses/DeactivateWarehouseHandlerTests.cs` and `Myrmex.Tests/Wms/Topology/Features/Warehouses/ReactivateWarehouseHandlerTests.cs`

### Implementation for User Story 4

- [ ] T049 [P] [US4] Enforce Warehouse source ownership by comparing requested source-owned values and lifecycle state before rejecting, while excluding external import state from local edit contracts in `Myrmex.Modules.Wms/Topology/Domain/Warehouses/Warehouse.cs`, `Myrmex.Modules.Wms/Topology/Features/Warehouses/UpdateWarehouseDetails.cs`, `Myrmex.Modules.Wms/Topology/Features/Warehouses/DeactivateWarehouse.cs`, and `Myrmex.Modules.Wms/Topology/Features/Warehouses/ReactivateWarehouse.cs`
- [ ] T050 [P] [US4] Enforce Unit of Measure source ownership by rejecting only actual source-owned value or lifecycle changes while allowing WMS-owned edits and excluding external import state from local edit contracts in `Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs`, `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/UpdateUnitOfMeasureDetails.cs`, `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/DeactivateUnitOfMeasure.cs`, and `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/ReactivateUnitOfMeasure.cs`
- [ ] T051 [P] [US4] Enforce Stock Keeping Unit source ownership by rejecting only actual source-owned value or lifecycle changes, checking equality before base-UoM validation, allowing WMS-owned edits, and excluding external import state from local edit contracts in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`, `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetails.cs`, `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/DeactivateStockKeepingUnit.cs`, and `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ReactivateStockKeepingUnit.cs`

**Checkpoint**: Linked-record ownership is enforced only for actual source-owned changes, with normal WMS-owned edits preserved.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete the developer-controlled handoff without adding command execution to the plan.

- [ ] T052 Update the prepared migration filenames, manual inspection points, acceptance scenarios, and developer-controlled validation handoff in `specs/109-reference-data-synchronization/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational; this is the MVP synchronization slice.
- **User Story 2 (Phase 4)**: Depends on the synchronize-one service established by User Story 1.
- **User Story 3 (Phase 5)**: Depends on Foundational and can proceed in parallel with User Stories 1 and 4 after shared contracts stabilize.
- **User Story 4 (Phase 6)**: Depends on Foundational and can proceed in parallel with User Stories 1 and 3.
- **Polish (Phase 7)**: Depends on every user story included in the intended delivery.

### User Story Dependency Graph

```text
Setup
  -> Foundational
       -> US1 (MVP) -> US2
       -> US3
       -> US4
US1 + US2 + US3 + US4
  -> Polish
```

### Within Each User Story

- Write the focused tests before their corresponding implementation changes.
- Stabilize internal result or public response contracts before their consumers.
- Preserve Feature #104 durable lifecycle ownership; do not create queue, processor, retry, recovery, or status alternatives.
- Keep the per-reference-type gate in-process and single-instance only; do not introduce distributed or cross-process locking.
- Treat operation outcomes as internal results and map them explicitly to existing durable request statuses only at the reactive handler boundary.

## Parallel Opportunities

- Foundational tests T002-T007 can be authored in parallel because they touch distinct test concerns.
- Aggregate changes T009-T011 and EF configurations T012-T014 can be split by reference type after T008.
- Existing import handler changes T017-T019 and source DTO/client projection work T020 can proceed in parallel after shared result semantics stabilize.
- User Story 1 tests T021-T026 can be split by transport, endpoint, service, handler, gate, and processor-cancellation concern.
- User Story 2 tests T035 and T036 can proceed in parallel.
- User Story 3 tests T038-T040 can proceed in parallel; localization T043 can proceed once the response wording is stable.
- User Story 4 tests T045-T048 and type-specific implementation tasks T049-T051 can be split by reference type.

## Parallel Example: User Story 1

```text
T021: Current-object transport reads
T022: Notification route mapping
T023: Synchronize-one outcome semantics
T024: Thin reactive-handler status mapping
T025: Per-type gate coordination
T026: Shutdown cancellation propagation
```

## Parallel Example: User Story 4

```text
T045 + T049: Warehouse ownership slice
T046 + T050: Unit of Measure ownership slice
T047 + T051: Stock Keeping Unit ownership slice
T048: Representative lifecycle behavior
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
4. Finish the documentation handoff in Phase 7.

## Notes

- `[P]` marks tasks that can proceed concurrently when their stated prerequisites are satisfied and their files do not overlap.
- `[US1]` through `[US4]` map tasks to independently testable specification stories.
- The same-version smoke coverage is deliberately limited to one compact test in each explicit import-handler test file; shared behavior uses representative or parameterized coverage.
- Feature #104 synchronization-foundation coverage is reused, not reproduced.
- Folder controlled skips apply only where the source reference type actually exposes folders.
- No task authorizes execution of build, tests, migrations, database updates, AppHost, Docker, or application startup.
