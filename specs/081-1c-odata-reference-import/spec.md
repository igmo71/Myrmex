# Feature Specification: 1C OData Reference Import MVP

**Feature Branch**: `081-1c-odata-reference-import-mvp`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: `StakeholderDocs\081 1C OData Reference Import MVP.md`

## Clarifications

### Session 2026-06-27

- Q: What persistence behavior applies when a connection-level failure occurs after one or more import batches complete? → A: Commit each completed batch; retain it if a later batch fails and report the import as incomplete.
- Q: What happens when a second import of the same reference type is triggered while one is already running? → A: Reject the second import with an "already in progress" result.
- Q: Does an import run synchronously or as a background job? → A: Keep the request active until completion, cancellation, or timeout, then return the final or incomplete summary.
- Q: How are deletion-marked records handled when they are unlinked or cannot be inactivated? → A: Skip an unlinked record; fail a linked record that cannot be inactivated and leave it unchanged.
- Q: Which records contribute to summary counts when an import stops with an uncommitted failed batch? → A: Count only records from completed committed batches and report an operation-level incomplete error.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Verify the 1C Connection (Priority: P1)

An authorized user opens the 1C integration page and checks whether Myrmex can use the configured 1C publication before attempting an import.

**Why this priority**: A clear connection check separates configuration and compatibility problems from data-import problems and is the prerequisite for a reliable pilot demonstration.

**Independent Test**: Configure a reachable 1C publication and verify that the page reports a successful check; then repeat with invalid credentials, an unavailable publication, and a missing required reference-data collection and verify that each failure is clearly distinguished.

**Acceptance Scenarios**:

1. **Given** the integration is enabled and the configured publication is reachable with valid credentials and required reference collections, **When** the user checks the connection, **Then** Myrmex reports that the connection is ready for import.
2. **Given** the configured credentials are invalid, **When** the user checks the connection, **Then** Myrmex reports an authentication failure without exposing the credentials.
3. **Given** the publication is unavailable or times out, **When** the user checks the connection, **Then** Myrmex reports that 1C could not be reached and provides an actionable diagnostic message.
4. **Given** a required reference-data collection is unavailable, **When** the user checks the connection, **Then** Myrmex identifies the incompatible or missing collection.

---

### User Story 2 - Import Warehouses and Units of Measure (Priority: P2)

An authorized user separately imports warehouses and units of measure from 1C so that Myrmex uses familiar operational reference data.

**Why this priority**: Warehouses and units of measure are foundational references needed to make later SKU and WMS workflows meaningful in demonstrations and pilots.

**Independent Test**: Start with a mix of new, previously imported, deletion-marked, and locally conflicting records; run each import separately and verify the resulting reference records and summary counts.

**Acceptance Scenarios**:

1. **Given** 1C contains a warehouse or unit of measure not previously imported and its code is available, **When** the corresponding import runs, **Then** Myrmex creates a reference record linked to the 1C identity.
2. **Given** a warehouse or unit of measure was previously imported, **When** changed source data is imported, **Then** Myrmex updates that same record without creating a duplicate.
3. **Given** a linked source record is marked for deletion and its Myrmex reference type supports inactivity, **When** it is imported, **Then** Myrmex makes the corresponding record inactive and does not physically delete it.
4. **Given** a local record already uses the same code but is not linked to the source identity, **When** the conflicting source record is imported, **Then** Myrmex skips it, preserves the local record, and reports the conflict.
5. **Given** the import completes with successful and unsuccessful records, **When** the result is displayed, **Then** the user sees processed, created, updated, skipped, and failed counts plus available error details.
6. **Given** a deletion-marked source record has no linked Myrmex record, **When** it is imported, **Then** Myrmex skips it without creating a record and reports the reason.
7. **Given** a deletion-marked source record is linked but its Myrmex reference type cannot represent inactivity, **When** it is imported, **Then** Myrmex fails that record, leaves the local record unchanged, and continues processing other records.

---

### User Story 3 - Import Nomenclature as SKUs (Priority: P3)

An authorized user imports a large 1C nomenclature catalog into the Myrmex SKU reference so that pilot users can work with recognizable products.

**Why this priority**: Recognizable product data is the main demonstration value, but it depends on a verified connection and foundational reference-data behavior.

**Independent Test**: Import a source catalog containing more than 15,000 records across multiple batches, including new, changed, deletion-marked, invalid, and conflicting records; verify complete deterministic processing and the final summary.

**Acceptance Scenarios**:

1. **Given** the source contains more records than one configured batch, **When** the SKU import runs, **Then** every available source record is considered exactly once in stable source-identity order.
2. **Given** a source SKU has not been imported and its code is available, **When** it is processed, **Then** Myrmex creates an SKU linked to the source identity.
3. **Given** a source SKU was previously imported, **When** changed source data is processed, **Then** Myrmex updates the same SKU without creating a duplicate.
4. **Given** one source SKU is invalid or conflicts with an unlinked local code, **When** the batch is processed, **Then** Myrmex reports and skips that record while continuing with safely processable records.
5. **Given** the source cannot provide deterministic identity ordering, **When** a multi-batch import is attempted, **Then** Myrmex stops the operation with a clear compatibility error rather than risk missing or duplicating records.

---

### User Story 4 - Repeat an Import Safely (Priority: P4)

An authorized user repeats any reference import after source data changes, confident that previously imported records will be updated rather than duplicated.

**Why this priority**: Repeatability is necessary for pilot use and demonstrations even though scheduled synchronization and conflict-management workflows are outside this MVP.

**Independent Test**: Run the same import twice without source changes and verify that the second run creates no duplicate linked records; then change a source record and verify that a subsequent run updates the existing Myrmex record.

**Acceptance Scenarios**:

1. **Given** a completed import and unchanged source data, **When** the same import runs again, **Then** no additional linked reference records are created.
2. **Given** a previously imported source record has changed, **When** the import runs again, **Then** the corresponding Myrmex record is updated and its latest-import time is refreshed.
3. **Given** an earlier import had record-level failures, **When** the data is corrected and the import is repeated, **Then** corrected records can succeed without duplicating records that succeeded earlier.

### Edge Cases

- The integration is disabled, the base publication location is absent, or one or more reference collection names are not configured.
- Credentials are invalid, unavailable to the running application, or inadvertently included in an import request.
- 1C is unreachable, responds after the configured timeout, returns malformed data, or becomes unavailable between batches.
- The source collection is unavailable or cannot provide stable ordering by source identity.
- A page is empty, the final page is exactly the configured batch size, or source data changes while a multi-batch import is running.
- A source record has an empty or invalid identity, code, description, or other required value.
- Multiple source records share a code, or a source code conflicts with an existing local record that has no source identity.
- A deletion-marked source record has never been imported, in which case it is skipped, or its linked Myrmex reference type does not support inactivity, in which case it fails without changing the local record.
- More record-level errors occur than can reasonably be shown in one response.
- An unauthorized user attempts to test the connection or trigger an import.
- If two users attempt the same reference import concurrently, the later attempt is rejected with an "already in progress" result; imports of other reference types are unaffected.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support configurable enablement, publication location, credentials or secure credential reference, warehouse collection, unit-of-measure collection, nomenclature collection, batch size, and timeout for one primary 1C reference-data source per Myrmex deployment.
- **FR-002**: The system MUST obtain 1C credentials from a secure runtime configuration source; credentials MUST NOT be committed in repository files, accepted in public import requests, exposed in results, or written to user-visible diagnostics.
- **FR-003**: Authorized users MUST be able to test the configured connection and independently trigger warehouse, unit-of-measure, and SKU imports from a dedicated 1C integration page.
- **FR-004**: The connection test MUST distinguish successful readiness, disabled or incomplete configuration, authentication failure, unavailability or timeout, malformed responses, and unavailable required reference collections.
- **FR-005**: The warehouse import MUST create new warehouses and update previously linked warehouses using the immutable 1C source identity.
- **FR-006**: The unit-of-measure import MUST create new units and update previously linked units using the immutable 1C source identity; a source short name or symbol MAY be imported when the configured source provides one that maps unambiguously to the Myrmex unit model.
- **FR-007**: The nomenclature import MUST create new SKUs and update previously linked SKUs using the immutable 1C source identity.
- **FR-008**: The SKU import MUST process catalogs larger than 15,000 records in configurable batches, request only data required for the import, and use deterministic source-identity ordering so records are not omitted or processed twice because of paging.
- **FR-009**: Every imported warehouse, unit of measure, and SKU MUST retain its 1C source identity and the time it was most recently imported successfully.
- **FR-010**: When an incoming source identity is already linked, the system MUST update that linked record even when other imported values have changed.
- **FR-011**: When an incoming source identity is not linked and its code is unused, the system MUST create a new linked record.
- **FR-012**: When an incoming source identity is not linked but its code belongs to a local record with no source identity, the system MUST preserve the local record, skip the incoming record, and report a code conflict; it MUST NOT infer a link from code alone.
- **FR-013**: When a source record is marked for deletion, the system MUST inactivate its linked Myrmex record when that reference type supports inactivity without validating or applying source detail fields; MUST refresh `LastImportedAtUtc`; MUST skip and report an unlinked source record as `SourceRecordDeletionMarked` without creating it; and MUST fail and report a linked record whose reference type cannot represent inactivity while leaving that local record unchanged. Import MUST NOT physically delete a Myrmex record.
- **FR-014**: A safely isolated record-level validation or conflict failure MUST NOT prevent other valid records in the same import from being processed.
- **FR-015**: Each completed batch MUST be committed atomically. A connection-level, authentication, source-compatibility, malformed-response, or unrecoverable paging failure MUST leave earlier completed batches committed, leave the failed batch uncommitted, and report the affected import as incomplete so it can be retried safely.
- **FR-016**: Each import result MUST report the reference type, start and completion times, processed, created, updated, skipped, and failed counts, plus bounded record-level error details containing available source identity, code, reason, and user-readable message.
- **FR-017**: Returned record-level error details MUST be limited to the first 50 errors while summary counts continue to reflect all processed errors.
- **FR-018**: The integration page MUST display Russian labels for connection testing, the three separate imports, summary counts, and visible error details, while broader application localization remains outside scope.
- **FR-019**: The integration page MUST display the result of the most recently completed action in the current page session.
- **FR-020**: All connection tests and imports MUST require the existing authorization mechanism appropriate for WMS integration operations; the feature MUST NOT introduce a new authentication baseline.
- **FR-021**: 1C-specific field names, collection names, and transport representations MUST remain within the 1C integration boundary and MUST NOT appear in WMS domain entities or public Myrmex business contracts.
- **FR-022**: The system MUST allow no more than one running import per reference type. A later attempt for that same type MUST be rejected immediately with an "already in progress" result, without affecting a running import or imports of other reference types.
- **FR-023**: Each import MUST run synchronously while the initiating request remains active. The integration page MUST show that the selected import is in progress and, on completion, cancellation, or timeout, display the resulting complete or incomplete summary; durable or transient background-job tracking is outside scope.
- **FR-024**: When an import is incomplete, its record counts MUST include only completed committed batches. Records from the uncommitted failed batch MUST NOT contribute to processed, created, updated, skipped, or failed counts, and an operation-level error MUST identify why the import ended early.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: A non-empty 1C source identity uniquely identifies at most one warehouse, one unit of measure, or one SKU within its own reference type.
- **DR-002**: The source identity, not the mutable business code, determines whether an imported record is created or updated.
- **DR-003**: An existing unlinked local record cannot become linked to 1C through automatic code matching.
- **DR-004**: Re-importing the same source record cannot create another linked record for that source identity.
- **DR-005**: Source deletion intent results only in supported inactivation. An unlinked deletion-marked record cannot create a Myrmex reference, unsupported inactivation cannot change the linked local record, and imports never physically delete Myrmex reference records.
- **DR-006**: A record's latest-import time changes only when that record is imported successfully.
- **DR-007**: Summary counts MUST reconcile so that processed equals created plus updated plus skipped plus failed for every complete or incomplete import, using only records from completed committed batches.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: The public integration contract MUST expose four distinct authorized actions: connection test, warehouse import, unit-of-measure import, and SKU import; a combined import-all action is not required.
- **CB-002**: Public import results MUST use a stable, reference-neutral summary and error shape suitable for the WebApp; source transport records and WMS domain entities MUST NOT be exposed directly.
- **CB-003**: The integration boundary MUST translate source-specific records into neutral WMS import inputs before WMS validation, upsert, and persistence behavior occurs.
- **CB-004**: The WMS capability MUST own import identity, conflict, validation, lifecycle, and persistence rules; the 1C boundary MUST own connection, source query, paging, and source-to-neutral mapping concerns.
- **CB-005**: Multi-batch source reads MUST use stable ascending source-identity ordering. If the source cannot support that ordering, the action MUST return a clear compatibility error or use a documented fallback proven to preserve complete deterministic processing.
- **CB-006**: Cancellation or timeout of a connection test or import MUST be honored and surfaced as an incomplete operation rather than a successful summary.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: The system MUST return clear, non-secret-bearing user errors for disabled or incomplete configuration, authentication failure, source unavailability, timeout, missing collections, incompatible ordering, malformed responses, invalid records, local code conflicts, and partial failures.
- **OE-002**: The system MUST provide operational diagnostics for connection attempts and imports, including reference type, start and completion, outcome, processed totals, and failure category, without recording credentials.
- **OE-003**: Record-level errors MUST use stable reason categories so support personnel can distinguish conflicts, invalid data, and source or processing failures.
- **OE-004**: When an operation stops before all source records are considered, its result and diagnostics MUST make the incomplete state explicit, identify the operation-level failure, scope record counts to completed committed batches, and MUST NOT present partial counts as a fully successful import.

### Key Entities *(include if feature involves data)*

- **1C Integration Configuration**: Deployment-level settings that identify whether integration is enabled, how the publication is reached securely, which source collections represent each reference type, and what batching and timeout limits apply.
- **Imported Warehouse**: A WMS warehouse reference optionally linked to one immutable 1C source identity and carrying its latest successful import time.
- **Imported Unit of Measure**: A WMS unit reference optionally linked to one immutable 1C source identity and carrying its latest successful import time.
- **Imported Stock Keeping Unit**: A WMS SKU reference optionally linked to one immutable 1C source identity and carrying its latest successful import time.
- **Import Result**: The outcome of one connection or reference-import action, including timing, reconciled counts, completion state, and bounded errors.
- **Import Record Error**: A categorized failure for one source record, identified where possible by source identity and code.

### Scope Boundaries

- The MVP includes online, user-triggered, one-way reference import only.
- The MVP excludes scheduled or real-time synchronization, bidirectional exchange, message-broker or outbox integration, and conflict-resolution or mapping administration screens.
- The MVP excludes receiving documents, customer orders, inventory balances, and prices.
- The MVP excludes dedicated persistent import-history storage; the action result and operational diagnostics are sufficient.
- The MVP excludes warehouse-level integration permissions, a new authentication/authorization baseline, full Myrmex localization, inventory-accounting refactoring, and changes to inventory-count or manual-move behavior.
- The MVP supports one primary 1C reference-data source per deployment and does not distinguish multiple external systems on imported entities.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 90% of pilot users can locate the 1C integration page, test the connection, and start one reference import on their first attempt without assistance.
- **SC-002**: For a reachable representative 1C publication, 95% of connection checks display a definitive readiness or failure result within 5 seconds, excluding a deliberately configured longer timeout.
- **SC-003**: A test catalog containing at least 15,000 valid SKUs is fully considered in one import with no source record omitted or processed more than once.
- **SC-004**: Repeating an unchanged import creates zero duplicate records linked to the same source identity across warehouses, units of measure, and SKUs.
- **SC-005**: In a mixed dataset with isolated invalid or conflicting records, 100% of otherwise valid records are processed and every skipped or failed record contributes to the reported totals.
- **SC-006**: For every completed import, the displayed processed count equals the sum of created, updated, skipped, and failed counts, and the result appears within 2 seconds after processing finishes.
- **SC-007**: In acceptance testing, 100% of authentication, unavailability, timeout, missing-collection, invalid-record, and code-conflict cases produce a distinct actionable result without exposing credentials.
- **SC-008**: After successful imports, 100% of sampled created or updated warehouses, units, and SKUs are visible in the corresponding WMS reference views with recognizable source values.

## Assumptions

- The target 1C publication provides online OData access and immutable source identities for warehouses, units of measure, and nomenclature.
- Exact source collection names vary by 1C configuration and will be supplied through deployment configuration rather than fixed by this specification.
- Nomenclature article number is optional; its absence does not prevent import when all required SKU values are valid.
- The configured default batch size will be chosen during planning and may be adjusted per deployment without changing import behavior.
- Response-only result reporting plus normal operational diagnostics is sufficient for this MVP; a dedicated import-history table is deferred.
- The closest existing authorization mechanism will protect the actions if the preferred integration-specific policy is not yet available.
- Existing Myrmex warehouse, unit-of-measure, and SKU validation rules remain authoritative for imported data.
- Myrmex and the 1C publication remain reasonably stable during one manual import; if source changes prevent deterministic completion, the operation reports an incomplete failure and can be repeated.
- Import actions are serialized per reference type by rejecting a duplicate same-type attempt while an import is running.
- The target deployment supplies secure network access and valid 1C credentials outside repository-managed configuration files.
