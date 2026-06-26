# API Contract: Inventory Counting MVP

All routes are under `/api/wms/inventory`. Read operations use existing required-load behavior. Write operations return `ApiResult<InventoryCountDetails>`. Actor identity is derived from the authenticated server principal and is never present in request JSON.

## List counts

```http
GET /api/wms/inventory/counts
```

Query parameters:

- `skip`, `take`;
- `sortBy`: `CreatedAtUtc`, `Status`, or `WarehouseCode`;
- `sortDescending`;
- optional `warehouseId`;
- optional exact `status`;
- optional `createdFromUtc`, `createdToUtc`.

Returns `ListResult<InventoryCountListItem>`. Default order is newest `CreatedAtUtc`, then newest ID. Progress totals use current lines only.

## Create count

```http
POST /api/wms/inventory/counts
Content-Type: application/json
```

```json
{
  "warehouseId": "018f0000-0000-7000-8000-000000000101",
  "reason": "Monthly aisle verification"
}
```

- `200 OK`: Draft `InventoryCountDetails`.
- `400`: inactive/ineligible warehouse or invalid reason.
- `404`: warehouse does not exist.
- `401`: no stable authenticated actor.

## Get count details

```http
GET /api/wms/inventory/counts/{inventoryCountId}
```

- `200 OK`: complete details including current and Superseded lines.
- `404`: count does not exist.

## Add line

```http
POST /api/wms/inventory/counts/{inventoryCountId}/lines
Content-Type: application/json
```

```json
{
  "stockKeepingUnitId": "018f0000-0000-7000-8000-000000000201",
  "storageLocationId": "018f0000-0000-7000-8000-000000000301",
  "expectedCountVersion": "AAAAAAAAB9E="
}
```

Captures current quantity and balance rowversion/absence.

- `400`: invalid/inactive/cross-warehouse/transit reference.
- `404`: count, SKU, or location missing.
- `409`: stale/final count or duplicate current pair.
- `401`: no actor.

## Remove Pending line

```http
DELETE /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}?expectedLineVersion={base64}
```

- `200 OK`: updated count details.
- `404`: count or line missing.
- `409`: stale line or line is not Pending.
- `401`: no actor.

## Record counted quantity

```http
POST /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/count
Content-Type: application/json
```

```json
{
  "countedQuantity": 12.0000,
  "comment": "Two units found behind pallet",
  "expectedLineVersion": "AAAAAAAAB9I="
}
```

Recalculates variance, records counter/time, and moves Draft to InProgress on first count entry.

- `400`: negative quantity or overlong comment.
- `404`: count or line missing.
- `409`: stale/final/incompatible state.
- `401`: no actor.

## Apply line

```http
POST /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/apply
Content-Type: application/json
```

```json
{
  "expectedLineVersion": "AAAAAAAAB9M="
}
```

Success:

- zero variance: line Applied, no balance/transaction/ledger change;
- non-zero variance: balance becomes counted quantity, one Adjustment transaction and one ledger entry are created, and line references the transaction.

Conflict:

- snapshot presence/version changed;
- line becomes Conflict and the response is `409`;
- no balance, transaction, or ledger change is made.

Other failures:

- `404`: count or line missing;
- `409`: stale line, already Applied, Superseded, or final count;
- `401`: no actor.

## Supersede Conflict line

```http
POST /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/supersede
Content-Type: application/json
```

```json
{
  "expectedLineVersion": "AAAAAAAAB9Q="
}
```

Atomically changes the Conflict line to Superseded and adds a new Pending replacement with a fresh quantity and balance version/absence snapshot.

- `404`: count or line/reference missing.
- `409`: stale line, line not Conflict, already superseded, final count, or current-line race.
- `401`: no actor.

## Complete count

```http
POST /api/wms/inventory/counts/{inventoryCountId}/complete
Content-Type: application/json
```

```json
{
  "expectedCountVersion": "AAAAAAAAB9U="
}
```

- `200 OK`: Completed details with completer/time.
- `409`: stale/final count, no current lines, or any current line not Applied.
- `404`: count missing.
- `401`: no actor.

## Cancel count

```http
POST /api/wms/inventory/counts/{inventoryCountId}/cancel
Content-Type: application/json
```

```json
{
  "expectedCountVersion": "AAAAAAAAB9Y="
}
```

- `200 OK`: Cancelled details with canceller/time.
- `409`: stale or already final count.
- `404`: count missing.
- `401`: no actor.

Applied inventory changes are not reversed.

## Error semantics

- `400 Bad Request`: malformed values/versions or ineligible active references.
- `401 Unauthorized`: write has no stable authenticated actor.
- `404 Not Found`: requested count, line, SKU, warehouse, or location does not exist.
- `409 Conflict`: stale rowversion/balance snapshot, duplicate current line, incompatible lifecycle, or concurrent insertion/update.

Cancellation tokens propagate through endpoint, dispatcher, handler, EF operation, and client.
