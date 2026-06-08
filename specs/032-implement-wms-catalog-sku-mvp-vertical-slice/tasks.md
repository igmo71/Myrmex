# Tasks: WMS Catalog/SKU MVP Vertical Slice

**Input**: Design documents from `specs/032-implement-wms-catalog-sku-mvp-vertical-slice/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/catalog-sku-api-and-ui-contract.md`, `quickstart.md`, `.specify/memory/constitution.md`, and `.specify/memory/myrmex-*.md`.

**Tests**: Required by the Myrmex constitution for new domain rules, command/query handlers, persistence mappings, and API clients. Endpoint and UI automated tests are conditional under Constitution v1.0.1 and are deferred for issue #32 through the plan's Complexity Tracking exception. Test tasks are listed before behavior-changing implementation tasks in each backend user-story phase.

**Organization**: Tasks are backend-first. Browser UI create/edit/list/lifecycle behavior is delivered after backend capabilities and API-client contracts exist.

## Phase 1: Setup (Folders Only)

**Purpose**: Prepare Catalog/SKU folders and test folders without changing existing WMS Topology behavior.

- [X] T001 Create Catalog feature folders in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits`, `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits`, and `Myrmex.Modules.Wms/Catalog/Endpoints`
- [X] T002 Create Catalog web folders in `Myrmex.WebApp/Wms/Catalog` and `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages`
- [X] T003 Create Catalog test folders in `Myrmex.Tests/Wms/Catalog/Domain`, `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits`, `Myrmex.Tests/Wms/Catalog/Persistence`, and `Myrmex.Tests/Wms/Catalog/Client`

---

## Phase 2: Backend Declarations (Non-Behavior Only)

**Purpose**: Add names and error identifiers required for later code to compile, without implementing runtime behavior.

**Guardrail**: Foundational declarations may introduce names and error identifiers required for compilation, but they must not implement behavior. Behavior-changing mapping, including duplicate-code persistence exception mapping, must be implemented after tests.

- [X] T004 Add StockKeepingUnit table, primary key, and unique code index constants to `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- [X] T005 Add StockKeepingUnit error identifiers for not found, duplicate code, validation, and create/update/lifecycle failures to `Myrmex.Modules.Wms/WmsErrors.cs`

**Checkpoint**: Backend declarations are ready; behavior-changing user-story work can begin.

---

## Phase 3: User Story 1 Backend - Create SKU Reference Data (Priority: P1) MVP

**Goal**: A catalog user can create a SKU through the domain, handler, Minimal API, and API client with normalized `Code`, required name, optional description, active status, `UpdatedAtUtc` null on create, duplicate-code protection, and clear validation errors.

**Independent Test**: Create a valid SKU through backend/API-client paths, confirm it persists with normalized `Code` and `UpdatedAtUtc` null, reject invalid input, and reject duplicate normalized codes. Browser UI creation is delivered in the frontend phase.

### Tests for User Story 1 Backend

> Write these tests first and ensure they fail before behavior-changing implementation.

- [X] T006 [P] [US1] Add StockKeepingUnit create validation, normalization, active-on-create, `UpdatedAtUtc` null, and created-domain-event tests in `Myrmex.Tests/Wms/Catalog/Domain/StockKeepingUnitTests.cs`
- [X] T007 [P] [US1] Add CreateStockKeepingUnit handler tests for invalid input, duplicate normalized `Code`, successful create, persisted details, and dispatched created event in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnitHandlerTests.cs`
- [X] T008 [P] [US1] Add practical SQLite/EnsureCreated persistence tests for StockKeepingUnit table creation, absence of a `NormalizedCode` column, and unique normalized `Code` index in `Myrmex.Tests/Wms/Catalog/Persistence/StockKeepingUnitPersistenceTests.cs`
- [X] T009 [P] [US1] Add WmsCatalogApiClient create error-handling tests for ProblemDetails and malformed error fallback in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 1 Backend

- [X] T010 [US1] Implement StockKeepingUnit aggregate using existing `AggregateRoot`/`EntityBase` patterns in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`
- [X] T011 [US1] Implement StockKeepingUnit created domain event in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnitEvents.cs`
- [X] T012 [US1] Implement StockKeepingUnitDetails projection in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/StockKeepingUnitDetails.cs`
- [X] T013 [US1] Add StockKeepingUnit DbSet to `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`
- [X] T014 [US1] Implement StockKeepingUnit EF Core mapping with stored normalized `Code`, no `NormalizedCode`, nullable `UpdatedAtUtc`, and unique code index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`
- [X] T015 [US1] Generate EF Core migration named `AddStockKeepingUnits`; expected generated files are `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddStockKeepingUnits.cs`, `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddStockKeepingUnits.Designer.cs`, and an updated `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`
- [X] T016 [US1] Add StockKeepingUnit duplicate-code behavior mapping to `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsPersistenceExceptionMapper.cs`
- [X] T017 [US1] Implement CreateStockKeepingUnit command and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnit.cs`
- [X] T018 [US1] Implement Catalog route group in `Myrmex.Modules.Wms/Catalog/Endpoints/CatalogEndpoints.cs`
- [X] T019 [US1] Map Catalog endpoints from `Myrmex.Modules.Wms/WmsModule.cs` without changing existing Topology endpoint registration behavior
- [X] T020 [US1] Implement create SKU endpoint and request contract in `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`
- [X] T021 [US1] Implement local Catalog `ApiResult<T>` support type in `Myrmex.WebApp/Wms/Catalog/ApiResult.cs` without moving or rewriting Topology client support types
- [X] T022 [US1] Implement local Catalog `ApiException` support type in `Myrmex.WebApp/Wms/Catalog/ApiException.cs` without moving or rewriting Topology client support types
- [X] T023 [US1] Implement WmsCatalogApiClient create SKU request, details record, and write/action error handling in `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

**Checkpoint**: User Story 1 backend/API/client behavior is fully functional and independently testable.

---

## Phase 4: User Story 2 Backend - Find and Review SKUs (Priority: P2)

**Goal**: A catalog user can list active SKUs by default, include inactive SKUs, search by code/name/description, sort by existing local pattern fields, and retrieve a SKU by identity through backend/API-client paths.

**Independent Test**: Create multiple SKUs, list active records, search by code/name, sort by `code`, `name`, `createdAtUtc`, `updatedAtUtc`, and `isActive`, retrieve by identity, and receive not-found for a missing identity. Browser UI list behavior is delivered in the frontend phase.

### Tests for User Story 2 Backend

- [ ] T024 [P] [US2] Add ListStockKeepingUnits handler tests for active-only default, include inactive, search, bounded paging, supported sorting, and fallback code ordering in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs`
- [ ] T025 [P] [US2] Add GetStockKeepingUnitById handler tests for existing active SKU, existing inactive SKU, and missing SKU not-found result in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/GetStockKeepingUnitByIdHandlerTests.cs`
- [ ] T026 [P] [US2] Add WmsCatalogApiClient read/load error-handling tests for list/get ProblemDetails and malformed error fallback in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 2 Backend

- [ ] T027 [US2] Implement ListStockKeepingUnits query and handler with active filtering, search, bounded paging, and local-pattern sorting in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnits.cs`
- [ ] T028 [US2] Implement GetStockKeepingUnitById query and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/GetStockKeepingUnitById.cs`
- [ ] T029 [US2] Add list and get SKU endpoints to `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`
- [ ] T030 [US2] Add ListRequest, ListResult, list method, and get method to `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

**Checkpoint**: User Stories 1 and 2 backend/API/client behavior work independently.

---

## Phase 5: User Story 3 Backend - Maintain SKU Details and Lifecycle (Priority: P3)

**Goal**: A catalog user can update SKU name/description without changing code, deactivate active SKUs, reactivate inactive SKUs, and receive idempotent success without lifecycle domain events for no-op deactivate/reactivate calls through backend/API-client paths.

**Independent Test**: Update details, deactivate, verify hidden from default list through backend list behavior, include inactive, reactivate, and verify default list visibility returns. Browser UI lifecycle behavior is delivered in the frontend phase.

### Tests for User Story 3 Backend

- [ ] T031 [P] [US3] Add StockKeepingUnit update/deactivate/reactivate domain tests including `UpdatedAtUtc` changes and no domain event for idempotent no-op lifecycle calls in `Myrmex.Tests/Wms/Catalog/Domain/StockKeepingUnitTests.cs`
- [ ] T032 [P] [US3] Add UpdateStockKeepingUnitDetails handler tests for not found, invalid details, successful update, preserved code, and dispatched details-updated event in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetailsHandlerTests.cs`
- [ ] T033 [P] [US3] Add DeactivateStockKeepingUnit handler tests for not found, successful deactivate, hidden active state, and idempotent no-op without domain event in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/DeactivateStockKeepingUnitHandlerTests.cs`
- [ ] T034 [P] [US3] Add ReactivateStockKeepingUnit handler tests for not found, successful reactivate, restored active state, and idempotent no-op without domain event in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ReactivateStockKeepingUnitHandlerTests.cs`
- [ ] T035 [P] [US3] Add WmsCatalogApiClient update/deactivate/reactivate write/action error-handling tests in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 3 Backend

- [ ] T036 [US3] Add StockKeepingUnit details-updated, deactivated, and reactivated domain events in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnitEvents.cs`
- [ ] T037 [US3] Add StockKeepingUnit update, deactivate, and reactivate behavior with `UpdatedAtUtc` updates only on real changes in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`
- [ ] T038 [US3] Implement UpdateStockKeepingUnitDetails command and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetails.cs`
- [ ] T039 [US3] Implement DeactivateStockKeepingUnit command and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/DeactivateStockKeepingUnit.cs`
- [ ] T040 [US3] Implement ReactivateStockKeepingUnit command and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ReactivateStockKeepingUnit.cs`
- [ ] T041 [US3] Add update/deactivate/reactivate endpoints to `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`
- [ ] T042 [US3] Add update/deactivate/reactivate request and client methods to `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

**Checkpoint**: All backend/API/client user stories are independently functional.

---

## Phase 6: Frontend SKU Page and Client Wiring

**Purpose**: Deliver the browser UI create/edit/list/lifecycle experience after backend capabilities and API-client contracts exist.

- [ ] T043 Register WmsCatalogApiClient in `Myrmex.WebApp/Program.cs`
- [ ] T044 Implement SKU list page route and loading behavior in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor` and `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor.cs`
- [ ] T045 Implement SKU filters component for search and include-inactive behavior in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuFilters.razor`
- [ ] T046 Implement SKU grid component with code, name, description, active state, timestamps, and action callbacks in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuGrid.razor`
- [ ] T047 Implement SKU create/edit dialog with code disabled in edit mode in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuEditDialog.razor`
- [ ] T048 Wire create, edit, deactivate, reactivate, snackbar, and reload behavior in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor.cs`

**Checkpoint**: Browser UI create/edit/list/lifecycle behavior is available for manual validation.

---

## Phase 7: Validation & Scope Guardrails

**Purpose**: Verify the complete vertical slice, preserve existing behavior, and prevent scope drift.

- [ ] T049 Run solution build with `dotnet build Myrmex.slnx -nologo -v:minimal` and record results in the implementation summary
- [ ] T050 Run Catalog/SKU-focused tests with `dotnet test Myrmex.Tests/Myrmex.Tests.csproj -nologo -v:minimal --filter "FullyQualifiedName~Wms.Catalog"` and record results in the implementation summary
- [ ] T051 Run full regression tests with `dotnet test Myrmex.Tests/Myrmex.Tests.csproj -nologo -v:minimal` and record results in the implementation summary
- [ ] T052 Validate migration and model snapshot do not add `NormalizedCode`, Inventory, Barcode, UoM, Packaging, Receiving, LPN, Picking, Shipping, or Integration artifacts in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations`
- [ ] T053 Validate final diff does not create `Myrmex.Core/Domain/Entity.cs` or move/rewrite existing `Myrmex.WebApp/Wms/Topology` API client infrastructure
- [ ] T054 Manually validate `/wms/catalog/skus` create, list, search, sort, update, deactivate, reactivate, include-inactive, and validation-error behavior against `specs/032-implement-wms-catalog-sku-mvp-vertical-slice/quickstart.md`
- [ ] T055 Confirm the Constitution v1.0.1 endpoint/UI automated-test exception is documented in `specs/032-implement-wms-catalog-sku-mvp-vertical-slice/plan.md` and validated through `quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Backend Declarations (Phase 2)**: Depends on Setup; contains non-behavior declarations only.
- **US1 Backend (Phase 3)**: Depends on Backend Declarations; this is the MVP.
- **US2 Backend (Phase 4)**: Depends on US1 because list/get requires the StockKeepingUnit aggregate and persistence.
- **US3 Backend (Phase 5)**: Depends on US1 and uses US2 list behavior for lifecycle visibility checks.
- **Frontend SKU Page (Phase 6)**: Depends on US1, US2, and US3 backend/API-client contracts.
- **Validation (Phase 7)**: Depends on all desired phases being complete.

### User Story Dependencies

- **US1 Create SKU Reference Data**: MVP and prerequisite for later SKU review/maintenance.
- **US2 Find and Review SKUs**: Requires StockKeepingUnit persistence and create path from US1.
- **US3 Maintain SKU Details and Lifecycle**: Requires StockKeepingUnit aggregate and list behavior from US1/US2.

### Within Each Backend User Story

- Tests must be written before behavior-changing implementation tasks in the same story.
- Domain and persistence implementation must precede handlers.
- Behavior-changing persistence exception mapping must occur after the duplicate-code tests.
- Handlers must precede endpoints.
- Endpoints and contracts must precede API client integration.
- Browser UI tasks must wait until backend/API-client capabilities exist.

---

## Parallel Execution Examples

### User Story 1 Backend

```text
Task: "T006 Add StockKeepingUnit create validation tests in Myrmex.Tests/Wms/Catalog/Domain/StockKeepingUnitTests.cs"
Task: "T007 Add CreateStockKeepingUnit handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnitHandlerTests.cs"
Task: "T008 Add persistence tests in Myrmex.Tests/Wms/Catalog/Persistence/StockKeepingUnitPersistenceTests.cs"
Task: "T009 Add WmsCatalogApiClient create error-handling tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

### User Story 2 Backend

```text
Task: "T024 Add ListStockKeepingUnits handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs"
Task: "T025 Add GetStockKeepingUnitById handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/GetStockKeepingUnitByIdHandlerTests.cs"
Task: "T026 Add WmsCatalogApiClient read/load error-handling tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

### User Story 3 Backend

```text
Task: "T032 Add UpdateStockKeepingUnitDetails handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetailsHandlerTests.cs"
Task: "T033 Add DeactivateStockKeepingUnit handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/DeactivateStockKeepingUnitHandlerTests.cs"
Task: "T034 Add ReactivateStockKeepingUnit handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ReactivateStockKeepingUnitHandlerTests.cs"
Task: "T035 Add WmsCatalogApiClient lifecycle error-handling tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

### Frontend SKU Page

```text
Task: "T045 Implement SKU filters component in Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuFilters.razor"
Task: "T046 Implement SKU grid component in Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuGrid.razor"
Task: "T047 Implement SKU create/edit dialog in Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuEditDialog.razor"
```

---

## Implementation Strategy

### MVP First (Backend US1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 non-behavior backend declarations.
3. Complete Phase 3 US1 backend/API-client tests and implementation.
4. Stop and validate create behavior, duplicate-code rejection, normalized `Code`, nullable `UpdatedAtUtc`, and no forbidden fields.

### Incremental Delivery

1. Add US1 backend create path and persistence.
2. Add US2 backend list/get/search/sort.
3. Add US3 backend update/deactivate/reactivate.
4. Add browser UI page, filters, grid, dialog, and action wiring.
5. Run full validation after each checkpoint.

### Scope Guardrails

- Do not add `NormalizedCode`.
- Do not add or reference `Myrmex.Core/Domain/Entity.cs`.
- Do not move or rewrite existing WMS Topology API client support types.
- Do not add SQL Server-specific migration execution tests.
- Do not implement Inventory, Barcode, UoM, Packaging, Receiving, LPN contents, Picking, Shipping, Integration, MediatR, new frameworks, or broad refactoring.
