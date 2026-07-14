# Research: External Integration Synchronization Foundation

## Decision: Integration-owned persistence with a dedicated `IntegrationDbContext`

**Decision**: Place synchronization request persistence in `Myrmex.Integrations`, register a dedicated `IntegrationDbContext` against `ConnectionStrings:MyrmexDatabase`, and use a separate `integration` schema.

**Rationale**: The synchronization queue is technical integration infrastructure, not WMS domain state. A dedicated context preserves WMS module ownership, keeps the queue provider-neutral, and still uses the normal Myrmex database connection workflow.

**Alternatives considered**:

- Add synchronization entities to `WmsDbContext`: rejected because the spec explicitly excludes WMS ownership of the queue.
- Store requests outside SQL: rejected because SQL persistence is the durable queue and source of truth for this slice.
- Reuse Identity persistence: rejected because machine notification state is unrelated to ASP.NET Identity.

## Decision: 1C notification contracts stay under `Myrmex.Integrations/OneC`

**Decision**: Keep the exact 1C notification request DTOs, endpoint routes, and API-key configuration under the 1C integration boundary.

**Rationale**: The contract is an external HTTP integration contract, not a WebApp/shared backend-client contract. Keeping it under `OneC` prevents `Myrmex.Shared` from accumulating provider-specific endpoint DTOs that no first-party client consumes.

**Alternatives considered**:

- Put notification DTOs in `Myrmex.Shared`: rejected because `Myrmex.Shared` is reserved for contracts that genuinely cross backend/client boundaries.
- Generalize notification DTOs for all ERP providers: rejected because a generalized ERP framework is out of scope.

## Decision: Add a named API-key authentication scheme without changing the default API session scheme

**Decision**: Add `Myrmex.IntegrationApiKey` as a named authentication scheme and `MyrmexAuthorizationPolicies.OneCIntegration` as a dedicated policy for notification endpoints only. The policy authenticates only through `Myrmex.IntegrationApiKey`; an Identity API-session cookie alone cannot satisfy it. Preserve `Myrmex.ApiSession` as the ApiService default authentication scheme.

**Rationale**: The external caller is a machine identity and must not be forced into ASP.NET Identity roles or GUID user identifiers. Existing 1C administrative routes remain user-operated and protected by the WMS operator policy. Registering the named scheme must not change default authentication behavior for current endpoints.

**Alternatives considered**:

- Reuse the WMS operator policy for notifications: rejected because the 1C caller is not a user identity.
- Make API key the default scheme: rejected because it would risk changing existing protected API behavior.
- Add Identity roles to the machine principal: rejected because it would blur user and machine identity boundaries.

## Decision: Configuration-only active API key for the first slice

**Decision**: Supply the one active integration API key through application configuration only. Use a disposable local development key for development, protected uncommitted environment or `.env` configuration for production, and do not persist the key in application data. Missing or empty configured API-key values fail startup options validation. Compare the presented plaintext key with the configured plaintext key using constant-time comparison. Do not log, persist, place in claims, or expose the key in errors.

**Rationale**: This matches the clarified first-slice security posture while avoiding premature key-rotation or key-hashing infrastructure. It keeps production secrets out of source control and application images. Constant-time comparison avoids key length/content timing leaks within the chosen plaintext-configuration model.

**Alternatives considered**:

- Persist a hashed key in application data: rejected as unnecessary for the first slice.
- Persist plaintext key material: rejected because the active key must not be stored as application data.
- Build key rotation workflows now: rejected because key rotation is out of scope.

## Decision: Bounded idempotency columns use canonical strings plus binary source version

**Decision**: Store `ExternalId` as a canonical bounded string, not as a database `uniqueidentifier`. For 1C, validate `Ref_Key` as a non-empty GUID and store its canonical `D` string form. Following repository schema/table/index naming conventions, use table `integration.synchronization_requests` and unique index `UX_integration_synchronization_requests_idempotency`. Use bounded SQL Server column sizes for the unique index: `SourceSystem nvarchar(32)`, `SourceInstance nvarchar(128)`, `EntityType nvarchar(32)`, `ExternalId nvarchar(128)`, and `ExternalDataVersion varbinary(128)`. Bound `ExternalDocumentNumber` as `nvarchar(64)`, `ExternalDocumentDate` as `datetime2`, and `LastError` as `nvarchar(2048)`.

**Rationale**: 1C supplies GUID identifiers in the first slice, but the synchronization request is provider-neutral and later external systems may not use GUID identifiers. Canonical strings preserve provider-neutrality while first-slice validation still rejects invalid 1C references. The composite unique key is physically valid on SQL Server: the bounded key columns require at most 768 bytes, below the 1700-byte nonclustered index key limit.

**Alternatives considered**:

- Store `ExternalId` as `uniqueidentifier`: rejected because it overfits the first 1C provider and weakens future provider-neutral persistence.
- Leave key fields unbounded: rejected because SQL Server cannot reliably enforce a physically valid composite unique index over unbounded strings/binary values.

## Decision: SQL uniqueness enforces idempotent intake

**Decision**: Enforce uniqueness over `SourceSystem`, `SourceInstance`, `EntityType`, `ExternalId`, and `ExternalDataVersion`; handle concurrent duplicate HTTP requests by preserving one durable synchronization request and returning empty `202 Accepted` for duplicates. Duplicate handling first verifies a SQL Server duplicate-key error category, then verifies the failure identifies `UX_integration_synchronization_requests_idempotency`. After a duplicate insert failure, the failed `Added` entity is detached or otherwise cleared from EF tracking before loading the existing record, and the failed insert is not retried. Unrelated persistence failures remain failures and are not converted to success.

**Rationale**: The database uniqueness constraint is the durable idempotency boundary. It prevents duplicates even when duplicate requests arrive concurrently and avoids relying on in-memory coordination.

**Alternatives considered**:

- In-memory duplicate tracking: rejected because it cannot survive restart and is not the source of truth.
- Treat duplicate receipt as replay or repair: rejected by clarification; duplicate receipt must not mutate lifecycle state.

## Decision: SQL polling is reliable; bounded channel is only a coalescing wake-up signal

**Decision**: Use a bounded in-process channel with capacity 1, `DropWrite` behavior when full, many signal writers, and one reader only to wake the processor after commit. The channel carries no synchronization request payload. After each wake-up, the processor scans SQL batches until no immediately eligible work remains. The processor still scans SQL immediately on startup and on a configurable fallback interval.

**Rationale**: SQL is the durable queue. Wake-up signals can be lost or delayed without losing work because polling and startup scanning discover committed requests.

**Alternatives considered**:

- Channel carries queue records: rejected because the channel has no reliability guarantee.
- RabbitMQ or an Outbox: rejected as out of scope and unnecessary for the first synchronization foundation.

## Decision: Explicit lifecycle states with `Deferred` for unsupported handlers

**Decision**: Use `Pending`, `Processing`, `Deferred`, `Completed`, and `Failed`; reserve `Superseded` for later. Resolve whether a document-specific handler exists before transitioning to `Processing`. When no handler is registered, transition directly from `Pending` to `Deferred`, without incrementing `AttemptCount`, setting `ProcessingStartedAtUtc`, or consuming a retry delay.

**Rationale**: The first slice must not claim receiving/shipping synchronization succeeded just because infrastructure selected the request. `Deferred` preserves replayable state for later handlers without counting unsupported work as a processing attempt.

**Alternatives considered**:

- Mark unsupported requests completed: rejected because it would hide unsynchronized external changes.
- Add `Superseded` now: rejected because it is not required for first-slice behavior.

## Decision: Retry attempts derive from explicit delay collection

**Decision**: Configure polling interval, batch size, processing-attempt timeout, processing timeout, and explicit retry delays in seconds. `AttemptCount` increments when a processing attempt starts; the first attempt has `AttemptCount = 1`. The processor durably commits the `Pending` to `Processing` transition, incremented `AttemptCount`, and `ProcessingStartedAtUtc` before invoking a handler. `N` configured retry delays permit `N + 1` total processing attempts. After attempt 1 fails transiently, `RetryDelaysSeconds[0]` determines the next eligibility time. `ProcessingAttemptTimeoutSeconds` is a transient failure and follows the configured retry policy. Host-shutdown cancellation leaves the durable record `Processing` for abandoned-processing recovery and does not schedule a normal handler retry. `Deferred` unsupported-handler outcomes do not consume retry delays.

**Rationale**: Explicit delays are operationally transparent and avoid contradictory retry-count and delay settings. The behavior remains understandable for support and tests.

**Alternatives considered**:

- Hidden exponential formula: rejected because the stakeholder decision preferred explicit delays.
- Separate max-attempt setting: rejected because it can contradict the delay collection.

## Decision: Risk-based tests use the lowest owning layer

**Decision**: Use endpoint tests for route/auth/JSON contract boundaries, persistence tests for mapping and uniqueness, lifecycle tests for state transitions and duplicate preservation, and worker tests for polling/retry/recovery. Do not add UI or API-client tests.

**Rationale**: This follows project testing guidance and avoids duplicating the same behavior at every layer.

**Alternatives considered**:

- Full HTTP coverage for every lifecycle state: rejected where lower-layer lifecycle tests fully own the behavior.
- WebApp/API-client coverage: rejected because no WebApp or first-party client surface is introduced.
