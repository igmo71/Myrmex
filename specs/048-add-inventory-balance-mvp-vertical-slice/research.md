# Research: Inventory Balance MVP Vertical Slice

## Decision: Add a New WMS Inventory Area

**Decision**: Introduce `Myrmex.Modules.Wms/Inventory` for `InventoryBalance` domain, features, and endpoints.

**Rationale**: The feature introduces the first Inventory capability. Inventory owns current stock state, while Catalog owns SKU/UoM reference data and Topology owns warehouse/storage-location structure.

**Alternatives considered**:

- Add balances under Catalog: rejected because stock state is not catalog reference data.
- Add balances under Topology: rejected because physical locations are topology reference data, not inventory quantity ownership.
- Create a separate Inventory service: rejected because the constitution requires the modular monolith unless a plan documents a need to split, and this MVP has no distributed-system need.

## Decision: Model `InventoryBalance` as Current State

**Decision**: `InventoryBalance` represents current known quantity for one SKU at one storage location. It has no activation lifecycle and no movement or transaction history.

**Rationale**: The MVP is intentionally the smallest useful inventory state. Quantity change is the only supported state transition after creation.

**Alternatives considered**:

- Inventory transaction ledger: rejected because receiving, movements, adjustments, and history are explicitly out of scope.
- Lifecycle-enabled reference data: rejected because balances are current operational state, not reference data that can be deactivated/reactivated.

## Decision: Store SKU, Storage Location, Quantity, and Timestamps Only

**Decision**: Persist `StockKeepingUnitId`, `StorageLocationId`, non-negative decimal `Quantity`, `CreatedAtUtc`, and `UpdatedAtUtc` on the balance.

**Rationale**: SKU and storage location identities are the source references needed to derive all required display context. Persisting warehouse or UoM identities on the balance would create duplicate business state that could conflict with Catalog or Topology.

**Alternatives considered**:

- Persist `WarehouseId`: rejected because warehouse context is derived from `StorageLocation.WarehouseId`.
- Persist `UnitOfMeasureId`: rejected because quantity is always in the SKU base UoM.
- Snapshot SKU/location/UoM display text: rejected because the MVP requires current context and no audit/history behavior.

## Decision: Validate Reference Eligibility at Create Time

**Decision**: Creating a balance requires an existing active SKU with a base UoM and an eligible storage location. Eligible storage locations are active `StorageLocation` records whose storage location type and status are active; `IsPickable` and type code do not restrict eligibility.

**Rationale**: The spec clarifies location eligibility and the existing WMS model has activation on storage locations, storage location types, and storage location statuses. Checking these references in the handler provides clear validation errors before persistence.

**Alternatives considered**:

- Rely only on FK failures: rejected because users need clear Myrmex validation/not-found feedback.
- Require `IsPickable = true`: rejected because current stock may exist in staging, dock, floor, or other non-pickable locations.
- Restrict to specific location type codes: rejected because type-code eligibility would hard-code topology reference data into Inventory.

## Decision: Enforce One Balance per SKU/Location Pair

**Decision**: Enforce uniqueness for `(StockKeepingUnitId, StorageLocationId)` in application behavior and with a unique persistence constraint.

**Rationale**: The domain rule states at most one balance per SKU/location pair. Persistence-level enforcement protects against duplicates under concurrent requests.

**Alternatives considered**:

- Application-only duplicate check: rejected because concurrent creates could still produce duplicates.
- Allow multiple balances and aggregate them at read time: rejected because it would introduce transaction/history semantics outside the MVP.

## Decision: Allow and List Zero Quantity Balances

**Decision**: Quantity may be zero, and regular list results include zero quantity balances by default.

**Rationale**: Zero means a known SKU/location pair currently has no on-hand quantity. Hiding or cleaning up zero balances would introduce delete or movement semantics that are out of scope.

**Alternatives considered**:

- Delete balance at zero: rejected because delete behavior and history are out of scope.
- Hide zero balances by default: rejected because users need known SKU/location pairs without adding a separate status or cleanup model.

## Decision: Quantity-Only Update Contract

**Decision**: Update requests accept only the new non-negative quantity. SKU and storage location are not part of the update contract.

**Rationale**: SKU and location define balance identity. Changing either would be a different balance or a future movement workflow.

**Alternatives considered**:

- Accept SKU/location and reject changed values: rejected because it invites ambiguous client behavior.
- Accept SKU/location and ignore them: rejected because ignored mutable identity fields make validation and tests less clear.

## Decision: Details Projection Joins for Display Context

**Decision**: Get/list responses include balance identity, SKU identity/code/name, storage location identity/code/name, warehouse identity/code/name, base UoM identity/code/symbol where available, quantity, and timestamps by joining existing Catalog and Topology tables.

**Rationale**: The user stories require enough context for stock visibility. Projection joins satisfy the read model without duplicating warehouse or UoM state in `InventoryBalance`.

**Alternatives considered**:

- Return only IDs: rejected because the spec requires display context for warehouse, storage location, SKU, and base UoM.
- Store denormalized display fields on the balance: rejected because this MVP has no snapshot/audit requirement and would risk stale data.

## Decision: Reuse Existing Myrmex Error and Diagnostics Style

**Decision**: Add Inventory Balance errors only where needed and return validation, not-found, duplicate, and persistence failures through existing service-result, ProblemDetails, API client, and diagnostics conventions.

**Rationale**: The constitution requires meaningful errors and diagnostics without unnecessary new infrastructure. Existing WMS slices already provide the expected result style.

**Alternatives considered**:

- Add a new error handling abstraction: rejected as unnecessary framework expansion.
- Log every validation branch manually: rejected because existing service-result/diagnostics conventions should remain the operational surface.

## Decision: Developer-Controlled Migration Workflow

**Decision**: Plan for EF Core model/configuration changes and document exact migration/database commands, but do not run migration generation or database update automatically.

**Rationale**: Project workflow and stakeholder source both require build, test, startup, migration generation, migration application, and database update to remain developer-controlled.

**Alternatives considered**:

- Generate migration during planning: rejected by explicit workflow boundary.
- Defer migration command guidance until implementation: rejected because the plan should make the expected developer-controlled migration step visible before tasks are generated.

## Decision: Focused Test Scope

**Decision**: Add focused tests for Inventory Balance domain rules, handler validation, read projections and filters, quantity update behavior, persistence constraints, API client contracts, and regressions around referenced Catalog/Topology records.

**Rationale**: The new risk is a cross-reference current-state aggregate with uniqueness and filtered visibility. Tests should protect those rules without copying every Catalog or Topology test.

**Alternatives considered**:

- Rely only on manual API checks: rejected because changed domain rules, handlers, persistence mappings, and API client contracts require automated coverage under the constitution.
- Add full endpoint-host automation now: rejected as a broader test-infrastructure decision already deferred in similar WMS plans.

## Decision: Explicit Non-Goals

**Decision**: Exclude receiving, putaway, picking, shipping, LPN, reservations, transaction history, movement history, adjustment documents, batch/lot, expiry, serial numbers, UoM conversion, packaging, cycle counting, seed/demo data, external integrations, WebApp UI, delete behavior, and activation/deactivation behavior.

**Rationale**: The feature is the first minimal Inventory state and must stay independently testable before later workflows are introduced.

**Alternatives considered**:

- Prepare generic movement-ready abstractions now: rejected because they do not solve the current current-balance problem and would violate pragmatic simplicity.
