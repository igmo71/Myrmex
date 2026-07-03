# Research: Server-Driven WMS Catalog and Topology Lists

## Decision 1: Retain the existing list handlers

**Decision**: Keep all five internal list queries and handlers. Preserve their filter-count-sort-page-project flow and feature 090's ascending ID tie-breakers, changing only missing contract, sort, and filter behavior.

**Rationale**: Repository inspection confirms the handlers already normalize paging, use no-tracking reads, filter before count, count before paging, project before materialization, return `ListResult<T>`, and order deterministically.

**Alternatives considered**: A shared generic list framework was rejected because explicit feature slices already work and local guidance prohibits abstraction without a current need.

## Decision 2: Separate shared DTOs from backend projections

**Decision**: Move public details and mutation request shapes to `Myrmex.Shared`, while domain conversion and query projection expressions remain in internal helpers beside each backend feature.

**Rationale**: Current details records combine transport shape with domain/EF mapping and cannot move unchanged into the dependency-free shared assembly. Plain shared records plus internal projection helpers preserve JSON and backend ownership.

**Alternatives considered**: Shared projection expressions were rejected because they require domain dependencies. Retaining duplicate backend/WebApp DTOs was rejected because it preserves contract drift.

## Decision 3: Use feature-specific nullable list requests

**Decision**: Add `ListStockKeepingUnitsRequest`, `ListUnitsOfMeasureRequest`, `ListWarehousesRequest`, `ListZonesRequest`, and `ListStorageLocationsRequest` as property-based shared records with nullable transport values. Endpoints apply current defaults when mapping to internal queries.

**Rationale**: This matches accepted Inventory slices, supports parameter binding, removes generic `ListRequest` from the public boundary, and allows omitted query values to retain current behavior.

**Alternatives considered**: Continuing with `ListRequest` was rejected because it hides ownership and cannot express slice filters. Non-nullable properties were rejected because they would change omitted-query compatibility.

## Decision 4: Standardize new sort callers without breaking old ones

**Decision**: Publish PascalCase constants for every supported sort and use them in WebApp column tags. Keep handler comparison case-insensitive. Add CreatedAtUtc and UpdatedAtUtc to SKU/UoM and retain deterministic existing fallbacks.

**Rationale**: Existing handlers already normalize case. Shared constants prevent drift while legacy lowercase callers continue to work.

**Alternatives considered**: Rejecting legacy casing was an unnecessary breaking change. Raw Razor sort strings were rejected because they permit client/backend mismatch.

## Decision 5: Add a dedicated bounded Warehouse lookup

**Decision**: Add `GET /api/wms/topology/warehouses/lookup`, `LookupWarehousesRequest`, `WarehouseLookupItem`, and an internal handler. Default/max Take is 20; search covers Code, Name, and Description; selectable-only defaults active; order is Name, Code, then ID.

**Rationale**: Zone and Storage Location pages currently preload only the first 100 Warehouses. A separate lookup follows accepted SKU/Storage Location patterns, preserves Topology ownership, remains bounded, supports cancellation, and can find any match.

**Alternatives considered**: Reusing the paged list for autocomplete, loading all Warehouses, and creating a generic lookup framework were rejected because list/lookup semantics differ and explicit bounded behavior is simpler.

## Decision 6: Filter Storage Locations in the existing query

**Decision**: Add nullable StorageLocationTypeId and StorageLocationStatusId to public/internal list requests and apply equality filters before count. Preserve Warehouse/Zone existence and mismatch validation. Unknown type/status IDs return an empty filtered result.

**Rationale**: Type/status are ordinary filters. Additional existence queries would add cost and new errors without an established contract.

**Alternatives considered**: Visible-page filtering was rejected because totals and matches are wrong. Not-found errors for unknown filter IDs were rejected because those IDs are not nested route resources.

## Decision 7: Defer the Storage Location Zone lookup

**Decision**: Keep the current first-page Zone selector on the Storage Location page and document a follow-up. Continue passing selected or URL-supplied ZoneId into server-side filtering.

**Rationale**: The specification defers this selector unless it blocks safe completion. It does not: the list handler already supports ZoneId and validates Warehouse consistency.

**Alternatives considered**: Adding Zone autocomplete now was rejected as scope expansion. Removing the Zone filter would regress existing behavior.

## Decision 8: Reuse established server-grid behavior

**Decision**: Model the legacy grids on Inventory Balance and Warehouse: page-local grid request, `ServerData`, one sort, shared tags, standard page sizes, filter reset, mutation reload, cancellation-aware empty results, and existing error display.

**Rationale**: This is the accepted local pattern and fixes client-only paging/sorting without redesign.

**Alternatives considered**: Increasing fixed Take and introducing shared grid infrastructure were rejected because neither solves correctness as directly as explicit page adapters.

## Decision 9: Test each risk at its owning boundary

**Decision**: Use handler tests for changed sort/filter/lookup semantics, endpoint tests for feature-request and route-value binding, API-client tests for URL/shared DTO/cancellation behavior, and manual smoke checks for repeated grid interaction.

**Rationale**: This follows risk-based testing. The repository has endpoint and fake-HTTP infrastructure but no established component-test pattern.

**Alternatives considered**: Copying the full matrix through every layer was rejected as duplication. Omitting endpoint tests was rejected because `[AsParameters]` and route-bound request properties introduce a distinct binding risk.

