# Data Model: External Integration Synchronization Foundation

## Integration Source Identity

Represents the one configured 1C source instance allowed to send notifications in the first slice.

### Fields

- `SourceSystem`: Stable source family identifier. First value: `OneC`.
- `SourceInstance`: Server-assigned identifier of the configured external 1C infobase.
- `ApiKey`: Supplied through application configuration only; not persisted in application data.

### Validation Rules

- `SourceSystem` and `SourceInstance` are resolved server-side and never accepted from notification bodies.
- First slice supports exactly one configured source instance and one active API key.
- Production key material must come from protected uncommitted deployment configuration and must not be included in the application image.

## 1C Change Notification

External HTTP request body sent by 1C when a receiving or shipping object changed.

### Fields

- `Ref_Key` (required): External 1C object identifier.
- `DataVersion` (required): Base64 source version marker guaranteed by 1C.
- `Number` (optional): Diagnostic external document number.
- `Date` (optional): Diagnostic source document date without source offset.

### Validation Rules

- `Ref_Key` is required.
- `DataVersion` is required and must be valid Base64.
- Valid `DataVersion` is decoded to binary data before persistence.
- `Number` and `Date` do not participate in idempotency.
- `Date` is not an authoritative UTC timestamp and must not drive ordering, retry timing, or freshness decisions.

## Integration Synchronization Request

Durable provider-neutral technical record of one accepted external entity version that needs synchronization processing.

### Fields

- `Id`: Internal request identity.
- `SourceSystem`: Source family identifier, such as `OneC`.
- `SourceInstance`: Server-assigned external source instance identity.
- `EntityType`: Stable internal entity type. First values: `ReceivingOrder`, `ShippingOrder`.
- `ExternalId`: External object identifier from `Ref_Key`.
- `ExternalDataVersion`: Binary decoded source version marker.
- `ExternalDocumentNumber`: Optional diagnostic number from `Number`.
- `ExternalDocumentDate`: Optional diagnostic date from `Date`.
- `Trigger`: Reason request was created. First value: change notification.
- `Status`: Lifecycle state.
- `ReceivedAtUtc`: Time Myrmex accepted the notification.
- `ProcessingStartedAtUtc`: Time processing started, if any.
- `CompletedAtUtc`: Time successful completion occurred, if any.
- `AttemptCount`: Number of processing attempts recorded.
- `NextAttemptAtUtc`: Next time a transiently failed request is eligible, if any.
- `LastError`: Last processing or validation error retained for diagnostics, if any.

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

### Status Transitions

```text
Pending -> Processing
Processing -> Completed
Processing -> Deferred
Processing -> Pending
Processing -> Failed
```

- `Pending`: Eligible for processing now or after `NextAttemptAtUtc`.
- `Processing`: Currently selected by the processor.
- `Deferred`: Infrastructure processed the request but no document-specific handler is registered; request remains replayable.
- `Completed`: Registered handler completed successfully.
- `Failed`: Terminal technical failure after configured retries or permanent contract/processing error.

### Validation Rules

- `EntityType` uses stable internal values, not OData entity-set names.
- `IntegrationSynchronizationRequest` is not a WMS aggregate root.
- Requests are not automatically deleted, archived, or cleaned up in the first slice.
- `Superseded` is reserved for later and not implemented here.

## Synchronization Processor

Operational worker that discovers eligible requests and drives lifecycle transitions.

### Inputs

- Durable pending synchronization requests.
- Bounded in-process wake-up signal.
- Configured polling interval, batch size, request timeout, processing timeout, and retry delays.
- Optional registered document-specific handlers.

### Behavior Rules

- Scan immediately on application startup.
- Poll on a configurable fallback interval even when no wake-up signal arrives.
- Process eligible work in configurable batches.
- Recover abandoned `Processing` records after the configured processing timeout.
- Mark unsupported requests `Deferred`, not `Completed`.
- For transient technical failures, record attempt/error and schedule the next attempt from configured retry delays.
- For permanent failures or exhausted retries, mark `Failed`.

## Retry Schedule

Configuration that determines when transiently failed work becomes eligible again.

### Fields

- `PollingIntervalSeconds`: Fallback SQL scan interval. Preliminary default: 60.
- `BatchSize`: Number of eligible requests selected per processing pass.
- `RequestTimeoutSeconds`: Timeout around a processing attempt.
- `ProcessingTimeoutSeconds`: Time after which `Processing` work is considered abandoned.
- `RetryDelaysSeconds`: Explicit retry delays. Preliminary sequence: `10, 30, 120, 600, 1800, 3600, 10800`.

### Validation Rules

- Values must be positive where applicable.
- Final attempt count is derived from `RetryDelaysSeconds`.
- Retry delays distinguish transient technical failures from permanent failures and unsupported-handler outcomes.
