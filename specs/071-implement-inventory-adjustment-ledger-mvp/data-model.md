# Data Model: Inventory Adjustment Ledger MVP

## InventoryBalance

**Purpose**: Current materialized quantity for one SKU at one storage location.

**Existing fields**:

- `Id`: unique identifier.
- `StockKeepingUnitId`: required reference to SKU.
- `StorageLocationId`: required reference to storage location.
- `Quantity`: non-negative decimal, precision `18,4`.
- `CreatedAtUtc`: required timestamp.
- `UpdatedAtUtc`: nullable timestamp.

**New field**:

- `RowVersion`: required `byte[]` SQL Server rowversion concurrency token.

**Transport mapping**:

- EF queries read `RowVersion` as `byte[]`.
- Base64 conversion happens only after EF materialization.
- List queries use a bounded internal projection model or equivalent local mapping; they must not load complete entity graphs just to encode `BalanceVersion`.
- Adjustment responses may map the tracked/materialized balance entity to `InventoryBalanceDetails` in memory.
- Transport encoding is not part of the database projection.

**Relationships**:

- Required `StockKeepingUnit` relationship with restrict delete.
- Required `StorageLocation` relationship with restrict delete.

**Indexes and constraints**:

- Unique `(StockKeepingUnitId, StorageLocationId)`.
- Existing index on `StorageLocationId`.

**Validation rules**:

- Quantity must be non-negative.
- Existing adjustment requires matching expected rowversion.
- Existing no-op must not call mutation behavior that touches timestamp or rowversion.

**State transitions**:

```text
Existing balance + material adjustment -> same SKU/location with new quantity, timestamp, rowversion
Existing balance + counted quantity equal current quantity -> unchanged balance
Missing balance + counted quantity > 0 + expected absence -> new balance plus ledger
Missing balance + counted quantity = 0 + expected absence -> new zero balance, no ledger
```

## InventoryTransaction

**Purpose**: Immutable aggregate root for one completed inventory operation.

**Fields**:

- `Id`: unique identifier.
- `TransactionType`: required value; MVP supports only `Adjustment`.
- `Reason`: required trimmed text, maximum length 500.
- `OccurredAtUtc`: required UTC timestamp for when the adjustment occurred.
- `CreatedAtUtc`: required creation timestamp.
- No normal `UpdatedAtUtc` lifecycle.
- `UpdatedAtUtc`: only present if forced by the current shared base-class design; it remains null and must never be touched.

**Relationships**:

- Owns one or more `InventoryLedgerEntry` children.
- MVP material adjustment creates exactly one entry.

**Validation rules**:

- Transaction type must be `Adjustment`.
- Reason must be trimmed, non-empty, and 500 characters or fewer.
- Material adjustment requires one ledger entry.
- No update or delete behavior is exposed.

**State transitions**:

```text
Create material adjustment transaction -> persisted immutable transaction
Incorrect transaction -> corrected by a new transaction
```

## InventoryLedgerEntry

**Purpose**: Immutable quantity-change record within an inventory transaction.

**Fields**:

- `Id`: unique identifier.
- `InventoryTransactionId`: required parent transaction reference.
- `StockKeepingUnitId`: required SKU reference.
- `StorageLocationId`: required storage-location reference.
- `QuantityDelta`: required decimal, precision `18,4`.
- `BalanceBefore`: required decimal, precision `18,4`.
- `BalanceAfter`: required decimal, precision `18,4`.
- No independent business occurrence timestamp; the parent transaction's `OccurredAtUtc` owns operation time.
- `CreatedAtUtc`: included only if unavoidable because of the current shared base-class design.
- `UpdatedAtUtc`: only present if forced by the current shared base-class design; it remains null and must never be touched.

**Relationships**:

- Required parent `InventoryTransaction`.
- Required `StockKeepingUnit` relationship with restrict delete.
- Required `StorageLocation` relationship with restrict delete.

**Indexes**:

- `InventoryTransactionId`.
- `StockKeepingUnitId`.
- `StorageLocationId`.

**Validation rules**:

- `BalanceAfter = BalanceBefore + QuantityDelta`.
- Entry quantity and identity fields cannot change after creation.
- Zero-delta entries are not created by this MVP.
- Immutability is enforced through approved factories, private/internal state mutation, no update/delete application flows or endpoints, and correction through a new transaction.
- Automated tests protect invariants and observable lifecycle behavior, not reflection/member absence or property-setter visibility.

## AdjustInventoryBalanceRequest

**Purpose**: Public API request for both existing-balance adjustment and missing-balance initialization.

**Fields**:

- `StockKeepingUnitId`: required non-empty GUID.
- `StorageLocationId`: required non-empty GUID.
- `CountedQuantity`: required non-negative decimal.
- `Reason`: required trimmed text, maximum length 500.
- `ExpectedBalanceVersion`: nullable string.

**Rules**:

- Non-null `ExpectedBalanceVersion` is Base64 rowversion and means the client expects an existing balance.
- Null `ExpectedBalanceVersion` means the client expects no balance row.
- Invalid Base64 is a validation error.

## InventoryBalanceDetails

**Purpose**: Public response shape for current balance list/get/adjustment results.

**Existing fields**:

- `Id`
- `Quantity`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `Sku`
- `StorageLocation`

**New field**:

- `BalanceVersion`: required Base64 representation of `InventoryBalance.RowVersion`.

**Rules**:

- Clients use `BalanceVersion` as `ExpectedBalanceVersion` when adjusting an existing balance.
- Missing-balance initialization does not have a prior balance version and submits null.

## Error Model

**InventoryBalance.ConcurrencyConflict**

Returned as HTTP 409 ProblemDetails when:

- Existing balance version differs from expected version.
- Client expected absence but balance exists.
- Client expected existing balance but no balance exists.
- EF Core save detects rowversion concurrency conflict.
- SQL Server duplicate insert occurs for the SKU/location unique index during expected-absence adjustment.

The adjustment slice owns the business classification of a duplicate insert as `InventoryBalance.ConcurrencyConflict`. Low-level persistence code may expose a predicate for SQL Server error 2601/2627 and the named unique index, but duplicate Inventory Balance insertions must not be globally reclassified as concurrency conflicts.

After a concurrency exception or adjustment duplicate-insert failure, the handler returns conflict immediately. It does not retry `SaveChangesAsync`, does not reuse the failed tracked graph for automatic retry, and does not automatically retry the absolute counted-quantity adjustment.

**Validation errors**

Normally returned as HTTP 400 ProblemDetails for:

- Missing or empty SKU/location identifiers.
- Negative counted quantity.
- Missing, whitespace-only, or over-500-character reason.
- Invalid Base64 expected version.

**Not-found errors**

Normally returned as HTTP 404 ProblemDetails for:

- Missing SKU.
- Missing storage location.
- Missing required related record used by the current create eligibility rules.

**Eligibility errors**

Existing but inactive or otherwise ineligible references during missing-balance initialization reuse the current create-handler validation/conflict convention. The adjustment implementation must not invent one generic 400 response for every eligibility failure.

## Persistence Mapping Summary

- Add `row_version` rowversion to `inventory_balances`.
- Add `inventory_transactions`.
- Add `inventory_ledger_entries`.
- Add transaction, SKU, storage-location, and occurrence-time indexes listed in `plan.md`.
- Use the EF convention-generated foreign-key index for `InventoryLedgerEntry.InventoryTransactionId` when present; do not add a duplicate explicit index for the same relationship.
- Keep zero-quantity balance rows.
- Do not add `InventoryAccount`, Transfer, LPN, zero-row deletion, event sourcing, or history UI data structures.

## Future Architecture Follow-Up

Move `CreatedAtUtc` and `UpdatedAtUtc` out of `EntityBase` into explicit capability interfaces or entity-specific models. This refactoring is out of scope for feature #71.
