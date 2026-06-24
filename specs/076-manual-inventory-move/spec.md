# Feature Specification: Manual Inventory Move

**Feature Branch**: `077-implement-manual-inventory-move`

**Created**: 2026-06-24

**Status**: Draft

**Input**: User description: "StakeholderDocs\Wms\Inventory\077 Implement Manual Inventory Move.md"

## Clarifications

### Session 2026-06-24

- Q: When the entire source quantity is moved, what happens to the source balance? → A: Retain the source balance with quantity zero.
- Q: If two valid moves from different source balances target the same destination balance concurrently, what should users observe? → A: One succeeds; the other returns a conflict and must be retried.
- Q: Should an existing balance for an inactive SKU be eligible for manual movement? → A: Reject movement unless the SKU is active.
- Q: If a balance exists but its SKU or storage location is inactive, what should the lookup return? → A: Return balance details; move submission validates operational eligibility.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Move Inventory from a Balance Row (Priority: P1)

As a warehouse operator, I want to move a quantity of one SKU from an existing inventory balance to another regular storage location in the same warehouse so that the system records an ad-hoc physical relocation without requiring a planned transfer document.

**Why this priority**: This is the primary operational workflow and directly keeps system inventory aligned with a physical move already chosen by the operator.

**Independent Test**: Can be tested by opening the move action for an existing balance, selecting an eligible destination, entering quantity and reason, confirming the move, and verifying the displayed before-and-after quantities.

**Acceptance Scenarios**:

1. **Given** an existing source balance with sufficient quantity and a different eligible destination location in the same warehouse, **When** the operator submits a positive quantity, required reason, and current source balance version, **Then** the source quantity decreases, the destination quantity increases, and the operator sees the moved quantity and both balances before and after the move.
2. **Given** no balance exists at the eligible destination for the selected SKU, **When** the operator completes a valid move, **Then** the system creates the destination balance with the moved quantity and reports a destination quantity before the move of zero.
3. **Given** a balance row is displayed in the Inventory Balances grid, **When** the operator selects its Move action, **Then** the move dialog shows the SKU, source warehouse, source location, current source quantity, and base unit of measure as read-only context.
4. **Given** a move completes successfully, **When** the result is acknowledged, **Then** the Inventory Balances grid refreshes and shows the updated quantities.

---

### User Story 2 - Preserve Auditable Inventory History (Priority: P1)

As an inventory controller, I want every successful manual inventory move to produce balanced inventory history so that the relocation can be audited without being represented as an inventory adjustment or planned transfer.

**Why this priority**: Accurate, balanced, and attributable ledger history is required for inventory integrity and operational investigation.

**Independent Test**: Can be tested by completing one valid manual move and verifying that it creates one transfer-type inventory transaction, exactly two opposing ledger entries, and no inventory transfer document.

**Acceptance Scenarios**:

1. **Given** a valid manual move, **When** the move succeeds, **Then** the system creates one inventory transaction classified as `Transfer` with the supplied reason.
2. **Given** a successful manual move, **When** ledger history is reviewed, **Then** exactly one negative entry exists for the source location and exactly one equal positive entry exists for the destination location.
3. **Given** a successful manual move, **When** its recorded effects are inspected, **Then** the two ledger quantities sum to zero and reference the same inventory transaction.
4. **Given** any rejected move, **When** inventory state and history are inspected, **Then** no source or destination quantity, inventory transaction, or ledger entry has changed.

---

### User Story 3 - Look Up Balance by SKU and Location (Priority: P2)

As a future scanner client, I need to retrieve the current inventory balance for a SKU at a source storage location so that available quantity and the current balance version can be validated before a manual move is submitted.

**Why this priority**: The lookup prepares a reusable read boundary for future scanner workflows while remaining useful independently of scanner UI.

**Independent Test**: Can be tested by requesting a known and unknown SKU/location pair and verifying that the known pair returns current balance details and the unknown pair returns not found.

**Acceptance Scenarios**:

1. **Given** an inventory balance exists for the requested SKU and storage location, **When** the balance is looked up, **Then** the system returns its current details, including quantity and balance version.
2. **Given** no inventory balance exists for the requested SKU and storage location, **When** the balance is looked up, **Then** the system returns a not-found result.
3. **Given** an inventory balance exists but its SKU, storage location, location type, or location status is inactive, **When** the balance is looked up, **Then** the system still returns the current balance details so the client can report the actual inventory state.

### Edge Cases

- The move is rejected when quantity is zero, negative, or greater than the current source quantity.
- The move is rejected when the source balance no longer exists or its current version differs from the version submitted by the client.
- The move is rejected when source and destination locations are the same or belong to different warehouses.
- The move is rejected when the destination location does not exist.
- The move is rejected when the SKU is inactive, even if a source balance exists.
- The move is rejected when the source or destination location, its location type, or its location status is inactive.
- The move is rejected when either location is not a regular storage location, including internal-transit and external-transit locations.
- The move is rejected when the reason is blank or exceeds the allowed inventory transaction reason length.
- Moving the full source quantity retains the source balance with quantity zero; the move does not delete the source balance.
- Concurrent attempts to move from the same source balance cannot both succeed using the same source balance version.
- Concurrent moves targeting the same existing destination balance cannot both commit from the same destination state; one succeeds and the other returns a conflict without partial changes.
- Failure while recording any part of the move leaves balances, transaction history, and ledger history unchanged.
- Planned transfer documents, transfer progress, inter-warehouse movement, transit workflows, scanner UI, inventory adjustment, reservations, approvals, package tracking, batch, serial, and expiry behavior remain outside this feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a warehouse operator to initiate a manual move from an existing Inventory Balances grid row.
- **FR-002**: The manual move interface MUST display the selected SKU, source warehouse, source storage location, current source quantity, and base unit of measure as read-only context.
- **FR-003**: The operator MUST be able to select only an eligible destination storage location in the same warehouse as the source.
- **FR-004**: The operator MUST provide a positive move quantity and a non-blank reason before confirming the move.
- **FR-005**: A successful move MUST decrease the source balance by the moved quantity.
- **FR-006**: A successful move MUST increase an existing destination balance by the moved quantity or create a destination balance when none exists.
- **FR-007**: A successful move MUST return the moved quantity, occurrence time, and source and destination quantities before and after the move.
- **FR-008**: The Inventory Balances grid MUST refresh after a successful move.
- **FR-009**: A successful move MUST create exactly one inventory transaction classified as `Transfer`.
- **FR-010**: A successful move MUST create exactly two inventory ledger entries: one negative source entry and one equal positive destination entry.
- **FR-011**: The move reason MUST be recorded with the inventory transaction and remain visible through existing inventory ledger history.
- **FR-012**: The system MUST provide a balance lookup by SKU and storage location.
- **FR-013**: A successful balance lookup MUST return current Inventory Balance details, including quantity and balance version.
- **FR-014**: A balance lookup MUST return not found when no balance exists for the requested SKU and storage location.
- **FR-015**: The manual move MUST NOT create, require, update, or reference an Inventory Transfer, Inventory Transfer Line, or Inventory Transfer Movement.
- **FR-016**: The manual move MUST NOT be recorded as an inventory adjustment.
- **FR-017**: A balance lookup MUST return an existing balance even when its SKU, storage location, location type, or location status is inactive.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: Source SKU, source storage location, destination storage location, quantity, reason, and expected source balance version are required for a manual move.
- **DR-002**: Source and destination storage locations must be different active regular storage locations in the same warehouse.
- **DR-003**: Source and destination location types and statuses must be active.
- **DR-004**: Internal-transit and external-transit locations cannot be used as either endpoint of a manual move.
- **DR-005**: The source inventory balance must exist and the moved quantity must not exceed its current quantity.
- **DR-006**: The expected source balance version must match the current source balance version at the time the move is committed.
- **DR-007**: A destination balance may exist before the move or be created as part of the move.
- **DR-008**: The destination balance represents the same SKU as the source balance at the selected destination location.
- **DR-009**: The transaction reason must be non-blank and no longer than the established inventory transaction reason limit.
- **DR-010**: The source decrement, destination increment or creation, transfer transaction, and two ledger entries must succeed or fail as one operation.
- **DR-011**: The two ledger entries for a move must reference the same transaction and have equal magnitude with opposite signs.
- **DR-012**: When the moved quantity equals the current source quantity, the source balance must remain persisted with quantity zero.
- **DR-013**: When concurrent moves target the same existing destination balance, only one may commit against a given destination state; another conflicting move must fail atomically and be retried.
- **DR-014**: The SKU represented by the source balance must be active when the manual move is committed.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: The read boundary MUST support lookup by SKU identifier and storage location identifier at `GET /api/wms/inventory/balances/lookup`.
- **CB-002**: The lookup response MUST use the existing Inventory Balance details shape and include the current balance version.
- **CB-003**: The write boundary MUST expose manual move as an explicit action at `POST /api/wms/inventory/balances/move`.
- **CB-004**: The move request MUST carry the SKU, source location, destination location, quantity, reason, and expected source balance version.
- **CB-005**: The move result MUST carry source and destination balance details, moved quantity, source and destination quantities before and after, and the occurrence time.
- **CB-006**: The write boundary MUST follow the established action-result convention; the lookup boundary MUST follow the established read/load and not-found convention.
- **CB-007**: Public request and response contracts MUST remain separate from internal move and lookup operations.
- **CB-008**: Destination selection in the manual move interface MUST exclude internal-transit and external-transit locations and must be restricted to the source warehouse.
- **CB-009**: The client MUST submit the balance version from the selected source row as the expected source balance version.
- **CB-010**: Balance lookup MUST report actual persisted inventory state without applying manual-move eligibility rules; operational eligibility MUST be enforced by the move boundary.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: System MUST return a clear validation result for missing required values, non-positive quantity, excessive reason length, inactive SKU, identical locations, ineligible locations, or a destination in another warehouse.
- **OE-002**: System MUST return a clear conflict result when the source balance is missing at commit time, the submitted source balance version is stale, available source quantity is insufficient, or the destination balance changed concurrently.
- **OE-003**: System MUST return not found when the requested balance lookup has no matching SKU and storage location.
- **OE-004**: Rejected moves MUST identify the business reason without exposing internal implementation details.
- **OE-005**: System MUST provide diagnostics sufficient to trace a successful or rejected move by SKU, source location, destination location, moved quantity, transaction identity when created, and rejection reason, without logging sensitive operational data unnecessarily.
- **OE-006**: System MUST prevent partial success from being visible when any balance, transaction, or ledger update fails.
- **OE-007**: Manual move submission MUST return a clear validation error when the SKU, source or destination storage location, source or destination location type, or source or destination location status is inactive.

### Key Entities *(include if feature involves data)*

- **Inventory Balance**: Current quantity of one SKU at one storage location, including a version used to detect stale move requests.
- **Inventory Transaction**: Auditable record classifying a successful manual relocation as a transfer and retaining its reason and occurrence time.
- **Inventory Ledger Entry**: One signed inventory effect at a storage location; a manual move creates one source debit and one destination credit.
- **Storage Location**: Warehouse location whose warehouse, activity, type, status, and regular-storage classification determine eligibility for a manual move.
- **Manual Move Result**: User-facing outcome containing moved quantity and source and destination balance states before and after the operation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: During user acceptance testing, an operator can complete a valid manual move from an Inventory Balances row in under 45 seconds.
- **SC-002**: For 100% of successful move acceptance tests, the source decrease and destination increase equal the submitted moved quantity.
- **SC-003**: For 100% of successful move acceptance tests, exactly one transfer transaction and exactly two balanced ledger entries are recorded.
- **SC-004**: The system rejects 100% of tested stale-version, insufficient-quantity, cross-warehouse, same-location, inactive-location, and transit-location moves without changing inventory or ledger history.
- **SC-005**: Users see the move outcome, including both before-and-after quantities, within 3 seconds for normal warehouse operating loads.
- **SC-006**: Balance lookup returns current details or a clear not-found result within 2 seconds for at least 95% of requests under normal warehouse operating loads.
- **SC-007**: At least 90% of participating warehouse operators complete the valid move scenario on their first attempt without assistance during user acceptance testing.
- **SC-008**: No acceptance-test move creates or changes an Inventory Transfer document, transfer line, transfer movement, transit state, or inventory adjustment record.

## Assumptions

- The current branch `077-implement-manual-inventory-move` is the intended feature branch; branch creation was explicitly skipped.
- Spec Kit directory numbering is independent from branch numbering, so this feature is stored in `specs/076-manual-inventory-move`.
- The operator has existing permission to view inventory balances and perform inventory operations; new roles or permission models are outside this feature.
- The physical relocation is performed or controlled by the operator; this feature records the move and does not orchestrate warehouse routing.
- Existing inventory balance, inventory transaction, inventory ledger, storage-location lookup, and Inventory Balance details concepts are available for reuse during planning.
- The current source balance version is available on Inventory Balances rows and can be submitted by the client.
- A missing destination balance begins with quantity zero and can be created by the successful move without a separate initialization workflow.
- Existing ledger history can display the recorded transaction reason without a separate history feature.
- Scanner-ready balance lookup is included, but scanner UI, device integration, and scan-session behavior are outside scope.
- No persistence migration is expected; planning must document a migration only if implementation analysis identifies a strict schema requirement.
