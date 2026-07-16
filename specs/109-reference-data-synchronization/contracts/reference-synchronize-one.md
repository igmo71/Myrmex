# Contract: Internal Reference Synchronize-One

This is an internal integration/application contract. It is not an HTTP endpoint, WebApp action, shared public transport type, or durable lifecycle.

## Input

- `ReferenceType`: exactly Warehouse, Unit of Measure, or Stock Keeping Unit.
- `ExternalRefKey`: required non-empty GUID.
- `CancellationToken`: caller cancellation must propagate.

## Lease and Processing Boundary

1. Attempt the matching existing in-process reference-type lease without waiting.
2. If unavailable, return `Busy` without a source read.
3. Hold the lease from before the current-object source read through the corresponding import-command commit.
4. Release it on success, failure, or cancellation.
5. Do not coordinate across processes or application instances.

## Outcomes

| Outcome | Contract meaning |
|---|---|
| `Applied` | Current source state was created or applied as an update. |
| `Unchanged` | The linked aggregate already stores the current source version; nothing was mutated. |
| `ControlledSkip` | A Warehouse/SKU folder or deletion-marked unlinked source record was intentionally skipped. |
| `NotFound` | No current source object exists for the external key; no local record is created or deactivated. |
| `Busy` | The same-type lease is held in this application instance. |
| `TransientFailure` | Source timeout/unavailability or a temporary bounded-dependency condition. |
| `PermanentFailure` | Invalid or disabled configuration, authentication rejection, unavailable entity set, malformed source data, validation failure, or unresolved business conflict. |

Every non-success result contains the supported reference type, external key, stable failure category/reason, and retry suitability without credentials or unrelated source data.

`OperationCanceledException` is propagated and is not converted into any outcome.

## Reactive Handler Mapping

| Synchronize-one result | Existing Feature 104 handler result |
|---|---|
| `Applied` | `Completed` |
| `Unchanged` | `Completed` |
| Applicable `ControlledSkip` | `Completed` |
| `Busy` | `TransientFailure` |
| `TransientFailure` | `TransientFailure` |
| `NotFound` | `PermanentFailure` |
| `PermanentFailure` | `PermanentFailure` |
| Shutdown cancellation while the processor/application stopping token is cancelled | Propagate; do not return a handler result |

The processor applies existing durable statuses and retry/recovery behavior. Operation outcomes never become durable statuses.

Direct internal on-demand caller cancellation propagates to that caller. During reactive processing, `OperationCanceledException` is rethrown as shutdown cancellation only when the processor/application stopping token is cancelled. Source timeout and non-shutdown failures continue through their normal transient/permanent classification. No durable cancelled status is added.

## SKU-to-UoM Repair

- Trigger only after the first SKU application reports missing or inactive base UoM for a valid external UoM key.
- Synchronize that one UoM once.
- Apply the SKU no more than one additional time.
- A busy/temporary UoM result remains transient.
- A missing, deletion-skipped, invalid, or still-inactive UoM is an explicit permanent/business failure.
- Do not recurse or call a generalized dependency resolver.
