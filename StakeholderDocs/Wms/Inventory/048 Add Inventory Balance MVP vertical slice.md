Create a Spec Kit feature specification for issue #47 Add Inventory Balance MVP vertical slice.

Use current branch: 048-add-inventory-balance-mvp-vertical-slice.

Goal:
Add the first minimal inventory balance capability so Myrmex WMS can store and review stock quantity for a SKU at a storage location.

Use existing project patterns from Catalog/SKU, Catalog/UoM, SKU Barcode, and Warehouse/Storage Location slices.

Keep scope minimal:
- InventoryBalance entity
- StockKeepingUnit reference
- StorageLocation reference
- Quantity stored in SKU base unit of measure
- create/update/get/list behavior
- persistence mapping and tests
- API contract and WebApp client contract if needed

Out of scope:
- receiving
- putaway
- picking
- shipping
- LPN
- batch/lot
- expiry date
- serial numbers
- reservations
- inventory transactions
- movement history
- UoM conversions
- packaging
- cycle counting
- seed/demo data
- external integrations

Do not run build, tests, app startup, EF migration generation, database update, or infrastructure commands.

When migration work becomes necessary, stop and recommend exact developer-controlled commands.