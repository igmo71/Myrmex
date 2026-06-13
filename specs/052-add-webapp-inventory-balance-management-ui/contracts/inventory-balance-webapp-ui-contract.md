# Contract: Inventory Balance WebApp UI

This contract defines the expected user-facing WebApp behavior for Inventory Balance management. It depends on the existing Inventory Balance API contract from the backend slice and the existing WebApp WMS API clients.

## Route and Navigation

**Route**: `/wms/inventory/balances`

**Navigation**:

- The WMS navigation exposes Inventory.
- Inventory exposes Inventory Balances.
- A user can reach Inventory Balances from the main application navigation in no more than 3 interactions.

## Page Contract

The Inventory Balances page shows:

- Title: `Inventory Balances`
- A short description consistent with existing WMS page headings.
- Filter area.
- Create action.
- Refresh action.
- Paged grid.
- Page-level alert for list or lookup load failures.
- Empty state when a successful list returns no rows.

## Grid Contract

Each visible row includes:

- SKU code.
- SKU name.
- Warehouse code/name.
- Storage location code/name.
- Quantity.
- SKU base UoM code or symbol.
- Row action for quantity update.

Rules:

- Zero quantity balances are visible.
- Rows do not expose delete, deactivate, reactivate, movement, transaction, reservation, or adjustment actions.
- Timestamps may be shown if consistent with existing grid patterns, but are not required for the MVP.

## Filter Contract

Filters:

- Warehouse.
- Storage location.
- SKU.

Rules:

- Changing warehouse reloads the list.
- Clearing warehouse clears storage location.
- Storage location selection is disabled until a warehouse is selected.
- When warehouse is selected, storage location choices are limited to that warehouse.
- Changing warehouse clears storage location if the previous storage location does not belong to the new warehouse.
- Changing storage location reloads the list.
- Changing SKU reloads the list.
- SKU and warehouse filters can be combined.
- No matching rows produce an empty state, not an error.

## Create Dialog Contract

The create dialog collects:

- SKU.
- Warehouse.
- Storage location.
- Quantity.

The create dialog displays:

- Selected SKU base UoM as read-only context.
- Dialog-local validation or API failure messages.
- Loading state for lookup data.
- Saving state while the create request is in flight.

Rules:

- SKU is required.
- Warehouse is required.
- Storage location is disabled until warehouse is selected.
- Storage location choices are limited to the selected warehouse.
- Quantity is required and must be greater than or equal to zero.
- The submitted request contains SKU, storage location, and quantity.
- Duplicate SKU/location conflict is displayed using existing WebApp error behavior.
- Successful create closes the dialog, shows success feedback, and refreshes the list.

## Update Quantity Dialog Contract

The update dialog displays read-only:

- SKU code/name.
- Warehouse code/name.
- Storage location code/name.
- Base UoM code or symbol.

The update dialog edits:

- Quantity only.

Rules:

- Quantity is required and must be greater than or equal to zero.
- The submitted request contains only quantity.
- SKU, warehouse, storage location, and base UoM cannot be edited.
- Missing balance or validation failures are displayed using existing WebApp error behavior.
- Successful update closes the dialog, shows success feedback, and refreshes the list.

## API Client Usage Contract

Use `WmsInventoryApiClient`:

- `ListInventoryBalancesAsync(ListInventoryBalancesRequest request, CancellationToken cancellationToken = default)`
- `TryCreateInventoryBalanceAsync(CreateInventoryBalanceRequest request, CancellationToken cancellationToken = default)`
- `TryUpdateInventoryBalanceQuantityAsync(Guid inventoryBalanceId, UpdateInventoryBalanceQuantityRequest request, CancellationToken cancellationToken = default)`
- `GetInventoryBalanceByIdAsync(Guid inventoryBalanceId, CancellationToken cancellationToken = default)` only if the chosen UI flow needs a fresh detail load before update.

Use `WmsCatalogApiClient`:

- `ListStockKeepingUnitsAsync(ListRequest request, CancellationToken cancellationToken = default)`
- `ListUnitsOfMeasureAsync(ListRequest request, CancellationToken cancellationToken = default)` only if base UoM display cannot be derived from already loaded SKU or balance context.

Use `WmsTopologyApiClient`:

- `ListWarehousesAsync(ListRequest request, CancellationToken cancellationToken = default)`
- `ListStorageLocationsByWarehouseAsync(Guid warehouseId, ListRequest request, CancellationToken cancellationToken = default)`

Read/load methods may throw the existing API exception shape on failed responses. Write/action methods return `ApiResult<T>` failures.

## Feedback and Error Contract

- List/load failures show a page-level error alert.
- Dialog submit failures remain in the dialog and keep entered values where possible.
- Successful create shows `Inventory balance created.` or equivalent existing success feedback.
- Successful update shows `Inventory balance quantity updated.` or equivalent existing success feedback.
- Duplicate, validation, missing-reference, missing-balance, and unexpected failures show user-visible messages.

## Out of Scope Contract

No UI route, page, grid, dialog, filter, action, client call, or validation flow may expose:

- Receiving.
- Putaway.
- Picking.
- Shipping.
- LPN.
- Batch/lot tracking.
- Expiry date.
- Serial numbers.
- Reservations.
- Inventory transactions.
- Inventory movement history.
- Adjustment documents.
- UoM conversions.
- Packaging.
- Cycle counting.
- Inventory Balance delete.
- Inventory Balance deactivate/reactivate.
- Changing SKU or storage location of an existing Inventory Balance.
- Bulk editing.
- Import/export.
- Seed/demo data.
- External integrations.
- Backend domain redesign.
- Backend persistence redesign.
