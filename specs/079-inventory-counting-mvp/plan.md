# Implementation Plan: Inventory Counting MVP

**Branch**: `079-inventory-counting-mvp` | **Date**: 2026-06-24 | **Spec**: `specs/079-inventory-counting-mvp/spec.md`

**Input**: Feature specification from `specs/079-inventory-counting-mvp/spec.md`, stakeholder document `StakeholderDocs/Wms/Inventory/079 Inventory Counting MVP.md`, project governance, and current Inventory Balance, Adjustment, Ledger, Inventory Transfer, topology lookup, WebApp, and test implementations.

## Summary

Add an auditable warehouse inventory-count document with persisted count lines and lifecycle state. Operators create a count, capture current SKU/location balance snapshots, enter physical quantities, apply zero or non-zero variances, supersede stale lines, and complete or cancel the count. Non-zero apply reuses the existing `InventoryBalance.ApplyCountedQuantityAdjustment` and `InventoryTransaction.CreateAdjustment` domain path in the same `WmsDbContext` save as the count-line state change. SQL Server rowversions protect count, line, and balance state. Shared contracts, Minimal API endpoints, a server-driven count list, detailed count projection, WebApp pages/dialogs, actor audit identity, and focused SQL Server-backed tests complete the vertical slices.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET 10 solution.

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core 10 SQL Server provider, Blazor WebApp, MudBlazor, existing Myrmex command/query dispatchers, `ServiceResult`/ProblemDetails helpers, WMS API clients, and xUnit v3.

**Storage**: Existing SQL Server WMS schema through `WmsDbContext`. Add `inventory_counts` and `inventory_count_lines` tables, rowversion columns, foreign keys, lifecycle/audit fields, a filtered current-line uniqueness index, and supporting list/detail indexes. Reuse existing `inventory_balances`, `inventory_transactions`, and `inventory_ledger_entries`.

**Testing**: Existing xUnit project with domain tests, SQL Server-backed handler/persistence tests, focused endpoint and API-client tests, and manual MudBlazor smoke validation.

**Target Platform**: Existing Myrmex modular-monolith API service and server-rendered Blazor WebApp.

**Project Type**: Brownfield WMS vertical slice spanning shared contracts, WMS domain/application/endpoints/persistence, ASP.NET actor extraction, API clients, and Inventory Counts UI.

**Performance Goals**: Count list and detail become usable within 2 seconds for at least 95% of normal-load requests. Each line command uses bounded identity queries and at most one relational save. Count list uses server-side filtering, count-before-paging, deterministic sorting, and paging.

**Constraints**: One warehouse per count; no inventory freeze, reservation changes, blind count, approval, scanner/mobile flow, lot/serial/LPN scope, external integration, or count task generation. No actor identity may be accepted from client input. Conflict recovery is limited to immutable Superseded history plus one fresh current line. Builds, tests, migration generation/application, startup, and database changes remain developer-controlled.

**Scale/Scope**: Two new entities, one migration, ten public operations, one server-driven list, one details projection, one WebApp list/details workflow, and focused tests. Normal count operations affect one count and one line; apply additionally affects at most one balance, one adjustment transaction, and one ledger entry.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan defines Inventory Count, Inventory Count Line, immutable system snapshot, Counted Quantity, Variance, Conflict, Superseded replacement, Applied adjustment, and count lifecycle invariants before transport or persistence details.
- **Modular Monolith Boundaries**: PASS. Public contracts stay in `Myrmex.Shared`; count entities, handlers, EF mapping, projections, and adjustment orchestration stay in `Myrmex.Modules.Wms`; claims extraction stays in `Myrmex.AspNetCore`; UI state stays in `Myrmex.WebApp`.
- **Vertical Slice Delivery**: PASS. List, details, create, add, remove, count entry, apply, supersede, complete, and cancel use explicit endpoints, shared transport contracts where needed, internal commands/queries, domain behavior, persistence, client methods, and UI integration.
- **Testing Discipline**: PASS with the documented UI automation exception. Domain tests own lifecycle invariants; SQL Server handler/persistence tests own snapshot, rowversion, filtered uniqueness, atomic adjustment, and projection risks; endpoint/client tests own contract boundaries; repeated MudBlazor behavior uses manual smoke validation.
- **Simplicity and Observability**: PASS. The design reuses existing balance adjustment and transaction factories, list/projection patterns, dispatching, ProblemDetails mapping, topology/catalog lookups, and UI components. It adds only the count aggregate, required persistence, and a small reusable authenticated-actor extraction boundary.

**Post-design re-check**: PASS. The Phase 1 artifacts preserve module boundaries, define actor handling and concurrency explicitly, and contain no unresolved technical clarification.

## Project Structure

### Documentation (this feature)

```text
specs/079-inventory-counting-mvp/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── inventory-counting-api-contract.md
│   └── inventory-counting-ui-contract.md
├── checklists/requirements.md
└── spec.md
```

### Source Code (repository root)

```text
Myrmex.AspNetCore/
└── Security/
    └── HttpContextActorExtensions.cs

Myrmex.Shared/Wms/Inventory/
├── InventoryCountStatusDetails.cs
├── InventoryCountLineStatusDetails.cs
├── InventoryCountSortBy.cs
├── InventoryCountListItem.cs
├── InventoryCountDetails.cs
├── InventoryCountLineDetails.cs
├── ListInventoryCountsRequest.cs
├── CreateInventoryCountRequest.cs
├── AddInventoryCountLineRequest.cs
├── RemoveInventoryCountLineRequest.cs
├── RecordInventoryCountLineRequest.cs
├── ApplyInventoryCountLineRequest.cs
├── SupersedeInventoryCountLineRequest.cs
└── ChangeInventoryCountStatusRequest.cs

Myrmex.Modules.Wms/Inventory/
├── Domain/InventoryCounts/
│   ├── InventoryCount.cs
│   ├── InventoryCountLine.cs
│   ├── InventoryCountStatus.cs
│   └── InventoryCountLineStatus.cs
├── Features/InventoryCounts/
│   ├── ListInventoryCounts.cs
│   ├── GetInventoryCountById.cs
│   ├── InventoryCountQueryableExtensions.cs
│   ├── CreateInventoryCount.cs
│   ├── AddInventoryCountLine.cs
│   ├── RemoveInventoryCountLine.cs
│   ├── RecordInventoryCountLine.cs
│   ├── ApplyInventoryCountLine.cs
│   ├── SupersedeInventoryCountLine.cs
│   ├── CompleteInventoryCount.cs
│   └── CancelInventoryCount.cs
└── Endpoints/
    ├── InventoryCountEndpoints.cs
    └── InventoryEndpoints.cs

Myrmex.Modules.Wms/Infrastructure/Persistence/
├── WmsDbContext.cs
├── WmsDatabaseNames.cs
├── Configurations/
│   ├── InventoryCountConfiguration.cs
│   └── InventoryCountLineConfiguration.cs
└── Migrations/
    └── [developer-generated inventory count migration]

Myrmex.WebApp/
├── Wms/Inventory/WmsInventoryApiClient.cs
├── Components/Layout/NavMenu.razor
└── Components/Pages/Wms/Inventory/InventoryCountPages/
    ├── Index.razor
    ├── Index.razor.cs
    ├── InventoryCountGrid.razor
    ├── InventoryCountGridRequest.cs
    ├── InventoryCountFilters.razor
    ├── InventoryCountDetails.razor
    ├── CreateInventoryCountDialog.razor
    ├── AddInventoryCountLineDialog.razor
    └── RecordInventoryCountLineDialog.razor

Myrmex.Tests/Wms/Inventory/
├── Domain/InventoryCountTests.cs
├── Persistence/InventoryCountPersistenceTests.cs
├── Features/InventoryCounts/
│   ├── InventoryCountLifecycleHandlerTests.cs
│   ├── InventoryCountLineHandlerTests.cs
│   ├── ApplyInventoryCountLineHandlerTests.cs
│   └── InventoryCountQueryHandlerTests.cs
├── Endpoints/InventoryCountEndpointTests.cs
├── Client/WmsInventoryApiClientTests.cs
└── Testing/InventoryCountTestData.cs
```

**Structure Decision**: Keep counting inside the existing WMS Inventory capability as its own aggregate and feature folder. Reuse shared public transport boundaries and existing Inventory API client. Do not place count entities in `Myrmex.Shared`, do not create a new module/service, and do not route apply through the existing HTTP adjustment operation because count-line and inventory effects require one local atomic save.

## Architectural Design Notes

- **Domain concepts first**: `InventoryCount` owns warehouse, lifecycle, actors, and lines. `InventoryCountLine` owns the immutable balance snapshot, entered quantity, variance, status, actor/timestamp audit, adjustment reference, and supersession link. Domain methods enforce Draft/InProgress/final states, Pending-only deletion, permanent counting evidence, and completion eligibility.
- **Shared contract boundary**: Public list/detail DTOs and write request bodies live in `Myrmex.Shared`. Actor identity, EF rowversion bytes, domain enums, entities, and internal commands do not cross this boundary. Versions are Base64 strings.
- **Internal request boundary**: Each operation has an explicit command/query. Actor ID is extracted server-side and passed into internal commands; it is never part of a public request contract.
- **Backend-owned projection**: `InventoryCountQueryableExtensions` projects warehouse, SKU, base UoM, location, actor IDs, versions, progress counts, all historical lines, and applied transaction references without serializing entities.
- **Server-driven list behavior**: Support warehouse, exact status, and created-date filters. Calculate `TotalCount` after filters and before paging. Support explicit `CreatedAtUtc`, `Status`, and `WarehouseCode` sort keys. Default to newest creation first with descending `Id` as stable tie-breaker.
- **Client/grid behavior**: Map MudDataGrid state into `ListInventoryCountsRequest`; reset to page one on filter changes; display current-line progress while details retain Superseded history. Reload details after every successful action and reload the list when status/progress changes.
- **Cancellation and errors**: Propagate cancellation end-to-end. Reads use existing required-load behavior. Writes return `ApiResult<InventoryCountDetails>`. Use 404 for missing count/line/reference, 400 for validation or eligibility, 409 for lifecycle/version/duplicate/stale-balance conflicts, and 401 when no stable authenticated actor claim is available.
- **Risk-based testing**: Domain tests cover state transitions and evidence immutability. SQL Server tests cover rowversion, current-line filtered uniqueness, supersession, balance-presence races, one-save adjustment/ledger/line atomicity, and projections. Endpoint tests cover route/body/actor extraction. Client tests cover URLs, bodies, versions, cancellation, and representative ProblemDetails. UI automation is deferred.
- **Existing pattern precedence**: Follow Inventory Transfer for aggregate/list/details structure, Inventory Balances for server-driven lists and Base64 versions, Inventory Adjustment for balance/ledger concurrency and error semantics, Topology/Catalog lookup clients for warehouse/SKU/location selection, and Inventory Transfer dialogs/details for lifecycle actions.

## Detailed Design

### Actor identity boundary

`HttpContextActorExtensions` resolves a stable actor identifier from the authenticated principal in this order: `sub`, `ClaimTypes.NameIdentifier`, then authenticated `Identity.Name`. It returns no value for an unauthenticated or blank identity. Every count write endpoint requires the actor and returns 401 before dispatch when absent. The actor is passed to the internal command and persisted as a bounded string. No actor ID appears in client request JSON.

Myrmex currently has no configured authentication provider. This plan adds only provider-neutral claim extraction, not an identity provider or count-specific authorization policy. Runtime validation of writes therefore requires the host/environment to supply an authenticated principal. Existing warehouse visibility rules remain authoritative; if no warehouse restriction exists, the current active warehouse lookup remains the selectable set.

### Aggregate lifecycle

1. Create validates an active visible warehouse and creates Draft with creator.
2. Add line is allowed in Draft/InProgress, validates active SKU and eligible active same-warehouse non-transit location, snapshots current quantity and rowversion/absence, and creates Pending without changing Draft.
3. Remove physically deletes only Pending preparation data.
4. Record count accepts non-negative quantity/comment, stores counter/time and variance, changes Pending or Counted to Counted, and moves Draft to InProgress.
5. Apply accepts only Counted. Zero variance marks Applied with actor/time and no balance/transaction write. Non-zero variance validates the captured balance state, changes/creates the balance, creates one Adjustment transaction/entry, links it, and marks Applied.
6. Stale apply changes only the line from Counted to Conflict and persists that audit outcome before returning 409; it creates no balance, transaction, or ledger effect.
7. Supersede accepts only Conflict, marks it Superseded, creates a new Pending current line with a fresh balance snapshot, and links the replacement.
8. Complete requires at least one current line, all current lines Applied, and no Conflict; it records completer/time and makes the aggregate read-only.
9. Cancel is allowed from Draft/InProgress, records canceller/time, preserves prior Applied inventory changes, and makes the aggregate read-only.

### Snapshot, apply, and atomicity

- Existing balance snapshot: `SystemQuantity = Quantity`, `ExpectedBalanceVersion = RowVersion`.
- Missing balance snapshot: `SystemQuantity = 0`, `ExpectedBalanceVersion = null`.
- Apply compares current existence and rowversion exactly with the snapshot.
- Zero variance never creates a missing zero balance.
- Positive variance from an expected-missing snapshot creates a balance and one adjustment transaction.
- Existing-balance variance calls `ApplyCountedQuantityAdjustment`; missing positive balance calls `InventoryBalance.Create`.
- `InventoryTransaction.CreateAdjustment` creates the required ledger entry. Its reason is generated from the count identity and optional count reason/line comment, trimmed to the existing 500-character limit.
- Successful non-zero apply persists count, line, balance, transaction, and ledger entry through one `SaveChangesAsync`.
- `DbUpdateConcurrencyException` and the existing balance SKU/location unique-index race map to 409. The failed tracked context is not reused.

### Persistence and uniqueness

- Both count and line have SQL Server rowversions used by write requests.
- `InventoryCountLine.IsCurrent` is persisted. Pending, Counted, Applied, and Conflict are current; Superseded is not.
- A filtered unique index on `(InventoryCountId, StockKeepingUnitId, StorageLocationId)` where `IsCurrent = 1` guarantees one current line while preserving Superseded history.
- A nullable unique `SupersedesInventoryCountLineId` on the replacement line guarantees a Conflict line is superseded at most once.
- A nullable unique `AppliedInventoryTransactionId` guarantees one adjustment transaction cannot be claimed by multiple count lines.
- Restrict deletes preserve audit/reference integrity. Pending removal is an explicit domain/application action before counting evidence exists.
- Count list progress uses current lines only. Count details return both current and Superseded lines.

### Public operations

```text
GET    /api/wms/inventory/counts
POST   /api/wms/inventory/counts
GET    /api/wms/inventory/counts/{inventoryCountId}
POST   /api/wms/inventory/counts/{inventoryCountId}/lines
DELETE /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}
POST   /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/count
POST   /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/apply
POST   /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/supersede
POST   /api/wms/inventory/counts/{inventoryCountId}/complete
POST   /api/wms/inventory/counts/{inventoryCountId}/cancel
```

All mutating existing-resource requests carry the relevant expected Base64 count or line version. Add-line carries expected count version. Supersede returns details containing the new line/version.

### Test Plan

- Domain: creation, add/remove, first count transition, recounted quantity, immutable snapshot, apply/supersede/final states, completion requirements, cancellation, and actor/timestamp capture.
- Persistence: tables, enum conversions, decimal precision, rowversions, actor/comment lengths, foreign keys, filtered current-line uniqueness, single supersession, and restricted relationships.
- SQL Server handlers: active/inactive/missing references, transit/cross-warehouse rejection, missing/existing snapshots, duplicate current line, Pending removal, Counted permanence, zero variance, positive/negative variance, missing-balance creation, exact transaction/entry, stale presence/version conflict, concurrent count/line writes, atomicity, supersession, completion, cancellation, and audit identities.
- Queries: filters, count-before-paging, deterministic sorting, current-line progress, Superseded detail visibility, and inactive reference display.
- Endpoint: representative list binding, actor-required write behavior, route/body/version dispatch, success serialization, 404/409/401 mapping.
- Client: list URL, details URL, each action route/body, DELETE version query, cancellation, successful DTO parsing, and representative conflict.
- UI: manual quickstart for navigation, list paging/filtering, create/add/remove/count/apply/conflict/supersede/complete/cancel, actor display, action availability, and refresh behavior.

## Complexity Tracking

No constitution violations or architecture complexity exceptions are required.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| Blazor component automation for Inventory Counts pages and dialogs | Current test infrastructure has no component-test framework; adding one is disproportionate for UI composed from established MudBlazor list/dialog patterns. | Domain, SQL Server handler/persistence, endpoint, and API-client tests protect lifecycle, data, concurrency, and transport behavior. | Navigation, action visibility, dialogs, conflict/supersede flow, final-state read-only behavior, actor display, and list/detail refresh. | No. Revisit when component automation is adopted project-wide. |

## Unresolved Technical Decisions Before `/speckit-tasks`

- None. The design selects provider-neutral claims-based actor extraction, two rowversioned count tables, filtered current-line uniqueness, one-save apply atomicity, existing adjustment/ledger domain primitives, server-driven list/detail projections, and manual UI smoke validation.
