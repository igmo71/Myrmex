# Feature Specification: Inventory Balance

**Feature Branch**: `048-add-inventory-balance-mvp-vertical-slice`

**Created**: 2026-06-11

**Status**: Draft

**Input**: User description: `--file StakeholderDocs\Wms\Inventory\048 Add Inventory Balance MVP vertical slice.md`

## Clarifications

### Session 2026-06-11

- Q: What should "valid storage location" mean when creating an inventory balance? → A: Existing active `StorageLocation` with active type/status; no `IsPickable` or type-code restriction.
- Q: How should the quantity update flow prevent SKU or storage location changes? → A: Update request accepts only quantity; SKU/location are not part of the contract.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record Current Stock at a Location (Priority: P1)

A warehouse operations user or upstream warehouse workflow records the current known on-hand quantity for one stock keeping unit at one storage location so Myrmex can represent basic inventory state.

**Why this priority**: This is the smallest useful inventory capability. Later warehouse workflows cannot reason about stock without a current SKU/location quantity.

**Independent Test**: Can be fully tested by creating one inventory balance for an existing active SKU with a base unit of measure and an eligible storage location, then confirming the stored quantity is available for retrieval.

**Acceptance Scenarios**:

1. **Given** an active SKU with a base unit of measure and an eligible storage location, **When** a user records a quantity of `10`, **Then** the system creates an inventory balance for that SKU/location pair with quantity `10` in the SKU base unit.
2. **Given** a SKU/location pair already has an inventory balance, **When** a user attempts to create another balance for the same pair, **Then** the system rejects the duplicate and keeps the existing balance unchanged.
3. **Given** a user provides a negative quantity, **When** the user attempts to create an inventory balance, **Then** the system rejects the request with a clear validation message.

---

### User Story 2 - View Inventory Balance Details (Priority: P2)

A warehouse operations user views a specific inventory balance to understand which SKU is stored at which location, in which warehouse context, and in what base unit quantity.

**Why this priority**: Once stock can be recorded, users must be able to verify the balance and inspect enough context to identify the physical and catalog references.

**Independent Test**: Can be fully tested by retrieving an existing inventory balance by identifier and confirming the response includes the balance, SKU, storage location, warehouse context, base unit of measure context, quantity, and timestamps.

**Acceptance Scenarios**:

1. **Given** an inventory balance exists, **When** a user retrieves it by identifier, **Then** the system returns the balance details with SKU, storage location, warehouse, base unit, quantity, created timestamp, and last updated timestamp.
2. **Given** no inventory balance exists for an identifier, **When** a user retrieves that identifier, **Then** the system reports that the balance was not found using the existing Myrmex error style.

---

### User Story 3 - Find Balances by Warehouse, Location, or SKU (Priority: P3)

A warehouse operations user lists inventory balances and narrows the list by SKU, storage location, warehouse, or SKU within a warehouse to answer basic stock visibility questions.

**Why this priority**: Stock visibility becomes useful when users can find balances across warehouse topology and catalog references.

**Independent Test**: Can be fully tested by creating balances across multiple SKUs, storage locations, and warehouses, then verifying each supported filter returns only matching balances with display context.

**Acceptance Scenarios**:

1. **Given** balances exist across multiple SKUs and locations, **When** a user lists balances without filters, **Then** the system returns the available balances with SKU, location, warehouse, base unit, and quantity context.
2. **Given** balances exist for multiple SKUs, **When** a user filters by SKU, **Then** the system returns balances only for that SKU across warehouses and storage locations.
3. **Given** balances exist in multiple locations, **When** a user filters by storage location, **Then** the system returns balances only for that location.
4. **Given** balances exist across warehouses, **When** a user filters by warehouse, **Then** the system returns balances only for storage locations in that warehouse.
5. **Given** balances exist for a SKU across multiple warehouses, **When** a user filters by SKU and warehouse together, **Then** the system returns only balances for that SKU in that warehouse.

---

### User Story 4 - Update Current Quantity Only (Priority: P4)

A warehouse operations user updates the current known quantity of an existing inventory balance without changing the SKU or storage location.

**Why this priority**: Quantity change is the only state change included in this MVP. Changing SKU or location would imply a different balance or later stock movement workflow.

**Independent Test**: Can be fully tested by updating an existing balance quantity to a non-negative decimal value, then confirming the quantity and last updated timestamp changed while SKU and location remained unchanged.

**Acceptance Scenarios**:

1. **Given** an inventory balance exists with quantity `10`, **When** a user updates the quantity to `5`, **Then** the system stores quantity `5` for the same SKU/location pair.
2. **Given** an inventory balance exists, **When** a user updates the balance through the quantity update flow, **Then** the update request accepts only the new quantity and provides no SKU or storage location fields.
3. **Given** a user provides a negative quantity, **When** the user attempts to update the inventory balance, **Then** the system rejects the update with a clear validation message.

### Edge Cases

- Creating a balance is rejected when the SKU does not exist, is inactive, or does not have a base unit of measure.
- Creating a balance is rejected when the storage location does not exist, the storage location is inactive, or its type or status is inactive.
- A storage location does not need to be pickable and does not need a specific location type code to accept a current inventory balance.
- Zero quantity is allowed. A zero quantity balance means the SKU/location pair is known but currently has no on-hand quantity.
- Regular list results include zero quantity balances by default so users can see known SKU/location pairs without introducing delete or movement semantics.
- Warehouse visibility is resolved through storage location context; the inventory balance itself must not become a conflicting source of warehouse placement.
- Referenced SKU or storage location records cannot be accidentally removed while balances depend on them.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST introduce Inventory Balance as the first minimal Inventory capability for representing current stock state.
- **FR-002**: System MUST allow users to create an inventory balance for an existing active SKU with a base unit of measure and an eligible storage location.
- **FR-003**: System MUST record SKU, storage location, quantity, creation time, and last update time for each inventory balance.
- **FR-004**: System MUST interpret every inventory balance quantity in the SKU base unit of measure.
- **FR-005**: System MUST reject inventory balance creation when the SKU does not exist, is inactive, lacks a base unit of measure, or cannot otherwise be used for current inventory state.
- **FR-006**: System MUST reject inventory balance creation when the storage location does not exist, the storage location is inactive, or its type or status is inactive. `IsPickable` and storage location type code MUST NOT restrict eligibility.
- **FR-007**: System MUST reject negative quantities for both creation and quantity update.
- **FR-008**: System MUST allow zero quantity balances.
- **FR-009**: System MUST prevent more than one inventory balance for the same SKU at the same storage location.
- **FR-010**: System MUST allow users to retrieve a single inventory balance by identifier.
- **FR-011**: Retrieved inventory balance details MUST include enough context to identify the warehouse, storage location, SKU, SKU base unit of measure, quantity, creation time, and last update time.
- **FR-012**: System MUST allow users to list inventory balances.
- **FR-013**: System MUST allow users to filter listed balances by SKU, storage location, warehouse, and SKU within a warehouse.
- **FR-014**: Listed inventory balances MUST include enough context to identify warehouse, storage location, SKU, base unit of measure, and quantity.
- **FR-015**: System MUST allow users to update only the quantity of an existing inventory balance.
- **FR-016**: Quantity update requests MUST accept only the new quantity; SKU and storage location MUST NOT be part of the update contract.
- **FR-017**: System MUST report not-found and validation failures using existing Myrmex behavior.
- **FR-018**: System MUST exclude receiving, putaway, picking, shipping, LPN, batch/lot tracking, expiry, serial numbers, reservations, transaction history, movement history, adjustment documents, unit conversions, packaging, cycle counting, seed/demo data, external integrations, WebApp UI, delete behavior, and activation/deactivation behavior from this MVP.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: An inventory balance represents the current known quantity of one SKU at one storage location.
- **DR-002**: Inventory Balance belongs to the Inventory capability and must not become Catalog or Topology reference data.
- **DR-003**: Inventory depends on Catalog for SKU and base unit context and on Topology for warehouse and storage location context.
- **DR-004**: Warehouse context is derived from the storage location relationship; an inventory balance must not duplicate warehouse placement as separate business state.
- **DR-005**: Quantity is a non-negative decimal value.
- **DR-006**: Quantity is always expressed in the SKU base unit of measure; an inventory balance does not carry its own unit of measure.
- **DR-007**: Inventory Balance has no activation lifecycle. It cannot be deactivated or reactivated.
- **DR-008**: The natural state change for an inventory balance is quantity change, including changes to zero.
- **DR-009**: A wrong SKU or storage location is represented by a separate correct SKU/location balance in a later workflow, not by mutating the identity of an existing balance.
- **DR-010**: There is at most one inventory balance for a SKU/location pair.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: System MUST provide clear validation errors when users reference invalid SKU or storage location records, provide negative quantities, or attempt duplicate SKU/location balances.
- **OE-002**: System MUST provide clear not-found errors when users request or update an inventory balance that does not exist.
- **OE-003**: Operationally important failures during create, retrieve, list, and quantity update MUST be diagnosable using existing Myrmex diagnostics and error conventions.

### Key Entities *(include if feature involves data)*

- **InventoryBalance**: Current stock state for one SKU at one storage location. Key attributes are identifier, SKU reference, storage location reference, non-negative quantity, creation time, and last update time.
- **StockKeepingUnit**: Existing catalog item whose active status and base unit of measure determine whether it can be used for inventory balances.
- **StorageLocation**: Existing topology location that determines where stock is physically placed and supplies warehouse context for inventory visibility.
- **Warehouse**: Existing topology context used for inventory balance visibility through storage locations.
- **UnitOfMeasure**: Existing catalog context identifying the SKU base unit in which inventory balance quantities are expressed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can create a valid inventory balance for an existing active SKU and eligible storage location in under 2 minutes using the available system interface.
- **SC-002**: 100% of accepted inventory balances have a non-negative decimal quantity expressed in the SKU base unit of measure.
- **SC-003**: 100% of attempted duplicate balances for the same SKU/location pair are rejected or prevented.
- **SC-004**: Users can retrieve an existing inventory balance by identifier and see SKU, storage location, warehouse, base unit, quantity, and timestamps in one result.
- **SC-005**: Users can answer all four MVP lookup questions: balances by SKU, by storage location, by warehouse, and by SKU within warehouse.
- **SC-006**: Users can update an existing balance quantity to any non-negative decimal value while the balance keeps the same SKU and storage location.
- **SC-007**: Invalid create or update attempts for missing references, inactive references where applicable, duplicate SKU/location pairs, negative quantity, or missing balances produce clear failure messages.
- **SC-008**: The MVP introduces no user-facing behavior for inventory movements, reservations, delete, activation/deactivation, unit conversion, or WebApp UI.

## Assumptions

- Existing Catalog records supply SKU active status and base unit of measure information.
- Existing Topology records supply storage location activation, type/status activation, and warehouse relationship information.
- Eligible storage locations are existing active `StorageLocation` records with active type/status; `IsPickable` and storage location type code do not restrict inventory balance creation.
- Zero quantity balances remain visible in regular list results by default because the MVP has no delete or movement history behavior.
- Quantity update does not revalidate unchanged SKU and storage location references unless existing Myrmex behavior requires that validation.
- Quantity update requests contain only the new quantity, so SKU and storage location cannot be changed through the update flow.
- Existing Myrmex authorization, validation, error, and diagnostics conventions apply.
