# Data Model: WebApp Inventory Balance Management UI

This feature does not add backend persistence. The model below describes UI state, read/write contracts consumed by the WebApp, and validation rules that protect the user flows.

## InventoryBalancePageState

**Purpose**: Coordinates the Inventory Balances page.

**Fields**:

- `Balances`: Current page of `InventoryBalanceDetails` rows.
- `TotalCount`: Total matching balance count when provided by the list result.
- `Skip`: Current list offset.
- `Take`: Current page size.
- `SortBy`: Current sort field, defaulting to the existing Inventory Balance list default.
- `SortDescending`: Current sort direction.
- `Filters`: Current `InventoryBalanceFilters`.
- `IsLoadingBalances`: True while the list request is in flight.
- `IsLoadingWarehouses`: True while warehouse lookup is in flight.
- `IsLoadingSkus`: True while SKU lookup is in flight.
- `IsLoadingStorageLocations`: True while warehouse-scoped storage location lookup is in flight.
- `ErrorMessage`: Page-level read/load failure message.

**Validation Rules**:

- A balance list load must distinguish failed load from empty successful results.
- Zero quantity rows remain visible.
- Active filters remain visible after list refresh.

**State Transitions**:

```text
Initialize -> Load lookups -> Load balance list
Filter changed -> Reset incompatible dependent selection -> Load balance list
Create succeeds -> Close dialog -> Show success -> Refresh balance list
Quantity update succeeds -> Close dialog -> Show success -> Refresh balance list
Read/load fails -> Show page error -> Keep recoverable filter state
```

## InventoryBalanceFilters

**Purpose**: Captures the user-selected list criteria.

**Fields**:

- `WarehouseId`: Optional warehouse filter.
- `StorageLocationId`: Optional storage location filter. Disabled until `WarehouseId` is selected.
- `StockKeepingUnitId`: Optional SKU filter.

**Validation Rules**:

- `StorageLocationId` cannot be selected unless `WarehouseId` is selected.
- Changing `WarehouseId` clears `StorageLocationId` when the current location is null or belongs to a different warehouse.
- Applying `WarehouseId` and `StockKeepingUnitId` together must request SKU-within-warehouse results.
- Clearing `WarehouseId` must also clear `StorageLocationId`.

## CreateInventoryBalanceDialogState

**Purpose**: Captures user input for creating a current balance.

**Fields**:

- `StockKeepingUnitId`: Required selected active SKU.
- `SelectedSku`: Selected SKU details used to display code, name, and base UoM context.
- `WarehouseId`: Required selected active warehouse.
- `StorageLocationId`: Required selected active storage location from the selected warehouse.
- `Quantity`: Required non-negative decimal value.
- `BaseUnitOfMeasureDisplay`: Read-only display derived from the selected SKU.
- `IsLoadingSkus`: True while SKU lookup is in flight.
- `IsLoadingWarehouses`: True while warehouse lookup is in flight.
- `IsLoadingStorageLocations`: True while storage location lookup is in flight.
- `IsSaving`: True while create is in flight.
- `ErrorMessage`: Dialog-local validation or API failure message.

**Validation Rules**:

- SKU is required.
- Warehouse is required before storage location can be selected.
- Storage location is required.
- Quantity must be greater than or equal to zero.
- Base UoM is display-only and cannot be edited.
- Backend validation remains authoritative for duplicate SKU/location and reference eligibility failures.

**State Transitions**:

```text
Open create dialog -> Load SKU and warehouse lookups
SKU selected -> Refresh base UoM display
Warehouse selected -> Clear location if incompatible -> Load locations for warehouse
Save valid form -> Submit create request
Create failure -> Stay open with entered values and error
Create success -> Close with created balance
```

## UpdateInventoryBalanceQuantityDialogState

**Purpose**: Captures quantity correction for an existing balance.

**Fields**:

- `InventoryBalanceId`: Required target balance identity.
- `StockKeepingUnitDisplay`: Read-only SKU code/name context.
- `WarehouseDisplay`: Read-only warehouse code/name context.
- `StorageLocationDisplay`: Read-only storage location code/name context.
- `BaseUnitOfMeasureDisplay`: Read-only base UoM code/symbol context.
- `Quantity`: Required editable non-negative decimal value.
- `IsSaving`: True while update is in flight.
- `ErrorMessage`: Dialog-local validation or API failure message.

**Validation Rules**:

- Quantity is the only editable business value.
- Quantity must be greater than or equal to zero.
- SKU, warehouse, storage location, and base UoM context are read-only.
- Not-found failures remain recoverable and should allow the page to refresh.

**State Transitions**:

```text
Open update dialog from row -> Seed read-only context and current quantity
Save valid quantity -> Submit quantity-only request
Update failure -> Stay open with entered quantity and error
Update success -> Close with updated balance
```

## InventoryBalanceGridRow

**Purpose**: User-facing row in the Inventory Balances grid.

**Fields**:

- `Id`
- `StockKeepingUnitId`
- `StockKeepingUnitCode`
- `StockKeepingUnitName`
- `WarehouseId`
- `WarehouseCode`
- `WarehouseName`
- `StorageLocationId`
- `StorageLocationCode`
- `StorageLocationName`
- `Quantity`
- `BaseUnitOfMeasureId`
- `BaseUnitOfMeasureCode`
- `BaseUnitOfMeasureSymbol`
- `CreatedAtUtc`
- `UpdatedAtUtc`

**Display Rules**:

- SKU code/name may be combined in one cell when consistent with existing grid patterns.
- Warehouse and storage location code/name may be combined in one cell when consistent with existing grid patterns.
- Quantity is displayed with the base UoM code or symbol.
- Each row exposes only a quantity update action for this MVP.

## Lookup Entities

### Warehouse Lookup

**Purpose**: Supplies active warehouses for filters and create dialog.

**Rules**:

- Uses existing Topology client behavior.
- Inactive warehouses are not selectable when the lookup contract supports active-only filtering.

### Storage Location Lookup

**Purpose**: Supplies storage locations for a selected warehouse.

**Rules**:

- Disabled until a warehouse is selected.
- Uses warehouse-scoped Topology lookup behavior.
- Should request active locations where supported.
- Backend create validation remains authoritative for active storage location type/status eligibility.
- `IsPickable` does not restrict Inventory Balance creation.

### SKU Lookup

**Purpose**: Supplies active SKUs and base UoM context for filters and create dialog.

**Rules**:

- Uses existing Catalog client behavior.
- Active SKUs are selectable when the lookup contract supports active-only filtering.
- Selected SKU base UoM is displayed as read-only context.

## Out of Scope State

The UI state model must not add fields or flows for:

- Inventory transactions or movement history.
- Receiving, putaway, picking, shipping, LPN, reservations, or allocations.
- Batch/lot, expiry, serial number, packaging, or cycle counting.
- UoM conversion or alternative UoM quantity.
- Delete, deactivate, reactivate, or zero-balance cleanup.
- Bulk edit, import, or export.
- Seed/demo data.
- External integrations.
