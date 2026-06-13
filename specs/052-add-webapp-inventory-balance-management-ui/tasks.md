# Tasks: WebApp Inventory Balance Management UI

**Input**: Design documents from `specs/052-add-webapp-inventory-balance-management-ui/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/inventory-balance-webapp-ui-contract.md`, `quickstart.md`

**Tests**: UI/component automation is deferred per `plan.md` because the test project has no component-test infrastructure. Existing Inventory Balance domain, handler, persistence, and API client tests protect backend/client contracts. This task list includes focused WebApp wiring tasks and required manual UI validation tasks instead of adding a new UI test framework.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently after shared setup.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it edits different files and does not depend on incomplete tasks in the same phase.
- **[Story]**: Maps to a user story from `spec.md`.
- Every task includes an exact repository path or feature artifact path.

## Phase 1: Setup (Shared Context)

**Purpose**: Prepare the WebApp Inventory UI area and review the existing WMS UI/client patterns before behavior changes.

- [X] T001 [P] Review existing WMS page/filter/grid/dialog conventions in `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/Index.razor`, `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/StorageLocationFilters.razor`, `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/StorageLocationGrid.razor`, and `Myrmex.WebApp/Components/Pages/Wms/Topology/StorageLocationPages/StorageLocationEditDialog.razor`
- [X] T002 [P] Review existing Catalog SKU lookup and base UoM display patterns in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/Index.razor.cs` and `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuEditDialog.razor`
- [X] T003 [P] Review Inventory, Catalog, and Topology client contracts in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`, `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`, and `Myrmex.WebApp/Wms/Topology/WmsTopologyApiClient.cs`
- [X] T004 Create the Inventory Balance page component folder in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared WebApp wiring that all Inventory Balance page stories depend on.

**Critical**: No user story implementation should begin until this phase is complete.

- [X] T005 Register `WmsInventoryApiClient` as a typed HTTP client with the existing API service base address in `Myrmex.WebApp/Program.cs`
- [X] T006 [P] Add required Inventory namespace imports if needed for the new page components in `Myrmex.WebApp/Components/_Imports.razor`

**Checkpoint**: WebApp can inject the Inventory API client and compile new Inventory page components.

---

## Phase 3: User Story 1 - View Current Inventory Balances (Priority: P1) MVP

**Goal**: A warehouse operations user can navigate to Inventory Balances and see a bounded list of current stock with SKU, warehouse, storage location, quantity, and base UoM context.

**Independent Test**: Open the Inventory Balances page and confirm it loads, displays existing balances with required row context, keeps zero quantity rows visible, and shows an empty state for successful no-result lists.

### Implementation for User Story 1

- [X] T007 [US1] Add Inventory navigation with an Inventory Balances link under WMS in `Myrmex.WebApp/Components/Layout/NavMenu.razor`
- [X] T008 [US1] Create the Inventory Balances page shell with route `/wms/inventory/balances`, title, description, create action placeholder, refresh action, error alert area, and grid placeholder in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor`
- [X] T009 [US1] Implement page state, injected `WmsInventoryApiClient`, initial list loading, refresh behavior, loading flag, page-level error message, and empty-result handling in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`
- [X] T010 [US1] Create the Inventory Balance grid with SKU, warehouse, storage location, quantity, base UoM, optional timestamps, and a placeholder row action area in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor`
- [X] T011 [US1] Wire `InventoryBalanceGrid` into the page with loading and empty state behavior in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor`

**Checkpoint**: User Story 1 is independently usable as the MVP stock visibility page.

---

## Phase 4: User Story 2 - Find Balances by Warehouse, Location, or SKU (Priority: P2)

**Goal**: A warehouse operations user can filter balances by warehouse, storage location, SKU, and SKU within warehouse using warehouse-first storage location lookup behavior.

**Independent Test**: Apply warehouse, storage location, SKU, and combined SKU+warehouse filters and confirm list results match; confirm storage location selection is disabled until warehouse is selected and clears when warehouse changes incompatibly.

### Implementation for User Story 2

- [X] T012 [US2] Create `InventoryBalanceFilters` with warehouse, storage location, and SKU selectors plus loading/disabled states in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceFilters.razor`
- [X] T013 [US2] Add warehouse, SKU, and warehouse-scoped storage location lookup state and loading methods using `WmsTopologyApiClient` and `WmsCatalogApiClient` in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`
- [X] T014 [US2] Implement filter change handlers that reload the balance list, disable storage location selection until warehouse is selected, clear storage location when warehouse is cleared or changed, and pass `WarehouseId`, `StorageLocationId`, and `StockKeepingUnitId` to `ListInventoryBalancesRequest` in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`
- [X] T015 [US2] Wire `InventoryBalanceFilters` above the grid with lookup loading states and selected filter values in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor`
- [X] T016 [US2] Ensure filter no-result behavior shows the page empty state rather than an error in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor`

**Checkpoint**: User Stories 1 and 2 work independently; users can answer stock visibility questions by warehouse, location, SKU, and SKU within warehouse.

---

## Phase 5: User Story 3 - Create an Initial Inventory Balance (Priority: P3)

**Goal**: A warehouse operations user can manually create a current balance for an active SKU at an eligible storage location.

**Independent Test**: Open create, select SKU, warehouse, storage location, and non-negative quantity, confirm base UoM context appears, submit successfully, and confirm the refreshed list includes the created balance when it matches active filters.

### Implementation for User Story 3

- [X] T017 [US3] Create the create dialog structure with SKU, read-only base UoM display, warehouse, storage location, quantity, dialog-local error, cancel, save, loading, and saving states in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/CreateInventoryBalanceDialog.razor`
- [X] T018 [US3] Implement create dialog lookup loading for active SKUs, active warehouses, and warehouse-scoped storage locations using `WmsCatalogApiClient` and `WmsTopologyApiClient` in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/CreateInventoryBalanceDialog.razor`
- [X] T019 [US3] Implement create dialog validation for required SKU, required warehouse, required storage location, non-negative quantity, disabled storage location before warehouse selection, and read-only base UoM display in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/CreateInventoryBalanceDialog.razor`
- [X] T020 [US3] Implement create submission with `CreateInventoryBalanceRequest`, `TryCreateInventoryBalanceAsync`, dialog-local failure handling, duplicate conflict display, and success close result in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/CreateInventoryBalanceDialog.razor`
- [X] T021 [US3] Wire the page create button to open `CreateInventoryBalanceDialog`, show success feedback, refresh the balance list, and preserve active filters where practical in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`
- [X] T022 [US3] Ensure created balances that do not match active filters produce non-misleading success feedback while preserving list state in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`

**Checkpoint**: User Stories 1 through 3 work independently; users can view, filter, and create current inventory balances.

---

## Phase 6: User Story 4 - Correct Current Quantity Only (Priority: P4)

**Goal**: A warehouse operations user can update only the current quantity of an existing inventory balance while keeping SKU, warehouse, storage location, and base UoM read-only.

**Independent Test**: Open the update action from a row, confirm only quantity is editable, submit a non-negative value, and confirm the refreshed list shows the updated quantity for the same SKU/location context.

### Implementation for User Story 4

- [X] T023 [US4] Create the quantity update dialog with read-only SKU, warehouse, storage location, base UoM context, editable quantity, dialog-local error, cancel, save, and saving state in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/UpdateInventoryBalanceQuantityDialog.razor`
- [X] T024 [US4] Implement update dialog validation for required non-negative quantity and no editable SKU, warehouse, storage location, or base UoM fields in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/UpdateInventoryBalanceQuantityDialog.razor`
- [X] T025 [US4] Implement quantity-only submission with `UpdateInventoryBalanceQuantityRequest`, `TryUpdateInventoryBalanceQuantityAsync`, validation failure handling, not-found handling, and success close result in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/UpdateInventoryBalanceQuantityDialog.razor`
- [X] T026 [US4] Add the grid row action to request quantity update without delete, deactivate, reactivate, movement, transaction, or adjustment actions in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor`
- [X] T027 [US4] Wire the row update action to open `UpdateInventoryBalanceQuantityDialog`, show success feedback, refresh the balance list, and preserve active filters where practical in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`

**Checkpoint**: All user stories are independently functional; users can view, filter, create, and quantity-correct Inventory Balances without changing SKU/location identity.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate scope boundaries, manual UI behavior, and developer-controlled command guidance across the completed feature.

- [X] T028 [P] Review final implementation against the WebApp UI contract in `specs/052-add-webapp-inventory-balance-management-ui/contracts/inventory-balance-webapp-ui-contract.md`
- [X] T029 [P] Review final implementation against UI state and validation rules in `specs/052-add-webapp-inventory-balance-management-ui/data-model.md`
- [X] T030 [P] Confirm the final diff adds no backend domain, handler, endpoint, persistence, EF migration, database update, seed/demo data, external integration, delete, deactivate/reactivate, transaction, movement, adjustment, receiving, putaway, picking, shipping, LPN, batch/lot, expiry, serial number, UoM conversion, packaging, or cycle counting behavior using `specs/052-add-webapp-inventory-balance-management-ui/quickstart.md`
- [ ] T031 Perform manual UI smoke validation for navigation, list loading, zero quantity visibility, empty state, page error state, and grid row context using `specs/052-add-webapp-inventory-balance-management-ui/quickstart.md`
- [ ] T032 Perform manual filter validation for warehouse, disabled storage location before warehouse, warehouse-scoped storage locations, storage-location clearing on warehouse change, SKU filter, and SKU-within-warehouse filter using `specs/052-add-webapp-inventory-balance-management-ui/quickstart.md`
- [ ] T033 Perform manual create dialog validation for lookup loading, base UoM display, warehouse-first storage location selection, zero quantity, negative quantity rejection, duplicate conflict display, success feedback, and list refresh using `specs/052-add-webapp-inventory-balance-management-ui/quickstart.md`
- [ ] T034 Perform manual update dialog validation for read-only context, quantity-only edit, zero quantity, negative quantity rejection, not-found feedback, success feedback, and list refresh using `specs/052-add-webapp-inventory-balance-management-ui/quickstart.md`
- [X] T035 Recommend developer-controlled validation commands for build, focused tests, full regression tests, and app startup from `specs/052-add-webapp-inventory-balance-management-ui/quickstart.md` without running them automatically

---

## Developer-Controlled Commands

These commands are recommended validation commands for the developer to run manually. Codex must not run them automatically.

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~WmsInventoryApiClient|FullyQualifiedName~InventoryBalance" -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
dotnet run --project Myrmex.AppHost\Myrmex.AppHost.csproj
```

No EF migration generation or database update command is expected for this feature.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundation; delivers the MVP visible Inventory Balances page.
- **User Story 2 (Phase 4)**: Depends on US1 page/list state; adds filters and lookup behavior.
- **User Story 3 (Phase 5)**: Depends on US1 page shell and US2 lookup behavior; adds create flow.
- **User Story 4 (Phase 6)**: Depends on US1 grid/list state; can proceed after grid row actions are available, but final validation benefits from US2 filters.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 View Current Inventory Balances**: MVP. Requires Foundation only.
- **US2 Find Balances by Warehouse, Location, or SKU**: Requires US1 list loading and page state.
- **US3 Create an Initial Inventory Balance**: Requires US1 page shell and US2 warehouse-scoped lookup behavior.
- **US4 Correct Current Quantity Only**: Requires US1 grid/list state and can be implemented independently from create after row actions exist.

### Within Each User Story

- Shared client registration precedes component injection.
- Page shell precedes filter, grid, and dialog wiring.
- Lookup loading precedes selectors that depend on lookup data.
- Dialog validation precedes submit behavior.
- Submit behavior precedes success feedback and refresh behavior.
- Manual validation happens after implementation and developer-controlled app startup.

---

## Parallel Opportunities

- Setup review tasks T001, T002, and T003 can run in parallel.
- Foundational import task T006 can run in parallel with T005 if it does not depend on the final `Program.cs` edit.
- In US1, T010 can be prepared after the expected grid contract is known, while T009 implements page loading in a different file.
- In US3, T017 can start before T021 because it edits the dialog file while page wiring happens in `Index.razor.cs`.
- In US4, T023 through T025 edit the dialog file and T026 edits the grid file, so T026 can proceed in parallel once the row action contract is agreed.
- Polish review tasks T028, T029, and T030 can run in parallel.

---

## Parallel Example: User Story 1

```text
Task: "T009 [US1] Implement page state, injected WmsInventoryApiClient, initial list loading, refresh behavior, loading flag, page-level error message, and empty-result handling in Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs"
Task: "T010 [US1] Create the Inventory Balance grid with SKU, warehouse, storage location, quantity, base UoM, optional timestamps, and a placeholder row action area in Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor"
```

## Parallel Example: User Story 3

```text
Task: "T017 [US3] Create the create dialog structure with SKU, read-only base UoM display, warehouse, storage location, quantity, dialog-local error, cancel, save, loading, and saving states in Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/CreateInventoryBalanceDialog.razor"
Task: "T021 [US3] Wire the page create button to open CreateInventoryBalanceDialog, show success feedback, refresh the balance list, and preserve active filters where practical in Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs"
```

## Parallel Example: User Story 4

```text
Task: "T023 [US4] Create the quantity update dialog with read-only SKU, warehouse, storage location, base UoM context, editable quantity, dialog-local error, cancel, save, and saving state in Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/UpdateInventoryBalanceQuantityDialog.razor"
Task: "T026 [US4] Add the grid row action to request quantity update without delete, deactivate, reactivate, movement, transaction, or adjustment actions in Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundation.
3. Complete Phase 3 User Story 1.
4. Stop and validate Inventory navigation, page loading, grid display, zero quantity visibility, loading state, empty state, and page-level list error behavior.

### Incremental Delivery

1. Add WebApp Inventory foundation and page visibility.
2. Add filters and warehouse-first storage location behavior.
3. Add create dialog and post-create refresh behavior.
4. Add quantity-only update dialog and post-update refresh behavior.
5. Complete polish, scope review, manual UI validation, and recommended developer-controlled validation command handoff.

### Parallel Team Strategy

1. One developer handles foundation and page/list state.
2. Another developer prepares grid and dialog component shells.
3. After US1, filters, create dialog, and update dialog can be developed in parallel with coordination around `Index.razor.cs`.
4. Coordinate edits to `Index.razor.cs`, because US1, US2, US3, and US4 all extend page state and callbacks.

---

## Notes

- `[P]` tasks are limited to different files or independent review tasks.
- `[US1]`, `[US2]`, `[US3]`, and `[US4]` labels map directly to the user stories in `spec.md`.
- Browser/component automation is intentionally not included; the plan documents the Principle IV exception and quickstart manual validation.
- Build, test, app startup, database update, EF migration generation, EF migration application, and infrastructure-affecting commands remain developer-controlled.
- Avoid backend domain, handler, endpoint, persistence, migration, seed/demo, external integration, inventory movement, transaction, adjustment, delete, deactivate/reactivate, import/export, and bulk edit scope.
