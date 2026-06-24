# Implementation Plan: Manual Inventory Move

**Branch**: `077-implement-manual-inventory-move` | **Date**: 2026-06-24 | **Spec**: `specs/076-manual-inventory-move/spec.md`

**Input**: Feature specification from `specs/076-manual-inventory-move/spec.md`, stakeholder document `StakeholderDocs/Wms/Inventory/077 Implement Manual Inventory Move.md`, project governance, and current Inventory Balance, Inventory Adjustment, Inventory Transfer, ledger, topology lookup, WebApp, and test implementations.

## Summary

Add an ad-hoc inventory movement vertical slice that moves one SKU quantity between two active regular storage locations in the same warehouse without creating `InventoryTransfer`. The write validates source rowversion and eligibility, updates or creates two balance snapshots, creates one existing `Transfer` transaction with exactly two balanced ledger entries, and saves the operation atomically. A scanner-ready read slice retrieves a balance by SKU/location without applying movement eligibility filters. The Inventory Balances grid gains a Move dialog using existing non-transit topology lookup behavior.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET 10 solution.

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core SQL Server provider, Blazor WebApp, MudBlazor, existing Myrmex command/query dispatchers, `ServiceResult`/ProblemDetails helpers, WMS API clients, and xUnit.

**Storage**: Existing SQL Server WMS schema through `WmsDbContext`. Reuse `InventoryBalance`, SQL Server rowversion, the SKU/location unique index, `InventoryTransaction`, and `InventoryLedgerEntry`. No schema change or migration is expected.

**Testing**: Existing xUnit project with SQL Server-backed handler/persistence tests, focused endpoint and API-client tests, domain tests, and manual Blazor smoke validation.

**Target Platform**: Existing Myrmex modular-monolith API service and Blazor WebApp.

**Project Type**: Brownfield WMS vertical slice spanning shared contracts, WMS application/endpoints, existing domain/persistence models, API clients, and Inventory Balances UI.

**Performance Goals**: Display a normal move result within 3 seconds; return balance lookup within 2 seconds for at least 95% of normal-load requests; use bounded point queries and one save.

**Constraints**: No transfer document linkage, new aggregate, scanner UI, inter-warehouse or transit workflow, inventory adjustment, automatic retry, broad abstraction, or migration unless a strict schema need emerges. Full-source moves retain zero balances. Destination concurrency returns conflict.

**Scale/Scope**: Two endpoints, two public move contracts, two internal handlers, one existing endpoint group/client/grid flow, and focused tests. Each move affects one SKU, at most two balances, one transaction, and two entries.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The design starts with Manual Inventory Move, source/destination Inventory Balances, active regular Storage Locations, source balance version, Transfer Inventory Transaction, and two balanced Inventory Ledger Entries. Invariants are enforced by the command/domain boundary.
- **Modular Monolith Boundaries**: PASS. Public transport contracts stay in `Myrmex.Shared`; internal requests, eligibility checks, EF queries, and domain entities stay in `Myrmex.Modules.Wms`; UI state stays in `Myrmex.WebApp`.
- **Vertical Slice Delivery**: PASS. Lookup and move have explicit endpoints, internal requests, handlers, projection/result behavior, client integration, and focused tests. The move also includes UI integration.
- **Testing Discipline**: PASS with the documented UI automation exception. Handler/persistence tests own balance, concurrency, eligibility, ledger, and atomicity risks; endpoint/client tests own transport risks; repeated Blazor behavior uses manual smoke validation.
- **Simplicity and Observability**: PASS. The design reuses rowversion, unique-index mapping, transaction/ledger factories, topology lookup, dispatching, Minimal API, ProblemDetails, and MudBlazor. It adds no framework, service split, repository, transaction abstraction, or schema.

**Post-design re-check**: PASS. Phase 1 artifacts preserve these boundaries and contain no unresolved clarification or constitution exception.

## Project Structure

### Documentation (this feature)

```text
specs/076-manual-inventory-move/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── manual-inventory-move-api-contract.md
│   └── manual-inventory-move-ui-contract.md
├── checklists/requirements.md
└── spec.md
```

### Source Code (repository root)

```text
Myrmex.Shared/Wms/Inventory/
├── MoveInventoryBalanceRequest.cs
└── MoveInventoryBalanceResult.cs

Myrmex.Modules.Wms/Inventory/
├── Features/InventoryBalances/
│   ├── GetInventoryBalanceBySkuAndStorageLocation.cs
│   └── MoveInventoryBalance.cs
└── Endpoints/InventoryBalanceEndpoints.cs

Myrmex.WebApp/
├── Wms/Inventory/WmsInventoryApiClient.cs
└── Components/Pages/Wms/Inventory/InventoryBalancePages/
    ├── Index.razor
    ├── Index.razor.cs
    ├── InventoryBalanceGrid.razor
    └── MoveInventoryBalanceDialog.razor

Myrmex.Tests/Wms/Inventory/
├── Client/WmsInventoryApiClientTests.cs
├── Endpoints/InventoryBalanceEndpointTests.cs
└── Features/InventoryBalances/
    ├── GetInventoryBalanceBySkuAndStorageLocationHandlerTests.cs
    └── MoveInventoryBalanceHandlerTests.cs
```

**Structure Decision**: Add behavior beside existing Inventory Balance reads because the operation starts from and returns balance snapshots. Reuse existing transaction/ledger domain models without a Manual Move entity or transfer-document dependency. Keep destination search in the Topology API.

## Architectural Design Notes

- **Domain concepts first**: A manual move is one completed relocation of a positive quantity for one active SKU between different active regular locations in the same warehouse. `InventoryBalance` is current state; `InventoryTransaction` and two entries are immutable history. No move document is persisted.
- **Shared contract boundary**: Add `MoveInventoryBalanceRequest` and `MoveInventoryBalanceResult`. Lookup reuses `InventoryBalanceDetails`; query parameters need no shared request type.
- **Internal request boundary**: Add internal `GetInventoryBalanceBySkuAndStorageLocation.Query` and `MoveInventoryBalance.Command`.
- **Backend-owned projection**: Reuse `ProjectDetailsData()` and convert rowversion after materialization. Lookup applies no active-state predicate. Post-move result reloads both balances through the same projection.
- **Server-driven list behavior**: Existing balance list filtering, count-before-paging, deterministic sorting, paging, and result shape remain unchanged.
- **Client/grid behavior**: Add Move beside History and Adjust. The dialog searches the source warehouse with `SelectableOnly = true` and `ExcludeTransitTypes = true`, excludes the source ID, submits the row version, shows a result summary, and reloads the grid after success/conflict.
- **Cancellation and errors**: Propagate cancellation end-to-end. Move returns `ApiResult<MoveInventoryBalanceResult>`; lookup uses existing read/load behavior. Validation is 400, absent lookup/reference is 404, and stale/insufficient/concurrent balance state is 409.
- **Risk-based testing**: Handler tests protect quantities, eligibility, concurrency, ledger, and atomicity. Existing domain tests already protect transfer transaction invariants. Endpoint/client tests protect route/body/result boundaries. UI uses manual smoke validation.
- **Existing pattern precedence**: Follow `AdjustInventoryBalance` for Base64 source version, `MoveInventoryTransferLine` for two-balance ledger orchestration, `GetInventoryBalanceById` for projection, `LookupStorageLocations` for destination search, and `AdjustInventoryBalanceDialog` for form/error behavior.

## Detailed Design

### Lookup Flow

1. Bind `skuId` and `storageLocationId` from `GET /api/wms/inventory/balances/lookup`.
2. Query the exact pair with `AsNoTracking()` and `ProjectDetailsData()`.
3. Return current details even when related SKU/location/type/status is inactive.
4. Return not found only when no balance row exists. Do not create a balance or apply move eligibility.

### Move Validation and Eligibility

Validate before mutation:

1. Required non-empty SKU/source/destination IDs and expected source version.
2. Positive quantity; trimmed required reason within `ReasonMaxLength`.
3. Valid Base64 source version decoding to the SQL Server rowversion length.
4. Different source/destination IDs.
5. Existing source balance, matching version, and sufficient quantity.
6. Existing active SKU. Base UoM activity is not a manual-move rule.
7. Existing active source/destination locations with active type/status, same warehouse, and types other than `INTERNAL_TRANSIT`/`EXTERNAL_TRANSIT`.

The handler is authoritative even when UI lookup has filtered destinations.

### Balance, Transaction, and Ledger Flow

1. Load source and destination balances as tracked.
2. Capture before quantities.
3. Reduce source; retain it at zero for a full move.
4. Increase existing destination or create a missing destination.
5. Capture one occurrence timestamp.
6. Use existing `InventoryTransaction.CreateTransfer` with the user reason and both balance pairs.
7. Add transaction and any new destination balance.
8. Save once through the existing WMS save/event pattern.
9. Reload both details and return moved quantity, before/after values, and occurrence time.

No Inventory Transfer, transfer movement, or adjustment record is created.

### Concurrency and Atomicity

- Explicit source comparison returns `InventoryBalance.ConcurrencyConflict` for absent/stale source state.
- EF rowversion protects existing source and destination rows; `DbUpdateConcurrencyException` returns 409 and is not retried.
- The existing SKU/location unique index rejects concurrent creation of the same destination and returns 409.
- One relational save is the persisted atomicity boundary for both balances, transaction, and entries.
- Failed tracked state is not reused and the server does not replay the operation.

### Test Plan

- SQL Server-backed handlers: existing/missing destination success, full-source zero, one Transfer transaction/two entries, stale/missing/insufficient source, invalid reason/version, same/cross-warehouse/transit/inactive references, destination rowversion conflict, and duplicate destination insertion without partial persistence where practical.
- Lookup handler: active/inactive references return details; missing pair returns not found.
- Domain: rely on current `InventoryTransaction.CreateTransfer` coverage unless its factory changes.
- Endpoint: query/body dispatch, success serialization, representative 404/409 mapping.
- Client: exact lookup URL, move route/body/result, cancellation, representative conflict.
- UI: quickstart smoke checks; do not duplicate the business matrix in component tests.

## Complexity Tracking

No constitution violations or architecture complexity exceptions are required.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| Blazor component automation for the manual move dialog and row action | Current test infrastructure has no component-test framework; adding one is disproportionate for this repeated dialog/grid pattern. | SQL Server handler tests protect business/persistence outcomes; endpoint and API-client tests protect transport. | Destination filtering, form behavior, success summary, conflict handling, and grid refresh. | No. Revisit if component automation is adopted project-wide. |

## Unresolved Technical Decisions Before `/speckit-tasks`

- None. The design selects existing rowversion/unique-index concurrency, one-save atomicity, existing transfer ledger creation, existing topology lookup, no migration, and manual UI smoke validation.
