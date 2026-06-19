# Contract: Inventory Ledger API

## Overview

Inventory Ledger API behavior is read-only. It exposes a server-driven ledger-entry list and transaction details. It does not expose ledger mutation, correction, reversal, transfer, export, analytics, or rebuild behavior.

## Routes

```text
GET /api/wms/inventory/ledger
GET /api/wms/inventory/transactions/{transactionId:guid}
```

## GET /api/wms/inventory/ledger

Lists Inventory Ledger entries with parent transaction context.

### Query Parameters

| Name | Type | Required | Semantics |
|------|------|----------|-----------|
| `skip` | integer | no | Number of filtered rows to skip after sorting. Normalize with existing list rules. |
| `take` | integer | no | Page size. Normalize with existing list rules. |
| `sortBy` | string | no | One supported Inventory Ledger sort key. |
| `sortDescending` | boolean | no | Direction for the requested primary sort. |
| `stockKeepingUnitId` | GUID | no | Exact SKU identity filter. |
| `warehouseId` | GUID | no | Warehouse filter through the entry storage location. |
| `storageLocationId` | GUID | no | Exact storage-location identity filter. |
| `transactionType` | string | no | Exact transaction type filter. Current supported value is `Adjustment`. |
| `occurredFromUtc` | DateTimeOffset | no | Inclusive UTC lower bound for `OccurredAtUtc`. |
| `occurredToUtc` | DateTimeOffset | no | Exclusive UTC upper bound for `OccurredAtUtc`. |

### Supported Sort Keys

```text
OccurredAtUtc
TransactionType
SkuCode
SkuName
WarehouseCode
WarehouseName
StorageLocationCode
BalanceBefore
QuantityDelta
BalanceAfter
Reason
```

### Default Sort

```text
OccurredAtUtc descending
then InventoryTransactionId descending
then InventoryLedgerEntry.Id descending
```

Requested sort behavior:

```text
requested primary sort in requested direction
then InventoryTransactionId ascending
then InventoryLedgerEntryId ascending
```

### Successful Response

Returns `ListResult<InventoryLedgerEntryDetails>`.

```json
{
  "items": [
    {
      "entryId": "018f0000-0000-7000-8000-000000000501",
      "transactionId": "018f0000-0000-7000-8000-000000000401",
      "transactionType": "Adjustment",
      "reason": "Cycle count correction",
      "occurredAtUtc": "2026-06-18T09:30:00+00:00",
      "balanceBefore": 10,
      "quantityDelta": -3,
      "balanceAfter": 7,
      "sku": {
        "id": "018f0000-0000-7000-8000-000000000101",
        "code": "SKU-001",
        "name": "Widget",
        "baseUom": {
          "id": "018f0000-0000-7000-8000-000000000111",
          "code": "EA",
          "symbol": "ea"
        }
      },
      "storageLocation": {
        "id": "018f0000-0000-7000-8000-000000000201",
        "code": "A-01-01",
        "name": "A-01-01",
        "warehouse": {
          "id": "018f0000-0000-7000-8000-000000000301",
          "code": "MAIN",
          "name": "Main Warehouse"
        }
      }
    }
  ],
  "totalCount": 1,
  "skip": 0,
  "take": 20
}
```

### Empty Result

When no entries match filters:

```json
{
  "items": [],
  "totalCount": 0,
  "skip": 0,
  "take": 20
}
```

Do not return NotFound for an empty list.

### Validation and Errors

- Malformed GUID and date/time query values use normal endpoint binding behavior.
- Unsupported transaction type returns validation ProblemDetails.
- `occurredFromUtc > occurredToUtc` returns validation ProblemDetails.
- `occurredFromUtc == occurredToUtc` is valid and returns an empty interval.
- Unexpected query failures use existing Myrmex failure behavior.

## GET /api/wms/inventory/transactions/{transactionId:guid}

Loads one inventory transaction and all ledger entries belonging to it.

### Route Parameters

| Name | Type | Required | Semantics |
|------|------|----------|-----------|
| `transactionId` | GUID | yes | Inventory transaction identifier. |

### Successful Response

Returns `InventoryTransactionDetails`.

```json
{
  "id": "018f0000-0000-7000-8000-000000000401",
  "transactionType": "Adjustment",
  "reason": "Cycle count correction",
  "occurredAtUtc": "2026-06-18T09:30:00+00:00",
  "createdAtUtc": "2026-06-18T09:30:01+00:00",
  "entries": [
    {
      "entryId": "018f0000-0000-7000-8000-000000000501",
      "balanceBefore": 10,
      "quantityDelta": -3,
      "balanceAfter": 7,
      "sku": {
        "id": "018f0000-0000-7000-8000-000000000101",
        "code": "SKU-001",
        "name": "Widget",
        "baseUom": {
          "id": "018f0000-0000-7000-8000-000000000111",
          "code": "EA",
          "symbol": "ea"
        }
      },
      "storageLocation": {
        "id": "018f0000-0000-7000-8000-000000000201",
        "code": "A-01-01",
        "name": "A-01-01",
        "warehouse": {
          "id": "018f0000-0000-7000-8000-000000000301",
          "code": "MAIN",
          "name": "Main Warehouse"
        }
      }
    }
  ]
}
```

### Not Found

Missing transaction returns NotFound ProblemDetails using current Myrmex conventions.

### Detail Entry Shape

Entries inside `InventoryTransactionDetails` use `InventoryTransactionEntryDetails`, not the full list-row `InventoryLedgerEntryDetails`. Detail entries contain only entry-owned values and reference context:

- `entryId`
- `balanceBefore`
- `quantityDelta`
- `balanceAfter`
- `sku` with base UoM
- `storageLocation` with warehouse

Transaction ID, type, reason, occurrence time, and transaction creation time belong to the transaction header and are not repeated in every detail entry.

## API Client Responsibilities

Add methods to `WmsInventoryApiClient`:

```text
ListInventoryLedgerEntriesAsync(ListInventoryLedgerEntriesRequest request, CancellationToken cancellationToken = default)
GetInventoryTransactionByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
```

Client requirements:

- Build `/api/wms/inventory/ledger` query strings from non-empty request values.
- Omit trailing `?` when no query parameters are present.
- Encode string query parameter values.
- Include all supported filter, sort, paging, and occurrence range parameters.
- Propagate cancellation.
- Deserialize nested list and details DTOs.
- Use existing required-read and ProblemDetails behavior.
