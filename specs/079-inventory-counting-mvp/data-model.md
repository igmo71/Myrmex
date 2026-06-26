# Data Model: Inventory Counting MVP

## InventoryCount

Warehouse-level aggregate root for one physical counting session.

### Fields

- `Id`: Guid v7 stable identity.
- `WarehouseId`: required warehouse foreign key.
- `Status`: `Draft`, `InProgress`, `Completed`, or `Cancelled`.
- `Reason`: optional trimmed text, maximum 500 characters.
- `CreatedByActorId`: required stable actor string, maximum 256 characters.
- `CompletedByActorId`: nullable actor string.
- `CancelledByActorId`: nullable actor string.
- `CompletedAtUtc`: nullable UTC timestamp.
- `CancelledAtUtc`: nullable UTC timestamp.
- `RowVersion`: SQL Server rowversion.
- `CreatedAtUtc`: required UTC timestamp inherited from `EntityBase`.
- `UpdatedAtUtc`: nullable UTC timestamp inherited from `EntityBase`.
- `Lines`: collection of `InventoryCountLine`.

### Relationships

- Many counts reference one Warehouse; delete restricted.
- One count has many lines; delete restricted.

### Invariants and transitions

```text
Create -> Draft
Draft + add/remove Pending line -> Draft
Draft + first count entry -> InProgress
InProgress + further line work -> InProgress
Draft/InProgress + all current lines Applied -> Completed
Draft/InProgress + cancel -> Cancelled
Completed/Cancelled -> final and read-only
```

- Completion requires at least one current line.
- Every current line must be Applied.
- No Conflict line may remain current.
- Cancellation never reverses Applied adjustments.

## InventoryCountLine

One immutable system snapshot and physical-count result for one SKU/location pair.

### Fields

- `Id`: Guid v7 stable identity.
- `InventoryCountId`: required parent count foreign key.
- `StockKeepingUnitId`: required SKU foreign key.
- `StorageLocationId`: required location foreign key.
- `SystemQuantity`: immutable decimal snapshot, precision 18,4.
- `ExpectedBalanceVersion`: nullable 8-byte captured balance rowversion; null means balance absence was captured.
- `CountedQuantity`: nullable decimal, precision 18,4.
- `VarianceQuantity`: nullable decimal, precision 18,4; always `CountedQuantity - SystemQuantity`.
- `Status`: `Pending`, `Counted`, `Applied`, `Conflict`, or `Superseded`.
- `IsCurrent`: persisted Boolean used by the unique filtered index.
- `Comment`: nullable trimmed text, maximum 500 characters.
- `CountedByActorId`: nullable actor string, maximum 256 characters.
- `CountedAtUtc`: nullable UTC timestamp.
- `AppliedByActorId`: nullable actor string, maximum 256 characters.
- `AppliedAtUtc`: nullable UTC timestamp.
- `AppliedInventoryTransactionId`: nullable adjustment transaction foreign key.
- `SupersedesInventoryCountLineId`: nullable self-reference from replacement to prior Conflict line.
- `RowVersion`: SQL Server rowversion.
- `CreatedAtUtc`: required UTC timestamp.
- `UpdatedAtUtc`: nullable UTC timestamp.

### Relationships

- Many lines belong to one count; delete restricted.
- Many lines reference one SKU; delete restricted.
- Many lines reference one storage location; delete restricted.
- At most one line references an applied inventory transaction; delete restricted.
- A replacement line optionally references one prior line; delete restricted and unique.

### Uniqueness and indexes

- Filtered unique current-line index:

  ```text
  UNIQUE (InventoryCountId, StockKeepingUnitId, StorageLocationId)
  WHERE IsCurrent = 1
  ```

- Unique filtered index on `SupersedesInventoryCountLineId` when non-null.
- Unique filtered index on `AppliedInventoryTransactionId` when non-null.
- Supporting indexes on count ID, SKU ID, location ID, status, and applied transaction ID.

### Lifecycle

```text
Add -> Pending (IsCurrent = true)
Pending/Counted + record quantity -> Counted
Counted + successful zero/non-zero apply -> Applied
Counted + stale inventory snapshot -> Conflict
Conflict + supersede -> Superseded (IsCurrent = false)
Supersede creates new Pending replacement (IsCurrent = true)
```

- Only Pending may be physically removed.
- System quantity and expected balance version never change.
- Counted/Applied/Conflict/Superseded lines cannot be deleted.
- Applied and Superseded are final.
- Conflict is immutable except for transition to Superseded.

## InventoryBalance integration

Existing current-state entity.

### Snapshot rules

- Existing pair: capture quantity and rowversion.
- Missing pair: capture quantity zero and null expected version.

### Apply rules

- Expected existing: current row must exist with identical rowversion.
- Expected missing: no row may exist.
- Zero variance: do not change or create a balance.
- Existing non-zero variance: set quantity to counted quantity.
- Missing positive variance: create balance at counted quantity.
- Any presence/version mismatch: mark line Conflict; no inventory transaction or ledger entry.

## InventoryTransaction and InventoryLedgerEntry integration

For non-zero variance:

- Create one existing `Adjustment` transaction.
- Create exactly one ledger entry.
- `QuantityDelta = VarianceQuantity`.
- `BalanceBefore = SystemQuantity`.
- `BalanceAfter = CountedQuantity`.
- Store transaction ID on the Applied line.
- Use one save for line/count, balance, transaction, and entry.

For zero variance:

- No transaction.
- No ledger entry.
- `AppliedInventoryTransactionId = null`.

## Actor identity

Actor identifiers are opaque stable strings derived from the authenticated principal.

- Public clients never submit actor IDs.
- Creation records creator.
- Count entry records/replaces latest counter and count time.
- Apply records applier and apply time.
- Completion records completer and completion time.
- Cancellation records canceller and cancellation time.

## Public list model

`InventoryCountListItem`:

- count ID and version;
- warehouse ID/code/name;
- status;
- reason;
- created/updated/completed/cancelled times;
- creator/completer/canceller actor IDs;
- current line count;
- current Applied line count;
- current unresolved line count;
- current Conflict line count.

Superseded lines do not affect progress totals.

## Public details model

`InventoryCountDetails` includes count metadata/version and every line.

Each `InventoryCountLineDetails` includes:

- line/version/status/current flag;
- SKU ID/code/name/base UoM;
- location ID/code/name;
- system, counted, and variance quantities;
- comment;
- count/apply actor IDs and times;
- adjustment transaction ID;
- prior superseded line ID and replacement line ID where applicable.

Historical labels are projected from referenced records and remain visible when references are inactive.

## Request versions

- Add line: expected count version.
- Remove Pending: expected line version.
- Record count: expected line version.
- Apply: expected line version; balance expectation comes from persisted snapshot.
- Supersede: expected Conflict line version.
- Complete/cancel: expected count version.

Malformed or stale versions are rejected before state change.

## Persistence impact

One developer-generated migration is required for:

- `wms.inventory_counts`;
- `wms.inventory_count_lines`;
- rowversions;
- actor and lifecycle columns;
- count/line/reference/self-reference foreign keys;
- filtered uniqueness and supporting indexes;
- `WmsDbContext` model snapshot updates.
