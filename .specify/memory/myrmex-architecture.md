# Myrmex Architecture

Durable solution and module structure guidance.

## Module Boundaries

Use the existing repository boundaries:

- `Myrmex.Core` for shared kernel code.
- `Myrmex.AppDispatching` for cross-cutting dispatching.
- `Myrmex.AspNetCore` for ASP.NET helpers.
- `Myrmex.Modules.Wms` for WMS capabilities.
- `Myrmex.Shared` for public transport contracts that cross the backend/client boundary.
- `Myrmex.ApiService`, `Myrmex.WebApp`, and host projects for their existing application roles.

Future module changes must preserve these boundaries unless a separate approved plan documents the reason to diverge.

## Shared Contract Boundary

`Myrmex.Shared` may contain public API request contracts, public API response
contracts, shared list request/result contracts, public sort-key constants,
transport-level enums, and other types that genuinely cross the backend/client
boundary.

`Myrmex.Shared` must not contain domain entities or aggregate roots, domain
events, internal commands or queries, handlers, EF Core projections or mappings,
`DbContext`, endpoint implementation, Blazor or MudBlazor types, UI state,
infrastructure-specific types, or unrelated generic helpers placed there only
because multiple projects use them.

`Myrmex.Shared` may depend only on the BCL and other Myrmex.Shared types unless
a future documented architectural decision approves otherwise.

Public contract types do not require the rest of the owning module to become
public. Internal commands, queries, handlers, domain entities, EF mappings, and
projections remain in the owning backend module.

## Server-Driven List Slices

For backend-owned lists with filtering, sorting, paging, and WebApp consumption,
use the server-driven list vertical-slice pattern documented in
`docs/architecture/server-driven-list-slice-pattern.md`.

The backend flow is:

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

The frontend flow is:

```text
MudDataGrid GridState
    -> UI-specific grid request
    -> shared API request
    -> API client
    -> backend
    -> shared ListResult<T>
    -> GridData<T>
```

Domain entities are never serialized as API responses. EF projections remain
inside the owning backend module. Shared DTOs must not contain EF expressions or
domain dependencies. `TotalCount` is calculated after filtering and before
paging. Supported sort keys are explicit contract values. Sorting used with
paging must be deterministic and include a stable secondary sort such as
`ThenBy(Id)`.

## Local Pattern Guidance

Use existing internal dispatching patterns for commands, queries, and domain events. Keep cross-module communication explicit through public module registration, API contracts, commands, queries, or events.

Prefer simple explicit code over broad generic abstractions. New abstractions must solve a current WMS problem and match existing local patterns.
