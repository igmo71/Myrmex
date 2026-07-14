# Tasks: External Integration Synchronization Foundation

**Input**: Design documents from `specs/104-external-integration-synchronization-foundation/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Automated test tasks are included because the specification and plan identify regression risks in authentication, HTTP contracts, SQL persistence/idempotency, lifecycle transitions, retry scheduling, wake-up behavior, and abandoned-work recovery.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested independently after the shared foundation is complete.

## Phase 1: Setup

**Purpose**: Prepare integration feature structure without changing behavior.

- [X] T001 Create integration synchronization folder structure in Myrmex.Integrations\Synchronization and Myrmex.Tests\Integrations\OneC\Synchronization
- [X] T002 [P] Create placeholder configuration files in Myrmex.Integrations\OneC\Configuration\OneCIntegrationApiKeyOptions.cs and Myrmex.Integrations\Synchronization\IntegrationSynchronizationOptions.cs
- [X] T003 [P] Create placeholder test fixture file in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationTestHost.cs

---

## Phase 2: Foundational

**Purpose**: Core authentication, persistence, options, signaling, constants, and registration prerequisites that block all user stories.

**Critical**: No user story implementation can start until these tasks are complete. Migration-dependent SQL tests must wait until the developer confirms the integration migration is ready.

- [X] T004 Define Myrmex.IntegrationApiKey in Myrmex.AspNetCore\Security\MyrmexAuthenticationSchemes.cs
- [X] T005 Define MyrmexAuthorizationPolicies.OneCIntegration policy hook in Myrmex.AspNetCore\Security\MyrmexAuthorizationPolicies.cs
- [X] T006 Implement OneCIntegrationApiKeyOptions with bounded SourceSystem, bounded SourceInstance, plaintext key, and startup validation in Myrmex.Integrations\OneC\Configuration\OneCIntegrationApiKeyOptions.cs
- [X] T007 Implement IntegrationSynchronizationOptions with PollingIntervalSeconds, BatchSize, ProcessingAttemptTimeoutSeconds, ProcessingTimeoutSeconds, RetryDelaysSeconds, and non-positive/empty-delay validation in Myrmex.Integrations\Synchronization\IntegrationSynchronizationOptions.cs
- [X] T008 [P] Create IntegrationSynchronizationStatus enum in Myrmex.Integrations\Synchronization\IntegrationSynchronizationStatus.cs
- [X] T009 [P] Create IntegrationSynchronizationEntityType constants in Myrmex.Integrations\Synchronization\IntegrationSynchronizationEntityTypes.cs
- [X] T010 [P] Create IntegrationSynchronizationTrigger constants in Myrmex.Integrations\Synchronization\IntegrationSynchronizationTriggers.cs
- [X] T011 Create IntegrationSynchronizationRequest entity with bounded properties in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequest.cs
- [X] T012 Configure IntegrationSynchronizationRequest table integration.synchronization_requests, PK name, column types, and UX_integration_synchronization_requests_idempotency in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestConfiguration.cs
- [X] T013 Create IntegrationDbContext with integration default schema in Myrmex.Integrations\Synchronization\IntegrationDbContext.cs
- [X] T014 Implement IntegrationApiKeyAuthenticationHandler with constant-time plaintext comparison, no key claims, and no key exposure in diagnostics in Myrmex.Integrations\OneC\Security\IntegrationApiKeyAuthenticationHandler.cs
- [X] T015 Register Myrmex.IntegrationApiKey named scheme without changing the default Myrmex.ApiSession scheme and configure MyrmexAuthorizationPolicies.OneCIntegration to require only that scheme in Myrmex.Integrations\OneC\OneCIntegrationModule.cs and Myrmex.AspNetCore\Security\MyrmexAuthorizationPolicies.cs
- [X] T016 Register IntegrationDbContext, synchronization options, TimeProvider, authentication services, synchronization services, and the IntegrationDbContext persistence reachability check in the existing health-check pipeline in Myrmex.Integrations\OneC\OneCIntegrationModule.cs
- [ ] T017 Developer-controlled migration checkpoint: developer generates, reviews, and applies the IntegrationDbContext migration in Myrmex.Integrations\Migrations; the agent must not generate or apply it, and migration-dependent SQL tests wait for developer confirmation
- [X] T018 Create SQL Server duplicate-key classifier for UX_integration_synchronization_requests_idempotency in Myrmex.Integrations\Synchronization\SqlServerDuplicateSynchronizationRequestDetector.cs
- [X] T019 Create IntegrationSynchronizationWakeUpSignal with bounded capacity 1, DropWrite, many signal writers, one reader, and no request payload in Myrmex.Integrations\Synchronization\IntegrationSynchronizationWakeUpSignal.cs
- [X] T020 [P] Add persistence mapping tests for bounded SQL column types and named unique index in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationPersistenceTests.cs
- [X] T021 [P] Add options validation tests for missing/empty API key, empty and over-length SourceSystem, empty and over-length SourceInstance, non-positive polling/batch/timeout values, non-positive retry-delay elements, and valid empty RetryDelaysSeconds allowing one attempt with no retries in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationOptionsTests.cs

**Checkpoint**: Integration authentication, persistence, configuration, wake-up signaling, and constants are ready for story work after the developer-controlled migration checkpoint is satisfied.

---

## Phase 3: User Story 1 - Accept 1C Change Notifications Durably (Priority: P1) MVP

**Goal**: Accept authenticated valid 1C receiving/shipping notifications, validate contracts, persist durable synchronization requests, emit a best-effort wake-up after commit, and return empty `202 Accepted` only after commit.

**Independent Test**: Configure one valid 1C integration identity, submit receiving and shipping notifications with a valid API key and required fields, verify durable records exist, verify malformed requests are rejected without records, and verify unauthenticated requests cannot satisfy the shared `OneCIntegration` policy.

### Tests for User Story 1

- [ ] T022 [P] [US1] Add endpoint integration tests for receiving/shipping route binding, explicit canonical JSON mappings for Ref_Key, DataVersion, Number, and Date, unknown-property tolerance, unchanged global ApiService JSON settings, and authenticated empty 202 response in Myrmex.Tests\Integrations\OneC\Endpoints\OneCNotificationEndpointTests.cs
- [ ] T023 [P] [US1] Add endpoint integration tests for invalid Ref_Key, missing Ref_Key, invalid Base64 DataVersion, empty decoded DataVersion, oversized DataVersion, over-length Number, malformed Date, and ProblemDetails-style responses identifying Ref_Key, DataVersion, Number, or Date without secrets or internal exception details in Myrmex.Tests\Integrations\OneC\Endpoints\OneCNotificationValidationTests.cs
- [ ] T024 [P] [US1] Add persistence tests for source-local ExternalDocumentDate Kind=Unspecified and SQL datetime2 diagnostics in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationPersistenceTests.cs

### Implementation for User Story 1

- [ ] T025 [P] [US1] Create OneCChangeNotificationRequest with explicit JSON property mappings in Myrmex.Integrations\OneC\Notifications\OneCChangeNotificationRequest.cs
- [ ] T026 [P] [US1] Create OneCChangeNotificationValidator for Ref_Key, DataVersion, Number, Date, and decoded version bounds in Myrmex.Integrations\OneC\Notifications\OneCChangeNotificationValidator.cs
- [ ] T027 [US1] Implement IntegrationSynchronizationRequestFactory to resolve SourceSystem, SourceInstance, EntityType, canonical ExternalId, decoded ExternalDataVersion, diagnostics, and ReceivedAtUtc in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestFactory.cs
- [ ] T028 [US1] Implement IntegrationSynchronizationRequestStore insert-and-commit path for new synchronization requests in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestStore.cs
- [ ] T029 [US1] Emit the best-effort IntegrationSynchronizationWakeUpSignal only after a newly inserted synchronization request has committed successfully in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestStore.cs
- [ ] T030 [US1] Add receiving-orders/changed and shipping-orders/changed route handlers with MyrmexAuthorizationPolicies.OneCIntegration applied in Myrmex.Integrations\OneC\Endpoints\OneCNotificationEndpoints.cs
- [ ] T031 [US1] Map OneC notification endpoints separately from WMS-operator admin routes in Myrmex.Integrations\OneC\Endpoints\OneCEndpoints.cs
- [ ] T032 [US1] Add diagnostics for accepted notifications and validation failures without logging secrets in Myrmex.Integrations\OneC\Endpoints\OneCNotificationEndpoints.cs

**Checkpoint**: User Story 1 is independently functional and testable as the authenticated durable intake MVP after the developer-controlled migration checkpoint is satisfied.

---

## Phase 4: User Story 2 - Preserve Authentication Boundaries (Priority: P1)

**Goal**: Prove notification endpoints authenticate only with the 1C integration API-key scheme while preserving existing Identity API-session behavior and WMS operator protection for current 1C admin/import endpoints.

**Independent Test**: Call notification endpoints with valid/missing/invalid API keys, call them with only an Identity API-session cookie, and call existing 1C admin endpoints with machine and WMS operator credentials.

### Tests for User Story 2

- [ ] T033 [P] [US2] Add authentication scheme default-preservation tests for Myrmex.ApiSession and Myrmex.IntegrationApiKey registration in Myrmex.Tests\Integrations\Authorization\IntegrationApiKeyAuthenticationTests.cs
- [ ] T034 [P] [US2] Add notification endpoint authorization tests for valid API key, missing API key, wrong API key, and Identity API-session-only rejection in Myrmex.Tests\Integrations\Authorization\IntegrationAuthorizationEndpointTests.cs
- [ ] T035 [US2] Add existing 1C admin/import route protection regression tests for machine key rejection and WMS operator acceptance in Myrmex.Tests\Integrations\Authorization\IntegrationAuthorizationEndpointTests.cs

**Checkpoint**: User Story 2 proves notification auth does not weaken existing user-operated routes.

---

## Phase 5: User Story 3 - Preserve Idempotent Synchronization Request Lifecycle (Priority: P1)

**Goal**: Treat duplicate notifications as idempotent intake through the named SQL unique index, preserve lifecycle fields, and avoid converting unrelated persistence failures to successful duplicates.

**Independent Test**: Submit repeated and concurrent duplicate notifications for the same source/version and verify empty `202 Accepted`, exactly one durable request, unchanged lifecycle fields, Pending-only duplicate wake-up behavior, and failure behavior for non-idempotency persistence errors.

### Tests for User Story 3

- [ ] T036 [P] [US3] Add persistence tests for UX_integration_synchronization_requests_idempotency uniqueness and different DataVersion/source-instance distinctness in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationIdempotencyTests.cs
- [ ] T037 [P] [US3] Add store tests for duplicate Pending, Processing, Deferred, Completed, and Failed records preserving status, attempts, retry timing, timestamps, and LastError in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationDuplicateTests.cs
- [ ] T038 [P] [US3] Add duplicate-key classifier tests that only named UX_integration_synchronization_requests_idempotency violations are treated as duplicates in Myrmex.Tests\Integrations\OneC\Synchronization\SqlServerDuplicateSynchronizationRequestDetectorTests.cs
- [ ] T039 [P] [US3] Add endpoint tests for concurrent duplicate HTTP intake returning empty 202 with one durable record in Myrmex.Tests\Integrations\OneC\Endpoints\OneCNotificationEndpointTests.cs

### Implementation for User Story 3

- [ ] T040 [US3] Extend IntegrationSynchronizationRequestStore to catch SQL Server duplicate-key errors and verify UX_integration_synchronization_requests_idempotency in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestStore.cs
- [ ] T041 [US3] Detach the failed Added entity or otherwise clear the failed insert from EF tracking before loading the existing record, without retrying the failed insert, in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestStore.cs
- [ ] T042 [US3] Implement duplicate intake result that loads existing lifecycle state without mutating it in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestStore.cs
- [ ] T043 [US3] Emit best-effort wake-up signal only for duplicate Pending requests in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestStore.cs
- [ ] T044 [US3] Map newly inserted synchronization requests and accepted duplicate synchronization requests to the same empty 202 Accepted response without exposing which result occurred in Myrmex.Integrations\OneC\Endpoints\OneCNotificationEndpoints.cs
- [ ] T045 [US3] Add diagnostics for duplicate notification detection without exposing new/existing state to callers in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestStore.cs

**Checkpoint**: User Story 3 is independently functional and duplicate delivery cannot mutate existing lifecycle state.

---

## Phase 6: User Story 4 - Operate a Recoverable Synchronization Queue (Priority: P2)

**Goal**: Process eligible synchronization requests through the defined lifecycle with SQL polling, coalescing wake-up signals, explicit retry delays, unsupported-handler deferral, processing-attempt timeout handling, host-shutdown cancellation handling, and abandoned `Processing` recovery.

**Independent Test**: Accept or seed notifications with and without registered handlers, simulate success/transient/permanent outcomes, distinguish processing-attempt timeout from host-shutdown cancellation, suppress wake-up, fill the coalescing channel, and restart with abandoned `Processing` records.

### Tests for User Story 4

- [ ] T046 [P] [US4] Add lifecycle tests for Pending-to-Deferred unsupported-handler behavior without AttemptCount, ProcessingStartedAtUtc, or retry consumption in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationProcessorTests.cs
- [ ] T047 [US4] Add lifecycle tests for durable Pending-to-Processing attempt start, AttemptCount increment, ProcessingStartedAtUtc, commit before handler invocation, Processing-to-Completed success, and completion timestamp recording in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationProcessorTests.cs
- [ ] T048 [P] [US4] Add retry tests for RetryDelaysSeconds[0], N+1 attempts, empty RetryDelaysSeconds allowing one attempt with no retries, transient failure becoming terminal Failed when no retry delay exists, exhausted retry, permanent failure, and ProcessingAttemptTimeoutSeconds as a transient failure in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationRetryTests.cs
- [ ] T049 [P] [US4] Add cancellation tests proving host-shutdown cancellation leaves durable records Processing for abandoned recovery and does not schedule a normal handler retry in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationCancellationTests.cs
- [ ] T050 [P] [US4] Add wake-up channel tests for capacity 1, DropWrite, no payload, and draining SQL batches until none are eligible in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationWakeUpTests.cs
- [ ] T051 [P] [US4] Add abandoned Processing recovery tests for preserved AttemptCount, immediate Pending eligibility when retries remain, Failed when retries are exhausted, cleared ProcessingStartedAtUtc when requeued, and bounded non-secret LastError in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationRecoveryTests.cs

### Implementation for User Story 4

- [ ] T052 [P] [US4] Define IIntegrationSynchronizationHandler and handler resolution abstractions in Myrmex.Integrations\Synchronization\IIntegrationSynchronizationHandler.cs
- [ ] T053 [US4] Implement eligible request query and batch selection in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestStore.cs
- [ ] T054 [US4] Implement direct Pending-to-Deferred transition before processing starts when no handler exists in Myrmex.Integrations\Synchronization\IntegrationSynchronizationProcessor.cs
- [ ] T055 [US4] Implement durable Processing attempt start by transitioning Pending to Processing, incrementing AttemptCount, setting ProcessingStartedAtUtc, and committing before invoking the document handler in Myrmex.Integrations\Synchronization\IntegrationSynchronizationProcessor.cs
- [ ] T056 [US4] Implement Completed, Pending retry, Failed terminal transitions, ProcessingAttemptTimeoutSeconds transient failure handling, host-shutdown cancellation behavior, and empty RetryDelaysSeconds transient-failure terminal behavior in Myrmex.Integrations\Synchronization\IntegrationSynchronizationProcessor.cs
- [ ] T057 [US4] Implement retry schedule calculation from RetryDelaysSeconds, including empty collections permitting one attempt and no retries, in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRetryPolicy.cs
- [ ] T058 [US4] Implement abandoned Processing recovery after ProcessingTimeoutSeconds with AttemptCount preserved, immediate Pending eligibility when retries remain, Failed when exhausted, ProcessingStartedAtUtc cleared when requeued, and bounded non-secret LastError in Myrmex.Integrations\Synchronization\IntegrationSynchronizationRequestStore.cs
- [ ] T059 [US4] Implement hosted service startup scan and fallback polling so each pass invokes store abandoned-Processing recovery before querying and processing currently eligible requests; keep wake-up read loop drain-until-no-eligible-work behavior in Myrmex.Integrations\Synchronization\IntegrationSynchronizationWorker.cs
- [ ] T060 [US4] Register IntegrationSynchronizationWorker and handler collection in Myrmex.Integrations\OneC\OneCIntegrationModule.cs
- [ ] T061 [US4] Add worker-loop diagnostics for startup scan, polling, wake-up, and drain cycles in Myrmex.Integrations\Synchronization\IntegrationSynchronizationWorker.cs
- [ ] T062 [US4] Add lifecycle and outcome diagnostics for transitions, retries, defer, failure, completion, and recovery in Myrmex.Integrations\Synchronization\IntegrationSynchronizationProcessor.cs

**Checkpoint**: User Story 4 is independently functional and durable requests are processed, deferred, retried, recovered, or failed through the recoverable lifecycle.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Final validation guidance, cleanup, and documentation consistency.

- [ ] T063 [P] Update quickstart validation notes for final option names and expected outcomes in specs\104-external-integration-synchronization-foundation\quickstart.md
- [ ] T064 [P] Review integration diagnostics for secret exposure in Myrmex.Integrations\OneC and Myrmex.Integrations\Synchronization
- [ ] T065 [P] Review public endpoint names and OpenAPI summaries for notification endpoints in Myrmex.Integrations\OneC\Endpoints\OneCNotificationEndpoints.cs
- [ ] T066 Document developer-controlled migration generation and application steps in specs\104-external-integration-synchronization-foundation\quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 Setup has no dependencies.
- Phase 2 Foundational depends on Phase 1 and blocks all user stories.
- The developer-controlled migration checkpoint in Phase 2 must be confirmed before migration-dependent SQL tests run.
- User Stories 1, 2, and 3 are all P1 and can be implemented after Phase 2. The suggested MVP starts with US1, then US2, then US3.
- User Story 4 depends on Phase 2 and benefits from US1/US3 records but can be developed with seeded synchronization requests.
- Final Phase depends on desired user stories being complete.

### User Story Dependencies

- US1 Accept Notifications: requires Phase 2 authentication, persistence, options, and wake-up foundation.
- US2 Authentication Boundaries: requires Phase 2 authentication implementation and US1 notification route shape for endpoint authorization tests.
- US3 Idempotent Lifecycle: requires Phase 2 persistence and duplicate-classifier foundation and integrates with US1 endpoint intake.
- US4 Recoverable Queue: requires Phase 2 persistence/options/wake-up foundation and may use seeded records before US1 is complete.

### Within Each User Story

- Write planned tests before implementation tasks they protect.
- Persistence/entity tasks precede store/service tasks.
- Store/service tasks precede endpoint/worker registration tasks.
- Endpoint/auth tests protect Minimal API binding, routing, serialization, and authorization boundaries not fully covered at lower layers.
- Tasks editing the same file are not marked parallel.

---

## Parallel Execution Examples

### User Story 1

```text
Task: T022 endpoint contract tests in Myrmex.Tests\Integrations\OneC\Endpoints\OneCNotificationEndpointTests.cs
Task: T023 validation tests in Myrmex.Tests\Integrations\OneC\Endpoints\OneCNotificationValidationTests.cs
Task: T024 persistence diagnostics tests in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationPersistenceTests.cs
Task: T025 request contract in Myrmex.Integrations\OneC\Notifications\OneCChangeNotificationRequest.cs
Task: T026 validator in Myrmex.Integrations\OneC\Notifications\OneCChangeNotificationValidator.cs
```

### User Story 2

```text
Task: T033 default scheme tests in Myrmex.Tests\Integrations\Authorization\IntegrationApiKeyAuthenticationTests.cs
Task: T034 notification authorization tests in Myrmex.Tests\Integrations\Authorization\IntegrationAuthorizationEndpointTests.cs
```

### User Story 3

```text
Task: T036 idempotency persistence tests in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationIdempotencyTests.cs
Task: T037 lifecycle preservation tests in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationDuplicateTests.cs
Task: T038 duplicate-key classifier tests in Myrmex.Tests\Integrations\OneC\Synchronization\SqlServerDuplicateSynchronizationRequestDetectorTests.cs
Task: T039 concurrent endpoint duplicate tests in Myrmex.Tests\Integrations\OneC\Endpoints\OneCNotificationEndpointTests.cs
```

### User Story 4

```text
Task: T046 unsupported-handler lifecycle tests in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationProcessorTests.cs
Task: T048 retry and timeout tests in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationRetryTests.cs
Task: T049 host-shutdown cancellation tests in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationCancellationTests.cs
Task: T050 wake-up channel tests in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationWakeUpTests.cs
Task: T051 recovery tests in Myrmex.Tests\Integrations\OneC\Synchronization\IntegrationSynchronizationRecoveryTests.cs
Task: T052 handler abstractions in Myrmex.Integrations\Synchronization\IIntegrationSynchronizationHandler.cs
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational, stopping at the developer-controlled migration checkpoint until the developer confirms migration readiness.
3. Complete Phase 3 US1 to accept authenticated, durably recorded receiving/shipping notifications.
4. Stop and validate US1 independently with endpoint and persistence checks.

### Incremental Delivery

1. US1 delivers authenticated durable notification intake.
2. US2 locks down machine-auth boundaries without changing existing WMS operator routes.
3. US3 hardens duplicate delivery and lifecycle preservation.
4. US4 adds recoverable processor behavior over the durable queue.

### Validation Commands

These are recommended for developer-controlled validation only:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~Integrations"
```

No build, test, app startup, database update, EF migration generation, or EF migration application is executed by this task-generation step.
