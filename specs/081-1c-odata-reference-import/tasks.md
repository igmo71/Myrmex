# Tasks: 1C OData Reference Import MVP

**Input**: Design documents from `specs/081-1c-odata-reference-import/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/onec-integration.openapi.yaml`, `quickstart.md`

**Tests**: Risk-based automated tests are required for changed domain identity/lifecycle rules, SQL Server mappings and unique indexes, WMS batch transactions, OData queries/deserialization/paging, orchestration/locking/counting, new endpoint contracts, and WebApp client behavior. The page itself uses the manual smoke exception documented in `plan.md`.

**Organization**: Tasks are grouped by user story. Shared import metadata and public result contracts are foundational; OneC transport, WMS handlers, endpoints, clients, and UI actions stay in the earliest user story that needs them.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it changes distinct files and has no incomplete dependency.
- **[Story]**: Maps the task to a specification user story.
- Every task includes exact repository-relative file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the integration project and solution dependency direction without adding runtime behavior.

- [X] T001 Create the .NET 10 integration adapter project with ASP.NET framework access, references to `Myrmex.Modules.Wms`, `Myrmex.AppDispatching`, `Myrmex.AspNetCore`, `Myrmex.Core`, and `Myrmex.Shared`, and test internals visibility in `src/Myrmex.Integrations/Myrmex.Integrations.csproj` and `src/Myrmex.Integrations/Properties/AssemblyInfo.cs`
- [X] T002 Add the integration project to the solution and one-way host/test references without adding a WMS-to-integration reference in `Myrmex.slnx`, `Myrmex.ApiService/Myrmex.ApiService.csproj`, and `Myrmex.Tests/Myrmex.Tests.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add WMS-owned imported identity, persistence constraints, and neutral result contracts required by every import story.

**⚠️ CRITICAL**: No import user story can complete until this phase is finished.

### Tests for Foundational Behavior

- [X] T003 [P] Add warehouse domain tests protecting immutable `ExternalRefKey`, successful `LastImportedAtUtc` updates, imported code/name updates, and source-driven deactivate/reactivate behavior in `Myrmex.Tests/Wms/Topology/Domain/WarehouseTests.cs`
- [X] T004 [P] Add UoM domain tests protecting immutable `ExternalRefKey`, successful `LastImportedAtUtc` updates, imported code/name/symbol updates, and source-driven lifecycle behavior in `Myrmex.Tests/Wms/Catalog/Domain/UnitOfMeasureTests.cs`
- [X] T005 [P] Add SKU domain tests protecting immutable `ExternalRefKey`, successful `LastImportedAtUtc` updates, imported code/name/base-UoM updates, and source-driven lifecycle behavior in `Myrmex.Tests/Wms/Catalog/Domain/StockKeepingUnitTests.cs`
- [X] T006 [P] Add SQL Server model metadata tests for nullable warehouse import columns and filtered unique `ExternalRefKey` index in `Myrmex.Tests/Wms/Topology/Persistence/WarehousePersistenceTests.cs`
- [X] T007 [P] Extend SQL Server model metadata tests for nullable UoM import columns and filtered unique `ExternalRefKey` index in `Myrmex.Tests/Wms/Catalog/Persistence/UnitOfMeasurePersistenceTests.cs`
- [X] T008 [P] Extend SQL Server model metadata tests for nullable SKU import columns and filtered unique `ExternalRefKey` index while preserving the internal `BaseUnitOfMeasureId` relationship in `Myrmex.Tests/Wms/Catalog/Persistence/StockKeepingUnitPersistenceTests.cs`

### Foundational Implementation

- [X] T009 [P] Add `ExternalRefKey`, `LastImportedAtUtc`, and domain methods for imported detail/lifecycle application without exposing 1C names in `Myrmex.Modules.Wms/Topology/Domain/Warehouses/Warehouse.cs`
- [X] T010 [P] Add `ExternalRefKey`, `LastImportedAtUtc`, and domain methods for imported detail/lifecycle application without exposing 1C names in `Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs`
- [X] T011 [P] Add `ExternalRefKey`, `LastImportedAtUtc`, and domain methods for imported detail/base-UoM/lifecycle application while keeping Myrmex `Id` and `BaseUnitOfMeasureId` internal in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`
- [X] T012 Add explicit external-reference index names for warehouses, UoMs, and SKUs in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- [X] T013 [P] Configure nullable warehouse import fields and unique filtered `[ExternalRefKey] IS NOT NULL` index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/WarehouseConfiguration.cs`
- [X] T014 [P] Configure nullable UoM import fields and unique filtered `[ExternalRefKey] IS NOT NULL` index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/UnitOfMeasureConfiguration.cs`
- [X] T015 [P] Configure nullable SKU import fields and unique filtered `[ExternalRefKey] IS NOT NULL` index without changing the required base-UoM foreign key in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`
- [X] T016 Extend unique-constraint error mapping for the three external-reference indexes while preserving existing code-conflict mappings in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsPersistenceExceptionMapper.cs`
- [X] T017 [P] Define neutral committed-batch counts and stable record-error types with no OData names in `Myrmex.Modules.Wms/Catalog/Features/Imports/ReferenceImportBatchResult.cs`
- [X] T018 [P] Add BCL-only public connection/import response, operation-error, and record-error contracts in `Myrmex.Shared/Integrations/OneC/OneCConnectionTestResponse.cs`, `Myrmex.Shared/Integrations/OneC/OneCImportResponse.cs`, `Myrmex.Shared/Integrations/OneC/OneCImportOperationError.cs`, and `Myrmex.Shared/Integrations/OneC/OneCImportRecordError.cs`

**Checkpoint**: WMS entities and shared contracts can represent imported identity safely, while 1C DTOs remain absent from WMS and public contracts.

---

## Phase 3: User Story 1 - Verify the 1C Connection (Priority: P1) 🎯 MVP

**Goal**: Let an authenticated operator verify configured connectivity, credentials, and all required entity sets before importing data.

**Independent Test**: With a stub or configured publication, invoke `POST /api/integrations/1c/connection/test` and distinguish ready, disabled/incomplete configuration, authentication failure, timeout/unavailability, malformed response, and missing entity-set outcomes without exposing secrets.

### Tests for User Story 1

- [X] T019 [P] [US1] Add transport tests protecting configuration validation, Basic-auth header handling without secret leakage, cancellation, timeout, malformed envelope handling, and one-record checks for all configured entity sets in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`
- [X] T020 [P] [US1] Add focused endpoint tests protecting the connection-test route, no-body POST binding, authenticated-actor requirement, success serialization, and safe ProblemDetails statuses/codes in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCEndpointTests.cs`
- [X] T021 [P] [US1] Add WebApp client tests protecting the connection-test URL, cancellation propagation, shared response parsing, and existing ProblemDetails-to-`ApiResult` mapping in `Myrmex.Tests/Integrations/OneC/Web/OneCIntegrationApiClientTests.cs`

### Implementation for User Story 1

- [X] T022 [P] [US1] Implement `OneCOptions` with secure connection settings, `Catalog_УпаковкиЕдиницыИзмерения` as the documented UoM entity-set value, `WarehouseCodeAvailable`, `UseFolderFilter`, batch limits, and timeout defaults—without any default SKU UoM option—in `src/Myrmex.Integrations/OneC/Configuration/OneCOptions.cs`
- [X] T023 [P] [US1] Implement the internal OData `value` envelope and categorized safe transport failure types in `src/Myrmex.Integrations/OneC/Transport/OneCODataCollectionResponse.cs` and `src/Myrmex.Integrations/OneC/Transport/OneCTransportException.cs`
- [X] T024 [US1] Implement the typed HTTP client foundation, secure credential header, per-request timeout/cancellation, envelope parsing, and required-entity-set connection probes in `src/Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`
- [X] T025 [US1] Register OneC options, typed HTTP client, `TimeProvider`, and integration services while keeping secrets out of repository configuration in `src/Myrmex.Integrations/OneC/OneCIntegrationModule.cs` and `Myrmex.ApiService/appsettings.json`
- [X] T026 [US1] Implement the `/api/integrations/1c/connection/test` endpoint with existing actor checks and connection error-to-ProblemDetails mapping in `src/Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs`
- [X] T027 [US1] Register and map the OneC integration module without adding authentication/authorization baseline services in `Myrmex.ApiService/Program.cs`
- [X] T028 [US1] Implement the typed WebApp connection-test call using shared contracts and existing `ApiResult<T>` HTTP helpers in `Myrmex.WebApp/Integrations/OneC/OneCIntegrationApiClient.cs`
- [X] T029 [US1] Register `OneCIntegrationApiClient` with the existing API service-discovery base address in `Myrmex.WebApp/Program.cs`
- [X] T030 [US1] Add `Интеграции → 1С` navigation and a Russian connection-test page with synchronous progress and safe success/error rendering in `Myrmex.WebApp/Components/Layout/NavMenu.razor` and `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`

**Checkpoint**: User Story 1 is independently usable as a connection/readiness diagnostic without any reference mutation.

---

## Phase 4: User Story 2 - Import Warehouses and Units of Measure (Priority: P2)

**Goal**: Import warehouses and `Catalog_УпаковкиЕдиницыИзмерения` UoMs as separate idempotent operations with source identity, lifecycle, conflicts, and summaries.

**Independent Test**: Import new, linked, deletion-marked, folder, invalid, and local-code-conflicting warehouse/UoM samples; verify exact mappings, warehouse-only generated codes, committed counts, record errors, and unchanged unrelated local data.

### Tests for User Story 2

- [X] T031 [P] [US2] Add warehouse batch-handler tests protecting create/update by `ExternalRefKey`, no code linking, local-code conflict skips, deletion/reactivation, generated-code acceptance, one-transaction rollback, and reconciled committed counts in `Myrmex.Tests/Wms/Topology/Features/Imports/ImportWarehousesHandlerTests.cs`
- [X] T032 [P] [US2] Add UoM batch-handler tests protecting create/update by `ExternalRefKey`, trimmed required codes, source-owned name/symbol, no code linking, deletion/reactivation, transaction rollback, and committed counts in `Myrmex.Tests/Wms/Catalog/Features/Imports/ImportUnitsOfMeasureHandlerTests.cs`
- [X] T033 [P] [US2] Extend OData client tests for exact warehouse `$select` variants, optional `$filter=IsFolder eq false`, exact `Catalog_УпаковкиЕдиницыИзмерения` `$select`, Unicode field deserialization, and cancellation in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`
- [X] T034 [P] [US2] Add orchestration tests for `Description -> Warehouse.Name`, warehouse-only uppercase `Ref_Key` `N` code fallback, UoM full-name/symbol fallback rules, pending `SourceFolder` skips, complete summaries, and error caps in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`
- [X] T035 [P] [US2] Extend endpoint tests for separate warehouse/UoM routes, no-body POST binding, complete/incomplete response serialization, and pre-start failures in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCEndpointTests.cs`
- [X] T036 [P] [US2] Extend WebApp client tests for separate warehouse/UoM URLs, shared summary parsing, and ProblemDetails mapping in `Myrmex.Tests/Integrations/OneC/Web/OneCIntegrationApiClientTests.cs`

### Implementation for User Story 2

- [X] T037 [P] [US2] Implement the internal warehouse DTO with `Guid Ref_Key`, `DeletionMark`, `IsFolder`, optional `Code`, and `Description` in `src/Myrmex.Integrations/OneC/Transport/Catalog_Склады.cs`
- [X] T038 [P] [US2] Implement the internal `Catalog_УпаковкиЕдиницыИзмерения` DTO with `Guid Ref_Key`, `DeletionMark`, `Code`, `Description`, `НаименованиеПолное`, and `МеждународноеСокращение` in `src/Myrmex.Integrations/OneC/Transport/Catalog_УпаковкиЕдиницыИзмерения.cs`
- [X] T039 [P] [US2] Implement neutral warehouse import items and a WMS-owned batch handler with identity/code preloads, validation, lifecycle alignment, explicit transaction, one save/event-dispatch unit, and committed results in `Myrmex.Modules.Wms/Topology/Features/Imports/ImportWarehouses.cs`
- [X] T040 [P] [US2] Implement neutral UoM import items and a WMS-owned batch handler with identity/code preloads, validation, lifecycle alignment, explicit transaction, one save/event-dispatch unit, and committed results in `Myrmex.Modules.Wms/Catalog/Features/Imports/ImportUnitsOfMeasure.cs`
- [X] T041 [US2] Implement separate warehouse/UoM source reads with exact `$select` fields, optional folder filter, optional warehouse `Code`, deterministic ordering, and typed DTO parsing in `src/Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`
- [X] T042 [US2] Implement and register warehouse/UoM source-to-neutral mapping, folder-only batch completion, WMS dispatch, committed-count aggregation, and the 50-error cap in `src/Myrmex.Integrations/OneC/Imports/OneCImportService.cs` and `src/Myrmex.Integrations/OneC/OneCIntegrationModule.cs`
- [X] T043 [US2] Add synchronous warehouse and UoM import endpoints under `/api/integrations/1c` with actor checks and complete/incomplete result mapping in `src/Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs`
- [X] T044 [US2] Add warehouse and UoM import methods to the typed WebApp client in `Myrmex.WebApp/Integrations/OneC/OneCIntegrationApiClient.cs`
- [X] T045 [US2] Add separate Russian warehouse/UoM actions, in-progress state, latest summary, operation error, counts, and capped record-error rendering in `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`

**Checkpoint**: Warehouse and UoM imports are independently demonstrable. `Catalog_УпаковкиЕдиницыИзмерения.Ref_Key` is stored only as UoM `ExternalRefKey`; Myrmex `Id` remains internal.

---

## Phase 5: User Story 3 - Import Nomenclature as SKUs (Priority: P3)

**Goal**: Import more than 15,000 nomenclature records with deterministic paging and resolve each SKU base UoM from its own `ЕдиницаИзмерения_Key` by UoM `ExternalRefKey`.

**Independent Test**: Import a multi-page source containing folders, valid per-item UoM keys, missing/empty keys, not-imported keys, inactive UoMs, conflicts, deletion marks, and a later failed batch; verify only affected records fail and only committed batches contribute counts.

### Tests for User Story 3

- [X] T046 [P] [US3] Add SKU batch-handler tests protecting per-item `BaseUnitOfMeasureExternalRefKey` resolution by UoM `ExternalRefKey`, no code matching, missing/not-imported/inactive reasons, create/update/lifecycle behavior, unlinked deletion skips reported as `SourceRecordDeletionMarked`, linked deletion bypass of detail/base-UoM validation, batch rollback, and committed counts in `Myrmex.Tests/Wms/Catalog/Features/Imports/ImportStockKeepingUnitsHandlerTests.cs`
- [X] T047 [P] [US3] Extend OData client tests for the exact nomenclature `$select` including `IsFolder`, `НаименованиеПолное`, `Артикул`, and `ЕдиницаИзмерения_Key`, stable `$orderby=Ref_Key`, `$skip`/`$top`, folder filter compatibility, and empty/exact/multi-page termination in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs`
- [X] T048 [P] [US3] Extend orchestration tests for full-name fallback, folder skips, nullable per-SKU UoM-key mapping, more-than-15,000-record batching, later-source/batch failure, failed-batch exclusion, cancellation, and 50-error cap in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`
- [X] T049 [P] [US3] Extend endpoint tests for the SKU route and complete/incomplete summary serialization with stable base-UoM record reasons in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCEndpointTests.cs`
- [X] T050 [P] [US3] Extend WebApp client tests for the SKU import URL, shared response parsing, cancellation, and stable error preservation in `Myrmex.Tests/Integrations/OneC/Web/OneCIntegrationApiClientTests.cs`

### Implementation for User Story 3

- [X] T051 [P] [US3] Implement the internal nomenclature DTO with `Guid Ref_Key`, `DeletionMark`, `IsFolder`, `Code`, `Description`, `НаименованиеПолное`, transport-only `Артикул`, and nullable `Guid ЕдиницаИзмерения_Key` in `src/Myrmex.Integrations/OneC/Transport/Catalog_Номенклатура.cs`
- [X] T052 [US3] Implement neutral SKU import items with nullable `BaseUnitOfMeasureExternalRefKey` and a WMS handler that resolves active imported UoMs only by `ExternalRefKey`, never by code, skips/reports unlinked deletion-marked records as `SourceRecordDeletionMarked`, deactivates linked deletion-marked records without detail/base-UoM validation or updates, and preserves internal Myrmex IDs and atomic batch results in `Myrmex.Modules.Wms/Catalog/Features/Imports/ImportStockKeepingUnits.cs`
- [X] T053 [US3] Implement deterministic nomenclature paging with exact `$select`, optional folder filter, `$orderby=Ref_Key`, `$skip`, `$top`, returned-count offset advancement, and final-page termination in `src/Myrmex.Integrations/OneC/Transport/OneCODataClient.cs`
- [X] T054 [US3] Implement nomenclature folder handling, source-to-SKU-item mapping of `ЕдиницаИзмерения_Key`, paged WMS dispatch, committed-batch aggregation, incomplete operation errors, and failed-batch exclusion in `src/Myrmex.Integrations/OneC/Imports/OneCImportService.cs`
- [X] T055 [US3] Add the synchronous SKU import endpoint under `/api/integrations/1c/skus/import` in `src/Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs`
- [X] T056 [US3] Add SKU import support to the typed WebApp client in `Myrmex.WebApp/Integrations/OneC/OneCIntegrationApiClient.cs`
- [X] T057 [US3] Add the Russian nomenclature action and multi-batch complete/incomplete result display to `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`

**Checkpoint**: SKU import supports the target scale and preserves the corrected relationship model: `ЕдиницаИзмерения_Key -> BaseUnitOfMeasureExternalRefKey -> UnitOfMeasure.ExternalRefKey -> internal BaseUnitOfMeasureId`.

---

## Phase 6: User Story 4 - Repeat an Import Safely (Priority: P4)

**Goal**: Make reruns and concurrent attempts safe without duplicate records, queued jobs, or persistent history.

**Independent Test**: Run unchanged imports twice, retry after a later-batch failure, and attempt concurrent same-type imports; verify no duplicate source links, refreshed timestamps, retained earlier batches, immediate `409`, and gate release after completion/cancellation.

### Tests for User Story 4

- [ ] T058 [P] [US4] Add repeat-import regression tests proving identity-based updates, immutable `ExternalRefKey`, refreshed `LastImportedAtUtc`, no duplicate creation, and no code-based linking across `Myrmex.Tests/Wms/Topology/Features/Imports/ImportWarehousesHandlerTests.cs`, `Myrmex.Tests/Wms/Catalog/Features/Imports/ImportUnitsOfMeasureHandlerTests.cs`, and `Myrmex.Tests/Wms/Catalog/Features/Imports/ImportStockKeepingUnitsHandlerTests.cs`
- [ ] T059 [P] [US4] Extend orchestration tests for zero-timeout same-type rejection, different-type concurrency, gate release in `finally`, retained committed batches, and idempotent retry after partial failure in `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`
- [ ] T060 [P] [US4] Extend endpoint tests for `409 OneCImport.AlreadyInProgress` and unaffected different-reference-type requests in `Myrmex.Tests/Integrations/OneC/Endpoints/OneCEndpointTests.cs`

### Implementation for User Story 4

- [ ] T061 [P] [US4] Implement and register the singleton three-key, non-waiting, process-local import gate with disposable/finally-safe release semantics in `src/Myrmex.Integrations/OneC/Imports/OneCImportGate.cs` and `src/Myrmex.Integrations/OneC/OneCIntegrationModule.cs`
- [ ] T062 [US4] Apply the gate to all three import paths, preserve prior committed-batch counts on later failure, and return the stable already-running conflict without adding queues, polling, jobs, or distributed locking in `src/Myrmex.Integrations/OneC/Imports/OneCImportService.cs` and `src/Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs`
- [ ] T063 [US4] Disable only the running action, preserve the latest completed result, and show already-running/incomplete feedback without persistent history in `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`

**Checkpoint**: All four user stories work together, and reruns remain safe under the documented single-API-instance assumption.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete operational diagnostics, scope checks, and developer-controlled handoff without executing prohibited commands.

- [ ] T064 [P] Add tests proving credentials never appear in transport exception messages, operation errors, or structured log state in `Myrmex.Tests/Integrations/OneC/Client/OneCODataClientTests.cs` and `Myrmex.Tests/Integrations/OneC/Imports/OneCImportServiceTests.cs`
- [ ] T065 Add structured connection/import completion, duration, reference type, counts, and failure-category logs without credentials or source payloads in `src/Myrmex.Integrations/OneC/Transport/OneCODataClient.cs` and `src/Myrmex.Integrations/OneC/Imports/OneCImportService.cs`
- [ ] T066 [P] Verify public contracts match `specs/081-1c-odata-reference-import/contracts/onec-integration.openapi.yaml` and update only documented contract drift in `Myrmex.Shared/Integrations/OneC/` and `specs/081-1c-odata-reference-import/contracts/onec-integration.openapi.yaml`
- [ ] T067 Review the implementation against source mapping, single-instance locking, and non-goals; record any approved validation-note corrections in `specs/081-1c-odata-reference-import/quickstart.md`
- [ ] T068 Stop before EF migration generation/application and hand the developer the reviewed commands and expected `AddOneCExternalReferenceMetadata` artifacts documented in `specs/081-1c-odata-reference-import/quickstart.md`
- [ ] T069 Stop before build/test/startup/database/Docker/infrastructure execution and hand the developer the recommended validation commands from `specs/081-1c-odata-reference-import/quickstart.md`
- [ ] T070 Hand off the developer-controlled API/UI smoke scenarios for connection, warehouses, `Catalog_УпаковкиЕдиницыИзмерения`, per-SKU `ЕдиницаИзмерения_Key`, partial failure, concurrency, and rerun behavior in `specs/081-1c-odata-reference-import/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup; blocks every user story.
- **US1 (Phase 3)**: Depends on Foundation and supplies common OneC HTTP/options/endpoint/client/page scaffolding.
- **US2 (Phase 4)**: Depends on Foundation; backend WMS/DTO work can proceed beside US1, but story completion depends on the US1 integration/client/page scaffolding.
- **US3 (Phase 5)**: Depends on Foundation and US2's UoM import model for live acceptance; handler tests may seed imported UoMs directly.
- **US4 (Phase 6)**: Depends on completed warehouse, UoM, and SKU import paths from US2 and US3.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependency Graph

```text
Setup
  -> Foundation
       -> US1 Connection
       -> US2 Warehouses + UoMs
              -> US3 SKUs (uses imported UoM ExternalRefKey)
       -> US1 + US2 + US3
              -> US4 Safe Repeat/Concurrency
                     -> Polish/Handoff
```

### Within Each User Story

- Write the listed risk-owning tests before implementation and confirm they fail when the developer runs them.
- Implement DTOs/domain models before clients/handlers that consume them.
- Implement WMS handlers and OData reads before orchestration.
- Implement orchestration before endpoints and WebApp actions.
- Keep `Myrmex.Shared` transport records separate from WMS commands and OneC DTOs.
- Never map 1C `Ref_Key` into Myrmex `Id`; store it only as `ExternalRefKey`.
- Never resolve SKU base UoM by code or by a single configured default.

### Parallel Opportunities

- T003–T008 can run in parallel as test-first foundational work.
- T009–T011, T013–T015, T017, and T018 affect distinct files and can run in parallel after their tests/prerequisites.
- US1 transport, endpoint, and WebApp client tests (T019–T021) can run in parallel.
- US2 handler, transport, orchestration, endpoint, and WebApp client tests (T031–T036) can run in parallel; DTOs T037–T038 and handlers T039–T040 can also be split.
- US3 tests T046–T050 can run in parallel; DTO T051 can proceed beside the WMS handler work after the test contracts are fixed.
- US4 tests T058–T060 can run in parallel before gate implementation.

---

## Parallel Example: User Story 1

```text
Task T019: Protect connection/configuration/OData failure behavior in OneCODataClientTests.cs
Task T020: Protect connection route/auth/serialization behavior in OneCEndpointTests.cs
Task T021: Protect WebApp connection URL/result/error behavior in OneCIntegrationApiClientTests.cs
```

## Parallel Example: User Story 2

```text
Task T031: Protect warehouse WMS upsert/transaction behavior
Task T032: Protect UoM WMS upsert/transaction behavior
Task T033: Protect exact warehouse and Catalog_УпаковкиЕдиницыИзмерения source queries
Task T034: Protect source mapping, folder skips, and summary aggregation
```

## Parallel Example: User Story 3

```text
Task T046: Protect per-SKU UoM ExternalRefKey resolution in WMS
Task T047: Protect exact nomenclature paging and Unicode source fields
Task T048: Protect paged orchestration and partial-failure counts
Task T050: Protect WebApp SKU route and result mapping
```

## Parallel Example: User Story 4

```text
Task T058: Protect idempotent repeat behavior in all WMS import handlers
Task T059: Protect import gate release, concurrency, and retry aggregation
Task T060: Protect already-in-progress HTTP behavior
```

---

## Implementation Strategy

### MVP First: User Story 1

1. Complete Setup and Foundation.
2. Complete US1 connection testing end-to-end.
3. Stop for developer-controlled validation using `quickstart.md`.
4. Demonstrate source readiness before enabling mutations.

### Incremental Delivery

1. **Foundation**: imported identity, timestamps, filtered uniqueness, neutral/public contracts.
2. **US1**: safe source connection/readiness diagnostic.
3. **US2**: warehouses and corrected `Catalog_УпаковкиЕдиницыИзмерения` import.
4. **US3**: paged nomenclature import with per-record `ЕдиницаИзмерения_Key` resolution.
5. **US4**: safe reruns and process-local same-type concurrency rejection.
6. **Polish**: diagnostics, contract review, and developer-controlled migration/validation handoff.

### Suggested Team Split

- Developer A: integration transport/options/query construction.
- Developer B: WMS imported metadata, handlers, persistence, and tests.
- Developer C: shared contracts, endpoints, WebApp client/page, and boundary tests.
- Integrate at each story checkpoint; do not merge parallel edits to the same orchestration/client/page files without sequencing.

---

## Notes

- Builds, tests, migration generation/application, database updates, application startup, Docker, and infrastructure commands remain developer-controlled and were not run during task generation.
- `Catalog_УпаковкиЕдиницыИзмерения.Ref_Key` becomes `UnitOfMeasure.ExternalRefKey`; it never becomes Myrmex `Id`.
- `Catalog_Номенклатура.ЕдиницаИзмерения_Key` becomes the neutral SKU item's `BaseUnitOfMeasureExternalRefKey`, then WMS resolves it to the internal `BaseUnitOfMeasureId` by UoM `ExternalRefKey` only.
- Source-folder skips are held with their source batch and counted only after that batch completes.
- The generated warehouse code fallback is limited to warehouses and uses uppercase 32-character `Ref_Key` `N` format.
- No task may add a default SKU UoM, code-based UoM resolution, `ExternalSystem`, background jobs, polling, persistent import history, distributed locking, full localization, auth baseline, or inventory-accounting refactoring.
