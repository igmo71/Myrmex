# Contract: Synchronization Lifecycle and Processor Behavior

This contract describes internal observable behavior that implementation and tests must preserve. It is not a public HTTP API.

## Lifecycle States

| State | Meaning |
|-------|---------|
| `Pending` | Eligible for processing now or after `NextAttemptAtUtc`. |
| `Processing` | Claimed by one processor instance. |
| `Deferred` | Processed by infrastructure but no document-specific handler is registered; remains replayable later. |
| `Completed` | A registered handler completed successfully. |
| `Failed` | Terminal technical failure after configured retries or permanent validation/processing error. |

`Superseded` is reserved for a future extension and is not implemented in Issue #104.

## Intake Sequence

```text
validate caller and request
-> persist synchronization request or detect duplicate
-> commit durable transaction
-> send best-effort wake-up signal
-> return empty 202 Accepted
```

The wake-up signal has no reliability guarantee and is not required to happen before the HTTP response completes.

## Processing Sequence

1. Startup scan runs when the application starts.
2. Fallback polling scans at the configured interval.
3. Each scan selects available requests in configurable batches.
4. A request is claimed by changing it to `Processing` with an application-managed concurrency value.
5. Only one application instance can hold a successful claim for a request at a time.
6. Handler outcome determines the durable status transition.

## Handler Outcomes

| Condition | Required outcome |
|-----------|------------------|
| Registered handler succeeds | `Completed` |
| No handler registered | `Deferred` |
| Transient technical failure and retries remain | `Pending` with next attempt scheduled |
| Transient technical failure and retries exhausted | `Failed` |
| Permanent validation or processing failure | `Failed` |

First-slice receiving and shipping requests must not become `Completed` merely because the processor recognized their entity type.

## Recovery

`Processing` records older than the configured processing timeout become eligible for recovery. Recovery must preserve multi-instance safety and must not allow concurrent duplicate processing.

## Retry Scheduling

Retry delays are configured as explicit seconds. The number of transient retry opportunities derives from the retry-delay collection.

Retry scheduling records:

- incremented attempt count;
- next attempt time when retries remain;
- bounded last error details;
- terminal failed state when retries are exhausted.

## Replay and Retention Scope

Issue #104 preserves information required for later controlled replay of `Deferred` and `Failed` requests, but does not implement:

- replay endpoint;
- replay UI;
- scheduled replay;
- administrative replay command.

Issue #104 does not automatically delete, archive, or clean up `Completed`, `Deferred`, or `Failed` requests.
