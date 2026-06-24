# API Contract: Manual Inventory Move

## Lookup balance

```http
GET /api/wms/inventory/balances/lookup?skuId={skuId}&storageLocationId={storageLocationId}
```

Endpoint: `GetInventoryBalanceBySkuAndStorageLocation`

- `200 OK`: existing `InventoryBalanceDetails`, including quantity and `balanceVersion`.
- `404 Not Found`: exact SKU/location balance does not exist.

The read returns an existing balance even when related SKU/location/type/status records are inactive. It does not create a balance or apply move eligibility.

## Move balance

```http
POST /api/wms/inventory/balances/move
Content-Type: application/json
```

Endpoint: `MoveInventoryBalance`

### Request

```json
{
  "stockKeepingUnitId": "018f0000-0000-7000-8000-000000000101",
  "sourceStorageLocationId": "018f0000-0000-7000-8000-000000000201",
  "destinationStorageLocationId": "018f0000-0000-7000-8000-000000000202",
  "quantity": 4.0000,
  "reason": "Consolidate picking stock",
  "expectedSourceBalanceVersion": "AAAAAAAAB9E="
}
```

| Field | Rules |
|-------|-------|
| `stockKeepingUnitId` | Required non-empty GUID; SKU must be active. |
| `sourceStorageLocationId` | Required non-empty GUID identifying the existing source balance. |
| `destinationStorageLocationId` | Required non-empty GUID different from source. |
| `quantity` | Greater than zero and no greater than current source quantity. |
| `reason` | Required trimmed text within the transaction reason limit. |
| `expectedSourceBalanceVersion` | Required Base64 encoding of the current 8-byte source rowversion. |

### Success

`200 OK` with:

```json
{
  "sourceBalance": { "quantity": 6.0000, "balanceVersion": "AAAAAAAAB9I=" },
  "destinationBalance": { "quantity": 7.0000, "balanceVersion": "AAAAAAAAB9M=" },
  "movedQuantity": 4.0000,
  "sourceQuantityBefore": 10.0000,
  "sourceQuantityAfter": 6.0000,
  "destinationQuantityBefore": 3.0000,
  "destinationQuantityAfter": 7.0000,
  "occurredAtUtc": "2026-06-24T09:00:00Z"
}
```

`sourceBalance` and `destinationBalance` are complete existing `InventoryBalanceDetails` objects; abbreviated objects above show only relevant fields.

### Failures

- `400 Bad Request`: malformed required values/version, non-positive quantity, invalid reason, same location, inactive SKU/location/type/status, cross-warehouse location, or transit location.
- `404 Not Found` means a requested reference or lookup target does not exist:
  - the requested lookup has no exact balance for `skuId + storageLocationId`;
  - the move references a SKU that does not exist;
  - the move references a source storage location that does not exist;
  - the move references a destination storage location that does not exist.
- `409 Conflict` means inventory state changed or cannot be safely committed based on the submitted state:
  - the SKU and source location exist, but the source balance is missing at commit time;
  - the submitted source balance version is stale;
  - the source quantity is insufficient;
  - an existing destination balance changed concurrently;
  - another request concurrently created the previously missing destination balance.

A missing destination balance before the move is not an error. The successful operation creates the destination balance with a prior quantity of zero.

Clients refresh current balances and require retry. The server does not replay the operation.

### Persisted outcome

Every success retains the source row, updates or creates destination, creates one `Transfer` transaction and exactly two balanced entries, and creates no Inventory Transfer or adjustment record.
