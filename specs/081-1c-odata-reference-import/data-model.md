# Data Model: 1C OData Reference Import MVP

## Model Boundary

Persistent WMS entities remain in `Myrmex.Modules.Wms`. OneC transport DTOs and options remain in `Myrmex.Integrations.OneC`. Public API responses remain in `Myrmex.Shared.Integrations.OneC`. Neutral WMS batch command items cross only the in-process integration-to-WMS boundary and are not persistent entities or public HTTP contracts.

The deployment has one primary 1C reference-data source. No `ExternalSystem` discriminator or multiple-source relationship is introduced.

## Persistent WMS Entities

### Warehouse

Existing fields remain authoritative except where source ownership is stated.

| Field | Shape | Rules |
|---|---|---|
| `Id` | `Guid` | Existing Myrmex identity and primary key. |
| `Code` | `string` | Required, normalized, existing maximum length, globally unique. Source-owned after linkage. |
| `Name` | `string` | Required, normalized, existing maximum length. Source-owned after linkage. |
| `Description` | nullable string | Existing local optional field; preserved because the selected 1C warehouse projection has no equivalent. |
| `IsActive` | `bool` | Existing lifecycle flag; aligned with source deletion intent after linkage. |
| `ExternalRefKey` | nullable `Guid` | Immutable imported identity; unique when non-null. Never named `Ref_Key` in WMS. |
| `LastImportedAtUtc` | nullable `DateTimeOffset` | Set only after this record is processed successfully by an import. |
| Existing audit fields | existing shapes | Continue to follow existing aggregate behavior. |

Persistence additions:

- Nullable `ExternalRefKey` column.
- Nullable `LastImportedAtUtc` column.
- Unique filtered index `UX_wms_warehouses_external_ref_key` with filter `[ExternalRefKey] IS NOT NULL`.
- Existing unique code index remains unchanged.

### UnitOfMeasure

| Field | Shape | Rules |
|---|---|---|
| `Id` | `Guid` | Existing Myrmex identity and primary key. |
| `Code` | `string` | Required, normalized, existing maximum length, globally unique. Source-owned after linkage. |
| `Name` | `string` | Required, normalized, existing maximum length. Source-owned after linkage. |
| `Symbol` | nullable string | Existing optional field; preserve on update unless an approved source symbol is mapped. New imported records may have null. |
| `IsActive` | `bool` | Existing lifecycle flag; aligned with source deletion intent after linkage. |
| `ExternalRefKey` | nullable `Guid` | Immutable imported identity; unique when non-null. |
| `LastImportedAtUtc` | nullable `DateTimeOffset` | Latest successful import observation. |
| Existing audit fields | existing shapes | Unchanged. |

Persistence additions:

- Nullable `ExternalRefKey` column.
- Nullable `LastImportedAtUtc` column.
- Unique filtered index `UX_wms_units_of_measure_external_ref_key` with filter `[ExternalRefKey] IS NOT NULL`.
- Existing unique code index remains unchanged.

### StockKeepingUnit

| Field | Shape | Rules |
|---|---|---|
| `Id` | `Guid` | Existing Myrmex identity and primary key. |
| `Code` | `string` | Required, normalized, existing maximum length, globally unique. Source-owned after linkage. |
| `Name` | `string` | Required, normalized, existing maximum length. Mapped from 1C `Description`; source-owned after linkage. |
| `Description` | nullable string | Existing optional field; preserved on update. `Артикул` is not written here. |
| `BaseUnitOfMeasureId` | `Guid` | Existing required relationship to one active UoM when assigned by import. |
| `IsActive` | `bool` | Existing lifecycle flag; aligned with source deletion intent after linkage. |
| `ExternalRefKey` | nullable `Guid` | Immutable imported identity; unique when non-null. |
| `LastImportedAtUtc` | nullable `DateTimeOffset` | Latest successful import observation. |
| Existing audit fields | existing shapes | Unchanged. |

Relationships and persistence additions:

- Existing required many-to-one relationship to `UnitOfMeasure` remains.
- Nullable `ExternalRefKey` column.
- Nullable `LastImportedAtUtc` column.
- Unique filtered index `UX_wms_stock_keeping_units_external_ref_key` with filter `[ExternalRefKey] IS NOT NULL`.
- Existing code and base-UoM indexes remain unchanged.

## Identity and Upsert Rules

For each reference type, normalize and validate source values using existing WMS rules, then apply these rules in order:

1. Empty `ExternalRefKey` is a record failure.
2. If a record with the same `ExternalRefKey` exists, that record is the update target.
3. If no identity match exists and `DeletionMark=true`, skip without creating.
4. If no identity match exists and normalized `Code` belongs to any local record, skip as `CodeAlreadyExistsWithoutExternalRefKey`; do not attach the local record.
5. Otherwise create a new linked record through the existing domain factory and set import metadata.
6. For an identity match, if the incoming normalized code belongs to another record, skip as a code conflict and leave the linked record unchanged.
7. A successful non-deleted import applies source-owned fields and reactivates an inactive linked record.
8. A successful deleted import deactivates the linked record.
9. Update `LastImportedAtUtc` only for successful create/update/reactivation/deactivation/unchanged observations.
10. Do not physically delete, reassign `ExternalRefKey`, or infer identity from code.

The three current entity types all implement `IActivatable`. The generic `DeletionNotSupported` record error remains available for future neutral command reuse, but is not expected for this MVP's three handlers.

## Lifecycle Transitions

| Current state | Source input | Result | Count |
|---|---|---|---|
| No linked record | Valid, `DeletionMark=false`, unused code | Create active linked record | Created |
| No linked record | `DeletionMark=true` | No record created | Skipped |
| No linked record | Valid, code already used locally | Local record unchanged | Skipped |
| Linked active/inactive record | Valid, `DeletionMark=false`, no code conflict | Apply source fields, ensure active, refresh import time | Updated |
| Linked active/inactive record | `DeletionMark=true`, inactivity supported | Ensure inactive, refresh import time | Updated |
| Linked record | Invalid values or code collision with another record | Record unchanged | Failed or Skipped according to stable reason |
| Linked record | Inactivity unsupported | Record unchanged | Failed |

An unchanged but valid linked source record counts as updated because the successful observation refreshes `LastImportedAtUtc`.

## OneC Integration Configuration

Configuration is not persisted in WMS tables.

| Option | Shape | Validation and purpose |
|---|---|---|
| `Enabled` | `bool` | Disabled operations return a configuration error. |
| `BaseUrl` | URI/string | Required when enabled; points to the configured 1C OData publication root. |
| `Username` | string | Required when enabled; never returned or logged. |
| `Password` | secret string | Required from user secrets, environment variables, deployment secrets, or another secure provider. |
| `WarehousesEntitySet` | string | Required collection name. |
| `UnitsOfMeasureEntitySet` | string | Required collection name. |
| `NomenclatureEntitySet` | string | Required collection name. |
| `BatchSize` | integer | Default 1,000; range 1–5,000. Applies to nomenclature. |
| `TimeoutSeconds` | integer | Default 30; positive per-OData-request timeout. |
| `DefaultSkuBaseUnitOfMeasureExternalRefKey` | `Guid` | Required for SKU import; resolved in WMS to an active imported UoM. |

Configuration validation occurs per action so a disabled or incomplete integration can still produce a clear page/API error without preventing application startup.

## OneC Transport DTOs

All types are private/internal to `Myrmex.Integrations.OneC.Transport`.

### OData Collection Envelope

- `Value`: collection of typed source records, mapped from JSON `value`.
- Unknown OData metadata properties are ignored.
- A missing/null `value` collection is a malformed-response operation error.

### Warehouse Source Record

- `Guid Ref_Key`
- `bool DeletionMark`
- nullable `string Code`
- nullable `string Description`

### Unit-of-Measure Source Record

- `Guid Ref_Key`
- `bool DeletionMark`
- nullable `string Code`
- nullable `string Description`
- Optional source symbol field only if the target publication supplies a verified mapping; it is not required by the base contract.

### Nomenclature Source Record

- `Guid Ref_Key`
- `bool DeletionMark`
- nullable `string Code`
- nullable `string Description`
- nullable `string Артикул` may be deserialized for source compatibility but is not persisted in this MVP.

## Neutral WMS Batch Command Models

These public in-process types live in `Myrmex.Modules.Wms`; they contain no OData names.

### ImportWarehouses.Item

- `Guid ExternalRefKey`
- nullable `string Code`
- nullable `string Name`
- `bool IsDeletionMarked`
- `DateTimeOffset ImportedAtUtc`

### ImportUnitsOfMeasure.Item

- `Guid ExternalRefKey`
- nullable `string Code`
- nullable `string Name`
- nullable `string Symbol`
- `bool IsDeletionMarked`
- `DateTimeOffset ImportedAtUtc`

### ImportStockKeepingUnits.Item

- `Guid ExternalRefKey`
- nullable `string Code`
- nullable `string Name`
- `Guid BaseUnitOfMeasureExternalRefKey`
- `bool IsDeletionMarked`
- `DateTimeOffset ImportedAtUtc`

### ReferenceImportBatchResult

- `Processed`, `Created`, `Updated`, `Skipped`, `Failed`: counts for the committed batch.
- `Errors`: record errors for that batch before the public 50-error cap.
- Invariant: `Processed = Created + Updated + Skipped + Failed`.
- The result exists only after the batch transaction commits.

### ReferenceImportRecordError

- nullable `Guid ExternalRefKey`
- nullable `string Code`
- stable `Reason`
- safe `Message`

Stable record reasons include:

- `InvalidSourceRecord`
- `CodeAlreadyExistsWithoutExternalRefKey`
- `CodeAlreadyUsedByAnotherRecord`
- `BaseUnitOfMeasureNotFound`
- `BaseUnitOfMeasureInactive`
- `DeletionNotSupported`

## Public API Response Models

Public records live in `Myrmex.Shared.Integrations.OneC` and contain no domain or OData types.

### OneCConnectionTestResponse

- `CheckedAtUtc`
- `IsReady=true` on the success response
- `CheckedReferenceTypes`: warehouses, units of measure, SKUs

Connection failures use ProblemDetails with a stable code and safe detail.

### OneCImportResponse

- `ReferenceType`: `warehouses`, `uoms`, or `skus`
- `IsComplete`
- `Processed`, `Created`, `Updated`, `Skipped`, `Failed`
- `StartedAtUtc`, `CompletedAtUtc`
- nullable `OperationError`
- `Errors`: first 50 record errors across committed batches

For complete and incomplete responses:

- `Processed = Created + Updated + Skipped + Failed`.
- Counts contain only completed committed batches.
- `IsComplete=false` requires an operation error.
- `IsComplete=true` requires no operation error.

### OneCImportOperationError

- stable `Reason`
- safe `Message`

Operation reasons include `AuthenticationFailed`, `SourceUnavailable`, `EntitySetUnavailable`, `MalformedResponse`, `Timeout`, `StableOrderingUnsupported`, `BatchCommitFailed`, and `Cancelled` when a response can still be delivered.

## Concurrency Model

- Gate key: reference type (`warehouses`, `uoms`, `skus`).
- Capacity: one running import per key per process.
- Acquisition: non-waiting; failure produces `409 OneCImport.AlreadyInProgress` before source access.
- Release: guaranteed in `finally` after completion, error, timeout, or cancellation.
- Different keys may run concurrently.
- The gate is not persisted and is not distributed. Deployment must use one API instance for the MVP.

Database unique indexes remain the final source-identity and code race protection.

## Batch Transaction Model

1. The adapter fetches and maps one source batch.
2. WMS validates and prepares all per-record outcomes.
3. WMS opens one explicit database transaction.
4. Accepted mutations are tracked and saved once.
5. Existing domain events are dispatched within the transaction boundary.
6. WMS commits and returns the batch result.
7. The adapter aggregates the committed result.
8. If steps 3–6 fail, WMS rolls back and no result from that batch contributes to public counts.
9. Previously committed batches remain durable and can be safely revisited by an idempotent rerun.

## Migration Scope

A developer-generated WMS migration must:

- add six nullable columns across the three existing tables;
- add three filtered unique indexes;
- preserve all existing local rows with null import metadata;
- update the model snapshot;
- avoid new tables, `ExternalSystem`, seed data, or changes to inventory tables.
