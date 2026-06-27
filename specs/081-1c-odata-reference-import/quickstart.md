# Quickstart: Validate 1C OData Reference Import MVP

This guide is for implementation review and developer-controlled validation. Planning did not run any build, test, migration, database, application, Docker, or infrastructure command.

## 1. Review the Design Artifacts

- Feature behavior: `specs/081-1c-odata-reference-import/spec.md`
- Implementation plan: `specs/081-1c-odata-reference-import/plan.md`
- Design decisions: `specs/081-1c-odata-reference-import/research.md`
- Data and transaction model: `specs/081-1c-odata-reference-import/data-model.md`
- HTTP contract: `specs/081-1c-odata-reference-import/contracts/onec-integration.openapi.yaml`

Confirm there are no `NEEDS CLARIFICATION` markers and no integration implementation outside `src/Myrmex.Integrations/Myrmex.Integrations.csproj` except registration, shared transport records, WMS-owned import behavior, WebApp UI/client, tests, and migration artifacts.

## 2. Configuration Prerequisites

Configure these values under `Myrmex:Integrations:OneC` for `Myrmex.ApiService`:

```text
Enabled
BaseUrl
Username
Password
WarehousesEntitySet
UnitsOfMeasureEntitySet
NomenclatureEntitySet
BatchSize
TimeoutSeconds
DefaultSkuBaseUnitOfMeasureExternalRefKey
```

Expected defaults and validation:

- `BatchSize`: default `1000`, valid `1`–`5000`.
- `TimeoutSeconds`: default `30`, must be positive.
- `DefaultSkuBaseUnitOfMeasureExternalRefKey`: the 1C `Ref_Key` of the UoM that all MVP-imported SKUs use as their required base UoM.
- One API instance only. The same-type import gate is process-local and is not safe across multiple replicas.

Credentials must come from user secrets, environment variables, deployment secrets, or another secure provider. Do not put credentials in repository files or HTTP request bodies.

Example developer-controlled user-secret setup:

```powershell
dotnet user-secrets set "Myrmex:Integrations:OneC:Enabled" "true" --project Myrmex.ApiService\Myrmex.ApiService.csproj
dotnet user-secrets set "Myrmex:Integrations:OneC:BaseUrl" "https://onec.example/odata/standard.odata/" --project Myrmex.ApiService\Myrmex.ApiService.csproj
dotnet user-secrets set "Myrmex:Integrations:OneC:Username" "<username>" --project Myrmex.ApiService\Myrmex.ApiService.csproj
dotnet user-secrets set "Myrmex:Integrations:OneC:Password" "<secret>" --project Myrmex.ApiService\Myrmex.ApiService.csproj
dotnet user-secrets set "Myrmex:Integrations:OneC:WarehousesEntitySet" "Catalog_Склады" --project Myrmex.ApiService\Myrmex.ApiService.csproj
dotnet user-secrets set "Myrmex:Integrations:OneC:UnitsOfMeasureEntitySet" "<target-uom-entity-set>" --project Myrmex.ApiService\Myrmex.ApiService.csproj
dotnet user-secrets set "Myrmex:Integrations:OneC:NomenclatureEntitySet" "Catalog_Номенклатура" --project Myrmex.ApiService\Myrmex.ApiService.csproj
dotnet user-secrets set "Myrmex:Integrations:OneC:DefaultSkuBaseUnitOfMeasureExternalRefKey" "<uom-ref-key-guid>" --project Myrmex.ApiService\Myrmex.ApiService.csproj
```

For local use before an auth baseline exists, use the repository's existing development-actor configuration. The feature must not add an authentication scheme or policy implementation.

## 3. Inspect Expected Source Requests

Warehouse request shape:

```text
<BaseUrl>/<WarehousesEntitySet>?$format=json&$orderby=Ref_Key&$select=Ref_Key,DeletionMark,Code,Description
```

UoM request shape:

```text
<BaseUrl>/<UnitsOfMeasureEntitySet>?$format=json&$orderby=Ref_Key&$select=Ref_Key,DeletionMark,Code,Description
```

Nomenclature page request shape:

```text
<BaseUrl>/<NomenclatureEntitySet>?$format=json&$orderby=Ref_Key&$skip=<offset>&$top=<batch-size>&$select=Ref_Key,DeletionMark,Code,Description
```

If the implemented source DTO deliberately retains `Артикул`, it may be added to `$select`, but it must remain inside the OneC boundary and must not be written into Myrmex `Description`.

Verify entity-set and field identifiers are URL encoded, credentials do not appear in URLs or logs, and nomenclature offset advances by the number of returned records.

## 4. Developer-Controlled Schema Migration

Implementation requires one WMS migration for nullable import metadata and three filtered unique indexes. A developer may run these commands after reviewing the entity and configuration changes:

```powershell
dotnet ef migrations add AddOneCExternalReferenceMetadata --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext --output-dir Infrastructure\Persistence\Migrations
dotnet ef database update --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext
```

Review the generated migration before applying it. It must:

- add nullable `ExternalRefKey` and `LastImportedAtUtc` to `wms.warehouses`, `wms.units_of_measure`, and `wms.stock_keeping_units`;
- add three unique filtered indexes for non-null `ExternalRefKey`;
- preserve existing local records with null metadata;
- avoid `ExternalSystem`, new tables, seed data, and inventory table changes.

## 5. Developer-Controlled Build and Tests

Recommended commands after implementation:

```powershell
dotnet build Myrmex.slnx
dotnet test Myrmex.Tests\Myrmex.Tests.csproj
```

Optional focused test run:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~OneC|FullyQualifiedName~Features.Imports"
```

Expected automated coverage:

- WMS import create/update/idempotency, code conflict, deletion/reactivation, invalid record, base-UoM resolution, atomic rollback, and reconciled counts.
- SQL Server metadata and uniqueness for all three filtered external-reference indexes.
- Exact OData query construction, Unicode DTO names, `Guid Ref_Key` deserialization, empty/exact/multi-page termination, malformed response, timeout, and cancellation.
- OneC orchestration mapping, error cap, partial committed counts, failed-batch exclusion, and same-type lock behavior.
- Four route contracts, authenticated-actor check, `409 AlreadyInProgress`, complete/incomplete JSON, and ProblemDetails.
- WebApp client route selection, successful response parsing, ProblemDetails mapping, and cancellation propagation.

## 6. Developer-Controlled Startup

After required configuration and schema update, a developer may start the application:

```powershell
dotnet run --project Myrmex.AppHost\Myrmex.AppHost.csproj
```

Do not scale `Myrmex.ApiService` above one instance for this MVP. Distributed locking is not implemented.

## 7. Manual API Validation

Set the API address shown by the development host:

```powershell
$apiBase = "https://<api-address>"
```

### Connection test

```powershell
Invoke-RestMethod -Method Post -Uri "$apiBase/api/integrations/1c/connection/test"
```

Expected success: `isReady=true` and all three checked reference types. Repeat with disabled integration, invalid credentials, an unavailable URL, and an invalid entity-set name; verify safe categorized ProblemDetails and no credential exposure.

### Warehouse import

```powershell
Invoke-RestMethod -Method Post -Uri "$apiBase/api/integrations/1c/warehouses/import"
```

Verify new records are created by source identity, linked records update, local same-code records remain unlinked and are skipped, deletion-marked linked records deactivate, and deletion-marked unlinked records are skipped.

### Unit-of-measure import

```powershell
Invoke-RestMethod -Method Post -Uri "$apiBase/api/integrations/1c/uoms/import"
```

Verify the configured `DefaultSkuBaseUnitOfMeasureExternalRefKey` now resolves to exactly one active UoM before importing SKUs.

### SKU import

```powershell
Invoke-RestMethod -Method Post -Uri "$apiBase/api/integrations/1c/skus/import"
```

Use a source dataset larger than one batch and preferably larger than 15,000 records. Verify stable ascending source-identity paging, all valid source records considered once, required base UoM assigned, and the final page behavior for both partial and exact batch-size totals.

### Idempotent repeat

Run each import again with unchanged source data. Verify:

- no duplicate `ExternalRefKey` values;
- no new records for already linked identities;
- linked records refresh `LastImportedAtUtc` and count as updated;
- count reconciliation: `processed = created + updated + skipped + failed`.

### Partial batch failure

Arrange for at least one batch to commit, then make a later source request or batch commit fail. Verify:

- `isComplete=false`;
- earlier batches remain persisted;
- the failed batch is rolled back;
- failed-batch records do not contribute to any count;
- `operationError` explains the incomplete result;
- rerunning safely revisits prior identities without duplication.

### Same-type concurrency

Start a long SKU import, then issue another SKU import from a second terminal. Verify the second request returns `409` with `OneCImport.AlreadyInProgress`. While SKU import is running, a warehouse or UoM import may acquire its own gate.

### Error cap

Provide more than 50 invalid/conflicting records in committed batches. Verify all failures contribute to counts while the response contains at most 50 record error objects.

## 8. Manual WebApp Validation

1. Navigate to `Интеграции → 1С`.
2. Confirm the page labels are Russian without broader application localization changes.
3. Run `Проверить подключение` and verify clear success/error feedback.
4. Run warehouses, UoMs, then nomenclature in that order.
5. Confirm the selected action shows progress and cannot be started twice from the page while running.
6. Confirm the latest complete or incomplete summary shows processed, created, updated, skipped, failed, operation error, and capped record errors.
7. Open existing WMS warehouse, UoM, and SKU screens and sample imported records.
8. Refresh or leave the page and verify no persistent import history was introduced.

## 9. Scope Regression Check

Confirm implementation did not add:

- background jobs, polling, schedules, queues, RabbitMQ, or outbox behavior;
- persistent import-history tables;
- bidirectional exchange or non-reference imports;
- mapping/conflict administration;
- `ExternalSystem` or multiple-source support;
- distributed locking or a claim of multi-instance safety;
- full localization resources;
- authentication/authorization baseline or new policy infrastructure;
- inventory-accounting handler refactoring;
- inventory count or manual move behavior changes;
- 1C DTOs or names in WMS domain/public Myrmex contracts;
- credentials in repository files, request bodies, responses, or logs.
