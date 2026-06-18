# Tasks: Implement Inventory Adjustment Ledger MVP

**Input**: Design documents from `specs/071-implement-inventory-adjustment-ledger-mvp/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Testing**: Risk-based focused tests are required by the approved plan. Add tests at the lowest owning layer for domain invariants, handler behavior, endpoint/client contracts, persistence concurrency, duplicate insert classification, and UI interaction paths.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified independently after the foundational work is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on another pending task in the same phase
- **[Story]**: User story label, for example `[US1]`
- Include exact file paths in every task description

## Phase 1: Setup

**Purpose**: Establish the implementation surface and protect the approved planning decisions before source changes begin.

- [ ] T001 Review the approved feature specification in `specs/071-implement-inventory-adjustment-ledger-mvp/spec.md` and keep the one-command adjustment API, strict `ExpectedBalanceVersion`, and non-goals visible during implementation.
- [ ] T002 Review the implementation plan corrections in `specs/071-implement-inventory-adjustment-ledger-mvp/plan.md`, especially post-materialization Base64 conversion, eligibility error semantics, duplicate-insert classification, and timestamp guidance.
- [ ] T003 Review the current stakeholder source in `StakeholderDocs/Wms/Implement Inventory Adjustment Ledger MVP.md` before making source edits.
- [ ] T004 [P] Review existing Inventory Balance domain code in `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs` for current mutation methods and invariants to preserve or replace.
- [ ] T005 [P] Review existing Inventory Balance endpoint code in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceEndpoints.cs` to identify direct create and direct quantity-update route removal points.
- [ ] T006 [P] Review existing WebApp Inventory Balance UI code in `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/Index.razor.cs` and `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/InventoryBalanceGrid.razor` to identify the direct update and direct create flows that must move to the adjustment API.

---

## Phase 2: Foundational

**Purpose**: Add shared contracts, domain model, EF mapping, projections, and migration shape that all stories depend on.

**CRITICAL**: No user story can be complete until this phase is done.

- [ ] T007 Create the adjustment request contract in `Myrmex.Shared/Wms/Inventory/AdjustInventoryBalanceRequest.cs` with SKU, storage location, counted quantity, nullable Base64 `ExpectedBalanceVersion`, and trimmed reason inputs.
- [ ] T008 Modify `Myrmex.Shared/Wms/Inventory/InventoryBalanceDetails.cs` to expose the current balance version as Base64 transport data.
- [ ] T009 Remove obsolete direct create request contract from `Myrmex.Shared/Wms/Inventory/CreateInventoryBalanceRequest.cs`.
- [ ] T010 Remove obsolete direct quantity-update request contract from `Myrmex.Shared/Wms/Inventory/UpdateInventoryBalanceQuantityRequest.cs`.
- [ ] T011 Modify `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs` to add `RowVersion` as a `byte[]` concurrency token without changing public transport encoding into the domain model.
- [ ] T012 Create the transaction type enum in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransactionType.cs` with the adjustment type needed for the MVP.
- [ ] T013 Create immutable ledger entry entity skeleton and factory boundaries in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryLedgerEntry.cs`.
- [ ] T014 Create `InventoryTransaction` aggregate root skeleton and factory boundaries in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs`.
- [ ] T015 Update database table and index constants in `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs` for inventory transactions, ledger entries, reason length 500, and relevant index names.
- [ ] T016 Modify EF mapping in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryBalanceConfiguration.cs` to map `InventoryBalance.RowVersion` as SQL Server rowversion and preserve the existing SKU/location uniqueness constraint.
- [ ] T017 Create EF mapping for transactions in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs` with `OccurredAtUtc`, `CreatedAtUtc`, no normal `UpdatedAtUtc` lifecycle, reason max length 500, and explicit indexes for SKU, storage location, and occurred time.
- [ ] T018 Create EF mapping for ledger entries in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryLedgerEntryConfiguration.cs` with immutable child relationship to transaction and no duplicate explicit FK index when EF convention already creates `InventoryTransactionId` index.
- [ ] T019 Modify `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs` to include `DbSet` properties and model configuration for `InventoryTransaction` and `InventoryLedgerEntry`.
- [ ] T020 Modify `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContextSaveExtensions.cs` so immutable ledger entities keep `UpdatedAtUtc` null and are not touched by update timestamp behavior.
- [ ] T021 Modify `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsPersistenceExceptionMapper.cs` to expose a low-level SQL Server duplicate-insert predicate for error 2601 or 2627 and the named SKU/location unique index without globally mapping all duplicates to concurrency.
- [ ] T022 Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceQueryableExtensions.cs` so EF projection reads `RowVersion` as `byte[]`, preserves bounded server-side projection, avoids complete entity graph loading, and performs Base64 conversion only after materialization.
- [ ] T023 Create EF migration artifacts for the implementation in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/<timestamp>_AddInventoryAdjustmentLedger.cs` and update `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs` during implementation, without hand-authoring a schema that conflicts with EF mappings.

**Checkpoint**: Shared contracts, domain skeleton, persistence mapping, rowversion projection policy, and migration shape are ready for story implementation.

---

## Phase 3: User Story 1 - Adjust Existing Balance With Ledger History (Priority: P1)

**Goal**: A user adjusts an existing Inventory Balance by entering a counted quantity and reason, and the system records the absolute correction with an inventory transaction and immutable ledger entry.

**Independent Test**: Given an existing balance with quantity 10 and the current Base64 rowversion, posting counted quantity 14 with reason "Cycle count" succeeds, updates the balance to 14, returns a new Base64 version, creates one `InventoryTransaction`, and creates one `InventoryLedgerEntry` with delta +4.

- [ ] T024 [P] [US1] Create domain tests for transaction and ledger creation invariants in `Myrmex.Tests/Modules/Wms/Inventory/Domain/InventoryTransactions/InventoryTransactionTests.cs`.
- [ ] T025 [P] [US1] Create handler test for successful existing-balance material adjustment in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T026 [US1] Implement adjustment transaction factory behavior in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs` to compute before quantity, after quantity, delta quantity, operation time, created time, SKU, storage location, and trimmed reason.
- [ ] T027 [US1] Implement ledger entry factory behavior in `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryLedgerEntry.cs` as an immutable child owned by `InventoryTransaction`.
- [ ] T028 [US1] Modify `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs` to apply material counted-quantity adjustments to existing balances through domain behavior.
- [ ] T029 [US1] Create the adjustment feature handler in `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` for existing-balance successful adjustments with explicit expected-version comparison before mutation.
- [ ] T030 [US1] Create public adjustment endpoint mapping in `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/InventoryAdjustmentEndpoints.cs` for `POST /api/wms/inventory/adjustments`.
- [ ] T031 [US1] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryEndpoints.cs` to map `InventoryAdjustmentEndpoints`.
- [ ] T032 [US1] Modify `Myrmex.Client/Wms/WmsInventoryApiClient.cs` to add the adjustment API client method and response mapping.
- [ ] T033 [US1] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceEndpoints.cs` to remove the obsolete direct quantity-update route.
- [ ] T034 [US1] Remove obsolete direct quantity-update handler from `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/UpdateInventoryBalanceQuantity.cs`.
- [ ] T035 [US1] Modify `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/InventoryBalanceGrid.razor` to replace the direct quantity-edit action with an adjustment action for existing rows.
- [ ] T036 [US1] Create the existing-balance adjustment dialog in `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/AdjustInventoryBalanceDialog.razor`.
- [ ] T037 [US1] Modify `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/Index.razor.cs` to call the adjustment client method with the row Base64 balance version and refresh the grid on success.
- [ ] T038 [US1] Remove obsolete direct quantity-update dialog from `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/UpdateInventoryBalanceQuantityDialog.razor`.
- [ ] T039 [US1] Add endpoint or API client contract tests for the successful existing-balance adjustment path in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/InventoryAdjustmentEndpointTests.cs`.

**Checkpoint**: Existing balances can be adjusted through the new public adjustment endpoint, and the old direct quantity-update path is removed.

---

## Phase 4: User Story 2 - Initialize Missing Balance From Expected Zero (Priority: P2)

**Goal**: A user can initialize stock for a SKU/location pair that has no balance by using the same adjustment command with `ExpectedBalanceVersion = null`.

**Independent Test**: Given no balance for an eligible SKU/location pair, posting counted quantity 7 with `ExpectedBalanceVersion = null` succeeds, creates the balance at 7, creates one transaction, and creates one ledger entry with delta +7.

- [ ] T040 [P] [US2] Add handler tests for missing-balance positive initialization in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T041 [P] [US2] Add handler tests for missing-balance zero initialization in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T042 [P] [US2] Add handler tests for missing-balance eligibility reuse in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T043 [US2] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to branch missing-balance requests only when `ExpectedBalanceVersion` is null.
- [ ] T044 [US2] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to reuse the current create eligibility rules for SKU, base UoM, storage location, and topology dependencies during missing-balance initialization.
- [ ] T045 [US2] Modify `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs` to support creation from an eligible missing-balance adjustment without exposing a separate public direct-create workflow.
- [ ] T046 [US2] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` so missing positive counted quantity creates the balance, transaction, and ledger entry in one persistence boundary.
- [ ] T047 [US2] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` so missing zero counted quantity creates a persisted zero balance, returns success, and creates no transaction or ledger entry.
- [ ] T048 [US2] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceEndpoints.cs` to remove the obsolete direct create route.
- [ ] T049 [US2] Remove obsolete direct create handler from `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/CreateInventoryBalance.cs`.
- [ ] T050 [US2] Modify `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/CreateInventoryBalanceDialog.razor` to become the initial-count workflow that submits the same adjustment request with `ExpectedBalanceVersion = null`, or remove it if replaced by `AdjustInventoryBalanceDialog.razor`.
- [ ] T051 [US2] Modify `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/Index.razor.cs` so the create/initial-count entry point calls the same adjustment API used by existing-row adjustment.
- [ ] T052 [US2] Modify `Myrmex.Client/Wms/WmsInventoryApiClient.cs` to remove obsolete direct create client method usage and route all balance mutation through the adjustment method.

**Checkpoint**: Missing balances are initialized only through the adjustment API, including the persisted-zero non-ledger case.

---

## Phase 5: User Story 3 - Preserve Non-Ledger Successes Without Ledger Noise (Priority: P3)

**Goal**: No-op existing balance adjustments and missing-zero initializations return success without creating ledger noise, while only missing-zero creates a persisted zero balance row.

**Independent Test**: Given an existing balance at quantity 10, posting counted quantity 10 with the current Base64 rowversion returns success and does not change quantity, timestamp, rowversion, transaction count, or ledger entry count.

- [ ] T053 [P] [US3] Add handler test for existing-balance no-op adjustment in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T054 [P] [US3] Add persistence-focused assertion for unchanged existing-balance rowversion on no-op in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T055 [US3] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to detect existing-balance no-op before domain mutation and persistence timestamp changes.
- [ ] T056 [US3] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` so existing no-op does not call balance mutation, does not add `InventoryTransaction`, and returns current balance details.
- [ ] T057 [US3] Modify `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/AdjustInventoryBalanceDialog.razor` to show successful completion for both no-op existing adjustment and missing-zero initialization without implying ledger history was created.
- [ ] T058 [US3] Add UI behavior test coverage for no-op success handling in `Myrmex.Tests/WebApp/Wms/InventoryBalances/InventoryBalanceManagementTests.cs`.

**Checkpoint**: Non-ledger-producing successes are explicit and do not create transaction or ledger records accidentally.

---

## Phase 6: User Story 4 - Reject Invalid Adjustments Clearly (Priority: P4)

**Goal**: Invalid adjustment requests fail with existing Myrmex result/error conventions and do not mutate stock.

**Independent Test**: Requests with missing identifiers, negative quantity, empty reason, reason longer than 500, invalid Base64 version, missing references, or missing-balance ineligible references return the documented validation/not-found/eligibility result and leave balances and ledger unchanged.

- [ ] T059 [P] [US4] Add handler validation tests for missing identifiers, negative quantity, empty reason, long reason, and invalid Base64 in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T060 [P] [US4] Add handler tests for missing SKU, missing storage location, and missing related required record results in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T061 [P] [US4] Add handler tests for inactive or otherwise ineligible references during missing-balance initialization in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T062 [P] [US4] Add handler test showing existing-balance adjustment is allowed when referenced SKU, base UoM, storage location, type, or status later became inactive but still exists in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T063 [US4] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to trim reason, require non-empty reason, enforce max length 500, persist the trimmed value, and return existing validation error conventions.
- [ ] T064 [US4] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to preserve existing not-found semantics for missing SKU, storage location, and required related records.
- [ ] T065 [US4] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to use current create-handler validation or conflict convention for existing but inactive or otherwise ineligible references during missing-balance initialization.
- [ ] T066 [US4] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to skip active-status eligibility checks for existing balances while still enforcing existence, identity, quantity, reason, and concurrency rules.
- [ ] T067 [US4] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/InventoryAdjustmentEndpoints.cs` to map validation, not-found, and eligibility failures to the same HTTP conventions documented in `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-api-contract.md`.
- [ ] T068 [US4] Modify `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/AdjustInventoryBalanceDialog.razor` to display validation, not-found, and eligibility failures without switching to obsolete direct create or update flows.

**Checkpoint**: Invalid requests are rejected predictably and do not create balances, transactions, or ledger entries.

---

## Phase 7: User Story 5 - Protect Against Stale Client State (Priority: P5)

**Goal**: Concurrent stock changes or stale client versions return `409 InventoryBalance.ConcurrencyConflict` without automatic retry.

**Independent Test**: Given an existing balance with current version B, posting an adjustment with stale version A returns `409 InventoryBalance.ConcurrencyConflict`, creates no transaction or ledger entry, does not retry, and leaves the balance unchanged by that request.

- [ ] T069 [P] [US5] Add handler test for existing-balance stale Base64 rowversion mismatch returning `InventoryBalance.ConcurrencyConflict` in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T070 [P] [US5] Add handler test for existing-balance request with `ExpectedBalanceVersion = null` returning `InventoryBalance.ConcurrencyConflict` in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T071 [P] [US5] Add handler test for missing-balance request with non-null `ExpectedBalanceVersion` returning `InventoryBalance.ConcurrencyConflict` in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T072 [P] [US5] Add persistence test for `DbUpdateConcurrencyException` translation to `InventoryBalance.ConcurrencyConflict` in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T073 [P] [US5] Add persistence test for concurrent duplicate insert of the SKU/location pair translating to `InventoryBalance.ConcurrencyConflict` only in the adjustment slice in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalanceHandlerTests.cs`.
- [ ] T074 [US5] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to enforce strict nullable `ExpectedBalanceVersion` semantics before mutation.
- [ ] T075 [US5] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to translate explicit version mismatch and expected-existence mismatch to `InventoryBalance.ConcurrencyConflict`.
- [ ] T076 [US5] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to catch `DbUpdateConcurrencyException`, return `InventoryBalance.ConcurrencyConflict`, and avoid retrying `SaveChangesAsync` or reusing the failed tracked graph.
- [ ] T077 [US5] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` or an adjustment-specific persistence helper to classify SQL Server duplicate insert of the named SKU/location unique index as `InventoryBalance.ConcurrencyConflict` for this adjustment command only.
- [ ] T078 [US5] Modify `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/InventoryAdjustmentEndpoints.cs` to map `InventoryBalance.ConcurrencyConflict` to HTTP 409.
- [ ] T079 [US5] Modify `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/Index.razor.cs` to refresh inventory balance data after a 409 conflict and keep the user on the adjustment workflow with current data visible.
- [ ] T080 [US5] Modify `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/AdjustInventoryBalanceDialog.razor` to surface concurrency conflicts as stale state instead of validation failure.

**Checkpoint**: Existing and missing balance concurrency conflicts use the capability-specific public conflict code with no automatic absolute-adjustment retry.

---

## Phase 8: Polish and Cross-Cutting

**Purpose**: Remove obsolete mutation surfaces, align documentation-facing contracts, and verify the feature coherently without changing scope.

- [ ] T081 Remove obsolete direct create client method from `Myrmex.Client/Wms/WmsInventoryApiClient.cs`.
- [ ] T082 Remove obsolete direct quantity-update client method from `Myrmex.Client/Wms/WmsInventoryApiClient.cs`.
- [ ] T083 Remove or update obsolete direct create handler tests in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryBalances/CreateInventoryBalanceHandlerTests.cs` so they no longer describe a public direct stock-mutation path.
- [ ] T084 Remove obsolete direct quantity-update handler tests from `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryBalances/UpdateInventoryBalanceQuantityHandlerTests.cs`.
- [ ] T085 Update Inventory Balance endpoint tests in `Myrmex.Tests/Modules/Wms/Inventory/Features/InventoryBalances/InventoryBalanceEndpointTests.cs` to assert removed direct create and quantity-update routes are no longer public mutation mechanisms.
- [ ] T086 Update test data builders or fixtures in `Myrmex.Tests/Modules/Wms/Inventory/InventoryTestData.cs` for rowversion, transaction, ledger, missing-balance initialization, and concurrency scenarios.
- [ ] T087 Review `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceQueryableExtensions.cs` to confirm no `Convert.ToBase64String(entity.RowVersion)` or transport encoding remains inside EF SQL projections.
- [ ] T088 Review `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs` to confirm a single `SaveChangesAsync` is used as the atomic persistence boundary unless a concrete repository reason requires an explicit transaction.
- [ ] T089 Review `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryLedgerEntryConfiguration.cs` and generated migration artifacts to confirm no duplicate explicit index is created for `InventoryTransactionId` if EF convention already creates the FK index.
- [ ] T090 Review `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs` and `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryLedgerEntry.cs` to confirm immutable lifecycle behavior is tested by observable behavior rather than reflection or member-absence checks.
- [ ] T091 Update `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/InventoryBalanceGrid.razor` and `Myrmex.WebApp/Components/Pages/Wms/InventoryBalances/Index.razor.cs` to ensure no UI path calls obsolete direct create or direct quantity-update stock mutation.
- [ ] T092 Verify `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-api-contract.md` still matches final public request, response, error code, and removed-route behavior after implementation.
- [ ] T093 Verify `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-ui-contract.md` still matches final existing-row adjustment and initial-count workflows after implementation.
- [ ] T094 Run developer-controlled verification from `specs/071-implement-inventory-adjustment-ledger-mvp/quickstart.md` only when explicitly allowed, using full Microsoft.Testing.Platform-compatible commands rather than unconfirmed VSTest `--filter` syntax.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup; blocks every user story.
- **US1 (Phase 3)**: Depends on Foundational; delivers the MVP path for existing-balance adjustment and direct quantity-update removal.
- **US2 (Phase 4)**: Depends on Foundational; can proceed after US1 endpoint/client shape is available.
- **US3 (Phase 5)**: Depends on US1 and US2 because it refines non-ledger success behavior for both paths.
- **US4 (Phase 6)**: Depends on Foundational; can run in parallel with US1/US2 implementation once the handler exists, but must finish before release.
- **US5 (Phase 7)**: Depends on Foundational and handler persistence shape; must finish before release.
- **Polish (Phase 8)**: Depends on all user stories.

### Story Dependencies

- **US1 (P1)**: First independently valuable slice. Existing balances can be adjusted through the new endpoint and obsolete direct quantity update is removed.
- **US2 (P2)**: Adds missing-balance initialization through the same endpoint and removes direct create as a stock mutation path.
- **US3 (P3)**: Adds explicit non-ledger success behavior for no-op existing adjustment and missing-zero initialization.
- **US4 (P4)**: Hardens validation, not-found, and eligibility result semantics.
- **US5 (P5)**: Hardens stale-state and persistence concurrency semantics.

### Parallel Opportunities

- T004, T005, and T006 can be reviewed in parallel.
- T024 and T025 can run in parallel before US1 implementation.
- T040, T041, and T042 can run in parallel before US2 implementation.
- T053 and T054 can run in parallel before US3 implementation.
- T059, T060, T061, and T062 can run in parallel before US4 implementation.
- T069, T070, T071, T072, and T073 can run in parallel before US5 implementation.
- UI edits and backend handler edits can run in parallel after contracts and endpoint shape are stable.

---

## Independent Test Criteria

- **US1**: Existing balance quantity changes from 10 to 14 with matching Base64 version; one transaction and one ledger entry are created with delta +4.
- **US2**: Missing eligible SKU/location with `ExpectedBalanceVersion = null` and counted quantity 7 creates balance, transaction, and ledger entry through `POST /api/wms/inventory/adjustments`.
- **US3**: Existing no-op returns success with unchanged quantity, timestamp, rowversion, and no ledger; missing zero creates a persisted zero balance and no ledger.
- **US4**: Invalid identifiers, quantity, reason, Base64, not-found references, and missing-balance eligibility failures return existing Myrmex result semantics and mutate nothing.
- **US5**: Stale rowversion, nullable version mismatch, `DbUpdateConcurrencyException`, and concurrent duplicate insert all return HTTP 409 with `InventoryBalance.ConcurrencyConflict` and no automatic retry.

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

## Developer-Controlled Commands

Do not run these during task generation. During implementation, run only when explicitly allowed by the developer:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
```

For targeted tests, use Visual Studio Test Explorer or the test-selection syntax confirmed for the installed Microsoft.Testing.Platform/xUnit version. Do not assume VSTest-style `--filter FullyQualifiedName...` reliably selects the intended subset in this repository.

