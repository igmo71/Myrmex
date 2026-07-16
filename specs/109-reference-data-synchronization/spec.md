# Feature Specification: Reactive and On-Demand Reference-Data Synchronization

**Feature Branch**: `109-add-reactive-and-on-demand-reference-data-synchronization`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: `StakeholderDocs/109 Add reactive and on-demand reference-data synchronization.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Apply Reference Changes Reactively (Priority: P1)

As an integration operator, I want a 1C change notification for a Warehouse, Unit of Measure, or Stock Keeping Unit to trigger durable synchronization of the current source object so that WMS reference data stays current without a full manual import.

**Why this priority**: Reactive processing is the primary new business capability and establishes the dependable reference-data path needed by later document synchronization work.

**Independent Test**: Notify a change for each supported reference type, allow the durable request to run, and verify that the current source values and lifecycle state are applied once while duplicate or same-version work completes without duplicate changes.

**Acceptance Scenarios**:

1. **Given** a valid machine-authenticated notification for a supported reference object whose current source version differs from the stored version, **When** the notification is accepted and processed, **Then** the current source object is applied and the durable request completes successfully.
2. **Given** a supported reference already stores the current source version, **When** the same change is processed again, **Then** the result is unchanged, no business data or timestamps change, no domain event is emitted, and the durable request completes successfully.
3. **Given** a notification carries an older version than the current source object, **When** it is processed, **Then** the current object and current version are applied rather than historical state.
4. **Given** the source object is temporarily unavailable or synchronization for that reference type is busy, **When** the durable request is processed, **Then** it remains eligible for the existing retry policy with clear diagnostics.
5. **Given** the source object is absent, malformed, invalid, or in unresolved business conflict, **When** the durable request is processed, **Then** it ends as a permanent failure with a reason that identifies the affected reference.

---

### User Story 2 - Synchronize One Required Reference On Demand (Priority: P2)

As an internal fulfillment workflow, I want to synchronize one supported reference by external identity so that later Receiving and Shipping synchronization can repair a missing dependency before retrying document mapping.

**Why this priority**: The single-reference capability is a prerequisite for the bounded dependency-repair flows planned for Receiving and Shipping.

**Independent Test**: Request synchronization of one Warehouse, Unit of Measure, or Stock Keeping Unit by external identity and verify explicit applied, unchanged, skipped, not-found, busy, or failure outcomes without creating a public operator operation.

**Acceptance Scenarios**:

1. **Given** a current supported source reference exists, **When** an internal caller requests synchronization by type and external identity, **Then** the same application rules used by full import apply the object and return an explicit outcome.
2. **Given** the requested source reference does not exist, **When** synchronization is requested internally, **Then** no local record is created or deactivated and the caller receives a not-found outcome.
3. **Given** an SKU refers to a missing or inactive base Unit of Measure, **When** the SKU is synchronized reactively or on demand, **Then** that Unit of Measure is synchronized at most once and the SKU is applied at most one additional time.
4. **Given** the bounded Unit of Measure repair cannot produce an active valid dependency, **When** SKU synchronization continues, **Then** it fails explicitly without recursion or further retries.

---

### User Story 3 - Reconcile Through Existing Full Imports (Priority: P3)

As a WMS operator, I want the existing full-import operations and page to remain available and report unchanged records separately so that I can perform initial loading, reconciliation, and repair without false update counts or a changed workflow.

**Why this priority**: Full import remains the operator-facing recovery mechanism and must stay compatible while adopting version-aware outcomes.

**Independent Test**: Run each existing manual import twice against unchanged source data and verify that the same routes, authorization, error shape, paging behavior, and response fields remain available while the second run reports records as unchanged.

**Acceptance Scenarios**:

1. **Given** an authorized operator uses an existing manual import operation, **When** the source contains new or changed records, **Then** current create, update, validation, lifecycle, transaction, paging, and error behaviors are preserved.
2. **Given** a full import is repeated without source version changes, **When** it completes, **Then** those records are counted as unchanged rather than updated, skipped, or failed.
3. **Given** an import result is shown through an existing response or operator page, **When** the result contains unchanged records, **Then** the unchanged count is displayed alongside created, updated, skipped, and failed counts without removing existing fields.

---

### User Story 4 - Protect 1C-Owned Reference Fields (Priority: P4)

As a WMS operator, I want source-owned fields on 1C-linked references protected from local edits while WMS-owned descriptive fields remain editable so that reactive synchronization can trust stored version state without silently overwriting local divergence.

**Why this priority**: Clear source ownership preserves data integrity and makes same-version no-op behavior safe.

**Independent Test**: Link each supported reference type to 1C, attempt actual local changes to every source-owned field, resubmit the current source-owned values while changing only WMS-owned fields, and verify that only actual source-owned changes are rejected; then repeat with an unlinked record and verify existing behavior remains.

**Acceptance Scenarios**:

1. **Given** a Warehouse linked to 1C, **When** a local operation tries to change its code, name, or source-controlled active state, **Then** the operation is rejected; resubmitting those values unchanged while changing only its description is permitted.
2. **Given** a Unit of Measure linked to 1C, **When** a local operation tries to change its code, name, symbol, or source-controlled active state, **Then** the operation is rejected; resubmitting those source-owned values unchanged is permitted.
3. **Given** an SKU linked to 1C, **When** a local operation tries to change its code, name, base Unit of Measure, or source-controlled active state, **Then** the operation is rejected; resubmitting those values unchanged while changing only its description is permitted.
4. **Given** a reference that is not linked to 1C, **When** an existing local edit operation is used, **Then** its current behavior is preserved.
5. **Given** any linked reference, **When** a normal local edit operation is used, **Then** external identity, source version, and last-import time are not available for local mutation.

### Edge Cases

- A linked legacy reference has an external identity and last-import time but no stored source version; the first successful version-aware synchronization applies the current state and establishes a non-empty version.
- A notification version is older than the version returned by the current source read; the current source object remains authoritative.
- The source version changes while all WMS-relevant values stay equal; synchronization stores the new version and import time and reports an applied update without emitting business-detail or activation events.
- A deletion-marked source object is linked to an active local record; the local record is deactivated without physical deletion.
- A deletion-marked source object is linked to an already inactive local record; synchronization records the new version without emitting a duplicate deactivation event.
- An active source object is linked to an inactive local record; valid current source data reactivates the local record.
- A deletion-marked source object has no linked local record; synchronization skips it successfully without creating an inactive record or validating irrelevant detail fields.
- A Warehouse or SKU source folder is encountered; it is skipped as a controlled successful outcome. Unit of Measure and any other reference type do not gain folder support.
- A single-object source read returns no object, a malformed response, a timeout, or cancellation; each outcome remains distinguishable.
- Processing commits a WMS change but stops before the durable request is marked complete; retry observes the stored version and completes unchanged without a duplicate mutation or event.
- Manual and reactive/on-demand work overlap for the same reference type; only one source-read-and-apply operation proceeds within the current application instance, while different reference types may proceed concurrently.
- Application shutdown occurs during reactive processing; cancellation propagates, the durable request is not classified as completed or failed, and the existing abandoned-processing recovery behavior handles it later.
- A missing SKU base Unit of Measure is itself absent, deletion-marked and unlinked, inactive, invalid, or unavailable; SKU synchronization fails after the single bounded repair attempt.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support reactive, manual full-import, and internal on-demand synchronization for exactly Warehouse, Unit of Measure, and Stock Keeping Unit references.
- **FR-002**: All three entry points MUST apply the same rules for external identity, creation, update, validation, deactivation, reactivation, dependency lookup, persistence, and business-event emission.
- **FR-003**: A reactive notification MUST create or resolve one durable synchronization request through the existing synchronization foundation before acknowledging acceptance.
- **FR-004**: Notification duplication MUST be resolved using source identity and the notified source version without creating parallel work for the same notification.
- **FR-005**: Reactive and on-demand synchronization MUST load the current source object by external identity; the notification version MUST NOT be treated as a request for historical source state.
- **FR-006**: Single-object loading MUST distinguish a found object, an absent object, a malformed source response, temporary source unavailability, timeout, and cancellation.
- **FR-007**: Both single-object and full-collection source loading MUST include the current opaque source version required for version-aware application.
- **FR-008**: When a linked reference stores the same current source version, synchronization MUST return unchanged without modifying business data, import or update timestamps, active state, or business events.
- **FR-009**: When the source version differs or was previously unknown, synchronization MUST validate and apply the current source state, store the current non-empty version, refresh the import timestamp, and return an applied outcome.
- **FR-010**: When a changed source version contains no changed WMS-relevant values, synchronization MUST still store the new version and import time and count the record as updated, but MUST emit business events only for business state that actually changed.
- **FR-011**: The first successful version-aware synchronization of a legacy linked record with no stored version MUST preserve its external identity, establish the current non-empty source version, and apply current source state.
- **FR-012**: A linked deletion-marked source reference MUST be deactivated if active, remain stored, and record the current source version; repeated inactive state MUST NOT emit another deactivation event.
- **FR-013**: An active current source reference linked to an inactive local record MUST be validated, updated, reactivated, and recorded as applied.
- **FR-014**: An unlinked deletion-marked source reference MUST be skipped without creating a local record, and deletion handling MUST occur before validation of reference details or SKU dependencies.
- **FR-015**: Only Warehouse and Stock Keeping Unit, whose sources expose folder records, MUST treat source folders as controlled skips that are not updated, unchanged, or failed records; this feature MUST NOT add folder support to Unit of Measure or any other reference type.
- **FR-016**: Synchronization MUST find linked records only by stable external identity and MUST NOT automatically link records by mutable business code.
- **FR-017**: Local edit operations MUST reject only an actual change to a source-owned value of a linked reference; they MUST permit requests that resubmit source-owned values unchanged while changing only explicitly WMS-owned fields, preserve existing behavior for unlinked records, and keep external import state inaccessible to normal local edit operations.
- **FR-018**: The system MUST provide an internal synchronize-one capability that accepts a supported reference type and external identity and returns an explicit applied, unchanged, controlled-skip, not-found, busy, or failure outcome.
- **FR-019**: An absent source object during synchronize-one MUST NOT create a local record or deactivate an existing linked record without an explicit current deletion mark.
- **FR-020**: Reactive and on-demand SKU synchronization MUST attempt to repair a missing or inactive base Unit of Measure by synchronizing no more than one Unit of Measure and applying the SKU no more than one additional time.
- **FR-021**: Failed SKU dependency repair MUST return an explicit failure and MUST NOT recurse, build a dependency chain, or perform additional repair or SKU retries.
- **FR-022**: Reactive processing MUST complete durable requests for applied, unchanged, applicable Warehouse or Stock Keeping Unit source-folder skip, and unlinked-deletion skip operation outcomes.
- **FR-023**: Reactive processing MUST classify temporary source failure, timeout, and same-type synchronization contention as retryable; it MUST classify object absence, malformed source data, validation failure, and unresolved business conflict as permanent.
- **FR-024**: Reference synchronization MUST be safe for repeated execution when WMS changes and durable request state do not complete atomically; retry MUST NOT duplicate mutations or business events.
- **FR-025**: Within one application instance, each reference type MUST have an independent coordination lease shared by manual, reactive, and on-demand synchronization. Manual full import MUST hold the lease from before its first source read until the whole operation completes, fails, or is cancelled, including every page and batch of an SKU import. Reactive and on-demand synchronization MUST hold it from before the single-object source read through the application commit. Different reference types MUST remain independently executable, and this feature MUST NOT introduce distributed or cross-process locking.
- **FR-026**: Manual import MUST retain its current fail-fast busy behavior; reactive work MUST return a retryable busy result; internal on-demand work MUST return an explicit busy result; all three MUST honor cancellation.
- **FR-027**: Existing manual import routes, operator authorization, source ordering and SKU paging, code-conflict handling, transaction and savepoint behavior, previously committed SKU batches, response fields, structured error shape, and returned-error limit MUST remain compatible.
- **FR-028**: Manual import results MUST add an unchanged count, and the operator-facing import view MUST display it alongside created, updated, skipped, and failed counts.
- **FR-029**: Existing Receiving and Shipping notification behavior MUST remain unchanged by this feature.
- **FR-030**: The feature MUST reuse the existing durable synchronization request intake, processing, retry, recovery, polling, and wake-up mechanisms rather than creating a parallel work lifecycle.
- **FR-031**: Automated tests added or changed for this feature MUST cover only behavior introduced or changed by Feature 109; they MUST NOT duplicate Feature 104 synchronization-foundation tests, MUST use representative or parameterized coverage for behavior shared by Warehouse, Unit of Measure, and Stock Keeping Unit, and MUST add type-specific tests only where mappings or domain rules materially differ.
- **FR-032**: Processor or application shutdown cancellation MUST propagate as cancellation and MUST NOT be classified as `Completed`, `TransientFailure`, `PermanentFailure`, or a new durable request status. Recovery MUST remain owned by Feature 104's existing abandoned-processing behavior. Manual import MUST preserve its existing caller-facing cancellation behavior, and internal on-demand synchronization MUST propagate cancellation to its caller.
- **FR-033**: Persistence changes MUST preserve all existing `ExternalRefKey` and `LastImportedAtUtc` values, add nullable `ExternalDataVersion` for the legacy unknown-version state, keep the column names `ExternalRefKey`, `ExternalDataVersion`, and `LastImportedAtUtc`, and preserve uniqueness enforcement for non-null external identities.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: External import state is an identity-less part of its owning reference aggregate and represents the single supported external-source link through external reference key, opaque source version, and last successful import time.
- **DR-002**: `ExternalDataVersion` is nullable only to represent the unknown-version state of a linked record that predates version-aware synchronization; an empty version is never a substitute for an unknown version, and a successful version-aware synchronization always leaves a non-empty version.
- **DR-003**: Source versions are opaque binary values: equality is determined by content only, no numeric or lexical ordering has business meaning, and stored versions MUST NOT be externally mutable through shared binary buffers.
- **DR-004**: Non-null external identities remain unique within each supported reference type and are the only automatic link between a source object and local aggregate.
- **DR-005**: For Warehouse, source-owned fields are code, name, source-controlled active state, and external import state; description is WMS-owned.
- **DR-006**: For Unit of Measure, source-owned fields are code, name, symbol, source-controlled active state, and external import state.
- **DR-007**: For Stock Keeping Unit, source-owned fields are code, name, base Unit of Measure, source-controlled active state, and external import state; description is WMS-owned.
- **DR-008**: Physical deletion is not a synchronization outcome; deletion marks drive deactivation of linked records or controlled skip of unlinked records.
- **DR-009**: The batch count invariant is `Processed = Created + Updated + Unchanged + Skipped + Failed`; unchanged is never classified as updated, skipped, or failed.
- **DR-010**: Applied, unchanged, controlled skip, not found, and busy are operation outcomes, not durable synchronization-request lifecycle states.
- **DR-011**: A reference synchronization result reflects the current source object observed during processing, even when the triggering notification identified an older source version.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: Machine-authenticated change notifications MUST be accepted at `POST /api/integrations/1c/warehouses/changed`, `POST /api/integrations/1c/uoms/changed`, and `POST /api/integrations/1c/skus/changed`.
- **CB-002**: Each reference notification body MUST contain `Ref_Key` and `DataVersion`; document-only diagnostics such as number and date MUST NOT be required.
- **CB-003**: A valid notification MUST receive an empty `202 Accepted` response only after durable insert or duplicate resolution commits; downstream processing remains asynchronous.
- **CB-004**: Durable synchronization requests MUST use stable internal entity types `Warehouse`, `UnitOfMeasure`, and `StockKeepingUnit`, independent of source transport naming.
- **CB-005**: The internal synchronize-one capability MUST remain available only to internal workflows; this feature MUST NOT add a public operator synchronize-one endpoint or WebApp operation.
- **CB-006**: Existing manual import contracts MUST keep every existing response field and add `Unchanged` as an additive count.
- **CB-007**: Source-specific entity-set names, property names, payload representations, and transport diagnostics MUST remain outside WMS domain contracts.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: Each failed single-reference synchronization MUST identify the reference type, external identity, failure category, and whether retry is appropriate without exposing credentials or unrelated source data.
- **OE-002**: Object absence, malformed response, temporary source failure, validation failure, business conflict, busy coordination, and cancellation MUST remain distinguishable to the caller or durable processor; operation outcomes MUST remain distinct from durable synchronization-request lifecycle statuses.
- **OE-003**: Reactive diagnostics MUST retain the notification identity and version while also indicating the current source outcome used for processing.
- **OE-004**: Applicable controlled Warehouse or Stock Keeping Unit folder skips and unlinked-deletion skips MUST be visible as successful operation outcomes without being reported as application failures.
- **OE-005**: Existing structured manual-import errors and their maximum returned-error limit MUST remain unchanged.

### Key Entities *(include if feature involves data)*

- **External Import State**: The source link owned by a Warehouse, Unit of Measure, or Stock Keeping Unit; records external identity, current opaque source version, and last successful import time.
- **Warehouse**: A WMS reference aggregate whose source-controlled code, name, and active state can be synchronized while its WMS description remains locally managed.
- **Unit of Measure**: A WMS reference aggregate whose code, name, symbol, and active state are controlled by the linked source.
- **Stock Keeping Unit**: A WMS reference aggregate whose source-controlled values include its base Unit of Measure relationship and whose description remains WMS-managed.
- **Synchronization Request**: The durable record of a source change notification, its processing lifecycle, attempts, retry scheduling, identity, and notified source version.
- **Reference Import Result**: Batch accounting across processed, created, updated, unchanged, skipped, and failed records, plus bounded structured errors.
- **Synchronize-One Outcome**: The internal result for one reference, distinguishing applied, unchanged, controlled skip, not found, busy, temporary failure, and permanent failure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For 100% of valid notifications across the three supported reference types, acceptance occurs only after exactly one durable request is inserted or an existing duplicate is resolved.
- **SC-002**: In repeated-delivery and interrupted-completion tests, 100% of references already storing the current source version complete without any duplicate data change, timestamp change, or business event.
- **SC-003**: For 100% of changed or legacy-unversioned linked references in acceptance tests, successful synchronization stores the current non-empty source version and produces the correct applied operation outcome.
- **SC-004**: For every eligible reactive request processed while the source is available and the per-reference-type coordination lease is free, an applied, unchanged, or controlled-skip operation outcome transitions the durable request to its existing completed lifecycle status during that same processing attempt.
- **SC-005**: Across all manual import acceptance cases, reported counts satisfy the processing invariant exactly, and a repeated unchanged import reports 100% of unchanged records in the unchanged count.
- **SC-006**: In all bounded SKU dependency-repair tests, no more than one Unit of Measure is synchronized and the SKU is applied no more than twice total, including the initial attempt.
- **SC-007**: Existing manual-import compatibility scenarios pass for all three reference types with zero removed routes, response fields, permissions, paging guarantees, or structured-error behavior.
- **SC-008**: Concurrent-operation tests show zero overlapping lease scopes for the same reference type, including all pages and batches of a manual SKU import, and allow all three different reference types to proceed independently within one application instance.
- **SC-009**: 100% of local-edit acceptance cases reject actual changes to linked source-owned values, permit unchanged resubmission of those values with changes only to explicitly WMS-owned fields, keep external import state inaccessible, and preserve existing unlinked-record behavior.

## Assumptions

- 1C remains the only external source represented by this feature, and it provides stable external identities, opaque non-empty versions for current objects, deletion marks, and current-object reads.
- The deployment model for this feature has one active application instance; cross-instance serialization is not promised.
- Existing manual full import remains the operator-facing operation for initial load, reconciliation, source-data correction, and operational repair.
- Source authentication, operator authorization, durable request lifecycle, retry scheduling, recovery, polling, and best-effort wake-up behavior established by the synchronization foundation remain available.
- Existing linked records may have no stored source version until their first successful version-aware synchronization.
- Manual full SKU import may continue to require Units of Measure to be imported first; per-SKU on-demand dependency repair applies only to reactive and synchronize-one flows.

## Dependencies

- Feature 104's machine authentication, durable synchronization requests, processor, retry and recovery policy, handler resolution, polling, and wake-up foundation.
- Existing full-import behavior and application rules for Warehouse, Unit of Measure, and Stock Keeping Unit.
- Existing local edit operations for the three reference aggregates, which must enforce source ownership after linking.
- Later Receiving and Shipping synchronization features depend on the internal synchronize-one capability but are not prerequisites for this feature.

## Out of Scope

- Receiving or Shipping document synchronization, snapshots, conflicts, demand, execution, or outbound updates.
- Additional master data such as partners, parties, characteristics, packaging, series, dimensions, volume, or weight.
- Multiple source providers, generalized external-link records, arbitrary reference types, metadata-driven mappings, or polymorphic synchronization frameworks.
- Recursive dependency resolution, dependency graphs, distributed or cross-process locking, or distributed transactions.
- Administrative replay UI, a public synchronize-one endpoint, or a new WebApp synchronize-one operation.
- Changes to existing Receiving and Shipping notification contracts.
- Unrelated WMS, integration, or infrastructure refactoring.
