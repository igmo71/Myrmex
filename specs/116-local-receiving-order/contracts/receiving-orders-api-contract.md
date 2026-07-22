# Contract: Receiving Orders API

## Route Group

```text
/api/wms/receiving-orders
```

- Authorization: existing `WmsOperator` policy.
- Tag: `Wms Receiving`.
- Dispatch: existing command/query dispatchers.
- Success/error serialization: existing `ServiceResult` HTTP mapping and RFC Problem Details.

## Endpoints

| Method | Route | Input | Success |
|---|---|---|---|
| GET | `/api/wms/receiving-orders` | Query parameters from `ReceivingOrderListRequest` | `200 ListResult<ReceivingOrderListItem>` |
| GET | `/api/wms/receiving-orders/{id}` | Order ID | `200 ReceivingOrderDetails` |
| POST | `/api/wms/receiving-orders` | `CreateReceivingOrderRequest` | `200 ReceivingOrderDetails` |
| PUT | `/api/wms/receiving-orders/{id}` | `UpdateReceivingOrderRequest` | `200 ReceivingOrderDetails` |
| DELETE | `/api/wms/receiving-orders/{id}?expectedOrderVersion=...` | URL-encoded Base64 aggregate version | `204 No Content` |
| POST | `/api/wms/receiving-orders/{id}/start` | `ReceivingOrderActionRequest` | `200 ReceivingOrderDetails` |
| POST | `/api/wms/receiving-orders/{id}/lines/{lineId}/receive` | `ReceiveReceivingOrderLineRequest` | `200 ReceivingOrderDetails` |
| POST | `/api/wms/receiving-orders/{id}/complete` | `ReceivingOrderActionRequest` | `200 ReceivingOrderDetails` |

DELETE uses a query-string version to follow the existing versioned DELETE convention. The WebApp client must URL-encode Base64 values.

## Shared Requests

### ReceivingOrderListRequest

| Field | Type | Rules |
|---|---|---|
| `Skip` | `int?` | Existing list normalization; default 0. |
| `Take` | `int?` | Existing default/max list normalization. |
| `SearchText` | `string?` | Trimmed search over normalized order Number. |
| `WarehouseId` | `Guid?` | Optional exact filter. |
| `Status` | `string?` | Optional exact supported status text. |
| `SortBy` | `string?` | One supported `ReceivingOrderSortBy` value. |
| `SortDescending` | `bool?` | Default true for CreatedAtUtc. |

### CreateReceivingOrderRequest

| Field | Type | Rules |
|---|---|---|
| `Number` | `string?` | Required; server normalizes and enforces global uniqueness. |
| `WarehouseId` | `Guid?` | Required active Warehouse. |
| `ReceivingLocationId` | `Guid?` | Required eligible Receiving location in Warehouse. |
| `Lines` | `IReadOnlyList<CreateReceivingOrderLineRequest>` | Required non-empty complete initial plan. |

`CreateReceivingOrderLineRequest` contains nullable `StockKeepingUnitId` and `PlannedQuantity`. Create never accepts client line IDs.

### UpdateReceivingOrderRequest

| Field | Type | Rules |
|---|---|---|
| `Number` | `string?` | Required replacement Number. |
| `WarehouseId` | `Guid?` | Required replacement Warehouse. |
| `ReceivingLocationId` | `Guid?` | Required replacement eligible Receiving location. |
| `ExpectedOrderVersion` | `string?` | Required Base64 eight-byte aggregate rowversion. |
| `Lines` | `IReadOnlyList<UpdateReceivingOrderLineRequest>` | Required complete replacement plan. |

`UpdateReceivingOrderLineRequest` contains nullable `LineId`, nullable `StockKeepingUnitId`, and `PlannedQuantity`:

- non-null LineId: retain and update that existing order line;
- null LineId: create a new line;
- an existing order line omitted from the request: delete it;
- duplicate or foreign LineId: reject the entire update;
- duplicate final SKU: reject the entire update.

### ReceivingOrderActionRequest

| Field | Type | Rules |
|---|---|---|
| `ExpectedOrderVersion` | `string?` | Required for a real Start/Complete mutation. |

An already InProgress Start or valid already Completed Complete returns current details without mutating or rejecting only because this supplied version is stale.

### ReceiveReceivingOrderLineRequest

| Field | Type | Rules |
|---|---|---|
| `Quantity` | `decimal` | Strictly positive increment in SKU base unit. |
| `ExpectedOrderVersion` | `string?` | Required current aggregate version; there is no line version. |

## Shared Responses

### ReceivingOrderListItem

| Field | Type |
|---|---|
| `Id` | `Guid` |
| `OrderVersion` | `string` Base64 |
| `Number` | `string` |
| `Status` | `string` |
| `CreatedAtUtc` | `DateTimeOffset` |
| `UpdatedAtUtc` | `DateTimeOffset?` |
| `StartedAtUtc` | `DateTimeOffset?` |
| `CompletedAtUtc` | `DateTimeOffset?` |
| `InventoryTransactionId` | `Guid?` |
| `Warehouse` | `{ Id, Code, Name }` |
| `ReceivingLocation` | `{ Id, Code, Name }` |
| `LineCount` | `int` |
| `TotalPlannedQuantity` | `decimal` |
| `TotalReceivedQuantity` | `decimal` |
| `TotalRemainingQuantity` | `decimal` |

### ReceivingOrderDetails

Contains the same identity, version, header, status, timestamps, summaries, totals, and transaction reference as the list item plus ordered `IReadOnlyList<ReceivingOrderLineDetails>`.

### ReceivingOrderLineDetails

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Stable retained identity. |
| `Sku` | `{ Id, Code, Name, BaseUom { Id, Code, Symbol } }` | Display and base-unit context. |
| `PlannedQuantity` | `decimal` | Base-unit plan. |
| `ReceivedQuantity` | `decimal` | Accumulated base-unit receipt. |
| `RemainingQuantity` | `decimal` | Derived planned minus received. |

No line concurrency version is exposed.

### ReceivingOrderStatusDetails

Expose canonical public constants:

```text
Draft
InProgress
Completed
```

### ReceivingOrderSortBy

Supported values:

```text
Number
Status
WarehouseCode
CreatedAtUtc
StartedAtUtc
CompletedAtUtc
TotalPlannedQuantity
```

Unknown status or sort values produce validation failure. Every sort adds a deterministic ID tie-breaker.

## List Semantics

- Query with `AsNoTracking`.
- Search trims input and matches Number.
- Filters combine Warehouse and Status.
- Count matching rows before paging.
- Use existing normalized Skip/Take limits and `ListResult<T>`.
- Default order: newest CreatedAtUtc first, then ID.
- Projections calculate line count and totals without tracking the aggregate.

## Receiving Location Lookup Contract

No Receiving-specific lookup endpoint is added. The WebApp uses the existing topology route for the selected Warehouse with:

```text
SelectableOnly = true
StorageLocationTypeCode = RECEIVING
SearchText = current user text
Take = existing lookup limit
```

This lookup filters choices for UX only. Create, Update Draft, and Start independently validate the active Warehouse, active StorageLocation, warehouse ownership, active status, active exact `RECEIVING` type, and existing inventory eligibility.

## Success and Idempotency Semantics

- Create/Update/Receive return the newly loaded current details after save.
- Start on Draft with current version mutates once; Start on InProgress returns current details; Start on Completed is conflict.
- Complete on fully received InProgress with current version posts once; Complete on a valid Completed order returns current details.
- A losing concurrent Complete reloads after its failed save. It returns `200` with the winner's current Completed result when the completed invariant exists; otherwise it returns `409` and never retries posting.
- Delete succeeds only for a current Draft with no inventory effect; it returns `204` and releases Number.

## Error Contract

All failures use the existing Problem Details format with `code` and optional `property` extensions.

| HTTP | Cases |
|---:|---|
| 400 | Malformed/missing IDs or version, empty plan, invalid quantities, duplicate submitted line IDs/SKUs, inactive or ineligible Warehouse/location/SKU, unsupported list filter/sort. |
| 404 | Receiving Order, line, Warehouse, StorageLocation, or SKU does not exist. |
| 409 | Stale aggregate version, invalid lifecycle action, over-receipt, incomplete completion, duplicate Number, database duplicate order/SKU, inventory rowversion or missing-balance race, forbidden non-Draft deletion. |

Stable feature error families should include:

```text
ReceivingOrder.ConcurrencyConflict
ReceivingOrder.InvalidState
ReceivingOrder.InventoryPostingConflict
ReceivingOrder.NumberConflict
ReceivingOrder.ReceivingLocationInvalid
ReceivingOrderLine.DuplicateSku
ReceivingOrderLine.ForeignLine
ReceivingOrderLine.OverReceipt
```

Exact message wording follows current WMS conventions; callers branch on HTTP status and stable code rather than text.
