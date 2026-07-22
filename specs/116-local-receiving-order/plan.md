# Implementation Plan: Local Receiving Order MVP

**Branch**: `116-implement-local-receiving-order-mvp-with-atomic-inventory-posting` | **Date**: 2026-07-22 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/116-local-receiving-order/spec.md`

**Planning Status**: Complete and ready for task generation

## Summary

Add a Receiving-owned vertical slice to the existing WMS module. A `ReceivingOrder` aggregate with separately persisted lines will support Draft create, full-plan reconciliation, Draft deletion, Start, incremental line receipt, and idempotent Complete. Completion will directly update or create existing Inventory Balances, create one multi-entry `Receiving` Inventory Transaction, set the completed order invariant, and persist the entire outcome through one EF Core `SaveChangesAsync` call.

Reuse the existing StorageLocation entity and taxonomy by adding one seeded system type with the planning code `RECEIVING`. The current `DOCK` type is not reused because it represents both receiving and shipping docks while dock behavior is outside this feature. Existing warehouse-scoped location lookup, active topology rules, inventory-balance eligibility, rowversion, unique-constraint mapping, list/query, Minimal API, Problem Details, MudBlazor, localization, and operational logging patterns remain authoritative. No external integration, generalized workflow, inventory-posting engine, location-capability framework, or new test infrastructure is introduced.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`)

**Primary Dependencies**: ASP.NET Core Minimal APIs, Entity Framework Core SQL Server 10.0.10, the existing command/query dispatchers and `ServiceResult` mappings, Blazor WebApp, MudBlazor 9, and existing WMS shared contracts

**Storage**: Existing SQL Server `wms` schema; two new Receiving tables, one new seeded `RECEIVING` StorageLocationType row, restrictive foreign keys, SQL Server rowversion on Receiving Order, existing Inventory Balance/Transaction/Ledger tables, and one generated migration

**Testing**: The current tracked repository and solution contain no test project or test sources. Use deterministic domain/API/persistence/WebApp acceptance scenarios in [quickstart.md](quickstart.md), existing build and application smoke paths, and the 300-line functional dataset. Do not restore or create a test project, browser framework, benchmark, or load-test harness as part of this feature.

**Target Platform**: Existing ASP.NET Core API and Blazor WebApp, locally orchestrated through the current Aspire host

**Project Type**: Modular-monolith web application with WMS module, shared contracts, Minimal API, SQL Server persistence, and server-rendered Blazor WebApp

**Performance Goals**: No latency, throughput, or load target. Exactly 300 planned lines is a deterministic functional acceptance dataset, not a performance SLA.

**Constraints**: Local-only workflow; statuses exactly Draft/InProgress/Completed; base-unit `decimal(18,4)` quantities; globally unique normalized user-entered Number; retained Draft line IDs preserved; only aggregate-level concurrency; no inventory effect before completion; one Receiving transaction and one positive ledger entry per line; one save for order/balances/transaction/entries; no automatic posting retry; Draft-only physical deletion; Receiving location must use the active `RECEIVING` type and pass existing eligibility; full-page create/edit/execution; no new test infrastructure

**Scale/Scope**: One aggregate and line entity, eight endpoints including Draft deletion, five mutating workflows, two read workflows, one topology seed plus demo Receiving location, one server-driven list page, one reusable full-page Draft editor, one full-page execution view, one small quantity dialog, and functional validation with up to 300 distinct SKU lines

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

### Pre-Research Gate

- **I. Clear Warehouse Behavior — PASS**: Receiving has explicit statuses, base-unit quantity rules, Receiving-only location classification, full-receipt completion, no pre-completion inventory effect, and a distinct positive Receiving transaction.
- **II. Explicit Ownership — PASS**: Receiving owns the order, lines, lifecycle, and orchestration; Inventory remains the sole owner of balances, transactions, and ledger entries; Topology remains the owner of warehouses, locations, and location types; Shared and WebApp expose/use contracts without owning domain rules.
- **III. Outcome-First Simplicity — PASS**: The design adds one aggregate, one line entity, one system location type, narrowly scoped vertical slices, and one multi-entry transaction factory while reusing existing dispatching, persistence, lookup, list, error, logging, API-client, and UI patterns.

### Post-Design Gate

- **I. Clear Warehouse Behavior — PASS**: [data-model.md](data-model.md) defines the lifecycle invariant, quantity constraints, Receiving location eligibility, line reconciliation, atomic posting, idempotency, and Draft deletion without introducing unrelated warehouse concepts.
- **II. Explicit Ownership — PASS**: [receiving-orders-api-contract.md](contracts/receiving-orders-api-contract.md) and [receiving-orders-webapp-contract.md](contracts/receiving-orders-webapp-contract.md) keep Receiving commands inside the Receiving capability, use Topology only for eligible lookup, and delegate all quantity/history mutation to existing Inventory domain behavior.
- **III. Outcome-First Simplicity — PASS**: The completed design uses one `RECEIVING` seed instead of a capability framework, one aggregate rowversion instead of line versions, explicit restrictive deletion instead of soft delete, one focused SKU dialog instead of hundreds of autocomplete controls, and one EF save instead of events, retries, or a posting framework.

All constitution gates pass. No exception or complexity justification is required.

## Project Structure

### Documentation (this feature)

```text
specs/116-local-receiving-order/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── receiving-orders-api-contract.md
│   └── receiving-orders-webapp-contract.md
└── tasks.md                              # Generated separately by /speckit-tasks
```

### Source Code (repository root)

```text
Myrmex.Modules.Wms/
├── Receiving/
│   ├── Domain/ReceivingOrders/
│   │   ├── ReceivingOrder.cs
│   │   ├── ReceivingOrderLine.cs
│   │   └── ReceivingOrderStatus.cs
│   ├── Features/ReceivingOrders/
│   │   ├── CreateReceivingOrder.cs
│   │   ├── UpdateReceivingOrderDraft.cs
│   │   ├── DeleteReceivingOrderDraft.cs
│   │   ├── StartReceivingOrder.cs
│   │   ├── ReceiveReceivingOrderLine.cs
│   │   ├── CompleteReceivingOrder.cs
│   │   ├── GetReceivingOrderById.cs
│   │   ├── ListReceivingOrders.cs
│   │   ├── ReceivingOrderQueryableExtensions.cs
│   │   ├── ReceivingOrderEligibility.cs
│   │   ├── ReceivingOrderErrors.cs
│   │   └── ReceivingOrderVersion.cs
│   └── Endpoints/
│       ├── ReceivingEndpoints.cs
│       └── ReceivingOrderEndpoints.cs
├── Inventory/
│   └── Domain/InventoryTransactions/
│       ├── InventoryTransaction.cs
│       └── InventoryTransactionType.cs
├── Topology/
│   └── Features/StorageLocations/LookupStorageLocations.cs
├── DemoData/Features/
│   ├── DemoDataDefinitions.cs
│   └── WmsDemoDataSeeder.cs
└── Infrastructure/Persistence/
    ├── WmsDbContext.cs
    ├── WmsDatabaseNames.cs
    ├── WmsPersistenceExceptionMapper.cs
    ├── WmsSeedIds.cs
    ├── Configurations/
    │   ├── ReceivingOrderConfiguration.cs
    │   ├── ReceivingOrderLineConfiguration.cs
    │   └── StorageLocationTypeConfiguration.cs
    └── Migrations/

Myrmex.Shared/
└── Wms/Receiving/
    ├── ReceivingOrderListRequest.cs
    ├── ReceivingOrderListItem.cs
    ├── ReceivingOrderDetails.cs
    ├── ReceivingOrderLineDetails.cs
    ├── ReceivingOrderStatusDetails.cs
    ├── ReceivingOrderSortBy.cs
    ├── CreateReceivingOrderRequest.cs
    ├── UpdateReceivingOrderRequest.cs
    ├── ReceiveReceivingOrderLineRequest.cs
    └── ReceivingOrderActionRequest.cs

Myrmex.WebApp/
├── Wms/
│   ├── Api/WmsApiClientHttp.cs
│   └── Receiving/WmsReceivingApiClient.cs
├── Components/Pages/Wms/Receiving/ReceivingOrderPages/
│   ├── Index.razor
│   ├── Index.razor.cs
│   ├── ReceivingOrderGrid.razor
│   ├── ReceivingOrderFilters.razor
│   ├── ReceivingOrderGridRequest.cs
│   ├── ReceivingOrderDraftPage.razor
│   ├── ReceivingOrderDraftPage.razor.cs
│   ├── ReceivingOrderDetailsPage.razor
│   ├── SelectReceivingOrderSkuDialog.razor
│   └── ReceiveReceivingOrderLineDialog.razor
├── Components/Layout/NavMenu.razor
└── Resources/Localization/SharedResource*.resx
```

**Structure Decision**: Create a top-level Receiving capability inside the existing WMS module because it owns a distinct warehouse document while coordinating with existing Topology and Inventory entities. Keep persistence in the existing WMS context and schema, expose contracts through the existing Shared project, and add a dedicated WebApp client/pages without a new project or framework.

## Phase 0: Research Output

[research.md](research.md) records the capability boundary, aggregate design, Receiving location type, persistence constraints, Draft reconciliation/deletion, version transport, atomic posting, concurrent completion behavior, public interface, UI strategy, logging, demo data, and proportional validation decisions. No planning clarification remains.

## Phase 1: Design & Contracts

- [data-model.md](data-model.md) defines Receiving Order/Line fields, relationships, constraints, state transitions, topology classification, inventory posting inputs, and concurrency/idempotency rules.
- [receiving-orders-api-contract.md](contracts/receiving-orders-api-contract.md) defines shared request/response shapes, routes, status/error semantics, list behavior, and version handling.
- [receiving-orders-webapp-contract.md](contracts/receiving-orders-webapp-contract.md) defines the list, full-page Draft editor, eligible location lookup, focused SKU selector, execution page, local 300-line handling, and conflict UX.
- [quickstart.md](quickstart.md) defines migration review, runnable post-implementation commands, deterministic local workflow validation, Draft identity/deletion, eligibility, atomicity, idempotency, concurrency, list behavior, and the 300-line functional scenario.

## Complexity Tracking

No constitution violation is present.
