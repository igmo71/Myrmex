# Feature Specification: WebApp Inventory Balance Management UI

**Feature Branch**: `52-add-webapp-inventory-balance-management-ui`

**Created**: 2026-06-13

**Status**: Draft

**Input**: User description: `Add WebApp Inventory Balance management UI. --file "StakeholderDocs\Wms\Inventory\052 Add WebApp Inventory Balance management UI.md" Use the current existing branch 52-add-webapp-inventory-balance-management-ui.`

## Clarifications

### Session 2026-06-13

- Q: How should storage location selectors behave before a warehouse is selected? → A: Require warehouse selection first.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Current Inventory Balances (Priority: P1)

A warehouse operations user opens the Inventory Balances page to see current stock by SKU, warehouse, storage location, quantity, and base unit of measure.

**Why this priority**: Visibility is the first useful value of the Inventory UI. Users need to confirm where stock is stored before creating or correcting balances.

**Independent Test**: Can be fully tested by opening the Inventory area, loading the Inventory Balances page, and confirming that a paged list shows existing balances with the required SKU, warehouse, location, quantity, and base unit context.

**Acceptance Scenarios**:

1. **Given** inventory balances exist, **When** a user opens the Inventory Balances page, **Then** the page shows a bounded list of balances with SKU code, SKU name, warehouse, storage location, quantity, and SKU base unit of measure.
2. **Given** an inventory balance has zero quantity, **When** the list is shown, **Then** the zero quantity balance remains visible.
3. **Given** no balances match the current filters, **When** the list finishes loading, **Then** the page shows a clear empty state rather than an error.

---

### User Story 2 - Find Balances by Warehouse, Location, or SKU (Priority: P2)

A warehouse operations user filters inventory balances to answer where stock is stored, what stock is in a warehouse or location, and where a selected SKU is held.

**Why this priority**: The list is only operationally useful when users can narrow it to the warehouse, storage location, or SKU they are investigating.

**Independent Test**: Can be fully tested by applying warehouse, storage location, and SKU filters independently and together, then confirming the visible balances match the selected criteria.

**Acceptance Scenarios**:

1. **Given** balances exist across multiple warehouses, **When** a user selects a warehouse filter, **Then** the list reloads and shows only balances in that warehouse.
2. **Given** no warehouse is selected, **When** a user reviews the storage location filter, **Then** storage location selection is unavailable until a warehouse is selected.
3. **Given** a warehouse is selected, **When** the user opens the storage location selector, **Then** the available locations are limited to that warehouse.
4. **Given** a storage location is selected, **When** the user changes to an incompatible warehouse, **Then** the storage location selection is cleared before reloading results.
5. **Given** balances exist for a SKU across multiple locations, **When** a user filters by that SKU, **Then** the list shows where the SKU is stored and in what quantity.
6. **Given** a user combines SKU and warehouse filters, **When** the list reloads, **Then** the list shows only matching balances for that SKU in that warehouse.

---

### User Story 3 - Create an Initial Inventory Balance (Priority: P3)

A warehouse operations user manually creates the current inventory balance for an active SKU at an eligible storage location.

**Why this priority**: Users need a controlled way to seed current stock state without introducing receiving, movement, or adjustment workflows.

**Independent Test**: Can be fully tested by opening the create flow, selecting an active SKU, warehouse, storage location, and non-negative quantity, then confirming the new balance appears in the refreshed list.

**Acceptance Scenarios**:

1. **Given** an active SKU with a base unit of measure and an eligible storage location exist, **When** a user creates a balance with quantity `10`, **Then** the dialog closes, success feedback is shown, and the refreshed list includes the new balance.
2. **Given** a user selects a SKU during creation, **When** the selection is made, **Then** the SKU base unit of measure is displayed as read-only context.
3. **Given** a user selects a warehouse during creation, **When** the storage location selector is used, **Then** only compatible storage locations are selectable.
4. **Given** a user enters a negative quantity, **When** the create attempt is submitted, **Then** the balance is not created and the user sees a clear validation message.
5. **Given** a balance already exists for the selected SKU and storage location, **When** the user submits the create flow, **Then** the duplicate conflict is shown and no second balance is created.

---

### User Story 4 - Correct Current Quantity Only (Priority: P4)

A warehouse operations user updates the current known quantity of an existing inventory balance while keeping the SKU, warehouse, storage location, and base unit unchanged.

**Why this priority**: Quantity correction is the only maintenance action included in the MVP; changing identity or location would imply a different inventory workflow.

**Independent Test**: Can be fully tested by opening the update flow from an existing balance row, changing only the quantity to a non-negative value, and confirming the refreshed list shows the new quantity for the same SKU and storage location.

**Acceptance Scenarios**:

1. **Given** an inventory balance exists with quantity `10`, **When** a user updates quantity to `5`, **Then** the dialog closes, success feedback is shown, and the refreshed list shows quantity `5`.
2. **Given** the update dialog is open, **When** the user reviews the balance context, **Then** SKU, warehouse, storage location, and base unit are read-only.
3. **Given** a user attempts a quantity update, **When** the update is submitted, **Then** the only editable business value is quantity.
4. **Given** a user enters a negative quantity, **When** the update is submitted, **Then** the balance is not changed and the user sees a clear validation message.
5. **Given** the inventory balance no longer exists, **When** the user submits a quantity update, **Then** the user sees a not-found message and can return to the refreshed list.

### Edge Cases

- Lookup data is still loading when the user opens a filter or dialog.
- A user attempts to choose a storage location before choosing a warehouse.
- The selected storage location becomes incompatible after the selected warehouse changes.
- A referenced SKU or storage location is no longer valid by the time a create request is submitted.
- A duplicate SKU/location balance is submitted because another user or process already created it.
- An inventory balance is removed or becomes unavailable before a quantity update is submitted.
- The balance list request fails unexpectedly.
- Filters return no matching balances.
- Zero quantity balances must remain visible and selectable for quantity update.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The WebApp MUST expose Inventory through application navigation and provide access to the Inventory Balances page using the application's established navigation conventions.
- **FR-002**: The Inventory Balances page MUST display a bounded, paged list of inventory balances.
- **FR-003**: Each visible balance row MUST show SKU code, SKU name, warehouse, storage location, quantity, and SKU base unit of measure.
- **FR-004**: The list MUST include zero quantity balances.
- **FR-005**: Users MUST be able to filter inventory balances by warehouse.
- **FR-006**: Users MUST be able to filter inventory balances by storage location.
- **FR-007**: Users MUST be able to filter inventory balances by SKU.
- **FR-008**: Users MUST be able to combine SKU and warehouse filters.
- **FR-009**: Changing a selected warehouse MUST reload the list and clear any selected storage location that does not belong to the newly selected warehouse.
- **FR-010**: When a warehouse is selected, storage location choices in filters and create flows MUST be limited to locations in that warehouse.
- **FR-011**: Storage location selectors in filters and create flows MUST be unavailable until a warehouse is selected.
- **FR-012**: The page MUST show a loading state while inventory balances are being loaded.
- **FR-013**: Lookup controls MUST show or handle loading state while warehouse, storage location, and SKU choices are being loaded.
- **FR-014**: When no balances match the active filters, the page MUST show a clear empty state and MUST NOT treat the result as an error.
- **FR-015**: Users MUST be able to open a create flow from the Inventory Balances page.
- **FR-016**: The create flow MUST collect SKU, warehouse, storage location, and quantity.
- **FR-017**: The create flow MUST use active SKUs and valid active storage locations as selectable choices.
- **FR-018**: The create flow MUST display the selected SKU base unit of measure as read-only context.
- **FR-019**: The create flow MUST require quantity to be greater than or equal to zero.
- **FR-020**: If a balance already exists for the selected SKU and storage location, the user MUST see the duplicate conflict and the system MUST NOT create a second balance for that SKU/location pair.
- **FR-021**: Users MUST be able to open a quantity update flow from each balance row.
- **FR-022**: The quantity update flow MUST display SKU, warehouse, storage location, and base unit of measure as read-only context.
- **FR-023**: The quantity update flow MUST allow editing quantity only.
- **FR-024**: The quantity update flow MUST require quantity to be greater than or equal to zero.
- **FR-025**: After a successful create or quantity update, the dialog MUST close, success feedback MUST be shown, and the list MUST refresh.
- **FR-026**: After a successful create or quantity update, active filters and paging state SHOULD be preserved where this does not hide the updated result or create misleading feedback.
- **FR-027**: The UI MUST show meaningful feedback for invalid quantity, invalid or unavailable SKU, invalid or unavailable storage location, duplicate balance, missing inventory balance, and unexpected request failures.
- **FR-028**: The feature MUST reuse existing WebApp interaction, validation, loading, feedback, and error-handling conventions.
- **FR-029**: The feature MUST NOT introduce receiving, putaway, picking, shipping, LPN, batch or lot tracking, expiry date, serial numbers, reservations, inventory transactions, movement history, adjustment documents, unit conversions, packaging, cycle counting, delete, deactivate/reactivate, bulk editing, import/export, seed/demo data, external integrations, backend domain redesign, or backend persistence redesign.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: Inventory Balance represents current stock state only: one SKU at one storage location with a current quantity.
- **DR-002**: Quantity is expressed in the SKU base unit of measure and that unit is displayed as context, not edited in this UI.
- **DR-003**: Zero quantity is valid and visible because this MVP has no delete, movement history, or lifecycle workflow for balances.
- **DR-004**: A balance's SKU, warehouse, storage location, and base unit context are not editable through the quantity update flow.
- **DR-005**: Backend inventory balance uniqueness and validation remain authoritative when create or update requests are submitted.
- **DR-006**: This UI does not create an inventory transaction, adjustment document, or warehouse execution workflow.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: The UI MUST surface clear user-facing messages for validation, not-found, duplicate, and unexpected failures using existing Myrmex WebApp conventions.
- **OE-002**: The UI MUST distinguish empty filtered results from failed list loading.
- **OE-003**: The UI MUST keep users informed during list and lookup loading so they can tell whether data is unavailable or still being retrieved.
- **OE-004**: Create and update failures MUST leave the user in a recoverable state without discarding entered values unless the existing WebApp convention requires a refresh.

### Key Entities *(include if feature involves data)*

- **InventoryBalance**: Current known quantity for one SKU at one storage location; shown, created, filtered, and quantity-corrected through the UI.
- **StockKeepingUnit**: Catalog item selected during create and used as a filter and row display context; active SKUs are selectable.
- **Warehouse**: Topology context used for navigation, filtering, and constraining storage location choices.
- **StorageLocation**: Topology location selected during create, used for filtering, and displayed as the physical stock location.
- **UnitOfMeasure**: SKU base unit shown as read-only context for create, list, and update flows.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can navigate from the main application navigation to the Inventory Balances page in no more than 3 interactions.
- **SC-002**: 100% of visible balance rows show SKU, warehouse, storage location, quantity, and base unit context.
- **SC-003**: Users can filter balances by warehouse, storage location, SKU, and SKU-within-warehouse and see matching results after each filter change.
- **SC-004**: Users can identify where a selected SKU is stored and the quantity at each visible location in under 30 seconds when matching balances exist.
- **SC-005**: Users can create a valid initial inventory balance in under 2 minutes from opening the create flow.
- **SC-006**: 100% of successful create attempts refresh the list and show the created balance when it matches active filters.
- **SC-007**: Users can update an existing balance quantity in under 1 minute without changing SKU, warehouse, storage location, or base unit context.
- **SC-008**: 100% of negative quantity create and update attempts are rejected before or during submission with a clear message.
- **SC-009**: Duplicate SKU/location create conflicts, missing balance update attempts, and unexpected request failures produce user-visible feedback.
- **SC-010**: The delivered MVP exposes no inventory transactions, movement history, delete, deactivate/reactivate, warehouse execution workflows, or backend redesign behavior.

## Assumptions

- The existing Inventory Balance backend capability for create, list, filter, get by id, and quantity-only update is available for the WebApp to use.
- Existing WebApp authorization and navigation conventions determine which authenticated users can access Inventory pages.
- Existing Catalog lookup behavior can provide active SKUs and base unit context.
- Existing Topology lookup behavior can provide warehouse and storage location choices.
- If lookup contracts cannot fully express active storage location, active type, or active status eligibility, backend validation remains the source of truth and the UI displays the returned failure.
- The MVP targets the existing WebApp user experience, not a new mobile-specific or offline workflow.
- Browser-based end-to-end automation is not required for this specification unless a later plan decides otherwise.
