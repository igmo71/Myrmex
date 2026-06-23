# API Contract: Internal Inventory Transfer MVP

Base route: `/api/wms/inventory`

All write/action operations use existing Myrmex `ApiResult<T>` and ProblemDetails conventions through the WebApp API client. Read/list operations use existing required read behavior and ProblemDetails awareness.

## Create Transfer

```http
POST /api/wms/inventory/transfers
```

### Request

```json
{
  "sourceWarehouseId": "00000000-0000-0000-0000-000000000001",
  "destinationWarehouseId": "00000000-0000-0000-0000-000000000001",
  "transitStorageLocationId": null,
  "lines": [
    {
      "stockKeepingUnitId": "00000000-0000-0000-0000-000000000101",
      "sourceStorageLocationId": "00000000-0000-0000-0000-000000000201",
      "destinationStorageLocationId": "00000000-0000-0000-0000-000000000202",
      "requestedQuantity": 10
    }
  ]
}
```

For transit transfer, `transitStorageLocationId` is an active internal transit location in the same warehouse.

### Success Response

Returns transfer details, including transfer id, code, status, lines, computed zero progress, and empty movement history.

### Error Outcomes

- Invalid identifiers, missing lines, non-positive requested quantity: validation ProblemDetails.
- Source and destination warehouses differ: validation or conflict ProblemDetails.
- Missing SKU, warehouse, or storage location: not-found ProblemDetails.
- Inactive or wrong-type references: validation ProblemDetails.

## Get Transfer Details

```http
GET /api/wms/inventory/transfers/{transferId:guid}
```

Line details include SKU, source location, destination location, requested, picked, placed, in-transit, remaining-to-pick, and remaining-to-place quantities.

Movement details include occurred time, SKU, from location, to location, quantity, derived movement meaning, and inventory transaction id.

## List Transfers

```http
GET /api/wms/inventory/transfers
```

### Query Parameters

- `skip`
- `take`
- `sortBy`
- `sortDescending`
- `warehouseId`
- `status`
- `createdFromUtc`
- `createdToUtc`
- `transferCode`
- `sourceStorageLocationId`
- `destinationStorageLocationId`
- `stockKeepingUnitId`
- `hasTransitLocation`

### Sort Keys

- `CreatedAtUtc`
- `Code`
- `Status`
- `WarehouseCode`
- `TotalRequestedQuantity`
- `TotalPickedQuantity`
- `TotalPlacedQuantity`
- `TotalInTransitQuantity`

Default sort: newest created transfer first, with stable transfer id tie-breaker.

### Success Response

Returns `ListResult<InventoryTransferListItem>`.

Each item includes id, code, warehouse, status, created time, transit location when present, total requested, total picked, total placed, and total in-transit quantities.

## Direct Move Line

```http
POST /api/wms/inventory/transfers/{transferId:guid}/lines/{lineId:guid}/move
```

### Request

```json
{
  "quantity": 5
}
```

Allowed only when transfer has no transit location. The backend derives from and to locations from the selected line.

## Pick Line To Transit

```http
POST /api/wms/inventory/transfers/{transferId:guid}/lines/{lineId:guid}/pick
```

### Request

```json
{
  "quantity": 5
}
```

Allowed only when transfer has an internal transit location. The backend derives from location from the line source and to location from the transfer transit location.

## Place Line From Transit

```http
POST /api/wms/inventory/transfers/{transferId:guid}/lines/{lineId:guid}/place
```

### Request

```json
{
  "quantity": 5
}
```

Allowed only when transfer has an internal transit location. The backend derives from location from the transfer transit location and to location from the line destination.

## Movement Success Response

Movement operations return refreshed `InventoryTransferDetails`.

The response includes updated transfer status, updated line progress quantities, and read-only movement history including the newly committed movement and its inventory transaction reference.

## Movement Error Outcomes

- Transfer or line missing: not-found ProblemDetails.
- Quantity non-positive: validation ProblemDetails.
- Completed transfer: conflict ProblemDetails.
- Wrong movement pattern: conflict ProblemDetails.
- Insufficient balance: conflict ProblemDetails.
- Direct over-move, over-pick, or over-place: conflict ProblemDetails.
- Stale balance/transfer state discovered at save time: conflict ProblemDetails.

## Shared Contract Types

Planned public contract files:

- `CreateInventoryTransferRequest`
- `CreateInventoryTransferLineRequest`
- `MoveInventoryTransferLineRequest`
- `PickInventoryTransferLineRequest`
- `PlaceInventoryTransferLineRequest`
- `ListInventoryTransfersRequest`
- `InventoryTransferSortBy`
- `InventoryTransferStatusDetails`
- `InventoryTransferListItem`
- `InventoryTransferDetails`
- `InventoryTransferLineDetails`
- `InventoryTransferMovementDetails`

Shared contracts must remain transport-only and must not reference domain entities, EF Core, handlers, or UI component types.
