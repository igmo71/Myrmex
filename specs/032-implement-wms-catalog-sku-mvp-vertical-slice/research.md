# Research: WMS Catalog/SKU MVP Vertical Slice

## Decision: Add Catalog as a WMS capability beside Topology

**Rationale**: SKU reference data is not warehouse topology. Placing it under `Myrmex.Modules.Wms/Catalog` keeps the business language clear while preserving the existing WMS module boundary and vertical-slice style.

**Alternatives considered**:

- Put SKU under `Topology`: rejected because topology currently owns Warehouse, Zone, and StorageLocation, not item catalog identity.
- Create a new module project: rejected because the constitution favors the existing modular monolith boundaries and this MVP does not need a module split.
- Add a generic reference-data framework: rejected because the project favors explicit local patterns and the MVP has one aggregate.

## Decision: Use `StockKeepingUnit` as the domain aggregate and SKU as user-facing wording

**Rationale**: `StockKeepingUnit` is explicit domain language and avoids an all-acronym class name while still presenting "SKU" in UI labels, API summaries, and validation messages.

**Alternatives considered**:

- Name the aggregate `Sku`: rejected because the codebase uses descriptive domain names such as `Warehouse` and `StorageLocation`.
- Create separate `CatalogItem` and `Sku` entities: rejected because the MVP only needs one item reference identity.
- Model product variants or item masters now: rejected because that expands beyond the requested SKU MVP.

## Decision: Keep SKU fields to code, name, description, active state, and timestamps

**Rationale**: The spec requires only the minimum descriptive reference data needed for later workflows. Existing WMS Topology entities already use code, name, description, active state, and audit timestamps, so this keeps the slice consistent.

**Alternatives considered**:

- Add barcode, UoM, packaging, or inventory fields: rejected because the issue explicitly excludes those areas.
- Add category, brand, dimensions, or costing: rejected because they are not required for the MVP acceptance scenarios.
- Allow direct deletion: rejected because existing WMS reference data uses deactivate/reactivate lifecycle behavior.

## Decision: Implement the same command/query set as Warehouse for the SKU MVP

**Rationale**: Create, list, get by id, update details, deactivate, and reactivate match the requested user flows and the accepted WMS Topology vertical-slice pattern.

**Alternatives considered**:

- Implement create/list only: rejected because the spec includes maintenance and lifecycle user stories.
- Add bulk import/export: rejected because the spec says a polished bulk import/export experience is not required.
- Add GetByCode as a first-class query: rejected because search and get-by-id satisfy the MVP.

## Decision: Use EF Core mapping with a `stock_keeping_units` table and unique SKU code index

**Rationale**: Existing WMS persistence maps aggregates through `WmsDbContext`, named table constants, configuration classes, migrations, and unique business-code indexes. SKU code uniqueness is a core invariant and should be protected in both handler checks and persistence.

**Alternatives considered**:

- Store SKUs in an existing table: rejected because no existing entity represents catalog item identity.
- Rely only on handler-level duplicate checks: rejected because persistence should protect uniqueness under concurrent writes.
- Use a natural key as the primary key: rejected because existing WMS aggregates use generated identity plus business code.

## Decision: Expose Catalog endpoints under `/api/wms/catalog/skus`

**Rationale**: Catalog is a distinct WMS capability. A `/api/wms/catalog` route group keeps it discoverable without coupling it to `/api/wms/topology`.

**Alternatives considered**:

- Use `/api/wms/topology/skus`: rejected because SKU is not topology.
- Use `/api/wms/skus`: rejected because it leaves less room for future catalog-level reference data.
- Use nested warehouse routes: rejected because SKU code is globally unique for this MVP and has no warehouse relationship.

## Decision: Add a separate `WmsCatalogApiClient` with local Catalog client support types

**Rationale**: A separate client keeps Catalog UI dependencies clear and avoids expanding the Topology API client with unrelated capability methods. For this MVP, duplicate the small API result and exception support shape under `Myrmex.WebApp/Wms/Catalog` rather than moving existing Topology client types into shared infrastructure. The implementation should still preserve the same write/action `ApiResult<T>` and read/load exception behavior.

**Alternatives considered**:

- Add SKU methods to `WmsTopologyApiClient`: rejected because it mixes Catalog into Topology.
- Introduce a shared generic API client abstraction: rejected because that would be a broader refactor than the MVP requires.
- Move existing `ApiResult` and `ApiException` immediately to a shared folder: rejected for this MVP because it touches existing Topology code without being required for Catalog/SKU behavior.

## Decision: Build a minimal MudBlazor SKU list page using existing Topology UI composition

**Rationale**: The Warehouse page already provides the needed pattern: page header, create/refresh buttons, alert, filters, grid, edit dialog, and deactivate/reactivate actions. SKU needs the same shape without navigation to topology child pages.

**Alternatives considered**:

- Build a richer catalog management UI: rejected because the request is for an MVP vertical slice.
- Add navigation shell changes beyond a reachable page route: rejected unless required by the existing app structure.
- Add advanced client-side validation or bulk workflows: rejected as non-MVP.

## Decision: Add focused regression tests for domain, handlers, persistence, and API client

**Rationale**: The constitution requires tests for new domain rules, handlers, persistence mappings, API clients, and critical UI flows. Existing WMS Topology tests show the expected coverage for invalid input, duplicate code, lifecycle idempotency, domain event dispatch, and ProblemDetails-aware error handling.

**Alternatives considered**:

- Rely on manual testing only: rejected because this is a new domain and persistence slice.
- Add broad end-to-end browser automation now: rejected because the repository's current WMS coverage pattern does not require it for the MVP.
- Add exhaustive list/get query handler tests: rejected for initial MVP unless needed by task generation; list behavior can be validated through focused handler tests if tasks include it.

## Decision: Keep roadmap exclusions explicit in every downstream artifact

**Rationale**: Catalog/SKU is the first roadmap implementation after topology documentation, so scope drift is likely. The plan must repeatedly exclude Inventory, Barcode, UoM, Packaging, Receiving, LPN contents, Picking, Shipping, and Integration.

**Alternatives considered**:

- Mention exclusions only in the spec: rejected because downstream task generation needs explicit boundaries.
- Add placeholder models for future areas: rejected because placeholder runtime types create accidental architecture commitments.

## Clarification Status

All planning unknowns are resolved. No unresolved clarification markers remain.
