# Stakeholder Document: Server-Driven SKU and Storage-Location Autocomplete

## Summary

Replace the fixed-size SKU and storage-location selections used by the Inventory Balance UI with server-driven autocomplete controls.

The current Inventory Balance UI preloads a limited number of reference-data records. This approach is acceptable only for very small datasets. In a real WMS, the number of SKUs and storage locations may be large, and a fixed `Take` limit can prevent users from finding valid records.

The feature must introduce focused server-side lookup behavior owned by the Catalog and Topology capabilities and consume those lookups from the Inventory Balance UI.

This is an incremental usability and scalability improvement. It must not introduce a generic lookup framework or refactor the existing Catalog and Topology list slices.

## Stakeholder Goal

A warehouse user must be able to find and select any relevant SKU or storage location without loading an entire large reference-data list into the browser.

Search must remain responsive and must not return unrelated storage locations from another warehouse.

## Current Problem

The Inventory Balance UI currently loads bounded reference-data lists, for example with a fixed `Take` value.

This causes several problems:

* valid SKUs beyond the loaded range are unavailable;
* valid storage locations beyond the loaded range are unavailable;
* increasing the fixed limit would transfer unnecessary data;
* initial page loading becomes increasingly expensive as reference data grows;
* the same approach would not scale to future Inventory Adjustment, Ledger, Transfer, LPN, batch, or lot workflows.

## Domain Ownership

Lookup behavior remains owned by the capability that owns the data:

* Catalog owns SKU lookup;
* Topology owns storage-location lookup;
* Inventory consumes both lookups;
* Inventory must not proxy Catalog or Topology lookup requests through Inventory endpoints.

This preserves modular-monolith boundaries.

## User Scenarios

### Scenario 1: Search for a SKU

A user enters part of a SKU code or name in an Inventory Balance filter or editor.

The UI waits for a short debounce interval and requests a bounded result set from the Catalog API.

The result displays enough information to distinguish similarly named SKUs.

### Scenario 2: Search for a storage location

A user first selects a warehouse.

The user then enters part of a storage-location code or name.

The UI requests storage locations belonging only to the selected warehouse.

The result must not include storage locations from another warehouse.

### Scenario 3: Change the selected warehouse

When the selected warehouse changes:

* any selected storage location is cleared;
* pending storage-location lookup requests are cancelled or ignored;
* subsequent searches use the new warehouse;
* Inventory Balance list filtering is refreshed according to the existing page-reset rules.

### Scenario 4: Slow or superseded request

When the user continues typing before a previous search completes, the previous request should be cancelled through the existing cancellation-token flow.

Expected cancellation must not be displayed as a user-facing error.

### Scenario 5: No matching records

The autocomplete displays an empty result without treating it as a failure.

## Functional Requirements

### SKU lookup

The system must provide a compact Catalog-owned SKU lookup operation.

It must:

* accept optional search text;
* search by SKU code and name;
* return only active SKUs by default;
* return a bounded number of results;
* use deterministic ordering;
* return only fields required by autocomplete display and selection;
* support cancellation.

Recommended initial maximum result count:

```text
20
```

The backend must enforce a safe maximum even if a larger value is requested.

### Storage-location lookup

The system must provide a compact Topology-owned storage-location lookup operation.

It must:

* require or accept a warehouse identifier according to the agreed UI behavior;
* search by storage-location code and name;
* return only locations belonging to the requested warehouse;
* return only active/selectable storage locations according to existing Topology semantics;
* return a bounded number of results;
* use deterministic ordering;
* return only fields required by autocomplete display and selection;
* support cancellation.

A storage-location search without a selected warehouse should not load an unrestricted cross-warehouse result set.

### Shared contracts

Public lookup request and response contracts that cross backend/client boundaries belong in `Myrmex.Shared`.

Shared contracts must not contain:

* domain entities;
* internal queries;
* EF Core expressions;
* MudBlazor types;
* UI state;
* infrastructure types.

Feature-specific contracts are preferred unless a genuinely identical shared abstraction is already justified.

Do not introduce a generic lookup framework merely because two lookups exist.

### Backend flow

Each lookup should follow the current vertical-slice conventions:

```text
Shared lookup request
    → Minimal API binding
    → internal explicit query
    → capability-owned filtering
    → deterministic ordering
    → bounded Take
    → backend-owned DTO projection
    → shared response contract
```

A full paged `ListResult<T>` is not required unless the lookup interaction needs total counts. Autocomplete normally requires only a bounded result collection.

### UI behavior

Use `MudAutocomplete` for:

* Inventory Balance SKU filter;
* SKU selection in Inventory Balance create/edit UI where applicable;
* Inventory Balance storage-location filter;
* storage-location selection in Inventory Balance create/edit UI where applicable.

Warehouse selection may remain a normal select because the number of warehouses is expected to remain relatively small.

Autocomplete behavior should include:

* server-side search;
* debounce;
* cancellation token propagation;
* bounded result set;
* clear display format;
* clear selected value handling;
* no preload of the first arbitrary 100 records.

Recommended initial UI behavior:

```text
Minimum characters: 1 or 2
Debounce: approximately 300 ms
Maximum results: 20
```

The exact values may follow existing MudBlazor conventions in the repository.

## Display Requirements

SKU results should display at least:

```text
Code — Name
```

The base unit-of-measure symbol may be included when it helps distinguish or understand the SKU.

Storage-location results should display at least:

```text
Code — Name
```

Warehouse code does not need to be repeated when the lookup is already constrained by the selected warehouse, unless it improves clarity.

## Error Handling

* Expected cancellation must not be displayed as an error.
* API failures must remain visible using existing Myrmex error conventions.
* An empty result is not an error.
* Invalid warehouse identifiers must follow current validation and Problem Details conventions.
* The UI must not retain a storage location that belongs to a previously selected warehouse.

## Performance and Query Requirements

Lookup queries must:

* use `AsNoTracking` where appropriate;
* project only required fields;
* avoid loading full entities or navigation graphs;
* apply search and warehouse filters before `Take`;
* apply deterministic ordering before `Take`;
* avoid unrestricted loading of all SKUs or locations.

Search should use database-translatable expressions.

Do not introduce full-text search infrastructure in this issue.

## Testing Approach

Follow the risk-based minimal testing guidance.

Protect only meaningful risks:

* SKU lookup search and result limiting;
* storage-location warehouse constraint;
* deterministic ordering;
* API-client query construction and cancellation when client-owned behavior changes;
* one focused boundary test if new binding or serialization behavior requires it;
* UI behavior through manual smoke validation unless a genuinely new component risk justifies automation.

Do not duplicate the full lookup matrix through handler, endpoint, client, and UI layers.

## Confirmed Design Decisions

The following decisions are authoritative for implementation.

### Separate filter and create eligibility

The same lookup endpoints serve both Inventory Balance filtering and Inventory Balance creation, but the eligibility rules differ.

Lookup request contracts must include:

```csharp
public bool SelectableOnly { get; init; } = true;
```

Semantics:

```text
SelectableOnly = true
```

Use for create workflows.

Return only items that satisfy the current production rules for creating a new Inventory Balance.

```text
SelectableOnly = false
```

Use for list filters.

Allow inactive or otherwise non-selectable records when they may still be referenced by existing Inventory Balance records.

Do not introduce separate filter and create lookup endpoints.

Do not introduce a generic `LookupPurpose` abstraction.

### SKU lookup eligibility

For `SelectableOnly = true`, SKU lookup must follow the current Inventory Balance create eligibility rules.

At minimum:

* the SKU must be active;
* its base unit of measure must satisfy the same active/valid rules enforced by the current create handler.

The lookup handler must not invent eligibility rules that differ from the current Inventory Balance creation behavior.

For `SelectableOnly = false`, inactive SKUs and SKUs with inactive base units of measure may be returned so existing balances remain filterable.

### Storage-location lookup eligibility

For `SelectableOnly = true`, storage-location lookup must follow the current Inventory Balance create eligibility rules.

The implementation must inspect the current create handler and reuse the same effective conditions, including only those already enforced for:

* storage-location activity;
* warehouse relationship;
* zone, type, or status eligibility where applicable.

Do not introduce new storage-location eligibility semantics in this issue.

For `SelectableOnly = false`, locations that may be referenced by existing Inventory Balance records may be returned even when they are no longer selectable for new balances.

### Lookup contracts

Use feature-specific transport contracts in `Myrmex.Shared`.

Do not introduce a generic lookup framework.

SKU request:

```csharp
public sealed record LookupStockKeepingUnitsRequest
{
    public string? SearchText { get; init; }

    public int? Take { get; init; }

    public bool SelectableOnly { get; init; } = true;
}
```

SKU item:

```csharp
public sealed record StockKeepingUnitLookupItem(
    Guid Id,
    string Code,
    string Name,
    Guid BaseUnitOfMeasureId,
    string BaseUnitOfMeasureCode,
    string? BaseUnitOfMeasureSymbol,
    bool IsActive,
    bool IsBaseUnitOfMeasureActive);
```

Storage-location request:

```csharp
public sealed record LookupStorageLocationsRequest
{
    public string? SearchText { get; init; }

    public int? Take { get; init; }

    public bool SelectableOnly { get; init; } = true;
}
```

Storage-location item must include the fields needed for selection, display, and compatibility checks:

```csharp
public sealed record StorageLocationLookupItem(
    Guid Id,
    Guid WarehouseId,
    string Code,
    string Name,
    bool IsActive);
```

Add further status/type activity fields only when the UI must display them or the contract must explain selectability. Do not expose domain entities or infrastructure types.

### Routes

Catalog owns SKU lookup:

```text
GET /api/wms/catalog/skus/lookup
```

Topology owns storage-location lookup:

```text
GET /api/wms/topology/warehouses/{warehouseId:guid}/locations/lookup
```

Inventory must consume these APIs directly through the existing Catalog and Topology API clients.

Do not add Inventory proxy endpoints.

### Lookup result shape

Return a bounded JSON collection of lookup items.

Do not use `ListResult<T>` because autocomplete does not need total count or paging metadata.

The client method may expose:

```csharp
Task<IReadOnlyList<T>>
```

### Search and limits

Backend search must support blank or null search text.

Blank search returns the first bounded deterministic result set.

Search must match code and name using database-translatable expressions.

Initial limits:

```text
DefaultTake = 20
MaxTake = 20
```

The server must enforce the maximum independently of the client.

Apply:

```text
filters
→ deterministic OrderBy
→ ThenBy
→ bounded Take
→ projection
```

Deterministic ordering must include `Id` as the final tie-breaker.

Do not add relevance ranking, full-text search, or database migrations in this issue.

### UI behavior

Use:

```csharp
MudAutocomplete<StockKeepingUnitLookupItem>
MudAutocomplete<StorageLocationLookupItem>
```

Initial UI values:

```text
MinCharacters = 1
DebounceInterval = 300
MaxItems = 20
Clearable = true
ShowProgressIndicator = true
```

Use the actual MudBlazor 9.5.0 API available in the repository.

Search delegates must use the cancellation-token signature supported by the installed version.

### Inventory Balance filters

Filters use:

```text
SelectableOnly = false
```

The selected lookup item is held by the UI, and the grid request uses its `Id`.

Inactive items may be visually marked as inactive.

Changing the warehouse must:

* clear the selected storage-location item;
* clear `StorageLocationId`;
* cancel or supersede an in-flight location search;
* reset the grid to the first page;
* reload server data.

SKU selection is not cleared when the warehouse changes.

### Create Inventory Balance dialog

Create lookups use:

```text
SelectableOnly = true
```

The create dialog must:

* use SKU autocomplete;
* keep warehouse as the existing select;
* use warehouse-scoped storage-location autocomplete;
* clear selected storage location when warehouse changes;
* use the selected SKU lookup item to display base UoM context;
* preserve the current create request contract and domain behavior.

### Update Inventory Balance behavior

The current update workflow changes quantity only.

Do not add SKU or storage-location autocomplete to the update dialog.

Do not expand update semantics.

### Cancellation and errors

Expected cancellation must not be displayed as a user-facing error.

Actual API failures must continue to use existing page/dialog error behavior.

After an awaited storage-location search, results must be ignored when the selected warehouse has changed since the request started.

### Testing

Use risk-based minimal testing.

Required protected risks:

* SKU search by code/name;
* selectable-only SKU eligibility;
* storage-location warehouse constraint;
* selectable-only location eligibility;
* bounded result count;
* deterministic ordering;
* client route/query construction and cancellation propagation.

Do not duplicate the complete lookup matrix through handler, endpoint, client, and UI tests.

A focused endpoint integration test is optional and should be added only when the new route/query binding introduces a distinct unprotected risk.

UI validation is manual smoke validation.

### Explicit non-goals

Do not include:

* Inventory Ledger;
* Inventory Transfer;
* Inventory Account;
* LPN or handling units;
* `StorageLocation.ParentId`;
* warehouse topology hierarchy;
* Catalog or Topology list refactoring;
* generic lookup framework;
* full-text search;
* database migrations;
* update workflow expansion.


## Out of Scope

This issue must not:

* implement Inventory Ledger;
* implement Inventory Adjustment;
* implement Inventory Transfer;
* introduce Inventory Account;
* introduce LPN or handling units;
* make `StorageLocation` hierarchical;
* add `ParentId` to `StorageLocation`;
* refactor Catalog list pages;
* refactor Topology list pages;
* convert every existing select in the application;
* introduce a generic lookup framework;
* introduce full-text search;
* add external search infrastructure;
* change current Inventory Balance domain semantics;
* change zero-balance behavior.

## Acceptance Criteria

* Inventory Balance no longer depends on fixed preloaded SKU and storage-location collections.
* A user can find SKUs by code or name through server-side autocomplete.
* A user can find storage locations by code or name within the selected warehouse.
* Changing warehouse clears an incompatible selected storage location.
* Lookup requests propagate cancellation tokens.
* Lookup results are bounded and deterministically ordered.
* Catalog owns SKU lookup.
* Topology owns storage-location lookup.
* Shared contracts remain transport-only.
* No generic lookup framework is introduced.
* No Inventory Ledger, Transfer, or StorageLocation hierarchy work is included.
* Existing Inventory Balance list, create, and update behavior remains functional.
* Build and complete test suite pass.
* Manual UI validation confirms filtering, selection, clearing, cancellation behavior, and empty results.
