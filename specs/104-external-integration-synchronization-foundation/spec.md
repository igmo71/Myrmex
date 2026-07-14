# Feature Specification: External Integration Synchronization Foundation

**Feature Branch**: `104-external-integration-synchronization-foundation`

**Created**: 2026-07-14

**Status**: Draft

**Input**: User description: "StakeholderDocs/104 Establish external integration synchronization foundation.md"

## Clarifications

### Session 2026-07-14

- Q: Does duplicate notification receipt change lifecycle state, schedule retry, or trigger replay? -> A: No. The existing lifecycle state is preserved; only a duplicate of a `Pending` request may send a best-effort wake-up signal.
- Q: Must Issue #104 implement an operational replay mechanism for `Deferred` or `Failed` requests? -> A: No. The foundation preserves replay information, but replay endpoints, UI, scheduled replay, and administrative commands are deferred to later synchronization features.
- Q: Does the first slice support one configured 1C infobase/API key or multiple simultaneously active source identities? -> A: One configured 1C source instance and one active API key; `SourceInstance` remains server-assigned, persisted, and part of idempotency for future multi-instance support.
- Q: Is automatic retention cleanup required in Issue #104? -> A: No. Completed, deferred, and failed requests are not automatically deleted; cleanup and archival are deferred until operational volume and support requirements are known.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Accept 1C Change Notifications Durably (Priority: P1)

A configured 1C source notifies Myrmex when a receiving order or shipping order changes. Myrmex authenticates the machine caller, validates the notification contract, records the change as a durable synchronization request, and returns acceptance only after the request is safely recorded.

**Why this priority**: Change notification intake is the first business-visible capability. Without a reliable accepted notification record, later synchronization handlers cannot safely load or replay external document changes.

**Independent Test**: Configure one valid 1C integration identity and submit receiving-order and shipping-order change notifications with required fields; verify valid notifications receive empty `202 Accepted` responses only after a durable synchronization request exists, while unauthenticated and malformed requests are rejected.

**Acceptance Scenarios**:

1. **Given** a configured 1C source instance and a valid integration API key, **When** 1C posts a receiving-order change notification with `Ref_Key` and valid Base64 `DataVersion`, **Then** Myrmex records one synchronization request for that external receiving order version and returns an empty `202 Accepted`.
2. **Given** a configured 1C source instance and a valid integration API key, **When** 1C posts a shipping-order change notification with optional `Number` and `Date`, **Then** Myrmex records the optional diagnostic values without treating the source date as an authoritative UTC timestamp.
3. **Given** a missing, invalid, or non-integration credential, **When** a notification endpoint is called, **Then** Myrmex rejects the request and records no synchronization request.
4. **Given** a malformed notification body, missing `Ref_Key`, missing `DataVersion`, or invalid Base64 `DataVersion`, **When** the notification endpoint is called, **Then** Myrmex rejects the request as a contract validation failure and records no synchronization request.
5. **Given** an accepted notification, **When** Myrmex resolves source identity, **Then** `SourceSystem` and `SourceInstance` are assigned from server-side integration configuration and are not accepted from the request body.

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

### User Story 3 - Preserve Idempotent Synchronization Request Lifecycle (Priority: P1)

Warehouse operators and support staff need external document changes to be tracked once per external source version, even if 1C delivers the same notification more than once. Myrmex must preserve the existing request lifecycle state on duplicates instead of treating duplicate delivery as replay, repair, or restart.

**Why this priority**: External systems commonly retry notifications. Idempotent intake prevents duplicate processing, accidental retry resets, and misleading lifecycle changes.

**Independent Test**: Submit the same notification more than once for the same configured source instance, entity type, external ID, and data version; verify each call returns empty `202 Accepted`, only one synchronization request identity is represented, and the existing lifecycle state, attempt count, retry schedule, and error information are not changed by the duplicate.

**Acceptance Scenarios**:

1. **Given** an existing pending synchronization request, **When** the same notification version is received again, **Then** Myrmex returns an empty `202 Accepted`, preserves the existing request state, and may only issue a best-effort wake-up signal.
2. **Given** an existing processing, deferred, completed, or failed synchronization request, **When** the same notification version is received again, **Then** Myrmex returns an empty `202 Accepted` without resetting attempts, clearing errors, changing status, scheduling retry, or restarting processing.
3. **Given** two notifications for the same external document but different data versions, **When** both are accepted, **Then** Myrmex tracks them as distinct synchronization requests.
4. **Given** the same external document and data version from different configured source instances in a future-compatible deployment, **When** both are accepted, **Then** their source instances are part of the idempotency identity and the requests remain distinct.

---

### User Story 4 - Operate a Recoverable Synchronization Queue (Priority: P2)

The integration foundation processes accepted synchronization requests through a recoverable lifecycle. It wakes promptly after new work arrives, also scans for durable work on startup and on a fallback interval, retries transient technical failures according to configuration, and recovers work abandoned by application failure or restart.

**Why this priority**: Durable intake alone leaves notifications stranded. A clear lifecycle and recovery model lets future document-specific handlers be added without changing the intake contract.

**Independent Test**: Accept notifications with and without a registered document handler, simulate transient and permanent processing outcomes, restart while work is marked in progress, and verify state transitions, retry timing, and recovery behavior match the defined lifecycle.

**Acceptance Scenarios**:

1. **Given** accepted work is pending, **When** the synchronization processor starts or wakes, **Then** it selects eligible requests in configurable batches and marks only actively handled work as processing.
2. **Given** no document-specific handler is registered for a supported entity type, **When** the processor evaluates the request, **Then** the request becomes `Deferred` and remains available for later replay rather than being marked `Completed`.
3. **Given** a registered handler completes successfully, **When** the request finishes, **Then** the request becomes `Completed` and records completion time.
4. **Given** a transient technical failure, **When** retries remain, **Then** the request records the attempt, keeps the last error, and becomes eligible again only after the next configured retry delay.
5. **Given** retries are exhausted or a permanent processing error occurs, **When** the request is evaluated, **Then** the request becomes `Failed` with enough diagnostic information to support troubleshooting.
6. **Given** a request was left `Processing` after application failure, **When** the processing timeout has elapsed, **Then** Myrmex recovers the abandoned request so it can be processed again according to retry rules.

### Edge Cases

- Duplicate receipt of a completed, deferred, failed, or currently processing request must not be treated as an administrative replay or repair command.
- The wake-up signal can be lost or arrive after the HTTP response; durable polling and startup scanning must still discover eligible work.
- The external `Date` field lacks a source offset and must not be converted into an authoritative instant for ordering or concurrency decisions.
- `DataVersion` is guaranteed by 1C but invalid Base64 in a request is still a contract failure.
- Notifications classify only that an external object changed; they do not identify posting, unposting, deletion, status transition, or business meaning.
- The first slice supports exactly one configured 1C source instance and one active API key; requests must not expose multi-key or key-rotation behavior.
- The current connection-test and manual import endpoints must remain operator-administered and must not silently become externally callable notification endpoints.
- A notification accepted immediately before process shutdown must remain discoverable from durable storage after restart even if no wake-up signal was processed.
- Two application instances receive the same external version at nearly the same time; at most one durable request exists for that source/version key, and the same request is not processed concurrently.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose receiving-order and shipping-order change notification intake for a configured 1C source.
- **FR-002**: The system MUST accept notifications only from a machine integration identity authenticated with `Authorization: ApiKey <secret>`.
- **FR-003**: The system MUST preserve the existing Identity API-session cookie as the default authentication scheme for current protected application-service behavior.
- **FR-004**: The system MUST authorize notification intake through a dedicated `MyrmexAuthorizationPolicies.OneCIntegration` policy that does not require Identity roles or a GUID user `NameIdentifier`.
- **FR-005**: Existing 1C connection-test and manual import operations MUST remain protected by the WMS operator policy and MUST NOT be changed to integration API-key authentication.
- **FR-006**: The system MUST resolve `SourceSystem` and `SourceInstance` from server-side integration identity configuration and MUST NOT accept either value from the notification body.
- **FR-007**: The first slice MUST support one configured 1C source instance and one active integration API key.
- **FR-008**: The receiving-order notification endpoint MUST be `POST /api/integrations/1c/receiving-orders/changed`.
- **FR-009**: The shipping-order notification endpoint MUST be `POST /api/integrations/1c/shipping-orders/changed`.
- **FR-010**: Notification request bodies MUST use the exact JSON field names `Ref_Key`, `DataVersion`, `Number`, and `Date`.
- **FR-011**: `Ref_Key` and `DataVersion` MUST be required; `Number` and `Date` MUST be optional diagnostic fields.
- **FR-012**: The system MUST decode valid Base64 `DataVersion` values into binary version data for persistence.
- **FR-013**: Invalid Base64 `DataVersion`, missing required fields, or malformed notification bodies MUST be contract validation failures and MUST NOT return `202 Accepted`.
- **FR-014**: The system MUST return an empty `202 Accepted` only after the synchronization request has been durably committed.
- **FR-015**: Duplicate notification delivery for the same source system, source instance, entity type, external ID, and external data version MUST return an empty `202 Accepted` without disclosing whether the request was newly inserted or already existed.
- **FR-016**: Duplicate receipt MUST preserve the existing synchronization request lifecycle state, attempt count, retry schedule, and last error.
- **FR-017**: Duplicate receipt of a pending request MAY issue only a best-effort wake-up signal and MUST NOT otherwise alter the request.
- **FR-018**: Accepted notifications MUST be recorded as provider-neutral synchronization requests, not as WMS domain aggregate roots.
- **FR-019**: The synchronization foundation MUST remain owned by the integration capability, while 1C-specific authentication, notification contracts, endpoint paths, and transport details remain under the 1C integration boundary.
- **FR-020**: Synchronization request persistence MUST be separate from WMS persistence ownership and MUST NOT require WMS context changes to own the queue.
- **FR-021**: The system MUST use stable internal entity type values `ReceivingOrder` and `ShippingOrder` and MUST NOT persist OData entity-set names as canonical entity types.
- **FR-022**: The system MUST track synchronization requests through the states `Pending`, `Processing`, `Deferred`, `Completed`, and `Failed`.
- **FR-023**: `Pending` requests MUST be eligible for processing now or after `NextAttemptAtUtc`.
- **FR-024**: `Processing` requests MUST represent work currently being processed by the synchronization processor.
- **FR-025**: `Deferred` requests MUST represent requests processed by the infrastructure for which no document-specific handler is registered, and they MUST remain replayable for future controlled handling.
- **FR-026**: `Completed` requests MUST represent requests for which a registered handler completed successfully.
- **FR-027**: `Failed` requests MUST represent terminal technical failure after configured retries or a permanent contract or processing error.
- **FR-028**: The first slice MUST NOT mark receiving or shipping notifications `Completed` merely because a processor stub selected them.
- **FR-029**: The processor MUST scan for eligible work immediately on application startup and on a configurable fallback polling interval.
- **FR-030**: The processor MUST process eligible work in configurable batches.
- **FR-031**: The processor MUST recover abandoned `Processing` requests after a configurable processing timeout.
- **FR-032**: Retry behavior MUST be configurable through explicit retry delays, and the final number of attempts MAY be derived from the configured delay collection.
- **FR-033**: The system MUST distinguish transient technical failures from permanent validation failures and unsupported-handler outcomes.
- **FR-034**: The feature MUST preserve enough synchronization request information for later controlled replay, while not implementing replay in this slice.
- **FR-035**: The processor MUST support safe claims when multiple application instances are running so the same request is not processed concurrently.
- **FR-036**: The first slice MUST NOT automatically delete, archive, or clean up completed, deferred, or failed synchronization requests.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: `IntegrationSynchronizationRequest` is a technical integration persistence entity and is not a WMS aggregate root.
- **DR-002**: A synchronization request is uniquely identified by `SourceSystem`, `SourceInstance`, `EntityType`, `ExternalId`, and `ExternalDataVersion`.
- **DR-003**: `SourceInstance` is assigned by Myrmex from server-side configuration, persisted on every accepted synchronization request, and included in the idempotency identity.
- **DR-004**: `SourceInstance` is never accepted from the 1C notification body.
- **DR-005**: `DataVersion` supports notification idempotency and version tracking only; it MUST NOT be described as optimistic concurrency support for outbound 1C updates.
- **DR-006**: `Date` from the notification is diagnostic source data only and MUST NOT be treated as an authoritative UTC timestamp.
- **DR-007**: A notification means only that the external object changed; it does not classify business status, deletion, posting, unposting, or warehouse effects.
- **DR-008**: `Superseded` is reserved as a possible future lifecycle extension and is not required for the first slice.
- **DR-009**: The synchronization foundation must not introduce a generalized external-link entity, universal ERP framework, or WMS-owned integration queue.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: The public receiving-order notification contract MUST accept this shape: `{ "Ref_Key": "...", "DataVersion": "...", "Number": "...", "Date": "..." }`, with only `Ref_Key` and `DataVersion` required.
- **CB-002**: The public shipping-order notification contract MUST use the same field names and required/optional rules as the receiving-order notification contract.
- **CB-003**: Notification endpoints MUST produce no response body for accepted new or duplicate notifications.
- **CB-004**: Authentication failures, authorization failures, and malformed-contract failures MUST NOT return `202 Accepted`.
- **CB-005**: The named authentication scheme for external notification intake MUST be `Myrmex.IntegrationApiKey`.
- **CB-006**: The integration machine principal MUST be independent from ASP.NET Identity users and MUST NOT require Identity roles or user actor identifiers.
- **CB-007**: Routing MUST keep current `/api/integrations/1c/connection/test`, `/api/integrations/1c/warehouses/import`, `/api/integrations/1c/uoms/import`, and `/api/integrations/1c/skus/import` operations under WMS operator authorization.
- **CB-008**: The integration capability MUST own generic synchronization-request behavior; 1C-specific endpoint paths, request contracts, authentication configuration, and transport details MUST remain under the 1C integration boundary.
- **CB-009**: WMS domain contracts MUST NOT be expanded to include the synchronization queue, receiving documents, shipping documents, or external-link abstractions as part of this slice.
- **CB-010**: The durable synchronization request MUST conceptually contain `Id`, `SourceSystem`, `SourceInstance`, `EntityType`, `ExternalId`, `ExternalDataVersion`, optional external document number, optional external document date, trigger, status, received time, optional processing start time, optional completion time, attempt count, optional next attempt time, and optional last error.
- **CB-011**: The request intake order MUST be validate, persist, commit, best-effort wake-up signal, then empty `202 Accepted`.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: Contract validation failures MUST identify the invalid or missing notification field without exposing configured API keys or other secrets.
- **OE-002**: Authentication and authorization failures MUST be distinguishable from malformed-contract failures and from accepted duplicate notifications.
- **OE-003**: Accepted synchronization requests MUST retain enough source, entity, version, timing, lifecycle, attempt, retry, and last-error data for operators to troubleshoot later processing behavior.
- **OE-004**: Processor failures MUST record whether the outcome was transient, permanent, unsupported-handler, or retry-exhausted.
- **OE-005**: Startup scanning, fallback polling, wake-up signaling, abandoned-work recovery, and retry exhaustion MUST provide diagnostics sufficient to investigate stuck or repeatedly failing synchronization requests.
- **OE-006**: Diagnostics MUST NOT log integration API keys, external credentials, or other secret material.

### Scope Boundaries

- OData loading of receiving or shipping documents is out of scope.
- Receiving and shipping domain entities are out of scope.
- Document status mapping, posting, unposting, deletion handling, and business state transitions are out of scope.
- Period-based document loading is out of scope.
- Reference-data dependency repair is out of scope.
- Outbound `PATCH`, actual quantity synchronization, and optimistic concurrency claims through `If-Match` are out of scope.
- RabbitMQ and outbound Outbox behavior are out of scope.
- UI, administration pages, replay endpoints, replay UI, scheduled replay, and administrative replay commands are out of scope.
- Automatic cleanup, archival, or deletion of completed, deferred, or failed synchronization requests is out of scope.
- Simultaneous multi-infobase configuration, multiple active integration API keys, and key-rotation workflows are out of scope.
- A generalized ERP integration framework, universal external-link model, and WMS-owned integration queue are out of scope.

### Key Entities *(include if feature involves data)*

- **Integration Source Identity**: A configured machine identity representing the one active 1C source instance allowed to send notifications in the first slice; includes source system, source instance, and active API-key authorization material without exposing secrets.
- **1C Change Notification**: A public contract sent by 1C when an external receiving or shipping object changes; contains external reference key, guaranteed source data version, and optional diagnostic document number and source date.
- **Integration Synchronization Request**: A durable provider-neutral technical record of one external entity version that needs synchronization processing; includes source identity, entity type, external identity, version, diagnostic document values, trigger, lifecycle state, timestamps, attempts, retry timing, and last error.
- **Synchronization Processor**: The operational worker that finds eligible synchronization requests, transitions lifecycle state, invokes registered document-specific handlers when available, defers unsupported work, retries transient failures, and recovers abandoned processing.
- **Retry Schedule**: Configured explicit retry delays that determine when transiently failed requests become eligible again and how many attempts are available before terminal failure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of valid receiving-order and shipping-order notifications submitted with the active integration API key are durably represented as synchronization requests before an empty `202 Accepted` is returned.
- **SC-002**: 100% of notifications missing authentication, using an invalid API key, missing required fields, or containing invalid Base64 `DataVersion` are rejected without creating a synchronization request.
- **SC-003**: Repeating the same accepted notification at least five times produces empty `202 Accepted` responses and leaves exactly one lifecycle state, attempt count, retry schedule, and last-error record unchanged by duplicates.
- **SC-004**: Notifications for the same external document with two different `DataVersion` values are tracked as two distinct synchronization requests in 100% of acceptance tests.
- **SC-005**: The processor discovers eligible pending work within one configured polling interval after startup even when no wake-up signal is delivered.
- **SC-006**: Abandoned `Processing` requests become eligible for recovery within one configured processing-timeout window in 100% of restart or failure simulations.
- **SC-007**: Unsupported receiving/shipping notifications are not marked `Completed`; 100% are preserved in a replayable non-completed state until document-specific handlers exist.
- **SC-008**: Across representative processing outcomes, 100% of transient failure, permanent failure, unsupported-handler, successful-handler, and retry-exhausted cases end in the expected lifecycle state with diagnostic data retained.
- **SC-009**: Existing WMS-operator 1C connection-test and manual import operations remain inaccessible to the integration API key and remain accessible to eligible WMS operator or administrator identities in the authorization acceptance matrix.
- **SC-010**: No accepted notification requires WMS domain ownership of synchronization queue data, receiving documents, or shipping documents during this slice.
- **SC-011**: In a two-instance acceptance test, no synchronization request is processed concurrently by more than one processor instance across at least 1,000 eligible requests.

## Assumptions

- The current branch already represents issue 104 work; this specify step must not create, rename, switch, or delete branches.
- The first external source is 1C only, but the generic synchronization request identity remains provider-neutral enough to support later providers or source instances without changing accepted records.
- The active integration API key and source instance configuration are deployment-provided; creating UI or operational key-rotation workflows is deferred.
- Standard application secret-handling practices apply to integration API keys and external credentials.
- Existing API error conventions apply to malformed notification contracts, authentication failures, and authorization failures.
- Existing project testing patterns are sufficient to validate authentication policy behavior, notification contract binding, persistence idempotency, lifecycle transitions, and processor recovery without introducing new broad test frameworks.
