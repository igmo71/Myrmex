# Tasks: Implement Inventory Adjustment Ledger MVP

**Input**: Design documents from `specs/071-implement-inventory-adjustment-ledger-mvp/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Testing**: Risk-based focused tests are required. Prefer grouped behavioral tests and theories at the lowest owning layer. Do not create duplicate domain, handler, endpoint, client, and UI matrices for the same rule.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified independently after foundational work is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on another pending task in the same phase
- **[Story]**: User story label, for example `[US1]`
- Include exact file paths in every task description

---

## Phase 1: Setup

**Purpose**: Confirm the approved scope and actual repository paths before source changes begin.

- [X] T001 Review the approved specification in `specs/071-implement-inventory-adjustment-ledger-mvp/spec.md` for one-command adjustment API, strict nullable `ExpectedBalanceVersion`, concurrency code, and non-goals.
- [X] T002 Review the implementation plan in `specs/071-implement-inventory-adjustment-ledger-mvp/plan.md` for rowversion projection, eligibility semantics, duplicate-insert classification, timestamp model, and testing guidance.
- [X] T003 Review the stakeholder source in `StakeholderDocs/Wms/Implement Inventory Adjustment Ledger MVP.md` before making source edits.
- [X] T004 [P] Review current Inventory Balance domain behavior in `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs`.
- [X] T005 [P] Review current Inventory Balance endpoint mappings in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs` and `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs`.
- [X] T006 [P] Review current WebApp Inventory Balance files in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs` and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor`.

---

## Phase 2: Foundational

**Purpose**: Add shared contracts, domain model, EF mapping, projection structure, and persistence hooks that all stories depend on.

**CRITICAL**: No user story can be complete until this phase is done.

- [X] T007 Create adjustment request contract in `Myrmex.Shared/Wms/Inventory/AdjustInventoryBalanceRequest.cs` with SKU, storage location, counted quantity, nullable Base64 `ExpectedBalanceVersion`, and reason.
- [X] T008 Modify `Myrmex.Shared/Wms/Inventory/InventoryBalanceDetails.cs` to expose current balance version as Base64 transport data.
- [X] T009 Remove obsolete direct create request contract from `Myrmex.Shared/Wms/Inventory/CreateInventoryBalanceRequest.cs`.
- [X] T010 Remove obsolete direct quantity-update request contract from `Myrmex.Shared/Wms/Inventory/UpdateInventoryBalanceQuantityRequest.cs`.
- [X] T011 Modify `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs` to add `RowVersion` as a `byte[]` concurrency token without adding transport encoding to the domain model.
- [X] T012 Create transaction type enum in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransactionType.cs`.
- [X] T013 Create `InventoryTransaction` aggregate root in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs` with factory-based construction, `OccurredAtUtc`, `CreatedAtUtc`, no normal `UpdatedAtUtc` lifecycle, and immutable correction semantics.
- [X] T014 Create immutable child entity in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryLedgerEntry.cs` with factory-based construction and no independent business occurrence timestamp.
- [X] T015 Update table, column, length, and index constants in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`.
- [X] T016 Modify EF mapping in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryBalanceConfiguration.cs` to map `InventoryBalance.RowVersion` as SQL Server rowversion and preserve the SKU/location uniqueness constraint.
- [X] T017 Create transaction EF mapping in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs` for transaction table, transaction type, reason max length 500, `OccurredAtUtc`, creation timestamp behavior, and index on `OccurredAtUtc`.
- [X] T018 Create ledger entry EF mapping in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryLedgerEntryConfiguration.cs` for `InventoryTransactionId`, `StockKeepingUnitId`, `StorageLocationId`, decimal quantity fields, transaction relationship, SKU index, storage-location index, and EF convention-generated FK index without a duplicate explicit FK index.
- [X] T019 Modify `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs` to include `InventoryTransaction` and `InventoryLedgerEntry` sets and configurations.
- [X] T020 Inspect timestamp behavior in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContextSaveExtensions.cs`; modify it only if current automatic behavior would set or update timestamps on immutable ledger entities, and keep future `EntityBase` timestamp extraction out of scope.
- [X] T021 Modify `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsPersistenceExceptionMapper.cs` to expose only a low-level predicate for SQL Server duplicate errors 2601 or 2627 and the named SKU/location unique index, without globally classifying all duplicate balance inserts as concurrency.
- [X] T022 Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceQueryableExtensions.cs` to split database projection from transport mapping: preserve server-side filtering, sorting, paging, bounded column projection, and no full entity graph loading while projecting `RowVersion` as `byte[]`.
- [X] T023 Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceQueryableExtensions.cs` to perform in-memory mapping from the materialized internal projection to `InventoryBalanceDetails` with Base64 `BalanceVersion`, never inside the EF SQL projection.
- [X] T024 Complete EF model and mapping changes for expected migration shape in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs`, `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryLedgerEntryConfiguration.cs`, and `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryBalanceConfiguration.cs`; do not hand-author generated migration or snapshot files.
- [ ] T025 BLOCKED until explicit developer approval: generate and review migration artifacts in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/<timestamp>_AddInventoryAdjustmentLedger.cs` and `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs` using repository-approved EF commands only after approval.

**Checkpoint**: Contracts, domain model, persistence mapping, rowversion projection policy, and migration shape are ready for story implementation. Migration generation remains blocked until explicit approval.

---

## Phase 3: User Story 1 - Adjust Existing Balance With Ledger History (Priority: P1)

**Goal**: A user adjusts an existing Inventory Balance by entering a counted quantity and reason, and the system records the absolute correction with an inventory transaction and immutable ledger entry.

**Independent Test**: Existing balance quantity changes from 10 to 14 with matching Base64 version; one transaction and one ledger entry are created with delta +4, and the response includes the updated balance with a new Base64 version.

- [X] T026 [P] [US1] Create one domain test group for transaction and ledger entry factories, invariants, reason normalization, and observable lifecycle behavior in `Myrmex.Tests/Wms/Inventory/Domain/InventoryTransactionTests.cs`; do not add reflection tests, member-absence tests, setter-visibility tests, or architecture-shape tests for immutability.
- [X] T027 [P] [US1] Create one existing-balance material adjustment handler test in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T028 [US1] Implement adjustment transaction factory behavior in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs`.
- [X] T029 [US1] Implement ledger entry factory behavior in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryLedgerEntry.cs`.
- [X] T030 [US1] Modify `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs` to apply material counted-quantity adjustments to existing balances through domain behavior.
- [X] T031 [US1] Create adjustment handler in `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` for existing-balance successful adjustment with explicit expected-version comparison before mutation.
- [X] T032 [US1] Create public adjustment endpoint mapping in `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryAdjustmentEndpoints.cs` for `POST /api/wms/inventory/adjustments`.
- [X] T033 [US1] Modify `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs` to map `InventoryAdjustmentEndpoints`.
- [X] T034 [US1] Modify `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs` to add the adjustment API client method and response mapping.
- [X] T035 [US1] Modify `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs` to remove the obsolete direct quantity-update route mapping.
- [X] T036 [US1] Remove obsolete direct quantity-update handler from `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/UpdateInventoryBalanceQuantity.cs`.
- [X] T037 [US1] Modify `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor` to replace the direct quantity-edit action with an adjustment action for existing rows.
- [X] T038 [US1] Create existing-balance adjustment dialog in `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/AdjustInventoryBalanceDialog.razor`.
- [X] T039 [US1] Modify `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs` to call the adjustment client method with the row Base64 balance version and refresh the grid after success.
- [X] T040 [US1] Remove obsolete direct quantity-update dialog from `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/UpdateInventoryBalanceQuantityDialog.razor`.
- [X] T041 [US1] Add one focused adjustment endpoint/API-client contract test covering request, response, Base64 version transport, and success mapping in `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryAdjustmentEndpointTests.cs` and `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`.

**Checkpoint**: Existing balances can be adjusted through the new adjustment endpoint, and the old direct quantity-update path is removed.

---

## Phase 4: User Story 2 - Initialize Missing Balance From Expected Zero (Priority: P2)

**Goal**: A user initializes stock for a SKU/location pair that has no balance by using the same adjustment command with `ExpectedBalanceVersion = null`.

**Independent Test**: Missing eligible SKU/location with `ExpectedBalanceVersion = null` and counted quantity 7 creates balance, transaction, and ledger entry through `POST /api/wms/inventory/adjustments`.

- [X] T042 [P] [US2] Add one missing-positive initialization handler test in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T043 [P] [US2] Add one missing-zero initialization handler test in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T044 [US2] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to branch missing-balance requests only when `ExpectedBalanceVersion` is null.
- [X] T045 [US2] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to reuse current create eligibility behavior for SKU, base UoM, storage location, and topology dependencies during missing-balance initialization.
- [X] T046 [US2] Modify `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs` to support balance creation from an eligible missing-balance adjustment without exposing a separate public direct-create workflow.
- [X] T047 [US2] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` so missing positive counted quantity creates the balance, transaction, and ledger entry in one persistence boundary.
- [X] T048 [US2] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` so missing zero counted quantity creates a persisted zero balance, returns success, and creates no transaction or ledger entry.
- [X] T049 [US2] Modify `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs` to remove the obsolete direct create route mapping.
- [X] T050 [US2] Remove obsolete direct create handler from `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/CreateInventoryBalance.cs`.
- [X] T051 [US2] Convert or remove `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/CreateInventoryBalanceDialog.razor` so the initial-count workflow submits the same adjustment request with `ExpectedBalanceVersion = null`.
- [X] T052 [US2] Modify `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs` so the create/initial-count entry point calls the same adjustment API used by existing-row adjustment.
- [X] T053 [US2] Modify `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs` to remove obsolete direct create client method usage and route all balance mutation through the adjustment method.

**Checkpoint**: Missing balances are initialized only through the adjustment API, including the persisted-zero non-ledger case.

---

## Phase 5: User Story 3 - Preserve Non-Ledger Successes Without Ledger Noise (Priority: P3)

**Goal**: No-op existing balance adjustments and missing-zero initializations return success without ledger noise, while only missing-zero creates a persisted zero balance row.

**Independent Test**: Existing no-op returns success with unchanged quantity, timestamp, rowversion, transaction count, and ledger entry count; missing zero creates a persisted zero balance and no ledger.

- [X] T054 [P] [US3] Add one existing no-op handler test covering unchanged quantity, timestamp, rowversion, transaction count, and ledger entry count in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T055 [US3] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to detect existing-balance no-op before domain mutation and persistence timestamp changes.
- [X] T056 [US3] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` so existing no-op does not call balance mutation, does not add `InventoryTransaction`, and returns current balance details.
- [X] T057 [US3] Modify `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/AdjustInventoryBalanceDialog.razor` to show successful completion for both no-op existing adjustment and missing-zero initialization without implying ledger history was created.

**Checkpoint**: Non-ledger-producing successes are explicit and do not create transaction or ledger records accidentally.

---

## Phase 6: User Story 4 - Reject Invalid Adjustments Clearly (Priority: P4)

**Goal**: Invalid adjustment requests fail with existing Myrmex result/error conventions and do not mutate stock.

**Independent Test**: Invalid identifiers, quantity, reason, Base64, not-found references, and missing-balance eligibility failures return documented result semantics and mutate nothing.

- [X] T058 [P] [US4] Add one validation theory for missing identifiers, negative quantity, invalid reason, and invalid Base64 in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T059 [P] [US4] Add one missing-reference/not-found test group for missing SKU, missing storage location, and missing required related records in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T060 [P] [US4] Add one missing-balance eligibility theory or focused grouped test for inactive or otherwise ineligible references during missing-balance initialization in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T061 [P] [US4] Add one existing-inactive-reference correction test showing existing referenced stock remains correctable when related records still exist but later became inactive in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T062 [US4] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to trim reason, require non-empty reason, enforce max length 500, persist the trimmed value, and return existing validation error conventions.
- [X] T063 [US4] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to preserve existing not-found semantics for missing SKU, storage location, and required related records.
- [X] T064 [US4] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to use current create-handler validation or conflict convention for existing but inactive or otherwise ineligible references during missing-balance initialization.
- [X] T065 [US4] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to allow existing-balance adjustment when referenced SKU, base UoM, storage location, type, or status later became inactive but still exists.
- [X] T066 [US4] Modify `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryAdjustmentEndpoints.cs` to map validation, not-found, and eligibility failures to the HTTP conventions in `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-api-contract.md`.
- [X] T067 [US4] Modify `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/AdjustInventoryBalanceDialog.razor` to display validation, not-found, and eligibility failures without switching to obsolete direct create or update flows.

**Checkpoint**: Invalid requests are rejected predictably and do not create balances, transactions, or ledger entries.

---

## Phase 7: User Story 5 - Protect Against Stale Client State (Priority: P5)

**Goal**: Concurrent stock changes or stale client versions return `409 InventoryBalance.ConcurrencyConflict` without automatic retry.

**Independent Test**: Stale rowversion, expected absence when balance exists, expected existence when balance is missing, save-time rowversion conflict, and duplicate insert race all return HTTP 409 with `InventoryBalance.ConcurrencyConflict` and no automatic retry.

- [X] T068 [P] [US5] Add one strict expected-state concurrency theory covering stale version, expected absence but balance exists, and expected existence but balance is missing in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T069 [P] [US5] Add one provider-sensitive save-time rowversion concurrency test for `DbUpdateConcurrencyException` translation in `Myrmex.Tests/Wms/Inventory/Persistence/InventoryBalancePersistenceTests.cs`.
- [X] T070 [P] [US5] Add one adjustment-specific duplicate-index classification test showing SQL Server duplicate SKU/location insert maps to `InventoryBalance.ConcurrencyConflict` only in the adjustment slice in `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [X] T071 [US5] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to enforce strict nullable `ExpectedBalanceVersion` semantics before mutation.
- [X] T072 [US5] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to translate explicit version mismatch and expected-existence mismatch to `InventoryBalance.ConcurrencyConflict`.
- [X] T073 [US5] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to catch `DbUpdateConcurrencyException`, return `InventoryBalance.ConcurrencyConflict`, and avoid retrying `SaveChangesAsync` or reusing the failed tracked graph.
- [X] T074 [US5] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` or an adjustment-specific persistence helper to classify SQL Server duplicate insert of the named SKU/location unique index as `InventoryBalance.ConcurrencyConflict` for this adjustment command only.
- [X] T075 [US5] Modify `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryAdjustmentEndpoints.cs` to map `InventoryBalance.ConcurrencyConflict` to HTTP 409.
- [X] T076 [US5] Modify `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/AdjustInventoryBalanceDialog.razor` so `409 InventoryBalance.ConcurrencyConflict` keeps entered counted quantity and reason where practical, shows a refresh-and-review message, does not retry automatically, and requires the user to close or cancel the stale dialog.
- [X] T077 [US5] Modify `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs` so after a concurrency conflict the grid reloads current data and the user reopens adjustment from the refreshed row.

**Checkpoint**: Existing and missing balance concurrency conflicts use the capability-specific public conflict code with no automatic absolute-adjustment retry.

---

## Phase 8: Polish and Cross-Cutting

**Purpose**: Remove obsolete mutation surfaces, align contracts, and verify scope without adding broad test or framework work.

- [X] T078 Review `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs` to confirm obsolete direct create and direct quantity-update client methods are removed and only the adjustment stock mutation method remains.
- [X] T079 Review `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs` to confirm obsolete direct create and direct quantity-update mappings are removed without adding route-absence tests tied to old URLs.
- [X] T080 Review `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`, `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/CreateInventoryBalanceDialog.razor`, and `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/UpdateInventoryBalanceQuantityDialog.razor` to confirm no UI path calls obsolete direct create or direct quantity-update stock mutation.
- [X] T081 Remove or update obsolete direct create and quantity-update tests in `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/UpdateInventoryBalanceQuantityHandlerTests.cs` and any new or existing `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/CreateInventoryBalanceHandlerTests.cs` so they no longer describe public direct stock-mutation paths.
- [X] T082 Update test data builders or fixtures in `Myrmex.Tests/Wms/Inventory/Testing/InventoryBalanceTestData.cs` for rowversion, transaction, ledger, missing-balance initialization, and concurrency scenarios.
- [X] T083 Review `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceQueryableExtensions.cs` to confirm no `Convert.ToBase64String(entity.RowVersion)` or transport encoding remains inside EF SQL projections.
- [X] T084 Review `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to confirm one `SaveChangesAsync` is the atomic persistence boundary unless a concrete repository reason required an explicit transaction.
- [X] T085 Review `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryLedgerEntryConfiguration.cs` to confirm SKU and storage-location indexes are explicit and no duplicate explicit `InventoryTransactionId` FK index is added when EF convention already creates it.
- [X] T086 Review `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs` and `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryLedgerEntry.cs` to confirm immutability is enforced through factories, private/internal mutation, absence of update/delete flows, and correction through new transactions.
- [X] T087 Verify `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-api-contract.md` still matches final public request, response, error code, eligibility semantics, and removed-mutation-path behavior.
- [X] T088 Verify `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-ui-contract.md` still matches final existing-row adjustment, initial-count workflow, no-op messaging, and concurrency-conflict behavior.
- [ ] T089 Developer-controlled only: run verification from `specs/071-implement-inventory-adjustment-ledger-mvp/quickstart.md` only after explicit approval, using full Microsoft.Testing.Platform-compatible commands or confirmed targeted test syntax.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup; blocks every user story.
- **US1 (Phase 3)**: Depends on Foundational; delivers existing-balance adjustment and direct quantity-update removal.
- **US2 (Phase 4)**: Depends on Foundational and shared endpoint/client shape from US1; replaces direct create with missing-balance initialization.
- **US3 (Phase 5)**: Depends on US1 and US2 because it refines non-ledger success behavior for both paths.
- **US4 (Phase 6)**: Depends on Foundational and adjustment handler shape; can proceed alongside US1/US2 once the handler exists.
- **US5 (Phase 7)**: Depends on Foundational and adjustment handler persistence shape; must finish before release.
- **Polish (Phase 8)**: Depends on all user stories.

### Story Dependencies

- **US1 (P1)**: First independently valuable slice. Existing balances can be adjusted through the new endpoint and obsolete direct quantity update is removed.
- **US2 (P2)**: Adds missing-balance initialization through the same endpoint and removes direct create as a stock mutation path.
- **US3 (P3)**: Adds explicit non-ledger behavior for no-op existing adjustment and missing-zero initialization.
- **US4 (P4)**: Hardens validation, not-found, and eligibility result semantics.
- **US5 (P5)**: Hardens strict expected-state and save-time concurrency semantics.

### Parallel Opportunities

- T004, T005, and T006 can be reviewed in parallel.
- T026 and T027 can run in parallel after Foundational work.
- T042 and T043 can run in parallel before US2 implementation.
- T058, T059, T060, and T061 can run in parallel for US4 test coverage.
- T068, T069, and T070 can run in parallel for US5 concurrency coverage.
- UI tasks and backend handler tasks can run in parallel after shared contracts, endpoint shape, and client method signatures are stable.

---

## Risk-Based Test Groups

- Domain transaction and entry factory invariants, reason normalization, and observable lifecycle behavior.
- Existing-balance material adjustment.
- Missing-positive initialization.
- Missing-zero initialization.
- Existing no-op with unchanged timestamp/version and no ledger.
- Validation theory for identifiers, negative quantity, invalid reason, and invalid Base64.
- Missing-reference/not-found test group.
- Missing-balance eligibility theory or focused grouped test.
- Existing-inactive-reference correction.
- Strict expected-state concurrency theory for stale version, expected absence but balance exists, and expected existence but balance is missing.
- Provider-sensitive save-time rowversion concurrency.
- Adjustment-specific duplicate-index classification.
- Focused adjustment endpoint/API-client contract test.

---

## Independent Test Criteria

- **US1**: Existing balance quantity changes from 10 to 14 with matching Base64 version; one transaction and one ledger entry are created with delta +4.
- **US2**: Missing eligible SKU/location with `ExpectedBalanceVersion = null` and counted quantity 7 creates balance, transaction, and ledger entry through `POST /api/wms/inventory/adjustments`.
- **US3**: Existing no-op returns success with unchanged quantity, timestamp, rowversion, and no ledger; missing zero creates a persisted zero balance and no ledger.
- **US4**: Invalid identifiers, quantity, reason, Base64, not-found references, and missing-balance eligibility failures return existing Myrmex result semantics and mutate nothing.
- **US5**: Stale version, expected-state mismatch, `DbUpdateConcurrencyException`, and concurrent duplicate insert all return HTTP 409 with `InventoryBalance.ConcurrencyConflict` and no automatic retry.

---

## MVP Scope

Implement through **Phase 3 (US1)** first for the minimum independently valuable behavior:

1. Rowversion contract and persistence foundation.
2. Existing-balance adjustment command and endpoint.
3. Transaction and ledger creation for material corrections.
4. UI existing-row adjustment flow.
5. Removal of obsolete direct quantity-update mutation path.

US2 is required for the full feature because direct create must also be replaced by missing-balance initialization through the same adjustment command.

---

## Execution Restrictions

Codex may analyze and generate or edit code only.

Codex must not automatically run:

```text
dotnet build
dotnet test
dotnet run
dotnet ef
migration generation
database update
application startup
formatters
linters
infrastructure commands
```

Migration generation, build, tests, and runtime validation remain developer-controlled and require explicit approval.

For developer-approved verification, use `specs/071-implement-inventory-adjustment-ledger-mvp/quickstart.md`. Do not assume VSTest-style `--filter FullyQualifiedName...` reliably selects targeted tests under the repository's Microsoft.Testing.Platform configuration.
