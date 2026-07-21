# Data Model: 1C Reference Vertical Slices

## Scope

Issue #111 introduces no new persisted entity, value object, table, column, index, relationship, EF mapping, migration, or durable status. This artifact records the existing data shapes and state transitions that the refactoring must preserve while relocating their integration ownership.

## Ownership Model

```text
Feature #104 durable synchronization request
    -> reference-specific ISynchronizationHandler
        -> reference-specific synchronize-one operation
            -> reference-specific 1C source
            -> existing WMS Import*.Command

Manual import endpoint
    -> reference-specific import operation
        -> reference-specific 1C source
        -> existing WMS Import*.Command

SKU synchronize-one
    -> explicit UoM synchronize-one (at most once)
    -> retry same SKU import item (at most once)
```

The integration slice owns source retrieval, mapping, coordination, application dispatch, and outcome interpretation. The existing WMS import command owns domain and persistence behavior.

## Existing Source Record Shapes

### Warehouse Source Record

| Field | Meaning | Validation/usage |
|-------|---------|------------------|
| `Ref_Key` | Stable 1C external identity | Required and non-empty for current-object synchronization |
| `DataVersion` | Opaque source version | Required for current-object reads; length 1–128 bytes |
| `DeletionMark` | Current source lifecycle marker | Passed to the existing Warehouse import command |
| `IsFolder` | Folder/group marker | Produces a controlled skip; never dispatched as a Warehouse item |
| `Code` | Optional source code | Used only when configured as available; otherwise an uppercase compact external key is used |
| `Description` | Source Warehouse name | Trimmed and passed to the existing import command |

The Warehouse slice owns the source entity set, exact projection, optional full-import folder filter, current-object query, mapping, and folder error text.

### Unit of Measure Source Record

| Field | Meaning | Validation/usage |
|-------|---------|------------------|
| `Ref_Key` | Stable 1C external identity | Required and non-empty for current-object synchronization |
| `DataVersion` | Opaque source version | Required for current-object reads; length 1–128 bytes |
| `DeletionMark` | Current source lifecycle marker | Passed to the existing UoM import command |
| `Code` | Source code | Trimmed and passed through |
| `Description` | Fallback display value | Used only when the preferred name or symbol is empty |
| `НаименованиеПолное` | Preferred full name | First non-empty trimmed value for WMS name |
| `МеждународноеСокращение` | Preferred international abbreviation | First non-empty trimmed value for WMS symbol |

Unit of Measure has no folder field, folder filter, or folder outcome.

### Stock Keeping Unit Source Record

| Field | Meaning | Validation/usage |
|-------|---------|------------------|
| `Ref_Key` | Stable 1C external identity | Required and non-empty for current-object synchronization |
| `DataVersion` | Opaque source version | Required for current-object reads; length 1–128 bytes |
| `DeletionMark` | Current source lifecycle marker | Passed to the existing SKU import command |
| `IsFolder` | Folder/group marker | Produces a controlled skip; never dispatched as an SKU item |
| `Code` | Source SKU code | Trimmed and passed through |
| `Description` | Fallback display value | Used when the preferred full name is empty |
| `НаименованиеПолное` | Preferred full name | First non-empty trimmed value for WMS name |
| `Артикул` | Existing projected source article | Projection remains unchanged; no new WMS ownership is introduced |
| `ЕдиницаИзмерения_Key` | Base-UoM external identity | Passed to the existing SKU import command and used only for bounded repair |

The SKU slice owns stable paging by `Ref_Key`, configured page size, page offset advancement by returned count, folder accounting, per-page command dispatch, committed partial results, and repair orchestration.

## Existing WMS Application Boundaries

| Reference | Existing operation | Integration responsibility | WMS responsibility |
|-----------|--------------------|----------------------------|--------------------|
| Warehouse | `ImportWarehouses.Command` | Build one or many import items and dispatch | External identity, version, validation, create/update, lifecycle, persistence, events |
| Unit of Measure | `ImportUnitsOfMeasure.Command` | Build one or many import items and dispatch | External identity, version, name/symbol rules, lifecycle, persistence, events |
| SKU | `ImportStockKeepingUnits.Command` | Build one or many import items, dispatch, optionally repair one UoM and retry once | External identity, version, validation, base-UoM validity, lifecycle, persistence, events |

These command and item shapes do not change.

## Existing Manual Import Result

`OneCImportResponse` remains the public result with:

- reference type;
- complete/incomplete flag;
- processed, created, updated, unchanged, skipped, and failed counts;
- started and completed timestamps;
- optional operation-level error;
- bounded record-level errors (maximum 50 returned).

### Manual Import State Transitions

```text
Requested
  -> Gate unavailable -> 409 Problem Details
  -> Configuration invalid -> 400 Problem Details
  -> Import started
       -> Complete 200 OneCImportResponse
       -> Incomplete 200 OneCImportResponse
       -> Incomplete 200 Cancelled response
       -> Lease released
```

Platform authentication and authorization remain pre-operation `401/403`. Configuration validation remains integration-wide and verifies enabled state, base URL, credentials, all three entity sets, batch size, and timeout. After configuration succeeds and source processing begins, 1C authentication rejection, entity-set unavailability, malformed response, source unavailability, source timeout, and unexpected application/batch failure remain incomplete `200 OK OneCImportResponse` results with the existing safe `OperationError`. They do not become `502/504` Problem Details on manual-import routes. The connection-test endpoint separately retains transport-failure Problem Details.

For Warehouse and UoM, a failed single WMS batch contributes no pending counts. For SKU, every successfully committed page contributes durable counts; later failure or cancellation returns those prior committed counts and errors.

## Existing Internal Synchronization Result

`ReferenceSynchronizationResult` retains:

- reference type;
- external reference key;
- outcome;
- optional reason;
- optional message;
- retry-suitable flag;
- safe diagnostic rendering.

Allowed outcomes remain:

| Outcome | Meaning | Durable handler mapping |
|---------|---------|-------------------------|
| `Applied` | One current source object created or updated | Completed |
| `Unchanged` | Stored source version is already current | Completed |
| `ControlledSkip` | Expected non-error skip such as folder or unlinked deletion | Completed |
| `NotFound` | Current source object does not exist | Permanent failure |
| `Busy` | Same reference type is already active in this process | Transient failure |
| `TransientFailure` | Retry-suitable source or application failure | Transient failure |
| `PermanentFailure` | Invalid configuration/data or non-retryable business/application failure | Permanent failure |

No outcome is added, removed, or persisted as a new durable status.

### Inconsistent One-Item Accounting

The current invariant remains unchanged:

```text
Processed != 1 or inconsistent counts
-> PermanentFailure
-> ApplicationFailure
-> retrySuitable = false
```

Issue #111 does not throw or route this condition through transient processor retry. Reconsidering that classification is deferred to a separate issue.

## Existing Durable Synchronization Request

Feature #104 remains the owner of source identity, entity type, external identity/version, trigger, status, timing, attempts, retry scheduling, last error, polling, wake-up, deferred handling, and abandoned-processing recovery.

Stable entity-type values remain:

- `Warehouse`
- `UnitOfMeasure`
- `StockKeepingUnit`

Durable lifecycle states remain `Pending`, `Processing`, `Deferred`, `Completed`, and `Failed`.

## Handler Correlation Record

Each concrete reference handler emits one structured correlation log after receiving the internal result and before mapping it to the Feature #104 handler result. The log contains:

- `SynchronizationRequestId`;
- `EntityType`;
- `ExternalId`;
- `NotifiedDataVersion`, rendered safely and deterministically as Base64;
- `CurrentOutcome`;
- `CurrentReason`;
- `RetrySuitable`.

When `ExternalId` is invalid, the handler logs the equivalent permanent invalid-request outcome before mapping it. Credentials, secrets, and source payloads are never included. This is diagnostic output, not a persisted entity or schema change. The common result mapper does not parse requests, select a slice, invoke callbacks, or log.

## Coordination Model

One singleton gate owns exactly three independent leases: Warehouse, Unit of Measure, and SKU.

- Manual import acquisition is non-waiting and throws the existing already-in-progress exception.
- Synchronize-one acquisition is non-waiting and returns `Busy` when unavailable.
- Same-type manual, reactive, and internal operations share a lease.
- Different reference types may proceed concurrently.
- SKU repair holds the SKU lease while attempting the independent UoM lease.

No cross-process or distributed coordination is introduced.

## SKU Repair State Transition

```text
Apply SKU once
  -> Applied/Unchanged/other final result -> return mapped result
  -> Exactly one failed item with missing/inactive base UoM
       -> Synchronize that one UoM once
            -> Busy/TransientFailure -> transient SKU failure
            -> NotFound/ControlledSkip/PermanentFailure -> permanent SKU repair failure
            -> Applied/Unchanged -> apply identical SKU item once more
                 -> Still repair-eligible -> permanent SKU repair failure
                 -> Otherwise -> return mapped final result
```

The SKU is applied at most twice, one UoM is synchronized at most once, and no recursive dependency state exists.

## Persistence Impact

None. Existing `ExternalImportState`, source versions, external keys, timestamps, WMS tables, integration synchronization requests, mappings, migrations, and model snapshots remain unchanged.
