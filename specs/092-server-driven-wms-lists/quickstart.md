# Validation Quickstart: Server-Driven WMS Catalog and Topology Lists

All commands and runtime checks are developer-controlled. Planning does not execute builds, tests, application startup, database changes, migrations, or infrastructure commands.

## Prerequisites

- .NET 10 SDK and the normal SQL Server test dependency are available.
- Dependencies have already been restored.
- Manual/performance validation uses representative data, including at least 35,000 SKUs where available.
- Do not generate or apply migrations; this feature has no schema changes.

## Focused Automated Validation

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --filter "FullyQualifiedName~ListStockKeepingUnitsHandlerTests|FullyQualifiedName~ListUnitsOfMeasureHandlerTests|FullyQualifiedName~ListWarehousesHandlerTests|FullyQualifiedName~LookupWarehousesHandlerTests|FullyQualifiedName~ListZonesHandlerTests|FullyQualifiedName~ListStorageLocationsHandlerTests|FullyQualifiedName~CatalogListEndpointTests|FullyQualifiedName~TopologyListEndpointTests|FullyQualifiedName~WmsCatalogApiClientTests|FullyQualifiedName~WmsTopologyApiClientTests"
```

Expected: shared contracts, request binding, filters, deterministic ordering, Warehouse lookup, URLs, and cancellation tests pass.

Optional broader checks:

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj
dotnet build Myrmex.slnx --no-restore
```

## Manual Application Validation

Start the application only when the developer chooses:

```powershell
dotnet run --project Myrmex.AppHost/Myrmex.AppHost.csproj
```

### SKU and Unit of Measure

1. Verify totals exceed one page on representative data.
2. Change page/page size and check for no duplicate or missing rows.
3. Search for a record outside the initial order.
4. Sort supported columns both ways across the full result.
5. Change search/inactive filters from a later page; verify page-one reset and total.
6. Create, edit, deactivate, and reactivate; verify server reload.

### Warehouse and Zone

1. Verify Warehouse paging/sorting/search remains server-driven.
2. Find/select a Warehouse beyond the first 20 lookup results on the Zone page.
3. Page, search, sort, and filter Zones; verify totals/reset.
4. Change Warehouse search rapidly; expected cancellation shows no error.

### Storage Location

1. Open without a Warehouse; verify no unrestricted rows are loaded.
2. Find a Warehouse through autocomplete and load locations.
3. Combine Zone, Type, Status, search, and inactive filters; verify rows and total represent the full intersection.
4. Change each filter from a later page; verify page-one reset.
5. Sort supported fields and verify stable page boundaries.
6. Run all existing mutations and verify page/total refresh.
7. Confirm the Zone selector retains documented current behavior; autocomplete conversion is deferred.

### Errors and Cancellation

1. Supersede active list/lookup loads; expected cancellation shows no message.
2. Simulate a genuine read failure; existing page error remains visible.
3. Simulate a failed mutation; existing `ApiResult`/ProblemDetails message remains visible.

## Performance Validation

1. Record at least 20 SKU interactions against 35,000 or more SKUs.
2. Record at least 20 interactions on any affected list with up to 50,000 records.
3. Confirm at least 95% display within 2 seconds.
4. Search known early-, middle-, and late-order records; all expected records must be reachable.

## Static Scope Review

- No route, domain rule, import, persistence configuration, migration, or schema changed.
- `Myrmex.Shared` has no domain, EF, handler, infrastructure, Blazor, or MudBlazor dependency.
- Affected WebApp-local public DTO duplicates are removed.
- Razor sort tags use shared PascalCase constants.
- No generic list/lookup framework or unrelated Inventory behavior was added.

