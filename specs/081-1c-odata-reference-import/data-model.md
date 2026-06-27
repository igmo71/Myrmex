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
| `Code` | `string` | Required, normalized, existing maximum length, globally unique. Use trimmed source code when available; otherwise use uppercase `ExternalRefKey` in 32-character `N` format. Source-owned after linkage. |
| `Name` | `string` | Required, normalized, existing maximum length. Mapped from 1C `Description`; source-owned after linkage. |
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
| `Name` | `string` | Required, normalized, existing maximum length. Mapped from non-empty `НаименованиеПолное`, otherwise `Description`; source-owned after linkage. |
| `Symbol` | nullable string | Mapped from non-empty `МеждународноеСокращение`, otherwise `Description`; source-owned after linkage. |
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
| `Name` | `string` | Required, normalized, existing maximum length. Mapped from non-empty 1C `НаименованиеПолное`, otherwise `Description`; source-owned after linkage. |
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

1. For warehouse/nomenclature source records, `IsFolder=true` is a `SourceFolder` skip before WMS upsert mapping. When the source supports `$filter=IsFolder eq false`, the source excludes these records instead.
2. Empty `ExternalRefKey` is a record failure.
3. For non-deletion-marked records, trim source `Code`. Warehouse alone uses `ExternalRefKey.ToString("N").ToUpperInvariant()` when source code is unavailable/empty. UoM and SKU empty codes fail validation. Linked deletion-marked records bypass code/detail validation and preserve existing values.
4. For non-deletion-marked SKU records, validate nullable `BaseUnitOfMeasureExternalRefKey`: null/empty fails as `BaseUnitOfMeasureExternalRefKeyMissing`, no imported UoM match fails as `BaseUnitOfMeasureNotImported`, and an inactive match fails as `BaseUnitOfMeasureInactive`. Linked deletion-marked SKUs bypass base-UoM resolution and preserve the existing relationship.
5. If a record with the same `ExternalRefKey` exists, that record is the update target.
6. If no identity match exists and `DeletionMark=true`, skip without creating and report `SourceRecordDeletionMarked`.
7. If no identity match exists and normalized `Code` belongs to any local record, skip as `CodeAlreadyExistsWithoutExternalRefKey`; do not attach the local record.
8. Otherwise create a new linked record through the existing domain factory and set import metadata.
9. For an identity match, if the incoming normalized code belongs to another record, skip as a code conflict and leave the linked record unchanged.
10. A successful non-deleted import applies source-owned fields and reactivates an inactive linked record.
11. A successful deleted import deactivates the linked record.
12. Update `LastImportedAtUtc` only for successful create/update/reactivation/deactivation/unchanged observations.
13. Do not physically delete, reassign `ExternalRefKey`, or infer identity/base UoM from code.

The three current entity types all implement `IActivatable`. The generic `DeletionNotSupported` record error remains available for future neutral command reuse, but is not expected for this MVP's three handlers.

## Lifecycle Transitions

| Current state | Source input | Result | Count |
|---|---|---|---|
| Any | `IsFolder=true` from warehouse/nomenclature without source filtering | No WMS item or entity mutation | Skipped (`SourceFolder`) |
| No linked record | Valid, `DeletionMark=false`, unused code | Create active linked record | Created |
| No linked record | `DeletionMark=true` | No record created | Skipped (`SourceRecordDeletionMarked`) |
| No linked record | Valid, code already used locally | Local record unchanged | Skipped |
| Linked active/inactive record | Valid, `DeletionMark=false`, no code conflict | Apply source fields, ensure active, refresh import time | Updated |
| Linked active/inactive record | `DeletionMark=true`, inactivity supported | Preserve source-owned details, bypass their validation, ensure inactive, refresh import time | Updated |
| Linked record | Invalid values or code collision with another record | Record unchanged | Failed or Skipped according to stable reason |
| Linked record | Inactivity unsupported | Record unchanged | Failed |
| Any SKU state | Missing/empty, not-imported, or inactive `ЕдиницаИзмерения_Key` | SKU unchanged/not created; other records continue | Failed with stable base-UoM reason |

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
| `UnitsOfMeasureEntitySet` | string | Required collection name; target value is `Catalog_УпаковкиЕдиницыИзмерения`. |
| `NomenclatureEntitySet` | string | Required collection name. |
| `WarehouseCodeAvailable` | `bool` | Controls whether warehouse `$select` requests `Code`; if false, every warehouse code uses the deterministic `Ref_Key` fallback. |
| `UseFolderFilter` | `bool` | Prefer `$filter=IsFolder eq false` for warehouse/nomenclature; disable only for a publication that does not support it. |
| `BatchSize` | integer | Default 1,000; range 1–5,000. Applies to nomenclature. |
| `TimeoutSeconds` | integer | Default 30; positive per-OData-request timeout. |

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
- `bool IsFolder`
- nullable `string Code`
- nullable `string Description`

If source `Code` is configured unavailable, omit it from `$select` and use `Ref_Key.ToString("N").ToUpperInvariant()` as the mapped warehouse code. If it is selected but blank, use the same fallback. This fallback is not shared with other reference types.

### Unit-of-Measure Source Record

- `Guid Ref_Key`
- `bool DeletionMark`
- nullable `string Code`
- nullable `string Description`
- nullable `string НаименованиеПолное`
- nullable `string МеждународноеСокращение`

Source entity set: `Catalog_УпаковкиЕдиницыИзмерения`.

### Nomenclature Source Record

- `Guid Ref_Key`
- `bool DeletionMark`
- `bool IsFolder`
- nullable `string Code`
- nullable `string Description`
- nullable `string НаименованиеПолное`
- nullable `string Артикул` is transport-only and is not persisted in this MVP.
- nullable `Guid ЕдиницаИзмерения_Key` supplies the SKU's base-UoM external identity.

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
- nullable `Guid BaseUnitOfMeasureExternalRefKey`
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
- `SourceFolder`
- `SourceRecordDeletionMarked`
- `CodeAlreadyExistsWithoutExternalRefKey`
- `CodeAlreadyUsedByAnotherRecord`
- `BaseUnitOfMeasureExternalRefKeyMissing`
- `BaseUnitOfMeasureNotImported`
- `BaseUnitOfMeasureInactive`
- `DeletionNotSupported`

`SourceFolder` is produced by the OneC mapping layer before a neutral WMS item is created. `SourceRecordDeletionMarked` is produced by WMS when deletion intent has no linked record. Other reasons are produced by WMS validation/upsert. The orchestrator merges both sources into the public error list only after the corresponding source batch completes successfully; a later WMS batch failure discards that batch's pending folder skips along with all other uncommitted-batch counts.

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

1. The adapter fetches one source batch and records mapping-level folder skips while mapping non-folder records to WMS items.
2. WMS validates and prepares all item outcomes.
3. WMS opens one explicit database transaction.
4. Accepted mutations are tracked and saved once.
5. Existing domain events are dispatched within the transaction boundary.
6. WMS commits and returns the batch result. A source batch containing only folder skips completes without a database mutation.
7. The adapter aggregates the committed WMS result plus that source batch's pending folder skips.
8. If steps 3–6 fail, WMS rolls back and neither WMS outcomes nor mapping skips from that batch contribute to public counts.
9. Previously committed batches remain durable and can be safely revisited by an idempotent rerun.

## Migration Scope

A developer-generated WMS migration must:

- add six nullable columns across the three existing tables;
- add three filtered unique indexes;
- preserve all existing local rows with null import metadata;
- update the model snapshot;
- avoid new tables, `ExternalSystem`, seed data, or changes to inventory tables.
