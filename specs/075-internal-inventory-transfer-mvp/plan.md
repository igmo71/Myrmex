# Implementation Plan: Internal Inventory Transfer MVP

**Branch**: `075-internal-inventory-transfer-mvp` | **Date**: 2026-06-23 | **Spec**: `specs/075-internal-inventory-transfer-mvp/spec.md`

**Input**: Feature specification from `specs/075-internal-inventory-transfer-mvp/spec.md`, stakeholder document `StakeholderDocs/Wms/Inventory/075 Internal Inventory Transfer MVP.md`, user planning constraints, Myrmex Constitution, durable architecture/testing/API guidance, implemented Inventory Adjustment Ledger, Inventory Balance, Inventory Ledger history, and current WMS topology patterns.

## Summary

Add the first Inventory Transfer vertical slice for controlled internal movement of SKU inventory inside one warehouse. The feature introduces `InventoryTransfer`, `InventoryTransferLine`, and immutable `InventoryTransferMovement` records while reusing the existing Inventory Balance snapshot and Inventory Ledger transaction/entry mechanism for inventory effects. Direct movement uses source storage to destination storage. Transit movement uses source storage to one nullable internal transit location, then internal transit to destination storage.

The design derives transfer execution pattern from nullable `InventoryTransfer.TransitStorageLocationId`; it does not persist `TransferExecutionMode` or `MovementType`. Each committed movement creates one `InventoryTransaction` with `InventoryTransactionType.Transfer = 2`, two `InventoryLedgerEntry` records, balance updates for the from and to locations, and one `InventoryTransferMovement` storing the created `InventoryTransactionId`. Transfer-specific references stay on transfer movement, not on `InventoryTransaction`.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET 10 solution.

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core SQL Server provider, Blazor WebApp, MudBlazor, existing Myrmex command/query dispatchers, `ServiceResult`/ProblemDetails helpers, `ListResult<T>`, `WmsInventoryApiClient`, xUnit test project.

**Storage**: Existing SQL Server-backed WMS schema through `WmsDbContext`. Add transfer entities/mappings, extend seeded storage-location types with `InternalTransit` and `ExternalTransit`, and extend `InventoryTransactionType` with `Transfer = 2`. EF migration is required during implementation but must not be generated during planning.

**Testing**: Existing xUnit tests. Follow risk-based minimal testing: domain tests for transfer invariants/progress/status, handler/persistence tests for create/move/pick/place atomic behavior and balance/ledger effects, persistence tests for mapping and relationships, focused endpoint/API-client tests for contracts and routes, manual UI smoke validation for Blazor pages/dialogs.

**Target Platform**: Existing Myrmex modular-monolith API service and Blazor WebApp.

**Project Type**: Brownfield WMS vertical slice spanning shared contracts, WMS backend domain/application/persistence/endpoints, WebApp API client, and Blazor UI.

**Performance Goals**: Supervisors can create transfers with up to 20 lines in under 2 minutes. Operators can commit direct move, pick, or place in under 30 seconds after selecting the line and quantity. Transfer details load in under 3 seconds for up to 100 lines and 500 movements. Transfer list pages load in under 3 seconds with supported filters.

**Constraints**: Reuse existing vertical slice patterns and Inventory Ledger/Balance mechanisms. Do not introduce persisted `TransferExecutionMode`. Do not introduce persisted `MovementType`. Keep `InventoryTransfer.TransitStorageLocationId` nullable. Add `InventoryTransactionType.Transfer = 2`. Each movement creates one transaction and two ledger entries. Store `InventoryTransactionId` on `InventoryTransferMovement`. Do not add transfer-specific source-reference fields to `InventoryTransaction`. Extend `StorageLocationType` with `InternalTransit` and `ExternalTransit`, but implement only internal transit behavior. Scanner workflow remains out of scope.

**Scale/Scope**: One WMS Inventory transfer write/read slice, one list/detail read slice, shared request/response contracts, transfer domain entities, EF mappings and migration shape, endpoint group, API client methods, transfer list/details UI, focused tests, and validation guidance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names internal transfer, transfer line, movement, nullable internal transit location, requested/picked/placed/in-transit quantities, transfer status, ledger transaction, ledger entries, balance changes, storage-location categories, and same-warehouse invariants before implementation mechanics.
- **Modular Monolith Boundaries**: PASS. Runtime changes stay in `Myrmex.Modules.Wms`, `Myrmex.Shared`, and `Myrmex.WebApp`. Public transport contracts remain in `Myrmex.Shared`; internal commands, queries, handlers, domain entities, EF mappings, and projections remain in the WMS module.
- **Vertical Slice Delivery**: PASS. The slice covers shared contracts, endpoints, internal commands/queries, domain logic, persistence mappings, API client, UI integration, and focused tests. Public transport contracts remain separate from internal commands/queries.
- **Testing Discipline**: PASS with documented UI automation exception below. The plan assigns regression risks to the lowest owning layer and avoids duplicating the full transfer matrix across endpoint, client, and UI tests.
- **Simplicity and Observability**: PASS. The plan reuses existing EF Core, dispatcher, Minimal API, API client, ProblemDetails, server-driven list, and MudBlazor patterns. It avoids generic repositories, new frameworks, service splits, transfer-specific transaction source fields, scanner state, and speculative abstractions.

## Project Structure

### Documentation (this feature)

```text
specs/075-internal-inventory-transfer-mvp/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- inventory-transfer-api-contract.md
|   `-- inventory-transfer-ui-contract.md
|-- checklists/
|   `-- requirements.md
`-- spec.md
```

### Source Code (repository root)

```text
Myrmex.Shared/Wms/Inventory/
|-- CreateInventoryTransferRequest.cs
|-- MoveInventoryTransferLineRequest.cs
|-- PickInventoryTransferLineRequest.cs
|-- PlaceInventoryTransferLineRequest.cs
|-- InventoryTransferDetails.cs
|-- InventoryTransferLineDetails.cs
|-- InventoryTransferMovementDetails.cs
|-- InventoryTransferListItem.cs
|-- InventoryTransferStatusDetails.cs
|-- InventoryTransferSortBy.cs
`-- ListInventoryTransfersRequest.cs

Myrmex.Modules.Wms/Inventory/
|-- Domain/InventoryTransfers/
|   |-- InventoryTransfer.cs
|   |-- InventoryTransferLine.cs
|   |-- InventoryTransferMovement.cs
|   `-- InventoryTransferStatus.cs
|-- Domain/InventoryTransactions/
|   |-- InventoryTransaction.cs              # add transfer factory
|   `-- InventoryTransactionType.cs          # add Transfer = 2
|-- Features/InventoryTransfers/
|   |-- CreateInventoryTransfer.cs
|   |-- MoveInventoryTransferLine.cs
|   |-- PickInventoryTransferLine.cs
|   |-- PlaceInventoryTransferLine.cs
|   |-- GetInventoryTransferById.cs
|   |-- ListInventoryTransfers.cs
|   `-- InventoryTransferQueryableExtensions.cs
`-- Endpoints/
    |-- InventoryTransferEndpoints.cs
    `-- InventoryEndpoints.cs                # map transfer endpoints

Myrmex.Modules.Wms/Infrastructure/Persistence/
|-- WmsDbContext.cs
|-- WmsDatabaseNames.cs
|-- Configurations/
|   |-- InventoryTransferConfiguration.cs
|   |-- InventoryTransferLineConfiguration.cs
|   |-- InventoryTransferMovementConfiguration.cs
|   `-- StorageLocationTypeConfiguration.cs
`-- Migrations/<timestamp>_AddInventoryTransfers.cs # implementation only

Myrmex.WebApp/
|-- Wms/Inventory/WmsInventoryApiClient.cs
|-- Components/Layout/NavMenu.razor
`-- Components/Pages/Wms/Inventory/InventoryTransferPages/
    |-- Index.razor
    |-- Index.razor.cs
    |-- InventoryTransferFilters.razor
    |-- InventoryTransferGrid.razor
    |-- InventoryTransferGridRequest.cs
    |-- InventoryTransferDetailsDialog.razor
    |-- CreateInventoryTransferDialog.razor
    `-- InventoryTransferMovementDialog.razor

Myrmex.Tests/Wms/Inventory/
|-- Client/WmsInventoryApiClientTests.cs
|-- Domain/InventoryTransferTests.cs
|-- Endpoints/InventoryTransferEndpointTests.cs
|-- Features/InventoryTransfers/InventoryTransferHandlerTests.cs
|-- Persistence/InventoryTransferPersistenceTests.cs
`-- Testing/InventoryTransferTestData.cs
```

**Structure Decision**: Add Inventory Transfer inside the existing WMS Inventory capability. Storage-location type reference data remains in Topology because that is the owning domain, while transfer behavior and balance/ledger effects stay in Inventory. No new module, repository abstraction, or service split is planned.

## Architectural Design Notes

- **Domain concepts first**: `InventoryTransfer` is the document. `InventoryTransferLine` is a requested SKU movement between regular storage locations. `InventoryTransferMovement` is the immutable physical movement fact linked to exactly one created inventory transaction. Progress quantities are derived from lines and movements.
- **Shared contract boundary**: Add public request/list/detail contracts in `Myrmex.Shared.Wms.Inventory`. Shared contracts contain transport data only and do not expose domain entities, EF expressions, handlers, or UI state.
- **Internal request boundary**: Add explicit internal commands for create, direct move, pick, and place. Add explicit internal queries for get details and list transfers. Endpoints map shared contracts into internal requests through the existing dispatcher.
- **Backend-owned projection**: Transfer details and list items are projected by WMS query handlers. Clients receive requested/picked/placed/in-transit/remaining quantities from backend-owned projections and do not recalculate authoritative progress.
- **Server-driven list behavior**: Use the existing list pattern: shared request -> `[AsParameters]` endpoint -> internal query -> filters -> count before paging -> deterministic sorting -> skip/take -> bounded projection -> `ListResult<T>`. Supported filters: warehouse, status, created date range, transfer code, source location, destination location, SKU, and transit presence.
- **Client/grid behavior**: Add `WmsInventoryApiClient` methods for transfer list/details/create/move/pick/place. Transfer grid uses a UI-specific grid request mapped into shared list request. Filter changes reset to page one; refresh reloads current grid state.
- **Cancellation and errors**: Propagate cancellation through UI, API client, endpoints, dispatcher, and EF operations. Write/action operations return `ApiResult<T>` from the WebApp client. Wrong movement pattern, insufficient balance, over-pick, over-place, completed transfer, invalid references, and stale state return clear ProblemDetails through existing conventions.
- **Existing pattern precedence**: Follow `AdjustInventoryBalance`, `InventoryBalanceQueryableExtensions`, `ListInventoryLedgerEntries`, `InventoryLedgerEndpoints`, `WmsInventoryApiClient`, Inventory Balance/Inventory Ledger page patterns, EF configurations, and existing test helpers.

## Required Design Details

### Transfer Domain Model

- `InventoryTransfer` owns lines and movements, stores nullable `TransitStorageLocationId`, and derives the execution pattern from null/non-null transit location.
- `InventoryTransferLine` stores one SKU, source regular storage location, destination regular storage location, and positive requested quantity.
- `InventoryTransferMovement` stores transfer id, line id, from location id, to location id, positive quantity, `InventoryTransactionId`, and occurrence time. It does not store persisted movement type or scanner state.
- Status transitions are `Created` -> `InProgress` -> `Completed`. Completed is terminal for MVP movement execution.

### Ledger and Balance Integration

- Add `InventoryTransactionType.Transfer = 2`.
- Add a transfer creation path to `InventoryTransaction`, for example a domain factory that accepts two ledger-entry facts and creates one transaction with exactly two entries.
- Do not add `InventoryTransferId`, `InventoryTransferMovementId`, source-reference fields, or transfer-specific foreign keys to `InventoryTransaction`.
- Use `InventoryTransferMovement.InventoryTransactionId` as the transfer-to-ledger linkage.
- Use existing `InventoryLedgerEntry` rows: from-location negative delta and to-location positive delta.
- Use existing `InventoryBalance` rowversion/current snapshot mechanism for movement balance updates.
- Use one `WmsDbContext.SaveChangesAsync` by default so movement, transaction, two ledger entries, balance changes, and status update succeed or fail together from the user's perspective.

### Movement Command Behavior

- Create transfer validates same warehouse scope, optional internal transit location, active SKU, active locations, positive quantities, and line source/destination as regular storage locations.
- Direct move is allowed only when transit location is null. It derives from source and destination from the line, requires sufficient source balance, and increases picked and placed together.
- Pick is allowed only when transit location is present. It derives from line source to transfer transit, requires sufficient source balance, and increases picked and in-transit.
- Place is allowed only when transit location is present. It derives from transfer transit to line destination, requires sufficient in-transit quantity, and increases placed while decreasing in-transit.

### Storage Location Type Extension

- Add system storage-location types `INTERNAL_TRANSIT` and `EXTERNAL_TRANSIT`.
- Implement only `INTERNAL_TRANSIT` behavior for transfers through transit.
- Treat `EXTERNAL_TRANSIT` as future-compatible reference data with no MVP behavior.
- Line source/destination cannot be transit types.

### Public API Contract Shape

- `POST /api/wms/inventory/transfers`
- `GET /api/wms/inventory/transfers/{transferId:guid}`
- `GET /api/wms/inventory/transfers`
- `POST /api/wms/inventory/transfers/{transferId:guid}/lines/{lineId:guid}/move`
- `POST /api/wms/inventory/transfers/{transferId:guid}/lines/{lineId:guid}/pick`
- `POST /api/wms/inventory/transfers/{transferId:guid}/lines/{lineId:guid}/place`

Create/move/pick/place are explicit business operations, not generic CRUD updates.

### UI Behavior

- Add Inventory Transfers navigation.
- Transfer list shows code, warehouse, status, created date, transit location, and aggregate requested/picked/placed/in-transit quantities.
- Transfer details shows header, lines, progress, read-only movement history, and valid movement actions.
- Direct transfers expose `Move`; transit transfers expose `Pick` and `Place`; completed transfers expose no movement actions.
- Movement history is read-only and links each row to the inventory transaction reference.
- No scanner UI/device/session workflow is included.

### Risk-Based Test Strategy

| Regression risk | Lowest owning layer | Planned coverage |
|-----------------|---------------------|------------------|
| External warehouse scope is accepted | Domain/handler | Create-transfer test rejects different source/destination warehouses |
| Nullable transit becomes required or execution mode is persisted | Domain/persistence | Direct and transit creation tests; mapping review/test for nullable transit FK |
| Direct and transit movement patterns are mixed | Domain/handler | Move/pick/place tests reject wrong operation for transfer pattern |
| Movement type or scanner state leaks into persistence | Domain/persistence review | Data-model and persistence tests verify movement fact fields |
| Movement is saved without transaction or transaction without movement | Handler/persistence | Movement command tests verify movement, transaction, two entries, balances, and status |
| Balance goes negative | Handler/domain | Move/pick tests reject quantity greater than available source balance |
| Over-pick or over-place is accepted | Domain/handler | Progress rule tests reject quantities exceeding requested or in-transit quantities |
| Progress quantities calculate incorrectly | Domain/query | Domain tests for formulas and query/detail tests for projected quantities |
| Completion status changes too early or too late | Domain/handler | Direct and transit completion tests cover final movement and incomplete transit quantity |
| Ledger transaction type remains Adjustment | Handler/persistence | Movement tests assert `InventoryTransactionType.Transfer` and two ledger entries |
| Storage type seed misses InternalTransit or ExternalTransit | Persistence | Seed/reference-data persistence test or migration review covers both values |
| Public route/body/list contract drifts | Endpoint/API client | Focused endpoint and client tests for create, movement commands, list URL, details route, and representative errors |
| UI exposes invalid actions | Manual smoke | Quickstart covers direct, transit, completed transfer, read-only history, and no scanner controls |

Do not reproduce the full movement validation matrix at every layer. Protect each risk at the lowest layer that owns it.

## Project Artifact Plan

### Created Documentation

- `specs/075-internal-inventory-transfer-mvp/research.md`
- `specs/075-internal-inventory-transfer-mvp/data-model.md`
- `specs/075-internal-inventory-transfer-mvp/contracts/inventory-transfer-api-contract.md`
- `specs/075-internal-inventory-transfer-mvp/contracts/inventory-transfer-ui-contract.md`
- `specs/075-internal-inventory-transfer-mvp/quickstart.md`

### Expected Production Files to Create During Implementation

- Shared transfer request/response/list contracts under `Myrmex.Shared/Wms/Inventory/`
- Transfer domain entities under `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransfers/`
- Transfer command/query slices under `Myrmex.Modules.Wms/Inventory/Features/InventoryTransfers/`
- Transfer endpoint group under `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryTransferEndpoints.cs`
- EF configurations for transfer entities under `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/`
- Transfer UI page/dialog components under `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryTransferPages/`
- Focused tests under `Myrmex.Tests/Wms/Inventory/`

### Expected Production Files to Modify During Implementation

- `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs`
- `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransactionType.cs`
- `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StorageLocationTypeConfiguration.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`
- `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- `Myrmex.WebApp/Components/Layout/NavMenu.razor`
- `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`

### Files Not Planned for This Feature

- No transfer-specific source-reference fields on `InventoryTransaction`.
- No persisted `TransferExecutionMode`.
- No persisted `MovementType`.
- No scanner-session, scanner-device, scan-step, package barcode, LPN, batch, serial, expiry, reservation, discrepancy, cancellation, correction, receiving, putaway, approval, route optimization, external transfer, or system warehouse `TRANSIT` implementation.

## Phase 0: Research Output

See `research.md`.

## Phase 1: Design Outputs

See `data-model.md`, `contracts/inventory-transfer-api-contract.md`, `contracts/inventory-transfer-ui-contract.md`, and `quickstart.md`.

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define internal inventory transfer, lines, movements, nullable transit location, storage-location categories, progress formulas, status transitions, ledger/balance effects, and same-warehouse invariants.
- **Modular Monolith Boundaries**: PASS. Public contracts stay in `Myrmex.Shared`; domain/application/persistence/endpoints stay in `Myrmex.Modules.Wms`; UI/client work stays in `Myrmex.WebApp`.
- **Vertical Slice Delivery**: PASS. The design covers endpoints, contracts, commands/queries, domain entities, persistence mappings, API client, UI flows, error behavior, tests, and validation guide.
- **Testing Discipline**: PASS with UI automation exception below. Tests are risk-based and assigned to owning layers; duplicate endpoint/client/UI matrices are intentionally avoided.
- **Simplicity and Observability**: PASS. The design uses existing local patterns, one-save persistence by default, existing ProblemDetails/service-result conventions, no new generic abstractions, and no speculative scanner or external-transfer model.

## Complexity Tracking

No architecture complexity exceptions are requested.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| Blazor component automation for transfer list, create dialog, details dialog, and movement dialogs | The current test project has no component-test infrastructure; adding one for this feature would be disproportionate and cross-cutting. | Domain, handler/persistence, endpoint, and API-client tests cover business rules, persistence effects, routes, request bodies, and representative errors. | Quickstart requires manual checks for direct transfer, transit transfer, movement actions, completed read-only state, list filters, and read-only movement history. | No. Revisit only if the project adopts component automation broadly. |

## Unresolved Technical Decisions Before `/speckit-tasks`

- None. Planning selects nullable transit location as the execution-pattern source, no persisted movement type, no persisted execution mode, transfer linkage through `InventoryTransferMovement.InventoryTransactionId`, existing ledger/balance mechanisms, one-save atomicity by default, and no scanner workflow for this MVP.
