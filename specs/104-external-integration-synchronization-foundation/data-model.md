# Data Model: External Integration Synchronization Foundation

## Integration Source Identity

Represents the one configured 1C source instance allowed to send notifications in the first slice.

### Fields

- `SourceSystem`: Stable source family identifier. First value: `OneC`. Persistence type: `nvarchar(32)`.
- `SourceInstance`: Server-assigned identifier of the configured external 1C infobase. Persistence type: `nvarchar(128)`.
- `ApiKey`: Supplied through application configuration only; not persisted in application data.

### Validation Rules

- `SourceSystem` and `SourceInstance` are resolved server-side and never accepted from notification bodies.
- First slice supports exactly one configured source instance and one active API key.
- Production key material must come from protected uncommitted deployment configuration and must not be included in the application image.
- Missing or empty API-key configuration fails startup options validation.
- Presented plaintext keys are compared with the configured plaintext key using constant-time comparison.
- API keys are not logged, persisted, placed in claims, or exposed in errors.

## 1C Change Notification

External HTTP request body sent by 1C when a receiving or shipping object changed.

### Fields

- `Ref_Key` (required): External 1C object identifier; must be a valid non-empty GUID.
- `DataVersion` (required): Base64 source version marker guaranteed by 1C; decoded value must be non-empty and no larger than 128 bytes.
- `Number` (optional): Diagnostic external document number; maximum 64 characters.
- `Date` (optional): Diagnostic source document date without source offset; malformed values are rejected.

### Validation Rules

- `Ref_Key` is required and must parse as a non-empty GUID.
- `DataVersion` is required, non-empty, valid Base64, decodes to a non-empty byte sequence, and respects the 128-byte persistence maximum.
- Valid `DataVersion` is decoded to binary data before persistence.
- Unknown JSON properties are ignored.
- Exact JSON names are enforced through explicit JSON property mapping for the notification contract; do not change global ApiService JSON case-sensitivity.
- `Number` and `Date` do not participate in idempotency.
- `Date` is not an authoritative UTC timestamp and must not drive ordering, retry timing, or freshness decisions.

## Integration Synchronization Request

Durable provider-neutral technical record of one accepted external entity version that needs synchronization processing.

### Fields

- `Id`: Internal request identity.
- `SourceSystem`: Source family identifier, such as `OneC`. Persistence type: `nvarchar(32)`.
- `SourceInstance`: Server-assigned external source instance identity. Persistence type: `nvarchar(128)`.
- `EntityType`: Stable internal entity type. First values: `ReceivingOrder`, `ShippingOrder`. Persistence type: `nvarchar(32)`.
- `ExternalId`: Provider-neutral canonical external object identifier. Persistence type: `nvarchar(128)`. For 1C, store `Ref_Key` as canonical GUID `D` format after validation.
- `ExternalDataVersion`: Binary decoded source version marker. Persistence type: `varbinary(128)`.
- `ExternalDocumentNumber`: Optional diagnostic number from `Number`. Persistence type: `nvarchar(64)`.
- `ExternalDocumentDate`: Optional diagnostic date from `Date`. Runtime type is source-local `DateTime` with `Kind = Unspecified`; persistence type is SQL Server `datetime2`. It is diagnostic only and is never automatically converted to UTC.
- `Trigger`: Reason request was created. First value: change notification.
- `Status`: Lifecycle state.
- `ReceivedAtUtc`: Time Myrmex accepted the notification.
- `ProcessingStartedAtUtc`: Time processing started, if any.
- `CompletedAtUtc`: Time successful completion occurred, if any.
- `AttemptCount`: Number of processing attempts recorded.
- `NextAttemptAtUtc`: Next time a transiently failed request is eligible, if any.
- `LastError`: Last processing or validation error retained for diagnostics, if any. Persistence type: `nvarchar(2048)`.

### Identity and Uniqueness

One synchronization request is uniquely identified by:

```text
SourceSystem
+ SourceInstance
+ EntityType
+ ExternalId
+ ExternalDataVersion
```

Duplicate notification receipt preserves the existing lifecycle state, attempt count, retry schedule, timestamps, and last error. A duplicate of a pending request may only emit a best-effort wake-up signal.

The SQL Server unique index over these columns is physically valid because the maximum key width is 768 bytes:

```text
SourceSystem nvarchar(32) = 64 bytes
SourceInstance nvarchar(128) = 256 bytes
EntityType nvarchar(32) = 64 bytes
ExternalId nvarchar(128) = 256 bytes
ExternalDataVersion varbinary(128) = 128 bytes
```

Only violations of the named idempotency unique constraint are handled as duplicate notification intake. Duplicate handling first verifies a SQL Server duplicate-key error category, then verifies the failure identifies `UX_integration_synchronization_requests_idempotency`. Other persistence failures remain failures and must not return successful duplicate responses.

### Table and Index Names

- Table: `integration.synchronization_requests`
- Idempotency unique index: `UX_integration_synchronization_requests_idempotency`

### Status Transitions

```text
Pending -> Deferred
Pending -> Processing
Processing -> Completed
Processing -> Pending
Processing -> Failed
```

- `Pending`: Eligible for processing now or after `NextAttemptAtUtc`.
- `Processing`: Currently selected by the processor.
- `Deferred`: No document-specific handler is registered; request remains replayable and no processing attempt has started.
- `Completed`: Registered handler completed successfully.
- `Failed`: Terminal technical failure after configured retries or permanent contract/processing error.

### Validation Rules

- `EntityType` uses stable internal values, not OData entity-set names.
- `ExternalId` remains a canonical bounded string for provider-neutrality even though first-slice 1C `Ref_Key` values must be valid GUIDs.
- `IntegrationSynchronizationRequest` is not a WMS aggregate root.
- Requests are not automatically deleted, archived, or cleaned up in the first slice.
- `Superseded` is reserved for later and not implemented here.

## Synchronization Processor

Operational worker that discovers eligible requests and drives lifecycle transitions.

### Inputs

- Durable pending synchronization requests.
- Bounded in-process wake-up signal.
- Configured polling interval, batch size, `ProcessingAttemptTimeoutSeconds`, processing timeout, and retry delays.
- Optional registered document-specific handlers.

### Behavior Rules

- Scan immediately on application startup.
- Poll on a configurable fallback interval even when no wake-up signal arrives.
- Process eligible work in configurable batches.
- After each wake-up signal, process eligible SQL batches until no immediately eligible work remains.
- Recover abandoned `Processing` records after the configured processing timeout.
- Resolve whether a document-specific handler exists before transitioning to `Processing`.
- When no handler exists, transition directly from `Pending` to `Deferred`, do not increment `AttemptCount`, do not set `ProcessingStartedAtUtc`, and do not consume a retry delay.
- For transient technical failures, record attempt/error and schedule the next attempt from configured retry delays.
- For permanent failures or exhausted retries, mark `Failed`.

## Retry Schedule

Configuration that determines when transiently failed work becomes eligible again.

### Fields

- `PollingIntervalSeconds`: Fallback SQL scan interval. Preliminary default: 60.
- `BatchSize`: Number of eligible requests selected per processing pass.
- `ProcessingAttemptTimeoutSeconds`: Timeout around one processing attempt.
- `ProcessingTimeoutSeconds`: Time after which `Processing` work is considered abandoned.
- `RetryDelaysSeconds`: Explicit retry delays. Preliminary sequence: `10, 30, 120, 600, 1800, 3600, 10800`.

### Validation Rules

- Values must be positive where applicable.
- `AttemptCount` increments when a processing attempt starts.
- The first processing attempt has `AttemptCount = 1`.
- `N` configured retry delays permit `N + 1` total processing attempts.
- After attempt 1 fails transiently, `RetryDelaysSeconds[0]` determines the next eligibility time.
- `Deferred` unsupported-handler outcomes do not consume retry delays.
- Retry delays distinguish transient technical failures from permanent failures and unsupported-handler outcomes.
