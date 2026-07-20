---

description: "Implementation tasks for Issue #111 1C reference vertical slices"
---

# Tasks: 1C Reference Vertical Slices

**Input**: Design documents from `specs/111-onec-reference-vertical-slices/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, and `contracts/`

**Tests**: Reuse and minimally retarget the existing Feature #104/#109 tests. The only new automated case is one compact parameterized method in the existing `StockKeepingUnitReferenceRepairTests` class for the material failed-UoM outcome boundary. Do not add test classes, per-reference behavior matrices, a logging suite, or Feature #104 foundation tests.

**Execution boundary**: These tasks describe code and test edits only. Build, test, migration, database-update, AppHost, Docker, application-startup, and runtime commands remain developer-controlled and are not tasks in this file.

**Organization**: Tasks are grouped by user story so each reference slice can be reviewed and validated as an explicit ownership increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can be completed in parallel because it uses different files and has no dependency on another incomplete task in the phase
- **[Story]**: Maps the task to User Story 1, 2, or 3
- Every checklist item names the exact file or files it changes

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the shared target locations without changing projects, packages, public contracts, persistence, or runtime configuration.

- [ ] T001 Relocate the singleton gate from `Myrmex.Integrations/OneC/Imports/OneCImportGate.cs` to `Myrmex.Integrations/OneC/Common/Imports/OneCImportGate.cs` and update its existing direct callers in `Myrmex.Integrations/OneC/Imports/OneCImportService.cs`, `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`, `Myrmex.Integrations/OneC/OneCIntegrationModule.cs`, `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`, `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs`, and `Myrmex.Tests/Integrations/OneC/References/StockKeepingUnitReferenceRepairTests.cs` while preserving one singleton with the exact Warehouse, UoM, and SKU lease identities and non-waiting semantics.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extract only the uniform mechanisms used by all three slices.

**Critical**: Complete this phase before implementing a reference-owned slice.

- [ ] T002 Extract integration-wide configuration validation, authenticated request execution, timeout/cancellation handling, query encoding, JSON-envelope deserialization, and transport error taxonomy into `Myrmex.Integrations/OneC/Common/Transport/IOneCODataTransport.cs`, `Myrmex.Integrations/OneC/Common/Transport/OneCODataTransport.cs`, `Myrmex.Integrations/OneC/Common/Transport/OneCODataCollectionResponse.cs`, and `Myrmex.Integrations/OneC/Common/Transport/OneCTransportException.cs`; temporarily refactor `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs` to use that common transport internally while `Myrmex.Integrations/OneC/Transport/IOneCODataClient.cs` and `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs` retain typed operations for unmigrated references. Preserve validation of enabled state, base URL, credentials, all three entity sets, batch size, and timeout; do not create a second independent transport implementation. Remove only Warehouse typed operations in T014, UoM typed operations in T022, and SKU typed operations and the remaining client surface in T031/T036.
- [ ] T003 Extract uniform complete/incomplete response construction, batch-result conversion, and the 50-error cap from `Myrmex.Integrations/OneC/Imports/OneCImportService.cs` into `Myrmex.Integrations/OneC/Common/Imports/OneCImportResponseFactory.cs`, preserving safe incomplete active-import errors for 1C authentication rejection, unavailable entity sets, malformed responses, source unavailability, source timeout, and unexpected application/batch failures without accepting source, mapping, dispatch, classification, or other workflow delegates.
- [ ] T004 [P] Move synchronization outcome/reason/result primitives from `Myrmex.Integrations/OneC/References/ReferenceSynchronizationResult.cs` into `Myrmex.Integrations/OneC/Common/References/ReferenceSynchronizationResult.cs` and extract a pure completed-result translator from `Myrmex.Integrations/OneC/References/ReferenceSynchronizationHandlers.cs` into `Myrmex.Integrations/OneC/Common/References/ReferenceSynchronizationHandlerResultMapper.cs` that does not parse requests, select slices, invoke callbacks, or log; update the temporary intermediate consumers in `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`, `Myrmex.Integrations/OneC/References/ReferenceSynchronizationHandlers.cs`, `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs`, `Myrmex.Tests/Integrations/OneC/References/ReferenceSynchronizationHandlerTests.cs`, and `Myrmex.Tests/Integrations/OneC/References/StockKeepingUnitReferenceRepairTests.cs` so no unresolved old namespace remains before their later slice retargeting.
- [ ] T005 Minimally retarget the existing representative outcome theory in `Myrmex.Tests/Integrations/OneC/References/ReferenceSynchronizationHandlerTests.cs` to the `ReferenceSynchronizationHandlerResultMapper` created by T004, preserving one shared mapping matrix and adding no concrete-handler or logging matrix; this task depends on T004.
- [ ] T006 [P] Retarget the existing shared-mechanism coverage without moving or multiplying test classes: in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`, make `OneCODataTransport` own integration-wide configuration validation, authentication and credential safety, HTTP/transport error classification, malformed JSON envelope, caller cancellation, and per-request timeout cases; in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`, make `OneCImportResponseFactory` own currently covered complete/incomplete response construction, transport-reason to safe operation-error conversion, record-error conversion, and the single representative 50-error-cap scenario. This task depends on T002 and T003, can proceed independently of T004/T005 once those dependencies complete, and adds no per-reference matrix.

**Checkpoint**: Shared code contains only transport, configuration/error taxonomy, lease coordination, response construction, synchronization result primitives, and pure durable-result mapping.

---

## Phase 3: User Story 1 - Understand and Change Warehouse Integration Locally (Priority: P1) MVP

**Goal**: Give Warehouse source loading, manual import, synchronize-one behavior, durable handling, mapping, classification, and diagnostics one explicit owner.

**Independent Test**: Trace the existing Warehouse import route and durable handler into `OneC/Warehouses/` and verify full-collection loading, optional folder filtering, folder skips, WMS command dispatch, unchanged outcome classification, lease scope, cancellation, safe active-import failure responses, and correlation logging without an all-reference selector or callback runner.

### Tests for User Story 1

- [ ] T007 [P] [US1] After T010 creates `WarehouseOneCSource`, retarget only the existing Warehouse projection, optional folder filtering, key lookup, current-object cardinality/DataVersion shape, fallback-code shape, and Warehouse folder behavior in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`, leaving common transport coverage in T006 and adding no Warehouse matrix; this task depends on T010.
- [ ] T008 [P] [US1] After T011 creates `WarehouseOneCImport`, retarget only the existing Warehouse-specific mapping, fallback code, folder accounting, unchanged/batch accounting, cancellation, lease, and logging-safety cases in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`, preserving incomplete results after an import has started while leaving shared response construction, error conversion, and the single error-cap scenario in T006; this task depends on T011.
- [ ] T009 [P] [US1] After T012 creates `WarehouseOneCSynchronizer`, retarget the existing representative Warehouse synchronize-one outcomes and caller-cancellation coverage in `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs`, preserving folder, not-found, transport, WMS-result, and inconsistent-accounting behavior without copying the full outcome theory to other slices; this task depends on T012.

### Implementation for User Story 1

- [ ] T010 [US1] Define the Warehouse source record, exact `$select`, stable ordering, optional full-import folder filter, key lookup, current-object cardinality/DataVersion validation, and probe in `Myrmex.Integrations/OneC/Warehouses/WarehouseSourceRecord.cs` and `Myrmex.Integrations/OneC/Warehouses/WarehouseOneCSource.cs` using `IOneCODataTransport`.
- [ ] T011 [P] [US1] Implement the narrow `IWarehouseOneCImport.ImportAsync` flow in `Myrmex.Integrations/OneC/Warehouses/WarehouseOneCImport.cs`, preserving pre-start integration-wide validation and 400 behavior, full-operation Warehouse lease and 409 behavior, folder handling, existing `ImportWarehouses.Command` mapping/accounting, incomplete active-import error/cancellation responses, safe logs, and lease release.
- [ ] T012 [P] [US1] Implement `IWarehouseOneCSynchronizer.SynchronizeAsync` in `Myrmex.Integrations/OneC/Warehouses/WarehouseOneCSynchronizer.cs`, preserving non-waiting same-type coordination, current-object/folder/not-found/source/cancellation behavior, one existing `ImportWarehouses.Command` dispatch, local outcome interpretation, and `Processed != 1` or inconsistent counts as permanent `ApplicationFailure` with `retrySuitable = false`.
- [ ] T013 [US1] Implement `WarehouseReferenceSynchronizationHandler` in `Myrmex.Integrations/OneC/Warehouses/WarehouseReferenceSynchronizationHandler.cs` with the exact flow `parse ExternalId -> call IWarehouseOneCSynchronizer -> log -> pure map`, logging one structured result containing `SynchronizationRequestId`, `EntityType`, `ExternalId`, Base64 `NotifiedDataVersion`, `CurrentOutcome`, `CurrentReason`, and `RetrySuitable`, including the equivalent permanent invalid-request result and excluding credentials, secrets, and payloads.
- [ ] T014 [US1] Rewire the Warehouse route and registrations in `Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs` and `Myrmex.Integrations/OneC/OneCIntegrationModule.cs` directly to the Warehouse import/source/synchronizer/handler contracts, then remove Warehouse methods and typed reads from `Myrmex.Integrations/OneC/Imports/IOneCImportService.cs`, `Myrmex.Integrations/OneC/Imports/OneCImportService.cs`, `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`, `Myrmex.Integrations/OneC/Transport/IOneCODataClient.cs`, and `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`; retain the UoM and SKU typed client operations temporarily, and leave no Warehouse compatibility facade or parallel path.

**Checkpoint**: Warehouse is independently understandable and testable through its explicit source, import, synchronizer, and handler while public behavior remains unchanged.

---

## Phase 4: User Story 2 - Understand and Change Unit of Measure Integration Locally (Priority: P2)

**Goal**: Give Unit of Measure source loading, manual import, synchronize-one behavior, durable handling, mapping, classification, and diagnostics one explicit owner with no folder semantics.

**Independent Test**: Trace the existing UoM import route and durable handler into `OneC/UnitsOfMeasure/` and verify exact source projection/fallbacks, WMS dispatch, outcomes, coordination, cancellation, active-import error compatibility, and correlation logging without a folder branch or shared reference workflow.

### Tests for User Story 2

- [ ] T015 [P] [US2] After T018 creates `UnitOfMeasureOneCSource`, retarget only the existing UoM projection, Unicode fields, key lookup, current-object cardinality/DataVersion shape, fallback shape, and explicit no-folder behavior in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`, leaving common transport coverage in T006 and adding no folder case or per-reference matrix; this task depends on T018.
- [ ] T016 [P] [US2] After T019 creates `UnitOfMeasureOneCImport`, retarget only the existing UoM-specific full-name/symbol fallback, mapping, accounting, cancellation, and lease cases in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`, preserving incomplete results after an import has started while leaving shared response construction, error conversion, and the single error-cap scenario in T006; this task depends on T019.
- [ ] T017 [P] [US2] After T020 creates `UnitOfMeasureOneCSynchronizer`, retarget only the existing UoM-specific synchronize-one rows in `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs`, retaining the shared representative outcome coverage and verifying the existing absence of folder handling without duplicating Warehouse tests; this task depends on T020.

### Implementation for User Story 2

- [ ] T018 [US2] Define the UoM source record, exact Unicode `$select`, stable ordering, key lookup, current-object cardinality/DataVersion validation, and probe in `Myrmex.Integrations/OneC/UnitsOfMeasure/UnitOfMeasureSourceRecord.cs` and `Myrmex.Integrations/OneC/UnitsOfMeasure/UnitOfMeasureOneCSource.cs` using `IOneCODataTransport` and no folder field, filter, or outcome.
- [ ] T019 [P] [US2] Implement the narrow `IUnitOfMeasureOneCImport.ImportAsync` flow in `Myrmex.Integrations/OneC/UnitsOfMeasure/UnitOfMeasureOneCImport.cs`, preserving pre-start validation/400, full-operation UoM lease/409, existing `ImportUnitsOfMeasure.Command` mapping and fallbacks, accounting, incomplete active-import error/cancellation responses, safe logs, and lease release.
- [ ] T020 [P] [US2] Implement the cross-slice-approved `IUnitOfMeasureOneCSynchronizer.SynchronizeAsync` contract in `Myrmex.Integrations/OneC/UnitsOfMeasure/UnitOfMeasureOneCSynchronizer.cs`, preserving busy/not-found/source/cancellation behavior, one existing `ImportUnitsOfMeasure.Command` dispatch, local classification with no folder branch, and inconsistent counts as permanent `ApplicationFailure` with `retrySuitable = false`.
- [ ] T021 [US2] Implement `UnitOfMeasureReferenceSynchronizationHandler` in `Myrmex.Integrations/OneC/UnitsOfMeasure/UnitOfMeasureReferenceSynchronizationHandler.cs` with the exact parse-call-log-map flow and the required seven structured fields, Base64 notified version, invalid-request log, typed logger, pure mapper dependency, and no credentials, secrets, or payloads.
- [ ] T022 [US2] Rewire the UoM route and registrations in `Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs` and `Myrmex.Integrations/OneC/OneCIntegrationModule.cs` directly to the UoM contracts, update the still-unmigrated SKU repair in `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs` to depend directly on `IUnitOfMeasureOneCSynchronizer`, and minimally update temporary SKU consumers in `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs` and `Myrmex.Tests/Integrations/OneC/References/StockKeepingUnitReferenceRepairTests.cs` only as required to keep the intermediate repository consistent. Remove UoM methods and typed reads from `Myrmex.Integrations/OneC/Imports/IOneCImportService.cs`, `Myrmex.Integrations/OneC/Imports/OneCImportService.cs`, `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`, `Myrmex.Integrations/OneC/Transport/IOneCODataClient.cs`, and `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`; retain SKU typed operations until T031, leave no UoM facade or parallel path, and defer the full SKU test migration to T023-T026.

**Checkpoint**: UoM is independently understandable and testable, has no folder semantics, and exposes only its synchronize-one contract to the later SKU slice.

---

## Phase 5: User Story 3 - Keep SKU Processing and Dependency Repair Explicit (Priority: P3)

**Goal**: Give SKU paging, batching, partial accounting, synchronize-one behavior, durable handling, and bounded direct UoM repair one explicit owner.

**Independent Test**: Trace the SKU import route and durable handler into `OneC/StockKeepingUnits/` and verify stable paging, committed partial results, folder handling, exact outcomes, whole-operation SKU lease, one direct UoM call, at most one SKU retry, no recursion, and complete correlation logging.

### Tests for User Story 3

- [ ] T023 [P] [US3] After T029 creates the new `StockKeepingUnitOneCSynchronizer`, retarget `Myrmex.Tests/Integrations/OneC/References/StockKeepingUnitReferenceRepairTests.cs` to that synchronizer; parameterize the successful repair test for UoM `Applied` and `Unchanged`, rename the existing second test to state that UoM synchronization succeeds but the single SKU retry still reports missing/inactive UoM and stops permanently, and add one compact parameterized method for `Busy`, `TransientFailure`, `NotFound`, `ControlledSkip`, and `PermanentFailure`, mapping the first two to transient SKU failure and the last three to permanent SKU failure while every row asserts one UoM call, one SKU dispatch, no SKU retry, and no recursion/additional dependency call; this task depends on T029.
- [ ] T024 [P] [US3] After T027 creates `StockKeepingUnitOneCSource`, retarget only the existing SKU projection, stable paging/returned-count offset, base-UoM key, key lookup, current-object cardinality/DataVersion shape, empty terminal page, and partial terminal-page behavior in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`, leaving common transport coverage in T006 and adding no source test class; this task depends on T027.
- [ ] T025 [P] [US3] After T028 creates `StockKeepingUnitOneCImport`, retarget only the existing SKU-specific folder mapping/accounting, page dispatch, large collection, committed partial failure/cancellation, whole-operation lease, and idempotent retry cases in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`, preserving incomplete results after an import has started while leaving shared response construction, error conversion, and the single error-cap scenario in T006; this task depends on T028.
- [ ] T026 [P] [US3] After T029 creates `StockKeepingUnitOneCSynchronizer`, retarget the existing SKU-specific outcome and cancellation cases in `Myrmex.Tests/Integrations/OneC/References/OneCReferenceSynchronizationServiceTests.cs` and delete only the test whose purpose is to verify the removed central `OneCReferenceType` selector, without replacing it with three equivalent tests; this task depends on T029.

### Implementation for User Story 3

- [ ] T027 [US3] Define the SKU source record, exact `$select`, stable `Ref_Key` paging, configured page size, returned-count offset advancement, folder filtering, key lookup, current-object cardinality/DataVersion validation, and probe in `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitSourceRecord.cs` and `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitOneCSource.cs` using `IOneCODataTransport`.
- [ ] T028 [P] [US3] Implement the narrow `IStockKeepingUnitOneCImport.ImportAsync` flow in `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitOneCImport.cs`, preserving integration-wide pre-start validation/400, whole-operation SKU lease/409 across all pages and batches, folder-only pages, existing `ImportStockKeepingUnits.Command` mapping, committed partial counts/errors, incomplete active-import failure/cancellation responses, safe logs, and lease release.
- [ ] T029 [P] [US3] Implement `IStockKeepingUnitOneCSynchronizer.SynchronizeAsync` in `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitOneCSynchronizer.cs` with direct dependency only on `IUnitOfMeasureOneCSynchronizer`, preserving folder/not-found/source/cancellation outcomes, repair eligibility, exactly one UoM call and at most one identical SKU retry, failed-UoM transient/permanent mapping, no recursion, and inconsistent counts as permanent `ApplicationFailure` with `retrySuitable = false`.
- [ ] T030 [US3] Implement `StockKeepingUnitReferenceSynchronizationHandler` in `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitReferenceSynchronizationHandler.cs` with the exact parse-call-log-map flow and the required seven structured fields, Base64 notified version, invalid-request log, typed logger, pure mapper dependency, and no credentials, secrets, or payloads.
- [ ] T031 [US3] Rewire the SKU route and registrations in `Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs` and `Myrmex.Integrations/OneC/OneCIntegrationModule.cs` directly to the SKU import/source/synchronizer/handler contracts, then remove SKU methods and typed reads from `Myrmex.Integrations/OneC/Imports/IOneCImportService.cs`, `Myrmex.Integrations/OneC/Imports/OneCImportService.cs`, `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`, `Myrmex.Integrations/OneC/Transport/IOneCODataClient.cs`, and `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs` so no SKU facade or typed client operation remains; T036 removes the now-unused client files after final caller verification.

**Checkpoint**: All three reference slices are explicit, and the SKU-to-UoM dependency is bounded, direct, and fully owned by the SKU synchronizer.

---

## Phase 6: Polish & Cross-Cutting Compatibility

**Purpose**: Finish integration-wide connection composition, minimally rewire shared fixtures, remove obsolete paths, and perform static acceptance review.

- [ ] T032 After T033 creates `OneCConnectionTest`, retarget only the existing three-probe connection orchestration cases in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs` and the existing connection endpoint cases in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCEndpointTests.cs`, preserving connection-route `400/502/504` Problem Details without reclaiming common transport cases from T006 or applying those responses to active manual imports; this task depends on T033.
- [ ] T033 Implement the integration-wide three-source probe and logging in `Myrmex.Integrations/OneC/Connection/OneCConnectionTest.cs`, then inject it from the unchanged connection route in `Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs` while keeping all reference-specific query and mapping knowledge in the three slice sources.
- [ ] T034 [P] After T033 creates and wires `OneCConnectionTest`, minimally rewire existing three-route response, pre-start 400, same-type 409/cross-type independence, stable serialization, and authorization setup in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCEndpointTests.cs` to the three narrow import contracts; do not add active-failure `502/504` expectations or a new endpoint matrix; this task depends on T033.
- [ ] T035 [P] After T033 creates and wires `OneCConnectionTest`, minimally rewire the existing 1C import endpoint fixtures in `Myrmex.Tests/Integrations/Authorization/IntegrationAuthorizationEndpointTests.cs` to the three narrow import contracts and `OneCConnectionTest` while preserving current 401/403 coverage and leaving notification authentication tests unchanged; this task depends on T033.
- [ ] T036 Complete composition and removal in `Myrmex.Integrations/OneC/OneCIntegrationModule.cs` by registering one typed common transport, one singleton gate, three sources, three imports, three synchronizers, three concrete `ISynchronizationHandler` implementations, and `OneCConnectionTest`; delete `Myrmex.Integrations/OneC/Imports/IOneCImportService.cs`, `Myrmex.Integrations/OneC/Imports/OneCImportService.cs`, `Myrmex.Integrations/OneC/References/OneCReferenceSynchronizationService.cs`, `Myrmex.Integrations/OneC/References/ReferenceSynchronizationHandlers.cs`, `Myrmex.Integrations/OneC/Transport/IOneCODataClient.cs`, `Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`, `Myrmex.Integrations/OneC/Transport/Catalog_Склады.cs`, `Myrmex.Integrations/OneC/Transport/Catalog_УпаковкиЕдиницыИзмерения.cs`, `Myrmex.Integrations/OneC/Transport/Catalog_Номенклатура.cs`, and the placeholder `Myrmex.Integrations/OneC/OneCOptions.cs` only after confirming all temporary typed-client consumers have been removed.
- [ ] T037 Perform the static ownership and compatibility review in `specs/111-onec-reference-vertical-slices/quickstart.md` against `specs/111-onec-reference-vertical-slices/contracts/manual-import-compatibility.md`, `specs/111-onec-reference-vertical-slices/contracts/reference-synchronization-compatibility.md`, and `specs/111-onec-reference-vertical-slices/contracts/slice-operation-boundaries.md`, confirming no composite selector/delegate runner/facade remains, active manual failures are incomplete 200 responses, handler logs have a concrete owner and all required safe fields, inconsistent accounting is unchanged, public/UI/domain/persistence/durable contracts are untouched, and no build/test/runtime command is executed as part of the task.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Can start immediately on the existing branch and projects.
- **Phase 2 (Foundational)**: Depends on T001 and blocks every user story.
- **Phase 3 (US1 Warehouse)**: Depends on Phase 2 and is the MVP increment; T007 depends on T010, T008 on T011, and T009 on T012.
- **Phase 4 (US2 UoM)**: Depends on Phase 2; T015 depends on T018, T016 on T019, and T017 on T020, while T022 follows T014 to avoid conflicts in the shared endpoint, composition root, typed-client implementation, and temporary composite files.
- **Phase 5 (US3 SKU)**: Depends on T020 establishing the explicit UoM synchronize-one contract and follows T022 in the no-facade migration sequence; T024 depends on T027, T025 on T028, and T023/T026 on T029.
- **Phase 6 (Polish)**: Depends on all three slice phases; T032, T034, and T035 depend on T033 creating and wiring `OneCConnectionTest`.

### User Story Dependencies

```text
Setup T001
  -> Foundation T002-T006
       -> US1 Warehouse
            T010 -> T007
            T010 -> T011 -> T008
            T010 -> T012 -> T009 and T013
            T013 -> T014
       -> US2 UoM
            T018 -> T015
            T018 -> T019 -> T016
            T018 -> T020 -> T017 and T021
            T021 -> T022
       -> US3 SKU
            T027 -> T024
            T027 -> T028 -> T025
            T027 -> T029 -> T023, T026, and T030
            T030 -> T031
       -> Cross-cutting
            T033 -> T032, T034, and T035
            T032/T034/T035 -> T036 -> T037
```

- **US1 (P1)**: Has no business dependency on another story after the shared foundation.
- **US2 (P2)**: Has no business dependency on Warehouse, but T022 follows T014 because both migrate shared composition and transitional typed-client files.
- **US3 (P3)**: Depends directly on the UoM synchronize-one contract from T020; T022 keeps the temporary SKU production and test consumers internally consistent until T029 creates the new SKU synchronizer and T023/T026 retarget its existing tests.

### Within Each User Story

- Retarget each existing test only after its target production contract and implementation exist, or perform the implementation and its minimal test retargeting in one controlled batch. Every completed task must leave production and test references internally consistent.
- Command execution remains developer-controlled.
- Complete T004 before T005, and complete T002 and T003 before the common-mechanism retargeting in T006.
- Create the source record/source before the import and synchronizer that consume it.
- After a source exists, import and synchronize-one implementations may proceed in parallel, followed by their respective test retargets.
- Complete the concrete handler after its matching synchronizer contract exists.
- Rewire endpoint/DI callers and remove that reference's old composite methods in the same integration task so no migrated reference has parallel production paths.

## Parallel Opportunities

- T004 can proceed in parallel with T002 and T003 because it uses separate synchronization files; T005 must wait for T004, while T006 waits for T002 and T003 and can then proceed independently of T004/T005.
- After T010 creates the Warehouse source, T007 may retarget its source tests while T011 and T012 implement import and synchronization in parallel; T008 follows T011 and T009 follows T012.
- After T018 creates the UoM source, T015 may retarget its source tests while T019 and T020 implement import and synchronization in parallel; T016 follows T019 and T017 follows T020.
- After T027 creates the SKU source, T024 may retarget its source tests while T028 and T029 implement import and synchronization in parallel; T025 follows T028, while T023 and T026 may proceed in parallel only after T029.
- After T033 creates and wires `OneCConnectionTest`, T035 can proceed in parallel with work in `OneCEndpointTests.cs`; T032 and T034 both edit `OneCEndpointTests.cs` and must be serialized.
- Core Warehouse and UoM slice-file work can be assigned concurrently after Phase 2, but their shared-file integration tasks T014 and T022 must remain ordered.

## Parallel Examples

### User Story 1

```text
T010 Implement WarehouseOneCSource.cs

After T010, in parallel where files do not overlap:
T007 Retarget Warehouse source coverage in OneCODataClientTests.cs
T011 Implement WarehouseOneCImport.cs -> T008 Retarget Warehouse import coverage
T012 Implement WarehouseOneCSynchronizer.cs -> T009 Retarget Warehouse synchronization coverage
```

### User Story 2

```text
T018 Implement UnitOfMeasureOneCSource.cs

After T018, in parallel where files do not overlap:
T015 Retarget UoM source coverage in OneCODataClientTests.cs
T019 Implement UnitOfMeasureOneCImport.cs -> T016 Retarget UoM import coverage
T020 Implement UnitOfMeasureOneCSynchronizer.cs -> T017 Retarget UoM synchronization coverage
```

### User Story 3

```text
T027 Implement StockKeepingUnitOneCSource.cs

After T027, in parallel where files do not overlap:
T024 Retarget SKU source coverage in OneCODataClientTests.cs
T028 Implement StockKeepingUnitOneCImport.cs -> T025 Retarget SKU import coverage
T029 Implement StockKeepingUnitOneCSynchronizer.cs -> T023 Retarget the repair tests and T026 retarget synchronization coverage
```

### Cross-Cutting Connection Work

```text
T033 Implement and wire OneCConnectionTest.cs
T033 -> T032 Retarget connection orchestration/endpoint cases -> T034 Finish the remaining OneCEndpointTests.cs fixture rewiring
T033 -> T035 Retarget IntegrationAuthorizationEndpointTests.cs in parallel with the serialized T032/T034 chain
```

## Implementation Strategy

### MVP First

1. Complete T001-T006 to establish only the approved common mechanisms and their single representative test owners.
2. Create the Warehouse source in T010, then implement T011 and T012 in parallel while retargeting T007 only after T010.
3. Retarget T008 only after T011 and T009 only after T012, then complete T013 and T014.
4. Stop for developer-controlled review/validation of the independently understandable Warehouse import and synchronize-one paths.

### Incremental Delivery

1. **Foundation**: Common transport, gate, response construction, result primitives, pure mapping, and their single representative existing test owners; keep the typed client as a temporary adapter over the common transport for unmigrated references.
2. **Warehouse MVP**: Create each Warehouse production target before its existing tests are retargeted, then rewire Warehouse and remove its old composite methods in the same increment.
3. **UoM increment**: Create each UoM production target before its existing tests are retargeted, expose its one explicit synchronize-one contract, and remove its old composite methods.
4. **SKU increment**: Create the SKU source/import/synchronizer before their corresponding existing tests are retargeted, retarget the repair suite specifically to the new synchronizer, enforce one dependency call/one retry, and remove the remaining composite methods.
5. **Final cleanup**: Create and wire `OneCConnectionTest` before retargeting its connection/endpoint/authorization fixtures, then delete obsolete files/registrations and complete static compatibility review.

## Guardrails

- Keep `ImportWarehouses.Command`, `ImportUnitsOfMeasure.Command`, and `ImportStockKeepingUnits.Command` as the application boundaries.
- Keep `Myrmex.Integrations/Synchronization/`, notification intake, public/shared contracts, WebApp code/localization, WMS domain/application code, persistence mappings, schema, migrations, and model snapshots unchanged.
- Keep manual pre-start errors at existing `400`/`409`/platform `401/403` boundaries; keep active manual source/application failures as incomplete `200 OneCImportResponse`; keep connection-test transport Problem Details.
- Keep `Processed != 1` or inconsistent counts as permanent `ApplicationFailure` with `retrySuitable = false`; any reconsideration belongs to another issue.
- Keep handler correlation logging in each concrete handler and keep `ReferenceSynchronizationHandlerResultMapper` pure.
- Every completed task or phase must leave all remaining production and test references internally consistent. Temporary adapters are allowed only for not-yet-migrated references and must be removed by their assigned integration task.
- Do not introduce a type/provider switch, generic workflow/dependency runner, compatibility facade, second durable foundation, new public endpoint, UI action, schema change, or distributed coordination.
- Do not add tests merely because production classes move or split, and do not execute developer-controlled validation commands from this task list.
