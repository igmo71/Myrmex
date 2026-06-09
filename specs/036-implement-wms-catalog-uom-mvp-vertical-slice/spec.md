# Feature Specification: WMS Catalog/UoM MVP Vertical Slice

**Feature Branch**: `036-implement-wms-catalog-uom-mvp-vertical-slic`

**Created**: 2026-06-09

**Status**: Draft

**Source Issue**: https://github.com/igmo71/Myrmex/issues/36

**Input**: User description: "Implement GitHub issue #36: WMS Catalog/UoM MVP vertical slice. Use the current branch 036-implement-wms-catalog-uom-mvp-vertical-slice. Do not create or switch branches. Use the established Myrmex Spec Kit workflow, constitution, templates, and durable project memory. Keep the specification strictly aligned with issue #36: implement a narrow Catalog Unit of Measure reference-data vertical slice; follow the established Catalog/SKU reference-data pattern where applicable; apply the repeated reference-data testing strategy from #34; do not expand beyond the issue scope. The specification should produce independently testable user stories and keep this MVP focused on UoM reference data only."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create UoM Reference Data (Priority: P1)

A warehouse catalog user can create a unit of measure with the minimum information needed to identify how future WMS quantities are expressed, without creating conversion, packaging, inventory, receiving, or SKU-binding behavior.

**Why this priority**: Unit of measure reference data is the foundation for later quantity-related WMS workflows. Without reliable UoM records, later inventory and receiving features cannot consistently describe quantities.

**Independent Test**: Can be fully tested by creating a UoM with a code, name, and optional symbol, then confirming the UoM is active and available for lookup.

**Acceptance Scenarios**:

1. **Given** no UoM exists with code `EA`, **When** a catalog user creates a UoM with code `EA`, name `Each`, and optional symbol `ea`, **Then** the system records an active UoM with that normalized code, name, optional symbol, creation timestamp, and no update timestamp.
2. **Given** a UoM already exists with code `EA`, **When** a catalog user attempts to create another UoM using the same code with different casing or surrounding spaces, **Then** the system rejects the request with a clear duplicate-code error.
3. **Given** a catalog user provides a missing code or missing name, **When** the user attempts to create a UoM, **Then** the system rejects the request with field-specific validation errors and does not create a UoM.

---

### User Story 2 - Find and Review UoMs (Priority: P2)

A warehouse catalog user can list, search, sort, and open UoM records so they can confirm which quantity labels are already available in the WMS catalog.

**Why this priority**: Users need a dependable way to avoid duplicate units and verify catalog setup before later SKU, inventory, receiving, packaging, or barcode workflows use UoM references.

**Independent Test**: Can be tested by creating multiple UoMs, listing active UoMs, searching by code or name, sorting results using supported list fields, and opening a single UoM by identity.

**Acceptance Scenarios**:

1. **Given** multiple active UoMs exist, **When** a catalog user opens the UoM list, **Then** the system returns a bounded result set with total count, stable ordering, and key UoM details.
2. **Given** active and inactive UoMs exist, **When** a catalog user lists UoMs without requesting inactive records, **Then** only active UoMs are shown.
3. **Given** a UoM exists with code `EA` and name `Each`, **When** a catalog user searches for `EA` or `Each`, **Then** the matching UoM appears in the result set.
4. **Given** a UoM identity exists, **When** a catalog user opens that UoM, **Then** the system returns its current details.
5. **Given** a UoM identity does not exist, **When** a catalog user opens that UoM, **Then** the system returns a clear not-found error.

---

### User Story 3 - Maintain UoM Details and Lifecycle (Priority: P3)

A warehouse catalog user can update UoM descriptive details and deactivate or reactivate UoMs without deleting historical catalog identity.

**Why this priority**: Basic lifecycle control keeps the reference data usable as real catalog terminology changes, while preserving stable UoM identity for future workflows.

**Independent Test**: Can be tested by updating an existing UoM's name or symbol, deactivating it, confirming it is hidden from default lists, and reactivating it.

**Acceptance Scenarios**:

1. **Given** an active UoM exists, **When** a catalog user updates its name or symbol with valid values, **Then** the system records the changed details and update timestamp while preserving the UoM code.
2. **Given** an active UoM exists, **When** a catalog user deactivates it, **Then** the UoM becomes inactive and is excluded from default UoM lists.
3. **Given** an inactive UoM exists, **When** a catalog user reactivates it, **Then** the UoM becomes active and appears in default UoM lists again.
4. **Given** a UoM is already inactive, **When** a catalog user deactivates it again, **Then** the system leaves it inactive and returns its current details without creating a duplicate lifecycle effect.
5. **Given** a UoM is already active, **When** a catalog user reactivates it again, **Then** the system leaves it active and returns its current details without creating a duplicate lifecycle effect.

---

### Edge Cases

- Duplicate UoM codes must be detected after normalizing casing and surrounding whitespace.
- Duplicate UoM code protection must use the stored normalized `Code` value directly and must not require a separate normalized-code field.
- Missing, blank, or overlong UoM code, name, or symbol values must produce clear validation errors tied to the affected field.
- Listing UoMs with negative or excessive paging values must use the system's existing bounded list behavior.
- Searching with empty or whitespace-only text must behave like an unfiltered list.
- Sorting must remain limited to consistently supported UoM list fields and must not introduce unreliable date sorting.
- Requests for inactive UoMs by direct identity must still return the UoM when it exists, so users can review and reactivate it.
- Catalog/UoM behavior must not require or create conversion rules, base or alternative UoM models, SKU-to-UoM bindings, packaging levels, barcodes, inventory quantities, receiving records, LPN behavior, picking or shipping behavior, or external integration messages.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow catalog users to create a UoM with a required UoM code, required name, and optional symbol.
- **FR-002**: The system MUST normalize UoM codes consistently so duplicate detection treats casing differences and surrounding spaces as the same UoM code.
- **FR-003**: The system MUST reject creation when the UoM code is missing, blank, too long, or already used by another UoM.
- **FR-004**: The system MUST reject creation or detail updates when the UoM name is missing, blank, or too long.
- **FR-005**: The system MUST reject creation or detail updates when the UoM symbol exceeds the allowed catalog symbol length.
- **FR-006**: The system MUST return the created or changed UoM details after successful create, update, deactivate, and reactivate operations.
- **FR-007**: The system MUST allow catalog users to retrieve a UoM by identity.
- **FR-008**: The system MUST allow catalog users to list UoMs with bounded paging, total count, text search, supported sorting, and an option to include inactive UoMs.
- **FR-009**: The default UoM list MUST include active UoMs only.
- **FR-010**: The system MUST allow catalog users to update UoM name and symbol without changing the UoM code.
- **FR-011**: The system MUST allow catalog users to deactivate an active UoM.
- **FR-012**: The system MUST allow catalog users to reactivate an inactive UoM.
- **FR-013**: The UoM page MUST be available from the Catalog navigation and support list, search, include-inactive, create, edit, deactivate, and reactivate workflows consistent with the existing Catalog reference-data experience.
- **FR-014**: Write/action operations MUST use the existing Catalog reference-data success/failure behavior, and read/load operations MUST use the existing Catalog reference-data not-found and load-error behavior.
- **FR-015**: Focused automated tests MUST be added according to the repeated reference-data testing policy from issue #34 rather than duplicating the full representative SKU test matrix unless UoM introduces new behavior.
- **FR-016**: The Catalog/UoM MVP MUST remain limited to UoM reference data and MUST NOT implement UoM conversions, base or alternative UoM models, SKU-to-UoM binding, packaging levels, barcode support, inventory quantities, receiving flows, LPN behavior, picking or shipping behavior, external integration behavior, or expanded automated endpoint/UI testing infrastructure.
- **FR-017**: The feature MUST preserve existing WMS Topology and Catalog/SKU behavior while adding the Catalog/UoM slice.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: A UoM represents catalog reference data for expressing quantities in future WMS workflows.
- **DR-002**: UoM code is the stable business identifier for a UoM and must be unique across the WMS catalog.
- **DR-003**: UoM code is assigned when the UoM is created, stored in normalized form, and cannot be changed by the MVP detail update flow.
- **DR-004**: UoM name is the user-facing label for the unit and is required.
- **DR-005**: UoM symbol is an optional short display label and must not carry conversion meaning in this MVP.
- **DR-006**: A UoM is active immediately after creation.
- **DR-007**: Deactivation and reactivation are lifecycle changes; they must not delete the UoM or create another UoM.
- **DR-008**: Repeating a deactivate request for an inactive UoM or a reactivate request for an active UoM is idempotent from a user perspective.
- **DR-009**: UoM records do not carry conversion factors, base-unit relationships, SKU bindings, packaging levels, barcode assignments, inventory balances, receiving state, LPN state, picking or shipping state, or integration state in this MVP.
- **DR-010**: `UpdatedAtUtc` is `null` when a UoM is created and is set only after a successful detail update, deactivate, or reactivate operation.
- **DR-011**: UoM domain events are emitted for create, details updated, deactivated, and reactivated changes only when those changes occur.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: The system MUST return clear validation errors for invalid UoM code, name, and symbol values, including the affected field when applicable.
- **OE-002**: The system MUST return a clear duplicate-code error when a catalog user attempts to create a UoM with an already-used code.
- **OE-003**: The system MUST return a clear not-found error when a catalog user requests or changes a UoM that does not exist.
- **OE-004**: Write/action operations MUST produce user-consumable success or failure results consistent with existing Catalog reference-data behavior.
- **OE-005**: Read/load operations MUST preserve user-facing error behavior consistent with existing Catalog reference-data behavior.
- **OE-006**: Operationally important UoM actions MUST provide enough diagnostics to distinguish validation failure, duplicate code, missing UoM, unsupported sorting, and persistence failure.

### Key Entities *(include if feature involves data)*

- **Unit of Measure (UoM)**: A WMS catalog reference record with identity, UoM code, name, optional symbol, active status, creation timestamp, and optional update timestamp.
- **Catalog**: The WMS reference-data capability that owns SKU and UoM records for future fulfillment workflows. In this MVP, the new behavior is limited to UoM lifecycle and lookup.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A catalog user can create a valid UoM and see the resulting active UoM details in under 1 minute.
- **SC-002**: 100% of duplicate UoM-code attempts are rejected without creating an additional UoM.
- **SC-003**: 100% of invalid UoM create or update attempts return field-specific validation feedback for code, name, or symbol errors.
- **SC-004**: A catalog user can find a known UoM by code or name from a list of at least 25 UoMs in under 30 seconds.
- **SC-005**: Active-only and include-inactive list views return correct lifecycle visibility for at least 10 mixed active and inactive UoMs.
- **SC-006**: Focused UoM automated tests cover UoM-specific domain validation and lifecycle behavior, persistence mapping and uniqueness, and any entity-specific handler or client behavior not already protected by the representative Catalog/SKU pattern.
- **SC-007**: Existing WMS Topology and Catalog/SKU create, list, update, deactivate, reactivate, and error-handling behaviors continue to pass their current acceptance checks after Catalog/UoM is added.

## Assumptions

- The primary MVP user is an internal warehouse catalog or operations user who maintains reference data.
- UoM code, name, and symbol follow the same general text-length expectations as existing WMS Catalog/SKU reference data unless the implementation plan identifies a UoM-specific exception.
- UoM codes are globally unique within the WMS catalog for this MVP.
- UoM code normalization follows the existing Catalog/SKU behavior by storing normalized `Code` directly.
- Direct deletion is out of scope; lifecycle is handled through deactivate and reactivate.
- The MVP includes user-facing behavior needed to create, read, list, update, deactivate, and reactivate UoMs, but does not require import, export, conversion, or binding to SKUs.
- Authentication and authorization behavior reuse the existing application defaults and are not changed by this feature.
- Endpoint and UI automation may be deferred during planning if doing so follows Constitution v1.0.1 and issue #34 testing guidance by documenting lower-level automated coverage, manual UI smoke validation, and whether a follow-up issue is needed.
