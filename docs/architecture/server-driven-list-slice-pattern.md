# Server-Driven List Slice Pattern

## 1. Purpose and applicability

This document describes the recommended Myrmex server-driven list vertical-slice pattern. It is derived from the current Inventory Balance implementation and applies when a backend-owned list is consumed by the WebApp or another API client with filtering, sorting, paging, and DTO projection.

Use this pattern for WMS lists where:

- the backend owns the query, filtering, ordering, paging, and projection;
- the client needs page-sized result sets and a `TotalCount`;
- public API contracts must be shared between backend and client;
- domain entities, EF Core expressions, and UI state must stay out of public transport contracts.

Required architectural rules are labeled **Required**. Local practices that match the current repository but may be adapted by a future approved plan are labeled **Recommended**. Inventory-specific details are labeled **Feature-specific**. Future inventory concepts are labeled **Future design note** and are not implemented by this issue.

## 2. Architectural responsibilities

**Required**: A server-driven list slice follows this backend flow:

```text
Shared request contract
    -> [AsParameters] endpoint binding
    -> internal explicit query
    -> filters
    -> CountAsync
    -> deterministic sorting
    -> Skip / Take
    -> backend-owned projection
    -> shared ListResult<T>
```

**Required**: The WebApp list flow follows this shape:

```text
MudDataGrid GridState
    -> UI-specific grid request
    -> shared API request
    -> API client
    -> backend
    -> shared ListResult<T>
    -> GridData<T>
```

**Required**:

- Domain entities are never serialized as API responses.
- EF Core projections remain inside the owning backend module.
- Shared DTOs must not contain EF expressions or domain dependencies.
- Public transport contracts are separate from internal commands and queries.
- Existing local repository patterns take precedence over new abstractions.

## 3. Shared transport contracts

`Myrmex.Shared` owns public contracts that cross the backend/client boundary. Allowed content includes:

- public API request contracts;
- public API response contracts;
- shared list request and result contracts;
- public sort-key constants;
- transport-level enums;
- other types that genuinely cross the backend/client boundary.

Disallowed content includes:

- domain entities and aggregate roots;
- domain events;
- internal commands and queries;
- handlers;
- EF Core projections and mappings;
- `DbContext`;
- endpoint implementation;
- Blazor or MudBlazor types;
- UI state;
- infrastructure-specific types;
- unrelated generic helpers placed there only because multiple projects use them.

**Required dependency rule**:

```text
Myrmex.Shared may depend only on the BCL and other Myrmex.Shared types unless a future documented architectural decision approves otherwise.
```

Public contract types do not require the rest of the WMS module to become public. For example, `ListInventoryBalancesRequest`, `InventoryBalanceDetails`, and `InventoryBalanceSortBy` are public shared contracts, while `ListInventoryBalances.Query`, handlers, EF mappings, and the `InventoryBalance` aggregate remain internal to `Myrmex.Modules.Wms`.

## 4. Minimal API endpoint binding with `[AsParameters]`

**Required**: Public list requests are bound directly by Minimal API from query parameters.

```csharp
private static async Task<IResult> ListInventoryBalancesAsync(
    [AsParameters] ListInventoryBalancesRequest request,
    IQueryDispatcher queryDispatcher,
    CancellationToken cancellationToken = default)
{
    // Map to internal query before dispatching.
}
```

**Recommended**: Keep endpoint methods thin. Endpoint code should bind transport contracts, normalize transport defaults where needed, construct an explicit internal command/query, dispatch, and convert the service result to an HTTP result.

## 5. Mapping public requests to internal explicit queries

**Required**: Shared request contracts are transport types. They are not application queries.

The Inventory Balance endpoint maps nullable public values to an internal query:

```csharp
var query = new ListInventoryBalances.Query
{
    Skip = request.Skip ?? 0,
    Take = request.Take ?? ListQuery.DefaultTake,
    SortBy = request.SortBy,
    SortDescending = request.SortDescending ?? false,
    StockKeepingUnitId = request.StockKeepingUnitId,
    StorageLocationId = request.StorageLocationId,
    WarehouseId = request.WarehouseId
};
```

**Required**: Internal command/query types stay in the owning backend slice and are handled through the internal dispatcher pattern.

## 6. Filtering

**Required**: Filters are applied before counting, sorting, paging, and projection.

```csharp
IQueryable<InventoryBalance> inventoryBalances = dbContext.InventoryBalances
    .AsNoTracking()
    .ApplyFilters(query);
```

Inventory Balance currently supports SKU, storage location, and warehouse filters:

```csharp
if (query.WarehouseId is Guid warehouseId)
{
    queryable = queryable.Where(x => x.StorageLocation.WarehouseId == warehouseId);
}
```

**Feature-specific**: Inventory Balance filters represent current quantity by SKU, storage location, and warehouse. They do not implement ledger history, transfers, inventory accounts, LPN, or handling units.

## 7. Count before paging

**Required**: `TotalCount` is calculated after filtering and before paging.

```csharp
int totalCount = await inventoryBalances.CountAsync(cancellationToken);
```

This count represents the number of matching rows for the current filters, not the number of rows returned in the current page.

## 8. Deterministic sorting with a stable tie-breaker

**Required**: Sorting used with paging must be deterministic.

**Required**: Every supported sort key must include a stable secondary sort such as `ThenBy(x => x.Id)`.

```csharp
return sortDescending
    ? queryable.OrderByDescending(x => x.StockKeepingUnit.Code).ThenBy(x => x.Id)
    : queryable.OrderBy(x => x.StockKeepingUnit.Code).ThenBy(x => x.Id);
```

**Required**: Supported sort keys are explicit contract values, for example `InventoryBalanceSortBy.SkuCode`.

**Required**: Current backend contracts support one active sort key unless a feature explicitly extends the public contract and backend implementation.

**Recommended**: For unknown or missing sort keys, choose a deterministic default. Inventory Balance defaults to `Id` ordering in the backend and uses SKU code as the WebApp grid default.

## 9. Paging with `Skip` and `Take`

**Required**: Normalize paging values in the internal query/handler path, then apply paging after filtering and sorting.

```csharp
int skip = ListQuery.NormalizeSkip(query.Skip);
int take = ListQuery.NormalizeTake(query.Take);

List<InventoryBalanceDetails> items = await inventoryBalances
    .ApplySorting(query.SortBy, query.SortDescending)
    .Skip(skip)
    .Take(take)
    .ProjectDetails()
    .ToListAsync(cancellationToken);
```

**Required**: Return the normalized `Skip` and `Take` values in `ListResult<T>`.

## 10. Backend-owned DTO projection

**Required**: The backend owns DTO projection.

```csharp
public static IQueryable<InventoryBalanceDetails> ProjectDetails(
    this IQueryable<InventoryBalance> queryable)
{
    return queryable.Select(balance => new InventoryBalanceDetails(
        balance.Id,
        balance.Quantity,
        balance.CreatedAtUtc,
        balance.UpdatedAtUtc,
        /* nested public DTO values */));
}
```

**Required**: Shared DTOs are public shapes only. They do not contain EF projection expressions, navigation loading logic, or domain behavior.

**Recommended**: Keep projection helpers near the owning feature, as Inventory Balance does in `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceQueryableExtensions.cs`.

## 11. Shared `ListResult<T>`

`Myrmex.Shared.Common.ListResult<T>` is the shared list response envelope:

```csharp
public sealed record ListResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Skip,
    int Take);
```

**Required**: Use `TotalCount` for the filtered count before paging. Use `Items` for the page content only.

## 12. API client query construction

**Required**: The WebApp API client constructs HTTP query parameters from the shared request contract and passes cancellation tokens to `HttpClient`.

```csharp
if (request.Take.HasValue)
{
    query.Add($"take={request.Take.Value}");
}

if (!string.IsNullOrWhiteSpace(request.SortBy))
{
    query.Add($"sortBy={HttpUtility.UrlEncode(request.SortBy)}");
}
```

**Recommended**: Omit nullable query parameters that have no value. This keeps URLs aligned with Minimal API default binding and allows endpoint mapping to apply current defaults.

**Required**: API-client tests should cover URL construction, query parameters, request body for write actions, cancellation propagation, success/error mapping, and Problem Details handling.

## 13. Blazor `MudDataGrid.ServerData`

**Required**: A server-driven MudBlazor grid uses `ServerData`, not client-side filtering/paging over a full backend result set.

```csharp
private Task<GridData<InventoryBalanceDetails>> LoadServerDataAsync(
    GridState<InventoryBalanceDetails> state,
    CancellationToken cancellationToken)
{
    var sortDefinition = state.SortDefinitions.FirstOrDefault();

    var request = new InventoryBalanceGridRequest(
        Skip: state.Page * state.PageSize,
        Take: state.PageSize,
        SortBy: ResolveSortBy(sortDefinition),
        SortDescending: sortDefinition?.Descending ?? false);

    return LoadData(request, cancellationToken);
}
```

**Recommended**: Use a UI-specific grid request type between MudBlazor and the shared API request. This keeps MudBlazor types out of `Myrmex.Shared` and the API client.

**Required**: Grid column sort tags must map to explicit public sort-key constants.

## 14. Grid page reset and reload semantics

**Required**: Filter changes reset the grid to the first page.

```csharp
private async Task OnWarehouseChanged(Guid? value)
{
    _selectedWarehouseId = value;
    _selectedStorageLocationId = null;
    await ResetAndReloadInventoryBalancesAsync();
}
```

**Required**: Refresh and successful mutations reload server data without unnecessarily resetting the current grid state.

```csharp
await ReloadInventoryBalancesAsync();
```

**Recommended**: If a filter reset is requested while the grid is already on page 0, reload server data directly; otherwise navigate to the first page and let the grid load.

## 15. Cancellation propagation

**Required**: Cancellation tokens propagate from MudBlazor through the API client to EF Core.

```text
MudDataGrid ServerData token
    -> page LoadData callback
    -> WmsInventoryApiClient.ListInventoryBalancesAsync
    -> HttpClient.GetAsync
    -> endpoint CancellationToken
    -> dispatcher
    -> EF Core CountAsync / ToListAsync
```

**Required**: Expected cancellation is not shown as a user-facing error.

Inventory Balance suppresses expected grid cancellation in page loading:

```csharp
catch (Exception exception)
    when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
{
    _errorMessage = exception.Message;
}
```

**Required**: Timeouts and actual API failures remain visible.

## 16. Error handling

**Required**: Preserve current Myrmex API error conventions.

- Read/list operations use exception-based API client flow that remains ProblemDetails-aware.
- Write/action operations return `ApiResult<T>` and surface ProblemDetails through `ApiError`.
- Expected cancellation is not treated as a user-facing failure.

**Recommended**: Do not duplicate every business error scenario at the API-client level when those scenarios only exercise the same generic ProblemDetails mapping. Test representative mapping behavior and place business-specific behavior tests at endpoint, handler, persistence, or domain level.

## 17. Testing expectations

**Required**: Tests protect current behavior, not obsolete representations.

**Required**: Successful response fixtures should be constructed from current shared DTO types and serialized with web JSON conventions when testing API clients:

```csharp
private static readonly JsonSerializerOptions JsonOptions =
    new(JsonSerializerDefaults.Web);
```

**Required**: Avoid manually maintaining duplicate successful JSON contract shapes when shared DTO serialization can produce the fixture.

**Required**: Endpoint integration tests should verify real Minimal API binding and real JSON serialization.

**Required**: Handler/persistence tests should verify:

- filtering;
- count before paging;
- paging;
- supported sorting;
- deterministic ordering;
- backend-owned projection;
- domain/application behavior.

**Required**: API-client tests should focus on URL construction, query parameters, request bodies for write actions, cancellation propagation, success/error mapping, and Problem Details.

**Recommended**: Prefer fewer strong behavioral tests over many weak tests that only reproduce framework behavior.

## 18. Implementation checklist

Use this checklist for a new server-driven list slice:

- Define public request, response DTO, and sort-key contracts in `Myrmex.Shared`.
- Keep internal command/query types in the owning backend slice.
- Bind the shared request in the Minimal API endpoint with `[AsParameters]`.
- Map public request values to an explicit internal query.
- Apply filters before count, sort, paging, and projection.
- Calculate `TotalCount` after filtering and before paging.
- Apply deterministic sorting with a stable secondary key.
- Apply `Skip` and `Take` after sorting.
- Project to backend-owned shared DTOs before materialization.
- Return `ListResult<T>` with items, total count, skip, and take.
- Construct API-client query strings from the shared request contract.
- Keep MudBlazor grid state in the WebApp and out of `Myrmex.Shared`.
- Reset page on filter changes.
- Reload current grid state after refresh and successful mutations unless the workflow requires a reset.
- Propagate cancellation tokens through UI, client, endpoint, dispatcher, and EF Core.
- Test behavior at the correct boundary.

## 19. Known limitations and future extensions

Current implemented limitations:

- Inventory Balance list contracts support one active sort key.
- The current list is server-driven but not full-text searchable.
- The current Inventory Balance WebApp page has manual UI validation rather than component automation.
- Inventory Balance represents current SKU quantity at a storage location. It is not ledger history.

Future design notes, not implemented:

- `InventoryBalance` is the current materialized quantity for a SKU in an inventory account.
- Inventory history should eventually be represented by immutable ledger transactions and ledger entries.
- An adjustment may accept an absolute counted quantity while the ledger stores the calculated quantity delta.
- Inventory movement should be modeled as one general `InventoryTransfer` process rather than separate intra-warehouse and inter-warehouse processes.
- Source and destination may belong to the same warehouse or different warehouses.
- A transfer may be long-running and consist of multiple short atomic inventory transactions.
- Inventory may temporarily reside in a movement, handling, transfer, or transit account.
- Future ledger design must not assume every ledger entry always belongs directly to a physical storage location.
- Inventory Ledger, Inventory Transfer, Inventory Account, LPN, and handling units are not implemented by this issue.
