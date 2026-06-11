# Tasks: Catalog/SKU Base UoM MVP Vertical Slice

**Input**: Design documents from `specs/044-catalogsku-base-uom-mvp-vertical-slice/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/catalog-sku-base-uom-api-contract.md`, `quickstart.md`

**Tests**: Required for changed domain rules, command/query handlers, persistence mapping, and API client contracts. Endpoint/UI automation remains deferred per `plan.md`; lower-level automated coverage and manual validation tasks are included instead.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently after shared foundation tasks.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it edits different files and does not depend on incomplete tasks in the same phase.
- **[Story]**: Maps to a user story from `spec.md`.
- Every task includes an exact repository path or feature artifact path.

## Phase 1: Setup (Shared Context)

**Purpose**: Confirm the brownfield files and scope guardrails before implementation.

- [X] T001 Inspect current SKU, UoM, and SKU Barcode source shapes in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`, `Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs`, and `Myrmex.Modules.Wms/Catalog/Domain/SkuBarcodes/SkuBarcode.cs`
- [X] T002 [P] Inspect current SKU endpoint, WebApp client, and test contract call sites in `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`, `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`, and `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the shared SKU Base UoM domain and persistence foundation required by all user stories.

**Critical**: No user story implementation should begin until this phase is complete.

### Tests for Foundation

- [X] T003 [P] Add SKU domain tests for required non-empty `BaseUnitOfMeasureId` on create/update in `Myrmex.Tests/Wms/Catalog/Domain/StockKeepingUnitTests.cs`
- [X] T004 [P] Add SKU persistence tests for required `BaseUnitOfMeasureId`, FK relationship, and index metadata in `Myrmex.Tests/Wms/Catalog/Persistence/StockKeepingUnitPersistenceTests.cs`

### Implementation for Foundation

- [X] T005 Add `BaseUnitOfMeasureId` to the `StockKeepingUnit` aggregate, create factory, update method, and validation in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`
- [X] T006 Add missing/inactive base UoM service errors for assignment validation in `Myrmex.Modules.Wms/WmsErrors.cs`
- [X] T007 Add stock keeping unit base UoM FK and index names in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- [X] T008 Configure required SKU to UoM relationship and base UoM index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`
- [X] T009 Verify no new Base UoM aggregate, endpoint group, conversion model, seed/demo data, or UI page is introduced while editing `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs` and `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`

**Checkpoint**: Foundation ready; `StockKeepingUnit` has a required persisted base UoM identity and no out-of-scope model has been added.

---

## Phase 3: User Story 1 - Create SKU With Base UoM (Priority: P1) MVP

**Goal**: A catalog user can create a SKU only when it references exactly one existing active base UoM.

**Independent Test**: Create an active UoM, create SKU `ITEM-001` with that UoM as `BaseUnitOfMeasureId`, confirm the create result includes the assignment, and confirm missing/nonexistent/inactive UoM attempts fail without creating a SKU.

### Tests for User Story 1

- [X] T010 [US1] Extend create handler tests for missing, empty, nonexistent, inactive, and valid active base UoM create cases in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnitHandlerTests.cs`
- [X] T011 [P] [US1] Extend API client tests for SKU create request and response `BaseUnitOfMeasureId` behavior in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 1

- [X] T012 [US1] Update `CreateStockKeepingUnit.Command` and handler to accept `BaseUnitOfMeasureId`, validate existing active UoM, and return clear assignment errors in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnit.cs`
- [X] T013 [US1] Update SKU create request binding to include nullable/validatable `BaseUnitOfMeasureId` in `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`
- [X] T014 [US1] Update WebApp SKU create request record and serialization contract for `BaseUnitOfMeasureId` without adding a new SKU Base UoM UI workflow in `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`
- [X] T015 [US1] Update existing create-related test data builders and assertions affected by required base UoM in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnitHandlerTests.cs`

**Checkpoint**: User Story 1 is independently functional and testable as the MVP.

---

## Phase 4: User Story 2 - Review SKU Base UoM (Priority: P2)

**Goal**: A catalog user can get and list SKUs with the stored base UoM identity visible in returned SKU details.

**Independent Test**: Create multiple SKUs with different active base UoMs, retrieve individual SKUs, list SKUs, and confirm every returned `StockKeepingUnitDetails` includes the correct `BaseUnitOfMeasureId`.

### Tests for User Story 2

- [X] T016 [US2] Extend get-by-id handler tests to assert `BaseUnitOfMeasureId` is returned for active and inactive SKUs in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/GetStockKeepingUnitByIdHandlerTests.cs`
- [X] T017 [P] [US2] Extend list handler tests to assert every listed SKU includes the correct `BaseUnitOfMeasureId` in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs`
- [X] T018 [P] [US2] Extend API client tests for SKU get/list response parsing with `BaseUnitOfMeasureId` in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 2

- [X] T019 [US2] Add `BaseUnitOfMeasureId` to `StockKeepingUnitDetails.From` and projection in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/StockKeepingUnitDetails.cs`
- [X] T020 [US2] Update get-by-id query projection usage for the expanded SKU details contract in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/GetStockKeepingUnitById.cs`
- [X] T021 [US2] Update list query projection usage for the expanded SKU details contract in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnits.cs`
- [X] T022 [US2] Update WebApp `StockKeepingUnitDetails` record to include `BaseUnitOfMeasureId` while preserving existing SKU list client behavior in `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

**Checkpoint**: User Stories 1 and 2 are independently functional and returned SKU details consistently expose the base UoM identity.

---

## Phase 5: User Story 3 - Change SKU Base UoM (Priority: P3)

**Goal**: A catalog user can change an existing SKU's base UoM to another existing active UoM without changing SKU identity or code.

**Independent Test**: Create a SKU with one active base UoM, update it to another active base UoM, confirm update/get/list show the new assignment, and confirm missing/nonexistent/inactive UoM updates fail without changing the current assignment.

### Tests for User Story 3

- [ ] T023 [US3] Extend update handler tests for valid base UoM change, missing base UoM, nonexistent UoM, inactive UoM, and unchanged assignment on failure in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetailsHandlerTests.cs`
- [ ] T024 [US3] Extend API client tests for SKU update request and response `BaseUnitOfMeasureId` behavior in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 3

- [ ] T025 [US3] Update `UpdateStockKeepingUnitDetails.Command` and handler to accept `BaseUnitOfMeasureId`, validate existing active UoM, and preserve the current assignment on validation failure in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetails.cs`
- [ ] T026 [US3] Update SKU update request binding to include nullable/validatable `BaseUnitOfMeasureId` in `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`
- [ ] T027 [US3] Update WebApp SKU update request record and serialization contract for `BaseUnitOfMeasureId` without adding a new SKU Base UoM UI workflow in `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`
- [ ] T028 [US3] Update existing SKU edit compatibility only as needed for compile-time contract changes, without adding Base UoM selection controls, in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuEditDialog.razor`

**Checkpoint**: All three user stories are independently functional and SKU Base UoM can be created, reviewed, and changed through the existing SKU contract surface.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Regression coverage, migration handoff, and scope validation across the feature.

- [ ] T029 [P] Add lifecycle regression assertions that deactivate/reactivate results retain `BaseUnitOfMeasureId` in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/DeactivateStockKeepingUnitHandlerTests.cs` and `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ReactivateStockKeepingUnitHandlerTests.cs`
- [ ] T030 [P] Update SKU Barcode test setup to create SKUs with valid base UoMs in `Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/` and `Myrmex.Tests/Wms/Catalog/Persistence/SkuBarcodePersistenceTests.cs`
- [ ] T031 Update existing SKU domain, handler, and persistence tests to provide valid base UoMs where the required constructor/factory signature changes in `Myrmex.Tests/Wms/Catalog/Domain/StockKeepingUnitTests.cs`, `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/`, and `Myrmex.Tests/Wms/Catalog/Persistence/StockKeepingUnitPersistenceTests.cs`
- [ ] T032 [P] Update existing SKU grid display only if needed to remain compatible with the expanded details record, without adding Base UoM columns or selection UI, in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuGrid.razor`
- [ ] T033 Stop before EF migration generation and recommend the developer-controlled migration commands documented in `specs/044-catalogsku-base-uom-mvp-vertical-slice/quickstart.md`
- [ ] T034 Verify final code diff adds no alternative UoM, conversion, packaging, inventory, receiving, LPN, picking, shipping, seed/demo, new UI page, or integration behavior across `Myrmex.Modules.Wms/`, `Myrmex.WebApp/`, and `Myrmex.Tests/`
- [ ] T035 Recommend developer-controlled validation commands from `specs/044-catalogsku-base-uom-mvp-vertical-slice/quickstart.md` without running build, tests, app startup, EF migration generation, database update, or migration application automatically

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundation; delivers the MVP create flow.
- **User Story 2 (Phase 4)**: Depends on Foundation and the details shape from US1; can be implemented after the shared details contract exists.
- **User Story 3 (Phase 5)**: Depends on Foundation and can proceed once update contract work starts.
- **Polish (Phase 6)**: Depends on desired user stories being complete.

### User Story Dependencies

- **US1 Create SKU With Base UoM**: MVP. Requires Foundation only.
- **US2 Review SKU Base UoM**: Requires the shared `BaseUnitOfMeasureId` details shape; can be validated independently with seeded SKUs.
- **US3 Change SKU Base UoM**: Requires the shared `BaseUnitOfMeasureId` domain/persistence shape; can be validated independently with an existing SKU and two UoMs.

### Within Each User Story

- Write or update tests first and confirm they fail before implementation.
- Domain/persistence model work precedes handler work.
- Handler work precedes endpoint and WebApp API client contract work.
- Manual endpoint/UI validation remains a developer-controlled quickstart activity, not an automated task.

---

## Parallel Opportunities

- T001 and T002 can be split after the branch and feature docs are confirmed.
- T003 and T004 can run in parallel because they edit different test files.
- T010 and T011 can run in parallel after Foundation because they edit handler tests and client tests.
- T016, T017, and T018 can run in parallel after `StockKeepingUnitDetails` changes are planned.
- T029, T030, and T032 can run in parallel during polish because they touch separate regression/UI compatibility areas.

---

## Parallel Example: User Story 1

```text
Task: "Extend create handler tests for missing, empty, nonexistent, inactive, and valid active base UoM create cases in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnitHandlerTests.cs"
Task: "Extend API client tests for SKU create request and response BaseUnitOfMeasureId behavior in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Extend get-by-id handler tests to assert BaseUnitOfMeasureId is returned for active and inactive SKUs in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/GetStockKeepingUnitByIdHandlerTests.cs"
Task: "Extend list handler tests to assert every listed SKU includes the correct BaseUnitOfMeasureId in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs"
Task: "Extend API client tests for SKU get/list response parsing with BaseUnitOfMeasureId in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "Extend update handler tests for valid base UoM change, missing base UoM, nonexistent UoM, inactive UoM, and unchanged assignment on failure in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetailsHandlerTests.cs"
Task: "Update SKU update request binding to include nullable/validatable BaseUnitOfMeasureId in Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational domain and persistence shape.
3. Complete Phase 3: User Story 1 create flow.
4. Stop and validate User Story 1 independently through focused tests and the manual quickstart scenario.
5. Do not generate or apply migrations automatically; recommend the developer-controlled commands in `quickstart.md`.

### Incremental Delivery

1. Complete Setup and Foundation.
2. Deliver US1 create with base UoM and validate independently.
3. Deliver US2 get/list projection and validate independently.
4. Deliver US3 update/change base UoM and validate independently.
5. Complete polish/regression tasks and recommend developer-controlled validation commands.

### Parallel Team Strategy

1. One developer handles foundation tests while another prepares persistence and contract changes.
2. After Foundation, US1, US2, and US3 test tasks can be assigned separately by file ownership.
3. Merge story work only after each story's independent tests and contract checks pass.

---

## Notes

- `[P]` tasks are limited to different files or independent review tasks.
- `[US1]`, `[US2]`, and `[US3]` labels map directly to the user stories in `spec.md`.
- Migration generation and database update are intentionally absent as executable implementation tasks; T033 requires stopping and recommending the documented developer-controlled commands.
- Avoid adding alternative UoMs, conversion factors, packaging, inventory, receiving, LPN, picking, shipping, seed/demo data, new UI pages, or external integration behavior.
