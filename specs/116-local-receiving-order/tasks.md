# Tasks: Local Receiving Order MVP

**Input**: Design documents from `specs/116-local-receiving-order/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/receiving-orders-api-contract.md`, `contracts/receiving-orders-webapp-contract.md`, `quickstart.md`

**Tests**: No automated test tasks are included. The tracked solution has no test project, and Issue #116 forbids creating or restoring test infrastructure. Migration generation, database update, build, application execution, and acceptance execution remain user-owned as documented in `specs/116-local-receiving-order/quickstart.md`.

**Organization**: Tasks are grouped by user story so the core receipt workflow, Draft planning behavior, and work-discovery experience can be implemented and validated as explicit increments.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel once its stated phase prerequisites are complete because it changes different files and has no dependency on another incomplete task in the same parallel set.
- **[Story]**: Maps the task to User Story 1, 2, or 3 from `spec.md`.
- Every checklist task names the exact repository file or files it changes.

---

## Phase 1: Setup (Shared WMS Conventions and Seed Data)

**Purpose**: Establish the shared constants and persistence-boundary convention required by all Receiving stories.

- [ ] T001 [P] Add the Topology-owned `StorageLocationTypeCodes.Receiving` constant with persisted value `RECEIVING` in `Myrmex.Shared/Wms/Topology/StorageLocationTypeCodes.cs`
- [ ] T002 [P] Add the shared static `WmsQuantityPersistence` helper that determines whether a decimal numeric value is exactly representable as `decimal(18,4)`, accepting CLR decimals with scale above four when discarded digits are only trailing zeroes; use it for Issue #116 Receiving paths only and do not retrofit unrelated Adjustment, Transfer, Inventory Count, or other quantity workflows, in `Myrmex.Modules.Wms/Domain/WmsQuantityPersistence.cs`
- [ ] T003 Add the stable Receiving type seed ID and active system `HasData` row using `StorageLocationTypeCodes.Receiving` in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsSeedIds.cs` and `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StorageLocationTypeConfiguration.cs`
- [ ] T004 Add one demonstrable Receiving StorageLocation definition without reclassifying legacy DOCK identities in `Myrmex.Modules.Wms/DemoData/Features/DemoDataDefinitions.cs` and `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs`

**Checkpoint**: Shared Receiving type identity and quantity persistence rules are available. Migration generation remains a user-owned gate after persistence implementation.

---

## Phase 2: Foundational (Blocking Domain, Contracts, Eligibility, and Persistence)

**Purpose**: Build the aggregate boundary and shared infrastructure required before any user story endpoint or page can work.

**⚠️ CRITICAL**: No user story implementation begins until this phase is complete.

- [ ] T005 [P] Define only Draft, InProgress, and Completed domain states in `Myrmex.Modules.Wms/Receiving/Domain/ReceivingOrders/ReceivingOrderStatus.cs`
- [ ] T006 [P] Define stable validation/conflict/invalid-persisted-state errors and the existing-style Base64 eight-byte rowversion parser in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ReceivingOrderErrors.cs` and `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ReceivingOrderVersion.cs`
- [ ] T007 [P] Add list/status/sort shared contracts, including the established `ReceivingOrderStatusDetails` name and `TotalPlannedQuantity` sort, in `Myrmex.Shared/Wms/Receiving/ReceivingOrderListRequest.cs`, `Myrmex.Shared/Wms/Receiving/ReceivingOrderListItem.cs`, `Myrmex.Shared/Wms/Receiving/ReceivingOrderStatusDetails.cs`, and `Myrmex.Shared/Wms/Receiving/ReceivingOrderSortBy.cs`
- [ ] T008 [P] Add create, Draft-update, action, receive-line, and nullable line-identity request contracts in `Myrmex.Shared/Wms/Receiving/CreateReceivingOrderRequest.cs`, `Myrmex.Shared/Wms/Receiving/UpdateReceivingOrderDraftRequest.cs`, `Myrmex.Shared/Wms/Receiving/ReceiveReceivingOrderLineRequest.cs`, and `Myrmex.Shared/Wms/Receiving/ReceivingOrderActionRequest.cs`
- [ ] T009 [P] Add details and line response contracts with aggregate OrderVersion, quantities, timestamps, summaries, and direct InventoryTransactionId in `Myrmex.Shared/Wms/Receiving/ReceivingOrderDetails.cs` and `Myrmex.Shared/Wms/Receiving/ReceivingOrderLineDetails.cs`
- [ ] T010 [P] Extract one authoritative active location/type/status selectability predicate and make the existing lookup reuse it in `Myrmex.Modules.Wms/Topology/Features/StorageLocations/StorageLocationEligibility.cs` and `Myrmex.Modules.Wms/Topology/Features/StorageLocations/LookupStorageLocations.cs`
- [ ] T011 Refactor balance-creation validation to delegate overlapping location/type/status checks to the Topology eligibility rule while retaining SKU/base-UOM validation in `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/InventoryBalanceCreateEligibility.cs`
- [ ] T012 [P] Implement the owned ReceivingOrderLine entity with base-unit planned/received invariants and aggregate-only concurrency in `Myrmex.Modules.Wms/Receiving/Domain/ReceivingOrders/ReceivingOrderLine.cs`
- [ ] T013 Implement ReceivingOrder creation, exact `decimal(18,4)` representability for new planned/accumulated quantities, full-plan LineId reconciliation, retained-line SKU changes, lifecycle invariants, aggregate Touch behavior, and complete persisted-Completed invariant in `Myrmex.Modules.Wms/Receiving/Domain/ReceivingOrders/ReceivingOrder.cs`
- [ ] T014 [P] Add Receiving table/index/constraint names and duplicate Number/order-SKU exception mappings in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs` and `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsPersistenceExceptionMapper.cs`
- [ ] T015 Configure restrictive relationships, aggregate rowversion, `decimal(18,4)` columns, unique normalized Number, unique order/SKU, and filtered unique InventoryTransactionId in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/ReceivingOrderConfiguration.cs` and `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/ReceivingOrderLineConfiguration.cs`
- [ ] T016 [P] Register ReceivingOrder and ReceivingOrderLine sets in the existing WMS context in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`
- [ ] T047 After T010 and T011, review every existing workflow directly affected by extracting/reusing `StorageLocationEligibility` and preserve its prior accepted/rejected location behavior and error semantics, removing duplicate active location/type/status checks only when behavior remains unchanged and never intentionally broadening/narrowing Adjustment, Count, Transfer, or other workflows for Issue #116, in `Myrmex.Modules.Wms/Topology/Features/StorageLocations/StorageLocationEligibility.cs`, `Myrmex.Modules.Wms/Topology/Features/StorageLocations/LookupStorageLocations.cs`, `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/InventoryBalanceCreateEligibility.cs`, `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs`, `Myrmex.Modules.Wms/Inventory/Features/InventoryCounts/AddInventoryCountLine.cs`, `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/CreateInventoryTransfer.cs`, and `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/MoveInventoryBalance.cs`

**Critical checkpoint**: Phase 2 is complete only after T047 verifies the T010/T011 shared eligibility refactor across every directly affected existing workflow. No US1, US2, or US3 implementation may begin until T047 is complete. The shared contracts, aggregate, authoritative location eligibility, and EF model are then ready for user-story slices. Do not generate or apply the migration as part of these tasks; hand that action to the user through `quickstart.md`.

---

## Phase 3: User Story 1 - Receive Goods Into Inventory (Priority: P1) 🎯 MVP

**Goal**: Create a Draft, start it, record received quantities without inventory effect, and complete it through one atomic Receiving inventory posting exposed through the WebApp.

**Independent Test**: Through the WebApp, create a multi-line order, start it, receive every line, and complete it; verify no inventory changes before completion, one atomic balance update set, one Receiving transaction with one positive entry per line, a direct transaction reference, and idempotent repeated/concurrent completion.

### Implementation for User Story 1

- [ ] T017 [P] [US1] Add the Receiving transaction type and `CreateReceiving(receivingLocationId, changes, reason, occurredAtUtc, out transaction)` factory with one location argument, per-line SKU/delta/before/after values, Receiving-scoped persistence-bound validation, and non-empty reason validation without knowledge of the Receiving Order reason format in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransactionType.cs` and `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs`
- [ ] T018 [P] [US1] Implement the shared Receiving eligibility orchestration for active Warehouse, ownership, `StorageLocationTypeCodes.Receiving`, authoritative selectability, and deterministic SKU/base-UOM reference order in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ReceivingOrderEligibility.cs`
- [ ] T019 [P] [US1] Implement no-tracking details projection and mapping with ordered lines, totals, aggregate version, and direct transaction reference in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ReceivingOrderQueryableExtensions.cs` and `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/GetReceivingOrderById.cs`
- [ ] T020 [P] [US1] Add the protected Receiving HTTP client and DI registration for details/create/start/receive/complete operations in `Myrmex.WebApp/Wms/Receiving/WmsReceivingApiClient.cs` and `Myrmex.WebApp/Program.cs`
- [ ] T021 [P] [US1] Implement the focused server-backed active-SKU selector with duplicate-current-line prevention in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/SelectReceivingOrderSkuDialog.razor`
- [ ] T022 [US1] Add invariant, English, and Russian strings needed for creation, execution, quantities, states, conflicts, and transaction display in `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`
- [ ] T023 [US1] Implement deterministic create validation, zero-received Draft persistence, 200-result details reload, and structured outcome logging in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/CreateReceivingOrder.cs`
- [ ] T024 [P] [US1] Implement versioned Start with authoritative eligibility revalidation, immutable plan transition, idempotent InProgress response, and structured outcome logging in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/StartReceivingOrder.cs`
- [ ] T025 [P] [US1] Implement aggregate-versioned positive line receipt, over-receipt protection, no inventory effect, decimal-bound validation, and structured outcome logging in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ReceiveReceivingOrderLine.cs`
- [ ] T026 [US1] Implement one-save atomic completion for balances/order/transaction/entries, direct InventoryTransactionId, Receiving-owned construction of the invariant non-localized reason `ReceivingOrder {ReceivingOrderId:D} Number {NormalizedNumber}`, Receiving-scoped decimal-bound checks, full persisted-Completed reload invariant, conflict observation without posting retry, and structured outcome logging in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/CompleteReceivingOrder.cs`
- [ ] T027 [US1] Map the authorized Receiving route group and details/create/start/receive/complete endpoints, and verify `ReceivingOrder.InvalidPersistedState` is returned as `ServiceErrorType.Failure` through the existing RFC Problem Details mapping to HTTP 500; if the shared path cannot represent that outcome, make only a narrow shared-mapper extension—never an unhandled exception, HTTP 409, or Receiving-specific envelope—in `Myrmex.Modules.Wms/Receiving/Endpoints/ReceivingEndpoints.cs`, `Myrmex.Modules.Wms/Receiving/Endpoints/ReceivingOrderEndpoints.cs`, `Myrmex.Modules.Wms/WmsModule.cs`, and `Myrmex.AspNetCore/Results/ServiceResultHttpExtensions.cs`
- [ ] T028 [US1] Implement the full-page create-mode complete-plan editor with Warehouse-dependent eligible Receiving lookup, shared type-code constant, complete backing collection, and create navigation in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDraftPage.razor` and `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDraftPage.razor.cs`
- [ ] T029 [P] [US1] Implement the positive receive-quantity dialog with planned/received/remaining context and aggregate OrderVersion in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceiveReceivingOrderLineDialog.razor`
- [ ] T030 [US1] Implement the details/execution page with state-gated Start/Receive/Complete actions, mutation refresh, planned/received/remaining quantities, full transaction link, and reload-on-true-execution-conflict behavior in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDetailsPage.razor`

**Checkpoint**: User Story 1 is a complete MVP workflow. User-owned acceptance follows Sections 5, 8, 10, and 12 of `quickstart.md` after the user generates/applies the migration and runs the application.

---

## Phase 4: User Story 2 - Revise the Planned Receipt (Priority: P2)

**Goal**: Replace a Draft header and complete line plan by LineId, preserve retained IDs even when SKU changes, and physically delete eligible Draft orders.

**Independent Test**: Revise a Draft by retaining/changing/removing/adding lines, verify stable retained IDs and all-or-nothing failure behavior, then delete a current Draft and reuse its Number; verify updates/deletion fail after Start and stale versions conflict.

### Implementation for User Story 2

- [ ] T031 [P] [US2] Implement versioned complete-plan Draft replacement with stable fail-fast validation, foreign/duplicate LineId rejection, retained-line SKU changes, omitted-line removal, new-line IDs, atomic save, and structured logging in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/UpdateReceivingOrderDraft.cs`
- [ ] T032 [P] [US2] Implement guarded physical Draft deletion with aggregate version, explicit atomic line/order removal, invalid-persisted-state defense, Number release, no Deleted lifecycle state, and structured logging in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/DeleteReceivingOrderDraft.cs`
- [ ] T033 [US2] Add PUT/DELETE endpoint mappings, URL-encoded Base64 version handling, 204 no-content client support, and Receiving client methods in `Myrmex.Modules.Wms/Receiving/Endpoints/ReceivingOrderEndpoints.cs`, `Myrmex.WebApp/Wms/Api/WmsApiClientHttp.cs`, and `Myrmex.WebApp/Wms/Receiving/WmsReceivingApiClient.cs`
- [ ] T034 [US2] Extend the Draft page to load edit-mode details and serialize full `UpdateReceivingOrderDraftRequest` reconciliation while preserving loaded LineIds through SKU/quantity edits in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDraftPage.razor` and `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDraftPage.razor.cs`
- [ ] T035 [US2] After HTTP 409 preserve the complete unsaved Draft plan, disable repeated Save, show explicit current-data conflict guidance, and provide a confirmed Reload latest/discard-local-changes action while keeping local state visible for optional manual resolution; do not add automatic/three-way merge, automatic replay, or generalized conflict resolution in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDraftPage.razor` and `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDraftPage.razor.cs`
- [ ] T036 [US2] Add Draft-only Edit/Delete actions, confirmation, successful navigation, and stale/non-Draft conflict guidance to `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDetailsPage.razor`
- [ ] T037 [US2] Add invariant, English, and Russian strings for Draft reconciliation, deletion, Number reuse, and unsaved-conflict guidance in `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`

**Checkpoint**: User Story 2 independently proves full-plan reconciliation and guarded Draft deletion using Sections 6, 7, and 9 of `quickstart.md` under user-owned execution.

---

## Phase 5: User Story 3 - Find and Execute Receiving Work (Priority: P3)

**Goal**: Search, filter, sort, page, open, and execute Receiving Orders through full pages that preserve a representative 300-line plan without line loss or splitting.

**Independent Test**: Populate multiple orders, verify deterministic list search/filter/sort/page results, open each lifecycle state, and execute the exactly-300-line acceptance procedure without filtered-out line loss, order splitting, performance thresholds, or direct API/database intervention.

### Implementation for User Story 3

- [ ] T038 [US3] Implement normalized list filters, supported deterministic sorts including TotalPlannedQuantity, ID tie-breakers, paging/count, and no-tracking aggregate projections in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ReceivingOrderQueryableExtensions.cs` and `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ListReceivingOrders.cs`
- [ ] T039 [US3] Add the list endpoint and Receiving client list method with existing ListResult and 200 semantics in `Myrmex.Modules.Wms/Receiving/Endpoints/ReceivingOrderEndpoints.cs` and `Myrmex.WebApp/Wms/Receiving/WmsReceivingApiClient.cs`
- [ ] T040 [P] [US3] Implement Warehouse/status/search/sort paging request state and filter controls in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderGridRequest.cs` and `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderFilters.razor`
- [ ] T041 [P] [US3] Implement the Receiving grid columns, totals, timestamps, and state-gated open/edit/delete actions in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderGrid.razor`
- [ ] T042 [US3] Implement the server-driven Receiving list page, paging lifecycle, filters, create/open/edit/delete coordination, and conflict reload behavior in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/Index.razor` and `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/Index.razor.cs`
- [ ] T043 [P] [US3] Add case-insensitive local SKU code/name search that filters only rendered Draft rows while every save submits the complete backing plan in `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDraftPage.razor` and `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDraftPage.razor.cs`
- [ ] T044 [P] [US3] Add the localized Receiving navigation link under WMS in `Myrmex.WebApp/Components/Layout/NavMenu.razor`
- [ ] T045 [US3] Review the implemented/generated list query shape and existing WMS conventions; either add a justified composite non-unique index in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/ReceivingOrderConfiguration.cs` or document in `specs/116-local-receiving-order/research.md` that no additional index is justified, without adding an index merely to complete the task
- [ ] T046 [US3] Add invariant, English, and Russian strings for navigation, list filters, columns, actions, empty states, and local line search in `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`

**Checkpoint**: User Story 3 is ready for the user-owned list/details checks and exactly-300-line deterministic procedure in Sections 11 and 13 of `quickstart.md`.

---

## Phase 6: Polish & Documentation Review

**Purpose**: Close accessibility, documentation consistency, and handoff gaps without expanding Issue #116.

- [ ] T048 Audit localized labels, disabled action states, icon tooltips, dialog focus, and keyboard behavior across `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/Index.razor`, `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDraftPage.razor`, `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceivingOrderDetailsPage.razor`, `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/SelectReceivingOrderSkuDialog.razor`, and `Myrmex.WebApp/Components/Pages/Wms/Receiving/ReceivingOrderPages/ReceiveReceivingOrderLineDialog.razor`
- [ ] T049 Reconcile final implementation-facing names, routes, error codes, user-owned migration/build/run/acceptance instructions, and excluded weight/1C scope with the approved plan without weakening specification behavior to match accidental implementation differences; do not modify `spec.md` unless a direct contradiction is discovered and explicitly reported, in `specs/116-local-receiving-order/quickstart.md`, `specs/116-local-receiving-order/contracts/receiving-orders-api-contract.md`, and `specs/116-local-receiving-order/contracts/receiving-orders-webapp-contract.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 — Setup**: Starts immediately. T003 depends on T001; T004 depends on T003.
- **Phase 2 — Foundational**: Depends on Phase 1. T011 depends on T010; T013 depends on T002, T005, and T012; T015/T016 depend on the aggregate model, with T015 also using T014 names. T047 depends on T010 and T011, is scheduled as the final Phase 2 review after T016, and blocks every user-story phase.
- **Phase 3 — User Story 1**: Depends on completed Phase 2, including T047. This is the MVP and establishes create/details/execution shells reused by later stories.
- **Phase 4 — User Story 2**: Depends on completed Phase 2, including T047, plus the US1 create/details/API-client/editor shells needed to obtain and revise a Draft.
- **Phase 5 — User Story 3**: Depends on completed Phase 2, including T047; its complete find-and-execute checkpoint also depends on the US1 execution page and US2 Draft editor behavior.
- **Phase 6 — Polish & Documentation**: Contains only T048 and T049 and depends on all selected user stories.

### User Story Dependencies

```text
Setup → Foundational T005–T016 → T047 eligibility regression gate
                                      ├──→ US1 (MVP)
                                      ├──→ US2
                                      └──→ US3
US1 + US2 + US3 → Polish & Documentation (T048–T049)
```

- **US1 (P1)**: First deliverable after the T047 foundational gate; no dependency on another user story.
- **US2 (P2)**: Begins only after the T047 foundational gate and uses US1 creation/details surfaces, while its reconciliation/deletion rules remain independently testable.
- **US3 (P3)**: Begins only after the T047 foundational gate; the full story integrates the US1 execution and US2 editor surfaces.

### Within-Phase and Story Dependencies

- **Foundational**: T011 follows T010. T047 depends on T010 and T011, runs as the final review after T016, and must complete before T017–T046 begin.
- **US1**: T023–T026 depend on T018 and the aggregate; T026 also depends on T017. T027 follows backend handlers. T028 depends on T020/T021. T029 depends on T020. T030 depends on T019, T020, T027, and T029.
- **US2**: T033 follows T031/T032. T034 follows T033. T035 follows T034. T036 follows T032/T033.
- **US3**: T039 follows T038. T042 follows T039–T041. T045 follows T038 so it evaluates the actual query shape.

### Parallel Opportunities

- T001 and T002 can run together.
- After Setup, T005–T010 and T012/T014 can be distributed by file; T011 waits for T010 and T013 waits for its domain prerequisites. T047 depends on T010/T011, is deliberately completed after T016 as the final foundational gate, and cannot run in parallel with downstream user-story implementation.
- Only after T047 completes, T017–T021 can run in parallel at US1 start. After T018, T024 and T025 can run together; after T020, T028 and T029 can proceed on separate page/dialog files.
- T031 and T032 can run in parallel for US2.
- For US3, T040, T041, T043, and T044 target separate concerns/files and can run in parallel subject to the listed prerequisites.
- T022, T037, and T046 all edit the same `SharedResource*.resx` files and must run serially in task-ID order; they are not parallel opportunities even if different user stories are staffed concurrently.

---

## Parallel Examples

### User Story 1

```text
Task T017: Inventory Receiving transaction factory
Task T018: Receiving eligibility orchestration
Task T019: Details projection/query
Task T020: WebApp Receiving API client registration
Task T021: Focused SKU selector
```

After T018:

```text
Task T024: Start handler
Task T025: Receive-line handler
```

### User Story 2

```text
Task T031: Complete-plan Draft update handler
Task T032: Guarded physical Draft delete handler
```

### User Story 3

```text
Task T040: Grid request and filter controls
Task T041: Receiving grid
Task T043: Draft local line search/backing-plan preservation
Task T044: Navigation
```

---

## Implementation Strategy

### MVP First — User Story 1

1. Complete Setup and foundational tasks T005–T016.
2. Complete T047 to finish Phase 2 as the blocking shared-eligibility regression gate; do not begin any user-story implementation before it passes review.
3. Complete US1 domain orchestration, endpoints, API client, create editor, and execution page.
4. Stop at the US1 checkpoint and hand migration generation/application, build, application execution, and acceptance execution to the user through `quickstart.md`.
5. Do not create test infrastructure, posting frameworks, source-document frameworks, location capabilities, weight fields, or 1C weight normalization.

### Incremental Delivery

1. **Foundation**: Shared constants, decimal boundary, aggregate, eligibility, contracts, and EF configuration, followed by the blocking T047 regression review of every directly affected existing location workflow.
2. **US1 MVP**: Draft → InProgress → Completed with one atomic inventory posting.
3. **US2**: Stable-identity Draft replacement and guarded physical Draft deletion.
4. **US3**: List/discovery plus representative 300-line WebApp behavior.
5. **Polish**: Accessibility, diagnostics/contract consistency, and user-owned validation handoff.

### Scope and Execution Notes

- The implementer changes production source and documentation only; no test project or test harness is created or restored.
- No task generates/applies a migration, updates a database, runs a build/application, or executes acceptance; those actions remain user-owned.
- Draft deletion is physical persistence removal, never a `Deleted` lifecycle state.
- Completion is attempted once per request and is never automatically retried.
- `ReceivingOrder.InventoryTransactionId` remains the authoritative direct link; no generic source-document ownership is introduced.
- `WmsQuantityPersistence` is applied in Issue #116 only to new Receiving inputs/calculations, `InventoryTransaction.CreateReceiving`, and InventoryBalance calculations directly used by Receiving completion; do not retrofit Adjustment, Transfer, Inventory Count, or other existing quantity workflows.
- Exact `decimal(18,4)` representability is based on numeric value, so a CLR decimal with scale above four remains valid when only trailing zeroes would be removed.
- Inventory validates only that `CreateReceiving` receives a non-empty reason; `CompleteReceivingOrder` owns the stable Receiving Order reason format.
- Invalid persisted state must use the shared RFC Problem Details failure path to HTTP 500, never an unhandled exception, 409, or Receiving-specific error envelope.
- A representative 300-line dataset is functional acceptance evidence, not a maximum or performance SLA.
- SKU weight support and 1C weight normalization remain outside Issue #116.
