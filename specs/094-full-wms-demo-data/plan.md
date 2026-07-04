# Implementation Plan: Full WMS Demo Data Seeding

**Branch**: `094-full-wms-demo-data-seeding` | **Date**: 2026-07-04 | **Spec**: `specs/094-full-wms-demo-data/spec.md`

**Input**: Feature specification from `specs/094-full-wms-demo-data/spec.md`, `StakeholderDocs/094 Full WMS Demo Data Seeding.md`, the clarification session, the Myrmex constitution and durable architecture/testing/API guidance, and the current WMS implementation.

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Add conditionally registered API-only administrative actions that atomically seed or clear a small, coherent Russian-language WMS demonstration dataset. The implementation stays inside `Myrmex.Modules.Wms`: shared request/response records live in `Myrmex.Shared`, internal explicit commands coordinate a dedicated seeder and clear service, existing domain factories and inventory command handlers preserve WMS invariants, and the existing `WmsDbContext` owns one explicit SQL transaction per request.

The seed reconciles stable business identities before mutation, reuses compatible records, aborts on incompatible stable-code collisions, and creates supported opening adjustments, direct/cart transfers, and inventory-count states without `HasData` or a schema change. The clear action deletes all mutable WMS data in foreign-key-safe order while preserving system storage-location reference rows, schema, and migration history. A singleton non-waiting gate rejects overlapping seed/clear requests. Routes exist only when `Myrmex:Wms:DemoData:Enabled=true` and the host is not Production; clear additionally requires `AllowClear=true` and an exact JSON-body confirmation value.

## Technical Context

**Language/Version**: C# on the existing .NET 10 solution.

**Primary Dependencies**: Existing ASP.NET Core Minimal APIs, Options, logging, `TimeProvider`, EF Core 10 with SQL Server, `Myrmex.Core`, `Myrmex.AppDispatching`, `Myrmex.AspNetCore`, `Myrmex.Modules.Wms`, `Myrmex.Shared`, and `Myrmex.ApiService`. No new third-party runtime dependency is required.

**Storage**: Existing SQL Server-backed `WmsDbContext` and `wms` schema. No new table, column, index, `HasData` entry, or migration is planned. Existing system storage-location types/statuses and `__EFMigrationsHistory` are preserved during clear.

**Testing**: Existing `Myrmex.Tests` xUnit v3 project, SQL Server `TestWmsDbContext` fixture, focused Minimal API host tests, and existing domain/handler patterns. Add service/persistence tests for atomicity, idempotency, collision handling, clear ordering, and coherent operational state, plus focused endpoint tests for route registration, JSON binding, safety gates, status mapping, and cancellation. No WebApp client or UI automation is required because the feature is API-only and existing pages are validated manually.

**Target Platform**: Existing Aspire-hosted modular-monolith server with `Myrmex.ApiService` exposing the WMS module. Demo use assumes one API-service process and a dedicated non-production SQL Server database.

**Project Type**: Brownfield modular-monolith web application; API-only administrative vertical slice inside the existing WMS module.

**Performance Goals**: Complete seed from an empty schema-ready database within 2 minutes and clear-plus-reseed within 3 minutes under normal demo conditions. Return only bounded area summaries.

**Constraints**: Routes absent when disabled or Production; authenticated actor required through the existing claims helper; clear requires explicit enablement and exact confirmation; each operation is all-or-nothing; incompatible stable identity aborts seed; overlapping demo operations return conflict; no schema creation/migration, 1C changes, generic import, UI redesign, deployment work, SKU groups, or barcodes; execution commands remain developer-controlled.

**Scale/Scope**: One deterministic definition with 4 UoMs, 10 SKUs, 1 warehouse, 7 zones, 15 locations, 10–20 balances/ledger entries, 4 transfers, and 2 counts; two POST actions; one process-local gate; no multi-instance guarantee.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The design starts from units, SKUs, warehouse topology, balances, immutable inventory transactions/ledger entries, direct/transit transfers, and inventory counts. Stable identity, compatibility, quantity consistency, lifecycle state, and clear/reseed invariants are explicit.
- **Modular Monolith Boundaries**: PASS. All orchestration and persistence remain in `Myrmex.Modules.Wms`; public BCL-only request/response records are limited to `Myrmex.Shared`; `Myrmex.ApiService` only registers the module.
- **Vertical Slice Delivery**: PASS. Seed and clear each have a public contract, Minimal API action, internal explicit command/handler, WMS service/persistence orchestration, structured diagnostics, and focused tests. No UI/client slice applies to this API-only feature.
- **Testing Discipline**: PASS. SQL Server service tests own transaction rollback, FK-safe deletion, reconciliation, and data coherence. Focused endpoint tests own conditional mapping, actor/confirmation binding, and HTTP results. Existing domain-handler tests are not duplicated.
- **Simplicity and Observability**: PASS. The plan reuses EF transactions, current handlers/domain factories, result/ProblemDetails mapping, claims extraction, and structured logging. It adds no repository, generic seeding engine, job system, or service split.

## Project Structure

### Documentation (this feature)

```text
specs/094-full-wms-demo-data/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── demo-data-admin.openapi.yaml
├── checklists/
│   └── requirements.md
└── spec.md
```

### Source Code (repository root)
```text
Myrmex.Modules.Wms/
├── DemoData/
│   ├── Configuration/WmsDemoDataOptions.cs
│   ├── Endpoints/DemoDataAdminEndpoints.cs
│   └── Features/
│       ├── ClearWmsDemoData.cs
│       ├── DemoDataDefinitions.cs
│       ├── WmsDemoDataClearService.cs
│       ├── WmsDemoDataErrors.cs
│       ├── WmsDemoDataOperationGate.cs
│       ├── WmsDemoDataSeeder.cs
│       └── SeedWmsDemoData.cs
└── WmsModule.cs

Myrmex.Shared/Wms/DemoData/
├── ClearDemoDataRequest.cs
├── DemoDataAreaSummary.cs
└── DemoDataOperationResponse.cs

Myrmex.ApiService/
├── appsettings.json
└── appsettings.Development.json

Myrmex.Tests/Wms/DemoData/
├── Endpoints/DemoDataAdminEndpointTests.cs
├── Features/WmsDemoDataClearServiceTests.cs
├── Features/WmsDemoDataSeederTests.cs
└── Testing/DemoDataTestHost.cs

docs/demo-data.md
README.md
```

**Structure Decision**: Add a feature-local `DemoData` area within the WMS module because the operation spans Catalog, Topology, and Inventory but remains WMS-owned. Keep public transport records in `Myrmex.Shared`, reuse the existing test project, and add operator documentation under `docs/` with a README link. Do not add a project, persistence abstraction, WebApp client/page, or migration.

## Architectural Design Notes

- **Domain concepts first**: The definition names four UoMs, ten fastener SKUs, one warehouse, seven zones, fifteen locations, opening inventory, four transfers, and two counts. Existing factories normalize reference/topology data. Existing adjustment, transfer-movement, and count commands preserve balance, immutable ledger, transfer, rowversion, variance, and count invariants.
- **Shared contract boundary**: `ClearDemoDataRequest`, `DemoDataOperationResponse`, and `DemoDataAreaSummary` cross HTTP and contain only BCL types. Options, definitions, commands, services, domain entities, EF details, gate state, and logs remain internal to WMS.
- **Internal request boundary**: `SeedWmsDemoData.Command(actorId)` and `ClearWmsDemoData.Command(actorId, confirmation)` are explicit internal commands. Handlers delegate to scoped WMS services and return `ServiceResult<DemoDataOperationResponse>`; endpoints only bind, extract actor identity, dispatch, and map results.
- **Conditional registration**: `AddWmsModule` binds options from `Myrmex:Wms:DemoData` and registers the gate/services. `MapWmsModule` maps `/api/admin/demo-data` only when enabled and not Production. `AllowClear=false`, a blank configured token, a missing/incorrect request token, or a missing actor rejects before database access.
- **Seed orchestration**: Acquire one non-waiting process-local gate, verify connectivity/migration readiness, begin one explicit transaction, reconcile stable identities, then apply stages in dependency order. Reference/topology rows use domain factories. Inventory operations reuse existing command handlers where practical; direct aggregate construction is limited to stable-code transfer creation and cases where a current public use case cannot express deterministic identity. Every failed `ServiceResult` or exception rolls back and clears tracking.
- **Stable identity and compatibility**: UoM/SKU/warehouse/location use code; zone uses `(WarehouseId, Code)`; transfer uses code; opening adjustment uses an exact `DEMO-OPEN-*` reason plus SKU/location ledger pair; count uses an exact `DEMO-CNT-*` reason plus warehouse/line shape. Compatible stages are reused, missing stages are created, and incompatible or ambiguous matches return 409 before commit.
- **Atomicity**: Seed and clear each own one explicit `WmsDbContext` transaction. Existing handlers share the scoped context and remain inside the outer transaction. Clear wraps every `ExecuteDeleteAsync` call in that transaction. Commit occurs only after the complete success summary is known.
- **Clear boundary and order**: Delete count lines, transfer movements, transfer lines, counts, transfers, ledger entries, transactions, balances, SKU barcodes, storage locations, zones, SKUs, UoMs, and warehouses. Preserve all storage-location type/status rows, database/schema objects, and migration history. Return deleted counts per stage.
- **Supported topology vocabulary**: Reuse `DOCK`, `PALLET_RACK`, `SHELF`, `STAGING`, `FLOOR`, `INTERNAL_TRANSIT`, `AVAILABLE`, `BLOCKED`, and `INVENTORY_CHECK`. Do not add unsupported `CART`, `HOLD`, or `DAMAGED` reference values. Existing system-reference English labels remain unchanged; records created by this feature use Russian text.
- **Operational scenarios**: Opening adjustments create initial ledger history. One completed direct transfer uses `Move`; one completed cart transfer uses `Pick` then `Place`; one in-progress cart transfer remains picked to `CART-01`; one created direct transfer has no movement. One picking count remains InProgress with zero, shortage, and surplus lines; one bulk count applies zero-variance lines and completes.
- **Cancellation and errors**: Cancellation flows endpoint to dispatcher, service, EF, and child commands. Missing actor is 401; clear disabled/confirmation failures are 403; overlap and incompatible identities are 409; malformed request/configuration is 400; schema/transaction failures use safe 500 ProblemDetails. Failure responses contain no summary or secret. Logs capture operation, actor, environment, outcome, duration, category, and counts without confirmation values.
- **Risk-based testing**: Seeder SQL Server tests own bounded content, Russian text, repeat idempotency, partial resume, collision/mid-stage rollback, stable identity, balance/ledger coherence, transfer states, count variance/state, and absent barcodes. Clear tests own complete deletion, user-created record deletion, system/schema/history preservation, FK order, and rollback. Endpoint tests own route gating, 401/403/409, JSON binding, serialization, and cancellation. Do not repeat every record assertion through HTTP or duplicate existing transfer/count domain matrices.
- **Existing pattern precedence**: Reuse `WmsDbContext`, `ICommandDispatcher`, nested command/handler slices, `ServiceResult<T>`, `ToHttpResult`, `HttpContext.GetActorId`, `TimeProvider`, `ILogger`, `TestWmsDbContext`, and the OneC-style gate/test host. Do not add MediatR, repositories, a generic seeding framework, a new result envelope, or production behavior changes.

## Complexity Tracking

No constitution violations or automated-test exceptions are required. The feature adds no project, schema migration, UI behavior, external framework, or service boundary.

## Phase 0: Research Output

`research.md` resolves module ownership, configuration/route gating, actor handling, transaction semantics, operation locking, reconciliation identities, domain-use-case reuse, supported dataset vocabulary, clear scope/order, response/error mapping, observability, schema readiness, and risk-based test ownership. No open planning clarification remains.

## Phase 1: Design Outputs

- `data-model.md` defines configuration, non-persisted summaries/definitions, stable identities, compatibility rules, the bounded dataset, supported lifecycle states, and existing persisted entities affected by seed/clear.
- `contracts/demo-data-admin.openapi.yaml` defines conditional route availability, seed and clear POST contracts, JSON confirmation, success summaries, and ProblemDetails failures.
- `quickstart.md` provides configuration, developer-controlled validation commands, API calls, reset/reseed checks, WebApp walkthrough, diagnostics checks, and negative scenarios.
- `AGENTS.md` is refreshed through the installed agent-context extension to point active work at `specs/094-full-wms-demo-data/plan.md`.

## Task Generation Guard

The future `tasks.md` must order focused tests before implementation, then cover shared contracts/options/registration, gate and errors, seeder, clear service, endpoints, configuration/docs, and manual validation. Tasks must list builds, tests, startup, database work, and infrastructure operations as developer-controlled commands.

Tasks must not add `HasData` operational records, a demo manifest/history table, schema migration, SKU groups/barcodes, 1C changes, a generic import/seeding framework, distributed locking, background jobs, production reset behavior, WebApp redesign, authorization infrastructure, deployment changes, or unrelated refactoring.

## Developer-Controlled Validation Commands

Planning did not run builds, tests, application startup, migrations, database updates, Docker, or infrastructure operations. `quickstart.md` lists recommended developer-run commands. No migration command is expected because the plan changes no persisted shape.

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define stable identities, compatibility, reference mappings, quantity/ledger consistency, transfer progression, count snapshots/variance, and clear/reseed invariants using current WMS vocabulary.
- **Modular Monolith Boundaries**: PASS. WMS owns orchestration and persistence; shared records are transport-only; host changes are configuration/registration only.
- **Vertical Slice Delivery**: PASS. Both actions have contracts, endpoints, internal commands, services, persistence behavior, diagnostics, and focused tests. API-only scope is explicit.
- **Testing Discipline**: PASS. Tests are assigned to SQL Server service or HTTP boundary ownership, with existing domain matrices reused and manual WebApp validation covering unchanged UI.
- **Simplicity and Observability**: PASS. Built-in options, transactions, bulk delete, logging, and a process-local gate solve the demo problem without a generic framework or distributed architecture.
