# Feature Specification: Catalog/SKU Barcode MVP Vertical Slice

**Feature Branch**: `042-catalogsku-barcode-mvp-vertical-slice`

**Created**: 2026-06-09

**Status**: Draft

**Source Issue**: https://github.com/igmo71/Myrmex/issues/42

**Input**: User description: "Create the feature specification for GitHub issue #42: Catalog/SKU Barcode MVP vertical slice. Use issue #42 as the primary source of truth. Follow existing Catalog/SKU and Catalog/UoM MVP vertical slice patterns. Keep this as a small Catalog master-data increment. Barcode symbology is a simple BarcodeSymbology constrained value/enum stored on SkuBarcode, preferably persisted as string in a Symbology field. Barcode symbology is not a separate reference-data table. Barcode value normalization means trimming leading/trailing whitespace only. Do not force uppercase/lowercase normalization because some barcode formats may be case-sensitive. Store the normalized barcode directly in Value. Do not add NormalizedValue. Include IsPrimary for a default SKU barcode. Prefer at most one active primary barcode per StockKeepingUnit if this fits cleanly with existing EF/provider conventions. Exclude barcode symbology reference data, scanning, printing, labels, GS1 parsing, check digit validation, packaging, SKU/UoM conversion, inventory, receiving, LPN, picking/shipping, UI phase, and automatic build/test/database/migration execution."

## Clarifications

### Session 2026-06-09

- Q: Should barcode value uniqueness treat values that differ only by case as duplicates after trimming? → A: Barcode value uniqueness is case-sensitive after trimming; `abc` and `ABC` may coexist.
- Q: When a SKU already has an active primary barcode, how should setting another active barcode as primary behave? → A: Setting one active barcode primary automatically clears primary from other active barcodes for that SKU.
- Q: How should primary barcode status behave during deactivate and reactivate lifecycle operations? → A: Deactivation clears primary status only on the deactivated barcode and never promotes another barcode; reactivation restores active status but leaves the barcode non-primary unless explicitly updated as primary.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Assign SKU Barcodes (Priority: P1)

A warehouse catalog user can assign one or more barcode values to an existing SKU so later catalog lookup work can identify the item by barcode without introducing operational scanning, packaging, or inventory behavior.

**Why this priority**: Barcode master data extends the existing SKU catalog with alternate item identifiers. Without reliable barcode assignment, future warehouse workflows cannot depend on barcode references.

**Independent Test**: Can be fully tested by creating an existing SKU, assigning a barcode value with a barcode symbology and primary flag, then confirming the barcode is active and tied to that SKU.

**Acceptance Scenarios**:

1. **Given** an existing active SKU and no barcode exists with value `012345678905`, **When** a catalog user assigns barcode value `012345678905` with symbology `UpcA`, **Then** the system records an active SKU barcode for that SKU with the provided symbology, primary flag, creation timestamp, and no update timestamp.
2. **Given** a catalog user enters barcode value `  AbC-123  `, **When** the barcode is saved, **Then** the stored value is `AbC-123` with leading and trailing whitespace removed and casing preserved.
3. **Given** no SKU exists for the requested SKU identity, **When** a catalog user tries to assign a barcode to that SKU, **Then** the system rejects the request with a clear missing-SKU error and does not create a barcode.

---

### User Story 2 - Find and Review SKU Barcodes (Priority: P2)

A warehouse catalog user can list SKU barcodes, filter them by SKU, and open a single barcode record so they can verify which barcode values are already assigned.

**Why this priority**: Users need a dependable way to avoid duplicate barcode assignments and inspect barcode setup before future operational workflows use barcode data.

**Independent Test**: Can be tested by creating multiple SKU barcodes across at least two SKUs, listing them, filtering by one SKU, and opening one barcode by identity.

**Acceptance Scenarios**:

1. **Given** multiple active SKU barcodes exist, **When** a catalog user opens the barcode list, **Then** the system returns a bounded result set with total count, stable ordering, and key barcode details.
2. **Given** active and inactive SKU barcodes exist, **When** a catalog user lists barcodes without requesting inactive records, **Then** only active barcodes are shown.
3. **Given** two SKUs have barcode assignments, **When** a catalog user filters the barcode list by one SKU, **Then** only barcodes for that SKU are returned.
4. **Given** a SKU barcode identity exists, **When** a catalog user opens that barcode, **Then** the system returns its current details.
5. **Given** a SKU barcode identity does not exist, **When** a catalog user opens that barcode, **Then** the system returns a clear not-found error.

---

### User Story 3 - Maintain Barcode Details and Lifecycle (Priority: P3)

A warehouse catalog user can update barcode details and deactivate or reactivate barcode assignments without deleting barcode history from the catalog.

**Why this priority**: Barcode values can be corrected, replaced, or temporarily removed from active use while preserving catalog identity and audit-friendly lifecycle behavior.

**Independent Test**: Can be tested by updating an existing barcode's value, symbology, or primary flag, deactivating it, confirming it is hidden from default lists, and reactivating it.

**Acceptance Scenarios**:

1. **Given** an active SKU barcode exists, **When** a catalog user updates its value, symbology, or primary flag with valid values, **Then** the system records the changed details and update timestamp while preserving the barcode identity and owning SKU.
2. **Given** an active non-primary SKU barcode exists, **When** a catalog user deactivates it, **Then** the barcode becomes inactive and is excluded from default barcode lists.
3. **Given** an active primary SKU barcode exists, **When** a catalog user deactivates it, **Then** the barcode becomes inactive, its primary flag is cleared, no other barcode is promoted to primary, and the SKU may have zero active primary barcodes.
4. **Given** a SKU already has an active primary barcode, **When** another active barcode for the same SKU is marked primary, **Then** the system marks the requested barcode as primary and clears primary status from other active barcodes for that SKU.
5. **Given** an inactive SKU barcode exists, **When** a catalog user reactivates it, **Then** the barcode becomes active, remains non-primary, and appears in default barcode lists again.
6. **Given** a SKU barcode is already inactive, **When** a catalog user deactivates it again, **Then** the system leaves it inactive and returns its current details without creating a duplicate lifecycle effect.
7. **Given** a SKU barcode is already active, **When** a catalog user reactivates it again, **Then** the system leaves it active and returns its current details without creating a duplicate lifecycle effect.

---

### Edge Cases

- Duplicate barcode values must be rejected after trimming leading and trailing whitespace.
- Duplicate barcode protection must use the stored `Value` directly and must not require a separate normalized-value field.
- Barcode value casing must be preserved; values that differ only by case must not be collapsed by normalization or duplicate checks in this MVP.
- Missing, blank-after-trim, or overlong barcode values must produce clear validation errors tied to the affected field.
- Unsupported barcode symbology values must produce clear validation errors; barcode symbology records must not be created or managed separately.
- Listing SKU barcodes with negative or excessive paging values must use the system's existing bounded list behavior.
- Searching or filtering with empty or whitespace-only text must behave like an unfiltered list, except when a specific SKU filter is supplied.
- Requests for inactive SKU barcodes by direct identity must still return the barcode when it exists, so users can review and reactivate it.
- Explicitly marking an active SKU barcode as primary during create or update must clear primary status from other active barcodes for the same SKU.
- Lifecycle operations must not silently choose a new default barcode.
- Deactivating a primary barcode must clear primary status on the deactivated barcode, must not promote another barcode, and may leave the SKU with zero active primary barcodes.
- Reactivating a barcode must restore active status only; the reactivated barcode must remain non-primary until explicitly updated as primary.
- Catalog/SKU Barcode behavior must not require or create barcode scanning, printing, labels, GS1 parsing, check digit validation, packaging, SKU/UoM conversion, inventory balances, receiving records, LPN behavior, picking or shipping behavior, external integration messages, or UI screens in this phase.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow catalog users to create a SKU barcode for an existing SKU with a required barcode value, required barcode symbology, active status, and primary flag.
- **FR-002**: The system MUST normalize barcode values by trimming leading and trailing whitespace only.
- **FR-003**: The system MUST store the trimmed barcode value as the barcode value and MUST NOT require a separate normalized-value field.
- **FR-004**: The system MUST preserve barcode value casing because some barcode formats may be case-sensitive.
- **FR-005**: The system MUST reject creation or update when the barcode value is missing, blank after trimming, too long, or already used by another SKU barcode.
- **FR-006**: The system MUST constrain barcode symbology to the supported BarcodeSymbology values for this MVP: Unknown, Ean13, Ean8, UpcA, Code128, QrCode, and Internal.
- **FR-007**: The system MUST NOT introduce user-managed barcode symbology reference data or barcode symbology CRUD behavior.
- **FR-008**: The system MUST include a primary flag so a SKU can identify its default barcode.
- **FR-009**: The system MUST prevent a SKU from presenting more than one active primary barcode by automatically clearing primary status from other active barcodes for that SKU when an active barcode is explicitly set as primary during create or update.
- **FR-010**: The system MUST return the created or changed SKU barcode details after successful create, update, deactivate, and reactivate operations.
- **FR-011**: The system MUST allow catalog users to retrieve a SKU barcode by identity.
- **FR-012**: The system MUST allow catalog users to list SKU barcodes with bounded paging, total count, stable ordering, an option to include inactive barcodes, and filtering by SKU.
- **FR-013**: The default SKU barcode list MUST include active barcodes only.
- **FR-014**: The system MUST allow catalog users to update barcode value, symbology, and primary flag without changing the owning SKU.
- **FR-015**: The system MUST allow catalog users to deactivate an active SKU barcode.
- **FR-016**: Deactivating a SKU barcode MUST set it inactive and, when it was primary, clear its primary flag without promoting another barcode.
- **FR-017**: The system MUST allow catalog users to reactivate an inactive SKU barcode.
- **FR-018**: Reactivating a SKU barcode MUST set it active and leave it non-primary unless the user explicitly updates it with the primary flag set to true.
- **FR-019**: The Catalog/SKU Barcode MVP MUST remain limited to SKU barcode master data and MUST NOT implement barcode scanning, barcode printing, labels, GS1 parsing, barcode check digit validation, packaging, SKU/UoM conversion, inventory, receiving, LPN, picking, shipping, separate barcode symbology reference data, or UI behavior.
- **FR-020**: The feature MUST preserve existing Catalog/SKU and Catalog/UoM behavior while adding the SKU barcode slice.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: A SKU barcode represents a catalog master-data identifier assigned to exactly one existing SKU.
- **DR-002**: A SKU may have multiple barcode assignments.
- **DR-003**: A barcode value must be unique across SKU barcode assignments after leading and trailing whitespace are removed, using case-sensitive comparison of the stored `Value`.
- **DR-004**: Barcode value normalization trims only leading and trailing whitespace; it does not change letter casing or internal whitespace.
- **DR-005**: Barcode symbology means the barcode format, such as Ean13, Ean8, UpcA, Code128, QrCode, or Internal.
- **DR-006**: Barcode symbology is represented by a constrained BarcodeSymbology value carried on the SKU barcode record in a Symbology field, not a separately maintained reference-data record.
- **DR-007**: A SKU barcode is active immediately after creation unless an existing catalog pattern requires an explicit active value in the request.
- **DR-008**: At most one active barcode for a SKU may be primary at a time; explicitly setting one active barcode as primary during create or update clears primary status from other active barcodes for that SKU.
- **DR-009**: Deactivation and reactivation are lifecycle changes; they must not delete the barcode assignment or create another barcode assignment.
- **DR-010**: Lifecycle operations must not silently choose a new default barcode.
- **DR-011**: Deactivating a SKU barcode sets it inactive; if it was primary, deactivation also clears its primary flag.
- **DR-012**: Deactivating a primary SKU barcode does not promote another barcode, so a SKU may have zero active primary barcodes.
- **DR-013**: Reactivating a SKU barcode sets it active but does not restore or assign primary status; reactivated barcodes are non-primary by default.
- **DR-014**: To make a reactivated barcode primary, the user must explicitly update it with the primary flag set to true.
- **DR-015**: Repeating a deactivate request for an inactive SKU barcode or a reactivate request for an active SKU barcode is idempotent from a user perspective.
- **DR-016**: `UpdatedAtUtc` is `null` when a SKU barcode is created and is set only after a successful detail update, deactivate, or reactivate operation.
- **DR-017**: SKU barcode records do not carry scanning events, print jobs, labels, GS1 parsing results, check digit validation state, packaging levels, SKU/UoM conversion rules, inventory balances, receiving state, LPN state, picking or shipping state, or integration state in this MVP.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: The system MUST return clear validation errors for invalid barcode value, symbology, or primary flag changes, including the affected field when applicable.
- **OE-002**: The system MUST return a clear duplicate-value error when a catalog user attempts to create or update a SKU barcode with an already-used value after trimming.
- **OE-003**: The system MUST return a clear missing-SKU error when a catalog user attempts to create a barcode for a SKU that does not exist.
- **OE-004**: The system MUST return a clear not-found error when a catalog user requests or changes a SKU barcode that does not exist.
- **OE-005**: Write/action operations MUST produce user-consumable success or failure results consistent with existing Catalog reference-data behavior.
- **OE-006**: Read/load operations MUST preserve user-facing error behavior consistent with existing Catalog reference-data behavior.
- **OE-007**: Operationally important SKU barcode actions MUST provide enough user/API error detail within existing Myrmex result conventions to distinguish validation failure, duplicate value, missing SKU, missing barcode, unsupported primary change, and persistence failure.

### Key Entities *(include if feature involves data)*

- **SKU Barcode**: A WMS catalog master-data record with identity, owning SKU, barcode value, constrained barcode symbology, primary flag, active status, creation timestamp, and optional update timestamp.
- **Stock Keeping Unit (SKU)**: The existing catalog item reference that owns zero or more SKU barcode assignments.
- **Catalog**: The WMS reference-data capability that owns SKU, UoM, and SKU barcode records for future fulfillment workflows. In this MVP, the new behavior is limited to SKU barcode lifecycle and lookup.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A catalog user can assign a valid barcode to an existing SKU and see the resulting active barcode details in under 1 minute.
- **SC-002**: 100% of duplicate barcode-value attempts are rejected after leading and trailing whitespace are trimmed and the stored `Value` matches case-sensitively, without creating or changing an additional barcode assignment.
- **SC-003**: 100% of valid mixed-case barcode values preserve their original casing after save and retrieval.
- **SC-004**: 100% of invalid SKU barcode create or update attempts return field-specific validation feedback for barcode value, symbology, missing SKU, duplicate value, or unsupported primary change.
- **SC-005**: A catalog user can find known barcode assignments for a specific SKU from a list of at least 25 barcode records in under 30 seconds.
- **SC-006**: Active-only and include-inactive list views return correct lifecycle visibility for at least 10 mixed active and inactive SKU barcodes.
- **SC-007**: A SKU never presents more than one active barcode as primary in validated user-facing results, and lifecycle operations may leave a SKU with zero active primary barcodes.
- **SC-008**: Existing Catalog/SKU and Catalog/UoM create, list, update, deactivate, reactivate, and error-handling acceptance checks remain valid after Catalog/SKU Barcode is added.

## Assumptions

- The primary MVP user is an internal warehouse catalog or operations user who maintains SKU master data.
- SKU barcode behavior is delivered through the same non-UI Catalog vertical slice style as the existing SKU and UoM MVP patterns; new UI screens are out of scope for this phase.
- Direct deletion is out of scope; lifecycle is handled through deactivate and reactivate.
- Barcode value uniqueness is global across SKU barcode assignments for this MVP.
- Barcode symbology is limited to the supported constrained values listed in this specification; adding new symbology values can be handled later without creating barcode symbology reference data in this increment.
- The plan should prefer storing symbology in a human-readable form while preserving the rule that barcode symbology is not separate reference data.
- The one-active-primary-barcode rule is treated as required user-facing behavior; if planning discovers a provider limitation, the trade-off must be documented before implementation proceeds.
- Authentication and authorization behavior reuse the existing application defaults and are not changed by this feature.
- Build, test, application startup, database update, migration generation, and migration application commands remain developer-controlled and are not run automatically by this specification workflow.

## Future Considerations

- Other WMS entities may become barcode-bearing later, such as StorageLocation and LPN. This issue must not introduce a generic barcode ownership model, generic Barcode table, OwnerType/OwnerId, IHasBarcodes, or Barcode module.
- Naming should avoid blocking future reuse of shared barcode primitives, while this MVP remains scoped to SKU barcode master data only.
