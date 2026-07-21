# Contract: 1C Reference Synchronization Compatibility

## Purpose

This contract preserves Feature #104/#109 notification intake, durable processing, synchronize-one outcomes, coordination, diagnostics, and SKU repair while replacing the all-reference synchronization service with three explicit slice operations.

## Notification Intake

| Reference | Method and route | Stable entity type |
|-----------|------------------|--------------------|
| Warehouse | `POST /api/integrations/1c/warehouses/changed` | `Warehouse` |
| Unit of Measure | `POST /api/integrations/1c/uoms/changed` | `UnitOfMeasure` |
| Stock Keeping Unit | `POST /api/integrations/1c/skus/changed` | `StockKeepingUnit` |

All three retain:

- existing machine authentication and authorization;
- request fields `Ref_Key`, `DataVersion`, optional `Number`, and optional `Date`;
- existing validation behavior;
- durable insert or duplicate resolution before response;
- an empty `202 Accepted` response;
- current source object as the source of truth;
- existing idempotency and diagnostic semantics.

Notification endpoints and Feature #104 request persistence are not relocated into reference slices.

## Durable Handler Boundary

Each slice retains one concrete implementation of the existing `ISynchronizationHandler` contract:

```text
EntityType
HandleAsync(SynchronizationRequest, CancellationToken)
```

Each concrete handler depends on its matching slice synchronizer, the pure common mapper, and its typed logger. Its visible flow is:

```text
parse and validate ExternalId
-> call the matching slice synchronizer
-> write structured correlation log
-> map the completed result through the pure common mapper
```

The structured log is written after the internal result is available and before durable mapping. It contains:

- `SynchronizationRequestId`;
- `EntityType`;
- `ExternalId`;
- `NotifiedDataVersion`, rendered safely and deterministically as Base64;
- `CurrentOutcome`;
- `CurrentReason`;
- `RetrySuitable`.

When `ExternalId` is invalid, the concrete handler produces and logs the equivalent permanent invalid-request result with the same correlation fields before mapping it. Credentials, secrets, and source payloads are never logged.

`ReferenceSynchronizationHandlerResultMapper` remains pure. It does not parse a request, select a reference slice, invoke a synchronization callback, or perform logging.

## Internal Synchronize-One Contract

Each slice exposes only an internal operation:

```text
SynchronizeAsync(Guid externalRefKey, CancellationToken cancellationToken)
    -> ReferenceSynchronizationResult
```

There is no public endpoint, WebApp action, generic reference selector, provider selector, or all-reference facade.

## Outcome Contract

| Internal outcome | Durable handler result | Retry suitable |
|------------------|------------------------|----------------|
| `Applied` | Completed | No |
| `Unchanged` | Completed | No |
| `ControlledSkip` | Completed | No |
| `Busy` | Transient failure | Yes |
| `TransientFailure` | Transient failure | Yes |
| `NotFound` | Permanent failure | No |
| `PermanentFailure` | Permanent failure | No |

Existing reasons and safe messages remain available, including applied/unchanged, source folder, deletion-mark skip, not found, busy, source unavailable, timeout, invalid configuration, authentication failure, entity-set failure, malformed data, validation failure, business conflict, application failure, invalid request, and both base-UoM repair reasons.

## Per-Reference Flow Invariants

### Warehouse

```text
validate request and acquire Warehouse lease
-> read current Warehouse
-> NotFound / folder ControlledSkip / source failure
-> map current record
-> dispatch ImportWarehouses.Command with one item
-> classify exact one-item result
-> log and return
```

### Unit of Measure

```text
validate request and acquire UoM lease
-> read current UoM
-> NotFound / source failure
-> map current record (no folder branch)
-> dispatch ImportUnitsOfMeasure.Command with one item
-> classify exact one-item result
-> log and return
```

### Stock Keeping Unit

```text
validate request and acquire SKU lease
-> read current SKU
-> NotFound / folder ControlledSkip / source failure
-> map current record
-> dispatch ImportStockKeepingUnits.Command with one item
-> optionally synchronize one base UoM
-> optionally retry the identical SKU item once
-> classify, log, and return
```

## Application Result Classification

- Exactly one created or updated record maps to `Applied`.
- Exactly one unchanged record maps to `Unchanged`.
- Exactly one unlinked deletion-mark skip maps to `ControlledSkip`.
- `Processed != 1` or otherwise inconsistent one-item counts map to `PermanentFailure` with reason `ApplicationFailure` and `retrySuitable = false`.
- Invalid, not-found, conflict, unauthorized, or forbidden service errors remain permanent; other service errors remain transient.
- Existing code-conflict record reasons map to permanent business conflict; other record failures map to permanent validation failure.
- Source unavailable and timeout remain transient; disabled/invalid configuration, authentication rejection, unavailable entity set, and malformed source data remain permanent.

Each slice owns this interpretation locally; no common all-reference classifier selects behavior.

Issue #111 preserves the inconsistent-accounting classification exactly. Reconsidering whether this invariant should throw or use the transient processor retry path is deferred to a separate issue.

## Coordination and Cancellation

- Empty external keys fail permanently before source access.
- Caller cancellation is checked before lease acquisition and propagates through source/application work.
- Same-type manual/reactive/internal work shares one non-waiting singleton lease.
- Busy synchronization returns retry-suitable `Busy` without waiting.
- Different reference types remain independent.
- Reactive shutdown cancellation retains Feature #104's current Processing/recovery behavior.

## SKU-to-UoM Repair Contract

Repair is eligible only when the first SKU command:

- succeeds as an application call;
- reports exactly one processed and one failed item;
- has a non-empty base-UoM external key; and
- reports `BaseUnitOfMeasureNotImported` or `BaseUnitOfMeasureInactive`.

The SKU synchronizer then calls the explicit UoM synchronizer at most once. `Busy` or `TransientFailure` maps to transient SKU repair unavailability. Any result other than `Applied` or `Unchanged` maps to permanent repair failure. Success permits exactly one retry of the same SKU item. A still-repairable retry fails permanently. No recursion or further dependency is permitted.

Minimum focused coverage remains in the existing `StockKeepingUnitReferenceRepairTests` class: successful UoM `Applied`/`Unchanged` outcomes are parameterized; the retry-still-missing/inactive case is descriptively renamed; and one compact failed-UoM theory covers `Busy`, `TransientFailure`, `NotFound`, `ControlledSkip`, and `PermanentFailure`, including one UoM call, one SKU dispatch, no retry, and no additional dependency call. No new repair test class or Feature #104 matrix is introduced.

## Durable Foundation Exclusions

The refactor does not alter request persistence, duplicate resolution, polling, wake-up, handler resolution, processing batches, retry delays, deferred handling, shutdown behavior, abandoned-processing recovery, health checks, or durable statuses.
