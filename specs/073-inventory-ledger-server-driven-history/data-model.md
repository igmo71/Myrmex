# Data Model: Inventory Ledger Server-Driven History

## Overview

This feature exposes existing Inventory Adjustment Ledger data through read-only projections. It does not introduce new domain entities, persistence mappings, migrations, or lifecycle transitions.

## Existing Domain Entities

### InventoryTransaction

Represents one completed inventory operation.

**Current key attributes used by this feature**:

- `Id`: transaction identifier.
- `TransactionType`: transaction type. Current MVP value is `Adjustment`.
- `Reason`: required free-text reason captured by the write model.
- `OccurredAtUtc`: business occurrence timestamp in UTC.
- `CreatedAtUtc`: persistence creation timestamp.
- `Entries`: immutable ledger entries belonging to the transaction.

**Relationships**:

- One `InventoryTransaction` has one or more `InventoryLedgerEntry` records.

**Read behavior**:

- Listed indirectly through ledger entry rows.
- Loaded directly by transaction details.
- Details must support multiple entries.

**State transitions**:

- None in this feature. Ledger transactions are read-only.

### InventoryLedgerEntry

Represents one immutable inventory quantity change within a transaction.

**Current key attributes used by this feature**:

- `Id`: ledger entry identifier.
- `InventoryTransactionId`: parent transaction identifier.
- `StockKeepingUnitId`: SKU affected by the entry.
- `StorageLocationId`: storage location affected by the entry.
- `BalanceBefore`: persisted quantity before the operation.
- `QuantityDelta`: persisted signed quantity change.
- `BalanceAfter`: persisted quantity after the operation.
- `CreatedAtUtc`: entry persistence creation timestamp if present from the shared entity base. This feature does not expose it in list rows.

**Relationships**:

- Belongs to one `InventoryTransaction`.
- References one `StockKeepingUnit`.
- References one `StorageLocation`.
- Storage location supplies warehouse context.
- SKU supplies base UoM context.

**Validation and invariants**:

- `BalanceAfter = BalanceBefore + QuantityDelta`.
- `BalanceBefore` and `BalanceAfter` are non-negative.
- Zero-delta entries are not expected from the adjustment write model.

**Read behavior**:

- Primary Ledger list row is one `InventoryLedgerEntry` plus parent transaction and reference context.
- List row does not depend on current `InventoryBalance`.

**State transitions**:

- None in this feature. Ledger entries are read-only.

### InventoryBalance

Represents current materialized quantity for one SKU at one storage location.

**Current key attributes used by this feature**:

- `StockKeepingUnitId`: source for balance-to-history SKU filter.
- `StorageLocationId`: source for balance-to-history storage-location filter.

**Relationships**:

- References SKU and storage location for current stock context.

**Read behavior**:

- Provides navigation context only.
- Ledger history must remain available if a current balance row is absent.

**State transitions**:

- None in this feature.

## Reference Entities

### StockKeepingUnit

Used for list/detail display and SKU filtering.

**Projected attributes**:

- `Id`
- `Code`
- `Name`
- Base UoM ID, code, and symbol
- Active state may be surfaced through existing lookup items where useful.

**Historical behavior**:

- Inactive SKUs must remain searchable and visible when they have ledger history.
- Current labels are displayed; historical label snapshots are not introduced.

### UnitOfMeasure

Used as SKU base UoM display context in transaction details and list rows where applicable.

**Projected attributes**:

- `Id`
- `Code`
- `Symbol`

**Historical behavior**:

- Inactive UoMs referenced through SKU base UoM context must remain visible when history exists.
- Current labels are displayed; historical label snapshots are not introduced.

### Warehouse

Used for list/detail display and warehouse filtering through storage-location context.

**Projected attributes**:

- `Id`
- `Code`
- `Name`
- Active state may be surfaced through existing list details where useful.

**Historical behavior**:

- Inactive warehouses must remain selectable/searchable for history filtering and visible in history rows.
- Current labels are displayed; historical label snapshots are not introduced.

### StorageLocation

Used for list/detail display and storage-location filtering.

**Projected attributes**:

- `Id`
- `WarehouseId`
- `Code`
- `Name`
- Active state may be surfaced through existing lookup items where useful.

**Historical behavior**:

- Inactive storage locations must remain searchable and visible when they have ledger history.
- Lookup is warehouse-scoped when a warehouse is selected.
- Current labels are displayed; historical label snapshots are not introduced.

## Public Read Models

### InventoryLedgerEntryDetails

Represents one ledger entry row enriched with transaction context.

**Fields**:

- `EntryId`
- `TransactionId`
- `TransactionType`
- `Reason`
- `OccurredAtUtc`
- `BalanceBefore`
- `QuantityDelta`
- `BalanceAfter`
- `Sku`
  - `Id`
  - `Code`
  - `Name`
  - `BaseUom`
    - `Id`
    - `Code`
    - `Symbol`
- `StorageLocation`
  - `Id`
  - `Code`
  - `Name`
  - `Warehouse`
    - `Id`
    - `Code`
    - `Name`

### InventoryTransactionDetails

Represents one transaction and all of its entries.

**Fields**:

- `Id`
- `TransactionType`
- `Reason`
- `OccurredAtUtc`
- `CreatedAtUtc`
- `Entries`: ordered collection of `InventoryTransactionEntryDetails` for that transaction.

### InventoryTransactionEntryDetails

Represents one ledger entry inside transaction details. It contains entry-owned values and reference context only; transaction ID, transaction type, reason, occurrence time, and transaction creation time are represented once on the transaction header.

Planned shared contract file:

```text
Myrmex.Shared/Wms/Inventory/InventoryTransactionEntryDetails.cs
```

**Fields**:

- `EntryId`
- `BalanceBefore`
- `QuantityDelta`
- `BalanceAfter`
- `Sku`
  - `Id`
  - `Code`
  - `Name`
  - `BaseUom`
    - `Id`
    - `Code`
    - `Symbol`
- `StorageLocation`
  - `Id`
  - `Code`
  - `Name`
  - `Warehouse`
    - `Id`
    - `Code`
    - `Name`

## Query Filters

### ListInventoryLedgerEntriesRequest

**Paging and sorting**:

- `Skip`
- `Take`
- `SortBy`
- `SortDescending`

**Filters**:

- `StockKeepingUnitId`: exact entry SKU identity.
- `WarehouseId`: entry storage location's warehouse identity.
- `StorageLocationId`: exact entry storage-location identity.
- `TransactionType`: exact transaction type string, currently `Adjustment`.
- `OccurredFromUtc`: inclusive lower UTC boundary.
- `OccurredToUtc`: exclusive upper UTC boundary.

**Validation**:

- Validate the public request before constructing the filtered EF query.
- Normalize `Skip` and `Take` using existing list normalization.
- Reject unsupported transaction type values.
- Reject occurrence range where `OccurredFromUtc > OccurredToUtc`.
- Treat `OccurredFromUtc == OccurredToUtc` as a valid empty interval.
- Unsupported sort keys follow current Inventory Balance deterministic fallback behavior.

**Backend list query sequence**:

```text
validate request
-> normalize paging
-> create base AsNoTracking query
-> apply filters
-> CountAsync
-> deterministic sorting
-> Skip / Take
-> bounded projection
-> materialize
-> ListResult
```

Unsupported transaction-type values and invalid occurrence ranges must not participate in SQL query construction.

## Sorting Model

Supported sort keys:

- `OccurredAtUtc`
- `TransactionType`
- `SkuCode`
- `SkuName`
- `WarehouseCode`
- `WarehouseName`
- `StorageLocationCode`
- `BalanceBefore`
- `QuantityDelta`
- `BalanceAfter`
- `Reason`

`InventoryLedgerSortBy` public constant values preserve the existing `InventoryBalanceSortBy` convention and remain PascalCase:

```csharp
public const string OccurredAtUtc = "OccurredAtUtc";
public const string TransactionType = "TransactionType";
public const string SkuCode = "SkuCode";
```

Default sort:

```text
OccurredAtUtc descending
then InventoryTransactionId descending
then InventoryLedgerEntry.Id descending
```

Requested sort:

```text
requested primary sort in requested direction
then InventoryTransactionId ascending
then InventoryLedgerEntryId ascending
```

Default sort uses descending tie-breakers. Requested sorts use ascending tie-breakers, matching the stable secondary ordering style used by the Inventory Balance reference pattern.

## Persistence and Index Notes

Existing schema already has:

- `InventoryTransaction.OccurredAtUtc` index.
- `InventoryLedgerEntry.InventoryTransactionId` foreign-key index.
- `InventoryLedgerEntry.StockKeepingUnitId` index.
- `InventoryLedgerEntry.StorageLocationId` index.

No new index or migration is part of this plan. Implementation should inspect actual query shapes before proposing any combined index in a later migration.
