# Research: Inventory Ledger Server-Driven History

## Decision: Reuse the Inventory Balance Server-Driven List Pattern

**Decision**: Implement Inventory Ledger history as a server-driven list vertical slice following the current Inventory Balance pattern: shared request/response contracts, `[AsParameters]` endpoint binding, internal query, filters, count-before-paging, deterministic sorting, paging, backend-owned projection, `ListResult<T>`, WebApp API-client query construction, and MudDataGrid `ServerData`.

**Rationale**: The feature is a backend-owned paged list consumed by the WebApp. The durable architecture guidance explicitly identifies this pattern for WMS lists, and Inventory Balance already proves the local structure for filters, sorting, cancellation, and grid reset/reload behavior.

**Alternatives considered**:

- Client-side filtering/paging over all history: rejected because it violates server-driven requirements and will not scale for audit history.
- New generic grid/list framework: rejected because the feature is specific and the constitution favors existing local patterns.
- POST-based search endpoint: rejected because current request shape is simple enough for GET query parameters and `[AsParameters]`.

## Decision: Entry-Oriented List and Transaction-Oriented Details

**Decision**: The primary list returns one row per `InventoryLedgerEntry` enriched with parent `InventoryTransaction` fields. Transaction details return one transaction with all related entries and must support multiple entries.

**Rationale**: Before, delta, and after quantities belong to ledger entries. SKU, warehouse, storage-location, and quantity filters naturally operate on entries. Details provide the transaction grouping needed for future multi-entry transactions without collapsing distinct movement rows in the primary list.

**Alternatives considered**:

- One list row per transaction: rejected because future multi-entry transactions would hide materially different SKU/location changes and make entry filters ambiguous.
- Separate single-entry details only for Adjustment: rejected because the stakeholder scope requires details to support multiple entries now.

## Decision: Use Existing Inactive-Inclusive Lookup Capabilities Where Adequate

**Decision**: Use `LookupStockKeepingUnits` with `SelectableOnly = false` for SKU history filtering; use `LookupStorageLocations` with `SelectableOnly = false` for warehouse-scoped storage-location history filtering; load warehouses with `IncludeInactive = true` for the Ledger warehouse filter.

**Rationale**: Current SKU and storage-location lookup handlers only restrict active/selectable records when `SelectableOnly = true`. The existing WebApp Inventory Balance filter already uses `SelectableOnly = false`, which includes inactive references. Warehouse list requests already support `IncludeInactive`, so Ledger can avoid hiding inactive historical warehouse references.

**Alternatives considered**:

- Reuse Inventory Balance warehouse load unchanged: rejected because it uses `IncludeInactive = false` and would hide inactive warehouses that still have history.
- Create a new generic historical lookup framework: rejected as out of scope.
- Create Ledger-specific lookup endpoints immediately: deferred unless implementation discovers the existing inactive-inclusive lookup shape cannot represent a required historical reference.

## Decision: Hydrate Routed Filter State by Exact ID Reads and Support Partial Routes

**Decision**: Routed Ledger filter state includes optional `stockKeepingUnitId`, `warehouseId`, and `storageLocationId`. Any valid subset hydrates exact display objects and applies only the corresponding filter IDs. `stockKeepingUnitId` alone hydrates and applies SKU. `warehouseId` alone hydrates and applies warehouse. `warehouseId` plus `storageLocationId` hydrates both, verifies the location belongs to the warehouse, and applies both. `storageLocationId` without `warehouseId` loads the exact storage location, derives its warehouse, hydrates both, and applies both filters. A `warehouseId` plus `storageLocationId` mismatch shows clear page-level validation/error feedback and must not issue a filtered ledger request with the inconsistent pair.

On page initialization, bind any present route values, load inactive-inclusive warehouses, resolve the selected or derived warehouse, resolve the exact SKU by ID through existing `WmsCatalogApiClient.GetStockKeepingUnitByIdAsync` when present, resolve the exact storage location by ID through existing `WmsTopologyApiClient.GetStorageLocationByIdAsync` when present, verify warehouse/location consistency, populate selected display objects, apply IDs to the ledger request, and then load the first grid page. Use existing `WmsTopologyApiClient.GetWarehouseByIdAsync` if the inactive-inclusive warehouse list does not contain the routed or derived warehouse. Current exact get-by-id handlers for SKU, warehouse, and storage location project by ID without `IsActive` filters, so they can restore inactive historical references.

**Rationale**: Copied or reloaded URLs must restore the same visible filter state for any valid subset of routed filters. Bounded empty-search autocomplete results are not reliable for exact-ID restoration because the selected SKU or storage location may not appear in the first page of lookup results, especially when inactive references are included. Deriving warehouse from a storage-location-only route preserves link usability without requiring callers to know redundant route state.

**Alternatives considered**:

- Hydrate by searching the first 20 empty-search autocomplete results: rejected because it is nondeterministic and can silently fail to restore valid routed filters.
- Add generic lookup-by-ID framework: rejected because the feature only needs exact hydration for known references and existing get-by-id clients already cover SKU, warehouse, and storage location.
- Add feature-specific exact reads immediately: not needed now; use only if implementation discovers a missing exact read.
- Require all three routed parameters before hydration: rejected because SKU-only, warehouse-only, and storage-location-only copied links are valid user workflows.
- Silently apply contradictory warehouse/location IDs: rejected because it can request misleading history and hide a bad link.

## Decision: Guard Initial Grid Load During Routed Hydration

**Decision**: When routed filter parameters are present, complete exact-ID hydration before rendering, activating, or allowing `MudDataGrid.ServerData` to issue the first request. Use an `_isInitializing`, `_isHydratingFilters`, or equivalent local guard that matches repository style. After hydration, issue exactly the intended first filtered page request. Do not perform an initial unfiltered request followed by a filtered request. Expected hydration cancellation must not appear as an error. When no routed filters are present, the page may load the initial unfiltered newest-first page normally.

**Rationale**: Server-driven grids can call `ServerData` as soon as they render. Without a hydration guard, copied routed links risk producing an observable unfiltered request before the intended filtered request, wasting work and briefly presenting the wrong history.

**Alternatives considered**:

- Let the grid load unfiltered and then reload after hydration: rejected because it violates copied-link determinism and may show/request misleading data.
- Introduce a new state-management framework: rejected because the behavior only needs a page-local initialization guard.

## Decision: Validate Ledger List Requests Before SQL Query Construction

**Decision**: The list handler sequence is validate request, normalize paging, create base `AsNoTracking` query, apply filters, `CountAsync`, deterministic sorting, `Skip`/`Take`, bounded projection, materialize, and return `ListResult`. Validation rejects unsupported transaction type values and `OccurredFromUtc > OccurredToUtc` before those values participate in SQL construction. `OccurredFromUtc == OccurredToUtc` remains valid and returns an empty interval.

**Rationale**: Validation before query construction produces clearer errors and avoids relying on SQL translation or filter branches to handle known invalid request states.

**Alternatives considered**:

- Validate after applying filters: rejected because invalid transaction type and occurrence range values should not participate in query construction.
- Reject equal occurrence boundaries: rejected because an exact empty interval is valid and already established by the specification.

## Decision: Calendar-Date UI Occurrence Range Mapped to UTC API Boundaries

**Decision**: Ledger UI uses culture-aware calendar date pickers for occurrence filtering. The selected From date maps to `OccurredFromUtc` at UTC start of that date. The selected To date is inclusive for the user and maps to `OccurredToUtc` at UTC start of the following day. Server filtering remains inclusive lower bound and exclusive upper bound: `OccurredAtUtc >= OccurredFromUtc` and `OccurredAtUtc < OccurredToUtc`. UI validation blocks From date later than To date; equal selected dates are valid and request the complete selected UTC calendar day. Raw API equal UTC boundaries remain valid and return an empty interval.

**Rationale**: Stakeholder UX now prefers calendar-date selection rather than timestamp text entry. Mapping UI dates to UTC day boundaries keeps the backend/API contract stable, preserves exclusive upper-bound query composition, and avoids workstation local-time conversion. Culture-aware pickers match normal regional date entry while the API client continues to send UTC `DateTimeOffset` values.

**Alternatives considered**:

- Free-text exact UTC timestamp fields: rejected because they are less usable for the Ledger filter workflow.
- Inclusive upper bound: rejected because it is ambiguous at sub-second precision and harder to compose for adjacent ranges.
- Local-time conversion: rejected because picker dates must map to UTC day boundaries without depending on the workstation time zone.
- Rejecting equal selected UI dates: rejected because users expect one selected calendar day to be included.

## Decision: Separate List and Transaction Detail Entry DTOs

**Decision**: Use `InventoryLedgerEntryDetails` only for list rows with parent transaction context. Use a separate `InventoryTransactionEntryDetails` shape inside `InventoryTransactionDetails` containing only entry-owned values and reference context: entry ID, before, delta, after, SKU/base UoM, storage location, and warehouse. The list row exposes `OccurredAtUtc` but no unqualified `CreatedAtUtc`; transaction `CreatedAtUtc` is exposed only on the transaction details header.

**Rationale**: Reusing the list row DTO inside transaction details would repeat transaction ID, type, reason, occurrence time, and creation time for every entry, and an unqualified list-row `CreatedAtUtc` is ambiguous between transaction creation and entry creation.

**Alternatives considered**:

- Reuse `InventoryLedgerEntryDetails` for transaction details entries: rejected because it duplicates transaction header data and weakens the contract boundary.
- Keep unqualified `CreatedAtUtc` on list rows: rejected because it is ambiguous.
- Rename list field to `TransactionCreatedAtUtc`: rejected because the list does not need creation time; `OccurredAtUtc` is the operational list timestamp.

## Decision: Preserve PascalCase Sort Contract Values

**Decision**: `InventoryLedgerSortBy` follows the existing `InventoryBalanceSortBy` convention: public constant names and public string values are PascalCase, such as `OccurredAtUtc`, `TransactionType`, and `SkuCode`. Do not introduce camelCase sort values for Ledger.

**Rationale**: Inventory Balance grid tags and API requests already use PascalCase public sort values. Keeping Ledger aligned avoids a one-off transport convention and reduces UI/client mapping risk.

**Alternatives considered**:

- Use camelCase JSON-style sort keys: rejected because it conflicts with the current Inventory Balance public sort-key convention.

## Decision: Bounded EF Projections Without Include-Heavy Graphs

**Decision**: List and details handlers project only required scalar and nested DTO fields from `InventoryLedgerEntry`, `InventoryTransaction`, SKU, base UoM, storage location, and warehouse relationships. Do not use `Include` for list or details projections unless implementation discovers a specific provider limitation.

**Rationale**: Inventory Balance list already uses backend-owned projections instead of serializing domain entities. Bounded projection reduces loaded data, preserves module boundaries, and keeps shared contracts independent from EF navigation graphs.

**Alternatives considered**:

- `Include` transaction, entries, SKU, UoM, storage location, and warehouse graphs, then map in memory: rejected because it loads more data than required and increases coupling to domain graph shape.
- Query current `InventoryBalance` rows for context: rejected because ledger history must remain available independently of current balance snapshots.

## Decision: Do Not Add Indexes During Planning

**Decision**: Do not add combined indexes, migrations, or EF mapping changes in this feature plan. Implementation should inspect actual query shapes, generated SQL, and existing indexes before proposing any later migration.

**Rationale**: The current schema already has occurrence-time, transaction FK, SKU, and storage-location indexes. The user explicitly requested no indexes until actual query shapes are inspected. Adding speculative indexes would create migration scope without measured need.

**Alternatives considered**:

- Add combined occurrence and entry filter indexes up front: rejected as speculative.
- Add a broad covering index for the list projection: rejected because data distribution and query shape are not yet validated.

## Decision: Minimal Risk-Based Tests

**Decision**: Protect core behavior at the lowest owning layer: handler/persistence tests for list/detail semantics; focused endpoint tests for binding and representative JSON; API-client tests for route/query construction, cancellation, and nested deserialization; manual UI smoke checks for Blazor behavior.

**Rationale**: Durable testing guidance says not to duplicate equivalent scenarios across domain, handler, endpoint, client, and UI layers. Server-driven list behavior is owned by the handler/query layer; endpoint and client tests should cover their boundary-specific risks only.

**Alternatives considered**:

- Full filter/sort matrix at endpoint and API client levels: rejected as duplicate coverage.
- New Blazor component-test framework: rejected as disproportionate for this read-only UI slice.
- Domain tests for read-only listing: rejected because no domain behavior changes are planned.
