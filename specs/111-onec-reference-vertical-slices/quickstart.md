# Quickstart: Validate 1C Reference Vertical Slices

## Purpose

Validate that Issue #111 changes code ownership without changing runtime behavior. This guide is a developer-controlled handoff: the planning agent did not run any command listed here.

## Prerequisites

- Work from the existing `111-onec-reference-vertical-slices` branch.
- Review [spec.md](./spec.md), [plan.md](./plan.md), and the contracts in [contracts](./contracts/).
- Use the existing repository .NET 10 SDK and test configuration.
- Runtime/API checks require the same prepared external `MyrmexDatabase`, 1C configuration, authentication configuration, and Data Protection prerequisites already required by Features #104/#109.
- Do not generate or edit migrations or the model snapshot; this feature has no schema change.

## 1. Structural Ownership Review

Verify that each reference folder contains its source record/source, manual import, synchronize-one operation, and durable handler.

Confirm that production searches find none of the obsolete composite paths:

```powershell
rg -n "IOneCImportService|OneCImportService|IOneCReferenceSynchronizationService|OneCReferenceSynchronizationService" Myrmex.Integrations
rg -n "SynchronizeAsync\(\s*OneCReferenceType|Func<Guid, CancellationToken, Task<ReferenceSynchronizationResult>>" Myrmex.Integrations
```

Expected outcome: no production matches for removed composite services, the central reference selector, or the delegate-driven synchronization runner. The enum may remain only as result/diagnostic identity if retained.

Review each slice against [slice-operation-boundaries.md](./contracts/slice-operation-boundaries.md). A developer should locate source read, mapping, WMS command dispatch, and outcome handling for either flow within 10 minutes.

## 2. Static Compatibility Review

Verify by code review that these files retain their public behavior:

- `Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs`
- `Myrmex.Integrations/OneC/Endpoints/OneCNotificationEndpoints.cs`
- `Myrmex.Shared/Integrations/OneC/OneCImportResponse.cs`
- `Myrmex.WebApp/Integrations/OneC/OneCIntegrationApiClient.cs`
- `Myrmex.Integrations/Synchronization/`
- existing WMS `ImportWarehouses`, `ImportUnitsOfMeasure`, and `ImportStockKeepingUnits` operations

Expected outcome: routes, authorization, response fields, endpoint names, notification intake, stable entity types, durable statuses, WebApp URLs, WMS rules, persistence mappings, and schema are unchanged.

## 3. Developer-Controlled Build

Run only when the developer explicitly chooses command-based validation:

```powershell
dotnet build Myrmex.Integrations/Myrmex.Integrations.csproj -nologo
dotnet build Myrmex.Tests/Myrmex.Tests.csproj -nologo --no-restore
```

Expected outcome: explicit slice contracts and registrations compile; removed composite types have no callers.

## 4. Developer-Controlled Focused Tests

Run the existing tests after they are minimally retargeted:

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --no-build --filter "FullyQualifiedName~Myrmex.Tests.Integrations.OneC.Client|FullyQualifiedName~Myrmex.Tests.Integrations.OneC.Imports|FullyQualifiedName~Myrmex.Tests.Integrations.OneC.References|FullyQualifiedName~Myrmex.Tests.Integrations.OneC.Endpoints|FullyQualifiedName~Myrmex.Tests.Integrations.Authorization.IntegrationAuthorizationEndpointTests"
```

Expected outcomes:

- Existing source projection, key filtering, DataVersion validation, folder filtering, paging, authentication, timeout, and cancellation cases pass against common transport plus slice sources.
- Existing manual import mapping, accounting, error caps, partial SKU commits, cancellation, logging safety, and gate tests pass against slice imports.
- Existing synchronize-one outcome/cancellation cases pass against explicit synchronizers; the obsolete central-switch test is absent.
- Existing representative durable-handler outcome mapping passes without per-reference duplication.
- Existing SKU repair tests prove one UoM synchronization and at most two SKU applications.
- Existing endpoint and authorization theories pass with the three narrow import contracts.

Do not add tests merely to mirror moved or split classes.

## 5. Optional Developer-Controlled Broader Regression

When explicitly requested, run the existing Feature #104/#109 regression suites without modifying their matrices:

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --no-build --filter "FullyQualifiedName~Myrmex.Tests.Integrations.OneC.Synchronization|FullyQualifiedName~Myrmex.Tests.Wms"
```

Expected outcome: durable intake, duplicate handling, retry, polling, recovery, source-version behavior, source ownership, WMS imports, domain events, and persistence behavior remain unchanged.

## 6. Runtime Acceptance (Only When Explicitly Requested)

With prepared external infrastructure and valid secrets, exercise the existing routes described in [manual-import-compatibility.md](./contracts/manual-import-compatibility.md) and [reference-synchronization-compatibility.md](./contracts/reference-synchronization-compatibility.md).

Verify:

- each manual route returns the existing response/accounting contract;
- same-type overlap fails fast or returns `Busy` as appropriate while another type remains independent;
- reference notifications persist before empty `202 Accepted`;
- applied, unchanged, controlled-skip, not-found, transient, and permanent outcomes retain current durable behavior and diagnostics;
- SKU repair makes no more than one UoM attempt and one SKU retry;
- no new endpoint, UI action, durable status, database object, or migration exists.

AppHost, Docker, database update, application startup, and runtime API checks are not required for the planning phase and must not be run by an agent without an explicit developer request.
