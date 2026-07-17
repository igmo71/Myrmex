# Implementation Plan: Reactive and On-Demand Reference-Data Synchronization

**Branch**: `109-add-reactive-and-on-demand-reference-data-synchronization` | **Date**: 2026-07-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/109-reference-data-synchronization/spec.md`

## Summary

Extend the existing 1C reference import slices so Warehouse, Unit of Measure, and Stock Keeping Unit share version-aware application behavior across manual full import, reactive Feature 104 requests, and internal synchronize-one calls. Add current-object OData reads, an owned external import state with opaque version, additive unchanged accounting, source-ownership guards, three thin synchronization handlers, and bounded SKU-to-UoM repair. Reuse the existing import commands, Feature 104 queue/processor/lifecycle, and singleton per-reference-type gate; do not add a generalized synchronization/provider/dependency framework or any distributed coordination.

## Technical Context

**Language/Version**: C# / .NET 10.0

**Primary Dependencies**: ASP.NET Core Minimal APIs and authentication/authorization; EF Core 10.0.9 SQL Server provider; existing Myrmex command and domain-event dispatchers; existing Feature 104 synchronization store/processor/worker/retry/recovery; `HttpClient` and `System.Text.Json` for 1C OData; existing Blazor/MudBlazor WebApp and `.resx` localization.

**Storage**: Existing `MyrmexDatabase`; WMS-owned `wms.warehouses`, `wms.units_of_measure`, and `wms.stock_keeping_units` gain only nullable `ExternalDataVersion varbinary(128)`. Existing `ExternalRefKey`, `LastImportedAtUtc`, values, filtered unique indexes, and index names remain. Feature 104 `integration.synchronization_requests` is reused without schema or lifecycle changes.

**Testing**: Prepare focused xUnit v3 test source in `Myrmex.Tests`: current handler/domain tests, persistence test source where mapping/index/provider behavior is material, current in-process Minimal API tests, and current transport/import service tests. Record manual WebApp localization acceptance expectations because the repository has no component-test framework. Test execution remains developer-controlled.

**Target Platform**: Existing Myrmex ApiService and Blazor WebApp in the current Aspire-composed modular-monolith deployment; one active application instance is the approved coordination scope.

**Project Type**: Existing .NET modular-monolith web application with integration, WMS module, shared transport contract, API, and WebApp changes; no new project or service.

**Performance Goals**: When an eligible reactive request is processed with an available source and free per-type gate, applied, unchanged, and controlled-skip outcomes complete the durable request in that same attempt. Gate acquisition is non-waiting. No elapsed-time percentile is introduced.

**Constraints**: Reuse `ImportWarehouses`, `ImportUnitsOfMeasure`, and `ImportStockKeepingUnits`; preserve manual routes/auth/error/paging/partial-commit behavior and cancellation contract; reuse Feature 104 intake, processor, retry, recovery, polling, and wake-up; hold one in-process lease per type over source read through commit; keep SKU repair to one UoM and one additional SKU apply; copy opaque binary versions defensively; avoid distributed locks, new durable statuses, public synchronize-one endpoints, recursive repair, generalized links/providers/reference types, and duplicate Feature 104 tests. The agent may modify domain/application source, test source, and EF mappings only. The developer generates, reviews, and applies migrations. The agent does not create or edit migration `.cs`, `.Designer.cs`, or `WmsDbContextModelSnapshot` files, and plans/tasks do not schedule migration generation, database update, build, test, AppHost, Docker, application startup, or other environment-changing command execution unless explicitly requested.

**Scale/Scope**: Exactly three reference types and the existing one configured 1C source. Warehouse and UoM retain full-collection import; SKU retains configured paging/batching. Reactive/on-demand processing loads exactly one current source object, apart from the single bounded UoM repair allowed for SKU.

## Constitution Check

*GATE: Passed before research and re-checked after Phase 1 design.*

- **Domain Model First — PASS**: The design starts with `ExternalImportState`, source-owned versus WMS-owned values, opaque version equality, deletion/reactivation, unchanged/applied results, and the explicit SKU base-UoM dependency. Invariants remain in aggregates/import handlers rather than UI or database-only logic.
- **Modular Monolith Boundaries — PASS**: WMS aggregates, import commands, and EF mappings remain in `Myrmex.Modules.Wms`; 1C transport/orchestration and Feature 104 handlers remain in `Myrmex.Integrations`; only additive HTTP response data crosses through `Myrmex.Shared`; WebApp localization/display remains in `Myrmex.WebApp`. No new service or direct integration access to `WmsDbContext` is introduced.
- **Vertical Slice Delivery — PASS**: Reactive intake extends the existing notification endpoint/request/factory/store path and dispatches explicit registered handlers. Synchronize-one remains an internal explicit request/service boundary. Manual import retains its route, shared response, client, and UI slice. Public transport types remain separate from internal commands and outcomes.
- **Testing Discipline — PASS**: Test source is assigned to the lowest owning layer and limited to Feature 109 changes. A compact same-version smoke case exists for each of the three explicit import handlers, while all broader shared behavior remains representative/parameterized; UoM/SKU-specific cases exist only for symbol, base-UoM, repair, and folder differences. Feature 104 intake idempotency, retry, polling, wake-up, recovery, auth, and lifecycle suites are not reproduced.
- **Simplicity and Observability — PASS**: Existing import commands, transport client, gate, processor, worker, structured error reasons, logging, and diagnostics are extended. The design adds no framework, queue, provider abstraction, dependency graph, external-link table, distributed lock, or lifecycle state.

### Post-Design Re-check

Phase 1 contracts and data model preserve the same boundaries and contain no constitution exception. The expected developer-generated migration is limited to three nullable `ExternalDataVersion` columns with no changes to existing external-key/timestamp columns or indexes. Cancellation uses the existing Processing/abandoned-recovery model while propagating to the caller. No Complexity Tracking entry is required.

## Project Structure

### Documentation (this feature)

```text
specs/109-reference-data-synchronization/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── manual-import-compatibility.md
│   ├── reference-change-notifications.md
│   └── reference-synchronize-one.md
└── tasks.md                         # Generated later by /speckit-tasks
```

### Source Code (repository root)

```text
Myrmex.Modules.Wms/
├── Domain/
│   └── ExternalImportState.cs
├── Topology/
│   ├── Domain/Warehouses/Warehouse.cs
│   └── Features/
│       ├── Imports/ImportWarehouses.cs
│       └── Warehouses/
│           ├── UpdateWarehouseDetails.cs
│           ├── DeactivateWarehouse.cs
│           └── ReactivateWarehouse.cs
├── Catalog/
│   ├── Domain/
│   │   ├── UnitsOfMeasure/UnitOfMeasure.cs
│   │   └── StockKeepingUnits/StockKeepingUnit.cs
│   └── Features/
│       ├── Imports/
│       │   ├── ImportUnitsOfMeasure.cs
│       │   ├── ImportStockKeepingUnits.cs
│       │   └── ReferenceImportBatchResult.cs
│       ├── UnitsOfMeasure/{UpdateUnitOfMeasureDetails,DeactivateUnitOfMeasure,ReactivateUnitOfMeasure}.cs
│       └── StockKeepingUnits/{UpdateStockKeepingUnitDetails,DeactivateStockKeepingUnit,ReactivateStockKeepingUnit}.cs
└── Infrastructure/Persistence/Configurations/
    └── {Warehouse,UnitOfMeasure,StockKeepingUnit}Configuration.cs

Myrmex.Integrations/
├── OneC/
│   ├── Endpoints/OneCNotificationEndpoints.cs
│   ├── Imports/
│   │   ├── OneCImportGate.cs
│   │   ├── IOneCImportService.cs
│   │   └── OneCImportService.cs
│   ├── References/
│   │   ├── OneCReferenceSynchronizationService.cs
│   │   ├── ReferenceSynchronizationResult.cs
│   │   └── ReferenceSynchronizationHandlers.cs
│   ├── Transport/
│   │   ├── IOneCODataClient.cs
│   │   ├── OneCODataClient.cs
│   │   └── Catalog_{Склады,УпаковкиЕдиницыИзмерения,Номенклатура}.cs
│   └── OneCIntegrationModule.cs
└── Synchronization/
    ├── SynchronizationEntityTypes.cs
    └── Processing/SynchronizationProcessor.cs

Myrmex.Shared/Integrations/OneC/
└── OneCImportResponse.cs

Myrmex.WebApp/
├── Components/Pages/Integrations/OneC/Index.razor
└── Resources/Localization/
    ├── SharedResource.resx
    ├── SharedResource.en-US.resx
    └── SharedResource.ru-RU.resx

Myrmex.Tests/
├── Integrations/OneC/{Client,Endpoints,Imports,Synchronization}/
└── Wms/
    ├── Topology/{Domain,Features/Imports,Features/Warehouses,Persistence}/
    └── Catalog/{Domain,Features/Imports,Features/UnitsOfMeasure,Features/StockKeepingUnits,Persistence}/
```

**Structure Decision**: Extend the existing integration and WMS vertical slices in place. Add one narrow `OneC/References` orchestration folder and one module-wide WMS domain value object because all three existing aggregate slices own identical source-link semantics. Keep public HTTP response shape in `Myrmex.Shared`; keep 1C DTOs, internal outcomes, synchronization handlers, EF configuration, and domain objects out of that assembly.

## Architectural Design Notes

- **Domain concepts first**: `ExternalImportState` owns stable source identity, current opaque version, and import time. Equal current version returns unchanged before aggregate mutation. Changed/legacy version applies through the aggregate, with deletion first, no physical delete, and events only for actual business-state changes. Source ownership guards protect linked values while allowing unchanged resubmission and WMS-owned Description edits.
- **Shared contract boundary**: Only `OneCImportResponse.Unchanged` is added to `Myrmex.Shared` because it crosses ApiService/WebApp. Existing request/response/error fields remain. Reference notification DTOs stay in the integration boundary, and synchronize-one outcomes stay internal.
- **Internal request boundary**: `Import*.Command` remains the explicit cross-module application boundary used by manual and one-item flows. `OneCReferenceSynchronizationService` accepts only the exact three supported reference kinds and external identity. Three thin `ISynchronizationHandler`s translate internal operation results into existing Feature 104 handler results.
- **Persistence and migration**: Configure `ExternalImportState` as an optional owned reference with backing-field access for the binary version and exact same-table column names. The agent changes EF mappings only. The developer generates, reviews, and applies the migration. Its expected shape is exactly three nullable `ExternalDataVersion` columns with no changes to existing `ExternalRefKey` or `LastImportedAtUtc` columns or their filtered unique indexes. The agent does not create or edit migration `.cs`, `.Designer.cs`, or `WmsDbContextModelSnapshot` files.
- **Transport boundary**: Add `DataVersion` to all three explicit DTOs and full projections. Add three explicit current-object reads filtered by `Ref_Key`; zero rows means absent; invalid cardinality/version/envelope means malformed. Retain current transport taxonomy; map timeout/source unavailability as transient and disabled/invalid configuration, authentication rejection, entity-set unavailability, and malformed data as permanent for reactive processing.
- **Coordination**: Extend the singleton `OneCImportGate` with non-waiting acquisition usable by synchronize-one. Manual import keeps its fail-fast exception/409 behavior and holds the existing whole-operation lease. Reactive/on-demand holds the same type lease from before one read through command commit. The only nested acquisition is SKU to one UoM; UoM never calls SKU, so no recursive lock graph is created.
- **Cancellation and errors**: Manual import preserves its existing incomplete `Cancelled` response, and direct internal on-demand caller cancellation propagates to that caller. During reactive processing, `SynchronizationProcessor` logs and rethrows `OperationCanceledException` as shutdown cancellation only when the processor/application stopping token is cancelled, leaving the request `Processing`; source timeout and non-shutdown failures retain their normal transient/permanent classification. The existing worker termination and Feature 104 abandoned recovery remain unchanged, and no durable cancelled status is added. Failure diagnostics include type, external identity, category, and retry suitability without credentials/source payloads.
- **Implementation/execution boundary**: The agent may modify domain/application source, test source, and EF mappings. The developer generates, reviews, and applies migrations. The agent does not generate, create, or edit migration `.cs`, `.Designer.cs`, or `WmsDbContextModelSnapshot` files. This plan and later tasks do not schedule build, test, migration-generation, database-update, AppHost, Docker, application-startup, or other environment-changing command execution unless explicitly requested.
- **Manual UI**: Add localized `Common.Unchanged` and render the count beside existing totals. Keep all three buttons, routes, busy mapping, structured errors, and client methods; add no reactive/on-demand UI.
- **Existing pattern precedence**: Reuse `OneCImportService`, `OneCODataClient`, the three import commands, `OneCImportGate`, Feature 104 endpoint/factory/store/handler resolver/processor, existing ApiResult/Problem Details conventions, and the current 1C WebApp page.

## Implementation Sequence

### 1. Domain and persistence foundation

1. Add `ExternalImportState` with non-empty identity/version rules, legacy null-version hydration, content comparison, and defensive copying.
2. Replace aggregate scalar import metadata with optional owned state while keeping queryable source identity and dedicated import mutation methods.
3. Update EF configurations with exact legacy/new column names, binary length, optional owned mapping, and existing filtered index names/filters.
4. Document the expected developer-generated migration shape as three nullable `ExternalDataVersion` columns with no changes to existing `ExternalRefKey` or `LastImportedAtUtc` columns or their indexes; leave generation, review, and application to the developer.

### 2. Version-aware shared application handlers

1. Add `ExternalDataVersion` to all three import item records and `Unchanged` to the batch result/invariant.
2. In the existing handlers, short-circuit equal linked versions before validation/mutation; apply changed or null legacy versions through aggregate methods.
3. Preserve deletion-first, conflicts, SKU dependency lookup, transaction/savepoint, rollback, committed batch, and event-dispatch behavior.
4. Ensure metadata-only changed versions count as Updated without a business-detail/activation event.

### 3. Current-object source and synchronize-one orchestration

1. Add version projection/decoding to full source reads and explicit one-object reads.
2. Extend the existing gate with non-waiting synchronize-one acquisition while retaining manual behavior.
3. Implement the narrow three-type synchronize-one service and focused outcome/diagnostic type.
4. Implement the one-directional SKU-to-UoM repair with hard call-count limits.

### 4. Reactive Feature 104 integration

1. Add stable reference entity types and three routes to the existing machine-authenticated endpoint group.
2. Add/register three thin handlers against the existing resolver.
3. Translate operation outcomes to the existing handler result kinds without new durable states.
4. Change the processor cancellation catch to log and rethrow as shutdown cancellation only when the processor/application stopping token is cancelled, keeping the request `Processing` for existing recovery; preserve normal classification for source timeout and non-shutdown failures.

### 5. Manual compatibility and local ownership

1. Add/aggregate `Unchanged` through manual service, public response, WebApp client deserialization, UI, and all three locale resources.
2. Add linked detail and lifecycle guards that reject only actual source-owned changes and preserve unlinked behavior.
3. Preserve all routes, authorization, busy Problem Details, structured record/operation errors, error cap, SKU paging, and manual cancellation behavior.

## Risk-Based Test Plan

| Regression risk introduced/changed by Feature 109 | Lowest owning coverage | Scope control |
|---|---|---|
| External version buffer can be mutated or compared by reference | One `ExternalImportState` domain test for input/output copies and content equality | One shared value-object test, not three aggregate copies. |
| Same-version no-op in every explicit import handler | Add one parameterized theory or one compact existing-handler test for Warehouse, Unit of Measure, and Stock Keeping Unit; each proves only `same current DataVersion -> Unchanged -> no timestamp mutation -> no aggregate mutation or domain event` | Do not add duplicate lifecycle, legacy-version, or changed-version suites for all three types. |
| Legacy null and changed-version application/event semantics | Extend Warehouse import handler tests as the representative broader versioning slice | UoM adds symbol-specific assertion; SKU adds base-UoM/version assertion only where those rules materially differ. |
| Deletion/reactivation with version-aware outcomes | Extend representative Warehouse lifecycle/import tests and adjust existing UoM/SKU tests only for new required version/count shape | Do not recreate identical lifecycle suites. |
| Exact owned columns, nullable version, legacy indexes | One parameterized EF metadata/provider-sensitive persistence test across three entity types; developer review of the generated migration confirms the expected additive shape | Do not duplicate general uniqueness tests or assign migration-file/model-snapshot work to the agent. |
| Current-object projections/mapping and absence | Extend `OneCODataClientTests` with one parameterized query/cardinality test and shape-specific Warehouse/UoM/SKU assertions | Reuse existing timeout/cancellation/malformed transport tests; add only newly distinct single-read cases. |
| Correct reference notification route/entity mapping | Extend the existing valid notification endpoint theory with three route/entity rows | Do not repeat auth, validation, duplicate intake, wake-up, or persistence suites from Feature 104. |
| Operation outcome to handler result mapping | Focused thin-handler theory for completed/transient/permanent plus cancellation propagation | Do not route these through processor retry/recovery matrices. |
| Same gate covers manual and synchronize-one while types remain independent | Extend existing `OneCImportServiceTests` gate scenario with same-type Busy and different-type progress | No per-type gate suites and no distributed-lock tests. |
| Bounded SKU-to-UoM repair | Two SKU-only orchestration tests: successful one-UoM/one-retry and failed non-recursive repair with call limits | No generic dependency resolver tests. |
| Actual-change source ownership | Warehouse representative Name/Description case; UoM Symbol case; SKU BaseUoM/Description case; one representative linked lifecycle transition guard | Preserve existing unlinked tests; do not clone all fields across all types. |
| Shutdown cancellation now propagates while Processing remains recoverable | Update existing `IntegrationSynchronizationCancellationTests` source to expect rethrow only when the processor/application stopping token is cancelled and retain its Processing assertions | Source timeout/non-shutdown failures retain normal classification; rely on existing Feature 104 recovery tests and add no recovery suite or cancelled status. |
| Additive manual response and unchanged count | Extend existing manual import result, endpoint, and API-client theories with `Unchanged`; one representative repeated import | Preserve existing error-shape tests; no three-type duplicate count suite. |
| Localized count appears in existing UI | Manual smoke in neutral/en-US/ru-RU on `/integrations/1c` | No new UI test framework. |

Intentionally omitted duplicate coverage: Feature 104 API-key authentication, notification validation, duplicate idempotency, request persistence, retry scheduling/exhaustion, polling, wake-up coalescing, worker draining, abandoned-processing algorithm, and generic lifecycle transitions. Existing Feature 81 full-import mapping/paging/error tests are adjusted only where the new version/count contract requires it.

## Planning Outcome

All technical choices are resolved. No blocking stakeholder contradiction or constitution violation remains. The feature is ready for `/speckit-tasks`; `tasks.md` is intentionally not generated by this command.
