# Data Model: External Integration Synchronization Foundation

## Entity: IntegrationSynchronizationRequest

Provider-neutral technical persistence record for one accepted external document-version notification.

### Fields

| Field | Required | Description |
|-------|----------|-------------|
| `Id` | Yes | Internal identifier for the synchronization request. |
| `SourceSystem` | Yes | External system family. First slice uses `OneC`. |
| `SourceInstance` | Yes | Myrmex-assigned source instance identifier for the configured 1C infobase. |
| `EntityType` | Yes | Stable internal document category. First values: `ReceivingOrder`, `ShippingOrder`. |
| `ExternalId` | Yes | External document identity from 1C `Ref_Key`. |
| `ExternalDataVersion` | Yes | Decoded binary `DataVersion` supplied by 1C. |
| `ExternalDocumentNumber` | No | Optional diagnostic value from 1C `Number`. |
| `ExternalDocumentDate` | No | Optional diagnostic value from 1C `Date`; not authoritative UTC. |
| `Trigger` | Yes | Reason the request was created. First value: 1C change notification. |
| `Status` | Yes | Current processing lifecycle state. |
| `ReceivedAtUtc` | Yes | Time Myrmex accepted and persisted the notification. |
| `ProcessingStartedAtUtc` | No | Time a processor claimed the request. |
| `CompletedAtUtc` | No | Time a handler completed or terminal processing outcome was recorded. |
| `AttemptCount` | Yes | Number of processing attempts. Starts at 0. |
| `NextAttemptAtUtc` | No | Earliest time a pending retry becomes eligible. |
| `LastError` | No | Bounded non-secret diagnostic details for the last processing failure. |
| `ConcurrencyStamp` | Yes | Application-managed concurrency value used for safe updates and claims. |

### Identity and Uniqueness

Unique external version key:

```text
SourceSystem
+ SourceInstance
+ EntityType
+ ExternalId
+ ExternalDataVersion
```

Rules:

- The same key can exist at most once.
- Duplicate intake returns empty `202 Accepted` and does not reveal whether a row was inserted.
- Duplicate intake does not change existing status, attempt count, retry timing, processing timestamps, completion time, or last error.
- A duplicate of a `Pending` request may send only a best-effort wake-up signal.

### Validation Rules

- `SourceSystem`, `SourceInstance`, `EntityType`, `ExternalId`, `ExternalDataVersion`, `Trigger`, `Status`, `ReceivedAtUtc`, and `ConcurrencyStamp` are required.
- `EntityType` is limited to stable internal values owned by Myrmex, not source OData entity-set names.
- `ExternalDataVersion` must be decoded from valid Base64 before persistence.
- `ExternalDocumentDate` preserves the source diagnostic value and must not be treated as authoritative UTC.
- `LastError` must not contain API keys, credentials, or other protected secrets.

### Status State Machine

```text
Pending
  -> Processing
  -> Deferred
  -> Completed
  -> Failed

Processing
  -> Pending      (abandoned processing recovery or transient retry)
  -> Deferred     (no handler registered)
  -> Completed    (registered handler succeeds)
  -> Failed       (terminal technical or permanent processing failure)
```

State meanings:

- `Pending`: eligible now or after `NextAttemptAtUtc`.
- `Processing`: claimed by one processor instance.
- `Deferred`: infrastructure processed the request but no document-specific handler is registered; the row remains available for future controlled replay.
- `Completed`: registered handler completed successfully.
- `Failed`: terminal technical failure after retries or permanent validation/processing failure.

State constraints:

- `Completed` requires a registered handler success.
- First-slice receiving/shipping requests without handlers become `Deferred`, not `Completed`.
- `Failed` is not made eligible again unless a later explicit replay or repair feature changes state.
- Issue #104 does not implement operational replay, cleanup, archival, or deletion.

## Entity: IntegrationSynchronizationOptions

Configuration values controlling intake and processor behavior.

### Fields

| Field | Description |
|-------|-------------|
| `PollingIntervalSeconds` | Fallback scan interval. Preliminary default: 60. |
| `BatchSize` | Maximum number of eligible requests claimed per processing scan. |
| `RequestTimeoutSeconds` | Timeout budget for individual request processing work. |
| `ProcessingTimeoutSeconds` | Age after which `Processing` records are considered abandoned. |
| `RetryDelaysSeconds` | Explicit retry delay sequence. Attempt count derives from this collection. |

### Rules

- Configuration validation must make retry behavior deterministic.
- Retry delays are explicit values in seconds, not a hidden formula.
- The final number of transient retry attempts is derived from the retry-delay collection.

## Entity: OneCIntegrationApiKeyOptions

Configuration values for the first-slice machine authentication boundary.

### Fields

| Field | Description |
|-------|-------------|
| `SourceInstance` | Server-assigned identifier for the currently configured 1C infobase. |
| `ApiKey` or secure secret reference | One active API key used by 1C notification requests. |

### Rules

- First slice supports one configured source instance and one active API key.
- Source instance is persisted on every accepted request and included in idempotency.
- Simultaneous multi-infobase management, multiple active keys, and key rotation workflows are out of scope.
- API-key secrets are never logged or returned.

## Entity: OneCChangeNotification

Provider-specific request body accepted only by 1C change-notification endpoints.

### Fields

| JSON field | Required | Description |
|------------|----------|-------------|
| `Ref_Key` | Yes | External document identity. |
| `DataVersion` | Yes | Base64 source version marker. |
| `Number` | No | Optional diagnostic document number. |
| `Date` | No | Optional diagnostic source date without offset. |

### Rules

- Exact JSON field names are required.
- Invalid Base64 `DataVersion` is a contract validation failure.
- `Number` and `Date` do not participate in idempotency.
- Notification meaning is only "the external object changed"; it does not classify posting, unposting, deletion, or status transition.
