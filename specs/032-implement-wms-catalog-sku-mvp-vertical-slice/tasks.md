# Tasks: WMS Catalog/SKU MVP Vertical Slice

**Input**: Design documents from `specs/032-implement-wms-catalog-sku-mvp-vertical-slice/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/catalog-sku-api-and-ui-contract.md`, `quickstart.md`, `.specify/memory/constitution.md`, and `.specify/memory/myrmex-*.md`.

**Tests**: Required by the Myrmex constitution for new domain rules, command/query handlers, persistence mappings, API client behavior, and critical UI behavior. Test tasks are listed before implementation tasks in each user-story phase.

**Organization**: Tasks are grouped by user story so the MVP can be implemented and validated first, then expanded incrementally.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare Catalog/SKU folders and test folders without changing existing WMS Topology behavior.

- [ ] T001 Create Catalog feature folders in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits`, `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits`, and `Myrmex.Modules.Wms/Catalog/Endpoints`
- [ ] T002 Create Catalog web folders in `Myrmex.WebApp/Wms/Catalog` and `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages`
- [ ] T003 Create Catalog test folders in `Myrmex.Tests/Wms/Catalog/Domain`, `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits`, `Myrmex.Tests/Wms/Catalog/Persistence`, and `Myrmex.Tests/Wms/Catalog/Client`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared Catalog/SKU registration, persistence names, and explicit guardrails that all user stories depend on.

**CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 Add StockKeepingUnit table, primary key, and unique code index constants to `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- [ ] T005 Add StockKeepingUnit error definitions for not found, duplicate code, and create failure to `Myrmex.Modules.Wms/WmsErrors.cs`
- [ ] T006 Add StockKeepingUnit duplicate-code persistence mapping to `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsPersistenceExceptionMapper.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Create SKU Reference Data (Priority: P1) MVP

**Goal**: A catalog user can create a SKU with normalized `Code`, required name, optional description, active status, `UpdatedAtUtc` null on create, duplicate-code protection, and clear validation errors.

**Independent Test**: Create a valid SKU, confirm it persists with normalized `Code` and `UpdatedAtUtc` null, reject invalid input, and reject duplicate normalized codes.

### Tests for User Story 1

> Write these tests first and ensure they fail before implementation.

- [ ] T007 [P] [US1] Add StockKeepingUnit create validation, normalization, active-on-create, `UpdatedAtUtc` null, and created-domain-event tests in `Myrmex.Tests/Wms/Catalog/Domain/StockKeepingUnitTests.cs`
- [ ] T008 [P] [US1] Add CreateStockKeepingUnit handler tests for invalid input, duplicate normalized `Code`, successful create, persisted details, and dispatched created event in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnitHandlerTests.cs`
- [ ] T009 [P] [US1] Add practical SQLite/EnsureCreated persistence tests for StockKeepingUnit table creation, absence of a `NormalizedCode` column, and unique normalized `Code` index in `Myrmex.Tests/Wms/Catalog/Persistence/StockKeepingUnitPersistenceTests.cs`
- [ ] T010 [P] [US1] Add WmsCatalogApiClient create error-handling tests for ProblemDetails and malformed error fallback in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 1

- [ ] T011 [US1] Implement StockKeepingUnit aggregate using existing `AggregateRoot`/`EntityBase` patterns in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`
- [ ] T012 [US1] Implement StockKeepingUnit created domain event in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnitEvents.cs`
- [ ] T013 [US1] Implement StockKeepingUnitDetails projection in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/StockKeepingUnitDetails.cs`
- [ ] T014 [US1] Add StockKeepingUnit DbSet to `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`
- [ ] T015 [US1] Implement StockKeepingUnit EF Core mapping with stored normalized `Code`, no `NormalizedCode`, nullable `UpdatedAtUtc`, and unique code index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`
- [ ] T016 [US1] Generate AddStockKeepingUnits EF Core migration in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/{timestamp}_AddStockKeepingUnits.cs` and update `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`
- [ ] T017 [US1] Implement CreateStockKeepingUnit command and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnit.cs`
- [ ] T018 [US1] Implement Catalog route group in `Myrmex.Modules.Wms/Catalog/Endpoints/CatalogEndpoints.cs`
- [ ] T019 [US1] Map Catalog endpoints from `Myrmex.Modules.Wms/WmsModule.cs` without changing existing Topology endpoint registration behavior
- [ ] T020 [US1] Implement create SKU endpoint and request contract in `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`
- [ ] T021 [US1] Implement local Catalog `ApiResult<T>` support type in `Myrmex.WebApp/Wms/Catalog/ApiResult.cs` without moving or rewriting Topology client support types
- [ ] T022 [US1] Implement local Catalog `ApiException` support type in `Myrmex.WebApp/Wms/Catalog/ApiException.cs` without moving or rewriting Topology client support types
- [ ] T023 [US1] Implement WmsCatalogApiClient create SKU request, details record, and write/action error handling in `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`
- [ ] T024 [US1] Register WmsCatalogApiClient in `Myrmex.WebApp/Program.cs`

**Checkpoint**: User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 - Find and Review SKUs (Priority: P2)

**Goal**: A catalog user can list active SKUs by default, include inactive SKUs, search by code/name/description, sort by existing local pattern fields, and retrieve a SKU by identity.

**Independent Test**: Create multiple SKUs, list active records, search by code/name, sort by `code`, `name`, `createdAtUtc`, `updatedAtUtc`, and `isActive`, retrieve by identity, and receive not-found for a missing identity.

### Tests for User Story 2

- [ ] T025 [P] [US2] Add ListStockKeepingUnits handler tests for active-only default, include inactive, search, bounded paging, supported sorting, and fallback code ordering in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs`
- [ ] T026 [P] [US2] Add GetStockKeepingUnitById handler tests for existing active SKU, existing inactive SKU, and missing SKU not-found result in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/GetStockKeepingUnitByIdHandlerTests.cs`
- [ ] T027 [P] [US2] Add WmsCatalogApiClient read/load error-handling tests for list/get ProblemDetails and malformed error fallback in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 2

- [ ] T028 [US2] Implement ListStockKeepingUnits query and handler with active filtering, search, bounded paging, and local-pattern sorting in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnits.cs`
- [ ] T029 [US2] Implement GetStockKeepingUnitById query and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/GetStockKeepingUnitById.cs`
- [ ] T030 [US2] Add list and get SKU endpoints to `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`
- [ ] T031 [US2] Add ListRequest, ListResult, list method, and get method to `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`
- [ ] T032 [US2] Implement SKU list page route and loading behavior in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor` and `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor.cs`
- [ ] T033 [US2] Implement SKU filters component for search and include-inactive behavior in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuFilters.razor`
- [ ] T034 [US2] Implement SKU grid component with code, name, description, active state, timestamps, and action menu placeholders in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuGrid.razor`

**Checkpoint**: User Stories 1 and 2 work independently.

---

## Phase 5: User Story 3 - Maintain SKU Details and Lifecycle (Priority: P3)

**Goal**: A catalog user can update SKU name/description without changing code, deactivate active SKUs, reactivate inactive SKUs, and receive idempotent success without lifecycle domain events for no-op deactivate/reactivate calls.

**Independent Test**: Update details, deactivate, verify hidden from default list, include inactive, reactivate, and verify default list visibility returns.

### Tests for User Story 3

- [ ] T035 [P] [US3] Add StockKeepingUnit update/deactivate/reactivate domain tests including `UpdatedAtUtc` changes and no domain event for idempotent no-op lifecycle calls in `Myrmex.Tests/Wms/Catalog/Domain/StockKeepingUnitTests.cs`
- [ ] T036 [P] [US3] Add UpdateStockKeepingUnitDetails handler tests for not found, invalid details, successful update, preserved code, and dispatched details-updated event in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetailsHandlerTests.cs`
- [ ] T037 [P] [US3] Add DeactivateStockKeepingUnit handler tests for not found, successful deactivate, hidden active state, and idempotent no-op without domain event in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/DeactivateStockKeepingUnitHandlerTests.cs`
- [ ] T038 [P] [US3] Add ReactivateStockKeepingUnit handler tests for not found, successful reactivate, restored active state, and idempotent no-op without domain event in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ReactivateStockKeepingUnitHandlerTests.cs`
- [ ] T039 [P] [US3] Add WmsCatalogApiClient update/deactivate/reactivate write/action error-handling tests in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 3

- [ ] T040 [US3] Add StockKeepingUnit details-updated, deactivated, and reactivated domain events in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnitEvents.cs`
- [ ] T041 [US3] Add StockKeepingUnit update, deactivate, and reactivate behavior with `UpdatedAtUtc` updates only on real changes in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`
- [ ] T042 [US3] Implement UpdateStockKeepingUnitDetails command and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetails.cs`
- [ ] T043 [US3] Implement DeactivateStockKeepingUnit command and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/DeactivateStockKeepingUnit.cs`
- [ ] T044 [US3] Implement ReactivateStockKeepingUnit command and handler in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ReactivateStockKeepingUnit.cs`
- [ ] T045 [US3] Add update/deactivate/reactivate endpoints to `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`
- [ ] T046 [US3] Add update/deactivate/reactivate request and client methods to `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`
- [ ] T047 [US3] Implement SKU create/edit dialog with code disabled in edit mode in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuEditDialog.razor`
- [ ] T048 [US3] Wire create, edit, deactivate, reactivate, snackbar, and reload behavior in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor.cs`
- [ ] T049 [US3] Wire SKU grid action callbacks for edit, deactivate, and reactivate in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuGrid.razor`

**Checkpoint**: All user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Validation

**Purpose**: Verify the complete vertical slice, preserve existing behavior, and prevent scope drift.

- [ ] T050 Run solution build with `dotnet build Myrmex.slnx -nologo -v:minimal` and record results in the implementation summary
- [ ] T051 Run Catalog/SKU-focused tests with `dotnet test Myrmex.Tests/Myrmex.Tests.csproj -nologo -v:minimal --filter "FullyQualifiedName~Wms.Catalog"` and record results in the implementation summary
- [ ] T052 Run full regression tests with `dotnet test Myrmex.Tests/Myrmex.Tests.csproj -nologo -v:minimal` and record results in the implementation summary
- [ ] T053 Validate migration and model snapshot do not add `NormalizedCode`, Inventory, Barcode, UoM, Packaging, Receiving, LPN, Picking, Shipping, or Integration artifacts in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations`
- [ ] T054 Validate final diff does not create `Myrmex.Core/Domain/Entity.cs` or move/rewrite existing `Myrmex.WebApp/Wms/Topology` API client infrastructure
- [ ] T055 Manually validate `/wms/catalog/skus` create, list, search, sort, update, deactivate, reactivate, include-inactive, and validation-error behavior against `specs/032-implement-wms-catalog-sku-mvp-vertical-slice/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational; this is the MVP.
- **User Story 2 (Phase 4)**: Depends on User Story 1 because list/get requires the StockKeepingUnit aggregate and persistence.
- **User Story 3 (Phase 5)**: Depends on User Story 1 and can proceed after User Story 2 foundations are available for UI list refresh behavior.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 Create SKU Reference Data**: MVP and prerequisite for later SKU review/maintenance.
- **US2 Find and Review SKUs**: Requires StockKeepingUnit persistence and create path from US1.
- **US3 Maintain SKU Details and Lifecycle**: Requires StockKeepingUnit aggregate and list/page foundation from US1/US2.

### Within Each User Story

- Tests must be written before implementation tasks in the same story.
- Domain and persistence implementation must precede handlers.
- Handlers must precede endpoints.
- Endpoints and contracts must precede web API client integration.
- API client methods must precede page behavior that calls them.

---

## Parallel Execution Examples

### User Story 1

```text
Task: "T007 Add StockKeepingUnit create validation tests in Myrmex.Tests/Wms/Catalog/Domain/StockKeepingUnitTests.cs"
Task: "T008 Add CreateStockKeepingUnit handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnitHandlerTests.cs"
Task: "T009 Add persistence tests in Myrmex.Tests/Wms/Catalog/Persistence/StockKeepingUnitPersistenceTests.cs"
Task: "T010 Add WmsCatalogApiClient create error-handling tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

### User Story 2

```text
Task: "T025 Add ListStockKeepingUnits handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs"
Task: "T026 Add GetStockKeepingUnitById handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/GetStockKeepingUnitByIdHandlerTests.cs"
Task: "T027 Add WmsCatalogApiClient read/load error-handling tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

### User Story 3

```text
Task: "T036 Add UpdateStockKeepingUnitDetails handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/UpdateStockKeepingUnitDetailsHandlerTests.cs"
Task: "T037 Add DeactivateStockKeepingUnit handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/DeactivateStockKeepingUnitHandlerTests.cs"
Task: "T038 Add ReactivateStockKeepingUnit handler tests in Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ReactivateStockKeepingUnitHandlerTests.cs"
Task: "T039 Add WmsCatalogApiClient lifecycle error-handling tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational registration and shared constants.
3. Complete Phase 3 User Story 1 tests and implementation.
4. Stop and validate create behavior, duplicate-code rejection, normalized `Code`, nullable `UpdatedAtUtc`, and no forbidden fields.

### Incremental Delivery

1. Add US1 create path and persistence.
2. Add US2 list/get/search/sort and read UI.
3. Add US3 update/deactivate/reactivate and write UI actions.
4. Run full validation after each user story checkpoint.

### Scope Guardrails

- Do not add `NormalizedCode`.
- Do not add or reference `Myrmex.Core/Domain/Entity.cs`.
- Do not move or rewrite existing WMS Topology API client support types.
- Do not add SQL Server-specific migration execution tests.
- Do not implement Inventory, Barcode, UoM, Packaging, Receiving, LPN contents, Picking, Shipping, Integration, MediatR, new frameworks, or broad refactoring.
