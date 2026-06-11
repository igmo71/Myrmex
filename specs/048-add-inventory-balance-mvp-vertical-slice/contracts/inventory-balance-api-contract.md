# Contract: Inventory Balance API

This contract defines the expected external behavior for the Inventory Balance MVP. It adds a new Inventory route group inside the existing WMS API surface.

## API Route Group

Base route: `/api/wms/inventory`

Tags: `Wms Inventory`

Inventory balance route prefix: `/api/wms/inventory/balances`

## Payloads

### InventoryBalanceDetails

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "stockKeepingUnitId": "11111111-1111-1111-1111-111111111111",
  "stockKeepingUnitCode": "ITEM-001",
  "stockKeepingUnitName": "Widget",
  "storageLocationId": "22222222-2222-2222-2222-222222222222",
  "storageLocationCode": "A-01-01",
  "storageLocationName": "Aisle 1 Bin 1",
  "warehouseId": "33333333-3333-3333-3333-333333333333",
  "warehouseCode": "WH-A",
  "warehouseName": "Main Warehouse",
  "baseUnitOfMeasureId": "44444444-4444-4444-4444-444444444444",
  "baseUnitOfMeasureCode": "EA",
  "baseUnitOfMeasureSymbol": "ea",
  "quantity": 10.0,
  "createdAtUtc": "2026-06-11T00:00:00+00:00",
  "updatedAtUtc": null
}
```

On create, `updatedAtUtc` remains `null`. It is set only after a successful quantity update.

### CreateInventoryBalanceRequest

```json
{
  "stockKeepingUnitId": "11111111-1111-1111-1111-111111111111",
  "storageLocationId": "22222222-2222-2222-2222-222222222222",
  "quantity": 10.0
}
```

### UpdateInventoryBalanceQuantityRequest

```json
{
  "quantity": 5.0
}
```

The update request accepts only `quantity`. SKU and storage location are not part of the update contract.

### ListResult<InventoryBalanceDetails>

```json
{
  "items": [],
  "totalCount": 0,
  "skip": 0,
  "take": 20
}
```

Each list item must include the same display context as `InventoryBalanceDetails`.

## Endpoints

### Create Inventory Balance

`POST /api/wms/inventory/balances`

**Request**: `CreateInventoryBalanceRequest`

**Success**: returns `InventoryBalanceDetails` for the created balance.

**Behavior**:

- Requires `stockKeepingUnitId`.
- Requires `storageLocationId`.
- Requires non-negative decimal `quantity`.
- Requires the SKU to exist, be active, and have a base UoM.
- Requires the storage location to exist and be eligible: active location with active type and active status.
- `IsPickable` and storage location type code do not restrict eligibility.
- Prevents more than one balance for the same SKU/location pair.
- Returns warehouse context derived through storage location.
- Returns base UoM context derived through SKU.

**Failure behavior**:

- Missing or empty SKU identity returns validation ProblemDetails with `stockKeepingUnitId` field details.
- Missing or empty storage location identity returns validation ProblemDetails with `storageLocationId` field details.
- Negative quantity returns validation ProblemDetails with `quantity` field details.
- Missing SKU returns missing-SKU feedback using existing Myrmex error style.
- Inactive SKU or SKU without base UoM returns validation feedback.
- Missing storage location returns missing-storage-location feedback using existing Myrmex error style.
- Inactive storage location, inactive storage location type, or inactive storage location status returns validation feedback.
- Duplicate SKU/location pair returns conflict feedback and keeps the existing balance unchanged.

### Get Inventory Balance By Id

`GET /api/wms/inventory/balances/{inventoryBalanceId:guid}`

**Success**: returns `InventoryBalanceDetails`.

**Failure behavior**:

- Missing balance returns existing not-found ProblemDetails style with an Inventory Balance not-found code.

### List Inventory Balances

`GET /api/wms/inventory/balances`

**Query parameters**:

- `skip`
- `take`
- `sortBy`
- `sortDescending`
- `stockKeepingUnitId`
- `storageLocationId`
- `warehouseId`

**Success**: returns `ListResult<InventoryBalanceDetails>`.

**Behavior**:

- No filters returns available balances, including zero quantity balances.
- `stockKeepingUnitId` returns balances only for that SKU across warehouses and storage locations.
- `storageLocationId` returns balances only for that location.
- `warehouseId` returns balances whose storage location belongs to that warehouse.
- `stockKeepingUnitId` and `warehouseId` together return balances for that SKU in that warehouse.
- List items include SKU, storage location, warehouse, base UoM, quantity, and timestamp context.

**Failure behavior**:

- If filter validation follows existing WMS patterns, nonexistent filter references may return not-found feedback. Otherwise, filters with no matching balances return an empty list. The implementation must choose the pattern consistent with existing list handlers.

### Update Inventory Balance Quantity

`PUT /api/wms/inventory/balances/{inventoryBalanceId:guid}/quantity`

**Request**: `UpdateInventoryBalanceQuantityRequest`

**Success**: returns updated `InventoryBalanceDetails`.

**Behavior**:

- Accepts only the new non-negative quantity.
- Does not accept SKU or storage location in the request body.
- Allows updating quantity to zero.
- Keeps the same SKU and storage location.
- Updates `updatedAtUtc`.

**Failure behavior**:

- Missing balance returns existing not-found ProblemDetails style with an Inventory Balance not-found code.
- Negative quantity returns validation ProblemDetails with `quantity` field details.

## Web API Client Contract

Client path: `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`

Expected records:

- `InventoryBalanceDetails`
- `CreateInventoryBalanceRequest`
- `UpdateInventoryBalanceQuantityRequest`

Expected methods:

- `ListInventoryBalancesAsync(ListInventoryBalancesRequest request, CancellationToken cancellationToken = default)`
- `GetInventoryBalanceByIdAsync(Guid inventoryBalanceId, CancellationToken cancellationToken = default)`
- `TryCreateInventoryBalanceAsync(CreateInventoryBalanceRequest request, CancellationToken cancellationToken = default)`
- `TryUpdateInventoryBalanceQuantityAsync(Guid inventoryBalanceId, UpdateInventoryBalanceQuantityRequest request, CancellationToken cancellationToken = default)`

Read/list methods throw the existing API exception shape on failed responses. Write/action methods return the existing API result shape on failed responses.

## Out of Scope Contract

No endpoint, payload, client method, or contract may expose:

- Delete inventory balance.
- Deactivate or reactivate inventory balance.
- Inventory transactions, movement history, reservations, or adjustments.
- Receiving, putaway, picking, shipping, or LPN state.
- Batch/lot, expiry, serial number, packaging, or cycle counting state.
- UoM conversion or alternative UoM quantities.
- Seed or demo balance management.
- External integration state or messages.
- WebApp pages, navigation, forms, grids, dialogs, or component behavior.
