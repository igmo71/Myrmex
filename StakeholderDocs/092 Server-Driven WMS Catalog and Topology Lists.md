# Server-Driven WMS Catalog and Topology Lists

## Summary

Convert the legacy WMS Catalog and Topology list pages from fixed-size client-loaded collections to server-driven paging, sorting, filtering, and shared transport contracts.

The current early list pages were acceptable while reference data was small. They commonly request the first bounded set of records, for example `Take = 100`, and then let the UI grid sort/page the already-loaded collection. This is no longer correct after integration loaded tens of thousands of SKUs.

A warehouse user must be able to browse, search, sort, and page through large Catalog and Topology reference data without records disappearing simply because they are beyond an arbitrary client preload limit.

This issue is a refactoring and scalability improvement. It should align legacy Catalog and Topology list slices with the already accepted server-driven list approach used by newer Inventory slices.

## Stakeholder Goal

A user working with WMS reference data must see correct paged results across the whole dataset, not just the first fixed-size subset loaded into the browser.

The system must provide consistent server-driven list behavior for the affected Catalog and Topology pages while preserving existing routes, business behavior, search semantics, and domain ownership.

## Current Problem

Several early WMS list pages still load a fixed number of records into the WebApp, commonly with a request shaped like:

```csharp
ListRequest request = new(
    Skip: 0,
    Take: 100,
    SearchText: _searchText,
    SortBy: "code",
    SortDescending: false,
    IncludeInactive: _includeInactive);
```

The grid then receives `Items` and performs local paging/sorting over that already-limited collection.

This causes incorrect behavior when the real dataset is large:

* valid records beyond the first fixed page are unavailable in the UI;
* sorting applies only to the loaded subset, not to the complete dataset;
* paging does not represent the real total count;
* filtering can silently miss matching records outside the preload limit;
* increasing `Take` only delays the problem and makes the UI slower;
* different WMS slices now use different list contract, sorting, and grid patterns.

The problem is already visible for SKUs, where integration data can contain around 35,000 positions.

## In Scope

Convert the following list pages and their backend/client boundaries to the accepted server-driven list pattern.

### Catalog

* Stock Keeping Units / SKUs
* Units of Measure / UoM

### Topology

* Warehouses
* Zones
* Storage Locations

## Target User Experience

For the affected list pages:

* the grid must request data from the server when the page, page size, sort column, sort direction, or filters change;
* the grid must display the server-provided total count;
* search must apply to the full dataset on the backend;
* sorting must apply to the full filtered dataset on the backend;
* paging must happen after filtering, count calculation, and deterministic sorting;
* create, edit, deactivate, and reactivate operations must refresh the server-driven grid without returning to a stale client collection;
* expected cancellation must not be shown as a user-facing error;
* actual API failures must remain visible using the existing WebApp error conventions.

## Target Architecture

Use the accepted server-driven list flow:

```text
MudDataGrid.ServerData
    → WebApp grid request
    → shared List...Request
    → API client
    → existing route
    → endpoint [AsParameters]
    → internal explicit Query
    → backend-owned filtering
    → filtered CountAsync
    → deterministic sorting
    → Skip / Take
    → backend-owned projection
    → shared ListResult<T>
    → GridData<T>
```

The public API request/response contracts that cross the backend/WebApp boundary must live in `Myrmex.Shared`.

Internal application queries, handlers, EF expressions, projections, MudBlazor state, and UI-specific grid state must not be moved into `Myrmex.Shared`.

## Shared Contracts

Move all public transport DTOs for the affected slices into `Myrmex.Shared`.

This includes list requests, response DTOs, create/update request DTOs, and sort-key constants where applicable.

The migration should remove WebApp-local duplicate public DTO declarations for the affected slices.

### DTO Naming

Keep the existing `...Details` naming for affected slices unless a slice genuinely needs a separate list projection.

Do not introduce `...ListItem` solely for consistency with Inventory slices when the existing details shape is already the public list/get/create/update response shape.

Expected public DTO style:

```text
StockKeepingUnitDetails
UnitOfMeasureDetails
WarehouseDetails
ZoneDetails
StorageLocationDetails
```

Separate `...ListItem` types may be introduced only when the list response shape intentionally differs from the details response shape.

## List Requests

Introduce feature-specific shared list request contracts for affected slices instead of using generic `ListRequest` directly across the WebApp/API boundary.

Expected request style:

```csharp
public sealed record ListStockKeepingUnitsRequest
{
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? SearchText { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
    public bool? IncludeInactive { get; init; }
}
```

Apply the same pattern to:

* `ListUnitsOfMeasureRequest`
* `ListWarehousesRequest`
* `ListZonesRequest`
* `ListStorageLocationsRequest`

Slice-specific filters belong in the relevant request.

For Storage Locations, the list request must support server-side filters for:

```text
WarehouseId
ZoneId
StorageLocationTypeId
StorageLocationStatusId
SearchText
IncludeInactive
Skip
Take
SortBy
SortDescending
```

`WarehouseId` may be nullable in the transport contract, but the WebApp must not load an unrestricted cross-warehouse Storage Location list from the main page when no warehouse is selected.

## Sort Keys

Introduce shared sort-key constants for the affected slices.

Use PascalCase sort-key values, following the existing `WarehouseSortBy` style:

```csharp
public static class WarehouseSortBy
{
    public const string Name = "Name";
    public const string CreatedAtUtc = "CreatedAtUtc";
    public const string UpdatedAtUtc = "UpdatedAtUtc";
}
```

Expected new/updated sort constants:

```text
StockKeepingUnitSortBy
UnitOfMeasureSortBy
WarehouseSortBy
ZoneSortBy
StorageLocationSortBy
```

Minimum user-facing sort support:

### SKUs

* Code
* Name
* CreatedAtUtc
* UpdatedAtUtc
* IsActive

### Units of Measure

* Code
* Name
* CreatedAtUtc
* UpdatedAtUtc
* IsActive

### Warehouses

* Name
* CreatedAtUtc
* UpdatedAtUtc
* IsActive, if currently user-visible and useful

### Zones

* Code
* Name
* CreatedAtUtc
* UpdatedAtUtc
* IsActive

### Storage Locations

* Code
* Name
* IsPickable
* CreatedAtUtc
* UpdatedAtUtc
* IsActive

Do not use raw string sort tags in Razor when a shared sort constant exists.

The backend may accept existing legacy sort-key casing only when needed for compatibility, but the new shared constants and WebApp usage must use PascalCase.

## Search Behavior

Preserve the existing search semantics.

Do not introduce full-text search, relevance ranking, fuzzy matching, external search infrastructure, or database migrations.

The migration should preserve the current meaning of `SearchText` for each slice and only change where the search is applied: it must apply server-side to the full dataset before count, sorting, and paging.

Expected search fields remain aligned with current behavior:

```text
SKU: Code, Name, Description
UoM: Code, Name, Symbol where currently applicable
Warehouse: Code, Name, Description
Zone: Code, Name, Description
StorageLocation: Code, Name, Description
```

## Backend Requirements

For each affected list handler:

* use `AsNoTracking` where appropriate;
* apply filters before `CountAsync`;
* calculate total count before paging;
* apply deterministic sorting before `Skip` and `Take`;
* apply paging after sorting;
* project to the public response DTO before materialization where possible;
* return `ListResult<T>` with items, total count, normalized skip, and normalized take;
* preserve existing domain behavior;
* preserve existing validation behavior;
* preserve existing create/update/deactivate/reactivate behavior;
* do not serialize domain entities as API responses.

The internal query object remains owned by the backend slice. The shared request contract must not become the internal application query.

## Endpoint Requirements

Preserve existing route paths.

Endpoints should accept the new shared list request contracts using `[AsParameters]` where applicable, then map them to internal explicit queries.

Example target shape:

```csharp
private static async Task<IResult> ListStockKeepingUnitsAsync(
    [AsParameters] ListStockKeepingUnitsRequest request,
    IQueryDispatcher queryDispatcher,
    CancellationToken cancellationToken = default)
{
    var query = new ListStockKeepingUnits.Query
    {
        Skip = request.Skip ?? 0,
        Take = request.Take ?? ListQuery.DefaultTake,
        SearchText = request.SearchText,
        SortBy = request.SortBy,
        SortDescending = request.SortDescending ?? false,
        IncludeInactive = request.IncludeInactive ?? false
    };

    ...
}
```

Do not change existing API route paths solely for this refactoring.

## WebApp Requirements

For affected list pages:

* replace fixed `Take = 100` list loading for the main grid with `MudDataGrid.ServerData`;
* introduce WebApp-only grid request types where useful;
* map `GridState<T>` to the WebApp grid request;
* map the WebApp grid request to the shared API request;
* use shared sort constants in grid column tags;
* set explicit default sort behavior;
* reset to the first page when search/filter values change;
* reload the current server data after refresh or mutations where appropriate;
* propagate cancellation tokens from `ServerData`;
* suppress expected cancellation as a user-facing error;
* preserve current localized UI labels and messages.

Do not put MudBlazor types into `Myrmex.Shared`.

## Warehouse Lookup / Selection

Warehouse selection on Zones and Storage Locations pages must be brought to the common server-driven lookup/autocomplete/select approach.

Do not continue relying on loading the first fixed `Take = 100` warehouses for those page selectors.

The lookup must:

* be owned by Topology;
* use shared transport contracts;
* return a bounded result set;
* support search by current warehouse display/search semantics;
* use deterministic ordering;
* support cancellation;
* not require loading all warehouses into the browser.

The Warehouse list page itself must also be server-driven.

## Zones on Storage Locations Page

The Zone selector on the Storage Locations page is deferred.

This issue should not introduce server-driven Zone autocomplete for the Storage Locations page unless required by the final implementation plan to safely complete Storage Location list filtering.

If left as-is, the limitation must be documented as deferred follow-up work.

## Storage Location Type and Status Filters

Storage Location type and status filters must move from local filtering over the current loaded collection to server-side filtering.

The server-provided total count must reflect the selected type/status filters.

The WebApp must not filter only the current page locally and present the result as if it represented the full filtered dataset.

## Lookup vs List

Keep list pages and lookup/autocomplete behavior conceptually separate.

### List pages

Use:

```text
List...Request
ListResult<T>
TotalCount
MudDataGrid.ServerData
server-side paging
server-side sorting
server-side filtering
```

### Lookup/autocomplete/select

Use:

```text
Lookup...Request
IReadOnlyList<T>
bounded Take
no total count unless explicitly needed
server-side search
server-side deterministic ordering
cancellation
```

Do not introduce a universal list/lookup framework just because several slices now use similar shapes.

Feature-specific explicit contracts are preferred.

## Domain and Module Ownership

Catalog owns:

* SKU list contracts and list behavior;
* UoM list contracts and list behavior.

Topology owns:

* Warehouse list and lookup behavior;
* Zone list behavior;
* Storage Location list behavior;
* Storage Location type/status filtering behavior.

Inventory must not proxy Catalog or Topology list requests.

## Testing Approach

Use risk-based minimal testing.

Protect changed behavior at the lowest appropriate boundary.

Expected test coverage areas:

* handler tests for server-side filtering, count-before-paging, deterministic sorting, and paging where not already covered;
* endpoint or binding tests where migration to `[AsParameters]` introduces a distinct transport risk;
* API-client tests for query-string construction and cancellation when client method signatures change;
* focused WebApp/component tests only if an existing pattern already supports them and the risk is not covered elsewhere.

Do not duplicate the full sort/filter matrix through handler, endpoint, client, and UI tests.

Manual smoke validation is acceptable for UI interaction details such as grid paging, filter reset, and autocomplete behavior unless an automated pattern already exists in the repository.

## Explicit Non-Goals

This issue must not:

* change existing route paths;
* change database schema;
* create migrations;
* change 1C import behavior;
* remove or reinterpret warehouse codes;
* introduce full-text search;
* introduce external search infrastructure;
* introduce a generic list framework;
* introduce a generic lookup framework;
* refactor unrelated Inventory workflows;
* change Inventory Ledger, Inventory Transfers, or Inventory Counts except where shared DTO movement requires compile-safe namespace updates;
* change domain entities or domain validation rules;
* redesign the WebApp visually;
* add new business behavior unrelated to server-driven lists and shared contracts;
* convert the deferred Zone selector on the Storage Locations page unless explicitly approved during planning.

## Acceptance Criteria

* SKU list page no longer depends on loading the first fixed set of SKUs into the browser.
* SKU list page can correctly page, search, and sort across a dataset with tens of thousands of records.
* UoM list page uses the same server-driven list pattern.
* Warehouse list page uses the same server-driven list pattern.
* Zone list page uses the same server-driven list pattern.
* Storage Location list page uses the same server-driven list pattern.
* Storage Location type/status filters are applied server-side and reflected in total count.
* Warehouse selection for Zones and Storage Locations no longer depends on a fixed `Take = 100` preload.
* Public DTOs for affected slices are moved to `Myrmex.Shared`.
* WebApp-local duplicate public DTO declarations for affected slices are removed.
* Shared contracts contain no domain entities, EF Core expressions, MudBlazor types, UI state, handlers, or infrastructure dependencies.
* Existing route paths remain unchanged.
* Existing search semantics are preserved.
* Sort constants use PascalCase values.
* Razor grid sort tags use shared sort constants instead of raw strings.
* Backend ordering remains deterministic.
* Filtering occurs before count.
* Count occurs before paging.
* Sorting occurs before paging.
* Projection remains backend-owned.
* `ListResult<T>` total count represents the full filtered dataset, not the current page.
* Expected cancellation is not shown as an error.
* Existing create, edit, deactivate, and reactivate behavior remains functional.
* No database migrations, schema changes, 1C import changes, or domain model changes are introduced.
