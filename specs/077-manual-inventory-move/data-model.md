# Data Model: Manual Inventory Move

## Manual Inventory Move

One completed ad-hoc relocation of SKU quantity between two regular locations in the same warehouse. This is an operation, not a persisted entity.

### Inputs

- `StockKeepingUnitId`: required active SKU.
- `SourceStorageLocationId`: required source.
- `DestinationStorageLocationId`: required destination.
- `Quantity`: positive decimal using existing inventory precision.
- `Reason`: trimmed, required, bounded by `InventoryTransaction.ReasonMaxLength`.
- `ExpectedSourceBalanceVersion`: required Base64 SQL Server rowversion.

### Invariants

- Source and destination differ.
- Source balance exists, its version matches, and its quantity is sufficient.
- SKU is active.
- Both locations exist, are active, have active type/status, belong to the same warehouse, and are not `INTERNAL_TRANSIT` or `EXTERNAL_TRANSIT`.
- Balance and ledger effects persist atomically.

## InventoryBalance

Existing current-state snapshot with `Id`, SKU/location identity, non-negative `Quantity`, `RowVersion`, and timestamps.

### Existing constraints

- Unique `(StockKeepingUnitId, StorageLocationId)`.
- Quantity precision `18,4`.
- Required SKU/location relationships.

### Transitions

```text
Source > moved quantity -> reduced positive source with new rowversion
Source = moved quantity -> retained zero source with new rowversion
Existing destination -> increased destination with new rowversion
Missing destination -> created destination with moved quantity
```

Existing destination writes use rowversion. Missing destination races use the unique index.

## InventoryTransaction

Existing immutable aggregate:

- `TransactionType = Transfer`.
- User-supplied trimmed reason.
- One operation timestamp.
- Exactly two ledger entries.

No Inventory Transfer or Manual Move reference is added.

## InventoryLedgerEntry

Source entry:

- source location;
- `QuantityDelta = -Quantity`;
- source before/after values.

Destination entry:

- destination location;
- `QuantityDelta = Quantity`;
- destination before/after values, starting at zero when absent.

The two entries use the same SKU and transaction, have opposite equal deltas, and sum to zero.

## StorageLocation and SKU eligibility

- SKU must exist and be active.
- Base UoM activity is not a manual-move rule.
- Source/destination location, type, and status must be active.
- Locations must share a warehouse.
- Transit type codes are prohibited.
- Parent warehouse activity is not a new rule.

## Balance lookup

Identity is exact `(StockKeepingUnitId, StorageLocationId)`.

- Reuses `InventoryBalanceDetails`.
- Returns quantity/version and SKU/location/warehouse context.
- Returns existing balances even when related records are inactive.
- Returns not found only when the balance row does not exist.

## MoveInventoryBalanceRequest

- Nullable SKU/source/destination GUIDs for required validation.
- Positive `Quantity`.
- Nullable `Reason` validated as required/bounded.
- Nullable `ExpectedSourceBalanceVersion` validated as required Base64 of one 8-byte rowversion.

## MoveInventoryBalanceResult

- `SourceBalance`
- `DestinationBalance`
- `MovedQuantity`
- `SourceQuantityBefore`
- `SourceQuantityAfter`
- `DestinationQuantityBefore`
- `DestinationQuantityAfter`
- `OccurredAtUtc`

## Error model

Use this distinction consistently:

- `404 Not Found` means a requested reference or lookup target does not exist.
- `409 Conflict` means inventory state changed or cannot be safely committed based on the submitted state.

### Validation

- Malformed required values or source version.
- Non-positive quantity or invalid reason.
- Same source and destination.
- Inactive or otherwise ineligible existing references.
- Cross-warehouse or transit location.

### Not found

- Balance lookup has no exact row for the requested `skuId + storageLocationId`.
- Move request references a SKU that does not exist.
- Move request references a source storage location that does not exist.
- Move request references a destination storage location that does not exist.

### Conflict

- The SKU and source location exist, but the source balance is missing at commit time.
- The source balance version is stale.
- The source balance quantity is insufficient.
- An existing destination balance changed concurrently.
- Another request concurrently created the previously missing destination balance.

A missing destination balance before the move is valid state, not an error. The successful move creates it with a prior quantity of zero.

## Persistence impact

No tables, columns, indexes, relationships, or migration are added.
