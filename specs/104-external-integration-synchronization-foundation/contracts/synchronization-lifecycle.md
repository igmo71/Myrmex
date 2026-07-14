# Contract: Synchronization Lifecycle

## Durable Queue Boundary

SQL persistence is the durable queue and source of truth. The synchronization request table is `integration.synchronization_requests`. The in-process channel is only a best-effort wake-up signal and never carries reliability guarantees.

The wake-up channel is a coalescing signal:

- bounded capacity 1;
- `DropWrite` when full;
- multiple writers and one reader;
- no synchronization request payload;
- after wake-up, process eligible SQL batches until no immediately eligible work remains.

Endpoint intake order:

```text
validate
-> persist
-> commit
-> best-effort Channel signal
-> empty 202 Accepted
```

## Status Values

| Status | Meaning |
|--------|---------|
| `Pending` | Eligible for processing now or after `NextAttemptAtUtc`. |
| `Processing` | Currently selected by the synchronization processor. |
| `Deferred` | No document-specific handler is registered; request remains replayable and no processing attempt has started. |
| `Completed` | A registered handler completed successfully. |
| `Failed` | Terminal technical failure after configured retries or permanent contract/processing error. |

`Superseded` is reserved for a later extension and is not implemented in the first slice.

## Startup and Polling

- Processor scans SQL immediately on application startup.
- Processor scans SQL on a configurable fallback interval.
- Wake-up signals may be lost, delayed, or processed after the HTTP response; polling still discovers committed work.
- Batch size is configurable.

## Handler Outcomes

| Outcome | Lifecycle Effect |
|---------|------------------|
| Registered handler completes | `Processing` -> `Completed` |
| No document-specific handler registered | `Pending` -> `Deferred`; do not increment `AttemptCount`, do not set `ProcessingStartedAtUtc`, and do not consume a retry delay. |
| Transient technical failure with retries remaining | attempt recorded, next retry scheduled |
| Retry delays exhausted | `Processing` -> `Failed` |
| Permanent validation or processing error | `Processing` -> `Failed` |

The first slice must not mark receiving or shipping notifications `Completed` merely because infrastructure selected them.

## Retry Configuration

The first slice uses explicit retry delays in seconds. Preliminary configuration shape:

```json
{
  "PollingIntervalSeconds": 60,
  "BatchSize": 20,
  "ProcessingAttemptTimeoutSeconds": 30,
  "ProcessingTimeoutSeconds": 300,
  "RetryDelaysSeconds": [10, 30, 120, 600, 1800, 3600, 10800]
}
```

- `AttemptCount` increments when a processing attempt starts.
- The first processing attempt has `AttemptCount = 1`.
- `N` configured retry delays permit `N + 1` total processing attempts.
- After attempt 1 fails transiently, `RetryDelaysSeconds[0]` determines the next eligibility time.
- `Deferred` unsupported-handler outcomes do not consume retry delays.

## Abandoned Processing Recovery

A request left in `Processing` beyond `ProcessingTimeoutSeconds` becomes eligible for recovery according to retry rules. This covers work left behind by application failure or restart.

## Duplicate Receipt

Duplicate HTTP notification receipt must not alter the existing lifecycle state.

| Existing Status | Duplicate Effect |
|-----------------|------------------|
| `Pending` | Preserve state; may emit best-effort wake-up signal. |
| `Processing` | Preserve state. |
| `Deferred` | Preserve state. |
| `Completed` | Preserve state. |
| `Failed` | Preserve state. |

Duplicate receipt never schedules retry, resets attempts, clears errors, restarts processing, transitions status, or acts as replay/repair.

Duplicate handling first verifies a SQL Server duplicate-key error category, then verifies the failure identifies `UX_integration_synchronization_requests_idempotency`. Unrelated persistence failures remain failures and must not return empty `202 Accepted` as though they were duplicates.
