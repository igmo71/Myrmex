# Feature Specification: Catalog/SKU Base UoM MVP Vertical Slice

**Feature Branch**: `044-catalogsku-base-uom-mvp-vertical-slice`

**Created**: 2026-06-10

**Status**: Draft

**Source Issue**: https://github.com/igmo71/Myrmex/issues/44

**Primary Requirements Source**: `StakeholderDocs/Wms/Catalog/044 Catalog-SKU Base UoM MVP vertical slice.md`

**Input**: User description: "Create the feature specification for GitHub issue #44. Use the existing current branch: 044-catalogsku-base-uom-mvp-vertical-slice. Do not create a new branch. Use StakeholderDocs/Wms/Catalog/044 Catalog-SKU Base UoM MVP vertical slice.md as the primary requirements source. Follow existing Catalog/SKU and Catalog/UoM MVP vertical slice patterns. Keep the feature limited to required SKU Base UoM binding. Do not run build, tests, app startup, EF migration generation, database update, or migration application automatically. When migration work becomes necessary, stop and recommend exact developer-controlled commands."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create SKU With Base UoM (Priority: P1)

A warehouse catalog user can create a SKU only when the SKU is assigned exactly one existing active base unit of measure, so future quantity workflows have an unambiguous unit for that SKU.

**Why this priority**: The base UoM is now required SKU master data. Without it, later inventory, receiving, packaging, and operational workflows cannot safely express quantities for the SKU.

**Independent Test**: Can be fully tested by creating an active UoM, creating a SKU with that UoM as its base unit, and confirming the created SKU returns the assigned base UoM identity.

**Acceptance Scenarios**:

1. **Given** an active UoM exists and no SKU exists with code `ITEM-001`, **When** a catalog user creates SKU `ITEM-001` with that UoM as its base UoM, **Then** the system records an active SKU with exactly that base UoM assignment and returns the assignment in the created SKU details.
2. **Given** a catalog user omits the base UoM while creating a SKU, **When** the user submits the SKU, **Then** the system rejects the request with a clear base-UoM validation error and does not create the SKU.
3. **Given** no UoM exists for the supplied base UoM identity, **When** a catalog user creates a SKU using that identity, **Then** the system rejects the request with a clear missing-UoM error and does not create the SKU.

---

### User Story 2 - Review SKU Base UoM (Priority: P2)

A warehouse catalog user can retrieve and list SKUs with their base UoM assignment visible in the SKU details returned by the catalog.

**Why this priority**: Users and downstream catalog consumers need to verify that each SKU has the correct base unit before operational workflows depend on SKU quantities.

**Independent Test**: Can be tested by creating multiple SKUs with different active base UoMs, retrieving individual SKUs, and listing SKUs to confirm each result includes the correct base UoM identity.

**Acceptance Scenarios**:

1. **Given** a SKU exists with base UoM `EA`, **When** a catalog user opens that SKU by identity, **Then** the returned SKU details include the assigned base UoM identity.
2. **Given** multiple SKUs exist with assigned base UoMs, **When** a catalog user lists SKUs, **Then** every returned SKU includes its base UoM identity.
3. **Given** a SKU does not exist, **When** a catalog user requests it by identity, **Then** the system returns the existing SKU not-found behavior without creating or changing any UoM assignment.

---

### User Story 3 - Change SKU Base UoM (Priority: P3)

A warehouse catalog user can change the base UoM for an existing SKU to another existing active UoM when correcting or completing SKU master data.

**Why this priority**: Catalog data can be entered incorrectly or refined during setup. Users need a controlled way to correct the required base UoM while keeping the SKU identity stable.

**Independent Test**: Can be tested by creating a SKU with one active base UoM, updating the SKU to a second active base UoM, and confirming subsequent create/update results, direct retrieval, and list results all show the new assignment.

**Acceptance Scenarios**:

1. **Given** a SKU exists with base UoM `EA` and another active UoM `CASE` exists, **When** a catalog user updates the SKU with `CASE` as the base UoM, **Then** the system records the new base UoM assignment and returns the updated SKU details.
2. **Given** an existing SKU and an inactive UoM, **When** a catalog user updates the SKU to use the inactive UoM as its base UoM, **Then** the system rejects the request with a clear inactive-UoM validation error and leaves the SKU's current base UoM unchanged.
3. **Given** an existing SKU, **When** a catalog user updates SKU name or description, **Then** the update still requires a valid base UoM and the returned SKU details include the current base UoM assignment.

---

### Edge Cases

- A SKU create request without a base UoM must be rejected even when all existing SKU code, name, and description values are otherwise valid.
- A SKU update request without a base UoM must be rejected so the SKU cannot become detached from its required base unit.
- A base UoM assignment must reference an existing UoM.
- A base UoM assignment must use an active UoM at the time of create or update.
- If an assigned UoM later becomes inactive through existing UoM lifecycle behavior, existing SKU retrieval and listing must still return the SKU's stored base UoM identity; this feature does not introduce cascading SKU changes.
- Existing SKU duplicate-code, field validation, list, search, lifecycle, and not-found behavior must continue to work with the added base UoM requirement.
- Existing UoM and SKU Barcode behavior must remain valid after SKU base UoM binding is added.
- Catalog/SKU Base UoM behavior must not require or create alternative UoMs, conversion factors, packaging, inventory balances, receiving records, LPN behavior, picking or shipping behavior, seed or demo data, external integration messages, or new UI screens in this phase.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST require a base UoM assignment when a catalog user creates a SKU.
- **FR-002**: The system MUST require a base UoM assignment when a catalog user updates SKU details.
- **FR-003**: The system MUST allow a catalog user to change an existing SKU's base UoM to another valid base UoM.
- **FR-004**: The system MUST reject SKU create or update requests when the supplied base UoM identity is missing.
- **FR-005**: The system MUST reject SKU create or update requests when the supplied base UoM does not exist.
- **FR-006**: The system MUST reject SKU create or update requests when the supplied base UoM is inactive at assignment time.
- **FR-007**: The system MUST return the base UoM identity in successful SKU create and update results.
- **FR-008**: The system MUST return the base UoM identity when a catalog user retrieves a SKU by identity.
- **FR-009**: The system MUST return the base UoM identity for each SKU in SKU list results.
- **FR-010**: The system MUST preserve existing SKU code, name, description, duplicate-code, lifecycle, list, search, sorting, paging, and not-found behavior while adding the required base UoM assignment.
- **FR-011**: The system MUST preserve existing UoM reference-data behavior while allowing active UoMs to be selected as SKU base UoMs.
- **FR-012**: The system MUST preserve existing SKU Barcode behavior while adding the required base UoM assignment to SKU create, update, retrieve, and list behavior.
- **FR-013**: The Catalog/SKU Base UoM MVP MUST remain limited to required SKU-to-base-UoM binding and MUST NOT implement alternative UoMs, UoM conversions, packaging, inventory, receiving, LPN, picking, shipping, seed or demo data, new UI screens, or external integration behavior.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: A SKU must reference exactly one base UoM.
- **DR-002**: The base UoM defines the unit in which the SKU's base quantity will be expressed by future WMS workflows.
- **DR-003**: The base UoM must be an existing Unit of Measure from the Catalog reference data.
- **DR-004**: The base UoM must be active when it is assigned during SKU create or update.
- **DR-005**: A SKU cannot be created without a base UoM.
- **DR-006**: A SKU cannot be updated into a state where it has no base UoM.
- **DR-007**: Changing a SKU's base UoM changes only the SKU's required unit assignment; it does not create conversions, alternative units, packaging definitions, inventory quantities, receiving state, or operational transactions.
- **DR-008**: Existing SKU identity, code normalization, lifecycle behavior, and update timestamp behavior remain governed by the Catalog/SKU MVP rules.
- **DR-009**: Existing UoM identity, code normalization, lifecycle behavior, and update timestamp behavior remain governed by the Catalog/UoM MVP rules.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: The system MUST return a clear validation error when a SKU create or update request is missing a base UoM.
- **OE-002**: The system MUST return a clear missing-UoM error when a SKU create or update request references a UoM that does not exist.
- **OE-003**: The system MUST return a clear inactive-UoM error when a SKU create or update request references an inactive UoM.
- **OE-004**: Write/action operations MUST produce user-consumable success or failure results consistent with existing Catalog reference-data behavior.
- **OE-005**: Read/load operations MUST preserve user-facing error behavior consistent with existing Catalog reference-data behavior.
- **OE-006**: Operationally important SKU base UoM actions MUST provide enough user-consumable error detail within existing Myrmex result conventions to distinguish validation failure, missing UoM, inactive UoM, missing SKU, duplicate SKU code, and persistence failure.

### Key Entities *(include if feature involves data)*

- **Stock Keeping Unit (SKU)**: The existing catalog item reference that now requires exactly one base UoM assignment in addition to its SKU code, name, optional description, lifecycle status, creation timestamp, and optional update timestamp.
- **Unit of Measure (UoM)**: The existing Catalog reference record used to express quantities. An active UoM can be assigned as a SKU's base UoM.
- **Catalog**: The WMS reference-data capability that owns SKU, UoM, and SKU Barcode records for future fulfillment workflows. In this MVP, the new behavior is limited to required SKU base UoM binding.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A catalog user can create a valid SKU with an existing active base UoM and see the assigned base UoM in the created SKU details in under 1 minute.
- **SC-002**: 100% of SKU create or update attempts with a missing, nonexistent, or inactive base UoM are rejected without creating a SKU or changing the SKU's current base UoM.
- **SC-003**: A catalog user can change an existing SKU's base UoM to another existing active UoM and verify the new assignment through direct SKU retrieval and SKU list results in under 1 minute.
- **SC-004**: 100% of returned SKU create, update, get, and list results include the SKU's base UoM identity.
- **SC-005**: Existing Catalog/SKU, Catalog/UoM, and Catalog/SKU Barcode create, list, update, lifecycle, and error-handling acceptance checks remain valid after required SKU base UoM binding is added.
- **SC-006**: No user-facing behavior for alternative UoMs, conversion factors, packaging, inventory, receiving, LPN, picking, shipping, seed/demo data, external integration, or new UI screens is introduced by this feature.

## Assumptions

- The primary MVP user is an internal warehouse catalog or operations user who maintains SKU and UoM master data.
- This feature extends existing SKU create, update, retrieve, and list behavior rather than introducing a separate SKU Base UoM management workflow.
- Active UoM validation applies when assigning a base UoM on SKU create or update; this feature does not change existing UoM deactivate/reactivate behavior.
- Existing development data does not require production-safe preservation for this MVP.
- Persistent data shape changes are expected during planning and implementation, but migration generation, database update, build, test, app startup, and migration application commands remain developer-controlled and must not be run automatically.
- Seed and demo data updates are out of scope and will be handled separately.
- Authentication and authorization behavior reuse the existing application defaults and are not changed by this feature.

## Future Considerations

- Alternative UoMs, conversion factors, packaging levels, inventory quantities, receiving flows, LPN behavior, picking and shipping workflows, and SKU/UoM conversion rules may be added in later features. This MVP must not introduce those models or workflows.
