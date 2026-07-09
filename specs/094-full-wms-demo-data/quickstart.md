# Quickstart: Full WMS Demo Data Seeding

This guide validates the completed feature against `spec.md`, `data-model.md`, and `contracts/demo-data-admin.openapi.yaml`. Commands are recommendations for a developer to run after implementation; Spec Kit planning did not execute them.

## 1. Prerequisites

- Use a dedicated non-production SQL Server database. Never point the API at production or a database containing data that must be preserved.
- The database name used for automated persistence tests must end in `_test`.
- Apply all existing WMS migrations before validation. This feature is expected to add no migration.
- Run one `Myrmex.ApiService` instance. The demo-operation gate is process-local.
- Ensure no users or integrations are changing the demo database during clear/seed validation.
- Use an authenticated Identity user with a role authorized for the demo-data endpoints.

## 2. Configure local demo support

Prefer environment variables or user secrets rather than committing an enabled destructive configuration.

PowerShell example for the current process:

```powershell
$env:Myrmex__Wms__DemoData__Enabled = 'true'
$env:Myrmex__Wms__DemoData__AllowClear = 'true'
$env:Myrmex__Wms__DemoData__ClearConfirmation = 'CLEAR-MYRMEX-DEMO'
```

The equivalent JSON shape is:

```json
{
  "Myrmex": {
    "Wms": {
      "DemoData": {
        "Enabled": true,
        "AllowClear": true,
        "ClearConfirmation": "CLEAR-MYRMEX-DEMO"
      }
    }
  }
}
```

Do not place the real confirmation value in source control, command logs, screenshots, or shared diagnostics.

## 3. Developer-controlled build and tests

Recommended build:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
```

Recommended focused tests after implementation:

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --settings Myrmex.Tests/local.runsettings --filter "FullyQualifiedName~Myrmex.Tests.Wms.DemoData"
```

Recommended full tests:

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --settings Myrmex.Tests/local.runsettings
```

Set `MYRMEX_WMS_TEST_CONNECTION` to the dedicated migrated SQL Server test database before persistence tests. Do not generate or apply a migration for this feature unless implementation discovers an unavoidable persisted-model change and planning is revised first.

## 4. Start the application

Recommended local startup:

```powershell
dotnet run --project Myrmex.AppHost/Myrmex.AppHost.csproj
```

Use the API base address shown by Aspire. The examples below assume:

```powershell
$api = 'https://localhost:7001'
```

Adjust the address to the actual API endpoint.

## 5. Seed an empty schema-ready database

```powershell
$seed = Invoke-RestMethod `
  -Method Post `
  -Uri "$api/api/admin/demo-data/seed"
$seed | ConvertTo-Json -Depth 5
```

Expected:

- HTTP 200;
- `operation` is `seed`;
- every area has non-negative created/reused/skipped counts and `deleted=0`;
- 4 UoMs, 10 SKUs, 1 warehouse, 7 zones, and 15 locations are created or reused;
- no SKU barcode or category/group is created;
- all created user-facing names/descriptions are Russian;
- logs include actor, environment, outcome, duration, and summary counts without the confirmation value.

## 6. Verify idempotency

Call seed again without clearing:

```powershell
$secondSeed = Invoke-RestMethod `
  -Method Post `
  -Uri "$api/api/admin/demo-data/seed"
$secondSeed | ConvertTo-Json -Depth 5
```

Expected:

- HTTP 200;
- stable records/stages are reported as reused;
- no duplicate code, transfer, count scenario, opening adjustment, balance effect, transaction, or ledger entry is created;
- current quantities and history remain unchanged by the second call.

## 7. WebApp demonstration walkthrough

Using the existing WebApp, verify:

1. Catalog shows ten recognizable Russian fastener SKUs and four UoMs.
2. Warehouse list shows `DEMO — Демонстрационный склад`.
3. Zone list shows the seven Russian demo zones.
4. Storage-location list shows fifteen locations.
5. Storage-location filters work for warehouse, zone, type, status, and active state.
6. Inventory Balances show bulk, pick, quarantine, packing, and `CART-01` stock.
7. Inventory Ledger shows opening adjustments and transfer movement history.
8. Transfer `DEMO-TRF-DIRECT-001` is Completed without transit.
9. Transfer `DEMO-TRF-CART-001` is Completed through `CART-01`.
10. Transfer `DEMO-TRF-CART-002` is InProgress with stock on `CART-01`.
11. Transfer `DEMO-TRF-DIRECT-002` is Created with no movement.
12. Open count `DEMO-CNT-OPEN-001` shows zero, shortage, and surplus lines; historical count `DEMO-CNT-CLOSED-001` is Completed.

## 8. Clear and reseed

The clear request removes all mutable application records, including records manually created after seeding.

```powershell
$body = @{ confirmation = 'CLEAR-MYRMEX-DEMO' } | ConvertTo-Json
$clear = Invoke-RestMethod `
  -Method Post `
  -Uri "$api/api/admin/demo-data/clear" `
  -ContentType 'application/json' `
  -Body $body
$clear | ConvertTo-Json -Depth 5
```

Expected:

- HTTP 200 and `operation=clear`;
- every area reports deleted counts and zero created/reused/skipped counts;
- UoMs, SKUs, barcodes, warehouses, zones, locations, balances, transactions, ledger entries, transfers/movements, and counts/lines are absent;
- system storage-location types/statuses remain;
- `wms` schema, tables, indexes, constraints, and `__EFMigrationsHistory` remain;
- the API can immediately seed the database again to the same stable codes and demonstration scenarios.

Repeat the seed request from section 5 and repeat the WebApp walkthrough.

## 9. Safety and failure validation

### Disabled route

Set `Enabled=false`, restart, and request both paths. Expected: normal 404 responses because routes are not registered.

### Production route

Run with Production environment and `Enabled=true`. Expected: both paths still return normal 404 responses.

### Clear disabled

Set `Enabled=true`, `AllowClear=false`, restart, and call clear with the correct body. Expected: 403 and no data change.

### Missing or wrong confirmation

With clear enabled, submit an empty/missing confirmation and then a wrong value. Expected: 400 for malformed/missing input or 403 for non-matching confirmation; no data change and no secret in responses/logs.

### Missing actor

Call either registered route without authenticated claims. Expected: 401 and no data access or mutation.

### Concurrent operation

Issue a second seed or clear request while one operation is held in progress by a test seam. Expected: 409 with code `DemoData.OperationInProgress`; the active request remains authoritative.

### Incompatible stable identity

In a dedicated test database, create a record using a demo code with incompatible immutable values and call seed. Expected: 409 with code `DemoData.IdentityConflict`; the complete seed transaction rolls back.

### Injected stage failure

Use the service test seam to fail after at least one seed or clear stage. Expected: failure response, zero committed changes from that request, and a failure log without confirmation data.

## 10. Reset local environment variables

After validation:

```powershell
Remove-Item Env:Myrmex__Wms__DemoData__Enabled -ErrorAction SilentlyContinue
Remove-Item Env:Myrmex__Wms__DemoData__AllowClear -ErrorAction SilentlyContinue
Remove-Item Env:Myrmex__Wms__DemoData__ClearConfirmation -ErrorAction SilentlyContinue
```

Do not use the clear endpoint against any database whose contents must be preserved.
