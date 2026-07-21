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

Expected outcome: routes, authorization, response fields, endpoint names, notification intake, stable entity types, durable statuses, WebApp URLs, WMS rules, persistence mappings, and schema are unchanged. Manual imports retain the pre-start boundary (`400` invalid configuration, `409` same-reference contention, platform `401/403`) and convert transport/application failures after source processing begins into incomplete `200 OK OneCImportResponse`; only the connection-test endpoint retains transport-failure `502/504` Problem Details.

Review each concrete reference handler and verify this visible sequence:

```text
parse and validate ExternalId
-> call the matching slice synchronizer
-> write structured correlation log
-> map the completed result through the pure common mapper
```

The correlation log contains `SynchronizationRequestId`, `EntityType`, `ExternalId`, Base64 `NotifiedDataVersion`, `CurrentOutcome`, `CurrentReason`, and `RetrySuitable`. Invalid `ExternalId` logs the equivalent permanent invalid-request result. The mapper performs no parsing, selection, callback invocation, or logging, and no credentials, secrets, or source payloads are logged.

Also verify that `Processed != 1` or inconsistent one-item counts remain permanent `ApplicationFailure` with `retrySuitable = false`; Issue #111 does not throw or route that condition through transient retry.

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
- Existing representative durable-handler outcome mapping passes without per-reference duplication. Correlation fields are code-review acceptance unless an existing logging assertion is trivially extended; no logging test suite is added.
- In the existing `StockKeepingUnitReferenceRepairTests`, the successful repair test is parameterized for UoM `Applied` and `Unchanged`.
- The renamed retry-still-fails test proves that successful UoM synchronization followed by another missing/inactive SKU result stops permanently after the single retry.
- One compact parameterized failed-UoM test covers `Busy`, `TransientFailure`, `NotFound`, `ControlledSkip`, and `PermanentFailure`: the first two map to transient SKU repair failure, the last three map to permanent failure, and every row proves one UoM call, one SKU dispatch, no retry, and no recursion/additional dependency call.
- Existing endpoint and authorization theories pass with the three narrow import contracts.

Do not add a new repair test class, per-reference matrix, Feature #104 suite, logging suite, or tests merely to mirror moved or split classes.

## 5. Optional Developer-Controlled Broader Regression

When explicitly requested, run the existing Feature #104/#109 regression suites without modifying their matrices:

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --no-build --filter "FullyQualifiedName~Myrmex.Tests.Integrations.OneC.Synchronization|FullyQualifiedName~Myrmex.Tests.Wms"
```

Expected outcome: durable intake, duplicate handling, retry, polling, recovery, source-version behavior, source ownership, WMS imports, domain events, and persistence behavior remain unchanged.

## 6. Runtime Acceptance (Only When Explicitly Requested)

With prepared external infrastructure and valid secrets, exercise the existing routes described in [manual-import-compatibility.md](./contracts/manual-import-compatibility.md) and [reference-synchronization-compatibility.md](./contracts/reference-synchronization-compatibility.md).

Verify:

- each manual route returns `400` only for invalid/disabled integration configuration, `409` only for same-reference pre-start contention, platform `401/403` for authentication/authorization, and incomplete `200 OneCImportResponse` for transport/application failures after import start;
- the connection-test endpoint continues returning its existing transport-failure Problem Details;
- same-type overlap fails fast or returns `Busy` as appropriate while another type remains independent;
- reference notifications persist before empty `202 Accepted`;
- applied, unchanged, controlled-skip, not-found, transient, and permanent outcomes retain current durable behavior and diagnostics;
- each concrete handler emits the required correlation fields before pure durable-result mapping, including the equivalent invalid-request log for an invalid external identity;
- inconsistent one-item accounting remains permanent `ApplicationFailure` with retry unsuitable;
- SKU repair makes no more than one UoM attempt and one SKU retry;
- no new endpoint, UI action, durable status, database object, or migration exists.

AppHost, Docker, database update, application startup, and runtime API checks are not required for the planning phase and must not be run by an agent without an explicit developer request.
