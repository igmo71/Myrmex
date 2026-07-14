# Contract: Synchronization Lifecycle

## Durable Queue Boundary

SQL persistence is the durable queue and source of truth. The in-process channel is only a best-effort wake-up signal and never carries reliability guarantees.

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
| `Deferred` | Infrastructure processed the request but no document-specific handler is registered; request remains replayable. |
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
| No document-specific handler registered | `Processing` -> `Deferred` |
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
  "RequestTimeoutSeconds": 30,
  "ProcessingTimeoutSeconds": 300,
  "RetryDelaysSeconds": [10, 30, 120, 600, 1800, 3600, 10800]
}
```

The final number of attempts is derived from `RetryDelaysSeconds`.

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
