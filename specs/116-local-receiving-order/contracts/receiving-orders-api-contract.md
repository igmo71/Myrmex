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
| PUT | `/api/wms/receiving-orders/{id}` | `UpdateReceivingOrderDraftRequest` | `200 ReceivingOrderDetails` |
| DELETE | `/api/wms/receiving-orders/{id}?expectedOrderVersion=...` | URL-encoded Base64 aggregate version | `204 No Content` |
| POST | `/api/wms/receiving-orders/{id}/start` | `ReceivingOrderActionRequest` | `200 ReceivingOrderDetails` |
| POST | `/api/wms/receiving-orders/{id}/lines/{lineId}/receive` | `ReceiveReceivingOrderLineRequest` | `200 ReceivingOrderDetails` |
| POST | `/api/wms/receiving-orders/{id}/complete` | `ReceivingOrderActionRequest` | `200 ReceivingOrderDetails` |

DELETE uses a query-string version to follow the existing versioned DELETE convention. The WebApp client must URL-encode Base64 values.

Create intentionally returns `200 OK`: current WMS create endpoints use the shared generic `ServiceResult<T>.ToHttpResult()` mapping, which returns 200 for successful payloads and has no Created/location variant. A Receiving-only `201 Created` response would be inconsistent with that established project convention.

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

### UpdateReceivingOrderDraftRequest

| Field | Type | Rules |
|---|---|---|
| `Number` | `string?` | Required replacement Number. |
| `WarehouseId` | `Guid?` | Required replacement Warehouse. |
| `ReceivingLocationId` | `Guid?` | Required replacement eligible Receiving location. |
| `ExpectedOrderVersion` | `string?` | Required Base64 eight-byte aggregate rowversion. |
| `Lines` | `IReadOnlyList<UpdateReceivingOrderLineRequest>` | Required complete replacement plan. |

`UpdateReceivingOrderLineRequest` contains nullable `LineId`, nullable `StockKeepingUnitId`, and `PlannedQuantity`:

- non-null LineId: retain and update that existing order line;
- a retained Draft line may change SKU while preserving its LineId;
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

`ReceivingOrderStatusDetails` is retained because the existing shared WMS constant containers are named `InventoryCountStatusDetails` and `InventoryTransferStatusDetails`. `TotalPlannedQuantity` is retained because the comparable Inventory Transfer list deliberately supports equivalent aggregate-total sorts; it does not imply a dedicated index.

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
StorageLocationTypeCode = StorageLocationTypeCodes.Receiving
SearchText = current user text
Take = existing lookup limit
```

`StorageLocationTypeCodes.Receiving` is the one Topology-owned public constant for the persisted `RECEIVING` code. The lookup and backend share one authoritative rule: active Warehouse; active StorageLocation belonging to it; active Receiving StorageLocationType; and current StorageLocationStatus/other conditions accepted by existing inventory/selectability eligibility. Create, Update Draft, and Start use the same backend eligibility orchestration. Receiving-specific validation does not duplicate active status checks already performed by the reused authoritative eligibility logic.

## Success and Idempotency Semantics

- Create/Update/Receive return the newly loaded current details after save.
- Start on Draft with current version mutates once; Start on InProgress returns current details; Start on Completed is conflict.
- Complete on fully received InProgress with current version posts once; Complete on a valid Completed order returns current details.
- A losing concurrent Complete reloads the order and all lines after its failed save. It returns `200` with the winner's result only when Status is Completed, StartedAtUtc/CompletedAtUtc/InventoryTransactionId are all present, and every line is fully received. It returns `409` when the order is not Completed, returns an invalid-persisted-state failure when Status claims Completed but that invariant is incomplete, and never retries posting.
- Delete succeeds only for a current Draft with no inventory effect; it returns `204` and releases Number.

## Error Contract

All failures use the existing Problem Details format with `code` and optional `property` extensions.

| HTTP | Cases |
|---:|---|
| 400 | Malformed/missing IDs or version, empty plan, invalid quantities, duplicate submitted line IDs/SKUs, inactive or ineligible Warehouse/location/SKU, unsupported list filter/sort. |
| 404 | Receiving Order, line, Warehouse, StorageLocation, or SKU does not exist. |
| 409 | Stale aggregate version, invalid lifecycle action, over-receipt, incomplete completion, duplicate Number, database duplicate order/SKU, inventory rowversion or missing-balance race, forbidden non-Draft deletion. |
| 500 | Detected invalid persisted aggregate state, including a row marked Completed without the complete persisted Completed invariant or a Draft carrying received quantity during deletion. |

Stable feature error families should include:

```text
ReceivingOrder.ConcurrencyConflict
ReceivingOrder.InvalidState
ReceivingOrder.InventoryPostingConflict
ReceivingOrder.InvalidPersistedState
ReceivingOrder.NumberConflict
ReceivingOrder.ReceivingLocationInvalid
ReceivingOrderLine.DuplicateSku
ReceivingOrderLine.ForeignLine
ReceivingOrderLine.OverReceipt
```

Exact message wording follows current WMS conventions; callers branch on HTTP status and stable code rather than text.

## Deterministic Reference and Decimal Validation

For a request containing multiple invalid references, fail fast in this stable order: request/version shape; target order existence and current lifecycle/version when applicable; submitted LineId/plan structure in request order; Warehouse; ReceivingLocation; SKUs/base UOMs by first occurrence in the submitted line list; remaining aggregate rules; persistence constraints. Create omits the target-order step. Set-based reads may be unordered, but failure selection must walk the original request order.

Every planned quantity, receipt increment, accumulated received quantity, balance before/after, transaction delta, and calculated balance-after value must fit SQL Server `decimal(18,4)` before SaveChanges. Use the shared static WMS-domain `WmsQuantityPersistence` convention rather than Receiving-specific limits. No weight fields or calculations are part of these contracts.

## Transaction Traceability

`ReceivingOrderDetails.InventoryTransactionId` is the authoritative direct link. Completion creates the transaction with the stable non-localized reason `ReceivingOrder {ReceivingOrderId:D} Number {NormalizedNumber}`. No generic source-document fields or Inventory-owned Receiving reference are exposed. Future reverse navigation may be a joined query/read-model composition.
