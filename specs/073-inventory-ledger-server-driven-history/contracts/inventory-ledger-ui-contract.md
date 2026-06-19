# Contract: Inventory Ledger UI

## Navigation

Add Inventory Ledger to the existing WMS Inventory navigation beside Inventory Balances.

Expected route:

```text
/wms/inventory/ledger
```

Supported query parameters:

```text
stockKeepingUnitId
warehouseId
storageLocationId
```

When all three query parameters are present, the Ledger page initializes with those filters active.

Inventory Balance history navigation uses:

```text
/wms/inventory/ledger?stockKeepingUnitId={Sku.Id}&warehouseId={StorageLocation.Warehouse.Id}&storageLocationId={StorageLocation.Id}
```

## Query-State Hydration

Copied or reloaded URLs must restore the same visible filter state and issue the same filtered history request.

Initialization flow:

1. Bind `stockKeepingUnitId`, `warehouseId`, and `storageLocationId`.
2. Load inactive-inclusive warehouses.
3. Resolve the selected warehouse.
4. Resolve the exact SKU by ID using existing `WmsCatalogApiClient.GetStockKeepingUnitByIdAsync`.
5. Resolve the exact storage location by ID using existing `WmsTopologyApiClient.GetStorageLocationByIdAsync` and verify it belongs to the selected warehouse.
6. Populate selected filter display objects.
7. Apply the IDs to the ledger request.
8. Load the first grid page.

Use existing `WmsTopologyApiClient.GetWarehouseByIdAsync` if the inactive-inclusive warehouse list does not contain the routed warehouse. Current exact get-by-id handlers for SKU, warehouse, and storage location project by ID without `IsActive` filters, so they can restore inactive historical references. If an exact read is missing during implementation, add the smallest feature-specific read needed for hydration. Do not hydrate selected SKU or storage location by searching the first page of empty-search autocomplete results.

## Inventory Ledger Page

The page is read-only.

### Initial Load

When opened without navigation/query filters:

- Load unfiltered ledger history.
- Use default newest-first ordering.
- Use server-side paging.

### Filters

The page provides:

- SKU autocomplete.
- Warehouse selector.
- Storage-location autocomplete.
- Transaction-type selector.
- Occurrence-from UTC control.
- Occurrence-to UTC control.
- Clear/reset action.

Filter behavior:

- SKU lookup uses inactive-inclusive search.
- Warehouse list includes inactive warehouses.
- Storage-location lookup is disabled until a warehouse is selected.
- Storage-location lookup is scoped to selected warehouse and inactive-inclusive.
- Changing warehouse clears incompatible selected storage location.
- Changing any filter resets the grid to the first page.
- Clearing filters returns the page to normal unfiltered browsing.
- Expected cancellation from rapid filter or lookup changes is not shown as an error.

### Occurrence Range

UI labels must make UTC behavior clear.

Request mapping:

```text
OccurredFromUtc = exact UTC lower bound, inclusive
OccurredToUtc = exact UTC upper bound, exclusive
```

Invalid range where from is later than to must be shown as validation feedback and must not silently issue an ambiguous request.

## Grid

Use server-driven grid behavior.

Recommended columns:

- Occurred UTC.
- Transaction type.
- SKU.
- Warehouse.
- Storage location.
- Balance before.
- Delta.
- Balance after.
- Reason.
- Details action.

Display rules:

- Quantity formatting follows Inventory Balance conventions.
- Delta preserves sign.
- Do not rely on color alone to communicate positive or negative movement.
- Long reasons may be shortened in the grid, but full reason is visible in details.
- No edit, delete, correction, reversal, transfer, export, or analytics action is shown.

## Transaction Details Dialog

Opening details from a row shows:

- Transaction ID.
- Transaction type.
- Reason.
- Occurred UTC.
- Created UTC.
- All ledger entries in deterministic order.

For each entry, show:

- SKU code and name.
- Base UoM code and symbol.
- Warehouse code and name.
- Storage-location code and name.
- Balance before.
- Quantity delta.
- Balance after.

No mutation controls are allowed.

## Inventory Balance Integration

Each Inventory Balance row provides a history action.

Action behavior:

```text
navigate to /wms/inventory/ledger?stockKeepingUnitId={Sku.Id}&warehouseId={StorageLocation.Warehouse.Id}&storageLocationId={StorageLocation.Id}
```

Expected Ledger result:

- The SKU, warehouse, and storage-location filters are active.
- The user can clear or change filters.
- Browser navigation and copied links preserve the filtered history context.

## Manual Smoke Validation

The implementation quickstart owns manual validation. At minimum, verify:

- Navigation link opens Ledger.
- Initial load is unfiltered and newest-first.
- Filters apply and reset paging.
- Balance row history action opens filtered Ledger.
- Copied Ledger URLs restore visible SKU, warehouse, and storage-location filters and request the same filtered history.
- Details dialog shows transaction header and all entries.
- Inactive historical references remain visible/searchable.
- No mutation/export/analytics controls appear.
