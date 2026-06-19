# Stakeholder Document: Implement Inventory Ledger Server-Driven History

## Summary

Introduce the first read-only Inventory Ledger history capability for Myrmex.

The Inventory Adjustment Ledger already records immutable inventory transactions and ledger entries. This feature exposes that history through a server-driven API and Blazor UI so warehouse operators and administrators can inspect how inventory quantities changed over time.

The feature must support:

* server-side filtering;
* server-side sorting;
* server-side pagination;
* compact transaction list projection;
* transaction details;
* navigation from an Inventory Balance to its relevant ledger history.

This feature is read-only. It must not introduce new inventory mutation operations.

## Stakeholder Goal

A warehouse operator or administrator must be able to answer questions such as:

* Why did the quantity of this SKU change?
* When was the inventory adjusted?
* What was the quantity before and after the operation?
* What delta was recorded?
* Which storage location and warehouse were affected?
* What reason was entered?
* Which ledger transaction contains the change?
* What history exists for a specific Inventory Balance context?

The current Inventory Balance shows only the latest materialized quantity. The ledger history must provide the corresponding audit trail.

## Current Problem

Myrmex now persists:

* `InventoryTransaction`;
* `InventoryLedgerEntry`;
* immutable before, delta, and after quantities;
* adjustment reason;
* occurrence time.

However, users currently cannot inspect this data through the application.

Without a ledger read side:

* balance changes cannot be investigated from the UI;
* adjustment reasons are stored but inaccessible;
* audit and troubleshooting require direct database access;
* the practical usefulness of the Ledger capability remains incomplete.

## Scope

The feature includes:

* server-driven list endpoint for Inventory Ledger history;
* server-side filtering;
* server-side sorting;
* server-side pagination;
* deterministic secondary sorting;
* transaction summary projection;
* transaction details endpoint or equivalent detail query;
* Blazor/MudBlazor history page;
* navigation from Inventory Balance to filtered ledger history;
* focused automated tests for handler, persistence projection, endpoint, and API-client risks;
* reuse of current shared paging, result, Problem Details, API-client, and grid patterns.

The feature does not change the Ledger write model.

## Primary User Stories

### User Story 1: Browse Inventory Ledger History

A user opens the Inventory Ledger history page and sees a server-driven list of inventory transactions.

Each row must show enough information to identify and understand the operation without opening details.

For the current Adjustment-only ledger, the list should show:

* transaction occurrence time;
* transaction type;
* SKU code and name;
* warehouse code and name;
* storage-location code and name;
* balance before;
* quantity delta;
* balance after;
* reason.

The list must support server-side paging and sorting.

### User Story 2: Filter Ledger History

A user can narrow the history by:

* SKU;
* warehouse;
* storage location;
* transaction type;
* occurrence date/time range.

Filters must be applied on the server before count and paging.

SKU and storage-location filters must use the existing server-driven autocomplete patterns.

Storage-location lookup must remain warehouse-scoped where a warehouse is selected.

Changing warehouse must clear an incompatible selected storage location.

### User Story 3: Inspect Transaction Details

A user can open a transaction and inspect its immutable details.

Transaction details must include:

* transaction ID;
* transaction type;
* reason;
* `OccurredAtUtc`;
* `CreatedAtUtc`;
* all ledger entries belonging to the transaction.

For each ledger entry, show:

* SKU code and name;
* base UoM code and symbol;
* warehouse code and name;
* storage-location code and name;
* balance before;
* quantity delta;
* balance after.

The current Adjustment transaction contains one entry, but the details contract and UI must support multiple entries because future transfer transactions may contain more than one.

### User Story 4: Open History from Inventory Balance

From an Inventory Balance row, a user can open ledger history already filtered by:

```text
StockKeepingUnitId + StorageLocationId
```

The history view must clearly show that filters are active.

The user must be able to clear or change those filters and continue browsing normal ledger history.

## Domain Semantics

### InventoryTransaction

`InventoryTransaction` represents one completed inventory operation.

The history list is transaction-oriented, not balance-oriented.

The transaction owns:

* transaction type;
* reason;
* occurrence time;
* ledger entries.

### InventoryLedgerEntry

`InventoryLedgerEntry` represents one immutable inventory quantity change.

The read side must preserve the invariant already stored by the write model:

```text
BalanceAfter = BalanceBefore + QuantityDelta
```

The UI must not recalculate or replace persisted before/after values from the current balance.

### Current MVP Transaction Type

The only currently supported transaction type is:

```text
Adjustment
```

The read model must not assume that all future transactions have exactly one entry.

The design must remain compatible with future transaction types such as transfer, receipt, pick, shipment, or correction without implementing them now.

## List Granularity Decision

The primary history list should use one row per ledger entry, enriched with its transaction context.

Recommended list item semantics:

```text
one InventoryLedgerEntry
+ parent InventoryTransaction fields
+ SKU context
+ storage-location and warehouse context
```

Rationale:

* filters by SKU and location naturally operate on entries;
* before, delta, and after belong to entries;
* future multi-entry transactions remain visible without collapsing materially different movements;
* transaction details can group all entries by `InventoryTransactionId`.

For current Adjustment transactions, one transaction produces one list row.

The UI may visually label the page “Inventory Ledger” or “Inventory History”; it must not imply that each row is always a complete transaction.

## Shared Contracts

Create feature-specific contracts in `Myrmex.Shared`.

Do not introduce a generic ledger or generic grid framework.

### List Request

Recommended contract direction:

```csharp
public sealed record ListInventoryLedgerEntriesRequest
{
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }

    public Guid? StockKeepingUnitId { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public string? TransactionType { get; init; }

    public DateTimeOffset? OccurredFromUtc { get; init; }
    public DateTimeOffset? OccurredToUtc { get; init; }
}
```

The exact inheritance or property style should follow the current server-driven list conventions.

### List Item

Recommended contract direction:

```csharp
public sealed record InventoryLedgerEntryDetails(
    Guid EntryId,
    Guid TransactionId,
    string TransactionType,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedAtUtc,
    decimal BalanceBefore,
    decimal QuantityDelta,
    decimal BalanceAfter,
    StockKeepingUnitInfo Sku,
    StorageLocationInfo StorageLocation);
```

Reuse existing nested info contract shapes only when doing so does not create inappropriate coupling.

Do not expose EF entities.

### Transaction Details

Recommended contract direction:

```csharp
public sealed record InventoryTransactionDetails(
    Guid Id,
    string TransactionType,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<InventoryLedgerEntryDetails> Entries);
```

The exact nested entry type may be specialized for transaction details if that produces a clearer contract.

## Filtering Requirements

### SKU Filter

Filter by exact `StockKeepingUnitId`.

UI selection should use the existing server-driven SKU autocomplete.

The lookup may include inactive SKUs because historical records must remain searchable after reference data is deactivated.

### Warehouse Filter

Filter entries through:

```text
InventoryLedgerEntry.StorageLocation.WarehouseId
```

Warehouse selection may use the existing bounded warehouse list pattern unless warehouse scale later requires autocomplete.

### Storage Location Filter

Filter by exact `StorageLocationId`.

UI lookup must use the existing server-driven storage-location autocomplete.

When a warehouse is selected:

* lookup results must be restricted to that warehouse;
* changing warehouse must clear incompatible storage-location selection.

Historical filtering should allow inactive storage locations to remain searchable.

If the current lookup contract cannot return inactive historical references, the feature plan must define a small history-appropriate lookup behavior rather than silently hiding valid history.

### Transaction Type Filter

The API must support filtering by transaction type.

For the current MVP, the UI may show only:

```text
Adjustment
```

Do not hardcode UI behavior in a way that prevents future types from being added.

Invalid or unsupported transaction type values should use current validation conventions.

### Occurrence Range

Filter by `InventoryTransaction.OccurredAtUtc`.

Recommended semantics:

```text
OccurredFromUtc
→ inclusive lower bound

OccurredToUtc
→ exclusive upper bound
```

An exclusive upper bound avoids ambiguity for date/time ranges and works well with day-based UI filtering.

The specification phase must clarify whether the UI sends exact UTC timestamps or local calendar dates converted to UTC boundaries.

The server must validate:

```text
OccurredFromUtc <= OccurredToUtc
```

## Sorting Requirements

Supported sort keys should include at least:

```text
occurredAtUtc
transactionType
skuCode
skuName
warehouseCode
warehouseName
storageLocationCode
balanceBefore
quantityDelta
balanceAfter
reason
```

Default sorting:

```text
OccurredAtUtc descending
then InventoryTransactionId descending
then InventoryLedgerEntry.Id descending
```

Every supported sort must include a stable deterministic tie-breaker.

Recommended pattern:

```text
requested sort
then InventoryTransactionId
then InventoryLedgerEntry.Id
```

The exact order may vary by primary key, but pagination must remain deterministic.

Unsupported sort keys should follow the established Inventory Balance server-driven list behavior.

## Paging Requirements

Use the existing Myrmex list normalization rules for:

* `Skip`;
* `Take`;
* default page size;
* maximum page size.

`TotalCount` must be calculated:

```text
after filters
before Skip/Take
```

No client-side paging is allowed.

## Projection Requirements

The handler must:

* use `AsNoTracking`;
* apply filters to `IQueryable`;
* calculate filtered count;
* apply sorting;
* apply `Skip` and `Take`;
* project only required columns;
* materialize after server-side paging;
* avoid `Include` when bounded projection is sufficient;
* avoid loading full transaction or navigation graphs for the list.

The projection should directly traverse required relationships:

```text
InventoryLedgerEntry
→ InventoryTransaction
→ StockKeepingUnit
→ BaseUnitOfMeasure
→ StorageLocation
→ Warehouse
```

The read side must not depend on current `InventoryBalance` rows.

Ledger history must remain available even if zero-balance snapshot rows are removed in a future feature.

## API Endpoints

Recommended routes:

```text
GET /api/wms/inventory/ledger
GET /api/wms/inventory/transactions/{transactionId:guid}
```

Alternative route naming may be selected during specification and planning if it better matches current repository conventions.

The list endpoint must use query-parameter binding consistent with current `[AsParameters]` list endpoints.

The details endpoint must return NotFound when the transaction does not exist.

No create, update, or delete Ledger endpoints are included.

## API Client

Add methods to the existing Inventory API client.

Recommended direction:

```csharp
Task<ListResult<InventoryLedgerEntryDetails>> ListInventoryLedgerEntriesAsync(
    ListInventoryLedgerEntriesRequest request,
    CancellationToken cancellationToken = default);

Task<InventoryTransactionDetails> GetInventoryTransactionByIdAsync(
    Guid transactionId,
    CancellationToken cancellationToken = default);
```

Client responsibilities:

* correct route construction;
* omit empty query string;
* encode supported query parameters;
* propagate cancellation;
* use existing required-read and Problem Details behavior;
* deserialize nested transaction and entry details.

Do not duplicate shared error-parser test matrices.

## UI Requirements

### Inventory Ledger Page

Add an Inventory section page for ledger history.

Recommended navigation:

```text
Inventory
- Balances
- Ledger
```

The page should use:

* MudBlazor;
* `MudDataGrid`;
* `ServerData`;
* existing server-driven grid wrapper/pattern where appropriate;
* separate filter component if consistent with Inventory Balance structure.

The UI must not preload full SKU or location catalogs.

### Filters

Provide:

* SKU autocomplete;
* warehouse selector;
* storage-location autocomplete;
* transaction-type selector;
* occurrence-from control;
* occurrence-to control;
* clear/reset action.

Applying or changing filters must reset paging to the first page.

Cancellation caused by rapid filter/search changes must not be shown as an error.

### Grid Columns

Recommended initial columns:

* occurred time;
* transaction type;
* SKU;
* warehouse;
* storage location;
* balance before;
* delta;
* balance after;
* reason;
* details action.

Quantity formatting must follow the existing Inventory Balance quantity conventions.

Delta should preserve its sign.

Do not use color alone to communicate positive or negative movement.

### Transaction Details Dialog or Page

For MVP, a MudBlazor dialog is sufficient.

The detail view must support multiple entries.

Show transaction header:

* ID;
* type;
* reason;
* occurred time;
* created time.

Show entries in a compact table or list.

No edit or delete controls are allowed.

### Navigation from Inventory Balance

Add a history action to each Inventory Balance row.

The action opens the ledger page or history dialog with:

```text
StockKeepingUnitId
StorageLocationId
```

already applied.

Preferred behavior:

```text
navigate to the dedicated Ledger page with query parameters
```

This makes the filtered history linkable and preserves browser navigation.

If current Blazor routing patterns make query-based navigation disproportionately complex, a filtered dialog may be accepted during planning, but the dedicated page remains preferred.

## Date and Time Behavior

Ledger timestamps are stored in UTC.

The API contracts use `DateTimeOffset`.

The UI should display time according to the application’s current time-display convention.

Do not silently reinterpret stored UTC timestamps as unspecified local time.

The specification phase must identify the current project convention for displaying UTC values in Blazor.

## Historical Reference Behavior

Ledger history is immutable, but related Catalog and Topology records may later change.

For this MVP, the read model may resolve current:

* SKU code and name;
* UoM code and symbol;
* storage-location code and name;
* warehouse code and name.

This means history reflects current reference labels while preserving immutable quantity and transaction values.

The feature does not introduce historical snapshots of reference-data names.

If a referenced record is inactive, history must still be returned.

If a referenced record is unexpectedly missing despite restrictive foreign keys, the query should fail visibly rather than fabricate incomplete history.

## Empty and Missing Data

When no entries match:

* return an empty item list;
* return `TotalCount = 0`;
* do not return NotFound.

When transaction details do not exist:

* return NotFound using current result conventions.

A long reason may be truncated visually in the grid, but the full persisted reason must be visible in transaction details.

## Performance and Index Considerations

The current schema already includes indexes for:

* transaction occurrence time;
* ledger-entry SKU;
* ledger-entry storage location;
* transaction foreign key.

The implementation plan must inspect actual generated indexes and query shapes before adding new indexes.

Potential combined indexes must not be introduced speculatively.

If query analysis demonstrates a need, likely candidates may include:

```text
InventoryTransaction.OccurredAtUtc + Id
InventoryLedgerEntry.StockKeepingUnitId + InventoryTransactionId
InventoryLedgerEntry.StorageLocationId + InventoryTransactionId
```

Do not add them without a concrete query and migration justification.

## Validation and Errors

List validation must cover:

* malformed GUID query parameters through normal endpoint binding;
* invalid paging values through existing normalization;
* unsupported transaction type;
* invalid date range.

Expected behaviors:

```text
invalid request
→ validation Problem Details

missing transaction details
→ NotFound Problem Details

unexpected database/query failure
→ existing failure behavior
```

The feature must not introduce a new error framework.

## Observability

Use existing logging and diagnostics conventions.

The list and details handlers should be diagnosable through:

* endpoint route;
* filter context where appropriate;
* transaction ID for detail failures;
* existing exception and Problem Details handling.

Do not log full free-text reasons as structured operational metadata unless current policy explicitly permits it.

Do not add a new observability framework.

## Testing Approach

Follow Myrmex risk-based minimal testing guidance.

### Handler and Projection

Protect:

* filters are applied correctly;
* `TotalCount` is calculated before paging;
* supported sorting is deterministic;
* default sorting is deterministic;
* nested projection returns transaction, SKU, UoM, location, and warehouse data;
* inactive reference data does not hide historical entries;
* date-range semantics are correct;
* details return all entries in deterministic order.

Use theories for filter and sort cases where appropriate.

Do not reproduce the full filter/sort matrix at endpoint and client layers.

### Persistence

Add focused provider-sensitive tests only for real schema or translation risks.

Do not add tests merely asserting that incidental columns or methods are absent.

### Endpoint

Protect:

* `[AsParameters]` binding for the list request;
* route and JSON shape for one representative history response;
* NotFound mapping for missing transaction details only if not already adequately protected.

### API Client

Protect:

* list URL/query construction;
* no trailing `?` for empty query;
* details route;
* cancellation propagation using the repository’s reliable test approach;
* nested success deserialization.

Do not duplicate generic Problem Details parser tests already owned elsewhere.

### UI

Use manual smoke testing.

Do not introduce a Blazor component-test framework for this feature.

## Manual Smoke Test Plan

Verify:

1. Ledger page loads with server-driven paging.
2. Default order shows newest transactions first.
3. Sorting works for supported columns.
4. SKU autocomplete filters history.
5. Warehouse filter works.
6. Storage-location filter is scoped to selected warehouse.
7. Changing warehouse clears an incompatible location.
8. Transaction-type filter works.
9. Occurrence date/time range works at both boundaries.
10. Empty result shows no rows and no error.
11. Transaction details show reason and all entries.
12. Adjustment transaction shows correct before, delta, and after values.
13. History action from Inventory Balance opens the correct filtered history.
14. Inactive SKU or location history remains visible.
15. Rapid filter changes do not surface cancellation errors.
16. API failure uses existing page error behavior.
17. Grid reason may be shortened visually, while details show the full reason.

## Acceptance Criteria

* Users can browse Inventory Ledger history through a server-driven list.
* Filtering is performed on the server.
* Sorting is performed on the server.
* Paging is performed on the server.
* Filtered total count is calculated before paging.
* Default and requested sorting are deterministic.
* Users can filter by SKU, warehouse, storage location, transaction type, and occurrence range.
* SKU and storage-location selection use server-driven lookup behavior.
* Historical records remain visible when related reference data is inactive.
* Each list row shows transaction context and before/delta/after quantities.
* Users can open transaction details.
* Transaction details support multiple ledger entries.
* Inventory Balance rows provide access to relevant filtered history.
* No Ledger mutation endpoint or UI is introduced.
* No current Inventory Adjustment behavior is changed.
* No speculative generic Ledger framework is introduced.
* Focused automated tests protect the material read-side risks.
* Build and full tests pass.
* Manual smoke testing confirms the primary workflows.

## Out of Scope

This feature must not implement:

* Inventory Transfer;
* receiving, picking, shipping, returns, or reservations;
* InventoryAccount;
* transit inventory;
* LPN or handling units;
* lot, batch, serial-number, or expiration history;
* cycle-count workflow;
* ledger corrections or reversal commands;
* ledger update or delete;
* zero-balance row deletion;
* historical snapshots of SKU, location, warehouse, or UoM names;
* export to CSV or Excel;
* dashboards or analytics;
* user/actor identity;
* event sourcing;
* ledger rebuild;
* generic reporting framework;
* generic lookup framework;
* new UI test infrastructure;
* refactoring timestamps out of `EntityBase`;
* changing domain-event dispatch semantics.

## Open Questions for Specification

The `/specify` phase should inspect the repository and clarify only genuine unresolved points:

1. Should the primary list be formally named `InventoryLedgerEntries` or `InventoryHistory` in public contracts and UI?
2. Should navigation from Inventory Balance use a dedicated routed page with query parameters or a filtered dialog?
3. What is the current application convention for displaying UTC timestamps?
4. Should the occurrence upper bound be exposed as exact exclusive UTC timestamp or as a user-facing inclusive calendar date converted to the next exclusive boundary?
5. Can existing SKU and storage-location lookup endpoints return inactive references for historical filtering, or is a small history-specific lookup behavior required?
6. Which supported sort keys should be exposed in the first UI version?
7. Should transaction details be a dialog or a dedicated page?

These questions must not reopen confirmed decisions about Ledger immutability, server-driven interaction, or the read-only scope.
