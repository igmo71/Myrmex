# 1C OData Reference Import MVP

## Summary

Implement a basic user-triggered integration with 1C via OData so Myrmex can import core reference data directly from a configured 1C publication.

The feature is intended to support early demonstrations and pilot usage by allowing users to load familiar business data from 1C into Myrmex.

Initial scope includes:

* warehouses;
* units of measure;
* stock keeping units / nomenclature.

The import is triggered manually from the Myrmex UI. It is not a scheduled synchronization and it is not a full bidirectional exchange.

## Business Context

Potential Myrmex users already maintain master data in 1C. For early demonstrations, pilot testing, and user acceptance, it is important that Myrmex operates on recognizable data instead of artificial demo records.

Users should be able to open Myrmex, navigate to the 1C integration page, click an import button, and see warehouses, units of measure, and nomenclature loaded from 1C.

This allows Myrmex to demonstrate a real integration path while keeping the initial implementation intentionally narrow.

## Goals

The feature must provide a basic but real online integration with 1C OData.

Primary goals:

1. Connect Myrmex to a configured 1C OData endpoint.
2. Import warehouses from 1C.
3. Import units of measure from 1C.
4. Import nomenclature / SKU records from 1C.
5. Support batched import for large nomenclature datasets.
6. Update existing Myrmex records idempotently using 1C `Ref_Key`.
7. Show a clear import summary to the user.
8. Keep 1C-specific DTOs and field names isolated inside the integration boundary.

## Non-Goals

This feature does not include:

* bidirectional exchange with 1C;
* scheduled background synchronization;
* RabbitMQ/outbox integration;
* receiving documents import;
* customer orders import;
* inventory balances import;
* prices import;
* realtime synchronization;
* mapping administration UI;
* conflict-resolution UI;
* warehouse-level permissions;
* full Myrmex localization;
* authentication/authorization baseline implementation, except for requiring an appropriate import policy where available;
* refactoring inventory accounting handlers;
* event-driven inventory accounting changes.

## User Scenarios

### Scenario 1: Test 1C connection

A user opens the 1C integration page and clicks “Проверить подключение”.

Myrmex attempts to connect to the configured 1C OData endpoint and returns a clear result.

Expected outcomes:

* success message when 1C is reachable and credentials are valid;
* clear error message when 1C is unavailable;
* clear error message when credentials are invalid;
* clear error message when required OData entity sets are unavailable.

### Scenario 2: Import warehouses

A user clicks “Загрузить склады из 1С”.

Myrmex fetches warehouse records from the configured 1C OData entity set and imports them into the WMS warehouse reference data.

Expected outcomes:

* new warehouses are created;
* existing warehouses with the same `ExternalRefKey` are updated;
* records marked for deletion in 1C are marked inactive in Myrmex where supported;
* conflicts are reported but do not stop the entire import;
* the UI shows processed, created, updated, skipped, and failed counts.

### Scenario 3: Import units of measure

A user clicks “Загрузить единицы измерения из 1С”.

Myrmex fetches units of measure from the configured 1C OData entity set and imports them into WMS reference data.

Expected outcomes:

* new units of measure are created;
* existing units with the same `ExternalRefKey` are updated;
* deleted records in 1C are marked inactive in Myrmex where supported;
* the UI shows an import summary.

### Scenario 4: Import nomenclature / SKU

A user clicks “Загрузить номенклатуру из 1С”.

Myrmex fetches nomenclature records from 1C using OData paging.

Expected outcomes:

* import supports more than 15,000 nomenclature records;
* import uses deterministic OData ordering with `$orderby=Ref_Key` when using `$top`/`$skip`;
* import uses `$select` to fetch only required fields;
* new SKU records are created;
* existing SKU records with the same `ExternalRefKey` are updated;
* code conflicts with local records are skipped and reported;
* the UI shows a summary after completion.

## Functional Requirements

### FR-001: Integration project

Create a separate integration project:

```text
src/Myrmex.Integrations/Myrmex.Integrations.csproj
```

The project must contain integration adapters and transport-specific DTOs.

C# naming convention:

```text
OneC
```

User-facing label:

```text
1С
```

HTTP route segment:

```text
/api/integrations/1c
```

### FR-002: Project dependencies

`Myrmex.Integrations` may depend on `Myrmex.Modules.Wms`.

`Myrmex.Modules.Wms` must not depend on `Myrmex.Integrations`.

The WMS module owns neutral import use cases and upsert behavior. The 1C integration adapter fetches 1C data and maps it into WMS import command items.

Expected dependency direction:

```text
Myrmex.Integrations -> Myrmex.Modules.Wms
Myrmex.ApiService -> Myrmex.Integrations
```

### FR-003: 1C OData DTO isolation

1C OData DTOs must live inside the integration project.

1C DTOs should keep 1C/OData names where practical, including names such as:

```csharp
Catalog_Номенклатура
Catalog_Склады
Ref_Key
DeletionMark
Code
Description
Артикул
```

1C DTOs must not leak into WMS domain entities or public Myrmex application contracts.

### FR-004: WMS import command items

The WMS module must expose import use cases or command contracts for imported reference data.

Example structure:

```text
ImportWarehouses
ImportUnitsOfMeasure
ImportStockKeepingUnits
```

Each import use case may define nested command item records, for example:

```csharp
ImportStockKeepingUnits.Item
```

These command items are not domain entities. They represent input data for the WMS import use case.

### FR-005: External reference key

1C `Ref_Key` must be represented as `Guid` in 1C DTOs.

Myrmex reference entities imported from 1C should store:

```csharp
Guid? ExternalRefKey
DateTimeOffset? LastImportedAtUtc
```

This applies to:

* warehouse;
* stock keeping unit;
* unit of measure.

`Ref_Key` must not be used as a property name in Myrmex domain entities. Myrmex domain/application code must use `ExternalRefKey`.

### FR-006: Idempotent upsert

Import must be idempotent.

For each imported record:

1. If `ExternalRefKey` exists in Myrmex, update the existing record.
2. If `ExternalRefKey` does not exist and `Code` is not used, create a new record.
3. If `ExternalRefKey` does not exist and `Code` is already used by a local record without `ExternalRefKey`, skip the record as a conflict.
4. If `DeletionMark = true`, mark the Myrmex record inactive where supported.
5. Do not physically delete Myrmex records during import.

### FR-007: Conflict behavior

Myrmex must not automatically link an imported 1C record to an existing local record by `Code`.

If an imported record has unknown `ExternalRefKey` but its `Code` already exists locally, the record must be skipped and reported as a conflict.

Example reason:

```text
CodeAlreadyExistsWithoutExternalRefKey
```

### FR-008: Separate import operations

The import operations must be separated by reference type.

Required endpoints:

```text
POST /api/integrations/1c/connection/test
POST /api/integrations/1c/warehouses/import
POST /api/integrations/1c/uoms/import
POST /api/integrations/1c/skus/import
```

A combined “import all” operation is not required in this MVP.

### FR-009: Warehouses import

Warehouses may be imported in a single operation.

Expected 1C fields:

```text
Ref_Key
DeletionMark
Code
Description
```

Suggested OData usage:

```text
$format=json
$orderby=Ref_Key
$select=Ref_Key,DeletionMark,Code,Description
```

### FR-010: Units of measure import

Units of measure may be imported in a single operation.

Expected 1C fields:

```text
Ref_Key
DeletionMark
Code
Description
```

If a short name or symbol field is available and already maps cleanly to the Myrmex unit of measure model, it may be used.

Suggested OData usage:

```text
$format=json
$orderby=Ref_Key
$select=Ref_Key,DeletionMark,Code,Description
```

### FR-011: Nomenclature / SKU import

Nomenclature import must support batching/paging.

Expected 1C fields:

```text
Ref_Key
DeletionMark
Code
Description
Артикул
```

Suggested OData usage:

```text
$format=json
$orderby=Ref_Key
$skip=<offset>
$top=<batch-size>
$select=Ref_Key,DeletionMark,Code,Description,Артикул
```

The default order is ascending. Explicit `asc` is not required.

If the configured 1C endpoint does not support `$orderby=Ref_Key`, the implementation must report a clear configuration/compatibility error or use a documented stable fallback.

### FR-012: Import result

Each import operation must return a summary.

Suggested response shape:

```csharp
public sealed record OneCImportResponse(
    string ReferenceType,
    int Processed,
    int Created,
    int Updated,
    int Skipped,
    int Failed,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<OneCImportRecordError> Errors);
```

Suggested error shape:

```csharp
public sealed record OneCImportRecordError(
    string? ExternalRefKey,
    string? Code,
    string Reason,
    string Message);
```

The response should limit returned per-record errors to a reasonable number, for example the first 50 errors.

### FR-013: Configuration

1C connection settings must be configurable.

Required options:

```text
Enabled
BaseUrl
Username
Password / secret reference
WarehousesEntitySet
NomenclatureEntitySet
UnitsOfMeasureEntitySet
BatchSize
Timeout
```

Credentials must not be committed to repository files.

Credentials must come from one of:

* user secrets;
* environment variables;
* deployment secrets;
* another secure configuration provider.

1C credentials must not be accepted from the public import request body.

### FR-014: UI page

Add a Myrmex WebApp page or section for 1C integration.

Suggested location:

```text
Интеграции → 1С
```

The page should provide separate actions:

```text
Проверить подключение
Загрузить склады из 1С
Загрузить единицы измерения из 1С
Загрузить номенклатуру из 1С
```

The page must show the latest import result after each operation.

User-facing text on this page should be Russian.

Full Myrmex localization is out of scope for this feature. If an existing localization/resource pattern is already present, use it for new labels. Otherwise, keep localization changes minimal and limited to this page.

### FR-015: Authorization

Import endpoints must require authorization.

Preferred policy name:

```text
Wms.Integrations.OneC.Import
```

If the policy infrastructure does not yet exist, the feature should document the required policy and use the closest existing authorization mechanism without implementing the full authentication/authorization baseline.

### FR-016: Error handling

The implementation must handle:

* 1C base URL not configured;
* integration disabled;
* 1C unavailable;
* authentication failure;
* unavailable OData entity set;
* malformed OData response;
* timeout;
* invalid record data;
* duplicate/conflicting local code;
* partial record-level failures.

A record-level failure should not stop the entire import when processing can safely continue.

A connection-level failure may fail the entire operation.

## Data Model Requirements

If not already present, add these fields to the relevant Myrmex reference entities:

```csharp
Guid? ExternalRefKey
DateTimeOffset? LastImportedAtUtc
```

Entities:

* Warehouse;
* StockKeepingUnit;
* UnitOfMeasure.

Add unique filtered indexes for `ExternalRefKey` where supported:

```text
ExternalRefKey is unique when ExternalRefKey is not null
```

Do not add `ExternalSystem` in this MVP.

Assumption: one primary 1C reference-data source is used per Myrmex deployment.

## Architecture Requirements

### Integration boundary

1C-specific DTOs, Russian field names, OData query construction, and transport concerns belong to:

```text
Myrmex.Integrations.OneC
```

WMS import rules, upsert behavior, domain validation, and persistence orchestration belong to:

```text
Myrmex.Modules.Wms
```

### Mapping boundary

Mapping must follow this direction:

```text
1C OData DTO
    -> WMS import command item
    -> WMS domain entity/upsert behavior
```

Do not map 1C DTOs directly into EF tracked domain entities inside the integration adapter.

### Handler responsibility

Handlers should remain orchestration units. This feature must not introduce an inventory accounting refactor and must not change existing inventory count or manual move behavior.

## Suggested Technical Names

C# namespace/folder:

```text
Myrmex.Integrations.OneC
```

Main service/client names:

```text
OneCODataClient
OneCOptions
OneCImportResponse
ImportOneCWarehouses
ImportOneCUnitsOfMeasure
ImportOneCStockKeepingUnits
```

HTTP routes:

```text
/api/integrations/1c/connection/test
/api/integrations/1c/warehouses/import
/api/integrations/1c/uoms/import
/api/integrations/1c/skus/import
```

UI labels:

```text
Интеграции
1С
Проверить подключение
Загрузить склады из 1С
Загрузить единицы измерения из 1С
Загрузить номенклатуру из 1С
Обработано
Создано
Обновлено
Пропущено
Ошибки
```

## Example 1C OData Request

Example nomenclature request:

```text
http://app1c.dobroga.ru/dobroga2025/odata/standard.odata/Catalog_Номенклатура?$format=json&$orderby=Ref_Key&$skip=1000&$top=1001&$select=Ref_Key,DeletionMark,Code,Description,Артикул
```

The actual base URL, entity set names, credentials, batch size, and timeout must come from configuration.

## Acceptance Criteria

### AC-001: Connection test

Given 1C connection settings are configured, when the user clicks “Проверить подключение”, then Myrmex verifies the 1C OData connection and shows a success or failure result.

### AC-002: Warehouses import

Given 1C warehouses are available through OData, when the user imports warehouses, then Myrmex creates or updates warehouse records by `ExternalRefKey` and shows an import summary.

### AC-003: Units of measure import

Given 1C units of measure are available through OData, when the user imports units of measure, then Myrmex creates or updates unit records by `ExternalRefKey` and shows an import summary.

### AC-004: Nomenclature import with batching

Given 1C contains more than one batch of nomenclature records, when the user imports nomenclature, then Myrmex loads records using `$top`/`$skip` with deterministic `$orderby=Ref_Key` and processes all available records.

### AC-005: Idempotency

Given the same 1C data is imported twice, when the second import runs, then previously imported records are updated, not duplicated.

### AC-006: Code conflict

Given Myrmex contains a local record with the same `Code` but without `ExternalRefKey`, when an imported 1C record with unknown `ExternalRefKey` has the same `Code`, then Myrmex skips the imported record and reports a conflict.

### AC-007: Deletion mark

Given a 1C record has `DeletionMark = true`, when it is imported, then Myrmex marks the corresponding entity inactive where supported and does not physically delete it.

### AC-008: UI summary

Given an import operation completes, when the result is displayed, then the user sees processed, created, updated, skipped, failed counts, and visible error details if any.

### AC-009: Secret handling

Given the application is configured for 1C integration, credentials are not stored in repository files and are not accepted from the import request body.

### AC-010: Boundary isolation

Given 1C DTOs use names such as `Ref_Key` and `Catalog_Номенклатура`, those names remain inside the `Myrmex.Integrations.OneC` boundary and do not leak into WMS domain entities.

## Open Questions

1. What are the exact OData entity set names for warehouses, nomenclature, and units of measure in the target 1C publication?
2. What is the exact unit of measure catalog used by the target 1C configuration?
3. Is `Артикул` always available for nomenclature in the target publication?
4. What batch size is optimal for the target 1C server?
5. Should import logs be persisted in a dedicated table in this MVP, or is response-only reporting sufficient?
6. Which existing Myrmex authorization mechanism should protect the import endpoints until the auth/authz baseline is implemented?

## Demo Script

1. Configure the 1C OData base URL and credentials.
2. Open Myrmex.
3. Navigate to “Интеграции → 1С”.
4. Click “Проверить подключение”.
5. Click “Загрузить склады из 1С”.
6. Verify the import summary.
7. Click “Загрузить единицы измерения из 1С”.
8. Verify the import summary.
9. Click “Загрузить номенклатуру из 1С”.
10. Verify that the import handles multiple batches.
11. Open WMS reference screens and verify that imported warehouses, units of measure, and nomenclature are visible.
12. Run the same import again and verify idempotent behavior.
