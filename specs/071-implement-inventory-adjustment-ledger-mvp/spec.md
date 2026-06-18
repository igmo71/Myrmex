# Feature Specification: Inventory Adjustment Ledger MVP

**Feature Branch**: `071-implement-inventory-adjustment-ledger-mvp`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: `Implement Inventory Adjustment Ledger MVP using StakeholderDocs/Wms/Implement Inventory Adjustment Ledger MVP.md`

## Clarifications

### Session 2026-06-18

- Confirmed stakeholder decision: Inventory Adjustment is the only ledger-producing operation in this MVP.
- Confirmed stakeholder decision: Users submit an absolute physically counted quantity, not a signed delta.
- Confirmed stakeholder decision: A valid existing-balance no-op succeeds without creating a ledger transaction, ledger entry, balance update, timestamp update, or balance-version change.
- Confirmed stakeholder decision: Zero-quantity balances remain persisted; zero-row deletion is out of scope.
- Confirmed stakeholder decision: `ExpectedBalanceVersion` uses strict nullable existence semantics. `null` means the client expects no persisted balance row; non-null means the client expects an existing balance with exactly that version. `null` never means "skip concurrency validation."
- Confirmed stakeholder decision: All stale-state adjustment conflicts use `409 Conflict` with public error code `InventoryBalance.ConcurrencyConflict`.
- Confirmed stakeholder decision: The obsolete direct quantity-update endpoint, client method, and UI flow must be removed within this feature.
- Confirmed stakeholder decision: Direct initial balance creation must be replaced by the same inventory adjustment command using `ExpectedBalanceVersion = null`.
- Confirmed stakeholder decision: The public business-command endpoint is `POST /api/wms/inventory/adjustments`.
- Confirmed stakeholder decision: Stale-state adjustment conflicts use the capability-specific public conflict code `InventoryBalance.ConcurrencyConflict`; they must not rely on the current generic entity conflict code.
- Confirmed stakeholder decision: Missing-balance adjustments apply the full current create eligibility rules for SKU, base unit of measure, storage location, and related topology dependencies.
- Confirmed stakeholder decision: Existing-balance adjustments remain allowed even when referenced SKU, base unit of measure, storage location, storage-location type, or storage-location status later became inactive. Existing referenced stock must remain correctable while existence, identity, quantity, reason, and concurrency rules still apply.
- Confirmed stakeholder decision: Existing-balance adjustment submits the current Base64 rowversion as `ExpectedBalanceVersion`.
- Confirmed stakeholder decision: Missing-balance adjustment submits `ExpectedBalanceVersion = null`.
- Confirmed stakeholder decision: The existing grid-row adjustment and initial-count/missing-balance workflow both use the same adjustment command and endpoint.
- Confirmed stakeholder decision: When no balance exists, `CountedQuantity = 0`, and `ExpectedBalanceVersion = null`, the system creates the persisted zero `InventoryBalance`, returns success, and creates no `InventoryTransaction` or `InventoryLedgerEntry`.
- Repository-specific finding: The current direct quantity update flow changes `InventoryBalance.Quantity` by balance identifier without reason, rowversion, or ledger history. This flow must be removed before this feature can be considered complete.
- Repository-specific finding: Current balance details do not expose a balance version, so clients cannot yet submit `ExpectedBalanceVersion`.
- Repository-specific finding: Current conflict error conventions produce generic conflict codes for duplicate balance creation, while this feature requires the public code `InventoryBalance.ConcurrencyConflict` for stale-state adjustment conflicts.
- Repository-specific finding: Current quantity update behavior does not revalidate unchanged SKU and storage-location eligibility, while current create behavior does. This feature resolves that difference: existing-balance adjustment allows inactive referenced records but still requires those references to exist; missing-balance adjustment uses the full current create eligibility rules.
- Repository-specific finding: Current quantity update can update an existing balance directly, including zero quantity, and does not define no-op preservation of timestamp or version. This feature replaces that behavior with adjustment semantics.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Adjust an Existing Balance with Ledger History (Priority: P1)

A warehouse operator or administrator records the physically counted quantity for an existing SKU at an existing storage location so the current balance is corrected and an immutable adjustment record explains the before quantity, counted quantity, delta, reason, and time of the correction.

**Why this priority**: This is the core business value. The warehouse needs current stock to be correct without losing the audit trail of how and why it changed.

**Independent Test**: Can be fully tested by adjusting an existing balance from quantity `10` to counted quantity `7` with a reason and a matching current Base64 balance version, then confirming the current balance is `7` and exactly one immutable adjustment record stores before `10`, delta `-3`, after `7`, reason, and timestamp.

**Acceptance Scenarios**:

1. **Given** an existing balance for a SKU/location pair has quantity `10` and the client holds the current Base64 balance version, **When** a user submits counted quantity `7`, a required reason, and that expected version through the adjustment API, **Then** the system records an adjustment with balance before `10`, quantity delta `-3`, balance after `7`, updates the current balance to `7`, and returns the refreshed balance details with the new version.
2. **Given** an existing balance for a SKU/location pair has quantity `4.5` and the client holds the current Base64 balance version, **When** a user submits counted quantity `6.25` and a required reason through the adjustment API, **Then** the system records the positive delta `1.75`, updates the current balance to `6.25`, and preserves the SKU/location identity.
3. **Given** an existing balance references a SKU, base unit of measure, storage location, storage-location type, or storage-location status that later became inactive, **When** a user submits a valid adjustment with the current expected balance version, **Then** the system allows the correction because existing referenced stock must remain correctable.
4. **Given** a completed adjustment exists, **When** a user or process attempts to change or delete its transaction or ledger entry, **Then** the system prevents mutation; corrections require a new adjustment.

---

### User Story 2 - Initialize a Missing Balance from Expected Zero (Priority: P2)

A warehouse operator or administrator records a physical count for a SKU/location pair that has no current balance row, so Myrmex uses the same adjustment API to initialize the balance from an expected zero state.

**Why this priority**: Warehouse users must be able to correct missing current-state records without using a separate direct balance-create path that bypasses the adjustment command.

**Independent Test**: Can be fully tested by submitting an adjustment for a valid SKU/location pair with no existing balance and `ExpectedBalanceVersion = null`, then confirming a positive counted quantity creates a balance and ledger from zero while a zero counted quantity creates only the persisted zero balance.

**Acceptance Scenarios**:

1. **Given** no balance exists for a valid SKU/location pair that satisfies the full current create eligibility rules, **When** a user submits counted quantity `5`, a required reason, and `ExpectedBalanceVersion = null` through the adjustment API, **Then** the system treats the prior quantity as zero, creates the current balance with quantity `5`, records an adjustment with before `0`, delta `5`, and after `5`, and returns the created balance details with its version.
2. **Given** no balance exists for a valid SKU/location pair that satisfies the full current create eligibility rules, **When** a user submits counted quantity `0`, a required reason, and `ExpectedBalanceVersion = null` through the adjustment API, **Then** the system creates a persisted zero-quantity balance, returns success with balance details and version, and creates no inventory transaction or ledger entry.
3. **Given** no balance exists and the SKU, base unit of measure, storage location, storage-location type, or storage-location status is inactive or otherwise ineligible under the current create rules, **When** a user submits an adjustment with `ExpectedBalanceVersion = null`, **Then** the system rejects the initialization and no balance or ledger record is created.
4. **Given** another user creates the same SKU/location balance before the adjustment is saved, **When** the original adjustment attempts to save from expected absence, **Then** the system returns `409 InventoryBalance.ConcurrencyConflict` and no partial ledger or balance change remains.

---

### User Story 3 - Preserve Non-Ledger Successes Without Ledger Noise (Priority: P3)

A warehouse operator or administrator receives success when a requested count produces no logical quantity change, without creating misleading zero-delta ledger entries.

**Why this priority**: The ledger must remain a history of material quantity changes. Recording physical count confirmations without stock change belongs to a future inventory count workflow.

**Independent Test**: Can be fully tested by submitting counted quantity equal to the current balance quantity with a valid expected version, and by submitting counted quantity `0` for a missing balance with expected absence, then confirming both succeed without ledger records while only the missing-balance case creates a persisted balance row.

**Acceptance Scenarios**:

1. **Given** an existing balance has quantity `10` and the client holds the current Base64 balance version, **When** a user submits counted quantity `10` with a required reason and the matching expected version, **Then** the system returns success with current balance details, creates no adjustment transaction, creates no ledger entry, does not update the balance, and preserves the existing balance version.
2. **Given** no balance exists and the client expects no balance, **When** a user submits counted quantity `0` with a required reason, **Then** the missing-balance zero initialization creates the persisted balance row and returns success without creating an inventory transaction or ledger entry.

---

### User Story 4 - Reject Invalid Adjustments Clearly (Priority: P4)

A warehouse operator or administrator receives clear validation feedback when an adjustment request is incomplete, references invalid data, or uses invalid quantities or balance-version data.

**Why this priority**: Invalid corrections must not create ambiguous stock state or unauditable records.

**Independent Test**: Can be fully tested by submitting invalid adjustment requests and confirming the current balance and ledger remain unchanged while the user receives a clear validation or not-found message.

**Acceptance Scenarios**:

1. **Given** a user omits the reason or provides only whitespace, **When** the adjustment is submitted, **Then** the system rejects the adjustment and no balance or ledger record is changed.
2. **Given** a user submits a negative counted quantity, **When** the adjustment is submitted, **Then** the system rejects the adjustment and no balance or ledger record is changed.
3. **Given** no balance exists and the requested SKU, base unit of measure, storage location, storage-location type, or storage-location status fails the full current create eligibility rules, **When** the adjustment is submitted with `ExpectedBalanceVersion = null`, **Then** the system rejects the adjustment using the existing Myrmex error style and no balance or ledger record is changed.
4. **Given** an existing balance references records that still exist but later became inactive, **When** the adjustment is submitted with a matching expected version, non-negative counted quantity, and required reason, **Then** the system does not reject the correction solely because those existing references are inactive.
5. **Given** an existing balance references a SKU, storage location, or related required reference that no longer exists, **When** the adjustment is submitted, **Then** the system rejects the adjustment and no balance or ledger record is changed.
6. **Given** `ExpectedBalanceVersion` is non-null but is not valid Base64, **When** the adjustment is submitted, **Then** the system returns a validation error and no balance or ledger record is changed.

---

### User Story 5 - Protect Against Stale Client State (Priority: P5)

A warehouse operator or administrator is prevented from applying an absolute physical count based on stale balance state and is instructed to refresh before deciding whether the counted quantity is still valid.

**Why this priority**: Absolute adjustments cannot be retried automatically without risking an incorrect stock correction.

**Independent Test**: Can be fully tested by submitting each expected-version state mismatch and confirming every stale-state case returns `409 InventoryBalance.ConcurrencyConflict` with no partial ledger or balance changes.

**Acceptance Scenarios**:

1. **Given** a balance exists and the submitted expected version differs from the current balance version, **When** the adjustment is submitted, **Then** the system returns `409 InventoryBalance.ConcurrencyConflict` and no balance or ledger record is changed.
2. **Given** a balance exists but the submitted expected version is `null`, **When** the adjustment is submitted, **Then** the system returns `409 InventoryBalance.ConcurrencyConflict` because the client expected absence.
3. **Given** no balance exists but the submitted expected version is non-null, **When** the adjustment is submitted, **Then** the system returns `409 InventoryBalance.ConcurrencyConflict` because the client expected an existing balance.
4. **Given** the balance changes or disappears after the system checks it but before the adjustment is saved, **When** the adjustment save completes, **Then** the system returns `409 InventoryBalance.ConcurrencyConflict` and no partial ledger or balance change remains.
5. **Given** the UI receives `409 InventoryBalance.ConcurrencyConflict`, **When** the error is shown to the user, **Then** the user is told to refresh and review the counted quantity before retrying.

### Edge Cases

- `CountedQuantity = 0` is valid and must preserve persisted zero-balance rows.
- `CountedQuantity < 0` is invalid.
- `Reason` must be required after trimming; whitespace-only reasons are invalid.
- `ExpectedBalanceVersion = null` means expected absence only.
- `ExpectedBalanceVersion != null` means expected existing balance with exactly that version only.
- Existing-balance adjustments use the current Base64 rowversion as `ExpectedBalanceVersion`.
- Missing-balance initializations use `ExpectedBalanceVersion = null`.
- Invalid Base64 expected-version values are validation failures, not unhandled server failures.
- Explicit version mismatch, expected absence but balance exists, expected balance but balance is absent, save-time concurrency failure, and concurrent duplicate missing-balance creation all map to `409 InventoryBalance.ConcurrencyConflict`.
- A no-op on an existing balance must not change the balance timestamp or version.
- A no-op on an existing balance and a missing-balance zero initialization are both non-ledger-producing successes; only the missing-balance zero initialization creates a persisted balance row.
- Zero-delta ledger entries are not created.
- Ledger records are immutable; incorrect adjustments are corrected by a new adjustment.
- The current balance and ledger must never diverge because of partial persistence.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST introduce an immutable Inventory Adjustment ledger as the durable history for inventory corrections.
- **FR-002**: System MUST keep `InventoryBalance` as the current materialized quantity snapshot for one SKU at one storage location.
- **FR-003**: System MUST support Inventory Adjustment as the only ledger-producing operation in this MVP.
- **FR-004**: Users MUST be able to adjust inventory by `StockKeepingUnitId` and `StorageLocationId`; the adjustment operation MUST NOT require `InventoryBalanceId`.
- **FR-005**: Users MUST submit an absolute `CountedQuantity`; users MUST NOT submit the signed quantity delta.
- **FR-006**: System MUST calculate `QuantityDelta = CountedQuantity - CurrentQuantity`.
- **FR-007**: A successful material adjustment MUST record the quantity before adjustment, calculated delta, quantity after adjustment, required reason, and adjustment time.
- **FR-008**: A successful material adjustment MUST update or create the current balance and record the corresponding ledger transaction and ledger entry as one atomic outcome.
- **FR-009**: System MUST reject negative counted quantities.
- **FR-010**: System MUST require a non-empty trimmed adjustment reason.
- **FR-011**: System MUST preserve the existing SKU/location uniqueness rule for balances.
- **FR-012**: System MUST allow adjustment of an existing balance when the submitted expected version exactly matches the current balance version.
- **FR-013**: System MUST allow creation of a missing balance from expected zero only when no balance exists and `ExpectedBalanceVersion` is null.
- **FR-014**: System MUST persist zero-quantity balances for this MVP, including a missing-balance initialization where counted quantity is zero.
- **FR-015**: System MUST return current inventory balance details after a successful adjustment or initialization, including the current balance version.
- **FR-016**: System MUST return the unchanged current balance details and unchanged version after a valid no-op on an existing balance.
- **FR-017**: System MUST NOT create an inventory transaction or ledger entry for a valid no-op on an existing balance.
- **FR-018**: System MUST NOT create an inventory transaction or ledger entry when a missing-balance initialization has counted quantity `0`; it MUST create the persisted zero balance and return success.
- **FR-019**: System MUST NOT automatically retry an absolute adjustment after a concurrency conflict.
- **FR-020**: System MUST remove the obsolete direct quantity-update endpoint, client method, and UI flow within this feature.
- **FR-021**: System MUST replace direct initial balance creation with the adjustment command using `ExpectedBalanceVersion = null`.
- **FR-022**: System MUST expose one public business-command endpoint for adjustments: `POST /api/wms/inventory/adjustments`.
- **FR-023**: System MUST use the same adjustment command and endpoint for existing grid-row adjustments and create/initial-count missing-balance workflows.
- **FR-024**: System MUST NOT preserve direct create or direct quantity-update paths as parallel stock-mutation mechanisms.
- **FR-025**: Missing-balance adjustment MUST apply the full current create eligibility rules for SKU, base unit of measure, storage location, and related topology dependencies.
- **FR-026**: Existing-balance adjustment MUST allow correction when referenced SKU, base unit of measure, storage location, storage-location type, or storage-location status later became inactive, provided the referenced records still exist and identity, quantity, reason, and concurrency rules pass.
- **FR-027**: System MUST keep ledger history UI, Inventory Transfer, Inventory Account, LPN or handling-unit behavior, automatic zero-row deletion, cycle count workflows, backdated adjustments, user identity integration, event sourcing, ledger rebuilding, and external accounting-style double-entry inventory out of scope.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: `InventoryBalance` represents the current known quantity for one `StockKeepingUnitId + StorageLocationId` pair and is not the source of historical truth.
- **DR-002**: `InventoryTransaction` represents one completed inventory operation. For this MVP, the only transaction type is `Adjustment`.
- **DR-003**: Each MVP adjustment transaction contains exactly one ledger entry.
- **DR-004**: `InventoryLedgerEntry` represents an immutable quantity change for one SKU/location pair.
- **DR-005**: Every ledger entry MUST satisfy `BalanceAfter = BalanceBefore + QuantityDelta`.
- **DR-006**: Ledger transactions and entries MUST NOT be edited or deleted after persistence.
- **DR-007**: Corrections to an incorrect adjustment MUST be represented by a new adjustment.
- **DR-008**: A no-op existing-balance adjustment is a successful command outcome but not a ledger-producing inventory transaction.
- **DR-009**: A missing-balance initialization with counted quantity `0` is a successful command outcome that creates the current-state balance row but is not a ledger-producing inventory transaction.
- **DR-010**: Missing-balance adjustments follow current create eligibility rules; existing-balance adjustments remain valid for existing referenced stock even if references later became inactive.
- **DR-011**: Storage-location inventory is the only stock-holding model implemented in this MVP.
- **DR-012**: Future extensibility toward inventory-account concepts MUST NOT introduce nullable future-account foreign keys or current MVP behavior for transfers, transit inventory, or LPNs.
- **DR-013**: Adjustment timestamps use current Myrmex UTC conventions. Backdated adjustments are not included.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: Public adjustment input MUST include SKU, storage location, counted quantity, reason, and nullable expected balance version.
- **CB-002**: The expected balance version MUST be transported as a Base64 string when present.
- **CB-003**: Public balance details MUST include the current Base64 balance version, derived from SQL Server rowversion concurrency protection, so a client can submit it with a later adjustment.
- **CB-004**: Existing-balance adjustment and missing-balance initialization MUST both use `POST /api/wms/inventory/adjustments`.
- **CB-005**: Existing grid-row adjustment MUST submit the current Base64 rowversion as `ExpectedBalanceVersion`.
- **CB-006**: Create/initial-count missing-balance workflow MUST submit `ExpectedBalanceVersion = null`.
- **CB-007**: `ExpectedBalanceVersion` semantics MUST follow this state matrix:

  | Current database state | ExpectedBalanceVersion | Required result |
  |------------------------|------------------------|-----------------|
  | Balance exists and version matches | non-null | Process adjustment |
  | Balance exists and version differs | non-null | Concurrency conflict |
  | Balance exists | null | Concurrency conflict |
  | Balance does not exist | null | Create from zero |
  | Balance does not exist | non-null | Concurrency conflict |

- **CB-008**: Invalid Base64 expected-version values MUST be validation failures.
- **CB-009**: All stale-state conflicts MUST return `409 Conflict` with public code `InventoryBalance.ConcurrencyConflict`.
- **CB-010**: Stale-state adjustment conflicts MUST use the capability-specific `InventoryBalance.ConcurrencyConflict` public code and MUST NOT rely on the current generic entity conflict code.
- **CB-011**: The current direct quantity-update workflow to remove is the balance-identifier update operation that accepts only a new quantity and updates the current snapshot without reason, expected version, or ledger outcome.
- **CB-012**: The existing WebApp quantity update experience MUST become an adjustment experience that shows read-only SKU and storage-location context, shows current quantity, accepts counted quantity, requires reason, submits the loaded balance version, handles concurrency conflict clearly, and refreshes server data after success.
- **CB-013**: The existing balance creation experience MUST become an expected-absence adjustment-from-zero experience for missing balances using the same adjustment API.
- **CB-014**: Read/list behavior for inventory balances remains server-driven and must continue to preserve existing filter, paging, sorting, cancellation, and error behavior unless a later plan documents a necessary change.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: System MUST show clear validation feedback for missing reason, negative counted quantity, invalid expected-version format, and invalid or ineligible SKU or storage-location references.
- **OE-002**: System MUST show clear concurrency feedback for `InventoryBalance.ConcurrencyConflict` and instruct the user to refresh and review the counted quantity before retrying.
- **OE-003**: System MUST distinguish validation, not-found, concurrency conflict, and unexpected persistence failures using existing Myrmex error conventions.
- **OE-004**: Operationally important concurrency conflicts and unexpected persistence failures MUST be diagnosable without logging sensitive or excessive payload data.

### Current Production Behavior to Remove or Replace

- The current backend mutation path that updates quantity directly by balance identifier must be removed within this feature.
- The current public update request that accepts only `Quantity` must be removed from stock-mutation usage.
- The current WebApp update dialog that edits only quantity must be replaced by an adjustment dialog requiring counted quantity, reason, and the current expected version.
- The current WebApp client method that sends the direct quantity update request must be removed.
- The current grid action labeled "Update quantity" must become an adjustment action using the adjustment API.
- The current create flow that creates an initial balance directly must be replaced by an initial-count workflow that uses the adjustment API with `ExpectedBalanceVersion = null`.
- The current `InventoryBalanceDetails` contract has no balance version for concurrency.
- The current balance persistence model has no rowversion concurrency token.

### Key Entities *(include if feature involves data)*

- **InventoryBalance**: Current materialized quantity for one SKU at one storage location. It remains the current-state snapshot and gains a client-visible version for optimistic concurrency.
- **InventoryTransaction**: Immutable record of one completed inventory operation. For this MVP, every recorded transaction is an adjustment.
- **InventoryLedgerEntry**: Immutable record of one quantity change within a transaction, including SKU, storage location, quantity delta, balance before, and balance after.
- **StockKeepingUnit**: Existing catalog item being adjusted. Full current create eligibility rules apply to missing-balance initialization; existing-balance adjustment requires the referenced SKU to exist but remains allowed if it later became inactive.
- **StorageLocation**: Existing topology location being adjusted. Full current create eligibility rules apply to missing-balance initialization; existing-balance adjustment requires the referenced location and related topology records to exist but remains allowed if they later became inactive.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete a valid existing-balance adjustment in under 1 minute when they start from a loaded balance row.
- **SC-002**: 100% of material accepted adjustments produce exactly one immutable transaction and one immutable ledger entry.
- **SC-003**: 100% of material accepted adjustments leave the current balance equal to the counted quantity.
- **SC-004**: 100% of ledger entries satisfy `BalanceAfter = BalanceBefore + QuantityDelta`.
- **SC-005**: 100% of valid existing-balance no-ops return success without creating ledger records or changing the balance version.
- **SC-006**: 100% of positive missing-balance adjustments with expected absence create a current balance and ledger from zero.
- **SC-007**: 100% of zero-count missing-balance initializations with expected absence create a persisted zero balance and no ledger records.
- **SC-008**: 100% of stale-state mismatch cases return `409 InventoryBalance.ConcurrencyConflict`.
- **SC-009**: 100% of invalid reason, negative counted quantity, and invalid expected-version format attempts are rejected without changing balance or ledger state.
- **SC-010**: Users can see a clear refresh-and-review message whenever a concurrency conflict occurs.
- **SC-011**: The delivered MVP exposes one adjustment stock-mutation API for existing-balance adjustment and missing-balance initialization, with no direct create or direct quantity-update parallel mutation path.
- **SC-012**: The delivered MVP exposes no ledger-history page, transfer workflow, inventory-account workflow, LPN behavior, or zero-balance deletion behavior.

## Assumptions

- Existing Myrmex authentication, authorization, validation, result, error, and diagnostics conventions apply.
- Existing Inventory Balance list behavior remains the source for viewing current balances.
- Existing Catalog and Topology records continue to supply SKU, base unit of measure, warehouse, and storage-location context.
- Existing SKU/location uniqueness remains the final protection against duplicate balance rows.
- A missing-balance adjustment from expected absence is the user workflow replacing direct initial balance creation.
- Existing-balance grid-row adjustment and create/initial-count missing-balance workflow use the same adjustment API.
- A single user-facing adjustment action is sufficient for this MVP; ledger history viewing belongs to a later feature.

## Decisions Required Before `/plan`

- None. The prior planning decisions are resolved by the 2026-06-18 stakeholder decisions recorded in Clarifications.
