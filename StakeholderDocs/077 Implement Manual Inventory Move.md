# Implement Manual Inventory Move

## Summary

Warehouse operators need a fast way to manually move SKU quantity from one storage location to another storage location in the same warehouse without creating an inventory transfer document in advance.

This feature represents an ad-hoc physical relocation performed directly by a warehouse operator. It must update inventory balances and inventory ledger history, but it must not create or depend on `InventoryTransfer`.

## Business context

In real warehouse operations, goods are often moved between storage locations by warehouse staff based on the current operational situation. The operator may decide to reorganize goods, free up a location, consolidate stock, or move goods to a more convenient location without a pre-created transfer request.

The system must support this practical workflow while keeping inventory balances and ledger history accurate.

## Goal

Allow an operator to manually move SKU quantity from one regular storage location to another regular storage location within the same warehouse.

The operation must:

* decrease the source inventory balance;
* increase or create the destination inventory balance;
* create one inventory transaction of type `Transfer`;
* create exactly two inventory ledger entries;
* record the reason for the move;
* return a result that can be displayed in the UI.

## Non-goals

This feature must not implement:

* planned inventory transfer documents;
* `InventoryTransfer`, `InventoryTransferLine`, or `InventoryTransferMovement`;
* requested quantity tracking;
* transfer progress or completion status;
* inter-warehouse transfer;
* internal/external transit workflow;
* scanner UI;
* LPN, package, batch, serial, expiry, reservation, discrepancy, approval, or route optimization;
* inventory adjustment;
* EF migration unless strictly required.

## User stories

### User Story 1: Move inventory manually from balance row

As a warehouse operator, I want to move quantity from an existing inventory balance row to another storage location, so that I can record a real physical relocation without creating a transfer document.

Acceptance criteria:

* The operator can open a manual move action from the Inventory Balances grid.
* The dialog shows SKU, source location, current source quantity, and base unit of measure.
* The operator selects a destination storage location.
* The operator enters a quantity greater than zero.
* The operator enters or confirms a reason.
* After confirmation, the system updates source and destination balances.
* The UI shows a clear success result with source and destination quantities before and after the move.

### User Story 2: Lookup balance by SKU and location

As a future scanner workflow, the system needs to retrieve the current inventory balance by SKU and source storage location, so that a mobile/scanner client can validate available quantity before submitting a manual move.

Acceptance criteria:

* The API supports looking up an inventory balance by SKU and storage location.
* If the balance exists, the API returns `InventoryBalanceDetails`.
* If the balance does not exist, the API returns not found.
* The returned details include the current quantity and source balance version.

### User Story 3: Preserve inventory ledger accuracy

As an inventory controller, I want every manual move to be visible in inventory ledger history, so that stock movements remain auditable.

Acceptance criteria:

* A successful manual move creates one `InventoryTransaction` with transaction type `Transfer`.
* A successful manual move creates exactly two `InventoryLedgerEntry` rows:

  * one negative entry for the source location;
  * one positive entry for the destination location.
* The source and destination ledger entries balance to zero.
* The transaction reason is stored and visible through existing ledger history.

## Business rules

* Source SKU is required.
* Source storage location is required.
* Destination storage location is required.
* Source and destination storage locations must be different.
* Quantity must be greater than zero.
* Source inventory balance must exist.
* Quantity must not exceed the current source balance quantity.
* Source balance version must match the expected source balance version supplied by the client.
* Destination inventory balance may already exist or may be created by the operation.
* Source and destination storage locations must belong to the same warehouse.
* Source and destination storage locations must be active.
* Source and destination storage location types and statuses must be active.
* Source and destination storage locations must be regular storage locations.
* `INTERNAL_TRANSIT` and `EXTERNAL_TRANSIT` locations must not be used for manual inventory move.
* Reason is required and must not exceed `InventoryTransaction.ReasonMaxLength`.
* The operation must be atomic.

## API expectations

Add a read endpoint for scanner-ready lookup:

```text
GET /api/wms/inventory/balances/lookup?skuId={skuId}&storageLocationId={storageLocationId}
```

Feature name:

```text
GetInventoryBalanceBySkuAndStorageLocation
```

Add a write endpoint for manual inventory move:

```text
POST /api/wms/inventory/balances/move
```

Request contract:

```csharp
public sealed record MoveInventoryBalanceRequest(
    Guid? StockKeepingUnitId,
    Guid? SourceStorageLocationId,
    Guid? DestinationStorageLocationId,
    decimal Quantity,
    string? Reason,
    string? ExpectedSourceBalanceVersion);
```

Result contract:

```csharp
public sealed record MoveInventoryBalanceResult(
    InventoryBalanceDetails SourceBalance,
    InventoryBalanceDetails DestinationBalance,
    decimal MovedQuantity,
    decimal SourceQuantityBefore,
    decimal SourceQuantityAfter,
    decimal DestinationQuantityBefore,
    decimal DestinationQuantityAfter,
    DateTimeOffset OccurredAtUtc);
```

Feature name:

```text
MoveInventoryBalance
```

## UI expectations

Update the Inventory Balances page:

* Add a `Move` action to each inventory balance row.
* Open a manual move dialog from the selected balance row.
* Show SKU, source warehouse, source storage location, current quantity, and base unit of measure as read-only context.
* Allow the operator to select a destination storage location from the same warehouse.
* Destination storage location lookup must exclude `INTERNAL_TRANSIT` and `EXTERNAL_TRANSIT` locations.
* Require quantity.
* Require reason.
* On successful move, show a clear result:

  * moved quantity;
  * source quantity before and after;
  * destination quantity before and after.
* Refresh the Inventory Balances grid after success.

## Validation and error handling

The system must return a clear validation or conflict result when:

* source balance does not exist;
* source balance version is stale;
* quantity is zero or negative;
* quantity exceeds source balance quantity;
* destination location does not exist;
* destination location belongs to another warehouse;
* destination location is the same as source location;
* source or destination location is inactive;
* source or destination location type/status is inactive;
* source or destination location is an internal or external transit location;
* reason is missing or too long.

## Testing expectations

Add tests for:

* successful move to an existing destination balance;
* successful move creating a missing destination balance;
* source balance quantity decreases;
* destination balance quantity increases;
* one transfer transaction is created;
* exactly two ledger entries are created;
* ledger entries balance to zero;
* stale source row version returns conflict;
* missing source balance returns not found or conflict according to project conventions;
* quantity greater than source balance is rejected;
* same source and destination location is rejected;
* destination location from another warehouse is rejected;
* transit source or destination location is rejected;
* missing or too long reason is rejected;
* balance lookup by SKU and storage location returns details;
* balance lookup by SKU and storage location returns not found when balance does not exist;
* WebApp API client builds the expected URLs and request body.

## Implementation constraints

* Do not create `InventoryTransfer`.
* Do not introduce a new document aggregate.
* Do not introduce scanner UI in this issue.
* Do not introduce inter-warehouse transfer behavior.
* Reuse existing inventory balance, inventory transaction, and inventory ledger patterns.
* Reuse existing projection patterns for `InventoryBalanceDetails`.
* Reuse existing storage location lookup behavior where possible.
* No migration is expected unless implementation reveals a strict need.

## Developer-controlled validation

Recommended validation commands after implementation:

```powershell
dotnet build
dotnet test
```

Do not run build, tests, migrations, database update, app startup, Docker, or infrastructure commands automatically from agent workflow.
