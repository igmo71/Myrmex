# Data Model: Local Receiving Order MVP

## Ownership Boundaries

- `Myrmex.Modules.Wms.Receiving` owns Receiving Order, Receiving Order Line, lifecycle, plan reconciliation, and completion orchestration.
- WMS Topology continues to own Warehouse, StorageLocation, StorageLocationType, and StorageLocationStatus.
- WMS Inventory continues to own InventoryBalance, InventoryTransaction, and InventoryLedgerEntry. Receiving invokes their domain behavior but does not maintain a separate quantity.
- `Myrmex.Shared.Wms.Receiving` exposes request/read contracts only; it never exposes domain or EF entities.
- `Myrmex.WebApp` holds unsaved Draft page state and invokes the public contracts; all rules remain authoritative on the server.

## Enum: ReceivingOrderStatus

| Value | Persisted text | Meaning |
|---:|---|---|
| 1 | `Draft` | Header and complete plan are editable; no inventory effect exists. |
| 2 | `InProgress` | Header and plan are immutable; positive received quantities may accumulate. |
| 3 | `Completed` | All lines were fully received and one inventory transaction was atomically posted. |

No Cancelled value or generic state-machine representation is added.

## Aggregate Root: ReceivingOrder

| Field | Type | Required | Rules |
|---|---|---:|---|
| `Id` | `Guid` | Yes | Inherited GUID v7 identity. |
| `Number` | `string` | Yes | `DomainText.NormalizeCode`; non-empty; maximum `DomainTextLengths.Code` (32); globally unique. |
| `WarehouseId` | `Guid` | Yes | Existing active Warehouse at create/update/start. |
| `ReceivingLocationId` | `Guid` | Yes | Existing eligible Receiving StorageLocation in `WarehouseId`. |
| `Status` | `ReceivingOrderStatus` | Yes | Starts Draft; only Draft → InProgress → Completed. |
| `StartedAtUtc` | `DateTimeOffset?` | No | Null in Draft; set once on Start. |
| `CompletedAtUtc` | `DateTimeOffset?` | No | Null until atomic completion; set once. |
| `InventoryTransactionId` | `Guid?` | No | Null until completion; references the one Receiving transaction. |
| `CreatedAtUtc` | `DateTimeOffset` | Yes | Inherited creation timestamp. |
| `UpdatedAtUtc` | `DateTimeOffset?` | No | Updated by every aggregate mutation. |
| `RowVersion` | `byte[8]` | Yes | SQL Server rowversion; transported as Base64 `OrderVersion`. |
| `Lines` | private collection | Yes | At least one; all owned mutations occur through the aggregate. |

### Aggregate Invariants

- Draft: `StartedAtUtc`, `CompletedAtUtc`, and `InventoryTransactionId` are null; every line has zero received quantity.
- InProgress: `StartedAtUtc` is non-null; `CompletedAtUtc` and `InventoryTransactionId` are null.
- Completed: `StartedAtUtc`, `CompletedAtUtc`, and `InventoryTransactionId` are all non-null; every line has `ReceivedQuantity == PlannedQuantity`.
- Number, Warehouse, ReceivingLocation, line set, SKU assignments, and planned quantities are immutable after Start.
- Number, WarehouseId, ReceivingLocationId, and every required identifier are non-empty.
- At least one line exists and no SKU occurs more than once.
- All quantities use the SKU base unit; planned quantity is positive and received quantity stays between zero and planned inclusive.
- Every child change calls aggregate `Touch()` so the parent rowversion advances when saved.

### Aggregate Operations

| Operation | Allowed state | Result |
|---|---|---|
| `Create` | New | Valid Draft with complete initial plan and zero received quantities. |
| `ReplaceDraft` | Draft | Header and complete plan reconciled by LineId after full validation. |
| `Start` | Draft | Revalidates plan externally, sets InProgress and `StartedAtUtc`. |
| `Start` | InProgress | Idempotent current result; no timestamp/version mutation. |
| `Receive` | InProgress | Adds a positive amount to one line without exceeding planned. |
| `Complete` | InProgress and fully received | Sets Completed, timestamp, and transaction reference together. |
| `ValidateDelete` | Draft with no effect | Permits physical deletion by the handler. |

## Entity: ReceivingOrderLine

| Field | Type | Required | Rules |
|---|---|---:|---|
| `Id` | `Guid` | Yes | Inherited GUID v7 identity; preserved for retained Draft lines. |
| `ReceivingOrderId` | `Guid` | Yes | Required owner reference. |
| `StockKeepingUnitId` | `Guid` | Yes | Existing active SKU with active base UOM at create/update/start. |
| `PlannedQuantity` | `decimal(18,4)` | Yes | Greater than zero. |
| `ReceivedQuantity` | `decimal(18,4)` | Yes | Starts zero; never negative or above planned. |
| `CreatedAtUtc` | `DateTimeOffset` | Yes | Inherited timestamp. |
| `UpdatedAtUtc` | `DateTimeOffset?` | No | Updated when a retained Draft line changes or receipt accumulates. |

There is no line status and no line rowversion. Remaining quantity is derived as `PlannedQuantity - ReceivedQuantity`.

### Draft Reconciliation Algorithm

1. Materialize the full submitted line list before mutation.
2. Reject duplicate non-null LineIds.
3. Reject every non-null LineId not found in the current order.
4. Validate every identifier and positive planned quantity.
5. Reject duplicate final SKU IDs.
6. Update retained entities in place, preserving their IDs.
7. Remove existing entities omitted from the submitted set.
8. Create entities for null-ID entries only.
9. Return removed entities to the handler for explicit persistence deletion.
10. Touch the aggregate once after the valid replacement is applied.

If any validation fails, the existing aggregate and tracked line set remain unchanged.

## Existing Entity Classification: Receiving StorageLocationType

Add one system `StorageLocationType` through the established topology seed pattern:

| Property | Planned value |
|---|---|
| Technical code | `RECEIVING` |
| Identity | Stable new `WmsSeedIds` value selected in the implementation migration |
| System row | Yes |
| Active initially | Yes |
| Semantic scope | The single StorageLocation type eligible as ReceivingLocation in this MVP |

An eligible ReceivingLocation:

- is an existing active StorageLocation;
- belongs to the selected active Warehouse;
- has an active StorageLocationType with code `RECEIVING`;
- has an active StorageLocationStatus through existing selectable/inventory eligibility rules;
- passes existing Inventory Balance creation eligibility.

This adds no ReceivingLocation entity, dock/door/staging model, capability flags, type collection, or multiple Receiving categories. Add one new demo StorageLocation with this type; keep legacy DOCK rows unchanged.

## Existing Aggregate: InventoryBalance

No fields or relationships change. During completion, for every order line at `ReceivingLocationId`:

- load an existing balance by unique `(StockKeepingUnitId, StorageLocationId)` when present;
- otherwise validate existing creation eligibility and create at the received quantity;
- for an existing balance, call `UpdateQuantity(balanceBefore + receivedQuantity)`;
- retain its existing `decimal(18,4)` quantity, non-negative validation, rowversion, and unique pair index.

All balances remain tracked until the one completion save.

## Existing Aggregate Extension: InventoryTransaction

Extend `InventoryTransactionType` with `Receiving`. Because the enum is stored as text, no numeric compatibility rule or schema change is needed for the enum value.

Add a narrow transient input used only by `CreateReceiving`:

| Field | Type | Rules |
|---|---|---|
| `StockKeepingUnitId` | `Guid` | Required; one order line SKU. |
| `StorageLocationId` | `Guid` | Required; always the order ReceivingLocation. |
| `QuantityDelta` | `decimal` | Strictly positive and equal to received quantity. |
| `BalanceBefore` | `decimal` | Non-negative. |
| `BalanceAfter` | `decimal` | `BalanceBefore + QuantityDelta`; non-negative. |

`CreateReceiving` requires at least one change, validates reason and all changes, calls existing `InventoryLedgerEntry.Create` once per change, and returns one transaction whose entry count equals the order line count. This input is not persisted and is not generalized for other posting sources.

## Relationships and Delete Behavior

| Principal | Dependent/reference | Cardinality | Delete behavior |
|---|---|---|---|
| Warehouse | ReceivingOrder | One-to-many | Restrict |
| StorageLocation | ReceivingOrder.ReceivingLocation | One-to-many | Restrict |
| InventoryTransaction | ReceivingOrder | One-to-zero/one | Restrict |
| ReceivingOrder | ReceivingOrderLine | One-to-many | Restrict; Draft handler explicitly removes lines first |
| StockKeepingUnit | ReceivingOrderLine | One-to-many | Restrict |

Completed documents, inventory transactions, and ledger entries cannot be cascaded through Receiving deletion.

## Persistence Constraints and Indexes

### `receiving_orders`

- Primary key on `Id`.
- Unique index on normalized `Number`.
- Indexes on `WarehouseId`, `ReceivingLocationId`, `Status`, and `CreatedAtUtc`.
- Unique filtered index on non-null `InventoryTransactionId`.
- Required string status mapping with sufficient length for `InProgress` and `Completed`.
- SQL Server rowversion column `row_version`.
- Required/restrict foreign keys described above.

### `receiving_order_lines`

- Primary key on `Id`.
- Unique index on `(ReceivingOrderId, StockKeepingUnitId)`.
- Index on `StockKeepingUnitId`.
- Required/restrict owner and SKU foreign keys.
- `decimal(18,4)` planned and received quantity columns.
- No rowversion or persisted line status.

Named unique indexes are added to `WmsDatabaseNames` and mapped by `WmsPersistenceExceptionMapper` to stable duplicate Number and duplicate SKU conflicts.

## State Transitions

| Current state | Input/action | Next state | Inventory effect |
|---|---|---|---|
| New | Create valid complete plan | Draft | None |
| Draft | Replace valid full plan | Draft | None |
| Draft | Delete with current version | Deleted | None; Number released |
| Draft | Start with current version and eligible references | InProgress | None |
| InProgress | Start again | InProgress | None; current result returned |
| InProgress | Receive valid line quantity | InProgress | None |
| InProgress | Complete while any line short | Rejected | None |
| InProgress | Complete fully received order | Completed | All balances and one transaction posted atomically |
| Completed | Complete again | Completed | None; current result returned |
| Completed | Any mutation/delete | Rejected | None |

## Concurrency and Completion Idempotency

- A mutating request carries the Base64 aggregate `OrderVersion` most recently read.
- The handler validates format and compares it with the loaded ReceivingOrder RowVersion before a real mutation.
- EF rowversion catches a race after the comparison.
- Balance rowversions and the unique SKU/location index independently protect inventory.
- Completion creates the entire tracked graph and attempts one save only.
- Existing aggregate domain events are captured before that save and dispatched/cleared only after it succeeds; posting itself is not event-driven.
- On a concurrency or missing-balance unique race, the handler clears/detaches failed state and reloads the order without tracking.
- If the reloaded order is Completed and has both `CompletedAtUtc` and `InventoryTransactionId`, return current details successfully.
- Otherwise return a conflict and require the caller to refresh; never rerun posting automatically.

## Draft Deletion

- Load the order and all lines.
- Validate expected aggregate version.
- Require Draft, null `StartedAtUtc`, null `CompletedAtUtc`, null `InventoryTransactionId`, and zero received quantities.
- Explicitly mark lines deleted, then mark the order deleted.
- Use one SaveChanges so a parent rowversion race rolls back every line delete.
- Successful physical deletion releases Number for reuse.

## Read Models

- List projection returns order version, header/status/timestamps, Warehouse and ReceivingLocation summaries, line count, totals, and optional transaction reference without loading tracked aggregates.
- Details projection returns the same header plus ordered lines, SKU/base-UOM summaries, planned/received/remaining quantities, and no line version.
- Status and remaining quantities are exposed as derived read data; they are not separate persistence fields.
