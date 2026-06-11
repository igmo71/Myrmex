# Data Model: Inventory Balance MVP Vertical Slice

## InventoryBalance

**Purpose**: Current known stock quantity for one SKU at one storage location.

**User-facing name**: Inventory balance.

**Domain base pattern**: Use the existing WMS aggregate/entity patterns. `InventoryBalance` must not implement `IActivatable` and must not expose deactivate/reactivate behavior.

**Fields**:

- `Id`: Stable system identity.
- `StockKeepingUnitId`: Required identity of the SKU whose current quantity is represented.
- `StorageLocationId`: Required identity of the storage location where the stock is placed.
- `Quantity`: Required non-negative decimal quantity.
- `CreatedAtUtc`: Creation timestamp.
- `UpdatedAtUtc`: Null on create; set after successful quantity update.
- `DomainEvents`: In-memory domain event collection ignored by persistence, if the aggregate follows existing event conventions.

**Validation Rules**:

- `StockKeepingUnitId` is required for create.
- `StorageLocationId` is required for create.
- `Quantity` is required and must be greater than or equal to zero for create and update.
- Create requires an existing active SKU with a base unit of measure.
- Create requires an eligible storage location: existing active `StorageLocation` with active storage location type and active storage location status.
- `IsPickable` and storage location type code do not restrict storage location eligibility.
- There can be only one balance for a `(StockKeepingUnitId, StorageLocationId)` pair.
- SKU and storage location cannot be changed after creation.

**State Transitions**:

```text
Create valid balance -> Current balance with non-negative quantity
Current balance -> Update quantity to non-negative value -> Same SKU/location balance with new quantity
Current balance -> Update quantity to zero -> Known SKU/location pair with zero on-hand quantity
```

**Invalid State Transitions**:

- Current balance -> change SKU.
- Current balance -> change storage location.
- Current balance -> deactivate/reactivate.
- Current balance -> delete or cleanup zero quantity.
- Current balance -> append movement/transaction history.

**Domain Events**:

- If local aggregate conventions require events, create emits an inventory-balance-created event and successful quantity update emits an inventory-balance-quantity-updated event.
- Do not introduce movement, transaction, reservation, adjustment, or lifecycle events.

**Relationships**:

- Required many-to-one relationship to `StockKeepingUnit` by `StockKeepingUnitId`.
- Required many-to-one relationship to `StorageLocation` by `StorageLocationId`.
- Warehouse context is derived through `StorageLocation.WarehouseId`.
- Base UoM context is derived through `StockKeepingUnit.BaseUnitOfMeasureId`.
- No direct relationship to Warehouse or UnitOfMeasure is stored on the balance.

## StockKeepingUnit

**Purpose**: Existing Catalog item whose active status and base UoM determine whether a balance may be created.

**Fields Used by This Feature**:

- `Id`: Used as `InventoryBalance.StockKeepingUnitId`.
- `Code`: Returned for display context.
- `Name`: Returned for display context.
- `BaseUnitOfMeasureId`: Determines the UoM in which the balance quantity is expressed.
- `IsActive`: Must be true for new balance creation.

**Behavior Unchanged by This Feature**:

- SKU create, update, list, get, deactivate, and reactivate behavior remains governed by the Catalog/SKU slices.
- Existing balances keep their SKU reference if a SKU is later deactivated unless a future workflow defines another rule.

## StorageLocation

**Purpose**: Existing Topology location identifying where stock is physically placed.

**Fields Used by This Feature**:

- `Id`: Used as `InventoryBalance.StorageLocationId`.
- `WarehouseId`: Supplies warehouse visibility.
- `StorageLocationTypeId`: Must reference an active storage location type for create eligibility.
- `StorageLocationStatusId`: Must reference an active storage location status for create eligibility.
- `Code`: Returned for display context.
- `Name`: Returned for display context.
- `IsPickable`: Returned only if useful for display; it does not restrict balance creation.
- `IsActive`: Must be true for new balance creation.

**Behavior Unchanged by This Feature**:

- Storage location create, update, list, get, deactivate, and reactivate behavior remains governed by Topology.
- Existing balances keep their storage location reference if a location is later deactivated unless a future workflow defines another rule.

## Warehouse

**Purpose**: Existing Topology context used for inventory visibility.

**Fields Used by This Feature**:

- `Id`: Returned as derived warehouse identity and used by the list warehouse filter.
- `Code`: Returned for display context where existing query patterns make it available.
- `Name`: Returned for display context where existing query patterns make it available.

**Persistence Rule**:

- `InventoryBalance` must not store `WarehouseId`. Warehouse context is derived from storage location.

## UnitOfMeasure

**Purpose**: Existing Catalog context identifying the SKU base unit in which balance quantity is expressed.

**Fields Used by This Feature**:

- `Id`: Returned as derived base UoM identity through SKU.
- `Code`: Returned for display context where existing query patterns make it available.
- `Symbol`: Returned for display context where existing query patterns make it available.

**Persistence Rule**:

- `InventoryBalance` must not store `UnitOfMeasureId`. Quantity is interpreted through the SKU base UoM.

## InventoryBalanceDetails

**Purpose**: Read model returned by Inventory Balance handlers, API endpoints, WebApp API client, and validation scenarios.

**Fields**:

- `Id`
- `StockKeepingUnitId`
- `StockKeepingUnitCode`
- `StockKeepingUnitName`
- `StorageLocationId`
- `StorageLocationCode`
- `StorageLocationName`
- `WarehouseId`
- `WarehouseCode`
- `WarehouseName`
- `BaseUnitOfMeasureId`
- `BaseUnitOfMeasureCode`
- `BaseUnitOfMeasureSymbol`
- `Quantity`
- `CreatedAtUtc`
- `UpdatedAtUtc`

**Projection Rules**:

- Must support query projection for get and list operations.
- Must derive warehouse fields through the storage location relationship.
- Must derive base UoM fields through the SKU relationship.
- Must not require denormalized warehouse or UoM columns on `InventoryBalance`.

## CreateInventoryBalance Command

**Purpose**: Creates one current balance for a SKU/location pair.

**Inputs**:

- `StockKeepingUnitId`
- `StorageLocationId`
- `Quantity`

**Result**:

- Success returns `InventoryBalanceDetails`.
- Missing or empty SKU identity returns field-specific validation feedback.
- Missing or empty storage location identity returns field-specific validation feedback.
- Negative quantity returns field-specific validation feedback.
- Missing SKU returns missing-SKU feedback.
- Inactive SKU or SKU without base UoM returns validation feedback.
- Missing storage location returns missing-location feedback.
- Inactive storage location, inactive storage location type, or inactive storage location status returns validation feedback.
- Duplicate SKU/location pair returns duplicate-balance feedback and keeps the existing balance unchanged.

## GetInventoryBalanceById Query

**Purpose**: Retrieves one inventory balance by identity.

**Inputs**:

- `InventoryBalanceId`

**Result**:

- Existing balance returns `InventoryBalanceDetails`.
- Missing balance returns not found.

## ListInventoryBalances Query

**Purpose**: Lists balances for stock visibility.

**Inputs**:

- Existing bounded-list inputs if local WMS list patterns apply: `Skip`, `Take`, `SortBy`, `SortDescending`.
- `StockKeepingUnitId`
- `StorageLocationId`
- `WarehouseId`

**Filter Rules**:

- No filters returns available balances, including zero quantity balances.
- `StockKeepingUnitId` returns balances only for that SKU across warehouses and storage locations.
- `StorageLocationId` returns balances only for that storage location.
- `WarehouseId` returns balances whose storage location belongs to that warehouse.
- `StockKeepingUnitId` and `WarehouseId` together return balances for that SKU in that warehouse.

**Result**:

- Returns bounded items plus total count, skip, and take if the existing `ListResult<T>` pattern is used.
- Each item includes display context for SKU, storage location, warehouse, base UoM, quantity, and timestamps.

## UpdateInventoryBalanceQuantity Command

**Purpose**: Changes only the quantity of an existing balance.

**Inputs**:

- `InventoryBalanceId`
- `Quantity`

**Result**:

- Success returns `InventoryBalanceDetails` with the updated quantity and updated timestamp.
- Missing balance returns not found.
- Negative quantity returns field-specific validation feedback.
- SKU and storage location are not accepted in the update contract and cannot be changed.

## Persistence Shape

**Table**: `wms.inventory_balances`

**Columns**:

- `Id`
- `StockKeepingUnitId`
- `StorageLocationId`
- `Quantity`
- `CreatedAtUtc`
- `UpdatedAtUtc`

**Indexes and Constraints**:

- Primary key on `Id`.
- Required foreign key from `StockKeepingUnitId` to `wms.stock_keeping_units.Id`.
- Required foreign key from `StorageLocationId` to `wms.storage_locations.Id`.
- Unique index on `(StockKeepingUnitId, StorageLocationId)`.
- Index on `StorageLocationId`.
- Foreign key delete behavior should prevent accidental deletion of referenced SKU or storage location while balances exist.
- Decimal precision must follow an explicit EF configuration suitable for WMS quantities and consistent with existing project conventions.

## Out of Scope Data

The MVP must not add data model fields, tables, relationships, or reference records for:

- Inventory transactions or movement history.
- Receiving, putaway, picking, shipping, LPN, reservations, or allocations.
- Batch/lot, expiry, serial number, packaging, or cycle counting.
- UoM conversion or alternative UoM behavior.
- Delete, deactivate, reactivate, or zero-balance cleanup behavior.
- Seed or demo balances.
- External integration messages.
- WebApp UI state.
- Denormalized warehouse or base UoM business state on `InventoryBalance`.
