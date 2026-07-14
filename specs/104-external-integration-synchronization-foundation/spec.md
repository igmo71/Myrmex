# Feature Specification: External Integration Synchronization Foundation

**Feature Branch**: `104-external-integration-synchronization-foundation`

**Created**: 2026-07-14

**Status**: Draft

**Input**: User description: `StakeholderDocs/104 Establish external integration synchronization foundation.md`

## Clarifications

### Session 2026-07-14

- Q: Does duplicate notification receipt change lifecycle state, schedule retry, or trigger replay? → A: No. The existing lifecycle state is preserved; only a duplicate of a `Pending` request may send a best-effort wake-up signal.
- Q: Must Issue #104 implement an operational replay mechanism for `Deferred` or `Failed` requests? → A: No. The foundation preserves replay information, but replay endpoints, UI, scheduled replay, and administrative commands are deferred to later synchronization features.
- Q: Does the first slice support one configured 1C infobase/API key or multiple simultaneously active source identities? → A: One configured 1C source instance and one active API key; `SourceInstance` remains server-assigned, persisted, and part of idempotency for future multi-instance support.
- Q: Is automatic retention cleanup required in Issue #104? → A: No. Completed, deferred, and failed requests are not automatically deleted; cleanup and archival are deferred until operational volume and support requirements are known.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Accept 1C Change Notifications Durably (Priority: P1)

A configured 1C infobase notifies Myrmex that a receiving or shipping document changed, and Myrmex durably records the notification before acknowledging it so later synchronization can process the document safely.

**Why this priority**: Durable, idempotent intake is the foundation for all later document synchronization. Without it, external changes can be lost or duplicated.

**Independent Test**: Submit valid receiving and shipping change notifications with the required machine credential and verify that each accepted notification is committed durably before the response, that duplicate notifications for the same external version are accepted without revealing whether they were new, and that malformed or unauthenticated requests are rejected.

**Acceptance Scenarios**:

1. **Given** the 1C machine identity is authorized and sends a receiving-order notification with `Ref_Key` and valid Base64 `DataVersion`, **When** Myrmex receives the notification, **Then** Myrmex commits a durable synchronization request and returns an empty `202 Accepted`.
2. **Given** the same source system, source instance, entity type, external id, and data version were already accepted, **When** the duplicate notification is submitted, **Then** Myrmex returns an empty `202 Accepted` without exposing whether the request already existed and without changing the existing request's lifecycle state.
3. **Given** the notification omits `Ref_Key` or `DataVersion`, **When** it is submitted, **Then** Myrmex rejects the request as a contract validation failure and does not return `202 Accepted`.
4. **Given** `DataVersion` is not valid Base64, **When** the notification is submitted, **Then** Myrmex rejects the request as a contract validation failure and does not create a synchronization request.
5. **Given** optional diagnostic `Number` or `Date` values are present, **When** the notification is accepted, **Then** Myrmex records them only as diagnostics and does not treat `Date` as an authoritative UTC timestamp.

---

### User Story 2 - Preserve Authentication Boundaries (Priority: P1)

External 1C notifications authenticate as a machine identity while existing user-operated 1C administration endpoints continue to require WMS operator authorization through the current user-session boundary.

**Why this priority**: Machine-to-machine notification intake must not weaken the existing Identity cookie boundary or require fake user claims for a non-user caller.

**Independent Test**: Call notification endpoints with and without `Authorization: ApiKey <secret>`, call existing administrative 1C endpoints with operator and machine credentials, and verify that each route uses only its intended authorization boundary.

**Acceptance Scenarios**:

1. **Given** a request to a change-notification endpoint includes a valid `Authorization: ApiKey <secret>` credential, **When** the request contract is valid, **Then** Myrmex authorizes it through the `Myrmex.IntegrationApiKey` scheme and `MyrmexAuthorizationPolicies.OneCIntegration` policy.
2. **Given** a change-notification request lacks a valid API key, **When** it is submitted, **Then** Myrmex rejects it before persisting a synchronization request.
3. **Given** the 1C machine principal has no Identity user id or WMS role, **When** it calls a notification endpoint with a valid API key, **Then** the request can be authorized without requiring Identity roles or a GUID user identifier.
4. **Given** a caller uses the 1C machine API key against existing connection-test or manual import endpoints, **When** the endpoint requires a WMS operator, **Then** Myrmex rejects the call.
5. **Given** an authorized WMS operator calls existing 1C connection-test or manual import endpoints, **When** those requests are valid, **Then** their current authorization behavior is preserved.

---

### User Story 3 - Process Synchronization Requests Safely (Priority: P2)

The synchronization processor finds eligible durable requests, claims them safely, routes them to registered document handlers when available, and records clear lifecycle outcomes.

**Why this priority**: Intake alone is not enough; the foundation must provide a replayable processing lifecycle that later document-specific features can build on.

**Independent Test**: Create eligible, duplicate, abandoned, unsupported, transiently failing, permanently failing, and successfully handled requests; run processing from one and multiple application instances; verify exactly one processor claims each request at a time and status transitions are durable and replayable.

**Acceptance Scenarios**:

1. **Given** a `Pending` synchronization request is due for processing, **When** a processor claims it, **Then** the request enters `Processing` and no other processor instance can process the same request concurrently.
2. **Given** a registered handler completes a claimed request successfully, **When** processing finishes, **Then** the request becomes `Completed` with completion time recorded.
3. **Given** no document-specific handler is registered for the request entity type, **When** the processor evaluates it, **Then** the request becomes `Deferred` and remains replayable rather than being marked `Completed`.
4. **Given** a receiving or shipping notification reaches the first slice before a document-specific synchronization handler exists, **When** the processor selects it, **Then** the request MUST NOT become `Completed` merely because the processor stub recognized the entity type.
5. **Given** a claimed request remains in `Processing` longer than the configured processing timeout, **When** recovery runs, **Then** the request becomes eligible for another safe claim without duplicate concurrent processing.

---

### User Story 4 - Retry and Recover Predictably (Priority: P3)

Operators and support personnel can rely on configured retry timing, bounded failures, startup scanning, and fallback polling so accepted notifications are not stranded by transient failures or missed in-process wake-up signals.

**Why this priority**: External synchronization must be operationally resilient across process restarts, transient errors, and multi-instance deployments.

**Independent Test**: Simulate transient processing failures, permanent validation failures, process restart, missed wake-up signals, and multiple running instances; verify retry scheduling, terminal failure, startup scan, fallback polling, and safe claim behavior.

**Acceptance Scenarios**:

1. **Given** a transient processing failure occurs, **When** retries remain, **Then** Myrmex increments the attempt count, records the error, schedules the next attempt using the configured delay sequence, and does not mark the request terminally failed.
2. **Given** the configured retry sequence is exhausted, **When** another transient processing failure occurs, **Then** Myrmex marks the request `Failed` with the last error retained.
3. **Given** a permanent validation or processing error occurs, **When** the processor detects it, **Then** Myrmex marks the request `Failed` without retrying as though it were transient.
4. **Given** a notification is committed but the in-process wake-up signal is missed, **When** startup scanning or fallback polling runs, **Then** the committed request is still discovered and processed.
5. **Given** multiple application instances are running, **When** eligible requests exist, **Then** instances may divide work but MUST NOT process the same request concurrently.

### Edge Cases

- A valid request is accepted immediately before the application stops; it is still discoverable after restart.
- The in-process wake-up signal fails, is dropped, or happens after the HTTP response; durable polling still recovers the request.
- A duplicate notification arrives while the original version is `Pending`, `Processing`, `Deferred`, `Completed`, or `Failed`; the caller still receives empty `202 Accepted`, does not learn the internal state, and the existing lifecycle state remains unchanged.
- A duplicate notification arrives while the original version is `Pending`; Myrmex may send a best-effort wake-up signal, but it does not schedule a retry, reset attempts, or act as replay or repair.
- A duplicate notification arrives while the original version is `Processing`, `Deferred`, `Completed`, or `Failed`; Myrmex does not change status, retry timing, attempt count, processing timestamps, completion time, or last error.
- Two instances receive the same external version at nearly the same time; at most one durable request exists for that source/version key.
- `Date` is present without a source offset; it is stored only as external diagnostic data and is not used for ordering, retry timing, or synchronization freshness.
- A request is `Processing` when its owning process crashes; recovery makes it eligible after the configured processing timeout.
- The retry delay collection is empty or shorter than expected; behavior must remain deterministic according to configuration validation or documented defaults.
- Unsupported entity types are deferred and replayable; they are not silently discarded or treated as successful synchronization.
- Malformed contracts, invalid credentials, and authorization failures never create synchronization requests and never return `202 Accepted`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose receiving and shipping change-notification actions at `POST /api/integrations/1c/receiving-orders/changed` and `POST /api/integrations/1c/shipping-orders/changed`.
- **FR-002**: Each notification request MUST use the exact JSON field names `Ref_Key`, `DataVersion`, `Number`, and `Date`, where `Ref_Key` and `DataVersion` are required and `Number` and `Date` are optional diagnostics.
- **FR-003**: The system MUST validate `DataVersion` as Base64 and persist the decoded binary value for accepted notifications.
- **FR-004**: The system MUST treat `DataVersion` as a source version marker for notification idempotency and version tracking only; it MUST NOT claim or require optimistic concurrency behavior through source `If-Match` semantics.
- **FR-005**: The system MUST return an empty `202 Accepted` only after the synchronization request has been durably committed.
- **FR-006**: Duplicate notification of the same source system, source instance, entity type, external id, and external data version MUST return an empty `202 Accepted` without revealing whether the notification was newly recorded or already existed.
- **FR-007**: Authentication failures, authorization failures, missing required fields, invalid Base64 `DataVersion`, and other malformed-contract failures MUST NOT return `202 Accepted` and MUST NOT create synchronization requests.
- **FR-008**: Accepted receiving notifications MUST use stable internal entity type `ReceivingOrder`; accepted shipping notifications MUST use stable internal entity type `ShippingOrder`.
- **FR-009**: The system MUST NOT persist source transport collection names as canonical synchronization entity types.
- **FR-010**: The synchronization request MUST record source system, source instance, entity type, external id, external data version, optional external document number, optional external document date, trigger, lifecycle status, received time, processing start time when claimed, completion time when finished, attempt count, next attempt time, last error, and an application-managed concurrency value.
- **FR-011**: The system MUST enforce idempotent uniqueness across source system, source instance, entity type, external id, and external data version.
- **FR-012**: The first slice MUST support one configured 1C source instance and one active API key at a time.
- **FR-013**: The generic synchronization foundation MUST be owned by the integration capability and MUST NOT place queue ownership or processing infrastructure inside the WMS domain.
- **FR-014**: 1C-specific authentication, notification contracts, endpoint paths, and transport details MUST remain inside the 1C integration boundary.
- **FR-015**: The system MUST keep the synchronization request as a technical persistence record, not a WMS aggregate root.
- **FR-016**: The system MUST NOT introduce a generalized external-link entity, universal ERP framework, or WMS-wide ERP abstraction as part of this feature.
- **FR-017**: Endpoint processing order MUST be: validate the contract and caller, persist the synchronization request, commit it durably, send a best-effort in-process wake-up signal, and then return an empty `202 Accepted`.
- **FR-018**: The in-process wake-up signal MUST NOT be the reliability boundary; accepted requests MUST remain discoverable through durable storage scanning and polling.
- **FR-019**: The system MUST scan for eligible requests when the application starts.
- **FR-020**: The system MUST support configurable fallback polling interval, processing batch size, request timeout, processing timeout, and explicit retry delays in seconds.
- **FR-021**: The default fallback polling interval SHOULD be 60 seconds unless deployment configuration chooses another value.
- **FR-022**: The final number of retry attempts SHOULD be derived from the configured retry-delay collection so retry count and retry timing cannot contradict each other.
- **FR-023**: The processor MUST process available records in configurable batches.
- **FR-024**: The processor MUST recover abandoned `Processing` records after the configured processing timeout.
- **FR-025**: The processor MUST support safe claims when multiple application instances are running so the same request is not processed concurrently.
- **FR-026**: Claim and concurrency behavior MUST be provider-neutral from an observable behavior perspective and MUST use an application-managed concurrency value rather than a database-engine-specific row version.
- **FR-027**: The processor MUST distinguish transient technical failures, permanent validation or processing failures, and unsupported-handler outcomes.
- **FR-028**: The processor MUST retain enough last-error information for support diagnosis without exposing secrets in caller-facing responses.
- **FR-029**: The foundation MUST preserve all information needed for later controlled replay of `Deferred` and `Failed` requests, but Issue #104 MUST NOT provide a replay endpoint, UI action, scheduled replay, or administrative replay command.
- **FR-030**: The foundation MUST NOT automatically delete `Completed`, `Deferred`, or `Failed` synchronization requests in Issue #104; retention cleanup and archival are deferred until operational volume and support requirements are known.
- **FR-031**: Duplicate notification receipt MUST preserve the existing request lifecycle state: `Pending` remains `Pending`, `Processing` remains `Processing`, `Deferred` remains `Deferred`, `Completed` remains `Completed`, and `Failed` remains `Failed`.
- **FR-032**: Duplicate notification receipt MUST NOT act as an implicit replay, repair, retry scheduling, attempt reset, error reset, processing restart, or status transition command. A duplicate of a `Pending` request MAY send only a best-effort wake-up signal.
- **FR-033**: `SourceInstance` MUST be assigned by Myrmex, persisted on accepted synchronization requests, and included in the idempotency key even while only one configured source instance is active.
- **FR-034**: The persistence model MUST NOT prevent future support for multiple source instances, but simultaneous multi-infobase management, multiple active API keys, and key rotation workflows are outside this feature.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: A synchronization request lifecycle MUST use `Pending`, `Processing`, `Deferred`, `Completed`, and `Failed` as the first-slice states.
- **DR-002**: `Pending` means the request is eligible for processing now or after its next-attempt time.
- **DR-003**: `Processing` means one processor instance has claimed the request.
- **DR-004**: `Deferred` means the infrastructure processed the request but no document-specific handler is registered; the request remains replayable.
- **DR-005**: `Completed` means a registered handler completed successfully.
- **DR-006**: `Failed` means the request reached a terminal technical failure after configured retries or encountered a permanent contract or processing error.
- **DR-007**: `Superseded` is reserved for possible later extension and MUST NOT be required in the first slice.
- **DR-008**: Receiving and shipping notifications MUST NOT be marked `Completed` unless a registered document-specific handler completes successfully.
- **DR-009**: `SourceSystem`, `SourceInstance`, `EntityType`, `ExternalId`, and `ExternalDataVersion` together identify one external version notification for idempotency.
- **DR-010**: Retry scheduling MUST never make a terminally `Failed` request eligible unless a later replay or repair capability explicitly changes that state.
- **DR-011**: `Deferred` requests MUST remain available for later replay when the relevant document-specific handler is introduced, but Issue #104 does not itself initiate that replay.
- **DR-012**: Duplicate notification intake is idempotent and side-effect limited; it cannot promote, demote, retry, replay, repair, or otherwise mutate an existing non-`Pending` lifecycle state.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: Change-notification endpoints MUST authenticate with named scheme `Myrmex.IntegrationApiKey` using request header `Authorization: ApiKey <secret>`.
- **CB-002**: Change-notification endpoints MUST require dedicated authorization policy `MyrmexAuthorizationPolicies.OneCIntegration`.
- **CB-003**: The external notification caller MUST be treated as a machine identity, not a Myrmex user-session identity.
- **CB-004**: The 1C machine principal MUST NOT require Identity roles or a GUID `NameIdentifier`.
- **CB-005**: The existing Identity API-session cookie MUST remain the default authentication scheme for existing user-operated API behavior.
- **CB-006**: Existing 1C connection-test and manual import endpoints MUST remain protected by WMS operator authorization and MUST NOT move to API-key authentication.
- **CB-007**: Route protection MUST be split so current connection-test and manual import actions use WMS operator authorization, while receiving and shipping change-notification actions use the 1C integration policy.
- **CB-008**: Change-notification success responses MUST be empty `202 Accepted` responses; they MUST NOT include synchronization ids, queue status, or duplicate/new indicators.
- **CB-009**: Malformed-contract failures SHOULD use the repository's normal problem response conventions and MUST be distinguishable from authentication and authorization failures.
- **CB-010**: The foundation MUST NOT expose OData document loading, document status mapping, quantity synchronization, outbound patching, or UI administration behavior in this feature.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: The system MUST produce diagnostics for accepted notification intake, duplicate detection, validation rejection, authorization rejection, processing claims, status transitions, retries, deferred outcomes, failures, abandoned-processing recovery, and polling activity.
- **OE-002**: Diagnostics MUST include stable request identifiers or correlation values, source system, source instance, entity type, external id, status, attempt count, and failure category where available.
- **OE-003**: Diagnostics MUST NOT expose API-key secrets or other protected credentials.
- **OE-004**: Contract validation failures MUST clearly identify invalid or missing contract fields without leaking sensitive configuration.
- **OE-005**: Retry diagnostics MUST make the next attempt time and terminal failure reason visible to support personnel.
- **OE-006**: Multi-instance claim conflicts MUST be treated as normal coordination outcomes, not as externally visible failures.

### Key Entities *(include if feature involves data)*

- **Integration Synchronization Request**: A provider-neutral technical record representing one accepted external change notification version. It contains identity fields, external diagnostic values, trigger, lifecycle state, processing timing, retry data, last error, and an application-managed concurrency value.
- **Source System**: The external provider family that produced the notification, such as 1C.
- **Source Instance**: A specific provider instance or infobase within a source system. It participates in idempotency so multiple instances can be supported.
- **Entity Type**: A stable internal document category such as `ReceivingOrder` or `ShippingOrder`; source collection names are not canonical entity types.
- **External Data Version**: The decoded version marker supplied by the source and used with source identity fields to identify duplicate notifications.
- **Processing Status**: The current lifecycle state of a synchronization request: `Pending`, `Processing`, `Deferred`, `Completed`, or `Failed`.
- **Retry Policy**: Deployment-configurable polling, batch, timeout, and retry-delay settings that determine when eligible requests are scanned and retried.

### Scope Boundaries

- This feature includes notification intake, machine authentication boundary, durable idempotent request storage, processing lifecycle states, configurable polling and retry behavior, abandoned-processing recovery, and multi-instance safe claiming.
- This feature excludes OData GET of receiving or shipping documents.
- This feature excludes receiving and shipping domain entities.
- This feature excludes document status mapping.
- This feature excludes period-based document loading.
- This feature excludes reference-data dependency repair.
- This feature excludes outbound `PATCH`.
- This feature excludes actual quantity synchronization.
- This feature excludes RabbitMQ or any other message broker.
- This feature excludes an Outbox for outbound operations.
- This feature excludes UI or administration pages.
- This feature excludes replay endpoints, replay UI, scheduled replay, and administrative replay commands for `Deferred` or `Failed` requests.
- This feature excludes simultaneous multi-infobase configuration, multiple active integration API keys, and key-rotation management.
- This feature excludes automatic retention cleanup, archival, or deletion workers for completed, deferred, or failed synchronization requests.
- This feature excludes a generalized ERP integration framework.
- This feature excludes changing existing administrative 1C endpoints to API-key authentication.
- This feature excludes moving integration queue entities or processing infrastructure into the WMS domain.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of valid receiving and shipping notifications submitted with a valid machine credential receive an empty `202 Accepted` only after durable commit.
- **SC-002**: 100% of duplicate notifications for the same source system, source instance, entity type, external id, and data version receive the same empty `202 Accepted` response as the original accepted notification.
- **SC-003**: 100% of authentication failures, authorization failures, missing required fields, and invalid Base64 `DataVersion` cases are rejected without creating a synchronization request.
- **SC-004**: In a two-instance acceptance test, no synchronization request is processed concurrently by more than one processor instance across at least 1,000 eligible requests.
- **SC-005**: After an application restart with accepted unprocessed notifications, 100% of eligible committed requests are discovered by startup scanning or fallback polling.
- **SC-006**: Transient processing failures follow the configured retry delays within a 5-second scheduling tolerance in acceptance testing.
- **SC-007**: Requests left in `Processing` beyond the configured processing timeout become eligible for safe recovery in 100% of tested crash or abandonment scenarios.
- **SC-008**: Unsupported receiving or shipping requests are recorded as `Deferred`, remain replayable, and are never counted as `Completed` without a registered handler.
- **SC-009**: Operational diagnostics allow support personnel to identify intake, duplicate, deferred, retrying, recovered, completed, and failed outcomes for 100% of sampled synchronization requests without exposing secrets.
- **SC-010**: Duplicate notification tests covering `Pending`, `Processing`, `Deferred`, `Completed`, and `Failed` existing requests preserve status, attempt count, retry timing, processing timestamps, completion time, and last error in 100% of cases.

## Assumptions

- The stakeholder document's accepted decisions are authoritative for the first synchronization-foundation slice.
- The first source system is 1C, and the first source instance represents the current configured infobase, while the model remains source-instance aware for later expansion.
- The source guarantees `DataVersion` for changed-document notifications.
- 1C invokes the new endpoints from a save subscription and the notification means only that the external object changed; it does not classify posting, unposting, deletion, or status transition.
- The durable synchronization store uses the existing Myrmex deployment database boundary while remaining owned by the integration capability.
- The in-process wake-up channel is an optimization only; durable polling is the reliability mechanism.
- Future document-specific synchronization features will supply handlers for receiving and shipping documents.
- Migration generation, database updates, production code, tests, and application startup are outside this specification command.
