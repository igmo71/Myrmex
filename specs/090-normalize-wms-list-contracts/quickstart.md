# Phase 2 Validation Quickstart

## Prerequisites

1. Read `spec.md` and `plan.md` in this feature directory.
2. Treat `research.md` as completed audit evidence.
3. Configure `MYRMEX_WMS_TEST_CONNECTION` for the existing dedicated SQL Server test database whose name ends in `_test`; its migrations must already be current.
4. Use a development environment capable of running the existing .NET test project.
5. Do not start WebApp, AppHost, Docker, or infrastructure, and do not generate/apply migrations or update the database schema.

## Static Review

- Confirm every supported and fallback branch in the four `ApplySorting` methods ends with ascending `ThenBy(x => x.Id)`.
- Confirm the original primary expression, primary direction, supported key strings, and default Code ordering remain unchanged.
- Confirm `Skip` and `Take` remain after sorting.
- Confirm no endpoint, shared contract, WebApp, resource, persistence configuration, or migration file changed.

## Focused Tests

The developer may run the focused suites after implementation:

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --filter "FullyQualifiedName~ListZonesHandlerTests|FullyQualifiedName~ListStorageLocationsHandlerTests|FullyQualifiedName~ListStockKeepingUnitsHandlerTests|FullyQualifiedName~ListUnitsOfMeasureHandlerTests"
```

Expected result: all four focused suites pass, including duplicate-Name ordering in both directions and adjacent-page completeness.

If broader compilation confidence is needed, the developer may run:

```powershell
dotnet build Myrmex.slnx --no-restore
```

These commands are recommendations only and are not run by the planning workflow.
