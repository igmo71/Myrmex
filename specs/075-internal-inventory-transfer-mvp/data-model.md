# Data Model: Internal Inventory Transfer MVP

## InventoryTransfer

Represents the transfer document for moving inventory inside one warehouse.

### Fields

- `Id`: transfer identity.
- `Code`: human-readable transfer code.
- `SourceWarehouseId`: required warehouse identity.
- `DestinationWarehouseId`: required warehouse identity, same as source for this MVP.
- `TransitStorageLocationId`: nullable storage location identity. Null means direct transfer; non-null means internal-transit transfer.
- `Status`: `Created`, `InProgress`, or `Completed`.
- `CreatedAtUtc`: creation time.
- `UpdatedAtUtc`: optional update time.
- `Lines`: transfer line collection.
- `Movements`: committed movement collection.

### Validation Rules

- Source warehouse is required.
- Destination warehouse is required.
- Source warehouse must equal destination warehouse.
- Transit location is optional.
- When transit location is present, it must belong to the transfer warehouse, be active, and have storage-location type `InternalTransit`.
- Transfer must contain at least one line.
- Completed transfer cannot accept new movements.
- Direct and transit movement patterns cannot be mixed inside one transfer document.
- No persisted transfer execution mode is introduced.

### State Transitions

```text
Created
  -> InProgress after first movement
  -> Completed after final movement when all lines are fully placed and no in-transit quantity remains

InProgress
  -> Completed after final movement when all lines are fully placed and no in-transit quantity remains

Completed
  -> terminal for MVP movement execution
```

## InventoryTransferLine

Represents requested movement of one SKU from one source storage location to one destination storage location.

### Fields

- `Id`: line identity.
- `InventoryTransferId`: parent transfer identity.
- `StockKeepingUnitId`: required SKU identity.
- `SourceStorageLocationId`: required regular storage location identity.
- `DestinationStorageLocationId`: required regular storage location identity.
- `RequestedQuantity`: positive requested quantity.

### Computed Quantities

- `PickedQuantity`: sum of movement quantities from the line source location.
- `PlacedQuantity`: sum of movement quantities to the line destination location.
- `InTransitQuantity`: for transit transfers, sum into transit minus sum out of transit; zero for direct transfers.
- `RemainingToPickQuantity`: requested minus picked.
- `RemainingToPlaceQuantity`: picked minus placed.

### Validation Rules

- SKU is required and active.
- Source location is required, active, belongs to the transfer warehouse, and is a regular storage location.
- Destination location is required, active, belongs to the transfer warehouse, and is a regular storage location.
- Source and destination locations must be different.
- Source and destination cannot be internal or external transit locations.
- Requested quantity must be greater than zero.
- Picked quantity must never exceed requested quantity.
- Placed quantity must never exceed picked quantity.
- Placed quantity must never exceed requested quantity.
- In-transit quantity must never be negative.

## InventoryTransferMovement

Represents an immutable committed physical movement fact for one transfer line.

### Fields

- `Id`: movement identity.
- `InventoryTransferId`: parent transfer identity.
- `InventoryTransferLineId`: line identity.
- `FromStorageLocationId`: movement source location.
- `ToStorageLocationId`: movement destination location.
- `Quantity`: positive moved quantity.
- `InventoryTransactionId`: inventory transaction created for this movement.
- `OccurredAtUtc`: movement occurrence time.
- `CreatedAtUtc`: persistence creation time if inherited by local entity base.

### Validation Rules

- Movement quantity must be greater than zero.
- Movement must belong to an existing transfer.
- Movement must belong to an existing line on the same transfer.
- Movement cannot be added to a completed transfer.
- Movement must reference exactly one created inventory transaction.
- Movement is immutable after creation.
- Movement type is not persisted.
- Scanner workflow state is not persisted.

### Allowed Movement Shapes

Direct transfer without transit:

```text
From = line.SourceStorageLocationId
To   = line.DestinationStorageLocationId
```

Transit transfer pick:

```text
From = line.SourceStorageLocationId
To   = transfer.TransitStorageLocationId
```

Transit transfer place:

```text
From = transfer.TransitStorageLocationId
To   = line.DestinationStorageLocationId
```

## InventoryTransaction

Existing immutable inventory transaction aggregate reused for transfer ledger effects.

### Changes

- Add `InventoryTransactionType.Transfer = 2`.
- Add a transfer creation path that creates one transaction with exactly two ledger entries.
- Do not add transfer-specific source-reference fields in this MVP.

### Transfer Rules

- Transaction type is `Transfer`.
- The transaction represents one committed transfer movement.
- The transaction has exactly two ledger entries: one negative entry for the from location and one positive entry for the to location.
- Transfer linkage is from `InventoryTransferMovement.InventoryTransactionId` to this transaction.

## InventoryLedgerEntry

Existing immutable quantity-change entry reused for transfer effects.

### Transfer Entry Rules

- From-location entry: same SKU as the transfer line, from storage location, negative quantity delta, and before/after balances for the from location.
- To-location entry: same SKU as the transfer line, to storage location, positive quantity delta, and before/after balances for the to location.

## InventoryBalance

Existing current-state SKU/location quantity snapshot reused for transfer movement.

### Transfer Rules

- Direct and pick commands require sufficient quantity in the movement from location.
- Place commands require sufficient in-transit quantity by transfer progress and must preserve non-negative transit balance.
- Movement decreases the from-location balance and increases the to-location balance.
- Existing rowversion/current-state behavior should be preserved for balance rows touched by movement commands.

## StorageLocationType

Existing reference data extended for transit behavior.

### Added Values

- `InternalTransit`: used by internal transfer through transit.
- `ExternalTransit`: future-compatible reference value only; no behavior implemented in this MVP.

### Rules

- Regular storage locations are valid line source/destination locations.
- `InternalTransit` is valid only as transfer transit location.
- `ExternalTransit` has no transfer behavior in this MVP.

## Relationships

```text
InventoryTransfer 1 -> many InventoryTransferLine
InventoryTransfer 1 -> many InventoryTransferMovement
InventoryTransferLine 1 -> many InventoryTransferMovement
InventoryTransferMovement many -> 1 InventoryTransaction
InventoryTransaction 1 -> many InventoryLedgerEntry
InventoryLedgerEntry many -> 1 StockKeepingUnit
InventoryLedgerEntry many -> 1 StorageLocation
InventoryBalance many -> 1 StockKeepingUnit
InventoryBalance many -> 1 StorageLocation
```

Delete behavior should restrict deletion of referenced SKU, warehouse, storage-location, and inventory transaction records when transfer history exists.

## Persistence Notes

- Add transfer tables for transfers, lines, and movements.
- Add indexes for transfer code, warehouse/status/created date list filters, line SKU/source/destination filters, and movement transaction lookup.
- Keep `TransitStorageLocationId` nullable.
- Use decimal precision consistent with Inventory Balance and Ledger quantities.
- Migration is created during implementation only.
