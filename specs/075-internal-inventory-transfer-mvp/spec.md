# Feature Specification: Internal Inventory Transfer MVP

**Feature Branch**: `075-internal-inventory-transfer-mvp`

**Created**: 2026-06-23

**Status**: Draft

**Input**: User description: "StakeholderDocs\Wms\Inventory\075 Internal Inventory Transfer MVP.md"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create Internal Transfer Document (Priority: P1)

As a warehouse supervisor, I want to create an internal inventory transfer with one or more lines so that operators have a controlled document for moving SKU inventory between storage locations in the same warehouse.

**Why this priority**: A transfer document is the business anchor for all movement execution, progress visibility, and ledger traceability.

**Independent Test**: Can be tested by creating transfers with and without a transit location and verifying the transfer header, lines, initial status, and allowed movement pattern.

**Acceptance Scenarios**:

1. **Given** valid same-warehouse transfer details without a transit location and at least one line, **When** the supervisor creates the transfer, **Then** the system records the transfer with status `Created` and allows only direct storage-to-storage movement for its lines.
2. **Given** valid same-warehouse transfer details with an active internal transit location and at least one line, **When** the supervisor creates the transfer, **Then** the system records the transfer with status `Created` and allows only pick-to-transit and place-from-transit movement for its lines.
3. **Given** source and destination warehouses differ, **When** the supervisor creates the transfer, **Then** the system rejects the request because external transfer is outside this MVP.

---

### User Story 2 - Execute Direct Internal Movement (Priority: P1)

As a warehouse operator, I want to confirm that inventory was moved directly from a source storage location to a destination storage location so that the system reflects the completed physical relocation and inventory balances stay accurate.

**Why this priority**: Direct movement is the simplest complete execution path and proves the transfer, ledger, balance, and progress behavior end to end.

**Independent Test**: Can be tested by creating a transfer without transit, committing a partial direct movement, and verifying movement history, ledger impact, balance changes, progress quantities, and status.

**Acceptance Scenarios**:

1. **Given** a direct transfer line with remaining quantity and sufficient source balance, **When** the operator moves a positive quantity, **Then** the system records one immutable movement, records the inventory transaction and ledger impact, decreases the source balance, increases the destination balance, and increases both picked and placed quantities by the moved quantity.
2. **Given** a direct transfer line with requested quantity 10 and already moved quantity 8, **When** the operator tries to move 3 more, **Then** the system rejects the operation and does not change balances, movement history, or transfer progress.
3. **Given** a direct transfer, **When** an operator attempts a pick-to-transit or place-from-transit operation, **Then** the system rejects the operation because the transfer has no transit location.

---

### User Story 3 - Execute Transfer Through Internal Transit (Priority: P1)

As a warehouse operator, I want to pick inventory from a source storage location into an internal transit location and later place it into the destination storage location so that distant or trolley-based movements are represented accurately.

**Why this priority**: Transit movement is a required warehouse execution pattern and introduces in-transit quantity tracking.

**Independent Test**: Can be tested by creating a transfer with transit, committing pick and place quantities independently, and verifying movement history, ledger impact, balance changes, in-transit quantities, and status.

**Acceptance Scenarios**:

1. **Given** a transit transfer line with remaining quantity to pick and sufficient source balance, **When** the operator picks a positive quantity, **Then** the system records one immutable movement, records the inventory transaction and ledger impact, decreases the source balance, increases the transit balance, increases picked quantity, and increases in-transit quantity.
2. **Given** a transit transfer line with positive in-transit quantity, **When** the operator places a positive quantity not exceeding the in-transit quantity, **Then** the system records one immutable movement, records the inventory transaction and ledger impact, decreases the transit balance, increases the destination balance, increases placed quantity, and decreases in-transit quantity.
3. **Given** a transit transfer, **When** an operator attempts a direct storage-to-storage movement, **Then** the system rejects the operation because direct and transit execution patterns cannot be mixed in this MVP.

---

### User Story 4 - Monitor Transfer Progress and History (Priority: P2)

As a warehouse supervisor, I want to list transfers, open transfer details, and view requested, picked, placed, in-transit, and remaining quantities with read-only movement history so that I can monitor execution and investigate inventory movement.

**Why this priority**: Supervisors need operational visibility after transfers can be created and executed.

**Independent Test**: Can be tested by creating transfers in different states, committing movements, and verifying list filtering, transfer details, computed quantities, status, and movement history.

**Acceptance Scenarios**:

1. **Given** transfer movements exist, **When** the supervisor opens transfer details, **Then** each line shows requested, picked, placed, in-transit, remaining-to-pick, and remaining-to-place quantities.
2. **Given** movements exist for a transfer, **When** the supervisor reviews movement history, **Then** the history is read-only and shows the time, SKU, from location, to location, quantity, derived movement meaning, and inventory transaction reference for each movement.
3. **Given** multiple transfers exist, **When** the supervisor lists transfers using supported filters and paging, **Then** the system returns a deterministic page containing transfer status and aggregate requested, picked, placed, and in-transit quantities.

---

### User Story 5 - Complete Transfer Automatically (Priority: P2)

As a warehouse supervisor, I want a transfer to become completed after all requested quantities are placed so that the document status reflects physical completion without manual status maintenance.

**Why this priority**: Completion protects finished transfers from accidental additional movement and makes operational lists reliable.

**Independent Test**: Can be tested by completing all lines through direct or transit movement and verifying automatic status transition and read-only behavior.

**Acceptance Scenarios**:

1. **Given** every transfer line has placed quantity equal to requested quantity and no line has in-transit quantity, **When** the final movement is committed, **Then** the transfer status becomes `Completed`.
2. **Given** a completed transfer, **When** an operator attempts any additional movement, **Then** the system rejects the operation and leaves all balances, movements, and progress quantities unchanged.

### Edge Cases

- Creation is rejected when a transfer has no lines, non-positive requested quantities, inactive SKU or location references, locations from another warehouse, identical source and destination locations, or source/destination locations that are not regular storage locations.
- Creation is rejected when a transit location is specified but is inactive, belongs to another warehouse, or is not an internal transit location.
- Direct movement is rejected when the quantity is non-positive, exceeds remaining requested quantity, or exceeds available source balance.
- Pick movement is rejected when the quantity is non-positive, exceeds remaining requested quantity, or exceeds available source balance.
- Place movement is rejected when the quantity is non-positive or exceeds current in-transit quantity.
- Movement history remains immutable after creation; movement correction and cancellation are outside this MVP.
- Scanner sessions, scanner audit, fixed scan order, package-level scanning, LPN, batch, serial, expiry, reservations, discrepancies, route optimization, approvals, receiving integration, putaway integration, and external transfer are outside this MVP.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a warehouse supervisor to create an internal inventory transfer document for one warehouse with one or more transfer lines.
- **FR-002**: System MUST allow each transfer line to identify one SKU, one source storage location, one destination storage location, and a positive requested quantity.
- **FR-003**: System MUST support transfers without a transit location for direct storage-to-storage movement.
- **FR-004**: System MUST support transfers with one optional internal transit location for storage-to-internal-transit and internal-transit-to-storage movement.
- **FR-005**: System MUST reject transfers where source warehouse and destination warehouse differ.
- **FR-006**: System MUST reject any attempt to mix direct and transit movement patterns inside the same transfer document.
- **FR-007**: System MUST allow an operator to commit a direct movement for a transfer line only when the transfer has no transit location.
- **FR-008**: System MUST allow an operator to commit a pick movement from the line source location to the transfer transit location only when the transfer has an internal transit location.
- **FR-009**: System MUST allow an operator to commit a place movement from the transfer transit location to the line destination location only when the transfer has an internal transit location.
- **FR-010**: System MUST record an immutable movement fact for each committed direct, pick, or place movement.
- **FR-011**: System MUST create one inventory transaction reference and two inventory ledger impacts for each committed movement.
- **FR-012**: System MUST update inventory balances so committed movement decreases the from-location quantity and increases the to-location quantity for the moved SKU.
- **FR-013**: System MUST prevent movement that would make source balance negative, picked quantity exceed requested quantity, placed quantity exceed picked quantity, placed quantity exceed requested quantity, or in-transit quantity become negative.
- **FR-014**: System MUST calculate requested, picked, placed, in-transit, remaining-to-pick, and remaining-to-place quantities for every transfer line from the transfer lines and committed movements.
- **FR-015**: System MUST set transfer status to `Created` when no movements exist, `InProgress` when at least one movement exists and completion rules are not met, and `Completed` when all requested quantities are placed and no quantity remains in transit.
- **FR-016**: System MUST reject new movements on completed transfers.
- **FR-017**: System MUST provide transfer details that include header information, status, transit location when present, line progress quantities, and read-only movement history.
- **FR-018**: System MUST provide a transfer list with server-driven paging, deterministic sorting, and filters for warehouse, status, created date range, transfer code, source location, destination location, SKU, and whether a transit location is present.
- **FR-019**: System MUST expose direct move, pick, and place as explicit business operations rather than generic transfer edits.
- **FR-020**: System MUST keep movement facts independent of scanner workflow state so future scanner execution can resolve scanned input into the same movement operations.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: An internal inventory transfer is limited to one warehouse in this MVP; source and destination warehouse must be the same.
- **DR-002**: A transfer without a transit location allows only regular storage-to-regular storage movement.
- **DR-003**: A transfer with a transit location allows only regular storage-to-internal transit movement and internal transit-to-regular storage movement.
- **DR-004**: A transfer must contain at least one line before it can be created.
- **DR-005**: Transfer line source and destination locations must belong to the transfer warehouse, be active, be different from one another, and be regular storage locations.
- **DR-006**: Transfer line SKU must be active.
- **DR-007**: A transit location, when specified, must be active, belong to the transfer warehouse, and be an internal transit location.
- **DR-008**: A committed movement belongs to exactly one transfer line and references the inventory transaction created for that movement.
- **DR-009**: Movement meaning is derived from from-location and to-location categories; movement type is not a separately maintained business value in this MVP.
- **DR-010**: Each committed movement has two inventory ledger impacts: a negative quantity at the from location and a positive quantity at the to location.
- **DR-011**: Completed transfers are read-only for movement execution.
- **DR-012**: External transit location behavior may be recognized as future reference data, but external transfer behavior is not part of this MVP.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: The create-transfer boundary MUST accept transfer header information and one or more lines, and it MUST return the created transfer identity, code, status, and line identities.
- **CB-002**: The movement boundaries MUST remain separate for direct move, pick to transit, and place from transit so clients and future scanner workflows invoke explicit domain operations.
- **CB-003**: The movement boundaries MUST accept a positive quantity and derive the movement locations from the transfer document and selected line.
- **CB-004**: Transfer details MUST include line progress quantities and movement history in a shape owned by the backend so clients do not recalculate authoritative progress.
- **CB-005**: Transfer list behavior MUST support server-driven paging, deterministic ordering, cancellation of long-running reads, and clear errors for unsupported filters or sort values.
- **CB-006**: User interface actions MUST match the transfer pattern derived from transit location presence: transfers without transit location expose move action, transfers with transit location expose pick and place actions, and completed transfers expose no movement actions.
- **CB-007**: Movement history MUST be displayed as read-only and MUST NOT expose edit or delete actions.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: System MUST return clear user-facing errors when transfer creation references invalid warehouses, SKUs, locations, transit location, line quantities, or unsupported external transfer scope.
- **OE-002**: System MUST return clear user-facing errors when movement execution is rejected because of wrong movement pattern, completed transfer, non-positive quantity, insufficient balance, over-pick, over-place, or stale transfer state.
- **OE-003**: System MUST provide diagnostics for transfer creation and movement execution sufficient to trace transfer identity, line identity, movement identity, inventory transaction reference, and rejection reason.
- **OE-004**: System MUST not partially record a movement: movement history, inventory transaction reference, ledger impact, balance update, and transfer status change must succeed or fail together from the user's perspective.

### Key Entities *(include if feature involves data)*

- **Inventory Transfer**: Transfer document representing the intention and execution state for moving SKU inventory within one warehouse; includes code, warehouse scope, optional transit location, status, lines, and movement history.
- **Inventory Transfer Line**: Requested movement of one SKU from one source storage location to one destination storage location with requested and computed progress quantities.
- **Inventory Transfer Movement**: Immutable committed physical movement fact for a transfer line, including from location, to location, quantity, occurrence time, and inventory transaction reference.
- **Inventory Transaction**: Inventory transaction record identifying that a committed movement came from inventory transfer.
- **Inventory Ledger Impact**: Quantity change entries for the movement's from and to locations that drive balance changes and inventory history.
- **Storage Location Category**: Business classification used to distinguish regular storage locations from internal transit locations for movement validation and derived movement meaning.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A supervisor can create a valid internal transfer with up to 20 lines in under 2 minutes during user acceptance testing.
- **SC-002**: An operator can commit a direct move, pick, or place operation in under 30 seconds after selecting the transfer line and quantity.
- **SC-003**: For 100% of committed movements in acceptance tests, movement history, inventory transaction reference, ledger impact, balance change, and transfer progress are consistent with the moved quantity.
- **SC-004**: The system rejects 100% of tested invalid transfer and movement scenarios without changing balances, movement history, or transfer progress.
- **SC-005**: Transfer details show current line progress and movement history within 3 seconds for transfers containing up to 100 lines and 500 movements.
- **SC-006**: Transfer lists return a deterministic first page within 3 seconds when filtering by warehouse, status, date range, transfer code, source location, destination location, SKU, or transit presence.
- **SC-007**: In user acceptance testing, supervisors can correctly identify whether each sampled transfer is Created, In Progress, or Completed from the list and detail views without external reconciliation.
- **SC-008**: The MVP introduces no scanner-session, package-level, LPN, batch, serial, expiry, reservation, discrepancy, cancellation, or external-transfer workflow in the delivered user-facing behavior.

## Assumptions

- The current branch `075-internal-inventory-transfer-mvp` is the intended feature branch for this specification; no new branch is created for this run.
- The feature directory is `specs/075-internal-inventory-transfer-mvp` to match the selected branch and stakeholder document number.
- Existing warehouse, SKU, storage location, inventory balance, inventory ledger, and server-driven list concepts are available for this feature to build on during planning.
- Regular storage locations already represent normal stock-holding locations; internal transit locations represent trolleys or equivalent temporary movement locations inside the same warehouse.
- Partial direct moves, partial picks, and partial places are allowed because real warehouse execution may happen in more than one physical movement.
- Multiple lines with the same SKU, source location, and destination location are allowed in this MVP; consolidation can be considered later.
- Transfer source and destination locations are required at creation time; automatic source selection and destination suggestion are outside this MVP.
- Scanner execution must remain possible in the future, but this MVP implements no scanner UI, device integration, scan sessions, scan audit, package barcode resolution, or fixed scan order.
