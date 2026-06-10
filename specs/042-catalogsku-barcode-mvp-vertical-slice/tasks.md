# Tasks: Catalog/SKU Barcode MVP Vertical Slice

**Input**: Design documents from `specs/042-catalogsku-barcode-mvp-vertical-slice/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/catalog-sku-barcode-api-contract.md`, `quickstart.md`

**Tests**: Required before implementation for changed domain rules, command/query handlers, persistence mappings/indexes/case-sensitive uniqueness, and Catalog API client behavior where client support follows the existing Catalog client pattern. Endpoint/UI automation remains deferred per the plan; use lower-level automated coverage plus manual API validation in `quickstart.md`.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Maps to user stories from `spec.md`
- Every task includes concrete file paths

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the SKU barcode slice folders and review existing Catalog patterns without changing behavior.

- [X] T001 Create SKU barcode source and test folders in `Myrmex.Modules.Wms/Catalog/Domain/SkuBarcodes`, `Myrmex.Modules.Wms/Catalog/Features/SkuBarcodes`, `Myrmex.Tests/Wms/Catalog/Domain`, `Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes`, and `Myrmex.Tests/Wms/Catalog/Persistence`
- [X] T002 [P] Review existing SKU aggregate, UoM aggregate, and Catalog feature patterns before implementation in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs`, `Myrmex.Modules.Wms/Catalog/Domain/UnitsOfMeasure/UnitOfMeasure.cs`, `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/CreateStockKeepingUnit.cs`, and `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/CreateUnitOfMeasure.cs`
- [X] T003 [P] Review existing Catalog endpoint and client patterns before implementation in `Myrmex.Modules.Wms/Catalog/Endpoints/StockKeepingUnitEndpoints.cs`, `Myrmex.Modules.Wms/Catalog/Endpoints/UnitOfMeasureEndpoints.cs`, and `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`
- [X] T004 [P] Review existing Catalog persistence and test infrastructure before implementation in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`, `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/UnitOfMeasureConfiguration.cs`, `Myrmex.Tests/Wms/Catalog/Persistence/StockKeepingUnitPersistenceTests.cs`, and `Myrmex.Tests/Wms/Catalog/Persistence/UnitOfMeasurePersistenceTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No production behavior is added here. User Story 1 establishes the shared SKU barcode aggregate, persistence, endpoint group, and client primitives after the required tests are written.

**Checkpoint**: Setup complete - User Story 1 can begin.

---

## Phase 3: User Story 1 - Assign SKU Barcodes (Priority: P1) MVP

**Goal**: A catalog user can assign an active barcode value with symbology and primary flag to an existing SKU.

**Independent Test**: Create an existing SKU, assign barcode value `  AbC-123  ` with symbology `Code128`, confirm stored value `AbC-123`, casing preserved, active state, timestamps, required SKU relationship, and duplicate/case-sensitive behavior.

### Tests for User Story 1

- [X] T005 [P] [US1] Add domain tests for create defaults, trimming-only normalization, casing preservation, required value, supported `BarcodeSymbology`, and no `NormalizedValue` behavior in `Myrmex.Tests/Wms/Catalog/Domain/SkuBarcodeTests.cs`
- [X] T006 [P] [US1] Add create handler tests for missing SKU, blank-after-trim value validation, duplicate trimmed value conflict, and case-sensitive coexistence of `abc` and `ABC` in `Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/CreateSkuBarcodeHandlerTests.cs`
- [X] T007 [US1] Add create handler tests for explicit `IsPrimary=true` clearing other active primary barcodes for the same SKU in `Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/CreateSkuBarcodeHandlerTests.cs`
- [X] T008 [P] [US1] Add persistence tests for `sku_barcodes` mapping, required `StockKeepingUnitId` relationship, string `Symbology`, no `NormalizedValue`, unique trimmed `Value`, and case-sensitive coexistence of `abc` and `ABC` in `Myrmex.Tests/Wms/Catalog/Persistence/SkuBarcodePersistenceTests.cs`
- [X] T009 [P] [US1] Add Catalog API client create route, DTO, `ApiResult<T>`, validation, missing-SKU, and duplicate-value tests in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 1

- [X] T010 [P] [US1] Add `BarcodeSymbology` constrained values `Unknown`, `Ean13`, `Ean8`, `UpcA`, `Code128`, `QrCode`, and `Internal` in `Myrmex.Modules.Wms/Catalog/Domain/SkuBarcodes/BarcodeSymbology.cs`
- [X] T011 [P] [US1] Add SKU barcode domain events for created, details updated, deactivated, and reactivated changes in `Myrmex.Modules.Wms/Catalog/Domain/SkuBarcodes/SkuBarcodeEvents.cs`
- [X] T012 [US1] Implement `SkuBarcode` aggregate with `StockKeepingUnitId`, trimmed `Value`, `Symbology`, `IsPrimary`, active state, timestamps, validation, create factory, and primary-selection helpers in `Myrmex.Modules.Wms/Catalog/Domain/SkuBarcodes/SkuBarcode.cs`
- [X] T013 [US1] Add SKU barcode validation, duplicate-value, missing-SKU, not-found, and unsupported-primary-change errors in `Myrmex.Modules.Wms/WmsErrors.cs`
- [X] T014 [US1] Add `SkuBarcodes` DbSet to the WMS context in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`
- [X] T015 [US1] Add SKU barcode table, primary key, required columns, SKU foreign key, `StockKeepingUnitId` index, string `Symbology`, and provider-appropriate case-sensitive unique `Value` configuration in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/SkuBarcodeConfiguration.cs`
- [X] T016 [US1] Add SKU barcode table, key, foreign key, value index, and SKU index constants in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- [X] T017 [US1] Implement `SkuBarcodeDetails` projection with `Id`, `StockKeepingUnitId`, `Value`, `Symbology`, `IsPrimary`, `IsActive`, `CreatedAtUtc`, and `UpdatedAtUtc` in `Myrmex.Modules.Wms/Catalog/Features/SkuBarcodes/SkuBarcodeDetails.cs`
- [X] T018 [US1] Implement `CreateSkuBarcode` command and handler with SKU existence check, trimming-only normalization, case-sensitive duplicate check, symbology validation, primary clearing for explicit create, and persistence failure handling in `Myrmex.Modules.Wms/Catalog/Features/SkuBarcodes/CreateSkuBarcode.cs`
- [X] T019 [US1] Add initial `SkuBarcodeEndpoints` with create route `POST /api/wms/catalog/sku-barcodes` in `Myrmex.Modules.Wms/Catalog/Endpoints/SkuBarcodeEndpoints.cs`
- [X] T020 [US1] Register SKU barcode endpoints in the Catalog route group in `Myrmex.Modules.Wms/Catalog/Endpoints/CatalogEndpoints.cs`
- [X] T021 [US1] Add SKU barcode create DTOs and `TryCreateSkuBarcodeAsync` to the existing Catalog client in `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`
- [ ] T022 [US1] Review developer-generated `AddSkuBarcodes` migration artifacts for the expected table, SKU foreign key, string `Symbology`, provider-appropriate case-sensitive `Value` uniqueness, no `NormalizedValue`, and no forbidden tables in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations`

**Checkpoint**: User Story 1 should create SKU barcodes independently through domain, handler, persistence, endpoint, and client paths.

---

## Phase 4: User Story 2 - Find and Review SKU Barcodes (Priority: P2)

**Goal**: A catalog user can list SKU barcodes, filter by SKU, include inactive records when requested, and retrieve one barcode by identity.

**Independent Test**: Create multiple SKU barcodes across at least two SKUs, list active records, filter by `StockKeepingUnitId`, include inactive records, search by `Value`, sort by supported fields, and get one active or inactive barcode by id.

### Tests for User Story 2

- [X] T023 [P] [US2] Add list handler tests for bounded paging, default active-only behavior, include-inactive behavior, `StockKeepingUnitId` filtering, value search, supported sorting by `value`, `symbology`, and `isActive`, and fallback sorting in `Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/ListSkuBarcodesHandlerTests.cs`
- [X] T024 [P] [US2] Add get-by-id handler tests for active barcode, inactive barcode, and missing barcode behavior in `Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/GetSkuBarcodeByIdHandlerTests.cs`
- [X] T025 [US2] Add Catalog API client list/get route, query string, DTO, read/load exception, and optional SKU filter tests in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 2

- [X] T026 [US2] Implement `ListSkuBarcodes` query and handler with bounded paging, active-only default, include-inactive flag, optional `StockKeepingUnitId` filter, value search, supported sorting, and provider-safe fallback ordering in `Myrmex.Modules.Wms/Catalog/Features/SkuBarcodes/ListSkuBarcodes.cs`
- [X] T027 [US2] Implement `GetSkuBarcodeById` query and handler returning active or inactive barcode details and not-found errors in `Myrmex.Modules.Wms/Catalog/Features/SkuBarcodes/GetSkuBarcodeById.cs`
- [X] T028 [US2] Add list and get routes to `SkuBarcodeEndpoints` in `Myrmex.Modules.Wms/Catalog/Endpoints/SkuBarcodeEndpoints.cs`
- [X] T029 [US2] Add `ListSkuBarcodesRequest`, `ListSkuBarcodesAsync`, and `GetSkuBarcodeByIdAsync` to the existing Catalog client in `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

**Checkpoint**: User Stories 1 and 2 should both work independently without UI screens.

---

## Phase 5: User Story 3 - Maintain Barcode Details and Lifecycle (Priority: P3)

**Goal**: A catalog user can update barcode value, symbology, and primary flag, deactivate barcodes, and reactivate barcodes while preserving the final primary lifecycle rules.

**Independent Test**: Update a barcode, select a new primary barcode, reject `IsPrimary=true` update on an inactive barcode, deactivate a primary barcode without promotion, and reactivate a barcode as non-primary.

### Tests for User Story 3

- [X] T030 [P] [US3] Add domain tests for update behavior, explicit primary selection, deactivate clearing `IsPrimary` only on the deactivated barcode, no promotion, reactivation as non-primary, and idempotent lifecycle calls in `Myrmex.Tests/Wms/Catalog/Domain/SkuBarcodeTests.cs`
- [X] T031 [P] [US3] Add update handler tests for trimming-only update normalization, case-sensitive duplicate conflicts, symbology validation, explicit `IsPrimary=true` clearing other active primary barcodes, explicit `IsPrimary=false`, and rejecting `IsPrimary=true` update on an inactive barcode in `Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/UpdateSkuBarcodeDetailsHandlerTests.cs`
- [X] T032 [P] [US3] Add lifecycle handler tests for deactivate primary clearing only the deactivated barcode, no automatic promotion, SKU with zero active primary barcodes, reactivate returning non-primary, and lifecycle idempotency in `Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/SkuBarcodeLifecycleHandlerTests.cs`
- [X] T033 [US3] Add Catalog API client update, deactivate, reactivate, unsupported-primary-change, and lifecycle result tests in `Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs`

### Implementation for User Story 3

- [X] T034 [US3] Extend `SkuBarcode` aggregate update and lifecycle methods for trimmed value updates, symbology changes, explicit primary changes, inactive primary update rejection, deactivate primary clearing, no promotion, reactivation as non-primary, and idempotency in `Myrmex.Modules.Wms/Catalog/Domain/SkuBarcodes/SkuBarcode.cs`
- [X] T035 [US3] Implement `UpdateSkuBarcodeDetails` command and handler with duplicate checks, unsupported inactive-primary update handling, explicit primary clearing for active barcode updates, and existing result conventions in `Myrmex.Modules.Wms/Catalog/Features/SkuBarcodes/UpdateSkuBarcodeDetails.cs`
- [X] T036 [US3] Implement `DeactivateSkuBarcode` command and handler with primary clearing only on the deactivated barcode, no promotion, idempotency, and updated timestamp behavior in `Myrmex.Modules.Wms/Catalog/Features/SkuBarcodes/DeactivateSkuBarcode.cs`
- [X] T037 [US3] Implement `ReactivateSkuBarcode` command and handler with active state restoration, non-primary default, idempotency, and updated timestamp behavior in `Myrmex.Modules.Wms/Catalog/Features/SkuBarcodes/ReactivateSkuBarcode.cs`
- [X] T038 [US3] Add update, deactivate, and reactivate routes to `SkuBarcodeEndpoints` in `Myrmex.Modules.Wms/Catalog/Endpoints/SkuBarcodeEndpoints.cs`
- [X] T039 [US3] Add `UpdateSkuBarcodeDetailsRequest`, `TryUpdateSkuBarcodeDetailsAsync`, `TryDeactivateSkuBarcodeAsync`, and `TryReactivateSkuBarcodeAsync` to the existing Catalog client in `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

**Checkpoint**: All user stories should be independently functional through API/domain/handler/persistence/client paths, with UI still out of scope.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final scope, regression, and documentation checks without adding new feature scope.

- [ ] T040 [P] Verify no UI phase was added by reviewing absence of SKU barcode pages, navigation, dialogs, grids, forms, and component tests under `Myrmex.WebApp/Components/Pages/Wms/Catalog` and `Myrmex.Tests`
- [ ] T041 [P] Verify no generic barcode abstraction was added by reviewing final changes for BarcodeType reference data, generic Barcode table, Barcode module, OwnerType/OwnerId, IHasBarcodes, generic ownership, scanning, printing, labels, GS1, check digit validation, packaging, SKU/UoM conversion, inventory, receiving, LPN, picking, shipping, and integration in `Myrmex.Modules.Wms`, `Myrmex.WebApp`, and `Myrmex.Tests`
- [ ] T042 [P] Update implementation notes if task execution changes validation commands or migration review expectations in `specs/042-catalogsku-barcode-mvp-vertical-slice/quickstart.md`
- [ ] T043 Review final implementation against the API contract in `specs/042-catalogsku-barcode-mvp-vertical-slice/contracts/catalog-sku-barcode-api-contract.md`
- [ ] T044 Review final implementation against the data model in `specs/042-catalogsku-barcode-mvp-vertical-slice/data-model.md`

---

## Developer-Controlled Commands

These commands are recommended validation or migration commands for the developer to run manually. Codex must not run them automatically.

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~SkuBarcode|FullyQualifiedName~SkuBarcodes" -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
dotnet ef migrations add AddSkuBarcodes --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext --output-dir Infrastructure\Persistence\Migrations
dotnet ef database update --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext
```

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and intentionally contains no production behavior.
- **User Story 1 (Phase 3)**: Depends on Setup; establishes the shared SKU barcode aggregate, persistence, endpoints, and client create path.
- **User Story 2 (Phase 4)**: Depends on User Story 1 because list/get require the aggregate, mapping, and details projection.
- **User Story 3 (Phase 5)**: Depends on User Story 1; may run alongside User Story 2 after shared aggregate and persistence exist.
- **Polish (Phase 6)**: Depends on all implemented user stories.

### User Story Dependencies

- **User Story 1 (P1)**: MVP. No dependency on other user stories.
- **User Story 2 (P2)**: Depends on the `SkuBarcode` aggregate, details projection, persistence mapping, and seeded/created barcode records from US1.
- **User Story 3 (P3)**: Depends on the `SkuBarcode` aggregate, details projection, persistence mapping, and created barcode records from US1.

### Within Each User Story

- Required tests must be written before implementation tasks in that story.
- Domain and persistence model tasks precede handlers.
- Handlers precede endpoint routes.
- Endpoint routes precede API client methods.
- Migration generation remains developer-controlled; after the developer generates migration artifacts, review them with T022.

### Parallel Opportunities

- Setup review tasks T002, T003, and T004 can run in parallel.
- US1 test tasks T005 through T009 can run in parallel after setup.
- US2 handler tests T023 and T024 can run in parallel; T025 shares the Catalog client test file and should be coordinated with other client-test edits.
- US3 domain/handler tests T030 through T032 can run in parallel; T033 shares the Catalog client test file and should be coordinated with other client-test edits.
- Polish checks T040 through T042 can run in parallel after implementation.

---

## Parallel Example: User Story 1

```text
Task: "T005 Add domain tests in Myrmex.Tests/Wms/Catalog/Domain/SkuBarcodeTests.cs"
Task: "T006 Add create handler validation and uniqueness tests in Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/CreateSkuBarcodeHandlerTests.cs"
Task: "T008 Add persistence mapping and case-sensitive uniqueness tests in Myrmex.Tests/Wms/Catalog/Persistence/SkuBarcodePersistenceTests.cs"
Task: "T009 Add Catalog API client create tests in Myrmex.Tests/Wms/Catalog/Client/WmsCatalogApiClientTests.cs"
```

---

## Parallel Example: User Story 2

```text
Task: "T023 Add list handler tests in Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/ListSkuBarcodesHandlerTests.cs"
Task: "T024 Add get-by-id handler tests in Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/GetSkuBarcodeByIdHandlerTests.cs"
```

---

## Parallel Example: User Story 3

```text
Task: "T030 Add lifecycle domain tests in Myrmex.Tests/Wms/Catalog/Domain/SkuBarcodeTests.cs"
Task: "T031 Add update handler tests in Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/UpdateSkuBarcodeDetailsHandlerTests.cs"
Task: "T032 Add lifecycle handler tests in Myrmex.Tests/Wms/Catalog/Features/SkuBarcodes/SkuBarcodeLifecycleHandlerTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 3 User Story 1 tests.
3. Implement the SKU barcode aggregate, create handler, persistence mapping, create endpoint, and create client support.
4. Stop and validate User Story 1 with focused tests and manual API checks from `quickstart.md`.

### Incremental Delivery

1. Add User Story 1 to support assigning SKU barcodes.
2. Add User Story 2 to support review/list/get workflows.
3. Add User Story 3 to support detail updates and lifecycle behavior.
4. Run developer-controlled focused and regression validation after each completed story.

### Scope Guard

Do not add UI pages, generic barcode ownership, BarcodeType reference data, scanning/printing/labels/GS1/check digit validation, packaging, SKU/UoM conversion, inventory, receiving, LPN, picking, shipping, integration, new frameworks, MediatR, or broad refactoring.
