# Feature Specification: Local Receiving Order MVP

**Feature Branch**: `116-implement-local-receiving-order-mvp-with-atomic-inventory-posting`

**Created**: 2026-07-22

**Status**: Draft

**Input**: User description: "Implement the local Receiving Order MVP with atomic inventory posting described in issue 116."

## Clarifications

### Session 2026-07-22

- Q: How does full-plan Draft replacement handle line identity? → A: Reconcile by LineId: update retained lines, remove omitted lines, assign IDs only to new lines, and reject foreign line IDs.
- Q: What result does a losing concurrent completion request receive? → A: Reload and return the current Completed result if another request completed the order; do not retry posting, and return conflict only if it remains uncompleted.
- Q: Which StorageLocations are eligible as a ReceivingLocation? → A: An active location in the selected Warehouse whose active StorageLocationType represents Receiving and which passes existing inventory-balance eligibility; use the existing StorageLocation entity without a generalized capability framework.
- Q: Is Receiving Order deletion supported? → A: Permanently delete Draft orders and their lines only, with the current aggregate version and no inventory effect; release the number, and add no archive, soft-delete, or cancellation.
- Q: How is large-order and WebApp success validated without unavailable usability studies? → A: Use deterministic WebApp workflow, information-display, and 300-line functional acceptance checks, with no performance SLA, load benchmark, or formal usability study.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Receive Goods Into Inventory (Priority: P1)

As a warehouse receiving user, I need to create a receiving order, record the goods that physically arrive, and complete the order so that inventory at the selected receiving location increases once and is supported by a distinct receiving transaction.

**Why this priority**: This end-to-end flow is the core warehouse outcome and makes local receiving usable without any external system.

**Independent Test**: Create an order with multiple planned SKU lines, start it, receive each line in one or more increments, and complete it; verify that inventory changes only at completion, all changes occur together, and one receiving transaction records the result.

**Acceptance Scenarios**:

1. **Given** active warehouse data, an eligible Receiving location, and a new unique order number, **When** a user creates an order with valid planned SKU lines, **Then** a Draft order is available with zero received quantity and no inventory effect.
2. **Given** a valid Draft order whose receiving location remains eligible, **When** the user starts receiving, **Then** the order becomes In Progress, records when receiving started, and locks its header and plan.
3. **Given** an In Progress order, **When** the user records a positive quantity against a planned line, **Then** that quantity is added to the line's received total without changing inventory.
4. **Given** every line is fully received, **When** the user completes the order, **Then** the order becomes Completed, inventory at the receiving location increases by each line's received quantity, and one receiving transaction contains one positive entry per line.
5. **Given** completion cannot commit every order and inventory change together, **When** completion fails, **Then** the order remains uncompleted and no partial inventory or transaction result remains.
6. **Given** an order is already completed, **When** completion is requested again, **Then** the existing completed result is returned and inventory is not increased again.

---

### User Story 2 - Revise the Planned Receipt (Priority: P2)

As a warehouse planner, I need to revise the receiving order header and complete planned line set while the order is still a Draft so that the plan accurately reflects the expected delivery before warehouse execution begins.

**Why this priority**: Draft correction prevents avoidable execution errors while preserving a simple, controlled plan once physical receiving starts.

**Independent Test**: Create a Draft, replace its header and full line set, and verify the revised plan is saved; then start the order and verify the same changes are rejected.

**Acceptance Scenarios**:

1. **Given** a Draft order, **When** the user replaces its editable header and full planned line set with valid values, **Then** the revised Draft is saved as one complete plan.
2. **Given** a Draft order, **When** the replacement plan has no lines, duplicate SKUs, inactive references, or an invalid warehouse/location relationship, **Then** the revision is rejected and the existing Draft remains unchanged.
3. **Given** an In Progress or Completed order, **When** a user attempts to change its header, line set, or planned quantities, **Then** the change is rejected.
4. **Given** a current Draft order with no inventory effect, **When** the user deletes it using its current version, **Then** the order and all its lines are permanently removed together and its number becomes available for reuse.
5. **Given** a selected Warehouse, **When** the user chooses a Receiving location while creating or editing a Draft, **Then** the WebApp offers only active locations in that Warehouse whose active StorageLocationType represents Receiving and which pass existing inventory-balance eligibility.

---

### User Story 3 - Find and Execute Receiving Work (Priority: P3)

As a warehouse user, I need to search and filter receiving orders, open their full details, and perform actions from pages suitable for large orders so that I can efficiently manage current and historical receiving work.

**Why this priority**: Discoverability, clear status, and scalable execution views make the core workflow practical for daily warehouse use.

**Independent Test**: Populate orders across warehouses and statuses, locate them through search, filtering, sorting, and paging, then open an order with hundreds of lines and verify that its permitted actions and quantities are clear.

**Acceptance Scenarios**:

1. **Given** receiving orders across multiple warehouses and statuses, **When** a user searches by order number or filters by warehouse and status, **Then** only matching orders are shown using the established sorting and paging behavior.
2. **Given** any receiving order, **When** the user opens its details, **Then** the header, lines, planned, received, and remaining quantities, status, timestamps, current version, and inventory transaction reference when present are visible.
3. **Given** a Draft order containing hundreds of lines, **When** the user creates or edits it, **Then** the full-page experience supports adding, removing, selecting, and locally finding lines without requiring a separate save for every cell.
4. **Given** an In Progress order, **When** the user opens its execution page, **Then** per-line receiving and completion actions are available while complete-document creation or editing is not presented in a modal dialog.

### Edge Cases

- An empty or duplicate order number is rejected; normalization cannot turn a number into an empty value, and uniqueness applies across all local receiving orders.
- A warehouse, receiving location, or SKU that is missing or inactive makes creation, Draft revision, or start invalid.
- A receiving location whose StorageLocationType is inactive or does not represent Receiving, or where existing inventory-balance eligibility rules prohibit inventory, is rejected during creation, Draft revision, and Start.
- A receiving location that does not belong to the order's warehouse is rejected.
- A request that bypasses the WebApp's eligible-location choices is still subject to the same Receiving location validation.
- An order with no lines, duplicate SKU lines, or a zero or negative planned quantity is rejected.
- A Draft replacement that references a line ID belonging to another receiving order is rejected without changing the Draft.
- A zero or negative receive operation, a line not belonging to the order, or a receive operation against a Draft or Completed order is rejected.
- A receive operation that would make the accumulated received quantity exceed the planned quantity is rejected without changing the line.
- Starting an already In Progress order returns the current order without resetting its start time; starting a Completed order is rejected.
- An order with any under-received line cannot be completed, even when other lines are fully received.
- Completion creates a missing eligible SKU/location inventory balance and otherwise increases the existing balance; an ineligible balance cannot be silently created.
- Simultaneous creation of the same missing SKU/location balance or another change to the same balance causes a clear conflict and no partial receiving completion.
- Simultaneous mutations of one receiving order allow at most one current mutation to succeed; stale operations receive a conflict rather than overwriting newer work.
- Simultaneous completion attempts produce at most one inventory posting. A losing request returns the current Completed result when the winner completed the order, without retrying inventory posting; it returns a conflict only when the order remains uncompleted.
- A Draft deletion using a stale aggregate version is rejected as a conflict without removing the order or any line.
- Deletion of an In Progress or Completed order, or any order linked to an inventory transaction or inventory effect, is rejected.
- Failure during completion leaves the order, balances, transaction, and entries as they were before the attempt.
- In Progress and Completed orders cannot be deleted, and completed orders and their inventory history cannot be edited or removed through this workflow.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a standalone local receiving workflow that does not depend on 1C or any other external system.
- **FR-002**: A receiving order MUST have a required, normalized, globally unique number entered by the user for this MVP.
- **FR-003**: A receiving order MUST reference one existing active warehouse and one existing active StorageLocation belonging to that warehouse. The StorageLocation's active StorageLocationType MUST represent Receiving, and existing inventory-balance eligibility rules MUST allow inventory there.
- **FR-004**: A receiving order MUST contain at least one planned line, and every line MUST reference an existing active SKU.
- **FR-005**: Each SKU MUST appear at most once in a receiving order.
- **FR-006**: All planned and received quantities MUST be expressed in the SKU's base unit of measure without packaging or unit conversion.
- **FR-007**: Every planned quantity MUST be greater than zero, every receive operation MUST be greater than zero, and accumulated received quantity MUST remain between zero and planned quantity inclusive.
- **FR-008**: The supported receiving order lifecycle MUST be exactly Draft, In Progress, and Completed, in that order.
- **FR-009**: A newly created receiving order MUST be Draft, MUST set every line's received quantity to zero, and MUST have no inventory effect.
- **FR-010**: Creation MUST save the header and complete initial planned line set as one valid Draft.
- **FR-011**: While an order is Draft, users MUST be able to replace its editable header and complete planned line set as one operation. The replacement MUST reconcile existing lines by line ID: update retained lines, remove omitted lines, and assign IDs only to new lines.
- **FR-012**: Draft creation and revision MUST reject the entire submitted plan when any header or line rule is invalid, including when a submitted line ID does not belong to the receiving order, leaving no partial plan change.
- **FR-013**: Starting MUST revalidate the current header, Receiving location eligibility, and full planned line set before moving a Draft order to In Progress and recording the start time.
- **FR-014**: Repeating start for an In Progress order MUST return the current order without changing its start time; starting a Completed order MUST be rejected.
- **FR-015**: After start, the order number, warehouse, receiving location, planned line set, SKUs, and planned quantities MUST be immutable.
- **FR-016**: While an order is In Progress, users MUST be able to identify a line and increment its received quantity by a valid positive amount.
- **FR-017**: Recording received quantity MUST update the order's modification record but MUST NOT change inventory.
- **FR-018**: Completion MUST be permitted only for an In Progress order whose received quantity equals planned quantity on every line.
- **FR-019**: Completion MUST increase inventory for every order line at the order's receiving location by that line's received quantity, creating an eligible missing balance or increasing the existing balance.
- **FR-020**: Completion MUST create exactly one inventory transaction classified as Receiving, containing exactly one positive inventory history entry for each order line and no fictitious source location.
- **FR-021**: Completion MUST record the completion time and the resulting inventory transaction reference on the order before presenting it as Completed.
- **FR-022**: The Completed state MUST always include a completion time and inventory transaction reference, and a Completed order MUST be immutable.
- **FR-023**: The order completion, all inventory balance changes, the receiving transaction, and all inventory history entries MUST succeed or fail as one indivisible outcome.
- **FR-024**: Repeating completion for an already Completed order MUST return its existing completed result without creating another transaction or changing inventory again.
- **FR-025**: Concurrent completion attempts MUST produce no more than one inventory posting for the order. After a failed concurrent attempt, the system MUST return the current Completed result when another request completed the order, without retrying inventory posting, and MUST return a conflict only when the order remains uncompleted.
- **FR-026**: Draft revision, start, received-quantity entry, completion, and Draft deletion MUST use the Receiving Order's current aggregate version to detect any order or line change made after the user loaded it and MUST return a conflict rather than overwrite the newer state. Receiving Order lines MUST NOT have independent concurrency versions.
- **FR-027**: Completion MUST detect conflicting inventory changes or simultaneous creation of the same SKU/location balance and MUST leave no partial receiving result; the user MUST receive a clear conflict outcome and may refresh before deciding whether to retry.
- **FR-028**: Users MUST be able to retrieve full receiving order details including header, lines, planned, received and remaining quantities, status, timestamps, current version, and inventory transaction reference.
- **FR-029**: Users MUST be able to list receiving orders with search by number, filtering by warehouse and status, sorting, and paging consistent with other WMS lists.
- **FR-030**: Receiving order creation, Draft editing, details, and execution MUST use full-page experiences suitable for orders containing hundreds of lines; only entry of one line's received quantity may use a small dialog.
- **FR-031**: After a successful mutation, the user-facing order view MUST refresh to show current quantities, status, timestamps, version, and available actions; conflicts MUST clearly direct the user to refresh.
- **FR-032**: Important create, update, start, receive, completion, rejection, and conflict outcomes MUST be available to existing operational diagnostics with the relevant order, line, warehouse, location, SKU, quantity, transaction, and outcome identifiers where applicable.
- **FR-033**: Failures for missing data, invalid data, invalid state, duplicate number, duplicate SKU, and concurrent changes MUST be presented consistently with existing WMS workflows.
- **FR-034**: This feature MUST NOT add cancellation, partial completion, negative corrections, reversals, damaged or excess receipt, discrepancies, putaway, scanner/mobile execution, printing, notifications, supplier or purchase-order behavior, packaging, tracked item attributes, automatic numbering, external identity or synchronization, docks, doors, staging areas, putaway rules, multiple receiving-location categories, a generalized location-capability framework, generalized workflow, generalized inventory posting, generic source-document, or generic idempotency capabilities.
- **FR-035**: The receiving capability MUST own the receiving order and its lines while using the existing inventory records as the sole inventory quantity and history source; it MUST NOT maintain a separate receiving inventory quantity.
- **FR-036**: Users MUST be able to permanently delete a Draft receiving order using its current aggregate version. Deletion MUST remove the order and all its lines as one indivisible outcome, MUST be rejected for In Progress or Completed orders and for any order with an inventory transaction or inventory effect, and MUST release the unique order number for reuse. No archive, soft-delete, or cancellation behavior is included.
- **FR-037**: The WebApp MUST offer only eligible Receiving locations for the selected Warehouse. The system MUST independently validate the same Receiving location rule during order creation, Draft revision, and Start.

### Key Entities

- **Receiving Order**: The local warehouse document that owns the receiving plan and execution state. It has a unique number, warehouse, receiving location, lifecycle status, start and completion times, modification history, current version, optional completed inventory transaction reference, and its lines.
- **Receiving Order Line**: A line owned by one receiving order that identifies one SKU and records positive planned quantity and accumulated received quantity in the SKU base unit. Its identity is preserved when retained during Draft replacement, and its state is derived from its quantities rather than stored separately.
- **Warehouse**: The active warehouse in which the physical receipt occurs and to which the receiving location must belong.
- **Storage Location**: The existing location entity used as the ReceivingLocation where completed quantities become inventory. It is active, belongs to the selected Warehouse, has an active StorageLocationType that represents Receiving, and passes existing inventory-balance eligibility rules; no separate ReceivingLocation entity or aggregate is introduced.
- **Storage Location Type**: The existing classification that identifies whether a StorageLocation represents Receiving. This MVP uses one Receiving classification without introducing a generalized location-capability model or multiple receiving-location categories.
- **SKU**: The active stock keeping unit planned and received on a line, with quantities expressed only in its base unit.
- **Inventory Balance**: The authoritative quantity for one SKU at one storage location, created when eligible or increased only upon successful completion.
- **Inventory Transaction**: The single Receiving-classified record of one completed order's inventory posting.
- **Inventory History Entry**: One positive inventory movement within the receiving transaction for one order line, showing the quantity increase and the balance before and after.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In acceptance testing, 100% of valid local orders can progress from Draft through In Progress to Completed without any 1C or other external-system dependency.
- **SC-002**: Across all tested orders, inventory balances and inventory history remain unchanged before completion, including after creation, Draft revision, start, and received-quantity entry.
- **SC-003**: For 100% of successfully completed orders, inventory at the selected receiving location increases by exactly the fully received quantities, exactly one Receiving transaction exists, and its entry count equals the order line count.
- **SC-004**: Across forced completion failures and inventory conflicts, 100% leave no partial order status, balance, transaction, or inventory history change.
- **SC-005**: Across repeated and simultaneous completion attempts in acceptance testing, each order produces no more than one inventory posting and no duplicated quantity.
- **SC-006**: A deterministic acceptance scenario completes the full Draft-to-Completed workflow entirely through the WebApp without bypassing the user-facing workflow through lower-level system or data access.
- **SC-007**: Using a 300-line functional acceptance dataset, an order can be created, revised while Draft, saved, reopened, searched within its current line set, executed, and completed without line loss or order splitting; this criterion is not a response-time or load-performance target.
- **SC-008**: In every defined WebApp state for the acceptance workflow, the UI shows the correct planned, received, and remaining quantities, current status, available actions, and resulting inventory transaction reference when completed.
- **SC-009**: Search, warehouse/status filters, sorting, and paging return the expected order set in 100% of defined list acceptance cases.
- **SC-010**: Review of the delivered scope finds zero introduced workflows or infrastructure for the excluded capabilities in FR-034.
- **SC-011**: Across defined creation, Draft revision, Start, and WebApp location-selection acceptance cases, 100% of accepted Receiving locations belong to the selected Warehouse, are active, have an active StorageLocationType representing Receiving, and pass existing inventory-balance eligibility; all other locations are unavailable for selection or rejected.

## Assumptions

- Existing WMS authorization determines who may view and mutate receiving orders; this feature introduces no new roles or permission model.
- Existing warehouse, storage-location, SKU, inventory eligibility, quantity precision, time, paging, sorting, diagnostics, and conflict-display conventions remain authoritative and are reused.
- Receiving location designation uses the existing StorageLocationType classification on the existing StorageLocation entity; no separate ReceivingLocation entity or aggregate is required.
- The Receiving StorageLocationType's technical identifier and setup are planning decisions governed by existing repository patterns rather than requirements of this specification.
- A single Receiving location category is sufficient for this MVP; docks, doors, staging areas, putaway rules, and generalized location capabilities remain outside scope.
- Physical receipt is performed against one receiving order and one receiving location; multiple receiving sessions and split-location receipt are outside this MVP.
- Users know the local receiving order number and enter it manually; automatic numbering and year-scoped numbering remain deferred.
- A user resolves a conflict by refreshing and deliberately repeating the action if it is still appropriate; the full completion operation is not silently retried.
- Existing inventory records can represent an eligible zero or missing SKU/location balance and ensure inventory quantities remain non-negative.
- Completed warehouse documents and inventory history follow existing retention and restrictive-deletion rules.
- The 300-line scenario is a functional acceptance dataset only; response-time thresholds, load benchmarks, and formal warehouse-user usability studies are outside this MVP.
- Future 1C synchronization will use a stable external identity separate from the local order number and will not change this feature's receiving execution behavior.
