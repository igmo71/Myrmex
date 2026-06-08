# Feature Specification: WMS Catalog/SKU MVP Vertical Slice

**Feature Branch**: `032-implement-wms-catalog-sku-mvp-vertical-slice`

**Created**: 2026-06-08

**Status**: Draft

**Source Issue**: https://github.com/igmo71/Myrmex/issues/32

**Input**: User description: "Use GitHub issue #32 as stakeholder input. Implement WMS Catalog/SKU MVP vertical slice. This is a real implementation feature. Keep the slice small and follow existing WMS Topology behavior patterns. Do not implement Inventory, Barcode, UoM, Packaging, Receiving, LPN contents, Picking, Shipping, or Integration."

## Clarifications

### Session 2026-06-08

- Q: Should SKU duplicate protection store normalized `Code` directly or introduce a separate `NormalizedCode` column? -> A: Store normalized `Code` directly; do not add a separate `NormalizedCode` column for this MVP.
- Q: Should `UpdatedAtUtc` be null on create or equal to `CreatedAtUtc`? -> A: `UpdatedAtUtc` is null on create and is set only after update, deactivate, or reactivate.
- Q: Should StockKeepingUnit domain events be included? -> A: Include events only where they match existing WMS aggregate/domain-event patterns, with no lifecycle event for idempotent no-op deactivate/reactivate calls.
- Q: Should persistence tests be required? -> A: Require practical SQLite/EnsureCreated persistence tests for mapping/table creation and unique code index; do not require SQL Server-specific migration execution tests.
- Q: Should list sorting support all planned fields? -> A: Support code, name, createdAtUtc, updatedAtUtc, and isActive if matching existing Warehouse/Zone list handler patterns; do not add advanced sorting.
- Q: Which domain base classes should the implementation use? -> A: Use existing `EntityBase` and `AggregateRoot` patterns; do not reference or create `Myrmex.Core\Domain\Entity.cs`.
- Q: Should Catalog refactor existing Topology API client support types? -> A: No. Keep Catalog client support local if needed and do not move or rewrite Topology API client infrastructure.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create SKU Reference Data (Priority: P1)

A warehouse catalog user can create a SKU with the minimum descriptive information needed to identify an item in the WMS catalog before inventory, receiving, picking, shipping, or integration workflows exist.

**Why this priority**: SKU reference data is the foundation for later fulfillment workflows. Without a reliable SKU record, future inventory and operational features have no product identity to reference.

**Independent Test**: Can be fully tested by creating a SKU with a code, name, and optional description, then confirming the SKU is active and available for later lookup.

**Acceptance Scenarios**:

1. **Given** no SKU exists with code `ITEM-001`, **When** a catalog user creates a SKU with code `ITEM-001` and a valid name, **Then** the system records an active SKU with that normalized code, name, optional description, creation timestamp, and no update timestamp.
2. **Given** a SKU already exists with code `ITEM-001`, **When** a catalog user attempts to create another SKU using the same code with different casing or surrounding spaces, **Then** the system rejects the request with a clear duplicate-code error.
3. **Given** a catalog user provides a missing code or missing name, **When** the user attempts to create a SKU, **Then** the system rejects the request with field-specific validation errors and does not create a SKU.

---

### User Story 2 - Find and Review SKUs (Priority: P2)

A warehouse catalog user can list, search, sort, and open SKU records so they can confirm which items are already known to the WMS catalog.

**Why this priority**: Users need a dependable way to avoid duplicates and verify catalog setup before operational workflows begin using SKU references.

**Independent Test**: Can be tested by creating multiple SKUs, listing active SKUs, searching by code or name, sorting results, and opening a single SKU by identity.

**Acceptance Scenarios**:

1. **Given** multiple active SKUs exist, **When** a catalog user opens the SKU list, **Then** the system returns a bounded result set with total count, stable ordering, and key SKU details.
2. **Given** active and inactive SKUs exist, **When** a catalog user lists SKUs without requesting inactive records, **Then** only active SKUs are shown.
3. **Given** a SKU exists with code `ITEM-001` and name `Widget`, **When** a catalog user searches for `ITEM` or `Widget`, **Then** the matching SKU appears in the result set.
4. **Given** a SKU identity exists, **When** a catalog user opens that SKU, **Then** the system returns its current details.
5. **Given** a SKU identity does not exist, **When** a catalog user opens that SKU, **Then** the system returns a clear not-found error.

---

### User Story 3 - Maintain SKU Details and Lifecycle (Priority: P3)

A warehouse catalog user can update SKU descriptive details and deactivate or reactivate SKUs without deleting historical catalog identity.

**Why this priority**: Basic lifecycle control keeps the MVP usable as real catalog records change, while preserving stable SKU identity for future features.

**Independent Test**: Can be tested by updating an existing SKU's name or description, deactivating it, confirming it is hidden from default lists, and reactivating it.

**Acceptance Scenarios**:

1. **Given** an active SKU exists, **When** a catalog user updates its name or description with valid values, **Then** the system records the changed details and update timestamp while preserving the SKU code.
2. **Given** an active SKU exists, **When** a catalog user deactivates it, **Then** the SKU becomes inactive and is excluded from default SKU lists.
3. **Given** an inactive SKU exists, **When** a catalog user reactivates it, **Then** the SKU becomes active and appears in default SKU lists again.
4. **Given** a SKU is already inactive, **When** a catalog user deactivates it again, **Then** the system leaves it inactive and returns its current details without creating a duplicate lifecycle effect.
5. **Given** a SKU is already active, **When** a catalog user reactivates it again, **Then** the system leaves it active and returns its current details without creating a duplicate lifecycle effect.

---

### Edge Cases

- Duplicate SKU codes must be detected after normalizing casing and surrounding whitespace.
- Duplicate SKU code protection must use the stored normalized `Code` value directly and must not require a separate normalized-code field.
- Missing, blank, or overlong SKU code, name, or description values must produce clear validation errors tied to the affected field.
- Listing SKUs with negative or excessive paging values must use the system's existing bounded list behavior.
- Searching with empty or whitespace-only text must behave like an unfiltered list.
- Requests for inactive SKUs by direct identity must still return the SKU when it exists, so users can review and reactivate it.
- Catalog/SKU behavior must not require or create inventory balances, barcodes, units of measure, packaging definitions, receiving records, LPN contents, picking work, shipping records, or external integration messages.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow catalog users to create a SKU with a required SKU code, required name, and optional description.
- **FR-002**: The system MUST normalize SKU codes consistently so duplicate detection treats casing differences and surrounding spaces as the same SKU code.
- **FR-003**: The system MUST reject creation when the SKU code is missing, blank, too long, or already used by another SKU.
- **FR-004**: The system MUST reject creation or detail updates when the SKU name is missing, blank, or too long.
- **FR-005**: The system MUST reject creation or detail updates when the SKU description exceeds the allowed catalog description length.
- **FR-006**: The system MUST return the created or changed SKU details after successful create, update, deactivate, and reactivate operations.
- **FR-007**: The system MUST allow catalog users to retrieve a SKU by identity.
- **FR-008**: The system MUST allow catalog users to list SKUs with bounded paging, total count, text search, sorting, and an option to include inactive SKUs.
- **FR-009**: The default SKU list MUST include active SKUs only.
- **FR-010**: The system MUST allow catalog users to update SKU name and description without changing the SKU code.
- **FR-011**: The system MUST allow catalog users to deactivate an active SKU.
- **FR-012**: The system MUST allow catalog users to reactivate an inactive SKU.
- **FR-013**: The Catalog/SKU MVP MUST remain limited to SKU reference data and MUST NOT implement Inventory, Barcode, UoM, Packaging, Receiving, LPN contents, Picking, Shipping, or Integration behavior.
- **FR-014**: The feature MUST preserve existing WMS Topology behavior while adding the Catalog/SKU slice.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: A SKU represents catalog reference data for an item that can be identified by future WMS workflows.
- **DR-002**: SKU code is the stable business identifier for a SKU and must be unique across the WMS catalog.
- **DR-003**: SKU code is assigned when the SKU is created, stored in normalized form, and cannot be changed by the MVP detail update flow.
- **DR-004**: A SKU is active immediately after creation.
- **DR-005**: Deactivation and reactivation are lifecycle changes; they must not delete the SKU or create another SKU.
- **DR-006**: Repeating a deactivate request for an inactive SKU or a reactivate request for an active SKU is idempotent from a user perspective.
- **DR-007**: SKU records do not carry stock quantity, barcode, unit-of-measure, packaging, receiving, LPN, picking, shipping, or integration state in this MVP.
- **DR-008**: `UpdatedAtUtc` is empty when a SKU is created and is set only after a successful detail update, deactivate, or reactivate operation.
- **DR-009**: StockKeepingUnit domain events are emitted for create, details updated, deactivated, and reactivated changes only when those changes occur.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: The system MUST return clear validation errors for invalid SKU code, name, and description values, including the affected field when applicable.
- **OE-002**: The system MUST return a clear duplicate-code error when a catalog user attempts to create a SKU with an already-used code.
- **OE-003**: The system MUST return a clear not-found error when a catalog user requests or changes a SKU that does not exist.
- **OE-004**: Write/action operations MUST produce user-consumable success or failure results consistent with existing WMS behavior.
- **OE-005**: Read/load operations MUST preserve user-facing error behavior consistent with existing WMS behavior.
- **OE-006**: Operationally important SKU actions MUST provide enough diagnostics to distinguish validation failure, duplicate code, missing SKU, and persistence failure.

### Key Entities *(include if feature involves data)*

- **SKU**: A WMS catalog item reference with identity, SKU code, name, optional description, active status, creation timestamp, and optional update timestamp.
- **Catalog**: The WMS reference-data capability that owns SKU records for future fulfillment workflows. In this MVP, the catalog contains SKU lifecycle and lookup behavior only.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A catalog user can create a valid SKU and see the resulting active SKU details in under 1 minute.
- **SC-002**: 100% of duplicate SKU-code attempts are rejected without creating an additional SKU.
- **SC-003**: 100% of invalid SKU create or update attempts return field-specific validation feedback for code, name, or description errors.
- **SC-004**: A catalog user can find a known SKU by code or name from a list of at least 25 SKUs in under 30 seconds.
- **SC-005**: Active-only and include-inactive list views return correct lifecycle visibility for at least 10 mixed active and inactive SKUs.
- **SC-006**: Existing WMS Topology create, list, update, deactivate, reactivate, and error-handling behaviors continue to pass their current acceptance checks after Catalog/SKU is added.

## Assumptions

- The primary MVP user is an internal warehouse catalog or operations user who maintains item reference data.
- SKU code, name, and description follow the same general text-length expectations as existing WMS reference data unless a later plan identifies a domain-specific exception.
- SKU codes are globally unique within the WMS catalog for this MVP.
- SKU code normalization follows existing WMS Topology behavior by storing normalized `Code` directly.
- Direct deletion is out of scope; lifecycle is handled through deactivate and reactivate.
- The MVP includes user-facing behavior needed to create, read, list, update, deactivate, and reactivate SKUs, but does not require a polished bulk import/export experience.
- Authentication and authorization behavior reuse the existing application defaults and are not changed by this feature.
