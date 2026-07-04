# Feature Specification: Full WMS Demo Data Seeding

**Feature Branch**: `094-full-wms-demo-data-seeding`

**Created**: 2026-07-04

**Status**: Draft

**Input**: User description: `--file StakeholderDocs\094 Full WMS Demo Data Seeding.md`

## Clarifications

### Session 2026-07-04

- Q: How should clear and seed behave if any stage fails? → A: Entire operation rolls back; no partial changes remain.
- Q: How should demo-data endpoints behave when disabled or running in Production? → A: Routes are not registered; requests receive the normal not-found response.
- Q: What should seeding do when a stable demo code belongs to incompatible existing data? → A: Abort and roll back the entire seed operation.
- Q: How should the clear confirmation token be supplied? → A: JSON request body containing the confirmation value.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Seed a Complete Demo Warehouse (Priority: P1)

A developer or demo administrator enables demo-data support for a non-production environment and requests seeding of an empty, schema-ready database. The resulting dataset presents a small, coherent construction-fasteners warehouse in Russian and covers every currently supported WMS demonstration area without depending on 1C.

**Why this priority**: A complete, recognizable dataset is the minimum outcome required to demonstrate Myrmex.

**Independent Test**: Start from an empty application database with its schema present, enable demo-data support, request seeding, and verify the returned summary and all seeded records through the existing application views.

**Acceptance Scenarios**:

1. **Given** a non-production environment, an empty schema-ready database, and demo-data support enabled, **When** an administrator requests seeding, **Then** the system creates the complete bounded demo dataset and returns a concise summary of created, reused, and skipped records.
2. **Given** a successful seed, **When** a user explores the existing WebApp, **Then** the user can demonstrate catalog items, units of measure, warehouse topology, storage locations, balances, ledger history, direct and cart-assisted transfers, and inventory counts.
3. **Given** the seeded records, **When** a user reads user-facing names and descriptions, **Then** those values are in Russian while stable technical codes remain human-readable.
4. **Given** an empty database without the application schema, **When** seeding is requested, **Then** the operation fails clearly without attempting to create or migrate the schema.

**Demo Walkthrough Coverage**:

1. Construction-fastener catalog list.
2. Units of measure.
3. Demo warehouse.
4. Warehouse zones.
5. Storage locations.
6. Storage-location filtering by warehouse, zone, type, status, and active state.
7. Inventory balances.
8. Inventory ledger and movement history.
9. Internal transfer without a cart.
10. Internal transfer through a cart or transit location.
11. Inventory-count examples.
12. Clearing and reseeding through the administrative operations.

---

### User Story 2 - Rerun Seeding Safely (Priority: P2)

A demo administrator can request seeding more than once without accumulating duplicate demo records or corrupting inventory history and balances.

**Why this priority**: Repeatability prevents accidental duplication during preparation and makes the demo setup dependable.

**Independent Test**: Seed an empty database twice without clearing it and compare record identities, business codes, balances, histories, and both execution summaries.

**Acceptance Scenarios**:

1. **Given** the complete known demo dataset already exists, **When** seeding is requested again, **Then** no uncontrolled duplicates are created and the response reports records as reused or skipped as applicable.
2. **Given** only part of the known demo dataset exists and existing records are compatible, **When** seeding is requested, **Then** the system reuses compatible records, creates missing records, preserves consistency, and reports both outcomes.
3. **Given** a stable demo code is already used by incompatible data, **When** seeding is requested, **Then** the system reports a clear conflict and rolls back the entire seed operation rather than overwriting, duplicating, or partially seeding records.

---

### User Story 3 - Reset the Demo Database (Priority: P3)

After users have changed the demonstration data, a demo administrator can explicitly clear all application data and seed it again to restore the known demonstration state without dropping the database or changing its schema.

**Why this priority**: A repeatable reset supports multiple live demonstrations while protecting the database structure.

**Independent Test**: Modify and add application records, request clear with all required safety controls, verify that application data is absent while schema history remains, then seed and compare the result with the original known dataset.

**Acceptance Scenarios**:

1. **Given** a non-production environment with clear enabled and the correct confirmation token, **When** an administrator requests clearing, **Then** all application and demo data is removed in a dependency-safe manner and a concise deletion summary is returned.
2. **Given** a successfully cleared database, **When** the administrator requests seeding, **Then** the database returns to the same known demonstration state.
3. **Given** user-created application records in the demo database, **When** an authorized clear is confirmed, **Then** those records are removed together with seeded data.
4. **Given** an authorized clear request, **When** clearing completes, **Then** the database, schema, and migration history remain intact.

---

### User Story 4 - Prevent Unsafe Demo Operations (Priority: P4)

An operator can rely on demo-data operations being unavailable by default and unusable in Production, with clearing requiring stronger explicit permission than seeding.

**Why this priority**: Destructive administration features must fail closed and cannot become a production reset mechanism.

**Independent Test**: Exercise both operations under disabled, partially enabled, incorrectly confirmed, non-production, and Production configurations and verify availability, errors, data preservation, and audit diagnostics.

**Acceptance Scenarios**:

1. **Given** demo-data support is not explicitly enabled, **When** either demo-data route is requested, **Then** the route is not registered, the caller receives the normal not-found response, and no data changes.
2. **Given** a Production environment, **When** demo-data support is configured as enabled, **Then** neither route is registered, callers receive the normal not-found response, and no data changes.
3. **Given** demo-data support enabled but clearing disabled, **When** clearing is requested, **Then** the request is rejected and no data changes.
4. **Given** clearing enabled but the confirmation token is absent or incorrect, **When** clearing is requested, **Then** the request is rejected and no data changes.
5. **Given** either operation is attempted or completed, **When** operators inspect diagnostics, **Then** they can identify the operation, outcome, and concise record summary without exposing the confirmation secret.

### Edge Cases

- A compatible reference record already exists under a stable demo code while dependent demo records are absent.
- A stable demo code collides with an incompatible existing record.
- Seeding fails after some stages have completed; the entire seed operation must roll back and the response must distinguish failure from complete success.
- Clearing encounters unexpected dependent application data; the entire clear operation must roll back and must not claim complete success.
- Two seed, clear, or seed-and-clear requests overlap; operations must not produce an internally inconsistent or partly reset dataset.
- Current domain terminology supports fewer location statuses or workflow states than the suggested examples; only supported states may be used.
- Cart behavior is represented by a storage location rather than a separate cart entity.
- A currently implemented WMS capability cannot represent a suggested packing, shipping, transfer, or count example; the seed must omit the unsupported example and report it as skipped rather than invent domain behavior.
- The configured confirmation value is empty or missing; clearing must remain unavailable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide an administrative seed operation at `POST /api/admin/demo-data/seed` for a schema-ready, non-production demo database.
- **FR-002**: The system MUST provide an administrative clear operation at `POST /api/admin/demo-data/clear` for a non-production demo database.
- **FR-003**: Demo-data routes MUST NOT be registered unless demo-data support is explicitly enabled.
- **FR-004**: Demo-data routes MUST NOT be registered in the Production environment even if other enablement settings are present.
- **FR-005**: Clearing MUST require both explicit clear enablement and a confirmation value supplied in the JSON request body that exactly matches the configured non-empty confirmation value.
- **FR-006**: A rejected or unavailable demo-data operation MUST make no application-data changes.
- **FR-007**: Seeding MUST create or reuse four units of measure representing pieces, packs, boxes, and kilograms, with the stable codes `PCS`, `PACK`, `BOX`, and `KG`.
- **FR-008**: Seeding MUST create 8–10 recognizable construction-fastener SKUs with stable, deterministic, human-readable codes and Russian user-facing names and descriptions.
- **FR-009**: Seeding MUST NOT create SKU groups, category hierarchies, or SKU barcodes.
- **FR-010**: Seeding MUST create one demo warehouse with code `DEMO` and a compact representative topology covering receiving, bulk storage, picking, packing, shipping, quarantine, and cart or internal-transit functions supported by the current domain.
- **FR-011**: Seeding MUST create or reuse the storage-location types and statuses needed by the dataset, using existing system-defined records and current domain terminology where available rather than duplicating or extending fixed reference models.
- **FR-012**: Seeding MUST create 12–20 storage locations distributed across the supported demo topology, with stable codes and Russian user-facing names.
- **FR-013**: Seeding MUST create 10–20 meaningful inventory balances across bulk, picking, quarantine, and supported packing, shipping, cart, or transit locations.
- **FR-014**: Seeding MUST create 10–20 ledger or movement entries that demonstrate a coherent subset of opening stock, bulk-to-picking, picking-to-packing, packing-to-shipping, quarantine, and cart/transit activity supported by the current domain.
- **FR-015**: Seeding MUST create 2–5 internal transfers, including at least one direct bulk-to-picking transfer and at least one transfer through a cart or transit storage location.
- **FR-016**: Seeded transfers MUST demonstrate only states and actions supported by the current transfer model and MUST include 1–3 SKU lines each.
- **FR-017**: Seeding MUST create 2–4 inventory counts, including an open or in-progress picking-area example with at least two SKUs and two locations and lines demonstrating no variance, shortage, and surplus.
- **FR-018**: Seeding MAY create a completed or historical count only when the current count workflow supports that state without bypassing its business rules.
- **FR-019**: All user-facing names and descriptions created by the feature MUST be in Russian; technical codes MAY be in English and MUST be stable, deterministic, and human-readable.
- **FR-020**: Repeated seeding without clearing MUST NOT create uncontrolled duplicate records or duplicate inventory effects.
- **FR-021**: Seeding MUST reuse compatible existing records identified by stable business identity, create missing records, and skip unsupported optional examples; an incompatible stable-code conflict MUST abort and roll back the entire seed operation rather than overwrite or duplicate the record.
- **FR-022**: Each seed response MUST report the operation outcome and concise counts of created, reused, and skipped records by demonstration area, plus any conflicts or failures.
- **FR-023**: Clearing MUST remove all application records from the configured demo database, including user-created demo records, in an order that respects data dependencies.
- **FR-024**: Clearing MUST NOT drop or recreate the database, modify or remove its schema, remove migration history, or attempt to preserve selected application records.
- **FR-025**: Each clear response MUST report the operation outcome and concise deletion counts by application-data area, plus any failures or remaining data that prevented a complete clear.
- **FR-026**: Clear and seed attempts and outcomes MUST be recorded in operational diagnostics, including the operation, environment, outcome, duration, and summary counts but excluding the confirmation token.
- **FR-027**: The feature MUST include operator documentation explaining enablement, safety controls, clear and seed requests, response interpretation, and verification through the existing WebApp.
- **FR-028**: The feature MUST NOT depend on 1C, change 1C import behavior, add a generic import facility, change production WMS behavior, redesign the UI, or introduce deployment-stand work.
- **FR-029**: Seeding MUST fail clearly when the database schema is absent or incompatible and MUST NOT create or migrate the schema.
- **FR-030**: Overlapping clear and seed operations MUST be rejected or serialized so that observers never receive a success response for an internally inconsistent or partially reset dataset.
- **FR-031**: Each clear or seed request MUST be atomic; if any stage fails, all data changes made by that request MUST be rolled back.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: Stable demo business codes uniquely identify reusable demo reference and operational records across repeated seed requests.
- **DR-002**: Seeded inventory balances, movement history, transfers, and counts MUST agree on SKU, location, quantity, and supported lifecycle state.
- **DR-003**: Inventory-affecting seed actions MUST honor the same current WMS invariants and state transitions as ordinary application operations; a demo-only path may establish otherwise unreachable history only if it preserves the same observable consistency.
- **DR-004**: A cart or transit leg MUST use the current domain representation; when there is no separate cart entity, a supported cart or internal-transit storage location represents the leg.
- **DR-005**: Reference types, statuses, transfer states, and count states MUST come from the current supported domain vocabulary; demo seeding cannot create unsupported lifecycle values.
- **DR-006**: A successful clear leaves zero application records in scope while preserving database structure and migration history.
- **DR-007**: A successful reseed after clear produces the same stable business identities and demonstration scenarios as the original seed.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: `POST /api/admin/demo-data/seed` is a write/action contract and MUST return the standard action-result shape containing a seed execution summary on success and established problem details on failure.
- **CB-002**: `POST /api/admin/demo-data/clear` is a write/action contract and MUST accept explicit clear confirmation in its JSON request body and return the standard action-result shape containing a clear execution summary on success and established problem details on failure.
- **CB-003**: Public request and response contracts MUST remain separate from internal clear/seed requests, domain entities, and persistence representations.
- **CB-004**: When demo-data support is disabled or the environment is Production, both routes MUST be absent and requests MUST receive the application's normal not-found response; registered routes MUST distinguish disallowed clear, invalid confirmation, conflicting data, missing schema, concurrent operation, and execution failure.
- **CB-005**: The feature MUST use existing WMS application and persistence boundaries and MUST NOT expose domain entities directly through the administrative responses.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: The system MUST return clear API errors for disabled support, Production use, disallowed clear, invalid confirmation, incompatible existing data, absent or incompatible schema, concurrent operation, and incomplete execution.
- **OE-002**: Operation diagnostics MUST allow an operator to correlate each request with its completion or failure and determine affected record counts and duration.
- **OE-003**: A failed operation MUST NOT return a success summary, and its error MUST identify whether retry, configuration correction, conflict resolution, or database repair is required.
- **OE-004**: Confirmation secrets and other sensitive configuration values MUST NOT appear in responses or diagnostics.

### Key Entities *(include if feature involves data)*

- **Demo Data Configuration**: The environment-specific controls governing whether demo operations exist, whether clearing is permitted, and which explicit confirmation value authorizes clearing.
- **Demo Seed Execution Summary**: The outcome and created, reused, skipped, conflict, and failure counts for each seeded WMS area.
- **Demo Clear Execution Summary**: The outcome and deletion or remaining-record counts for each cleared application-data area.
- **Unit of Measure and SKU**: The compact construction-fasteners catalog used throughout all demonstration scenarios.
- **Warehouse Topology**: The demo warehouse, zones, location types, statuses, and storage locations that give spatial context to inventory operations.
- **Inventory Balance and Movement**: The current stock position and coherent historical activity for a SKU at storage locations.
- **Internal Transfer**: A direct or cart/transit-assisted movement with lines and lifecycle state supported by the existing WMS domain.
- **Inventory Count**: An open, in-progress, or supported historical count whose lines demonstrate matching, shortage, and surplus outcomes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From an empty schema-ready database, an administrator can seed the complete demo dataset in one request and receive a definitive summary within 2 minutes under normal demo conditions.
- **SC-002**: After seeding, all 12 defined WebApp validation scenarios are demonstrable, including both transfer paths and inventory-count variance examples supported by the current domain.
- **SC-003**: The seeded dataset remains within the stated compact bounds: 4 units, 8–10 SKUs, 1 warehouse, 6–7 zones, 12–20 locations, 10–20 balances, 10–20 ledger entries, 2–5 transfers, and 2–4 counts.
- **SC-004**: 100% of seeded user-facing names and descriptions are Russian, and 100% of seeded technical codes are stable across clear-and-reseed cycles.
- **SC-005**: A second seed request creates zero duplicate business identities and zero duplicate inventory effects.
- **SC-006**: An administrator can clear and reseed a modified demo database back to the known demonstration state within 3 minutes, without losing schema or migration history.
- **SC-007**: Across disabled, Production, clear-disabled, missing-confirmation, and wrong-confirmation validation cases, 100% of unsafe requests leave application data unchanged.
- **SC-008**: A demonstrator can identify the purpose of every seeded warehouse area and complete the primary catalog, topology, inventory, transfer, and count walkthrough without needing undocumented sample data.

## Assumptions

- The database schema and all migrations required by currently implemented WMS capabilities already exist before either operation is used.
- Existing application authentication and network access controls continue to govern who can reach administrative routes; authorization redesign is outside this feature.
- The current WebApp already exposes the catalog, topology, balance, ledger, transfer, and count views needed for verification; only operator documentation is added.
- Suggested location statuses and operational lifecycle examples may be narrowed to the states currently supported by the domain, with unsupported optional records reported as skipped.
- Stable codes are the business identities used to recognize compatible existing demo records; compatibility includes the immutable or invariant attributes required by the current domain.
- The target is a small demonstration dataset, not performance, volume, or concurrency testing data.
- 1C integration, deployment infrastructure, schema migrations, SKU groups, SKU barcodes, generic import, user-management changes, and UI redesign remain out of scope.
