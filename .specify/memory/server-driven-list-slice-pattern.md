# Server-Driven List Slice Pattern

Durable conventions for backend-owned WebApp/API lists. See [the detailed architecture reference](../../docs/architecture/server-driven-list-slice-pattern.md) for examples and additional rationale.

## Applicability and Flow

Use this pattern for backend-owned lists that require filtering, sorting, paging, DTO projection, and `TotalCount`. It applies to WMS catalog, topology, inventory balance, ledger, transfer, count, and similar list slices.

The required backend flow is:

```text
Shared request contract
    -> endpoint binding
    -> internal explicit query
    -> filters
    -> CountAsync
    -> deterministic sorting
    -> Skip / Take
    -> backend-owned projection
    -> shared ListResult<T>
```

The required WebApp flow is:

```text
MudDataGrid GridState
    -> UI-specific grid request
    -> shared API request
    -> API client
    -> backend
    -> shared ListResult<T>
    -> GridData<T>
```

## Architectural Boundaries and Shared Contracts

- Domain entities are never serialized as API responses.
- EF Core projections remain inside the owning backend module.
- Public transport contracts are separate from internal commands and queries.
- Existing local repository patterns take precedence over new abstractions.
- `Myrmex.Shared` owns public request and response contracts that cross backend/client boundaries. Shared list contracts may include request records, result DTOs, sort-key constants, and transport-level enums.
- `Myrmex.Shared` must remain free of domain, EF Core, Blazor, MudBlazor, infrastructure, and handler dependencies. Shared DTOs must not contain EF expressions, domain dependencies, UI state, MudBlazor types, `DbContext`, handlers, or infrastructure types.

## Endpoint and Internal Query Mapping

Simple GET list endpoints should normally bind request contracts through `[AsParameters]`. Endpoint methods stay thin: bind the transport contract, map its values to an explicit internal command or query, dispatch it, and map the result to HTTP.

Public request contracts are transport types, not application queries. Internal command and query types remain in the owning backend slice and are handled through the internal dispatcher.

## Query Pipeline

Apply operations in this order:

1. Normalize filters and paging inputs as required.
2. Apply filters.
3. Calculate `TotalCount` after filtering and before paging.
4. Apply deterministic sorting.
5. Apply normalized `Skip` and `Take`.
6. Project to public DTOs in the backend before materialization.
7. Return `ListResult<T>` with page items, filtered total count, normalized skip, and normalized take.

Every supported sort key is an explicit public contract value. Sorting used with paging must be deterministic: each supported key includes a stable secondary tie-breaker, usually `ThenBy(x => x.Id)`. Unknown or missing sort keys fall back to deterministic default ordering.

## API Client and MudDataGrid

WebApp API clients construct query strings from shared request contracts. Nullable query parameters with no value should normally be omitted. Clients pass cancellation tokens to `HttpClient`.

Server-driven grids use `MudDataGrid.ServerData`; they do not load a complete backend result set for client-side filtering or paging. Use a UI-specific grid request between MudBlazor `GridState` and the shared API request, keeping MudBlazor types out of `Myrmex.Shared`. Grid column sort tags map to explicit public sort-key constants.

Filter changes reset the grid to the first page. If the grid is already on page 0, the reset may reload directly. Refresh and successful mutations reload server data without unnecessarily resetting current grid state.

## Cancellation and Errors

Cancellation tokens propagate from `MudDataGrid` through the API client, endpoint, dispatcher, and EF Core. Expected cancellation is not displayed as a user-facing error; timeouts and real API failures remain visible.

Preserve Myrmex API error conventions: read/list operations may use exception-based API-client flow, while write/action operations return `ApiResult<T>` and surface `ProblemDetails` through `ApiError`.

## Testing Expectations

Use risk-based minimal testing. Tests protect significant behavior and architectural boundaries rather than line coverage, at the lowest layer that owns each risk.

- Handler and persistence tests protect filtering, count-before-paging, paging, sorting, deterministic ordering, backend-owned projection, and domain/application behavior.
- Endpoint tests verify real Minimal API binding, routing, and serialization only when that boundary changes or lower-level tests cannot protect it.
- API-client tests protect URL construction, query parameters, write request bodies, cancellation propagation, success/error mapping, and `ProblemDetails` handling when relevant.
- Do not duplicate a sorting or filtering matrix across handler, endpoint, and API-client tests unless each test protects a distinct risk.
- Prefer fewer strong behavioral tests over weak tests that reproduce framework behavior.

## Implementation Checklist

- Define public request, response, and sort contracts in `Myrmex.Shared`.
- Keep the internal query and handler in the owning backend slice.
- Bind simple GET list contracts through `[AsParameters]` unless another shape is justified.
- Map the public request to an explicit internal query.
- Apply filters before count, sorting, paging, and projection.
- Count before paging.
- Apply deterministic sorting with a stable tie-breaker.
- Apply normalized `Skip` and `Take` after sorting.
- Project to shared DTOs before materialization.
- Return `ListResult<T>`.
- Construct API-client query strings from shared request contracts.
- Keep MudBlazor grid state out of `Myrmex.Shared`.
- Reset the page when filters change.
- Reload current grid state after refresh and successful mutations.
- Propagate cancellation tokens.
- Test behavior at the lowest layer that owns the risk.
