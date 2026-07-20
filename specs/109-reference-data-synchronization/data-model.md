# Data Model: Reactive and On-Demand Reference-Data Synchronization

**Feature**: `109-add-reactive-and-on-demand-reference-data-synchronization`  
**Date**: 2026-07-16

## External Import State

`ExternalImportState` is an identity-less, optional owned value object on Warehouse, Unit of Measure, and Stock Keeping Unit. It represents the aggregate's single supported 1C link and is not a separate entity or table.

| Field | Domain type | Persistence | Rules |
|---|---|---|---|
| `RefKey` | `Guid` | `ExternalRefKey uniqueidentifier NULL` | Non-empty when state exists; immutable after link; unique among non-null values within the owning reference type. |
| `DataVersion` | defensively held `byte[]?` | `ExternalDataVersion varbinary(128) NULL` | Null only for a linked legacy row whose current source version is unknown; new successful synchronization requires 1-128 bytes; equality is by content; input/output buffers are copied. |
| `ImportedAtUtc` | `DateTimeOffset` | `LastImportedAtUtc datetimeoffset NULL` | Preserved for legacy rows; refreshed when a changed or unknown version is successfully applied; unchanged for a same-version result. |

### Invariants

- Absence of `ExternalImportState` means the local record is unlinked.
- An existing state always has a non-empty `RefKey` and import time.
- Domain creation/update never writes an empty version and never creates new null-version state.
- Null `DataVersion` is accepted only when EF materializes a pre-feature linked row.
- Version ordering has no meaning; only content equality is valid.
- `HasDataVersion` compares the private buffer without exposing it.
- Any accessor returning version bytes returns a copy; any mutation stores a copy.

## Aggregate Ownership

### Warehouse

| Ownership | Fields |
|---|---|
| Source-owned after linking | `Code`, `Name`, source-controlled `IsActive`, `ExternalImportState` |
| WMS-owned | `Description` |

### Unit of Measure

| Ownership | Fields |
|---|---|
| Source-owned after linking | `Code`, `Name`, `Symbol`, source-controlled `IsActive`, `ExternalImportState` |
| WMS-owned | None in the current edit contract |

### Stock Keeping Unit

| Ownership | Fields |
|---|---|
| Source-owned after linking | `Code`, `Name`, `BaseUnitOfMeasureId`, source-controlled `IsActive`, `ExternalImportState` |
| WMS-owned | `Description` |

Normal edit contracts continue to omit external import state. Local edit rules compare normalized requested source-owned values with current values and reject only an actual difference. Import application uses the dedicated import domain path and may change source-owned values.

## Version-Aware Import Items

The existing command item types remain explicit and type-specific.

### `ImportWarehouses.Item`

- `ExternalRefKey: Guid`
- `ExternalDataVersion: byte[]`
- `Code: string?`
- `Name: string?`
- `IsDeletionMarked: bool`
- `ImportedAtUtc: DateTimeOffset`

### `ImportUnitsOfMeasure.Item`

- `ExternalRefKey: Guid`
- `ExternalDataVersion: byte[]`
- `Code: string?`
- `Name: string?`
- `Symbol: string?`
- `IsDeletionMarked: bool`
- `ImportedAtUtc: DateTimeOffset`

### `ImportStockKeepingUnits.Item`

- `ExternalRefKey: Guid`
- `ExternalDataVersion: byte[]`
- `Code: string?`
- `Name: string?`
- `BaseUnitOfMeasureExternalRefKey: Guid?`
- `IsDeletionMarked: bool`
- `ImportedAtUtc: DateTimeOffset`

All new items require a non-empty version. Source folders are removed before these command items are created, and UoM has no folder field.

## Import Result

Extend `ReferenceImportBatchResult` with:

- `Processed`
- `Created`
- `Updated`
- `Unchanged`
- `Skipped`
- `Failed`
- `Errors`

Invariant:

```text
Processed = Created + Updated + Unchanged + Skipped + Failed
```

`Updated` includes a changed/previously unknown source version even if no WMS business value changed. `Unchanged` is reserved for equal current source version and produces no data/timestamp/event mutation.

## Application State Transitions

| Local state | Current source state | Version relation | Result | Mutation/event behavior |
|---|---|---|---|---|
| Unlinked | Active, valid | N/A | Created | Create, link, store version/import time, emit normal creation behavior. |
| Unlinked | Deletion-marked | N/A | Controlled skip | No record, validation, dependency lookup, or event. |
| Linked | Any | Equal | Unchanged | No business value, active state, import/update timestamp, or event change. |
| Linked legacy | Active, valid | Unknown (`NULL`) | Updated/applied | Apply current values, store version/import time, emit only events for actual business changes. |
| Linked | Active, valid | Different | Updated/applied | Apply current values, reactivate if needed, store version/import time, emit only actual detail/reactivation events. |
| Linked active | Deletion-marked | Different/unknown | Updated/applied | Store version/import time, deactivate, emit one deactivation event. |
| Linked inactive | Deletion-marked | Different/unknown | Updated/applied | Store version/import time; no duplicate deactivation event. |
| Linked | Invalid active values or unresolved conflict | Different/unknown | Failed | No aggregate mutation is committed; existing structured error and transaction semantics are preserved. |

Deletion handling occurs before active-record detail validation and before SKU dependency resolution.
Controlled skips are limited to applicable Warehouse/SKU source folders and unlinked deletion-marked records.

## Internal Synchronize-One Result

The internal orchestration result is not persisted.

| Outcome | Meaning | Reactive handler mapping |
|---|---|---|
| `Applied` | One item was created or updated. | Existing `Completed` handler result. |
| `Unchanged` | Stored and current source versions match. | Existing `Completed` handler result. |
| `ControlledSkip` | Applicable source folder or unlinked deletion mark. | Existing `Completed` handler result. |
| `NotFound` | Current source object is absent. | `PermanentFailure` for reactive; returned directly on demand. |
| `Busy` | Same-type in-process lease is unavailable. | `TransientFailure` for reactive; returned directly on demand. |
| `TransientFailure` | Timeout or temporary source/unavailable dependency condition. | Existing `TransientFailure` handler result. |
| `PermanentFailure` | Invalid/disabled configuration, authentication rejection, unavailable entity set, malformed source data, validation failure, or unresolved conflict. | Existing `PermanentFailure` handler result. |

Cancellation is not an outcome. Direct internal on-demand caller cancellation propagates as `OperationCanceledException`. During reactive processing, `OperationCanceledException` is rethrown as shutdown cancellation only when the processor/application stopping token is cancelled; source timeout and non-shutdown failures retain their normal transient/permanent classification. No durable cancelled status is added.

## Bounded SKU Dependency Transition

```text
read current SKU
  -> dispatch one SKU item
  -> only BaseUnitOfMeasureNotImported/BaseUnitOfMeasureInactive:
       synchronize that one UoM once
       -> if active/applicable: dispatch the same SKU item once more
       -> otherwise: explicit transient/permanent failure
```

Limits:

- at most one UoM synchronize-one call;
- at most two SKU command dispatches including the initial attempt;
- no recursion or dependency graph;
- missing/empty base-UoM source key is permanent and does not trigger repair;
- manual full SKU import keeps its current batch prerequisite behavior.

## Durable Synchronization Request

Feature 104's entity and schema remain unchanged. Add only stable values to `SynchronizationEntityTypes`:

- `Warehouse`
- `UnitOfMeasure`
- `StockKeepingUnit`

The request's notification `ExternalDataVersion` remains part of durable idempotency and diagnostics. It can differ from the current version loaded during processing. Applied/unchanged/skip/not-found/busy are not durable statuses; durable status remains one of `Pending`, `Processing`, `Deferred`, `Completed`, or `Failed`.

When the processor/application stopping token is cancelled, the request stays `Processing` and `OperationCanceledException` is rethrown as shutdown cancellation; Feature 104 abandoned-processing recovery later applies its existing retry decision. Source timeout and non-shutdown failures continue through their normal classification.

## Physical Persistence Compatibility

| Table | Existing identity column/index | New column | Existing timestamp |
|---|---|---|---|
| `wms.warehouses` | `ExternalRefKey`; `UX_wms_warehouses_external_ref_key`; filter `[ExternalRefKey] IS NOT NULL` | `ExternalDataVersion varbinary(128) NULL` | `LastImportedAtUtc` |
| `wms.units_of_measure` | `ExternalRefKey`; `UX_wms_units_of_measure_external_ref_key`; filter `[ExternalRefKey] IS NOT NULL` | `ExternalDataVersion varbinary(128) NULL` | `LastImportedAtUtc` |
| `wms.stock_keeping_units` | `ExternalRefKey`; `UX_wms_stock_keeping_units_external_ref_key`; filter `[ExternalRefKey] IS NOT NULL` | `ExternalDataVersion varbinary(128) NULL` | `LastImportedAtUtc` |

Migration acceptance rule: `Up` adds only the three nullable version columns; `Down` drops only those columns. Existing column values, names, filters, and index names are not rewritten.
