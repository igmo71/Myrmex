# Contract: Inventory Adjustment API

## Endpoint

```text
POST /api/wms/inventory/adjustments
```

Business command endpoint for both:

- Existing-balance adjustment from a loaded balance row.
- Missing-balance initialization from expected zero.

No other stock-mutation endpoint remains for direct inventory balance create or direct quantity update.

## Request

```json
{
  "stockKeepingUnitId": "018f0000-0000-7000-8000-000000000101",
  "storageLocationId": "018f0000-0000-7000-8000-000000000201",
  "countedQuantity": 7.0,
  "reason": "Cycle count correction",
  "expectedBalanceVersion": "AAAAAAAAB9E="
}
```

### Field Rules

| Field | Required | Rules |
|-------|----------|-------|
| `stockKeepingUnitId` | Yes | Non-empty GUID |
| `storageLocationId` | Yes | Non-empty GUID |
| `countedQuantity` | Yes | Decimal, greater than or equal to zero |
| `reason` | Yes | Trimmed, non-empty, maximum 500 characters |
| `expectedBalanceVersion` | No | Base64 SQL Server rowversion when existing balance is expected; null when no balance is expected. Non-null values must decode to exactly 8 bytes. |

## Existing-Balance Adjustment

The client submits the row's current `balanceVersion` as `expectedBalanceVersion`.

```json
{
  "stockKeepingUnitId": "018f0000-0000-7000-8000-000000000101",
  "storageLocationId": "018f0000-0000-7000-8000-000000000201",
  "countedQuantity": 7.0,
  "reason": "Shelf count correction",
  "expectedBalanceVersion": "AAAAAAAAB9E="
}
```

Expected success:

- If counted quantity differs from current quantity, update balance and create one transaction plus one ledger entry.
- If counted quantity equals current quantity, return success without balance update or ledger records.

## Missing-Balance Initialization

The client submits `expectedBalanceVersion = null`.

```json
{
  "stockKeepingUnitId": "018f0000-0000-7000-8000-000000000101",
  "storageLocationId": "018f0000-0000-7000-8000-000000000201",
  "countedQuantity": 5.0,
  "reason": "Initial physical count",
  "expectedBalanceVersion": null
}
```

Expected success:

- Counted quantity greater than zero creates balance and ledger from zero.
- Counted quantity equal to zero creates persisted zero balance and no ledger records.

## Successful Response

Response status: `200 OK`

Body: `InventoryBalanceDetails`.

```json
{
  "id": "018f0000-0000-7000-8000-000000000001",
  "quantity": 7.0,
  "createdAtUtc": "2026-06-18T10:00:00+00:00",
  "updatedAtUtc": "2026-06-18T10:15:00+00:00",
  "balanceVersion": "AAAAAAAAB9I=",
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
```

For existing-balance no-op, `balanceVersion` is unchanged.

## Validation Failure

Response status: `400 Bad Request`

Used for:

- Missing or empty identifiers.
- Negative counted quantity.
- Missing, whitespace-only, or over-500-character reason.
- Invalid Base64 expected version.
- Valid Base64 expected version values that do not decode to exactly 8 bytes.

ProblemDetails follows existing Myrmex validation conventions.

## Not Found

Response status: `404 Not Found`

Used for missing SKU, missing storage location, or a missing required related record when current Myrmex behavior reports the condition as NotFound.

## Missing-Balance Eligibility Failure

Existing but inactive or otherwise ineligible references during missing-balance initialization reuse the current create-handler validation/conflict convention.

The adjustment API must not collapse every eligibility failure into one generic 400 response.

## Concurrency Conflict

Response status: `409 Conflict`

ProblemDetails extension:

```json
{
  "code": "InventoryBalance.ConcurrencyConflict"
}
```

Used for:

- Existing balance version mismatch.
- Expected absence but balance exists.
- Expected existing balance but no balance exists.
- EF Core rowversion concurrency exception.
- Duplicate insert on the inventory-balance SKU/location unique index during expected-absence initialization.

Client behavior:

- Do not automatically retry.
- Ask user to refresh and review the counted quantity.

Server behavior after failed save:

- Return conflict immediately after `DbUpdateConcurrencyException` or adjustment duplicate-insert failure.
- Do not retry `SaveChangesAsync`.
- Do not reuse the failed tracked graph for automatic retry.

Duplicate-insert classification:

- Low-level persistence code may detect SQL Server error 2601 or 2627 and the named SKU/storage-location unique index.
- The adjustment slice owns the business decision to map that duplicate insert to `InventoryBalance.ConcurrencyConflict`.
- Do not globally reclassify all duplicate Inventory Balance insertions as concurrency conflicts.

## Removed Mutation Contracts

The following public stock-mutation contracts are removed from API-client/UI usage:

- `POST /api/wms/inventory/balances`
- `PUT /api/wms/inventory/balances/{inventoryBalanceId}/quantity`
- `CreateInventoryBalanceRequest`
- `UpdateInventoryBalanceQuantityRequest`
