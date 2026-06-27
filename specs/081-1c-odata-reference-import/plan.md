# Implementation Plan: 1C OData Reference Import MVP

**Branch**: `081-1c-odata-reference-import-mvp` | **Date**: 2026-06-27 | **Spec**: `specs/081-1c-odata-reference-import/spec.md`

**Input**: Feature specification from `specs/081-1c-odata-reference-import/spec.md`, `StakeholderDocs/081 1C OData Reference Import MVP.md`, the supplied planning decisions, the Myrmex constitution and durable architecture/testing/API guidance, and the current WMS reference-data implementation.

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Add a manually triggered, synchronous, one-way 1C OData integration for connection testing and separate warehouse, unit-of-measure, and nomenclature/SKU imports. A new `src/Myrmex.Integrations/Myrmex.Integrations.csproj` project owns all `Myrmex.Integrations.OneC` transport concerns and maps 1C DTOs into public neutral WMS batch commands. WMS owns validation, source identity, conflict handling, lifecycle changes, atomic batch persistence, and idempotent upsert behavior.

Imported `Warehouse`, `UnitOfMeasure`, and `StockKeepingUnit` records gain nullable `ExternalRefKey` and `LastImportedAtUtc` metadata plus filtered unique source-identity indexes. UoMs come from `Catalog_УпаковкиЕдиницыИзмерения`; each SKU carries its own `ЕдиницаИзмерения_Key`, which WMS resolves to an active imported UoM by `ExternalRefKey`. Nomenclature is read in stable `Ref_Key` order with `$select`, `$top`, and `$skip`. Each accepted WMS batch commits atomically; earlier batches remain committed after a later failure and only committed-batch counts are returned. Same-type imports use a non-waiting process-local gate, so the MVP assumes one `Myrmex.ApiService` instance and defers distributed locking.

The WebApp adds a Russian-labelled `Интеграции → 1С` page and typed client. Public response contracts live in `Myrmex.Shared`; 1C/OData DTOs, field names, credentials, and query details never cross the OneC boundary. Background jobs, polling, persistent import history, full localization, an auth baseline, `ExternalSystem`, and inventory-accounting refactoring remain out of scope.

## Technical Context

**Language/Version**: C# on the existing .NET 10 solution.

**Primary Dependencies**: Existing ASP.NET Core Minimal APIs, `HttpClient`/`IHttpClientFactory`, `System.Text.Json`, EF Core 10 with SQL Server, Blazor interactive server components, MudBlazor 9, xUnit v3, `Myrmex.Core`, `Myrmex.AppDispatching`, `Myrmex.AspNetCore`, `Myrmex.Modules.Wms`, `Myrmex.Shared`, `Myrmex.ApiService`, and `Myrmex.WebApp`. No new third-party runtime framework is required.

**Storage**: Existing SQL Server-backed `WmsDbContext` and `wms` schema. Add nullable `ExternalRefKey` and `LastImportedAtUtc` columns to warehouses, units of measure, and SKUs, with one filtered unique `ExternalRefKey IS NOT NULL` index per table. Do not add integration-history tables or `ExternalSystem`. EF migration generation and database application remain developer-controlled.

**Testing**: Existing `Myrmex.Tests` xUnit project, SQL Server persistence fixture, focused Minimal API test host, and stub `HttpMessageHandler` patterns. Add risk-based WMS domain/handler/persistence tests, OneC OData client and orchestration tests, focused endpoint contract tests, WebApp API-client tests, and manual page smoke validation. Avoid duplicating the same upsert matrix across all layers and reference types.

**Target Platform**: Existing Aspire-hosted modular-monolith server: `Myrmex.ApiService` for HTTP endpoints and `Myrmex.WebApp` for the operator UI. 1C is an online deployment dependency configured per environment.

**Project Type**: Brownfield modular-monolith web application with a new in-process integration adapter project and existing WMS vertical slices.

**Performance Goals**: Preserve the five-second connection-check target for a healthy representative endpoint; process at least 15,000 nomenclature records without omission or duplication; keep memory bounded to one configured source batch plus at most 50 returned record errors; display the result within two seconds after the synchronous operation finishes.

**Constraints**: Use `/api/integrations/1c`; use `1С` in user-facing text and `OneC` in C# names; deterministic OData paging; one explicit transaction per accepted batch; no failed-batch counts; synchronous request/response only; reject concurrent same-type imports; one primary source; secure runtime credentials only; no new auth baseline, full localization, background processing, polling, persistent history, messaging/outbox, `ExternalSystem`, or inventory-accounting changes. Planning must not run builds, tests, migrations, database updates, application startup, Docker, or infrastructure commands.

**Scale/Scope**: One configured 1C publication, three reference types, four public actions, at least 15,000 SKUs, default batch size 1,000 with range 1–5,000, and a per-OData-request timeout default of 30 seconds. The MVP lock is process-local and safe only for one `Myrmex.ApiService` instance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan starts from warehouse, unit-of-measure, SKU, source identity, base UoM assignment, lifecycle, code conflict, import timestamp, batch, and import-result rules. WMS commands own these invariants before transport details are applied.
- **Modular Monolith Boundaries**: PASS. `Myrmex.Integrations` is an in-process adapter with the one-way dependency `Myrmex.Integrations -> Myrmex.Modules.Wms`; WMS has no integration dependency. Public WebApp contracts are limited to BCL-only `Myrmex.Shared` records. Cross-boundary work uses explicit WMS commands and module registration.
- **Vertical Slice Delivery**: PASS. The design covers OneC transport, WMS commands/handlers/domain/persistence, Minimal API routes, shared responses, WebApp client/page/navigation, diagnostics, and focused tests. Transport DTOs, neutral internal commands, and public response contracts remain separate.
- **Testing Discipline**: PASS with the documented UI automation exception below. Tests target source query/deserialization, WMS upsert/transaction/index behavior, orchestration/locking/count aggregation, endpoint routing/serialization/auth checks, and client mapping at the lowest owning layer.
- **Simplicity and Observability**: PASS. The design uses built-in HTTP/JSON/options/time/locking primitives and existing result/ProblemDetails patterns. Structured logs and categorized results cover operational failures without adding a job system, distributed service, or generic import framework.

## Project Structure

### Documentation (this feature)

```text
specs/081-1c-odata-reference-import/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── onec-integration.openapi.yaml
├── checklists/
│   └── requirements.md
└── spec.md
```

### Source Code (repository root)
```text
src/Myrmex.Integrations/
├── Myrmex.Integrations.csproj
├── OneC/
│   ├── Configuration/OneCOptions.cs
│   ├── Endpoints/OneCEndpoints.cs
│   ├── Imports/
│   │   ├── OneCImportGate.cs
│   │   └── OneCImportService.cs
│   ├── Transport/
│   │   ├── OneCODataClient.cs
│   │   ├── OneCODataCollectionResponse.cs
│   │   ├── Catalog_Номенклатура.cs
│   │   ├── Catalog_Склады.cs
│   │   └── Catalog_УпаковкиЕдиницыИзмерения.cs
│   └── OneCIntegrationModule.cs
└── Properties/AssemblyInfo.cs

Myrmex.Modules.Wms/
├── Catalog/
│   ├── Domain/
│   │   ├── StockKeepingUnits/StockKeepingUnit.cs
│   │   └── UnitsOfMeasure/UnitOfMeasure.cs
│   └── Features/Imports/
│       ├── ImportStockKeepingUnits.cs
│       ├── ImportUnitsOfMeasure.cs
│       └── ReferenceImportBatchResult.cs
├── Topology/
│   ├── Domain/Warehouses/Warehouse.cs
│   └── Features/Imports/ImportWarehouses.cs
└── Infrastructure/Persistence/
    ├── Configurations/{Warehouse,UnitOfMeasure,StockKeepingUnit}Configuration.cs
    ├── Migrations/*_AddOneCExternalReferenceMetadata.cs
    ├── WmsDatabaseNames.cs
    └── WmsDbContext.cs

Myrmex.Shared/Integrations/OneC/
├── OneCConnectionTestResponse.cs
├── OneCImportOperationError.cs
├── OneCImportRecordError.cs
└── OneCImportResponse.cs

Myrmex.ApiService/
├── Myrmex.ApiService.csproj
└── Program.cs

Myrmex.WebApp/
├── Components/
│   ├── Layout/NavMenu.razor
│   └── Pages/Integrations/OneC/Index.razor
├── Integrations/OneC/OneCIntegrationApiClient.cs
└── Program.cs

Myrmex.Tests/
├── Integrations/OneC/
│   ├── Client/OneCODataClientTests.cs
│   ├── Endpoints/OneCEndpointTests.cs
│   ├── Imports/OneCImportServiceTests.cs
│   └── Web/OneCIntegrationApiClientTests.cs
└── Wms/
    ├── Catalog/Features/Imports/
    ├── Catalog/Persistence/
    ├── Topology/Features/Imports/
    └── Topology/Persistence/

Myrmex.slnx
```

**Structure Decision**: Add exactly one integration adapter project at the required `src/Myrmex.Integrations` path. The adapter contains integration-owned endpoints but delegates every WMS mutation to public neutral WMS commands. `Myrmex.ApiService` references and registers the adapter; `Myrmex.WebApp` depends only on shared transport records. Existing WMS reference screens and inventory handlers stay in place.

## Architectural Design Notes

- **Domain concepts first**: `ExternalRefKey` is the sole imported identity. New identity plus unused code creates; known identity updates the same entity; a code collision with another entity is reported and skipped; code never establishes identity. `DeletionMark` deactivates a linked record without validating or applying source detail fields, refreshes its import timestamp, and skips/reports an unlinked record as `SourceRecordDeletionMarked`; it never deletes. A valid non-deleted re-import reactivates a previously source-deactivated entity. `LastImportedAtUtc` changes only for successfully applied records.
- **Shared contract boundary**: `Myrmex.Shared.Integrations.OneC` contains only connection and import response records used by API and WebApp. It contains no OData DTOs, domain entities, EF types, internal commands, options, locks, clients, or UI state.
- **Internal request boundary**: Public WMS command shells `ImportWarehouses`, `ImportUnitsOfMeasure`, and `ImportStockKeepingUnits` expose nested neutral `Item` records and return a neutral batch result. Handlers and persistence stay internal to WMS. OneC maps source DTOs into items and dispatches them through `ICommandDispatcher`.
- **Source mapping**: All source codes are trimmed before WMS normalization. UoM maps `НаименованиеПолное` when non-empty, otherwise `Description`, to `Name`; it maps `МеждународноеСокращение` when non-empty, otherwise `Description`, to `Symbol`. SKU maps `НаименованиеПолное` when non-empty, otherwise `Description`, to `Name`; `Артикул` stays transport-only. Warehouse maps `Description -> Name`, skips `IsFolder=true`, and uses source `Code` only when configured as available. An unavailable/empty warehouse code falls back only for warehouses to `Ref_Key.ToString("N").ToUpperInvariant()`.
- **Required SKU base UoM**: Nomenclature `ЕдиницаИзмерения_Key` maps directly to nullable `ImportStockKeepingUnits.Item.BaseUnitOfMeasureExternalRefKey`. WMS resolves it to one active imported UoM by `ExternalRefKey`. Missing/empty, not-imported, and inactive identities fail only that SKU record with `BaseUnitOfMeasureExternalRefKeyMissing`, `BaseUnitOfMeasureNotImported`, or `BaseUnitOfMeasureInactive`. Code matching and one-default-UoM behavior are prohibited.
- **Publication compatibility**: `UnitsOfMeasureEntitySet` uses `Catalog_УпаковкиЕдиницыИзмерения`. `WarehouseCodeAvailable` controls whether warehouse queries select source `Code`; `UseFolderFilter` controls whether warehouse/nomenclature queries request `$filter=IsFolder eq false`. When filtering is disabled for compatibility, folder records remain in the DTO stream and are held as pending `SourceFolder` skips until that source batch completes.
- **Atomic batch persistence**: Each WMS handler preloads relevant source identities and codes, accumulates record outcomes, opens one database transaction, applies only accepted mutations, calls one WMS save/event-dispatch unit, and commits only after it succeeds. Batch-level persistence or dispatch failure rolls back that batch. The orchestrator adds counts only after the handler returns a committed result.
- **OData paging**: Nomenclature requests use `$format=json`, `$orderby=Ref_Key`, `$skip`, `$top`, and `$select=Ref_Key,DeletionMark,IsFolder,Code,Description,НаименованиеПолное,Артикул,ЕдиницаИзмерения_Key`. Offset advances by returned count; paging stops when fewer than `BatchSize` records return. Warehouse `$select` is `Ref_Key,DeletionMark,IsFolder,Code,Description` when `Code` is available and omits `Code` otherwise. UoM uses `$select=Ref_Key,DeletionMark,Code,Description,НаименованиеПолное,МеждународноеСокращение`. Prefer `$filter=IsFolder eq false` for warehouse/nomenclature when enabled and supported; otherwise retain `IsFolder` and report folders as skipped.
- **Synchronous orchestration and locking**: `OneCImportService` holds a keyed singleton `SemaphoreSlim` for the selected reference type using zero-timeout acquisition and releases it in `finally`. Different types may run concurrently. The gate is process-local; production remains single-instance until a distributed lock is separately designed.
- **Cancellation and errors**: Cancellation flows endpoint → orchestrator → OData client → dispatcher → EF. Pre-start configuration, user authorization, and already-running failures use existing ProblemDetails conventions. An import that starts and later fails returns `OneCImportResponse` with `IsComplete=false`, committed-batch counts, and an operation error. Record errors are capped at 50 while totals remain complete.
- **Authorization boundary**: Do not add authentication/authorization services or policies. Endpoints use the existing authenticated-actor check and return 401 without an actor. Document `Wms.Integrations.OneC.Import` as the intended future policy name without registering it in this feature.
- **WebApp behavior**: A typed client uses `ApiResult<T>` and existing ProblemDetails parsing. The Russian page disables the running action, shows synchronous progress, keeps only the latest result in component state, and renders counts plus capped errors. No resource framework or broad translation is added.
- **Risk-based testing**: WMS tests own idempotency, source/code conflicts, lifecycle, per-SKU base-UoM resolution and failure reasons, filtered indexes, rollback, and committed counts. OneC client tests own exact Unicode `$select`/folder-filter queries, DTO deserialization, optional warehouse code, deterministic warehouse fallback, paging, timeout, and malformed/upstream responses. Orchestrator tests own mapping, folder skips, aggregation, partial failure, cancellation, and locking. Focused endpoint tests own routes, 401/409/200 serialization; WebApp client tests own route and ProblemDetails mapping. UI uses manual smoke validation.
- **Existing pattern precedence**: Reuse `WmsDbContext`, internal handlers, `ServiceResult<T>`, `ToHttpResult`, `HttpContext.GetActorId`, `ApiResult<T>`, typed clients, `StubHttpMessageHandler`, Minimal API test hosting, and reference lifecycle methods. Do not add MediatR, repositories, generic integration engines, or a new result framework.

## Phase 0: Research Output

`research.md` records resolved decisions for project/dependency direction, DTO isolation, source mapping, SKU base UoM resolution, OData query/paging behavior, batch transaction/count semantics, process-local locking, synchronous responses, error/authorization behavior, EF indexes/migration, UI localization scope, observability, and risk-based tests. No `NEEDS CLARIFICATION` item remains.

## Phase 1: Design Outputs

- `data-model.md` defines the three imported WMS entities, integration configuration, neutral batch items/results, shared responses, lifecycle transitions, source ownership, indexes, base-UoM resolution, and non-persisted transport fields.
- `contracts/onec-integration.openapi.yaml` defines the four `/api/integrations/1c` actions, complete/incomplete responses, ProblemDetails failures, response schemas, error categories, and no-body POST requests.
- `quickstart.md` provides configuration, developer-controlled build/test/migration/start commands, OData request expectations, API/UI validation scenarios, single-instance assumptions, and non-goal checks. No command is executed by planning.
- `AGENTS.md` is updated between Spec Kit markers to direct active feature work to this plan while preserving durable-memory routing.

## Task Generation Guard

The future `tasks.md` must order focused tests before the implementation tasks they protect and separate UI work into its own phase. Tasks may add only the integration project, neutral WMS import slices and metadata, API/shared contracts, WebApp integration page/client, registration, focused tests, documentation, and a developer-generated migration.

Tasks must not add scheduled/background execution, polling, persistent job/import history, RabbitMQ/outbox, bidirectional exchange, documents/orders/balances/prices, mapping or conflict-resolution administration, `ExternalSystem`, multiple-source support, distributed locking, full localization, an authentication/authorization baseline, broad reference-data abstractions, inventory-accounting handler refactoring, or changes to inventory count/manual move behavior.

## Developer-Controlled Validation and Migration Commands

Build, test, startup, migration generation/application, database updates, Docker, and infrastructure commands were not run during planning. `quickstart.md` lists commands for a developer to run after implementation. The required schema change is expected to produce:

- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddOneCExternalReferenceMetadata.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddOneCExternalReferenceMetadata.Designer.cs`
- an updated `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define imported identity, source-owned fields, code conflicts, base-UoM assignment, lifecycle transitions, import timestamps, batch/result rules, and explicit WMS command ownership.
- **Modular Monolith Boundaries**: PASS. The adapter has one-way dependencies into WMS and shared infrastructure; WMS does not reference it. OData DTOs remain private to `Myrmex.Integrations.OneC`, and shared contracts stay BCL-only.
- **Vertical Slice Delivery**: PASS. Data model, OpenAPI contract, quickstart, and source structure cover endpoint, shared response, OneC orchestration, neutral WMS command, domain/persistence, WebApp client/page, diagnostics, and tests for every action.
- **Testing Discipline**: PASS with the UI exception below. Each material risk has a lowest owning automated layer; endpoint tests are included because routes, auth checks, status codes, and serialization are new. Duplicate three-type matrices and generic framework behavior are intentionally omitted.
- **Simplicity and Observability**: PASS. The design uses built-in primitives and existing conventions, caps user-facing errors, logs structured outcomes without secrets, and documents the single-instance lock constraint instead of claiming distributed safety.

No architecture complexity exception is requested. The separate integration project is a user-mandated modular boundary, not a distributed service or constitution violation.

## Complexity Tracking

No architecture complexity exceptions are requested.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| OneC integration page component automation | The repository has no established Blazor component-test framework, and adding one for a single synchronous action page is disproportionate. | WMS handler/persistence tests, OneC client/orchestrator tests, focused endpoint tests, and WebApp API-client tests protect domain, transport, result, and error behavior. | Run the `quickstart.md` UI smoke: Russian navigation/labels, disabled in-progress action, complete and incomplete summaries, capped errors, and separate actions. | No. Add a cross-cutting UI-test issue only if the project adopts component automation generally. |
