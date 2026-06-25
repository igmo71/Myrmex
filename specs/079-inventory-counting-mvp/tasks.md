# Tasks: Inventory Counting MVP

**Input**: Design documents from `/specs/079-inventory-counting-mvp/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Automated tasks protect the identified lifecycle, persistence, rowversion, atomic adjustment, API-boundary, and client-transport risks. UI behavior follows existing MudBlazor list/dialog patterns and uses the manual smoke validation in `quickstart.md`.

**Organization**: Tasks are grouped by user story. Shared contracts and actor/test infrastructure are established first; each story then adds an independently verifiable operational increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it changes a different file and has no dependency on an incomplete task
- **[Story]**: Maps the task to a user story from `spec.md`
- Every task includes an exact repository file or directory path

## Phase 1: Setup (Shared Contracts)

**Purpose**: Define public transport types used across backend, API client, and WebApp without leaking domain or persistence types.

- [X] T001 [P] Define count and line status constants plus supported list sort keys in `Myrmex.Shared/Wms/Inventory/InventoryCountStatusDetails.cs`, `Myrmex.Shared/Wms/Inventory/InventoryCountLineStatusDetails.cs`, and `Myrmex.Shared/Wms/Inventory/InventoryCountSortBy.cs`
- [X] T002 [P] Define count list/detail, line detail, warehouse/SKU/location/base-UoM, actor audit, progress, version, transaction-link, and supersession response contracts in `Myrmex.Shared/Wms/Inventory/InventoryCountListItem.cs`, `Myrmex.Shared/Wms/Inventory/InventoryCountDetails.cs`, and `Myrmex.Shared/Wms/Inventory/InventoryCountLineDetails.cs`
- [X] T003 [P] Define list filters and all create/add/remove/count/apply/supersede/complete/cancel request contracts without actor input in `Myrmex.Shared/Wms/Inventory/ListInventoryCountsRequest.cs`, `Myrmex.Shared/Wms/Inventory/CreateInventoryCountRequest.cs`, `Myrmex.Shared/Wms/Inventory/AddInventoryCountLineRequest.cs`, `Myrmex.Shared/Wms/Inventory/RemoveInventoryCountLineRequest.cs`, `Myrmex.Shared/Wms/Inventory/RecordInventoryCountLineRequest.cs`, `Myrmex.Shared/Wms/Inventory/ApplyInventoryCountLineRequest.cs`, `Myrmex.Shared/Wms/Inventory/SupersedeInventoryCountLineRequest.cs`, and `Myrmex.Shared/Wms/Inventory/ChangeInventoryCountStatusRequest.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish trusted actor extraction and reusable SQL Server test data required by every write slice.

**Critical**: Complete this phase before user-story handler and endpoint work.

- [X] T004 Implement provider-neutral authenticated actor resolution using `sub`, NameIdentifier, then authenticated Name in `Myrmex.AspNetCore/Security/HttpContextActorExtensions.cs`
- [X] T005 Build reusable count fixtures for active/inactive warehouses, SKUs, regular/transit/cross-warehouse locations, existing/missing balances, actors, and count versions in `Myrmex.Tests/Wms/Inventory/Testing/InventoryCountTestData.cs`

**Checkpoint**: Public contracts, trusted actor extraction, and reusable count fixtures are ready.

---

## Phase 3: User Story 1 - Create a Warehouse Count (Priority: P1) MVP

**Goal**: Create a Draft count, add eligible SKU/location snapshot lines, reject duplicate/ineligible pairs, and remove preparation errors while lines remain Pending.

**Independent Test**: Create a count for an active visible warehouse, add existing- and missing-balance lines, verify captured snapshots and Draft status, reject duplicates/ineligible references, then remove one Pending line without inventory effects.

### Tests for User Story 1

- [X] T006 [P] [US1] Write domain tests for count creation, actor/reason validation, Draft state, Pending snapshot lines, duplicate current-pair rejection, and Pending-only removal in `Myrmex.Tests/Wms/Inventory/Domain/InventoryCountTests.cs`
- [X] T007 [P] [US1] Write SQL Server persistence tests for count/line tables, enum conversion, decimal precision, rowversions, actor/comment limits, restricted foreign keys, current-line filtered uniqueness, and transaction/supersession indexes in `Myrmex.Tests/Wms/Inventory/Persistence/InventoryCountPersistenceTests.cs`
- [X] T008 [US1] Write SQL Server handler tests for create, active warehouse validation, existing/missing balance snapshots, eligible/ineligible references, duplicate current lines, stale count versions, Pending removal, and actor audit in `Myrmex.Tests/Wms/Inventory/Features/InventoryCounts/InventoryCountLineHandlerTests.cs`
- [X] T009 [P] [US1] Add focused endpoint tests for authenticated actor extraction, unauthenticated 401, create/add/remove routing and binding, shared detail serialization, and representative 404/409 results in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryCountEndpointTests.cs`
- [X] T010 [P] [US1] Add API-client tests for create/details/add/remove routes, request bodies or DELETE version query, shared DTO deserialization, cancellation, and representative conflict mapping in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 1

- [X] T011 [P] [US1] Define `InventoryCountStatus` and `InventoryCountLineStatus` domain enums in `Myrmex.Modules.Wms/Inventory/Domain/InventoryCounts/InventoryCountStatus.cs` and `Myrmex.Modules.Wms/Inventory/Domain/InventoryCounts/InventoryCountLineStatus.cs`
- [X] T012 [US1] Implement count creation, line snapshot creation, one-current-pair checks, Pending removal, actor validation, reason/comment limits, rowversion properties, and aggregate navigation in `Myrmex.Modules.Wms/Inventory/Domain/InventoryCounts/InventoryCount.cs` and `Myrmex.Modules.Wms/Inventory/Domain/InventoryCounts/InventoryCountLine.cs`
- [X] T013 [US1] Add inventory-count table, key, foreign-key, index, actor-length, decimal, enum, rowversion, filtered uniqueness, and delete-behavior constants in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- [X] T014 [US1] Configure `InventoryCount` and `InventoryCountLine` persistence, filtered unique indexes, self-reference, applied-transaction link, and field-backed lines in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryCountConfiguration.cs` and `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryCountLineConfiguration.cs`
- [X] T015 [US1] Register count DbSets and mappings in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`
- [X] T016 [US1] Developer-controlled: generate and review the EF Core migration and model snapshot for the two count tables and indexes in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/` and `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`
- [X] T017 [US1] Implement count-details projection with Base64 count/line versions, warehouse/SKU/location/base-UoM labels, all line history, actor audit, and replacement links in `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/InventoryCountQueryableExtensions.cs`
- [X] T018 [US1] Implement create, add-line snapshot/eligibility, and Pending-remove commands with rowversion, duplicate-index, not-found, validation, and cancellation handling in `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/CreateInventoryCount.cs`, `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/AddInventoryCountLine.cs`, and `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/RemoveInventoryCountLine.cs`
- [X] T019 [US1] Implement required-load details query in `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/GetInventoryCountById.cs`
- [X] T020 [US1] Map create, details, add-line, and remove-Pending routes with server-derived actor identity and existing result conventions in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryCountEndpoints.cs` and register them in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs`
- [X] T021 [US1] Add create, details, add-line, and remove-Pending client methods with cancellation and exact route/body construction in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T022 [P] [US1] Add the Inventory Counts navigation link in `Myrmex.WebApp/Components/Layout/NavMenu.razor`
- [X] T023 [P] [US1] Build warehouse/reason creation and warehouse-scoped active SKU/non-transit location add-line dialogs in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/CreateInventoryCountDialog.razor` and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/AddInventoryCountLineDialog.razor`
- [X] T024 [US1] Build the count details page with header audit, current version, Pending line display/removal confirmation, add-line orchestration, error handling, and refresh behavior in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/InventoryCountDetails.razor`

**Checkpoint**: Operators can create and prepare a Draft count with auditable snapshots and remove only Pending preparation errors.

---

## Phase 4: User Story 2 - Record a Physical Count (Priority: P1)

**Goal**: Enter or revise non-negative physical quantities, calculate variance from the immutable snapshot, record counter audit, and move Draft to InProgress on first count entry.

**Independent Test**: Enter quantity 12 against system quantity 10, verify variance +2, counter/time, and InProgress status; revise the quantity and confirm the original system snapshot remains unchanged.

### Tests for User Story 2

- [X] T025 [P] [US2] Extend domain tests for non-negative count entry, immutable system snapshot, variance calculation, counter replacement, first-entry InProgress transition, and incompatible/final states in `Myrmex.Tests/Wms/Inventory/Domain/InventoryCountTests.cs`
- [X] T026 [US2] Write SQL Server handler tests for Pending/Counted edits, actor/time persistence, count rowversion updates, negative/comment validation, stale line conflicts, and Counted deletion rejection in `Myrmex.Tests/Wms/Inventory/Features/InventoryCounts/InventoryCountLineHandlerTests.cs`
- [X] T027 [P] [US2] Extend endpoint tests for count-entry body/version/actor dispatch, success serialization, unauthenticated 401, and representative validation/conflict behavior in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryCountEndpointTests.cs`
- [X] T028 [P] [US2] Extend API-client tests for the count-entry route/body, version, cancellation, result mapping, and representative conflict in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 2

- [X] T029 [US2] Implement physical count entry, variance recalculation, counter audit, immutable snapshot preservation, and Draft-to-InProgress transition in `Myrmex.Modules.Wms/Inventory/Domain/InventoryCounts/InventoryCount.cs` and `Myrmex.Modules.Wms/Inventory/Domain/InventoryCounts/InventoryCountLine.cs`
- [X] T030 [US2] Implement rowversion-aware count-entry command and handler in `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/RecordInventoryCountLine.cs`
- [X] T031 [US2] Map the line count-entry route with server-derived actor identity in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryCountEndpoints.cs`
- [X] T032 [US2] Add count-entry API-client transport in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T033 [US2] Build the count-entry dialog with read-only SKU/location/system/base-UoM context, non-negative quantity, live variance, optional comment, and stale-result handling in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/RecordInventoryCountLineDialog.razor`
- [X] T034 [US2] Add Pending/Counted entry/edit actions and refresh the count status, audit, line version, and variance after success in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/InventoryCountDetailsPage.razor`

**Checkpoint**: Physical quantities are recorded as permanent counting evidence independently of inventory adjustment.

---

## Phase 5: User Story 3 - Apply a Count Result (Priority: P1)

**Goal**: Resolve zero variance without movement records, apply non-zero variance through one adjustment/ledger entry atomically, detect stale inventory as Conflict, and supersede conflicts with a fresh current line.

**Independent Test**: Apply one zero-variance line and one non-zero line, verify exact balance/transaction/ledger outcomes, then force a stale snapshot, confirm Conflict with no inventory effects, and supersede it with a fresh Pending replacement.

### Tests for User Story 3

- [X] T035 [P] [US3] Extend domain tests for zero/non-zero apply states, applier audit, Conflict immutability, Superseded final state, current replacement linkage, and duplicate supersession rejection in `Myrmex.Tests/Wms/Inventory/Domain/InventoryCountTests.cs`
- [X] T036 [US3] Write SQL Server apply tests for zero variance, positive/negative existing-balance variance, missing positive balance creation, exactly one Adjustment transaction/ledger entry, generated reason, actor/link persistence, one-save atomicity, and duplicate apply rejection in `Myrmex.Tests/Wms/Inventory/Features/InventoryCounts/ApplyInventoryCountLineHandlerTests.cs`
- [X] T037 [US3] Extend SQL Server apply tests for changed/disappeared/appeared balances, rowversion races, persisted Conflict without inventory effects, superseding fresh snapshots, filtered current-line uniqueness, and concurrent replacement conflicts in `Myrmex.Tests/Wms/Inventory/Features/InventoryCounts/ApplyInventoryCountLineHandlerTests.cs`
- [X] T038 [P] [US3] Extend persistence tests for unique applied-transaction ownership, unique supersession, current-line filtering, and restricted audit relationships in `Myrmex.Tests/Wms/Inventory/Persistence/InventoryCountPersistenceTests.cs`
- [X] T039 [P] [US3] Extend endpoint tests for apply/supersede route/body/actor dispatch, zero/non-zero success serialization, persisted-conflict 409, and unauthenticated behavior in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryCountEndpointTests.cs`
- [X] T040 [P] [US3] Extend API-client tests for apply/supersede routes, versions, cancellation, updated detail mapping, and representative conflict in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 3

- [X] T041 [US3] Implement Applied, Conflict, and Superseded transitions, applier audit, replacement creation/linking, and final-state invariants in `Myrmex.Modules.Wms/Inventory/Domain/InventoryCounts/InventoryCount.cs` and `Myrmex.Modules.Wms/Inventory/Domain/InventoryCounts/InventoryCountLine.cs`
- [X] T042 [US3] Implement apply handler snapshot comparison, zero-variance resolution, balance update/create, `InventoryTransaction.CreateAdjustment`, generated bounded reason, one-save persistence, conflict-state save, concurrency mapping, diagnostics, and cancellation in `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/ApplyInventoryCountLine.cs`
- [X] T043 [US3] Implement supersede handler that atomically marks Conflict as Superseded and adds one fresh Pending current line with a new balance snapshot in `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/SupersedeInventoryCountLine.cs`
- [X] T044 [US3] Map apply and supersede routes with server-derived actor identity in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryCountEndpoints.cs`
- [X] T045 [US3] Add apply and supersede API-client methods in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T046 [US3] Add Counted Apply, Conflict Supersede, zero/non-zero result messaging, replacement history, transaction links, stale lockout, and details refresh behavior in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/InventoryCountDetailsPage.razor`

**Checkpoint**: Count results safely reconcile inventory or produce recoverable immutable conflict evidence.

---

## Phase 6: User Story 4 - Resolve or Close a Count (Priority: P2)

**Goal**: Complete only fully Applied counts or cancel unfinished counts while preserving prior adjustments and actor audit.

**Independent Test**: Reject completion for empty/unresolved counts, complete an all-Applied count with actor/time and read-only state, then cancel another count and verify prior Applied inventory remains unchanged.

### Tests for User Story 4

- [X] T047 [P] [US4] Extend domain tests for non-empty/all-current-Applied completion, unresolved rejection, completion/cancellation actors and times, final-state immutability, and no adjustment reversal in `Myrmex.Tests/Wms/Inventory/Domain/InventoryCountTests.cs`
- [X] T048 [US4] Write SQL Server lifecycle handler tests for completion/cancellation versions, unresolved/current-line checks, preserved Applied balances/transactions, actor persistence, and final-state conflicts in `Myrmex.Tests/Wms/Inventory/Features/InventoryCounts/InventoryCountLifecycleHandlerTests.cs`
- [X] T049 [P] [US4] Extend endpoint tests for complete/cancel route/body/actor dispatch, success serialization, unauthenticated 401, and lifecycle conflicts in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryCountEndpointTests.cs`
- [X] T050 [P] [US4] Extend API-client tests for complete/cancel routes, versions, cancellation, final detail mapping, and representative conflict in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 4

- [X] T051 [US4] Implement completion eligibility, completion/cancellation actor audit, final-state immutability, and preserved Applied effects in `Myrmex.Modules.Wms/Inventory/Domain/InventoryCounts/InventoryCount.cs`
- [X] T052 [US4] Implement rowversion-aware complete and cancel handlers in `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/CompleteInventoryCount.cs` and `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/CancelInventoryCount.cs`
- [X] T053 [US4] Map complete and cancel routes with server-derived actor identity in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryCountEndpoints.cs`
- [X] T054 [US4] Add complete and cancel API-client methods in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T055 [US4] Add complete/cancel confirmations, Applied-adjustment warning, lifecycle action availability, final audit display, and read-only final states in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/InventoryCountDetailsPage.razor`

**Checkpoint**: Counts have reliable final states without losing audit history or reversing inventory corrections.

---

## Phase 7: User Story 5 - Review Count History (Priority: P2)

**Goal**: List counts with server-driven progress and open complete audit details, including Superseded history and inactive references.

**Independent Test**: Create counts in multiple states, filter/sort/page them, verify current-line progress totals, open details with all line/audit/transaction data, and confirm inactive referenced records remain understandable.

### Tests for User Story 5

- [X] T056 [US5] Write SQL Server query tests for warehouse/status/date filters, count-before-paging, supported/deterministic sorting, current-line progress, Superseded detail visibility, inactive-reference labels, versions, and cancellation in `Myrmex.Tests/Wms/Inventory/Features/InventoryCounts/InventoryCountQueryHandlerTests.cs`
- [X] T057 [P] [US5] Extend endpoint tests for list query binding, details routing, shared list/detail serialization, validation/not-found behavior, and cancellation dispatch in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryCountEndpointTests.cs`
- [X] T058 [P] [US5] Extend API-client tests for encoded list filters/sorts/dates, details URL, current DTO fixture deserialization, cancellation, and read-load errors in `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Implementation for User Story 5

- [X] T059 [US5] Implement count list filters, status parsing, count-before-paging, deterministic sorting, current-line progress projection, and `ListResult<InventoryCountListItem>` in `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/ListInventoryCounts.cs` and `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/InventoryCountQueryableExtensions.cs`
- [X] T060 [US5] Map count list and details GET routes in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryCountEndpoints.cs`
- [X] T061 [US5] Add count list URL construction and details load methods in `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- [X] T062 [P] [US5] Build count grid request mapping, MudDataGrid server loading, progress/status columns, deterministic sort tags, pager, and empty state in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/InventoryCountGridRequest.cs` and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/InventoryCountGrid.razor`
- [X] T063 [P] [US5] Build warehouse/status/date filters with page reset and reload behavior in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/InventoryCountFilters.razor`
- [X] T064 [US5] Build the Inventory Counts list page and code-behind with create/open/cancel orchestration, server-driven loading, errors, and refresh behavior in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/Index.razor` and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/Index.razor.cs`

**Checkpoint**: Operators can monitor active counts and audit all historical count evidence.

---

## Phase 8: Polish & Cross-Cutting Validation

**Purpose**: Verify diagnostics, security boundaries, generated schema, excluded scope, and end-to-end behavior.

- [ ] T065 Add structured diagnostics for actor, count, line, warehouse, SKU, location, action, outcome, conflict reason, and adjustment transaction in `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/`
- [ ] T066 Review endpoint and shared contracts to confirm actor IDs are server-derived only and no write request accepts actor identity in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryCountEndpoints.cs` and `Myrmex.Shared/Wms/Inventory/`
- [ ] T067 Developer-controlled: build the solution and run automated tests with a dedicated `_test` SQL Server connection, then record commands/results in `specs/079-inventory-counting-mvp/quickstart.md`
- [ ] T068 Developer-controlled: apply/review the inventory-count migration and execute authenticated API/UI smoke scenarios for create, add/remove, count, apply, conflict/supersede, complete/cancel, list, and details in `specs/079-inventory-counting-mvp/quickstart.md`
- [ ] T069 Review the final diff for one-save non-zero apply, persisted Conflict without inventory effects, filtered current-line uniqueness, audit identity, no client-supplied actor, and no freeze/reservation/approval/scanner/batch/external workflow; record the review in `specs/079-inventory-counting-mvp/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1** has no dependencies.
- **Phase 2** depends on Phase 1 and blocks all user-story endpoint/handler work.
- **US1 (Phase 3)** depends on Phases 1-2 and introduces the aggregate, schema, baseline details, and initial UI.
- **US2 (Phase 4)** depends on US1 because physical entry operates on persisted Pending lines.
- **US3 (Phase 5)** depends on US2 because apply requires Counted lines; supersede depends on Conflict produced by apply.
- **US4 (Phase 6)** depends on US3 because completion requires Applied current lines; cancellation can be developed after US1 but is grouped with final-state behavior.
- **US5 (Phase 7)** depends on US1 persistence/details, but its query and list UI can proceed in parallel with US2-US4 when shared projection and endpoint/client files are coordinated.
- **Polish (Phase 8)** depends on all selected stories and the developer-generated migration.

### User Story Dependency Graph

```text
Setup -> Foundation -> US1 -> US2 -> US3 -> US4
                         \---------------> US5
```

- US1 is the minimum demonstrable preparation workflow.
- US2 adds physical evidence without inventory mutation.
- US3 delivers the core reconciliation value and conflict recovery.
- US4 adds final lifecycle closure.
- US5 adds operational monitoring and audit history and can overlap with US2-US4 after US1.

### Within Each User Story

- Write the identified risk-based automated tests before the implementation they protect and confirm expected failures.
- Keep public request/response types in `Myrmex.Shared`; keep internal commands, queries, domain entities, EF mappings, and projections in `Myrmex.Modules.Wms`.
- Implement domain behavior before handlers, handlers before endpoints, endpoints before API-client/UI integration.
- Pass actor identity from server claims only; never accept it from public write contracts.
- Use explicit count/line rowversions and the persisted balance snapshot for concurrency.
- Use one relational save for successful non-zero apply effects.
- Builds, tests, startup, migration generation/application, database updates, and infrastructure commands remain developer-controlled.

### Parallel Opportunities

- T001-T003 can run in parallel.
- T006-T007 and T009-T010 can run in parallel after Foundation.
- T011 and the initial test drafts can proceed in parallel when contracts are stable.
- T022 and T023 can run in parallel after client operations exist.
- T025, T027, and T028 can run in parallel.
- T035, T038, T039, and T040 can run in parallel.
- T047, T049, and T050 can run in parallel.
- T057 and T058 can run in parallel; T062 and T063 can run in parallel.
- US5 query/list work can overlap with US2-US4 if edits to `InventoryCountQueryableExtensions.cs`, `InventoryCountEndpoints.cs`, and `WmsInventoryApiClient.cs` are sequenced.

---

## Parallel Example: User Story 1

```text
Task T006: Write aggregate lifecycle tests in Myrmex.Tests/Wms/Inventory/Domain/InventoryCountTests.cs
Task T007: Write persistence mapping/index tests in Myrmex.Tests/Wms/Inventory/Persistence/InventoryCountPersistenceTests.cs
Task T009: Write endpoint actor/routing tests in Myrmex.Tests/Wms/Inventory/Endpoints/InventoryCountEndpointTests.cs
Task T010: Write API-client create/add/remove tests in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs
```

## Parallel Example: User Story 3

```text
Task T035: Write apply/conflict/supersede domain tests in Myrmex.Tests/Wms/Inventory/Domain/InventoryCountTests.cs
Task T038: Write applied-transaction and supersession persistence tests in Myrmex.Tests/Wms/Inventory/Persistence/InventoryCountPersistenceTests.cs
Task T039: Write apply/supersede endpoint boundary tests in Myrmex.Tests/Wms/Inventory/Endpoints/InventoryCountEndpointTests.cs
Task T040: Write apply/supersede API-client tests in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs
```

## Parallel Example: User Story 5

```text
Task T057: Write count list/details endpoint boundary tests in Myrmex.Tests/Wms/Inventory/Endpoints/InventoryCountEndpointTests.cs
Task T058: Write count list/details API-client tests in Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs
Task T062: Build the server-driven count grid in Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/InventoryCountGrid.razor
Task T063: Build count filters in Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryCountPages/InventoryCountFilters.razor
```

---

## Implementation Strategy

### Suggested MVP Scope

1. Complete Setup and Foundation.
2. Complete US1 to create counts and prepare snapshot lines.
3. Complete US2 and US3 before operational release; these deliver physical counting, audited adjustment, and conflict recovery.
4. Validate US1-US3 independently before adding closure and history screens.

US1 alone is the smallest demonstrable increment, but US1-US3 together are the minimum operational Inventory Counting MVP because no inventory reconciliation occurs before US3.

### Incremental Delivery

1. Setup + Foundation → contracts, actor boundary, fixtures.
2. US1 → count preparation and snapshot evidence.
3. US2 → physical counting evidence and variance.
4. US3 → safe reconciliation and conflict recovery.
5. US4 → complete/cancel lifecycle.
6. US5 → server-driven monitoring and audit.
7. Polish → developer-controlled migration, build/test, and runtime validation evidence.

### Review Boundaries

- Review shared contracts separately from domain/persistence.
- Review aggregate tests and aggregate implementation together.
- Review migration/mapping/index changes as one schema unit.
- Review apply tests and handler atomically because they protect the highest-risk inventory behavior.
- Review endpoint/client boundaries separately from MudBlazor pages.
- Treat identity-provider setup, count-specific permissions, inventory freeze, or broader recount/approval behavior as separate scope.

## Notes

- `[P]` means different files and no dependency on an incomplete task.
- Tests identify concrete regression risks and use the lowest owning layer.
- Endpoint/client tests cover transport boundaries without duplicating the handler business matrix.
- UI component automation remains deferred; execute the manual scenarios in `quickstart.md`.
- Migration generation/application and runtime commands require explicit developer execution.
