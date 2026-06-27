# 048 Add Inventory Balance MVP vertical slice

## Status

Draft stakeholder requirements.

## Context

Myrmex WMS currently has foundational WMS capabilities:

* Catalog:

  * `StockKeepingUnit`;
  * `UnitOfMeasure`;
  * `SkuBarcode`;
  * SKU `BaseUnitOfMeasureId`.

* Topology:

  * `Warehouse`;
  * `Zone`;
  * `StorageLocation`;
  * storage location reference data.

The project also separates activation lifecycle from `EntityBase` through `IActivatable`. Only entities with explicit activation lifecycle should implement `IActivatable`.

The next required capability is the first minimal Inventory state: the system must be able to store and query the current quantity of a SKU at a storage location.

This feature intentionally does not implement full inventory accounting, stock movements, receiving, putaway, picking, shipping, LPN, reservations, or transaction history.

## Business Goal

Enable Myrmex WMS to represent a basic on-hand inventory balance:

* which SKU is stored;
* in which warehouse;
* at which storage location;
* what quantity is currently available;
* quantity is expressed in the SKU base unit of measure.

This is the smallest useful Inventory capability needed before later warehouse workflows are introduced.

## Capability Placement

This feature introduces the Inventory capability.

Inventory is responsible for representing current stock state.

Inventory depends on:

* Catalog, for SKU and SKU base unit of measure;
* Topology, for warehouse and storage location structure.

`InventoryBalance` belongs to Inventory. It must not be added to Catalog or Topology.

Suggested module location should follow existing Myrmex module conventions and introduce an Inventory area under the WMS module.

## MVP Scope

Implement an Inventory Balance vertical slice.

The MVP must include:

* `InventoryBalance` domain entity;
* reference to an existing active `StockKeepingUnit`;
* reference to an existing active or otherwise valid `StorageLocation`;
* warehouse-level visibility derived through `StorageLocation` / Topology;
* decimal quantity stored in the SKU base unit of measure;
* create inventory balance;
* get inventory balance by id;
* list inventory balances with optional filters;
* update inventory balance quantity only;
* persistence mapping;
* validation;
* API contract/client contract as needed;
* tests for domain/application/API/persistence behavior consistent with existing project patterns.

## Explicitly Out of Scope

Do not implement in this feature:

* receiving;
* putaway;
* picking;
* shipping;
* LPN;
* batch/lot tracking;
* expiry date;
* serial numbers;
* reservations;
* inventory transactions;
* movement history;
* stock adjustment documents;
* UoM conversions;
* alternative UoM behavior;
* packaging;
* cycle counting;
* seed/demo data;
* external integrations;
* WebApp UI;
* inventory balance delete behavior;
* inventory balance deactivate/reactivate behavior.

## Domain Model

### InventoryBalance

`InventoryBalance` represents the current known quantity of one SKU at one storage location.

Suggested fields:

* `Id`;
* `StockKeepingUnitId`;
* `StorageLocationId`;
* `Quantity`;
* `CreatedAtUtc`;
* `UpdatedAtUtc`.

The exact base class, constructor style, factory methods, validation style, timestamp handling, and domain event style must follow existing Myrmex conventions.

### Activation Lifecycle

`InventoryBalance` must not implement `IActivatable`.

`InventoryBalance` must not expose `Deactivate()` or `Reactivate()` behavior.

An inventory balance is current stock state, not lifecycle-enabled reference data.

The natural state change for an inventory balance is quantity change:

* `Quantity = 10`;
* `Quantity = 5`;
* `Quantity = 0`;
* `Quantity = 12`.

It must not be modeled as:

* active;
* inactive;
* reactivated.

## Warehouse Context

Inventory balances must support warehouse-level visibility.

The balance itself should reference a storage location. If storage locations already belong to warehouses through the existing Topology model, `InventoryBalance` must not duplicate `WarehouseId` as persisted state.

Warehouse context may be returned in query responses by resolving it through the storage location relationship.

This avoids inconsistent state such as:

* `InventoryBalance.WarehouseId = A`;
* `StorageLocation.WarehouseId = B`.

The persisted source of truth for physical placement is `StorageLocationId`.

## Functional Requirements

### FR-001 Create Inventory Balance

The system must allow creating an inventory balance for an existing active SKU and an existing valid storage location.

The created balance must store:

* SKU id;
* storage location id;
* quantity.

Quantity must be interpreted as quantity in the SKU base unit of measure.

The system must reject creation when:

* SKU does not exist;
* SKU is inactive;
* SKU does not have `BaseUnitOfMeasureId`;
* storage location does not exist;
* storage location is inactive, if storage locations implement activation lifecycle;
* quantity is negative;
* a balance already exists for the same SKU and storage location.

### FR-002 Get Inventory Balance

The system must allow retrieving a single inventory balance by id.

The result must include at minimum:

* inventory balance id;
* SKU id;
* storage location id;
* quantity;
* SKU base unit of measure context.

The response should include enough display context to identify:

* warehouse;
* storage location;
* SKU;
* base UoM.

Exact DTO names and route names must follow existing Myrmex conventions.

### FR-003 List Inventory Balances

The system must allow listing inventory balances with optional filters.

The list operation must support the following MVP lookup scenarios:

* find balances by SKU across warehouses and storage locations;
* find balances by storage location;
* find balances by warehouse;
* find balances by SKU within a warehouse.

The response should include enough information to display:

* warehouse context;
* storage location;
* SKU;
* quantity;
* SKU base unit of measure context.

The implementation may use a single list/query endpoint with optional filters instead of separate endpoints for each lookup scenario.

Suggested optional filters:

* `warehouseId`;
* `storageLocationId`;
* `stockKeepingUnitId`.

Filtering, search, paging, and sorting beyond these MVP filters are not required unless already standard for similar Myrmex slices.

### FR-004 Update Inventory Balance Quantity

The system must allow updating the quantity of an existing inventory balance.

The MVP must not support changing SKU or storage location of an existing inventory balance.

If SKU or location is wrong, a separate balance should be created for the correct pair in a later workflow.

The system must reject update when:

* inventory balance does not exist;
* quantity is negative.

### FR-005 Quantity Rules

Quantity must be stored as a decimal value.

Quantity must be greater than or equal to zero.

Quantity is always stored in the SKU base unit of measure.

`InventoryBalance` must not store its own `UnitOfMeasureId`.

Zero quantity is allowed in this MVP to avoid introducing delete, movement history, adjustment documents, or inventory transaction behavior.

A zero quantity balance means that the SKU/location pair is known, but currently has no on-hand quantity.

Automatic cleanup of zero quantity balances is out of scope.

Whether regular list responses should hide or include zero quantity balances by default may be decided by the API contract, but the MVP must not introduce inventory movement semantics to handle zero quantity.

### FR-006 Reference Validation

The system must validate referenced entities consistently with existing Myrmex patterns.

Create must require:

* existing active SKU;
* SKU with base unit of measure;
* existing valid storage location.

If `StorageLocation` implements `IActivatable`, inactive storage locations must not accept new inventory balances.

Update quantity does not need to revalidate SKU and storage location unless required by existing project patterns.

### FR-007 Uniqueness

There must be at most one inventory balance for the same SKU at the same storage location.

The implementation should enforce uniqueness at persistence level where appropriate.

Suggested unique key:

* `StockKeepingUnitId`;
* `StorageLocationId`.

### FR-008 Persistence

Inventory balances must be persisted in the database.

The persistence model must include:

* primary key;
* SKU foreign key;
* storage location foreign key;
* quantity;
* created timestamp;
* updated timestamp.

Foreign key behavior should prevent accidental deletion of referenced SKU or storage location when inventory balances exist.

Suggested delete behavior:

* SKU FK: Restrict;
* StorageLocation FK: Restrict.

### FR-009 Error Handling

Validation and not-found behavior must follow existing Myrmex API/application patterns.

The feature must not introduce a new error handling style.

### FR-010 API Contract

The API contract must follow existing Myrmex vertical slice conventions.

Required operations:

* create inventory balance;
* get inventory balance by id;
* list inventory balances with optional filters;
* update inventory balance quantity.

Not required:

* delete inventory balance;
* deactivate inventory balance;
* reactivate inventory balance.

## Suggested Response Shape

Exact DTO names must follow existing project conventions.

List/get responses should include enough context for future UI display.

Suggested details:

* inventory balance id;
* warehouse id;
* warehouse code/name if already available through existing query patterns;
* storage location id;
* storage location code/name;
* stock keeping unit id;
* SKU code;
* SKU name;
* base unit of measure id;
* base unit of measure code/symbol;
* quantity;
* created timestamp;
* updated timestamp.

Persisted `InventoryBalance` must not duplicate warehouse id or base unit of measure id unless there is already a strong existing project convention requiring it.

## Non-Functional Requirements

* Follow existing Myrmex vertical slice conventions.
* Follow existing Catalog and Topology implementation patterns where applicable.
* Do not introduce new architectural abstractions unless required.
* Do not introduce inventory transactions or movement ledger abstractions.
* Do not introduce UoM conversion logic.
* Keep the feature narrow and independently testable.
* Do not mix WebApp UI into this issue.
* Build, tests, app startup, EF migration generation, database update, migration application, and infrastructure commands are developer-controlled and must not be executed by the agent.

## Test Expectations

Tests should cover at minimum:

* create valid inventory balance;
* reject missing SKU;
* reject inactive SKU;
* reject SKU without base UoM, if such state is possible;
* reject missing storage location;
* reject inactive storage location, if storage location activation lifecycle exists;
* reject negative quantity;
* allow zero quantity;
* enforce uniqueness for SKU + storage location;
* get by id;
* list balances without filters;
* list balances by SKU;
* list balances by storage location;
* list balances by warehouse;
* list balances by SKU within warehouse;
* update quantity;
* reject changing SKU/location through update, if update contract might otherwise allow it;
* persistence mapping for quantity and foreign keys;
* FK delete behavior where practical.

## Migration Guidance

If EF migration is required, the agent must stop and recommend developer-controlled commands only.

The developer will run migration generation, database update, migration application, build, and tests manually.

Suggested migration name:

`AddInventoryBalance`

The exact commands must be provided by the agent when the implementation reaches the migration point.

## UI Guidance

Do not include WebApp UI in this issue.

A later WebApp-only issue may add:

* Inventory navigation section;
* inventory balance browse screen;
* inventory balances grid;
* filter by warehouse;
* filter by SKU;
* filter by storage location;
* display SKU code/name;
* display warehouse/location;
* display quantity and base UoM.

Manual create/edit UI for balances should be considered separately because manual stock editing is close to inventory adjustment behavior.

## Acceptance Criteria

* Inventory capability is introduced.
* `InventoryBalance` is added as current stock state.
* `InventoryBalance` references SKU.
* `InventoryBalance` references storage location.
* Warehouse context is available through storage location/topology.
* Quantity is decimal and stored in SKU base UoM.
* `InventoryBalance` does not implement `IActivatable`.
* `InventoryBalance` does not expose deactivate/reactivate endpoints.
* Inventory balance can be created for an active SKU with base UoM and a valid storage location.
* Inventory balance can be retrieved by id.
* Inventory balances can be listed.
* Inventory balances can be filtered by SKU.
* Inventory balances can be filtered by storage location.
* Inventory balances can be filtered by warehouse.
* Inventory balances can be filtered by SKU within warehouse.
* Inventory balance quantity can be updated.
* Negative quantity is rejected.
* Zero quantity is allowed.
* Duplicate balance for the same SKU and storage location is rejected or prevented.
* Persistence mapping includes required foreign keys and quantity.
* Tests are added or updated according to existing project conventions.
* The feature does not implement receiving, LPN, inventory movements, reservations, UoM conversions, delete, deactivate/reactivate, or WebApp UI.
