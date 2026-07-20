# Implementation Plan: 1C Reference Vertical Slices

**Branch**: `111-onec-reference-vertical-slices` | **Date**: 2026-07-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/111-onec-reference-vertical-slices/spec.md`

## Summary

Refactor the existing behavior-complete 1C Warehouse, Unit of Measure, and Stock Keeping Unit integration into three explicit reference-owned slices. Each slice will own its source record and query shape, manual import operation, synchronize-one operation, durable synchronization handler, mapping, result interpretation, logging, and reference-specific error behavior. Retain only uniform mechanisms—authenticated OData execution, configuration and error taxonomy, per-reference lease coordination, import-response construction, synchronization result primitives, and pure durable-result mapping—in common locations. Continue to dispatch the existing WMS `ImportWarehouses`, `ImportUnitsOfMeasure`, and `ImportStockKeepingUnits` commands and reuse Feature #104's durable intake and processing foundation unchanged.

The migration removes `IOneCImportService`/`OneCImportService`, `IOneCReferenceSynchronizationService`/`OneCReferenceSynchronizationService`, the central reference-type switch, both delegate-driven workflow runners, and the all-reference typed OData client surface. Existing public routes, contracts, authorization, WebApp behavior, domain behavior, persistence, diagnostics, coordination, cancellation, paging, partial-result, and synchronization outcome semantics remain unchanged.

## Technical Context

**Language/Version**: C# / .NET 10.0

**Primary Dependencies**: ASP.NET Core Minimal APIs and authorization; existing Myrmex command dispatcher; existing Feature #104 synchronization request store, resolver, processor, retry, recovery, and worker; `HttpClient`, options, logging, and `System.Text.Json` for 1C OData; existing WMS reference import commands.

**Storage**: Existing integration and WMS SQL Server persistence remains unchanged. No schema, EF mapping, migration, model snapshot, durable status, or stored contract changes are planned.

**Testing**: Existing xUnit v3 Feature #104/#109 suites in `Myrmex.Tests`. Retarget only tests whose constructors, contracts, or namespaces change. Continue using `StockKeepingUnitReferenceRepairTests`: parameterize the successful repair for `Applied` and `Unchanged`, rename the current retry-still-fails case to describe that scenario, and add one compact parameterized test for failed UoM outcomes (`Busy`, `TransientFailure`, `NotFound`, `ControlledSkip`, and `PermanentFailure`). Do not add a new test class or duplicate per-reference/durable-foundation matrices.

**Target Platform**: Existing Myrmex ApiService and WebApp in the current Aspire-composed modular monolith; coordination remains in-process within one application instance.

**Project Type**: Existing .NET modular-monolith web application. Production changes are confined to `Myrmex.Integrations`; WMS application/domain, shared transport contracts, WebApp, persistence, and hosting remain behaviorally unchanged.

**Performance Goals**: Preserve current non-waiting same-reference coordination, Warehouse/UoM full-collection behavior, configured SKU paging/batching, one-current-object synchronization, and bounded SKU repair. Introduce no additional source round trips or synchronization attempts.

**Constraints**: Preserve all Feature #104/#109 observable behavior; use existing WMS import commands; keep the durable synchronization lifecycle unchanged; keep source-specific concepts outside WMS contracts; remove obsolete orchestration rather than wrap it; avoid a replacement generic workflow, provider abstraction, dependency engine, second queue, or distributed coordination; hold current lease scopes exactly; preserve diagnostic safety; add no public synchronize-one operation; make no database, UI, or public-contract change. Build, test, migration, database, AppHost, Docker, and application-startup execution remains developer-controlled.

**Scale/Scope**: Exactly three existing reference types and one configured 1C source. Seven owned flows are reorganized: three manual imports, three synchronize-one operations, and bounded SKU-to-UoM repair.

## Constitution Check

*GATE: Passed before Phase 0 research and re-checked after Phase 1 design.*

- **Domain Model First — PASS**: Warehouse, Unit of Measure, SKU, source identity/version, source lifecycle, and SKU base-UoM rules remain owned by the existing WMS aggregates and import commands. The integration slices only read, map, coordinate, dispatch, and classify; they do not duplicate domain rules.
- **Modular Monolith Boundaries — PASS**: All refactoring remains inside `Myrmex.Integrations`. Cross-module behavior continues through the existing public WMS import commands. Public DTOs remain in `Myrmex.Shared`, while source records, internal synchronization outcomes, durable handlers, and transport mechanisms remain internal to the integration module.
- **Vertical Slice Delivery — PASS**: Warehouse, Unit of Measure, and SKU each receive an independently understandable source/manual/synchronize-one/handler slice. Existing endpoint routes remain composition points and the Feature #104 handler boundary remains the durable entry point.
- **Testing Discipline — PASS**: Existing import, source, synchronize-one, handler mapping, endpoint, authorization, and SKU repair coverage is minimally retargeted. One compact parameterized case is added to the existing SKU repair test class because current coverage does not prove failed UoM outcome mapping or the no-retry boundary. Feature #104 processor, retry, persistence, recovery, and notification suites remain unchanged. The obsolete central-switch test is removed because it protects a representation this feature explicitly eliminates.
- **Simplicity and Observability — PASS**: The design deletes two composite services and their callback runners, retains only small uniform mechanisms, uses no new framework, and assigns request/result correlation logging to each concrete reference handler without making the common mapper stateful or reducing error detail.

### Post-Design Re-check

Research, the no-schema data model, and compatibility contracts confirm the same boundaries. The design introduces no new service/project, database object, public contract, durable status, UI behavior, or abstraction framework. The internal SKU-to-UoM dependency remains one explicit contract; its success, retry-still-fails, and failed-UoM outcome mappings are protected within the existing repair test class by the minimum focused changes. No constitution exception or Complexity Tracking entry is required.

## Project Structure

### Documentation (this feature)

```text
specs/111-onec-reference-vertical-slices/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── manual-import-compatibility.md
│   ├── reference-synchronization-compatibility.md
│   └── slice-operation-boundaries.md
└── tasks.md                              # Generated later by /speckit-tasks
```

### Source Code (repository root)

```text
Myrmex.Integrations/OneC/
├── Common/
│   ├── Imports/
│   │   ├── OneCImportGate.cs
│   │   └── OneCImportResponseFactory.cs
│   ├── References/
│   │   ├── ReferenceSynchronizationResult.cs
│   │   └── ReferenceSynchronizationHandlerResultMapper.cs
│   └── Transport/
│       ├── IOneCODataTransport.cs
│       ├── OneCODataTransport.cs
│       ├── OneCODataCollectionResponse.cs
│       └── OneCTransportException.cs
├── Connection/
│   └── OneCConnectionTest.cs
├── Warehouses/
│   ├── WarehouseSourceRecord.cs
│   ├── WarehouseOneCSource.cs
│   ├── WarehouseOneCImport.cs
│   ├── WarehouseOneCSynchronizer.cs
│   └── WarehouseReferenceSynchronizationHandler.cs
├── UnitsOfMeasure/
│   ├── UnitOfMeasureSourceRecord.cs
│   ├── UnitOfMeasureOneCSource.cs
│   ├── UnitOfMeasureOneCImport.cs
│   ├── UnitOfMeasureOneCSynchronizer.cs
│   └── UnitOfMeasureReferenceSynchronizationHandler.cs
├── StockKeepingUnits/
│   ├── StockKeepingUnitSourceRecord.cs
│   ├── StockKeepingUnitOneCSource.cs
│   ├── StockKeepingUnitOneCImport.cs
│   ├── StockKeepingUnitOneCSynchronizer.cs
│   └── StockKeepingUnitReferenceSynchronizationHandler.cs
├── Configuration/                       # Existing configuration retained
├── Endpoints/                           # Existing routes/contracts retained
├── Notifications/                       # Feature #104/#109 intake retained
├── Security/                            # Existing machine authentication retained
└── OneCIntegrationModule.cs             # Composition updated to explicit slices

Myrmex.Integrations/Synchronization/      # Feature #104 durable foundation unchanged

Myrmex.Tests/Integrations/OneC/
├── Client/OneCODataClientTests.cs        # Existing cases retargeted to transport/sources
├── Imports/OneCImportServiceTests.cs     # Existing cases retargeted to slice imports
├── References/
│   ├── OneCReferenceSynchronizationServiceTests.cs  # Existing outcome/cancellation cases retargeted
│   ├── ReferenceSynchronizationHandlerTests.cs      # Existing representative mapper theory retained
│   └── StockKeepingUnitReferenceRepairTests.cs      # Existing explicit dependency-limit cases retained
├── Endpoints/OneCEndpointTests.cs        # Existing route fixtures minimally rewired
└── Synchronization/                      # Feature #104 suites unchanged

Myrmex.Tests/Integrations/Authorization/
└── IntegrationAuthorizationEndpointTests.cs         # Existing fixture minimally rewired
```

**Structure Decision**: Keep the existing project/module boundaries and reorganize only the internal `OneC` integration source by business reference. Each slice owns its source record, OData query/projection, manual import, synchronize-one flow, durable handler, and logging. `Common` contains no entity-set names, reference-field mapping, folder rules, WMS command selection, or workflow callback runner. Existing test files remain in place to minimize churn; they are retargeted to new contracts rather than multiplied to mirror production folders.

## Architectural Design Notes

- **Domain concepts first**: The WMS import commands remain the sole application owners of external identity lookup, create/update, version equality, lifecycle behavior, validation, conflicts, transactions, persistence, and domain events. Integration slices do not inspect WMS persistence or reimplement these rules.
- **Public/shared contract boundary**: `OneCImportResponse`, record/operation errors, notification request fields, routes, endpoint names, status codes, Problem Details, authorization policies, WebApp client behavior, and localization remain unchanged. No type is added to `Myrmex.Shared`.
- **Internal operation boundaries**: Each slice exposes one narrow import contract with `ImportAsync(CancellationToken)` and one narrow synchronize-one contract with `SynchronizeAsync(Guid, CancellationToken)`. Manual endpoints inject the matching import contract. Each durable handler injects the matching synchronizer, the pure handler-result mapper, and its typed logger. SKU alone depends on the explicit Unit-of-Measure synchronizer for bounded repair.
- **Source transport boundary**: `IOneCODataTransport`/`OneCODataTransport` owns the existing integration-wide configuration validation, request creation, Basic authentication, timeout, HTTP status classification, JSON envelope deserialization, and query encoding. Configuration validation continues to require all three entity-set settings plus the existing base URL, credentials, batch-size, and timeout rules before any manual import begins. Each slice source owns use of its configured entity set, exact `$select`, stable ordering, key filter, folder filter, current-object cardinality/version validation, paging, and source record type. The integration-wide connection test coordinates three explicit slice probes without becoming a reference business orchestrator.
- **Manual import orchestration**: Each slice explicitly acquires its gate, validates configuration, reads its source, maps records, dispatches its existing WMS command, accounts for the result, handles cancellation/errors, logs completion, and releases the lease. Invalid/disabled configuration remains a pre-start `400` and lease contention remains a pre-start `409`. After validation succeeds and source processing begins, authentication rejection, entity-set unavailability, malformed/source-unavailable/timeout failures, and unexpected application or batch failures remain incomplete `200 OK OneCImportResponse` results with safe `OperationError`; they do not become `502/504` import-route Problem Details. `OneCImportResponseFactory` may construct uniform response/error shapes and enforce the existing 50-error cap, but accepts data—not workflow delegates. The connection-test endpoint separately retains transport-failure Problem Details.
- **Reactive and internal synchronization**: Each synchronizer explicitly validates the key/cancellation, attempts the matching gate, reads the current object, handles reference-specific skips, maps and dispatches one WMS import command, classifies the result, logs, and returns `ReferenceSynchronizationResult`. Each concrete handler then follows `parse and validate ExternalId -> call matching synchronizer -> write structured correlation log -> map through the pure common mapper`. Invalid `ExternalId` produces and logs the equivalent permanent invalid-request result. The mapper receives only an already-produced result and does not parse requests, select a slice, invoke callbacks, or log.
- **SKU repair**: `StockKeepingUnitOneCSynchronizer` retains the SKU lease, recognizes only the two existing base-UoM error reasons under consistent one-item accounting, invokes `IUnitOfMeasureOneCSynchronizer` once, and retries the identical SKU command at most once. It introduces no recursion, type selection, graph, or generic dependency service.
- **Coordination and cancellation**: The singleton gate retains three independent non-waiting semaphores and current string identities. Manual acquisition continues to throw for HTTP 409; synchronize-one acquisition continues to return `Busy`. Manual leases span configuration validation through logging, including every SKU page/batch. Synchronize-one leases span source read through outcome classification and SKU repair. Caller cancellation behavior remains unchanged.
- **Cancellation and errors**: Manual callers continue to receive incomplete `Cancelled` responses, with committed SKU counts retained. Internal caller cancellation continues to propagate. Source timeout remains distinct from caller cancellation. Manual source/active-import failures retain incomplete `200` response semantics, while only pre-start configuration/lease failures use import-route Problem Details. Existing synchronize-one transport and service-error classifications remain exact, and no credentials or source payloads enter responses or logs.
- **Outcome compatibility**: A one-object WMS result with `Processed != 1` or otherwise inconsistent counts remains `PermanentFailure` with reason `ApplicationFailure` and `retrySuitable = false`. Issue #111 does not reclassify or throw this condition; reconsidering whether it should use an exception or the transient processor retry path is deferred to a separate issue.
- **Observability**: Each concrete Warehouse, UoM, and SKU handler owns one structured correlation log after receiving the internal result and before durable mapping. It records `SynchronizationRequestId`, `EntityType`, `ExternalId`, safely and deterministically rendered `NotifiedDataVersion` (Base64), `CurrentOutcome`, `CurrentReason`, and `RetrySuitable`; invalid external identity logs the equivalent permanent invalid-request values. Synchronizers retain reference-local operational logs. Credentials, secrets, and source payloads are excluded. The pure common mapper performs no logging.
- **Migration sequence**: (1) extract the common OData executor/result primitives and relocate the single gate without changing behavior; (2) move Warehouse source/import/synchronization/handler and rewire its callers while removing its old composite methods; (3) do the same for Unit of Measure and establish the explicit synchronize-one contract; (4) move SKU, wire its direct UoM dependency, and retain repair limits; (5) align connection test, endpoints, and DI, then delete the composite services, all-reference typed client, old DTO locations, delegate runners, central switch, obsolete registrations, and placeholder `OneC/OneCOptions.cs`; (6) minimally retarget existing tests and remove only the obsolete central-switch test. No slice retains a compatibility wrapper or parallel production path after its callers move.
- **Risk-based testing**: Existing tests protect source query shapes/mapping, import accounting/error limits, paging/partial commits/cancellation, gate scope, synchronize-one outcomes, durable mapping, route/auth contracts, and SKU repair limits. Constructor, fake, namespace, parameterization, and descriptive rename changes are permitted. In `StockKeepingUnitReferenceRepairTests`, the success case covers both `Applied` and `Unchanged`; the renamed retry-still-fails case proves permanent termination after the sole retry; one new compact theory covers `Busy`/`TransientFailure` as transient and `NotFound`/`ControlledSkip`/`PermanentFailure` as permanent, asserting one UoM call, one SKU dispatch, no retry, and no recursion. Correlation fields are code-review/quickstart acceptance unless an existing logging assertion can be trivially extended; no logging test suite is added. Durable Feature #104 and WebApp client suites remain unchanged unless a using directive must follow an internal namespace move.
- **Existing pattern precedence**: The design follows Myrmex's explicit request/handler boundaries and vertical-slice naming while retaining the accepted Feature #104 durable handler boundary and Feature #109 WMS import commands. Controlled local duplication is preferred over another generalized workflow abstraction.

## Removal Inventory

- Remove `OneC/Imports/IOneCImportService.cs` and `OneC/Imports/OneCImportService.cs` after all three manual endpoints use slice contracts.
- Remove `OneC/References/OneCReferenceSynchronizationService.cs`, including its interface, `SynchronizeAsync` type switch, delegate runner, shared reference workflows, and embedded SKU repair.
- Split `OneC/References/ReferenceSynchronizationHandlers.cs`: move each handler to its slice and retain only a pure result mapper in `Common`.
- Remove the all-reference `IOneCODataClient`/`OneCODataClient` typed-read surface after the common executor and three slice sources are wired.
- Move the three `Catalog_*` source DTOs into their owning slices under clear source-record names.
- Remove obsolete composite DI registrations and add explicit source/import/synchronizer registrations.
- Remove the placeholder `Myrmex.Integrations/OneC/OneCOptions.cs`; the real configuration remains under `OneC/Configuration/OneCOptions.cs`.
- Remove the test that exists only to prove the prohibited central `OneCReferenceType` switch; do not replace it with three equivalent tests.
