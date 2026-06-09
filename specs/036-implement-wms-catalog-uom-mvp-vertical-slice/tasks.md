# Tasks: WMS Catalog/UoM MVP Vertical Slice

**Input**: Design documents from `specs/036-implement-wms-catalog-uom-mvp-vertical-slice/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/catalog-uom-api-and-ui-contract.md`, `quickstart.md`

**Tests**: Include focused tests required by Constitution v1.0.1 and issue #34 repeated reference-data guidance. Do not duplicate the full Catalog/SKU matrix. Endpoint and UI automation remain deferred per `plan.md`; include manual API/UI smoke validation tasks instead.

**Organization**: Tasks are grouped by user story to enable incremental implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other tasks in the same phase when files do not conflict
- **[Story]**: User story label for story phases only
- Every task includes exact file paths

## Phase 1: Setup (Shared Context)

**Purpose**: Confirm the active feature context and prepare UoM folders without changing existing SKU/Topology behavior.

- [X] T001 Review issue #36 guardrails in specs/036-implement-wms-catalog-uom-mvp-vertical-slice/plan.md before implementation
- [X] T002 [P] Review the representative Catalog/SKU implementation under Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits and Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages
- [X] T003 [P] Create UoM source directories Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure, Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure, and Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages
- [X] T004 [P] Create UoM test directories Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure and Myrmex.Tests/Wms/Catalog/Persistence

---

## Phase 2: Foundational Guardrails (Blocking Prerequisites)

**Purpose**: Make the shared implementation boundaries explicit before story work starts.

**CRITICAL**: No user story implementation should add conversions, SKU binding, packaging, barcode, inventory, receiving, LPN, picking/shipping, provider-specific sorting, `AsEnumerable()` sorting, new endpoint/UI test frameworks, or new observability infrastructure.

- [X] T005 Confirm no out-of-scope UoM files or migrations exist before coding by checking Myrmex.Modules.Wms/Catalog and Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations
- [X] T006 Confirm existing Catalog client support remains local by checking Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs, Myrmex.WebApp/Wms/Catalog/ApiResult.cs, and Myrmex.WebApp/Wms/Catalog/ApiException.cs

**Checkpoint**: UoM work can proceed using Catalog/SKU as the representative pattern.

---

## Phase 3: User Story 1 - Create UoM Reference Data (Priority: P1) MVP

**Goal**: A catalog user can create a valid UoM with code, name, and optional symbol, receive normalized active details, and get clear validation or duplicate-code errors.

**Independent Test**: Create UoM `EA` with name `Each` and symbol `ea`; verify it is active, `UpdatedAtUtc` is null, duplicate normalized code is rejected, and invalid fields return field-specific errors.

### Tests for User Story 1

- [X] T007 [P] [US1] Add focused UnitOfMeasure create/validation/domain-event tests in Myrmex.Tests/Wms/Catalog/Domain/UnitOfMeasureTests.cs
- [X] T008 [P] [US1] Add focused CreateUnitOfMeasure handler tests for success, validation failure, duplicate code, and persistence failure in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/CreateUnitOfMeasureHandlerTests.cs
- [X] T009 [P] [US1] Add UoM persistence tests for table creation, required fields, nullable UpdatedAtUtc, no NormalizedCode, and unique Code in Myrmex.Tests/Wms/Catalog/Persistence/UnitOfMeasurePersistenceTests.cs
- [X] T010 [P] [US1] Add UoM create route and write-result client wiring tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs

### Implementation for User Story 1

- [X] T011 [P] [US1] Create UoM domain events in Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasureEvents.cs
- [X] T012 [US1] Create UnitOfMeasure aggregate with normalized Code, required Name, optional Symbol, active-on-create state, UpdatedAtUtc null on create, and real-change domain events in Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs
- [X] T013 [P] [US1] Create UnitOfMeasureDetails projection in Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/UnitOfMeasureDetails.cs
- [X] T014 [US1] Add UnitOfMeasure error codes for validation, duplicate code, not found, and persistence failure using existing conventions in Myrmex.Modules.Wms/WmsErrors.cs
- [X] T015 [US1] Implement CreateUnitOfMeasure command/handler with validation, duplicate normalized Code check, persistence save, and ServiceResult behavior in Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/CreateUnitOfMeasure.cs
- [X] T016 [US1] Add units_of_measure database names and unique Code index names in Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs
- [X] T017 [US1] Add UnitOfMeasure DbSet to Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs
- [X] T018 [US1] Add UnitOfMeasure EF Core mapping with required Code/Name/CreatedAtUtc/IsActive, optional Symbol/UpdatedAtUtc, ignored DomainEvents, and unique Code index in Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/UnitOfMeasureConfiguration.cs
- [X] T019 [US1] Generate AddUnitsOfMeasure migration and model snapshot changes in Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations
- [X] T020 [US1] Add create UoM endpoint and request contract in Myrmex.Modules.Wms/Catalog/Endpoints/UnitOfMeasureEndpoints.cs
- [X] T021 [US1] Register UnitOfMeasure endpoints in Myrmex.Modules.Wms/Catalog/Endpoints/CatalogEndpoints.cs
- [X] T022 [US1] Add UnitOfMeasureDetails and CreateUnitOfMeasureRequest DTOs plus TryCreateUnitOfMeasureAsync to Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs
- [X] T023 [US1] Create UoM edit dialog with create mode, code/name/symbol inputs, existing ApiResult error display, and no conversion fields in Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomEditDialog.razor

**Checkpoint**: User Story 1 is functional through domain, handler, persistence, endpoint, client, and create dialog wiring.

---

## Phase 4: User Story 2 - Find and Review UoMs (Priority: P2)

**Goal**: A catalog user can list active UoMs, include inactive UoMs, search by code/name/symbol, sort by provider-safe fields, and retrieve a UoM by identity.

**Independent Test**: Seed multiple UoMs, list default active records, include inactive records, search by `EA` or `Each`, sort by `code`, `name`, and `isActive`, and retrieve an active or inactive UoM by id.

### Tests for User Story 2

- [X] T024 [P] [US2] Add focused ListUnitsOfMeasure handler tests for active-only default, includeInactive, search, code/name/isActive sorting, unknown sort fallback, and no date sorting behavior in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasureHandlerTests.cs
- [X] T025 [P] [US2] Add focused get-by-id handler tests for active, inactive, and missing UoM behavior in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/GetUnitOfMeasureByIdHandlerTests.cs
- [X] T026 [US2] Add UoM list/get read-client route and exception-flow wiring tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs

### Implementation for User Story 2

- [X] T027 [US2] Implement ListUnitsOfMeasure query/handler with bounded paging, active-only default, includeInactive, search by Code/Name/Symbol, and provider-safe code/name/isActive sorting in Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasure.cs
- [X] T028 [US2] Implement GetUnitOfMeasureById query/handler returning active or inactive UoMs and existing not-found behavior in Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/GetUnitOfMeasureById.cs
- [X] T029 [US2] Add list and get UoM endpoints in Myrmex.Modules.Wms/Catalog/Endpoints/UnitOfMeasureEndpoints.cs
- [X] T030 [US2] Add ListUnitsOfMeasureAsync and GetUnitOfMeasureByIdAsync to Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs
- [X] T031 [P] [US2] Create UoM filters component for search and include-inactive controls in Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomFilters.razor
- [X] T032 [P] [US2] Create UoM grid component with code, name, symbol, active state, timestamps, provider-safe sort fields, and action slots in Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomGrid.razor
- [X] T033 [US2] Create UoM page load/search/refresh behavior using WmsCatalogApiClient in Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/Index.razor.cs
- [X] T034 [US2] Create UoM page markup at route /wms/catalog/uoms in Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/Index.razor
- [X] T035 [US2] Add UoMs to Catalog navigation without changing existing SKU/Topology links in Myrmex.WebApp/Components/Layout/NavMenu.razor

**Checkpoint**: User Stories 1 and 2 are functional and can be reviewed from the UoM page and API/client list/get flows.

---

## Phase 5: User Story 3 - Maintain UoM Details and Lifecycle (Priority: P3)

**Goal**: A catalog user can update UoM name/symbol and deactivate or reactivate UoMs without deleting identity or emitting lifecycle events for no-op calls.

**Independent Test**: Update an existing UoM, verify code is preserved and UpdatedAtUtc is set, deactivate it, confirm default lists hide it, include inactive to review it, then reactivate it.

### Tests for User Story 3

- [ ] T036 [P] [US3] Add focused UnitOfMeasure update/deactivate/reactivate domain tests including idempotent no-op lifecycle behavior in Myrmex.Tests/Wms/Catalog/Domain/UnitOfMeasureTests.cs
- [ ] T037 [P] [US3] Add focused UpdateUnitOfMeasureDetails handler tests for success, validation failure, missing UoM, code preservation, and timestamp update in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/UpdateUnitOfMeasureDetailsHandlerTests.cs
- [ ] T038 [P] [US3] Add focused lifecycle handler tests for deactivate/reactivate success, missing UoM, and idempotent no-op behavior in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/UnitOfMeasureLifecycleHandlerTests.cs
- [ ] T039 [US3] Add UoM update/deactivate/reactivate client write-result wiring tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs

### Implementation for User Story 3

- [ ] T040 [US3] Implement UpdateDetails, Deactivate, and Reactivate behavior in Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs
- [ ] T041 [US3] Implement UpdateUnitOfMeasureDetails command/handler in Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/UpdateUnitOfMeasureDetails.cs
- [ ] T042 [US3] Implement DeactivateUnitOfMeasure command/handler in Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/DeactivateUnitOfMeasure.cs
- [ ] T043 [US3] Implement ReactivateUnitOfMeasure command/handler in Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/ReactivateUnitOfMeasure.cs
- [ ] T044 [US3] Add update, deactivate, and reactivate UoM endpoints in Myrmex.Modules.Wms/Catalog/Endpoints/UnitOfMeasureEndpoints.cs
- [ ] T045 [US3] Add TryUpdateUnitOfMeasureDetailsAsync, TryDeactivateUnitOfMeasureAsync, and TryReactivateUnitOfMeasureAsync to Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs
- [ ] T046 [US3] Add edit-mode behavior to UoM dialog with code locked and name/symbol update support in Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomEditDialog.razor
- [ ] T047 [US3] Add edit/deactivate/reactivate handlers, snackbar feedback, and reload behavior to Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/Index.razor.cs
- [ ] T048 [US3] Wire UoM grid action callbacks for edit, deactivate, and reactivate in Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/Index.razor

**Checkpoint**: All user stories are independently functional through the repeated Catalog/UoM slice.

---

## Phase 6: Polish & Validation

**Purpose**: Verify the repeated-slice scope, focused test strategy, manual endpoint/UI validation, and regression safety.

- [ ] T049 Run build validation from specs/036-implement-wms-catalog-uom-mvp-vertical-slice/quickstart.md with dotnet build Myrmex.slnx -nologo -v:minimal
- [ ] T050 Run focused UoM tests from specs/036-implement-wms-catalog-uom-mvp-vertical-slice/quickstart.md with dotnet test Myrmex.Tests/Myrmex.Tests.csproj --filter "FullyQualifiedName~UnitOfMeasure|FullyQualifiedName~UnitsOfMeasure" -nologo -v:minimal
- [ ] T051 Run full regression tests from specs/036-implement-wms-catalog-uom-mvp-vertical-slice/quickstart.md with dotnet test Myrmex.Tests/Myrmex.Tests.csproj -nologo -v:minimal
- [ ] T052 Manually verify UoM migration shape against specs/036-implement-wms-catalog-uom-mvp-vertical-slice/quickstart.md in Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations
- [ ] T053 Manually perform UoM API smoke validation from specs/036-implement-wms-catalog-uom-mvp-vertical-slice/quickstart.md for create, duplicate create, list/search/sort, get, update, deactivate, include inactive, and reactivate behavior
- [ ] T054 Manually perform UoM UI smoke validation from specs/036-implement-wms-catalog-uom-mvp-vertical-slice/quickstart.md at /wms/catalog/uoms for navigation, create/edit dialog, search, include inactive, lifecycle actions, snackbar/reload behavior, and absence of out-of-scope controls
- [ ] T055 Verify final diff against specs/036-implement-wms-catalog-uom-mvp-vertical-slice/plan.md contains no conversions, SKU-to-UoM binding, packaging, barcode, inventory, receiving, LPN, picking/shipping, integration, provider-specific sorting, AsEnumerable sorting, new endpoint/UI test frameworks, new observability infrastructure, or broad SKU/Topology refactors

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational Guardrails (Phase 2)**: Depends on Setup completion and blocks story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational Guardrails and delivers the MVP create flow plus shared UoM aggregate/persistence foundations.
- **User Story 2 (Phase 4)**: Depends on US1 domain/persistence foundations and adds list/get review behavior.
- **User Story 3 (Phase 5)**: Depends on US1 domain/persistence foundations and US2 page/list wiring for complete UI lifecycle behavior.
- **Polish & Validation (Phase 6)**: Depends on selected story phases being complete; full validation depends on all stories.

### User Story Dependencies

- **User Story 1 (P1)**: MVP. No dependency on other user stories after setup/foundational phases.
- **User Story 2 (P2)**: Requires the UoM aggregate and persistence from US1, but list/get tests and implementation are independently reviewable once those foundations exist.
- **User Story 3 (P3)**: Requires the UoM aggregate and persistence from US1; UI lifecycle wiring also benefits from the US2 page/grid.

### Within Each User Story

- Write the listed focused tests first and confirm they fail before implementation.
- Implement domain and details models before handlers.
- Implement handlers before endpoints and client methods.
- Implement client methods before UI wiring that depends on them.
- Endpoint/UI automation remains deferred; complete manual validation tasks T053 and T054 instead.

---

## Parallel Opportunities

- Setup review/directory tasks T002, T003, and T004 can run in parallel.
- US1 tests T007, T008, T009, and T010 can run in parallel because they use different files.
- US1 domain events T011 and details projection T013 can run in parallel before handler implementation.
- US2 tests T024 and T025 can run in parallel; T026 shares the Catalog client test file and should be sequenced with other client-test edits.
- US2 UI components T031 and T032 can run in parallel before page integration.
- US3 tests T036, T037, and T038 can run in parallel; T039 shares the Catalog client test file and should be sequenced with other client-test edits.
- Manual validation tasks T052, T053, and T054 can run after implementation and build/test validation.

---

## Parallel Example: User Story 1

```text
Task: "T007 Add focused UnitOfMeasure create/validation/domain-event tests in Myrmex.Tests/Wms/Catalog/Domain/UnitOfMeasureTests.cs"
Task: "T008 Add focused CreateUnitOfMeasure handler tests in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/CreateUnitOfMeasureHandlerTests.cs"
Task: "T009 Add UoM persistence tests in Myrmex.Tests/Wms/Catalog/Persistence/UnitOfMeasurePersistenceTests.cs"
Task: "T010 Add UoM create route and write-result client wiring tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "T024 Add focused ListUnitsOfMeasure handler tests in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasureHandlerTests.cs"
Task: "T025 Add focused get-by-id handler tests in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/GetUnitOfMeasureByIdHandlerTests.cs"
Task: "T031 Create UoM filters component in Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomFilters.razor"
Task: "T032 Create UoM grid component in Myrmex.WebApp/Components/Pages/Wms/Catalog/UomPages/UomGrid.razor"
```

## Parallel Example: User Story 3

```text
Task: "T036 Add focused UnitOfMeasure update/deactivate/reactivate domain tests in Myrmex.Tests/Wms/Catalog/Domain/UnitOfMeasureTests.cs"
Task: "T037 Add focused UpdateUnitOfMeasureDetails handler tests in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/UpdateUnitOfMeasureDetailsHandlerTests.cs"
Task: "T038 Add focused lifecycle handler tests in Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/UnitOfMeasureLifecycleHandlerTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup and Phase 2 guardrails.
2. Complete Phase 3 User Story 1 tests and implementation.
3. Validate create behavior independently through domain, handler, persistence, endpoint, and client paths.
4. Stop and review scope before adding list/get and lifecycle behavior.

### Incremental Delivery

1. Complete US1 create flow and validate the UoM aggregate/persistence foundation.
2. Add US2 list/get review behavior and validate search/sort/include-inactive behavior.
3. Add US3 update/deactivate/reactivate lifecycle behavior and validate idempotent no-op handling.
4. Run build, focused tests, full regression tests, and manual API/UI smoke validation.

### Scope Discipline

- Keep all UoM work inside issue #36 files and shared registration points listed in `plan.md`.
- Reuse Catalog/SKU patterns where applicable, but do not duplicate its full test matrix.
- Do not add conversion, SKU binding, packaging, barcode, inventory, receiving, LPN, picking/shipping, integration, new endpoint/UI automation, new observability infrastructure, or broad refactors.
